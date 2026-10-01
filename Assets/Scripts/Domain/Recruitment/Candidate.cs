using System;
namespace SilverScreen.Domain.Recruitment
{
 public enum CandidateStatus{Arriving,WaitingForRecruitment,Hired,Departing,Gone}
 public enum RecruitmentDestination{StageSchool,ScriptOffice,StudioServices,CrewFacility}
 public enum RecruitmentCategory{StudioServices,Talent,Crew,Writing}
 [Serializable] public sealed class Candidate
 {
  public PersonProfile Person{get;} public int SalaryExpectation{get;} public RecruitmentCategory Category{get;} public RecruitmentDestination Destination{get;} public bool IsStarterApplicant{get;} public CandidateStatus Status{get;private set;} public int WaitingMinutes{get;private set;} public int WaitingPositionIndex{get;private set;}=-1;
  public Candidate(PersonProfile person,int salaryExpectation):this(person,salaryExpectation,CategoryFor(person),false){}
  public Candidate(PersonProfile person,int salaryExpectation,RecruitmentCategory category,bool isStarterApplicant=false){Person=person??throw new ArgumentNullException(nameof(person));SalaryExpectation=Math.Max(1,salaryExpectation);Category=category;Destination=DestinationFor(category);IsStarterApplicant=isStarterApplicant;Status=CandidateStatus.Arriving;}
  private static RecruitmentCategory CategoryFor(PersonProfile person){if(person==null)throw new ArgumentNullException(nameof(person));return person.ProfessionalRole==ProfessionalRole.Writer?RecruitmentCategory.Writing:person.ProfessionalRole==ProfessionalRole.Crew?RecruitmentCategory.Crew:RecruitmentCategory.Talent;}
  private static RecruitmentDestination DestinationFor(RecruitmentCategory category)=>category switch{RecruitmentCategory.StudioServices=>RecruitmentDestination.StudioServices,RecruitmentCategory.Crew=>RecruitmentDestination.CrewFacility,RecruitmentCategory.Writing=>RecruitmentDestination.ScriptOffice,_=>RecruitmentDestination.StageSchool};
  public bool MarkWaiting(int index){if(Status!=CandidateStatus.Arriving||index<0)return false;WaitingPositionIndex=index;Status=CandidateStatus.WaitingForRecruitment;return true;}
  public bool MarkHired(){if(Status!=CandidateStatus.WaitingForRecruitment)return false;Status=CandidateStatus.Hired;return true;}
  public bool BeginDeparture(){if(Status!=CandidateStatus.WaitingForRecruitment)return false;Status=CandidateStatus.Departing;return true;}
  public bool MarkGone(){if(Status!=CandidateStatus.Arriving&&Status!=CandidateStatus.Departing)return false;Status=CandidateStatus.Gone;return true;}
  public bool AdvanceWaitingMinute(){if(Status!=CandidateStatus.WaitingForRecruitment)return false;WaitingMinutes++;return true;}
 }
}
