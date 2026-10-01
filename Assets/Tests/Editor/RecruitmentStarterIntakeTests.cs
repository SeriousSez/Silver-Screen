using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;
namespace SilverScreen.Tests.EditMode
{
 public sealed class RecruitmentStarterIntakeTests
 {
  sealed class Router:ICandidateWorldRouter
  {
   public readonly Dictionary<string,Action<CandidateRouteResult,int>> Routes=new Dictionary<string,Action<CandidateRouteResult,int>>();
   public bool Complete=true;public int Arrivals;
   public CandidateRouteResult RouteToRecruitmentLocation(Candidate c,Action<CandidateRouteResult,int> done){Arrivals++;Routes[c.Person.Id]=done;if(Complete)done(CandidateRouteResult.Started,c.WaitingPositionIndex);return CandidateRouteResult.Started;}
   public CandidateRouteResult RouteToExit(Candidate c,Action<CandidateRouteResult,int> done){done(CandidateRouteResult.Started,-1);return CandidateRouteResult.Started;}
   public void ReleaseCandidate(Candidate c){}
  }
  sealed class Setup:IDisposable
  {
   public readonly SimulationClock Clock=new SimulationClock();public readonly Router World=new Router();public readonly FacilityApplicantPool Pool=new FacilityApplicantPool();public readonly RecruitmentCoordinator Service;
   public Setup(){Service=new RecruitmentCoordinator(Clock,new CandidateGenerator(new SeededRandomSource(1930)),World);Service.Facilities=Pool;}
   public RecruitmentFacility School(string id="school",bool available=true,int capacity=6,RecruitmentDefinition definition=null){var f=new RecruitmentFacility(id,RecruitmentDestination.StageSchool,new[]{ProfessionalRole.Actor,ProfessionalRole.Director,ProfessionalRole.Extra},capacity,true,definition){IsAvailable=()=>available};Pool.Register(f);return f;}
   public void Minutes(int n)=>Clock.Advance(n);
   public void Dispose()=>Service.Dispose();
  }
  [Test]public void DefaultsAreGenericAndTalentStarterTargetIsFour()
  {
   Assert.That(RecruitmentDefinitions.StudioServices().StarterApplicantCount,Is.EqualTo(6));Assert.That(RecruitmentDefinitions.Talent().StarterApplicantCount,Is.EqualTo(4));Assert.That(RecruitmentDefinitions.Crew().StarterApplicantCount,Is.EqualTo(4));Assert.That(RecruitmentDefinitions.Writers().StarterApplicantCount,Is.EqualTo(3));
   var d=RecruitmentDefinitions.Talent();Assert.That(d.NormalCadenceMinutes,Is.EqualTo(180));Assert.That(d.RetryMinutes,Is.EqualTo(30));Assert.That(d.WaitingCapacity,Is.EqualTo(6));
  }
  [Test]public void FirstOperationalFacilityTriggersGrantNotConstructionOrElapsedTime()
  {
   using var x=new Setup();x.Minutes(100);Assert.That(x.Service.CaptureIntakes(),Is.Empty);
   var f=x.School(available:false);x.Minutes(100);Assert.That(x.Service.CaptureIntakes(),Is.Empty);Assert.That(x.Service.Candidates,Is.Empty);
   f.IsAvailable=()=>true;x.Minutes(1);Assert.That(x.Service.Candidates.Count,Is.EqualTo(1));Assert.That(x.Service.CaptureIntakes().Single().StarterTarget,Is.EqualTo(4));
  }
  [Test]public void StarterArrivalsAreStaggeredAndUseExistingRouting()
  {
   using var x=new Setup();x.School();var minutes=new List<long>();x.Service.OnCandidateAdded+=c=>minutes.Add((x.Clock.Now-new SimulationDateTime(1930,1,1,8,0).ToInstant()).Seconds/60);
   x.Minutes(25);CollectionAssert.AreEqual(new long[]{1,9,17,25},minutes);Assert.That(x.World.Arrivals,Is.EqualTo(4));Assert.That(x.Service.Candidates.All(c=>c.IsTalentApplicant&&c.Status==CandidateStatus.WaitingForRecruitment),Is.True);
   Assert.That(x.Service.Candidates.Select(c=>c.WaitingPositionIndex).Distinct().Count(),Is.EqualTo(4));
  }
  [Test]public void StarterUsesPhysicalWaitingCapacityAndResumesAfterHire()
  {
   using var x=new Setup();var f=x.School(capacity:2);x.Minutes(40);Assert.That(x.Service.Candidates.Count,Is.EqualTo(2));Assert.That(x.Service.StarterRemaining(RecruitmentCategory.Talent),Is.EqualTo(2));
   foreach(var c in x.Service.Candidates.ToArray())Assert.That(x.Service.Hire(c,ProfessionalRole.Actor),Is.Not.Null);
   x.Minutes(15);Assert.That(x.Service.Candidates.Count,Is.EqualTo(2));Assert.That(x.Service.StarterRemaining(RecruitmentCategory.Talent),Is.Zero);Assert.That(f.WaitingReservations.Count,Is.EqualTo(2));
  }
  [Test]public void UnreachableIntakeRetainsRemainingAndRetries()
  {
   using var x=new Setup();var f=x.School();f.IsReachable=()=>false;x.Minutes(40);Assert.That(x.Service.Population.People,Is.Empty);Assert.That(x.Service.StarterRemaining(RecruitmentCategory.Talent),Is.EqualTo(4));f.IsReachable=()=>true;x.Minutes(21);Assert.That(x.Service.Candidates.Count,Is.EqualTo(1));
  }
  [Test]public void FailedPhysicalArrivalIsNotCountedAsDelivered()
  {
   using var x=new Setup();x.World.Complete=false;x.School();x.Minutes(1);var c=x.Service.Candidates.Single();var state=x.Service.CaptureIntakes().Single();Assert.That(state.InFlightPersonIds,Is.EqualTo(new[]{c.Person.Id}));Assert.That(state.RemainingToDispatch,Is.EqualTo(3));
   x.World.Routes[c.Person.Id](CandidateRouteResult.NavigationRejected,-1);Assert.That(x.Service.StarterRemaining(RecruitmentCategory.Talent),Is.EqualTo(4));Assert.That(x.Service.Candidates,Is.Empty);
  }
  [Test]public void NormalCadenceWaitsForPhysicalStarterCompletion()
  {
   using var x=new Setup();x.World.Complete=false;x.School();x.Minutes(25);Assert.That(x.Service.Candidates.Count,Is.EqualTo(4));x.Minutes(180);Assert.That(x.World.Arrivals,Is.EqualTo(4));
   foreach(var c in x.Service.Candidates.ToArray())x.World.Routes[c.Person.Id](CandidateRouteResult.Started,c.WaitingPositionIndex);
   x.Minutes(179);Assert.That(x.World.Arrivals,Is.EqualTo(4));x.Minutes(1);Assert.That(x.World.Arrivals,Is.EqualTo(5));
  }
  [Test]public void NormalCadenceStarts180MinutesAfterStarterAndNeverCapsEmployees()
  {
   using var x=new Setup();x.School();x.Minutes(25);var roster=new WorkforceRoster();foreach(var c in x.Service.Candidates.ToArray())roster.Add(x.Service.Hire(c,ProfessionalRole.Actor));
   x.Minutes(179);Assert.That(x.Service.Candidates,Is.Empty);x.Minutes(1);Assert.That(x.Service.Candidates.Count,Is.EqualTo(1));
   for(int i=0;i<20;i++){foreach(var c in x.Service.Candidates.ToArray())roster.Add(x.Service.Hire(c,ProfessionalRole.Actor));x.Minutes(180);}
   Assert.That(roster.Employees.Count,Is.GreaterThan(20));Assert.That(x.Service.Candidates.Count,Is.EqualTo(1));Assert.That(x.Service.StarterRemaining(RecruitmentCategory.Talent),Is.Zero);
  }
  [Test]public void RebuildReenableAndSecondFacilityNeverGrantAnotherBatch()
  {
   using var x=new Setup();var f=x.School();x.Minutes(25);foreach(var c in x.Service.Candidates.ToArray())x.Service.Hire(c,ProfessionalRole.Director);
   f.IsAvailable=()=>false;x.Minutes(60);f.IsAvailable=()=>true;x.Pool.Remove(f.Id);x.Pool.Register(f);x.School("second");x.Minutes(1);Assert.That(x.Service.Candidates,Is.Empty);
   x.Pool.Remove(f.Id);x.Pool.Remove("second");x.Minutes(300);x.School("rebuilt");x.Minutes(1);Assert.That(x.Service.Candidates,Is.Empty);Assert.That(x.Service.CaptureIntakes().Single().StarterTarget,Is.EqualTo(4));Assert.That(x.Service.StarterRemaining(RecruitmentCategory.Talent),Is.Zero);
  }
  [Test]public void PartialIntakeSnapshotRestoresWithoutRegrant()
  {
   using var x=new Setup();x.School();x.Minutes(9);var snapshot=x.Service.Capture();Assert.That(snapshot.CategoryIntakes.Single().RemainingToDispatch,Is.EqualTo(2));
   var clock=new SimulationClock();using var restored=new RecruitmentCoordinator(clock,new CandidateGenerator(new SeededRandomSource(91)),new Router());restored.RestoreIntakes(snapshot.CategoryIntakes);restored.Facilities=new FacilityApplicantPool();restored.Facilities.Register(new RecruitmentFacility("rebuilt",RecruitmentDestination.StageSchool,new[]{ProfessionalRole.Actor},6,true));
   clock.Advance(16);Assert.That(restored.Candidates.Count,Is.EqualTo(2));Assert.That(restored.StarterRemaining(RecruitmentCategory.Talent),Is.Zero);
  }
  [TestCase(SimulationSpeed.Normal,1)] [TestCase(SimulationSpeed.Fast,2)] [TestCase(SimulationSpeed.VeryFast,3)]
  public void PauseAndSpeedsOwnStarterTiming(SimulationSpeed speed,int multiplier)
  {
   using var x=new Setup();var d=RecruitmentDefinitions.Talent();d.StarterInitialDelayMinutes=6;x.School(definition:d);x.Clock.SetSpeed(SimulationSpeed.Paused);x.Minutes(100);Assert.That(x.Service.Candidates,Is.Empty);x.Clock.SetSpeed(speed);x.Clock.Advance(6.0/multiplier-.5);Assert.That(x.Service.Candidates,Is.Empty);x.Clock.Advance(.5);Assert.That(x.Service.Candidates.Count,Is.EqualTo(1));
  }
 }
}
