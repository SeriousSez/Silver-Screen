using System;
namespace SilverScreen.Domain.Recruitment
{
 public enum CandidateStatus{Arriving,WaitingAtStageSchool,Hired,Departing,Gone}
 [Serializable] public sealed class Candidate
 {
  public PersonProfile Person{get;} public int SalaryExpectation{get;} public CandidateStatus Status{get;private set;} public int WaitingMinutes{get;private set;} public int WaitingPositionIndex{get;private set;}=-1;
  public Candidate(PersonProfile person,int salaryExpectation){Person=person??throw new ArgumentNullException(nameof(person));SalaryExpectation=Math.Max(1,salaryExpectation);Status=CandidateStatus.Arriving;}
  public bool MarkWaiting(int index){if(Status!=CandidateStatus.Arriving||index<0)return false;WaitingPositionIndex=index;Status=CandidateStatus.WaitingAtStageSchool;return true;}
  public bool MarkHired(){if(Status!=CandidateStatus.WaitingAtStageSchool)return false;Status=CandidateStatus.Hired;return true;}
  public bool BeginDeparture(){if(Status!=CandidateStatus.WaitingAtStageSchool)return false;Status=CandidateStatus.Departing;return true;}
  public bool MarkGone(){if(Status!=CandidateStatus.Arriving&&Status!=CandidateStatus.Departing)return false;Status=CandidateStatus.Gone;return true;}
  public bool AdvanceWaitingMinute(){if(Status!=CandidateStatus.WaitingAtStageSchool)return false;WaitingMinutes++;return true;}
 }
}