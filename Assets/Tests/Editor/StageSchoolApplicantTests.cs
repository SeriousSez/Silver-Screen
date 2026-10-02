using System;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Interaction;
using UnityEngine;
namespace SilverScreen.Tests.EditMode
{
 public sealed class StageSchoolApplicantTests
 {
  sealed class Router:ICandidateWorldRouter
  {
   public bool CompleteArrival=true,CompleteExit=true;
   public CandidateRouteResult ArrivalResult=CandidateRouteResult.Started;
   public Action<CandidateRouteResult,int> Arrival,Exit;
   public CandidateRouteResult RouteToRecruitmentLocation(Candidate c,Action<CandidateRouteResult,int> done){Arrival=done;if(CompleteArrival&&ArrivalResult==CandidateRouteResult.Started)done(CandidateRouteResult.Started,Math.Max(0,c.WaitingPositionIndex));return ArrivalResult;}
   public CandidateRouteResult RouteToExit(Candidate c,Action<CandidateRouteResult,int> done){Exit=done;if(CompleteExit)done(CandidateRouteResult.Started,-1);return CandidateRouteResult.Started;}
   public void ReleaseCandidate(Candidate c){}
  }
  sealed class Setup:IDisposable
  {
   public readonly SimulationClock Clock=new SimulationClock();public readonly Router Router=new Router();
   public readonly FacilityApplicantPool Facilities=new FacilityApplicantPool();public readonly RecruitmentCoordinator Service;
   public bool Operational=true,Reachable=true;
   public Setup(int capacity=6,int firstDelay=1){Service=new RecruitmentCoordinator(Clock,new CandidateGenerator(new SeededRandomSource(1930)),Router);Service.Facilities=Facilities;School("school-1",capacity,firstDelay);}
   public RecruitmentFacility School(string id,int capacity=6,int firstDelay=1){var definition=RecruitmentDefinitions.Talent();definition.StarterInitialDelayMinutes=firstDelay;var f=new RecruitmentFacility(id,RecruitmentDestination.StageSchool,new[]{ProfessionalRole.Actor,ProfessionalRole.Director,ProfessionalRole.Extra},capacity,true,definition){IsAvailable=()=>Operational,IsReachable=()=>Reachable};Facilities.Register(f);return f;}
   public Candidate Arrive()=>Service.TryGenerateTalentArrival();public void Minutes(int n)=>Clock.Advance(n);
   public RecruitmentFacility OtherFacility(string id,ProfessionalRole profession)
   {
    var facility=new RecruitmentFacility(id,RecruitmentDestination.ServiceFacility,new[]{profession});
    Facilities.Register(facility);return facility;
   }
   public void Dispose()=>Service.Dispose();
  }
  [Test] public void OperationalActiveReachableSchoolRequiredWithoutHiddenBacklog()
  {
   using var x=new Setup();x.Operational=false;x.Minutes(600);Assert.That(x.Service.Candidates,Is.Empty);Assert.That(x.Service.Population.People,Is.Empty);
   x.Operational=true;x.Reachable=false;x.Minutes(1);Assert.That(x.Service.Candidates,Is.Empty);
   x.Reachable=true;x.Minutes(30);Assert.That(x.Service.Candidates.Count,Is.EqualTo(1));
  }
  [Test] public void NoSchoolMeansNoTalentQueue(){using var x=new Setup();x.Facilities.Remove("school-1");x.Minutes(1440);Assert.That(x.Service.Candidates,Is.Empty);}
  [Test] public void CapacityUsesDistinctAuthoredSlotsAndGlobalCap()
  {
   using var x=new Setup(2);var a=x.Arrive();var b=x.Arrive();Assert.That(x.Arrive(),Is.Null);Assert.That(a.WaitingPositionIndex,Is.Not.EqualTo(b.WaitingPositionIndex));
   x.School("school-2");for(int i=0;i<4;i++)Assert.That(x.Arrive(),Is.Not.Null);Assert.That(x.Arrive(),Is.Null);Assert.That(x.Service.Candidates.Count,Is.EqualTo(6));
  }
  [Test] public void WaitingIntentReservedBeforePhysicalArrivalAndSnapshotContainsIt()
  {
   using var x=new Setup();x.Router.CompleteArrival=false;var c=x.Arrive();var s=c.Capture();Assert.That(s.Status,Is.EqualTo(CandidateStatus.Arriving));Assert.That(s.WaitingPositionIndex,Is.Zero);Assert.That(s.FacilityId,Is.EqualTo("school-1"));Assert.That(s.ProfessionNeutral,Is.True);
  }
  [Test] public void UnassignedApplicantsHaveLowIndependentSkillsAndNoDefaultHire()
  {
   using var x=new Setup();var c=x.Arrive();Assert.That(c.JobSought,Is.EqualTo(ProfessionalRole.Unassigned));Assert.That(c.Person.ProfessionalRole,Is.EqualTo(ProfessionalRole.Unassigned));Assert.That(x.Service.Hire(c),Is.Null);
   var g=new CandidateGenerator(new SeededRandomSource(8));var people=Enumerable.Range(0,150).Select(_=>g.GenerateTalentApplicant(x.Clock.CurrentTime).Person).ToArray();
   Assert.That(people.Average(p=>p.Talent.ActingAbility),Is.LessThan(15));Assert.That(people.All(p=>p.Talent.DirectingAbility<=40&&p.Talent.GenreExperience.All(e=>e.Experience<=12)),Is.True);
   Assert.That(people.Select(p=>p.Id).Distinct().Count(),Is.EqualTo(150));
  }
  [TestCase(ProfessionalRole.Actor,EmployeeRole.Actor)] [TestCase(ProfessionalRole.Director,EmployeeRole.Director)] [TestCase(ProfessionalRole.Extra,EmployeeRole.Extra)]
  public void ImmediateHiringPreservesPersonDataAndReleasesApplicantState(ProfessionalRole role,EmployeeRole employeeRole)
  {
   using var x=new Setup();var c=x.Arrive();var p=c.Person;var talent=p.Talent;int a=talent.ActingAbility,d=talent.DirectingAbility,w=talent.WritingAbility;var genres=talent.GenreExperience.ToArray();
   var e=x.Service.Hire(c,role,"school-1");Assert.That(e.Person,Is.SameAs(p));Assert.That(e.Id,Is.EqualTo(p.Id));Assert.That(e.Role,Is.EqualTo(employeeRole));Assert.That(e.Person.Talent,Is.SameAs(talent));
   Assert.That(new[]{talent.ActingAbility,talent.DirectingAbility,talent.WritingAbility},Is.EqualTo(new[]{a,d,w}));CollectionAssert.AreEqual(genres,talent.GenreExperience);
   Assert.That(c.Status,Is.EqualTo(CandidateStatus.Hired));Assert.That(c.WaitingPositionIndex,Is.EqualTo(-1));Assert.That(x.Service.Candidates,Is.Empty);Assert.That(x.Facilities.Facilities[0].WaitingReservations,Is.Empty);Assert.That(x.Service.Population.People[p.Id],Is.SameAs(p));Assert.That(x.Service.Hire(c,role),Is.Null);
  }
  [Test] public void RejectionReleasesSlotBeforeExitAndRetainsIdentityAfterExit()
  {
   using var x=new Setup(1);x.Router.CompleteExit=false;var c=x.Arrive();Assert.That(x.Service.Reject(c),Is.True);Assert.That(c.Status,Is.EqualTo(CandidateStatus.Departing));Assert.That(x.Facilities.Facilities[0].WaitingReservations,Is.Empty);var next=x.Arrive();Assert.That(next,Is.Not.Null);
   x.Router.Exit(CandidateRouteResult.Started,-1);Assert.That(c.Status,Is.EqualTo(CandidateStatus.Gone));Assert.That(x.Service.Population.People[c.Person.Id],Is.SameAs(c.Person));
  }
  [Test] public void ExpiryPausesWhileCarriedAndUsesSimulationMinutes()
  {
   using var x=new Setup();x.Service.ConfigureTalentApplicants(new TalentApplicantSettings{MaximumWaitingMinutes=4});var c=x.Arrive();c.SetCarried(true);x.Minutes(5);Assert.That(c.WaitingMinutes,Is.Zero);c.SetCarried(false);x.Minutes(4);Assert.That(c.Status,Is.EqualTo(CandidateStatus.Gone));
  }
  [TestCase(SimulationSpeed.Normal,6)] [TestCase(SimulationSpeed.Fast,3)] [TestCase(SimulationSpeed.VeryFast,2)]
  public void ArrivalUsesAuthoritativeSpeedAndPause(SimulationSpeed speed,int seconds)
  {
   using var x=new Setup(firstDelay:6);x.Clock.SetSpeed(SimulationSpeed.Paused);x.Clock.Advance(500);Assert.That(x.Service.Candidates,Is.Empty);x.Clock.SetSpeed(speed);x.Clock.Advance(seconds-1);Assert.That(x.Service.Candidates,Is.Empty);x.Clock.Advance(1);Assert.That(x.Service.Candidates.Count,Is.EqualTo(1));
  }
  [Test] public void UnavailableSchoolReassignsOrReleasesWithoutLosingPeople()
  {
   using var x=new Setup(firstDelay:60);var first=x.Facilities.Facilities[0];var c=x.Arrive();var other=x.School("school-2");first.IsAvailable=()=>false;x.Minutes(1);Assert.That(c.FacilityId,Is.EqualTo("school-2"));Assert.That(first.WaitingReservations,Is.Empty);Assert.That(other.WaitingReservations.Count,Is.EqualTo(1));other.IsAvailable=()=>false;x.Minutes(1);Assert.That(c.Status,Is.EqualTo(CandidateStatus.Gone));Assert.That(other.WaitingReservations,Is.Empty);
  }
  [Test] public void MultipleSchoolsHaveIndependentOwnershipAndDeterministicAssignment()
  {
   using var x=new Setup(2);var second=x.School("school-2",2);var a=x.Arrive();var b=x.Arrive();Assert.That(a.FacilityId,Is.EqualTo("school-1"));Assert.That(b.FacilityId,Is.EqualTo("school-2"));Assert.That(a.WaitingPositionIndex,Is.Zero);Assert.That(b.WaitingPositionIndex,Is.Zero);
   Assert.That(x.Service.Hire(a,ProfessionalRole.Director,"school-2"),Is.Not.Null);Assert.That(second.WaitingReservations.Count,Is.EqualTo(1));
  }
  [Test] public void FailedArrivalReleasesReservationAndPreservesOffsiteIdentity()
  {
   using var x=new Setup();x.Router.ArrivalResult=CandidateRouteResult.NavigationRejected;var c=x.Arrive();Assert.That(c.Status,Is.EqualTo(CandidateStatus.Gone));Assert.That(x.Facilities.Facilities[0].WaitingReservations,Is.Empty);Assert.That(x.Service.Population.People.ContainsKey(c.Person.Id),Is.True);
  }
  [Test] public void ContextualActionsExcludeEmployeesAndFutureActionIsClearlyDisabled()
  {
   using var x=new Setup();var c=x.Arrive();var go=new GameObject();try{
    var action=go.AddComponent<ApplicantHiringAction>();action.Configure("actor","school-1",ProfessionalRole.Actor);
    var context=new PersonDropContext(c,null,x.Service,null);Assert.That(action.IsRelevant(context),Is.True);Assert.That(action.CanExecute(context),Is.True);
    action.Configure("future","school-1",ProfessionalRole.Unassigned,true);Assert.That(action.IsRelevant(context),Is.True);Assert.That(action.CanExecute(context),Is.False);StringAssert.Contains("NOT YET AVAILABLE",action.FloorLabel(context));
    Assert.That(action.IsRelevant(new PersonDropContext(null,new Employee(c.Person,EmployeeRole.Actor,600),x.Service,null)),Is.False);
   }finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [TestCase(ProfessionalRole.Extra,ProfessionalRole.Actor,EmployeeRole.Actor)]
  [TestCase(ProfessionalRole.Extra,ProfessionalRole.Director,EmployeeRole.Director)]
  [TestCase(ProfessionalRole.Extra,ProfessionalRole.Extra,EmployeeRole.Extra)]
  [TestCase(ProfessionalRole.Actor,ProfessionalRole.Director,EmployeeRole.Director)]
  [TestCase(ProfessionalRole.Director,ProfessionalRole.Extra,EmployeeRole.Extra)]
  [TestCase(ProfessionalRole.ConstructionWorker,ProfessionalRole.Actor,EmployeeRole.Actor)]
  public void GeneratedApplicantRoleDoesNotRestrictStageSchoolTargets(ProfessionalRole intended,ProfessionalRole chosen,EmployeeRole expected)
  {
   using var x=new Setup();x.OtherFacility("origin",intended);var c=x.Service.TryGenerateArrival();
   Assert.That(c.JobSought,Is.EqualTo(intended));
   c.Person.Talent.SetPrimaryAbility(ProfessionalRole.Actor,3);
   c.Person.Talent.SetPrimaryAbility(ProfessionalRole.Director,2);
   c.Person.Wellbeing.ApplyDelta(PersonWellbeingDimension.Stress,27);
   c.Person.Career.SetCareerDrive(83);
   var targets=new[]{ProfessionalRole.Actor,ProfessionalRole.Director,ProfessionalRole.Extra};
   var objects=targets.Select(_=>new GameObject()).ToArray();
   try{
    var context=new PersonDropContext(c,null,x.Service,null);
    var actions=targets.Select((role,index)=>{var action=objects[index].AddComponent<ApplicantHiringAction>();action.Configure(role.ToString(),"school-1",role);return action;}).ToArray();
    Assert.That(actions.All(action=>action.IsRelevant(context)&&action.CanExecute(context)),Is.True,"Every exposed Stage School job remains available for this applicant.");
    var person=c.Person;var personId=person.Id;var talent=person.Talent;var wellbeing=person.Wellbeing;var career=person.Career;
    var acting=talent.ActingAbility;var directing=talent.DirectingAbility;var writing=talent.WritingAbility;var genres=talent.GenreExperience.ToArray();var traits=person.TraitIds.ToArray();Employee hired=null;
    x.Service.OnCandidateHired+=(_,employee)=>hired=employee;
    var selected=actions.Single(action=>action.Profession==chosen);
    Assert.That(selected.TryExecute(context),Is.True);
    Assert.That(hired,Is.Not.Null);
    Assert.That(hired.Person,Is.SameAs(person));
    Assert.That(hired.Id,Is.EqualTo(personId));
    Assert.That(hired.Role,Is.EqualTo(expected));
    Assert.That(person.ProfessionalRole,Is.EqualTo(chosen));
    Assert.That(c.JobSought,Is.EqualTo(intended),"The applicant's original role remains descriptive data.");
    Assert.That(c.Capture().Profession,Is.EqualTo(intended));
    Assert.That(hired.Person.Talent,Is.SameAs(talent));
    Assert.That(hired.Person.Wellbeing,Is.SameAs(wellbeing));
    Assert.That(hired.Person.Career,Is.SameAs(career));
    Assert.That(person.Talent.ActingAbility,Is.EqualTo(acting));
    Assert.That(person.Talent.DirectingAbility,Is.EqualTo(directing));
    Assert.That(person.Talent.WritingAbility,Is.EqualTo(writing));
    Assert.That(hired.Skill,Is.EqualTo(chosen==ProfessionalRole.Director?directing:acting));
    CollectionAssert.AreEqual(genres,person.Talent.GenreExperience);
    Assert.That(wellbeing.Stress,Is.EqualTo(PersonWellbeing.DefaultStress+27));
    Assert.That(career.CareerDrive,Is.EqualTo(83));
    CollectionAssert.AreEqual(traits,person.TraitIds);
   }finally{foreach(var go in objects)UnityEngine.Object.DestroyImmediate(go);}
  }
  [Test] public void TalentApplicantCanBeHiredAtAnotherFacilitiesExposedTarget()
  {
   using var x=new Setup();var candidate=x.Arrive();x.OtherFacility("services",ProfessionalRole.Groundskeeper);
   var go=new GameObject();try{
    var action=go.AddComponent<WorkforceRoomAction>();action.Configure("groundskeeper","services",ProfessionalRole.Groundskeeper,false,null);
    var context=new PersonDropContext(candidate,null,x.Service,null);var person=candidate.Person;var acting=person.Talent.ActingAbility;Employee hired=null;
    x.Service.OnCandidateHired+=(_,employee)=>hired=employee;
    Assert.That(action.CanExecute(context),Is.True);
    Assert.That(action.TryExecute(context),Is.True);
    Assert.That(hired.Person,Is.SameAs(person));
    Assert.That(hired.Role,Is.EqualTo(EmployeeRole.Groundskeeper));
    Assert.That(person.ProfessionalRole,Is.EqualTo(ProfessionalRole.Groundskeeper));
    Assert.That(person.Talent.ActingAbility,Is.EqualTo(acting));
   }finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [TestCase(CandidateStatus.Arriving)]
  [TestCase(CandidateStatus.Hired)]
  [TestCase(CandidateStatus.Departing)]
  [TestCase(CandidateStatus.Gone)]
  public void ContextualHiringRejectsNonWaitingApplicants(CandidateStatus status)
  {
   using var x=new Setup();x.Router.CompleteArrival=status!=CandidateStatus.Arriving;var c=x.Arrive();
   if(status==CandidateStatus.Hired)Assert.That(x.Service.Hire(c,ProfessionalRole.Actor,"school-1"),Is.Not.Null);
   if(status==CandidateStatus.Departing)Assert.That(c.BeginDeparture(),Is.True);
   if(status==CandidateStatus.Gone)Assert.That(x.Service.Reject(c),Is.True);
   Assert.That(c.Status,Is.EqualTo(status));
   var go=new GameObject();try{
    var action=go.AddComponent<ApplicantHiringAction>();action.Configure("actor","school-1",ProfessionalRole.Actor);
    var context=new PersonDropContext(c,null,x.Service,null);
    Assert.That(action.IsRelevant(context),Is.False);
    Assert.That(action.CanExecute(context),Is.False);
    Assert.That(action.TryExecute(context),Is.False);
    Assert.That(x.Service.Hire(c,ProfessionalRole.Actor,"school-1"),Is.Null);
    Assert.That(c.Status,Is.EqualTo(status));
   }finally{UnityEngine.Object.DestroyImmediate(go);}
  }
  [TestCase("unavailable-facility")]
  [TestCase("missing-facility")]
  [TestCase("unavailable-origin")]
  [TestCase("disabled-action")]
  [TestCase("unexposed-job")]
  [TestCase("unassigned-job")]
  [TestCase("undefined-job")]
  [TestCase("recruitment-unavailable")]
  public void ContextualHiringRejectsInvalidTargetsWithoutConsumingApplicant(string restriction)
  {
   using var x=new Setup();var c=x.Arrive();var facility=x.School("target");var go=new GameObject();try{
    var action=go.AddComponent<ApplicantHiringAction>();action.Configure("actor","target",ProfessionalRole.Actor);
    switch(restriction){
     case "unavailable-facility":facility.IsAvailable=()=>false;break;
     case "missing-facility":x.Facilities.Remove("target");break;
     case "unavailable-origin":x.Facilities.Facilities[0].IsAvailable=()=>false;break;
     case "disabled-action":action.enabled=false;break;
     case "unexposed-job":action.Configure("writer","target",ProfessionalRole.Writer);break;
     case "unassigned-job":action.Configure("unassigned","target",ProfessionalRole.Unassigned);break;
     case "undefined-job":action.Configure("invalid","target",(ProfessionalRole)int.MaxValue);break;
     case "recruitment-unavailable":x.Service.RecruitmentAvailable=()=>false;break;
     default:Assert.Fail("Unknown restriction.");break;
    }
    var context=new PersonDropContext(c,null,x.Service,null);
    Assert.That(action.IsRelevant(context),Is.False);
    Assert.That(action.CanExecute(context),Is.False);
    Assert.That(action.TryExecute(context),Is.False);
    Assert.That(c.Status,Is.EqualTo(CandidateStatus.WaitingForRecruitment));
    CollectionAssert.Contains(x.Service.Candidates,c);
   }finally{UnityEngine.Object.DestroyImmediate(go);}
  }
 }
}
