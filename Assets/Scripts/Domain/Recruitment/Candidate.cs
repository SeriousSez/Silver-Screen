using System;
namespace SilverScreen.Domain.Recruitment
{
 public enum CandidateStatus{Arriving,WaitingForRecruitment,Hired,Departing,Gone}
 public enum RecruitmentDestination{StageSchool,ScriptOffice,ServiceFacility,CastingOffice}
 [Serializable] public sealed class Candidate
 {
  public ProfessionalRole JobSought { get; }
  public bool IsTalentApplicant => JobSought == ProfessionalRole.Unassigned;
  public RecruitmentCategory? IntakeCategory { get; private set; }
  public bool IsStarterApplicant { get; private set; }
  internal void AssignIntake(RecruitmentCategory category, bool starter) { IntakeCategory=category;IsStarterApplicant=starter; }
  public bool IsCarried { get; private set; }
  public int TravelMinutes { get; private set; }
  public void SetCarried(bool carried) => IsCarried = carried;
  internal void AdvanceTravelMinute() => TravelMinutes++;
  internal void ReserveWaiting(int index) { WaitingPositionIndex=index;TravelMinutes=0; }
  internal void ReleaseWaiting() => WaitingPositionIndex=-1;
  internal void Reassign(RecruitmentFacility facility,int index) { FacilityId=facility.Id;Destination=facility.Destination;WaitingPositionIndex=index;TravelMinutes=0;Status=CandidateStatus.Arriving; }
  public PersonProfile Person{get;} public int SalaryExpectation{get;} public RecruitmentDestination Destination{get;private set;} public string FacilityId{get;private set;} public CandidateStatus Status{get;private set;} public int WaitingMinutes{get;private set;} public int WaitingPositionIndex{get;private set;}=-1;
  public Candidate(PersonProfile person,int salaryExpectation){Person=person??throw new ArgumentNullException(nameof(person));JobSought=person.ProfessionalRole;SalaryExpectation=Math.Max(1,salaryExpectation);Destination=person.ProfessionalRole==ProfessionalRole.Writer?RecruitmentDestination.ScriptOffice:RecruitmentDestination.StageSchool;Status=CandidateStatus.Arriving;}
  internal void AssignFacility(RecruitmentFacility facility){if(Status!=CandidateStatus.Arriving||FacilityId!=null)throw new InvalidOperationException("Facility context is fixed before routing.");FacilityId=facility.Id;Destination=facility.Destination;}
  public ApplicantSnapshot Capture()=>new ApplicantSnapshot{PersonId=Person.Id,FacilityId=FacilityId,Profession=JobSought,Status=Status,WaitingMinutes=WaitingMinutes,WaitingPositionIndex=WaitingPositionIndex,TravelMinutes=TravelMinutes,Destination=Destination,ProfessionNeutral=IsTalentApplicant,HasIntakeCategory=IntakeCategory.HasValue,IntakeCategory=IntakeCategory.GetValueOrDefault(),IsStarterApplicant=IsStarterApplicant,Wellbeing=Person.CaptureWellbeing()};
  public bool MarkWaiting(int index){if(Status!=CandidateStatus.Arriving||index<0)return false;WaitingPositionIndex=index;Status=CandidateStatus.WaitingForRecruitment;return true;}
  public bool MarkHired(){if(Status!=CandidateStatus.WaitingForRecruitment)return false;Status=CandidateStatus.Hired;WaitingPositionIndex=-1;return true;}
  public bool BeginDeparture(){if(Status!=CandidateStatus.WaitingForRecruitment&&Status!=CandidateStatus.Arriving)return false;Status=CandidateStatus.Departing;TravelMinutes=0;WaitingPositionIndex=-1;return true;}
  public bool MarkGone(){if(Status!=CandidateStatus.Arriving&&Status!=CandidateStatus.Departing)return false;Status=CandidateStatus.Gone;WaitingPositionIndex=-1;return true;}
  public bool AdvanceWaitingMinute(){if(Status!=CandidateStatus.WaitingForRecruitment)return false;WaitingMinutes++;return true;}
 }
}
