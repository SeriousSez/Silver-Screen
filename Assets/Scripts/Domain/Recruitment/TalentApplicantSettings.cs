using System;
namespace SilverScreen.Domain.Recruitment
{
    [Serializable] public sealed class TalentApplicantSettings
    {
        // Kept for serialized compatibility; category RecruitmentDefinition now owns arrival timing.
        public int FirstArrivalMinutes = 60;
        public int ArrivalIntervalMinutes = 180;
        public int RetryMinutes = 30;
        public int MaximumActiveApplicants = 6;
        public int MaximumWaitingMinutes = 1440;
        public int MaximumTravelMinutes = 720;
        public void Validate()
        {
            if(FirstArrivalMinutes<1||ArrivalIntervalMinutes<1||RetryMinutes<1||MaximumActiveApplicants<1||MaximumWaitingMinutes<1||MaximumTravelMinutes<1)
                throw new ArgumentOutOfRangeException("Applicant tuning values must be positive.");
        }
    }
}
