using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverScreen.Domain.Recruitment
{
    public sealed class RecruitmentFacility
    {
        public string Id { get; }
        public RecruitmentDestination Destination { get; }
        public IReadOnlyList<ProfessionalRole> Professions { get; }
        internal int NextProfession;
        public bool ProfessionNeutral { get; }
        public RecruitmentDefinition Definition { get; }
        public int WaitingCapacity { get; }
        public Func<bool> IsAvailable { get; set; }
        public Func<bool> IsReachable { get; set; }
        public bool Available => IsAvailable == null || IsAvailable();
        private readonly Dictionary<string,int> _waiting = new Dictionary<string,int>();
        public IReadOnlyDictionary<string,int> WaitingReservations => _waiting;
        public bool HasWaitingCapacity => _waiting.Count < WaitingCapacity;
        public bool TryReserve(string personId,out int index)
        {
            if (_waiting.TryGetValue(personId,out index)) return true;
            for(index=0;index<WaitingCapacity;index++) if(!_waiting.ContainsValue(index)) { _waiting.Add(personId,index);return true; }
            index=-1;return false;
        }
        public void Release(string personId) => _waiting.Remove(personId);
        public RecruitmentFacility(string id, RecruitmentDestination destination, IEnumerable<ProfessionalRole> professions, int waitingCapacity = int.MaxValue, bool professionNeutral = false, RecruitmentDefinition definition = null)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("Facility instance ID required.");
            Definition=definition ?? (professionNeutral ? RecruitmentDefinitions.Talent() : destination==RecruitmentDestination.ServiceFacility ? RecruitmentDefinitions.StudioServices() : null);
            Definition?.Validate();
            Id = id; Destination = destination; WaitingCapacity = Math.Max(0,waitingCapacity); ProfessionNeutral = professionNeutral; Professions = Array.AsReadOnly(professions.Distinct().ToArray());
        }
    }
    /// <summary>Only operational facilities register. Selection is fair across facilities and their profession pools.</summary>
    public sealed class FacilityApplicantPool
    {
        private readonly List<RecruitmentFacility> _facilities = new List<RecruitmentFacility>();
        private int _next;
        public event Action<RecruitmentFacility> Registered;
        public IReadOnlyList<RecruitmentFacility> Facilities => _facilities.AsReadOnly();
        public void Register(RecruitmentFacility facility)
        {
            if (_facilities.Any(f => f.Id == facility.Id)) return;
            _facilities.Add(facility);Registered?.Invoke(facility);
        }
        public bool Contains(string id) => _facilities.Any(f => f.Id == id);
        public void Remove(string id)
        {
            foreach (var facility in _facilities.Where(f => f.Id == id))
                foreach (var personId in facility.WaitingReservations.Keys.ToArray())
                    facility.Release(personId);
            _facilities.RemoveAll(f => f.Id == id);
        }
        public RecruitmentFacility Select(Func<string, bool> hasCapacity, out ProfessionalRole role)
        {
            role = default;
            for (int i = 0; i < _facilities.Count; i++)
            {
                var f = _facilities[_next++ % _facilities.Count];
                if (f.ProfessionNeutral || !f.Available || f.Professions.Count == 0 || !hasCapacity(f.Id)) continue;
                role = f.Professions[f.NextProfession++ % f.Professions.Count]; return f;
            }
            return null;
        }

        public RecruitmentFacility SelectForProfession(ProfessionalRole profession, Func<string, bool> hasCapacity)
        {
            for (int i = 0; i < _facilities.Count; i++)
            {
                var facility = _facilities[_next++ % _facilities.Count];
                if (!facility.ProfessionNeutral && facility.Available && facility.Professions.Contains(profession) && hasCapacity(facility.Id)) return facility;
            }
            return null;
        }
    }
    [Serializable] public sealed class RecruitmentStateSnapshot
    {
        public int TalentMinutesUntilArrival, NextTalentFacility;
        public ApplicantSnapshot[] Applicants;
        public RecruitmentIntakeSnapshot[] CategoryIntakes=Array.Empty<RecruitmentIntakeSnapshot>();
    }
    [Serializable]
    public sealed class ApplicantSnapshot
    {
        public string PersonId, FacilityId;
        public ProfessionalRole Profession;
        public CandidateStatus Status;
        public int WaitingMinutes, WaitingPositionIndex, TravelMinutes;
        public RecruitmentDestination Destination;
        public bool ProfessionNeutral;
        public bool HasIntakeCategory, IsStarterApplicant;
        public RecruitmentCategory IntakeCategory;
    }
}
