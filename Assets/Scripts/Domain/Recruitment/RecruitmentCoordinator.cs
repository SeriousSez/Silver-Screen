using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;
namespace SilverScreen.Domain.Recruitment
{
 public enum CandidateRouteResult{Started,CandidateMissing,RecruitmentLocationMissing,CapacityFull,AgentMissing,NavigationRejected}
 public interface ICandidateWorldRouter{CandidateRouteResult RouteToRecruitmentLocation(Candidate candidate,Action<CandidateRouteResult,int> onComplete);CandidateRouteResult RouteToExit(Candidate candidate,Action<CandidateRouteResult,int> onComplete);void ReleaseCandidate(Candidate candidate);}
 [Serializable] public sealed class RecruitmentConfiguration
 {
  public int Capacity{get;} public int ArrivalCadenceMinutes{get;} public int MaximumWaitingMinutes{get;} public ProfessionalRole? FirstPrototypeCandidateRole{get;}
  public RecruitmentConfiguration(int capacity=5,int arrivalCadenceMinutes=360,int maximumWaitingMinutes=2880,ProfessionalRole? firstPrototypeCandidateRole=null){Capacity=Math.Max(1,capacity);ArrivalCadenceMinutes=Math.Max(1,arrivalCadenceMinutes);MaximumWaitingMinutes=Math.Max(60,maximumWaitingMinutes);FirstPrototypeCandidateRole=firstPrototypeCandidateRole;}
 }
 public sealed class RecruitmentCoordinator:IDisposable
 {
  private readonly List<Candidate> _candidates=new List<Candidate>();private readonly ISimulationTimeService _time;private readonly CandidateGenerator _generator;private readonly ICandidateWorldRouter _router;private int _minutesUntilArrival;private bool _generatedPrototypeCandidate;
  public RecruitmentConfiguration Configuration{get;} public IReadOnlyList<Candidate> Candidates=>_candidates;public bool HasCapacity=>_candidates.Count<Configuration.Capacity;
  public event Action<Candidate> OnCandidateAdded;public event Action<Candidate> OnCandidateChanged;public event Action<Candidate> OnCandidateRemoved;public event Action<Candidate,Employee> OnCandidateHired;public event Action<Candidate,CandidateRouteResult> OnRoutingFailed;
  public RecruitmentCoordinator(ISimulationTimeService time,CandidateGenerator generator,ICandidateWorldRouter router,RecruitmentConfiguration configuration=null){_time=time??throw new ArgumentNullException(nameof(time));_generator=generator??throw new ArgumentNullException(nameof(generator));_router=router??throw new ArgumentNullException(nameof(router));Configuration=configuration??new RecruitmentConfiguration();_minutesUntilArrival=1;_time.OnMinutePassed+=HandleMinutePassed;}
  public Candidate TryGenerateArrival(){if(!HasCapacity)return null;var role=(!_generatedPrototypeCandidate?Configuration.FirstPrototypeCandidateRole:null);var candidate=role.HasValue?_generator.GenerateForRole(_time.CurrentTime,role.Value):_generator.Generate(_time.CurrentTime);_generatedPrototypeCandidate=true;_candidates.Add(candidate);OnCandidateAdded?.Invoke(candidate);var result=_router.RouteToRecruitmentLocation(candidate,(completed,position)=>HandleArrival(candidate,completed,position));if(result!=CandidateRouteResult.Started)HandleArrival(candidate,result,-1);return candidate;}
  public Employee Hire(Candidate candidate){if(candidate==null||!_candidates.Contains(candidate)||!candidate.MarkHired())return null;var employee=new Employee(candidate.Person,ToEmployeeRole(candidate.Person.ProfessionalRole),candidate.SalaryExpectation,80);OnCandidateHired?.Invoke(candidate,employee);_router.ReleaseCandidate(candidate);_candidates.Remove(candidate);OnCandidateRemoved?.Invoke(candidate);return employee;}
  public bool Reject(Candidate candidate){if(candidate==null||!_candidates.Contains(candidate)||!candidate.BeginDeparture())return false;OnCandidateChanged?.Invoke(candidate);var result=_router.RouteToExit(candidate,(completed,unused)=>HandleDeparture(candidate,completed));if(result!=CandidateRouteResult.Started)HandleDeparture(candidate,result);return true;}
  private void HandleArrival(Candidate candidate,CandidateRouteResult result,int position){if(!_candidates.Contains(candidate))return;if(result==CandidateRouteResult.Started&&candidate.MarkWaiting(position)){OnCandidateChanged?.Invoke(candidate);return;}candidate.MarkGone();_router.ReleaseCandidate(candidate);_candidates.Remove(candidate);OnRoutingFailed?.Invoke(candidate,result);OnCandidateRemoved?.Invoke(candidate);}
  private void HandleDeparture(Candidate candidate,CandidateRouteResult result){if(!_candidates.Contains(candidate))return;candidate.MarkGone();_router.ReleaseCandidate(candidate);_candidates.Remove(candidate);if(result!=CandidateRouteResult.Started)OnRoutingFailed?.Invoke(candidate,result);OnCandidateRemoved?.Invoke(candidate);}
  private void HandleMinutePassed(SimulationDateTime time){for(int i=_candidates.Count-1;i>=0;i--){var candidate=_candidates[i];if(candidate.Status!=CandidateStatus.WaitingForRecruitment)continue;candidate.AdvanceWaitingMinute();if(candidate.WaitingMinutes>=Configuration.MaximumWaitingMinutes)Reject(candidate);}_minutesUntilArrival--;if(_minutesUntilArrival<=0){TryGenerateArrival();_minutesUntilArrival=Configuration.ArrivalCadenceMinutes;}}
  private static EmployeeRole ToEmployeeRole(ProfessionalRole role)=>role switch{ProfessionalRole.Director=>EmployeeRole.Director,ProfessionalRole.Extra=>EmployeeRole.Extra,ProfessionalRole.Crew=>EmployeeRole.Crew,ProfessionalRole.Writer=>EmployeeRole.Writer,_=>EmployeeRole.Actor};
  public void Dispose()=>_time.OnMinutePassed-=HandleMinutePassed;
 }
}
