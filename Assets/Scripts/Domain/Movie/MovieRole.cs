using System;

namespace SilverScreen.Domain.Movie
{
    public class MovieRole
    {
        public string Id { get; }
        public MovieRoleType RoleType { get; private set; }
        public MovieRoleProminence Prominence { get; private set; }
        public string CharacterName { get; private set; }
        public string Description { get; private set; }
        public string AssignedActorId { get; private set; }

        // Temporary runtime compatibility bridge for existing routing, UI, and quality code.
        // AssignedActorId is the stable cast assignment identity.
        public Employee AssignedActor { get; private set; }

        public bool CastingCompleted { get; private set; }
        public int CastingMinutesElapsed { get; private set; }
        public bool IsCastingActive { get; private set; }
        public bool ArrivedAtStage { get; private set; }

        public event Action<MovieRole> OnRoleUpdated;

        public MovieRole(string id, MovieRoleType roleType, string characterName)
            : this(
                id,
                roleType == MovieRoleType.Protagonist
                    ? MovieRoleProminence.Lead
                    : MovieRoleProminence.Supporting,
                characterName)
        {
            RoleType = roleType;
        }

        public MovieRole(
            string id,
            MovieRoleProminence prominence,
            string characterName,
            string description = null)
        {
            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id.Trim();
            Prominence = prominence;
            RoleType = prominence == MovieRoleProminence.Lead
                ? MovieRoleType.Protagonist
                : MovieRoleType.Supporting;
            CharacterName = string.IsNullOrWhiteSpace(characterName)
                ? prominence == MovieRoleProminence.Lead ? "Lead Character" : "Supporting Character"
                : characterName.Trim();
            Description = description?.Trim();
            AssignedActorId = null;
            CastingCompleted = false;
            CastingMinutesElapsed = 0;
            IsCastingActive = false;
            ArrivedAtStage = false;
        }

        public void UpdateCharacter(string characterName, string description, MovieRoleProminence prominence)
        {
            if (string.IsNullOrWhiteSpace(characterName))
                throw new ArgumentException("A fictional character requires a name.", nameof(characterName));

            CharacterName = characterName.Trim();
            Description = description?.Trim();
            Prominence = prominence;
            RoleType = prominence == MovieRoleProminence.Lead
                ? MovieRoleType.Protagonist
                : MovieRoleType.Supporting;
            OnRoleUpdated?.Invoke(this);
        }

        public bool TryAssignActor(Employee actor)
        {
            if (actor == null || actor.Role != EmployeeRole.Actor) return false;
            if (AssignedActorId == actor.Id)
            {
                AssignedActor = actor;
                return true;
            }

            AssignedActorId = actor.Id;
            AssignedActor = actor;
            ResetCastingState();
            OnRoleUpdated?.Invoke(this);
            return true;
        }

        public void AssignActor(Employee actor)
        {
            TryAssignActor(actor);
        }

        public void ClearActor()
        {
            AssignedActorId = null;
            AssignedActor = null;
            ResetCastingState();
            OnRoleUpdated?.Invoke(this);
        }

        private void ResetCastingState()
        {
            CastingCompleted = false;
            CastingMinutesElapsed = 0;
            IsCastingActive = false;
            ArrivedAtStage = false;
        }

        public void StartCasting()
        {
            IsCastingActive = true;
            CastingCompleted = false;
            OnRoleUpdated?.Invoke(this);
        }

        public void AdvanceCastingMinute()
        {
            if (!IsCastingActive || CastingCompleted) return;

            CastingMinutesElapsed++;
            if (CastingMinutesElapsed >= 60)
            {
                CompleteCasting();
            }
            else
            {
                OnRoleUpdated?.Invoke(this);
            }
        }

        public void CompleteCasting()
        {
            IsCastingActive = false;
            CastingCompleted = true;
            CastingMinutesElapsed = 60;
            OnRoleUpdated?.Invoke(this);
        }

        public void SetArrivedAtStage(bool arrived)
        {
            ArrivedAtStage = arrived;
            OnRoleUpdated?.Invoke(this);
        }

        public void ResetForProduction()
        {
            ArrivedAtStage = false;
            OnRoleUpdated?.Invoke(this);
        }
    }
}
