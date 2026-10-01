using System;
namespace SilverScreen.Domain.Recruitment
{
 public enum RecruitmentCategory { StudioServices, Talent, Crew, Writers }
 /// <summary>Category intake policy; waiting capacity limits applicants, never employees.</summary>
 [Serializable] public sealed class RecruitmentDefinition
 {
  public RecruitmentCategory Category;
  public int StarterApplicantCount;
  public int StarterInitialDelayMinutes=1;
  public int StarterSpacingMinutes=8;
  public int NormalCadenceMinutes=180;
  public int RetryMinutes=30;
  public int WaitingCapacity=6;
  public string WaitingAnchorPrefix;
  public ProfessionalRole[] StarterProfessionSequence=Array.Empty<ProfessionalRole>();
  public void Validate()
  {
   if(!Enum.IsDefined(typeof(RecruitmentCategory),Category)||StarterApplicantCount<0||StarterInitialDelayMinutes<1||StarterSpacingMinutes<1||NormalCadenceMinutes<1||RetryMinutes<1||WaitingCapacity<1)
    throw new ArgumentOutOfRangeException("Recruitment intake configuration");
  }
 }
 public static class RecruitmentDefinitions
 {
  public static RecruitmentDefinition StudioServices()=>new RecruitmentDefinition{Category=RecruitmentCategory.StudioServices,StarterApplicantCount=6,NormalCadenceMinutes=360,WaitingAnchorPrefix="ApplicantWaiting_",StarterProfessionSequence=new[]{ProfessionalRole.ConstructionWorker,ProfessionalRole.Groundskeeper,ProfessionalRole.ConstructionWorker,ProfessionalRole.ConstructionWorker,ProfessionalRole.Groundskeeper,ProfessionalRole.ConstructionWorker}};
  public static RecruitmentDefinition Talent()=>new RecruitmentDefinition{Category=RecruitmentCategory.Talent,StarterApplicantCount=4,WaitingAnchorPrefix="applicant.exterior."};
  // Authoring defaults only. No Crew/Writing gameplay or facility is registered by these definitions.
  public static RecruitmentDefinition Crew()=>new RecruitmentDefinition{Category=RecruitmentCategory.Crew,StarterApplicantCount=4,WaitingAnchorPrefix="applicant.exterior."};
  public static RecruitmentDefinition Writers()=>new RecruitmentDefinition{Category=RecruitmentCategory.Writers,StarterApplicantCount=3,WaitingAnchorPrefix="applicant.exterior."};
 }
 [Serializable] public sealed class RecruitmentIntakeSnapshot
 {
  public RecruitmentCategory Category;
  public bool Granted;
  public int StarterTarget, RemainingToDispatch, MinutesUntilArrival, NextFacility;
  public string[] InFlightPersonIds=Array.Empty<string>();
 }
}
