using System;

namespace SilverScreen.Domain.Movie
{
    public class MovieRole
    {
        public string Id { get; }
        public MovieRoleType RoleType { get; set; }
        public string CharacterName { get; set; }
        public Employee AssignedActor { get; private set; }

        public bool CastingCompleted { get; private set; }
        public int CastingMinutesElapsed { get; private set; }
        public bool IsCastingActive { get; private set; }
        public bool ArrivedAtStage { get; private set; }

        public event Action<MovieRole> OnRoleUpdated;

        public MovieRole(string id, MovieRoleType roleType, string characterName)
        {
            Id = id ?? Guid.NewGuid().ToString();
            RoleType = roleType;
            CharacterName = string.IsNullOrWhiteSpace(characterName) ? (roleType == MovieRoleType.Protagonist ? "Lead" : "Supporting") : characterName;
            CastingCompleted = false;
            CastingMinutesElapsed = 0;
            IsCastingActive = false;
            ArrivedAtStage = false;
        }

        public void AssignActor(Employee actor)
        {
            if (AssignedActor == actor) return;
            AssignedActor = actor;
            CastingCompleted = false;
            CastingMinutesElapsed = 0;
            IsCastingActive = false;
            ArrivedAtStage = false;
            OnRoleUpdated?.Invoke(this);
        }

        public void ClearActor()
        {
            AssignedActor = null;
            CastingCompleted = false;
            CastingMinutesElapsed = 0;
            IsCastingActive = false;
            ArrivedAtStage = false;
            OnRoleUpdated?.Invoke(this);
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
