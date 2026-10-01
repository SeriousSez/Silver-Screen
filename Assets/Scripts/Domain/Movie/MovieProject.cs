using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Writing;

namespace SilverScreen.Domain.Movie
{
    public class MovieProject
    {
        public string Id { get; }
        public string Title { get; set; }
        public string GenreId { get; }
        public string GenreDisplayName { get; }
        public string Genre => GenreDisplayName;
        public string SourceScreenplayId { get; }
        public ProductionAuthoringIntent AuthoringIntent { get; internal set; }
        public bool IsScreenplayAdaptation => !string.IsNullOrEmpty(SourceScreenplayId);
        public int Budget { get; set; }
        public string BudgetTierId { get; }
        public MovieProductionResult ProductionResult { get; private set; }
        public SimulationDateTime? ReleaseDate { get; private set; }
        public MovieTheatricalRun TheatricalRun { get; private set; }
        public MovieCommercialResult CommercialResult { get; private set; }
        public ProductionControlMode ProductionControlMode { get; private set; }

        // A summary of owned facts, never an independently writable completion flag.
        public MovieProductionState CurrentState => ReleaseDate.HasValue
            ? MovieProductionState.Released
            : IsProductionComplete ? MovieProductionState.Completed
            : IsFilming ? MovieProductionState.Filming
            : ReadyForFilming ? MovieProductionState.ReadyToFilm
            : _roles.Exists(role => role.AssignedActorId != null)
                ? MovieProductionState.Casting : MovieProductionState.Draft;
        private MovieProductionState _lastNotifiedState;
        private float _lastNotifiedProgress;
        public Employee AssignedDirector { get; private set; }
        public bool DirectorArrivedAtStage { get; private set; }

        private readonly List<MovieRole> _roles = new List<MovieRole>();
        public IReadOnlyList<MovieRole> CastRoles { get; }
        // Compatibility alias retained for the existing casting and production UI.
        public IReadOnlyList<MovieRole> Roles => CastRoles;

        private readonly List<MovieScene> _scenes = new List<MovieScene>();
        public IReadOnlyList<MovieScene> Scenes { get; }

        // Completed scenes only. Active take work is reported separately by the coordinator.
        public float ProductionProgress => HasScenes ? (float)CompletedSceneCount / _scenes.Count : 0f;
        public int CompletedSceneCount
        {
            get
            {
                int count = 0;
                foreach (var scene in _scenes) if (scene.IsCompleted) count++;
                return count;
            }
        }
        public bool HasScenes => _scenes.Count > 0;
        public bool IsFilming => _scenes.Exists(scene => scene.Status == MovieSceneStatus.Filming);
        public bool IsProductionComplete => AllScenesCompleted;
        public bool CanRelease => IsProductionComplete && ProductionResult != null && !ReleaseDate.HasValue;
        public bool CastingReady => HasDirector && AllRolesCast && AllCastingComplete;
        public SimulationDateTime CreatedDate { get; }

        public bool HasDirector => AssignedDirector != null;
        public bool AllRolesCast => _roles.Count > 0 && _roles.TrueForAll(r => r.AssignedActorId != null);
        public bool AllCastingComplete => _roles.Count > 0 && _roles.TrueForAll(r => r.CastingCompleted);
        public bool ReadyForFilming => CastingReady && HasUnfinishedScenes && !IsFilming && !ReleaseDate.HasValue;
        public bool AllParticipantsAtStage => DirectorArrivedAtStage && (_roles.Count == 0 || _roles.TrueForAll(r => r.ArrivedAtStage));
        public bool HasUnfinishedScenes => _scenes.Exists(scene => scene.RequiresFilming);
        public bool AllScenesCompleted =>
            _scenes.Count > 0 && _scenes.TrueForAll(scene => scene.IsCompleted);

        public event Action<MovieProject> OnProjectUpdated;
        public event Action<MovieProject, MovieProductionState> OnStateChanged;
        public event Action<MovieProject, float> OnProgressChanged;

        public MovieProject(
            string id,
            string title,
            string genreId,
            string genreDisplayName,
            int budget,
            SimulationDateTime createdDate,
            string budgetTierId = null,
            string sourceScreenplayId = null)
        {
            CastRoles = _roles.AsReadOnly();
            Scenes = _scenes.AsReadOnly();
            Id = id ?? Guid.NewGuid().ToString();
            Title = title;
            GenreId = string.IsNullOrWhiteSpace(genreId) ? "drama" : genreId.Trim();
            GenreDisplayName = string.IsNullOrWhiteSpace(genreDisplayName) ? GenreId : genreDisplayName.Trim();
            SourceScreenplayId = string.IsNullOrWhiteSpace(sourceScreenplayId)
                ? string.Empty
                : sourceScreenplayId.Trim();
            Budget = budget;
            BudgetTierId = string.IsNullOrWhiteSpace(budgetTierId)
                ? BudgetTier.GetIdForAmount(budget)
                : budgetTierId.Trim();
            CreatedDate = createdDate;
            DirectorArrivedAtStage = false;
            ProductionResult = null;
            ReleaseDate = null;
            TheatricalRun = null;
            CommercialResult = null;
            ProductionControlMode = ProductionControlMode.Automatic;
        }

        public bool SetProductionControlMode(ProductionControlMode mode)
        {
            if (CurrentState == MovieProductionState.Completed ||
                CurrentState == MovieProductionState.Released ||
                ProductionControlMode == mode)
            {
                return false;
            }

            ProductionControlMode = mode;
            NotifyUpdated();
            return true;
        }

        public bool AddScene(MovieScene scene)
        {
            if (scene == null || IsProductionComplete || IsFilming ||
                _scenes.Exists(existing => existing.Id == scene.Id)) return false;

            int insertionIndex = Math.Clamp(scene.SceneNumber - 1, 0, _scenes.Count);
            _scenes.Insert(insertionIndex, scene);
            scene.OnSceneUpdated += HandleSceneUpdated;
            RenumberScenes();
            NotifyUpdated();
            return true;
        }

        public bool ReorderScene(string sceneId, int newSceneNumber)
        {
            var scene = GetScene(sceneId);
            if (scene == null || newSceneNumber < 1 || newSceneNumber > _scenes.Count) return false;

            int currentIndex = _scenes.IndexOf(scene);
            int newIndex = newSceneNumber - 1;
            if (currentIndex == newIndex) return true;

            _scenes.RemoveAt(currentIndex);
            _scenes.Insert(newIndex, scene);
            RenumberScenes();
            NotifyUpdated();
            return true;
        }

        public MovieScene GetScene(string sceneId) =>
            string.IsNullOrWhiteSpace(sceneId) ? null : _scenes.Find(scene => scene.Id == sceneId);

        public MovieScene GetNextFilmableScene()
        {
            foreach (var scene in _scenes)
            {
                if (scene.Status == MovieSceneStatus.Planned ||
                    scene.Status == MovieSceneStatus.Ready)
                {
                    return scene;
                }
            }

            return null;
        }

        public bool AddCharacterToScene(string sceneId, string castRoleId)
        {
            var scene = GetScene(sceneId);
            var castRole = GetCastRole(castRoleId);
            return scene != null && castRole != null && scene.AddCharacter(castRole.Id);
        }

        public bool AddBeatToScene(string sceneId, ScreenplayBeat beat)
        {
            var scene = GetScene(sceneId);
            if (scene == null || beat == null) return false;
            if (!ReferencesOwnedCastRole(beat.PerformingCharacterId) ||
                !ReferencesOwnedCastRole(beat.TargetCharacterId))
            {
                return false;
            }

            return scene.AddBeat(beat);
        }

        public bool AddShotToScene(string sceneId, SceneShot shot)
        {
            var scene = GetScene(sceneId);
            if (scene == null || shot == null) return false;
            if (!ReferencesOwnedCastRole(shot.SubjectCharacterId)) return false;
            return scene.AddShot(shot);
        }

        private void RenumberScenes()
        {
            for (int index = 0; index < _scenes.Count; index++)
                _scenes[index].SetSceneNumber(index + 1);
        }

        private bool ReferencesOwnedCastRole(string castRoleId) =>
            castRoleId == null || GetCastRole(castRoleId) != null;

        public bool AddCastRole(MovieRole role)
        {
            if (role == null || _roles.Exists(existing => existing.Id == role.Id)) return false;
            _roles.Add(role);
            role.OnRoleUpdated += HandleRoleUpdated;
            NotifyUpdated();
            return true;
        }

        public void AddRole(MovieRole role)
        {
            AddCastRole(role);
        }

        public void RemoveRole(string roleId)
        {
            var role = _roles.Find(r => r.Id == roleId);
            if (role != null)
            {
                role.OnRoleUpdated -= HandleRoleUpdated;
                _roles.Remove(role);
                NotifyUpdated();
            }
        }

        public MovieRole GetCastRole(string roleId) =>
            string.IsNullOrWhiteSpace(roleId) ? null : _roles.Find(role => role.Id == roleId);

        public MovieRole GetRole(string roleId) => GetCastRole(roleId);

        public bool IsActorAssignedAnyRole(Employee actor)
        {
            if (actor == null) return false;
            return _roles.Exists(role => role.AssignedActorId == actor.Id);
        }

        public void AssignDirector(Employee director)
        {
            if (AssignedDirector == director) return;
            AssignedDirector = director;
            DirectorArrivedAtStage = false;
            NotifyUpdated();
        }

        public bool AssignActorToCastRole(string roleId, Employee actor)
        {
            var role = GetCastRole(roleId);
            if (role == null || actor == null || actor.Role != EmployeeRole.Actor) return false;

            // Preserve the existing prototype rule: one actor occupies one role per movie.
            if (IsActorAssignedAnyRole(actor) && role.AssignedActorId != actor.Id)
            {
                return false;
            }

            if (!role.TryAssignActor(actor)) return false;
            NotifyUpdated();
            return true;
        }

        public bool UnassignActorFromCastRole(string roleId)
        {
            var role = GetCastRole(roleId);
            if (role == null || role.AssignedActorId == null) return false;
            role.ClearActor();
            NotifyUpdated();
            return true;
        }

        public void AssignActorToRole(string roleId, Employee actor)
        {
            AssignActorToCastRole(roleId, actor);
        }

        public void SetDirectorArrivedAtStage(bool arrived)
        {
            DirectorArrivedAtStage = arrived;
            NotifyUpdated();
        }

        public bool TrySetProductionResult(MovieProductionResult result)
        {
            if (result == null || ProductionResult != null || !IsProductionComplete) return false;
            ProductionResult = result;
            NotifyUpdated();
            return true;
        }

        public bool TryRelease(SimulationDateTime releaseDate, MovieTheatricalRun theatricalRun)
        {
            if (!CanRelease || theatricalRun == null || theatricalRun.MovieId != Id ||
                !theatricalRun.ReleaseDate.Equals(releaseDate))
            {
                return false;
            }

            ReleaseDate = releaseDate;
            TheatricalRun = theatricalRun;
            NotifyUpdated();
            return true;
        }

        public bool TrySetCommercialResult(MovieCommercialResult result)
        {
            if (result == null || CommercialResult != null || TheatricalRun == null || !TheatricalRun.IsCompleted)
            {
                return false;
            }

            CommercialResult = result;
            NotifyUpdated();
            return true;
        }

        private void NotifyUpdated()
        {
            MovieProductionState state = CurrentState;
            float progress = ProductionProgress;
            bool stateChanged = state != _lastNotifiedState;
            bool progressChanged = progress != _lastNotifiedProgress;
            _lastNotifiedState = state;
            _lastNotifiedProgress = progress;
            if (stateChanged) OnStateChanged?.Invoke(this, state);
            if (progressChanged) OnProgressChanged?.Invoke(this, progress);
            OnProjectUpdated?.Invoke(this);
        }

        private void HandleSceneUpdated(MovieScene scene)
        {
            NotifyUpdated();
        }
        private void HandleRoleUpdated(MovieRole role)
        {
            NotifyUpdated();
        }
    }
}

