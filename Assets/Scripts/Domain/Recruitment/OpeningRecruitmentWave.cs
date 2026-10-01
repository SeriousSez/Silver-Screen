using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Recruitment
{
    /// <summary>
    /// One-session bootstrap input for the normal recruitment pipeline. It chooses
    /// which jobs attract the first applicants; it never limits how they may be hired.
    /// </summary>
    public sealed class OpeningRecruitmentWave
    {
        private readonly ProfessionalRole[] _roles;

        public IReadOnlyList<ProfessionalRole> Roles => Array.AsReadOnly(_roles);
        public int ArrivalCadenceMinutes { get; }

        public OpeningRecruitmentWave(IEnumerable<ProfessionalRole> roles, int arrivalCadenceMinutes)
        {
            if (roles == null) throw new ArgumentNullException(nameof(roles));
            _roles = new List<ProfessionalRole>(roles).ToArray();
            if (_roles.Length == 0) throw new ArgumentException("An opening wave needs at least one applicant.", nameof(roles));
            foreach (var role in _roles)
                if (!Enum.IsDefined(typeof(ProfessionalRole), role)) throw new ArgumentOutOfRangeException(nameof(roles));
            ArrivalCadenceMinutes = Math.Max(1, arrivalCadenceMinutes);
        }

        public static OpeningRecruitmentWave CreateNewStudio1930() => new OpeningRecruitmentWave(new[]
        {
            ProfessionalRole.ConstructionWorker,
            ProfessionalRole.Groundskeeper,
            ProfessionalRole.ConstructionWorker,
            ProfessionalRole.ConstructionWorker,
            ProfessionalRole.Groundskeeper,
            ProfessionalRole.ConstructionWorker
        // Eight seconds between departures at current 1x pacing gives walking
        // applicants room to separate; the first still arrives after one minute.
        }, 8);
    }
}
