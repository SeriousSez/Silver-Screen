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

        public MovieProductionState CurrentState { get; private set; }
        public Employee AssignedDirector { get; private set; }
        public bool DirectorArrivedAtStage { get; private set; }

        private readonly List<MovieRole> _roles = new List<MovieRole>();
        public IReadOnlyList<MovieRole> Roles => _roles;

        public float ProductionProgress { get; private set; }
        public SimulationDateTime CreatedDate { get; }

        public bool HasDirector => AssignedDirector != null;
        public bool AllRolesCast => _roles.Count > 0 && _roles.TrueForAll(r => r.AssignedActor != null);
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
        }

        public void AddRole(MovieRole role)
        {
            if (role == null || _roles.Contains(role)) return;
            _roles.Add(role);
            role.OnRoleUpdated += HandleRoleUpdated;
            OnProjectUpdated?.Invoke(this);
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

        public MovieRole GetRole(string roleId) => _roles.Find(r => r.Id == roleId);

        public bool IsActorAssignedAnyRole(Employee actor)
        {
            if (actor == null) return false;
            return _roles.Exists(r => r.AssignedActor == actor);
        }

        public void AssignDirector(Employee director)
        {
            if (AssignedDirector == director) return;
            AssignedDirector = director;
            DirectorArrivedAtStage = false;
            OnProjectUpdated?.Invoke(this);
        }

        public void AssignActorToRole(string roleId, Employee actor)
        {
            var role = GetRole(roleId);
            if (role == null) return;

            // Enforce: an actor may only occupy one role in the same movie
            if (actor != null && IsActorAssignedAnyRole(actor))
            {
                foreach (var r in _roles)
                {
                    if (r.AssignedActor == actor) r.ClearActor();
                }
            }

            role.AssignActor(actor);
            OnProjectUpdated?.Invoke(this);
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

        private void HandleRoleUpdated(MovieRole role)
        {
            OnProjectUpdated?.Invoke(this);
        }
    }
}

