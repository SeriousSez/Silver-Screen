using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Movie
{
    public class MovieProject
    {
        public string Id { get; }
        public string Title { get; set; }
        public string GenreId { get; }
        public string GenreDisplayName { get; }
        public string Genre => GenreDisplayName;
        public int Budget { get; set; }
        public string BudgetTierId { get; }
        public MovieProductionResult ProductionResult { get; private set; }
        public SimulationDateTime? ReleaseDate { get; private set; }
        public MovieTheatricalRun TheatricalRun { get; private set; }
        public MovieCommercialResult CommercialResult { get; private set; }

        public MovieProductionState CurrentState { get; private set; }
        public Employee AssignedDirector { get; private set; }
        public bool DirectorArrivedAtStage { get; private set; }

        private readonly List<MovieRole> _roles = new List<MovieRole>();
        public IReadOnlyList<MovieRole> CastRoles => _roles;
        // Compatibility alias retained for the existing casting and production UI.
        public IReadOnlyList<MovieRole> Roles => _roles;

        private readonly List<MovieScene> _scenes = new List<MovieScene>();
        public IReadOnlyList<MovieScene> Scenes => _scenes;

        public float ProductionProgress { get; private set; }
        public SimulationDateTime CreatedDate { get; }

        public bool HasDirector => AssignedDirector != null;
        public bool AllRolesCast => _roles.Count > 0 && _roles.TrueForAll(r => r.AssignedActorId != null);
        public bool AllCastingComplete => _roles.Count > 0 && _roles.TrueForAll(r => r.CastingCompleted);
        public bool ReadyForFilming => HasDirector && AllRolesCast && AllCastingComplete;
        public bool AllParticipantsAtStage => DirectorArrivedAtStage && (_roles.Count == 0 || _roles.TrueForAll(r => r.ArrivedAtStage));

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
            string budgetTierId = null)
        {
            Id = id ?? Guid.NewGuid().ToString();
            Title = title;
            GenreId = string.IsNullOrWhiteSpace(genreId) ? "drama" : genreId.Trim();
            GenreDisplayName = string.IsNullOrWhiteSpace(genreDisplayName) ? GenreId : genreDisplayName.Trim();
            Budget = budget;
            BudgetTierId = string.IsNullOrWhiteSpace(budgetTierId)
                ? BudgetTier.GetIdForAmount(budget)
                : budgetTierId.Trim();
            CreatedDate = createdDate;
            CurrentState = MovieProductionState.Draft;
            ProductionProgress = 0f;
            DirectorArrivedAtStage = false;
            ProductionResult = null;
            ReleaseDate = null;
            TheatricalRun = null;
            CommercialResult = null;
        }

        public bool AddScene(MovieScene scene)
        {
            if (scene == null || _scenes.Exists(existing => existing.Id == scene.Id)) return false;

            int insertionIndex = Math.Clamp(scene.SceneNumber - 1, 0, _scenes.Count);
            _scenes.Insert(insertionIndex, scene);
            scene.OnSceneUpdated += HandleSceneUpdated;
            RenumberScenes();
            OnProjectUpdated?.Invoke(this);
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
            OnProjectUpdated?.Invoke(this);
            return true;
        }

        public MovieScene GetScene(string sceneId) =>
            string.IsNullOrWhiteSpace(sceneId) ? null : _scenes.Find(scene => scene.Id == sceneId);

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
            OnProjectUpdated?.Invoke(this);
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
                OnProjectUpdated?.Invoke(this);
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
            OnProjectUpdated?.Invoke(this);
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
            OnProjectUpdated?.Invoke(this);
            return true;
        }

        public bool UnassignActorFromCastRole(string roleId)
        {
            var role = GetCastRole(roleId);
            if (role == null || role.AssignedActorId == null) return false;
            role.ClearActor();
            OnProjectUpdated?.Invoke(this);
            return true;
        }

        public void AssignActorToRole(string roleId, Employee actor)
        {
            AssignActorToCastRole(roleId, actor);
        }

        public void SetDirectorArrivedAtStage(bool arrived)
        {
            DirectorArrivedAtStage = arrived;
            OnProjectUpdated?.Invoke(this);
        }

        public void SetState(MovieProductionState newState)
        {
            if (CurrentState == newState) return;
            CurrentState = newState;
            OnStateChanged?.Invoke(this, newState);
            OnProjectUpdated?.Invoke(this);
        }

        public void SetProgress(float progress)
        {
            float clamped = Math.Clamp(progress, 0f, 1f);
            if (Math.Abs(ProductionProgress - clamped) < 0.0001f) return;
            ProductionProgress = clamped;
            OnProgressChanged?.Invoke(this, ProductionProgress);
            OnProjectUpdated?.Invoke(this);
        }

        public bool TrySetProductionResult(MovieProductionResult result)
        {
            if (result == null || ProductionResult != null) return false;
            ProductionResult = result;
            OnProjectUpdated?.Invoke(this);
            return true;
        }

        public bool TryRelease(SimulationDateTime releaseDate, MovieTheatricalRun theatricalRun)
        {
            if (CurrentState != MovieProductionState.Completed ||
                ReleaseDate.HasValue ||
                theatricalRun == null)
            {
                return false;
            }

            ReleaseDate = releaseDate;
            TheatricalRun = theatricalRun;
            SetState(MovieProductionState.Released);
            return true;
        }

        public bool TrySetCommercialResult(MovieCommercialResult result)
        {
            if (result == null || CommercialResult != null || TheatricalRun == null || !TheatricalRun.IsCompleted)
            {
                return false;
            }

            CommercialResult = result;
            OnProjectUpdated?.Invoke(this);
            return true;
        }

        private void HandleSceneUpdated(MovieScene scene)
        {
            OnProjectUpdated?.Invoke(this);
        }
        private void HandleRoleUpdated(MovieRole role)
        {
            OnProjectUpdated?.Invoke(this);
        }
    }
}

