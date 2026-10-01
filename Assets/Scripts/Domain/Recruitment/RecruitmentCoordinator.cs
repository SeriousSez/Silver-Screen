using System;
using System.Collections.Generic;
using System.Linq;
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
 public sealed partial class RecruitmentCoordinator:IDisposable
 {
  private readonly List<Candidate> _candidates=new List<Candidate>();private readonly ISimulationTimeService _time;private readonly CandidateGenerator _generator;private readonly ICandidateWorldRouter _router;private int _minutesUntilArrival;private bool _generatedPrototypeCandidate;
 
  public RecruitmentConfiguration Configuration{get;} public IReadOnlyList<Candidate> Candidates=>_candidates;public bool HasCapacity=>_candidates.Count<Configuration.Capacity;
 public int OpeningArrivalsRemaining=>StarterRemaining(RecruitmentCategory.StudioServices);
  public event Action<Candidate> OnCandidateAdded;public event Action<Candidate> OnCandidateChanged;public event Action<Candidate> OnCandidateRemoved;public event Action<Candidate,Employee> OnCandidateHired;public event Action<Candidate,CandidateRouteResult> OnRoutingFailed;
  public RecruitmentCoordinator(ISimulationTimeService time,CandidateGenerator generator,ICandidateWorldRouter router,RecruitmentConfiguration configuration=null){_time=time??throw new ArgumentNullException(nameof(time));_generator=generator??throw new ArgumentNullException(nameof(generator));_router=router??throw new ArgumentNullException(nameof(router));Configuration=configuration??new RecruitmentConfiguration();_minutesUntilArrival=1;_time.OnMinutePassed+=HandleMinutePassed;}
  public PersonPopulation Population { get; set; } = new PersonPopulation();
  private readonly List<Candidate> _history=new List<Candidate>();
  public IReadOnlyList<Candidate> ApplicantHistory => _history;
  public TalentApplicantSettings TalentSettings { get; private set; } = new TalentApplicantSettings();
  private int _nextTalentFacility;
  public int TalentMinutesUntilArrival => IntakeMinutes(RecruitmentCategory.Talent);
  public RecruitmentStateSnapshot Capture()=>new RecruitmentStateSnapshot{TalentMinutesUntilArrival=TalentMinutesUntilArrival,CategoryIntakes=CaptureIntakes(),NextTalentFacility=_nextTalentFacility,Applicants=_history.Select(c=>c.Capture()).ToArray()};
  public void ConfigureTalentApplicants(TalentApplicantSettings settings) { settings.Validate();TalentSettings=settings; }
  public Func<bool> RecruitmentAvailable { get; set; }
  private FacilityApplicantPool _facilities;
  public FacilityApplicantPool Facilities { get=>_facilities; set {
   if(_facilities!=null)_facilities.Registered-=ObserveFacility;
   _facilities=value;
   if(_facilities!=null){_facilities.Registered+=ObserveFacility;foreach(var f in _facilities.Facilities)ObserveFacility(f);}
  }}
  // Compatibility entry point: adjusts the Services category definition, never creates a second wave.
  public void ConfigureOpeningWave(OpeningRecruitmentWave wave)
  {
   if(wave==null)throw new ArgumentNullException(nameof(wave));
   if(_history.Count>0||_intakes.Values.Any(s=>s.Remaining!=s.Target))throw new InvalidOperationException("Configure intake before recruitment begins.");
   if(Facilities!=null)foreach(var f in Facilities.Facilities.Where(f=>f.Definition?.Category==RecruitmentCategory.StudioServices)){
    if(_intakes.TryGetValue(RecruitmentCategory.StudioServices,out var state)){state.Target=state.Remaining=wave.Roles.Count;}
    f.Definition.StarterApplicantCount=wave.Roles.Count;f.Definition.StarterSpacingMinutes=wave.ArrivalCadenceMinutes;f.Definition.StarterProfessionSequence=wave.Roles.ToArray();
   }
  }
  public Candidate TryGenerateArrival()
  {
   return TryGenerateArrival(null);
  }
  private Candidate TryGenerateArrival(ProfessionalRole? requestedProfession, bool legacyOnly=false)
  {
   if(RecruitmentAvailable!=null&&!RecruitmentAvailable())return null;
   Candidate candidate;
   if(Facilities!=null)
   {
    ProfessionalRole profession;
    RecruitmentFacility facility;
    if(requestedProfession.HasValue){profession=requestedProfession.Value;facility=Facilities.SelectForProfession(profession,id=>(!legacyOnly||Facilities.Facilities.Any(f=>f.Id==id&&f.Definition==null))&&_candidates.Count(c=>c.FacilityId==id)<Configuration.Capacity);}
    else facility=Facilities.Select(id=>(!legacyOnly||Facilities.Facilities.Any(f=>f.Id==id&&f.Definition==null))&&_candidates.Count(c=>c.FacilityId==id)<Configuration.Capacity,out profession);
    if(facility==null)return null;
    candidate=requestedProfession.HasValue?_generator.GenerateEntryLevelForRole(_time.CurrentTime,profession):_generator.GenerateForRole(_time.CurrentTime,profession);candidate.AssignFacility(facility);
   }
   else
   {
    if(!HasCapacity)return null;
    var role=!_generatedPrototypeCandidate?Configuration.FirstPrototypeCandidateRole:null;
    candidate=role.HasValue?_generator.GenerateForRole(_time.CurrentTime,role.Value):_generator.Generate(_time.CurrentTime);
    _generatedPrototypeCandidate=true;
   }
   Population.Register(candidate.Person);_history.Add(candidate);_candidates.Add(candidate);OnCandidateAdded?.Invoke(candidate);
   var result=_router.RouteToRecruitmentLocation(candidate,(completed,position)=>HandleArrival(candidate,completed,position));
   if(result!=CandidateRouteResult.Started)HandleArrival(candidate,result,-1);return candidate;
  }
  public Employee Hire(Candidate candidate) => candidate == null ? null : Hire(candidate, candidate.JobSought);
  public bool CanHire(Candidate candidate, ProfessionalRole profession, string hiringFacilityId = null)
  {
   if (profession == ProfessionalRole.Unassigned || !Enum.IsDefined(typeof(ProfessionalRole), profession) || candidate == null || !_candidates.Contains(candidate) || candidate.Status != CandidateStatus.WaitingForRecruitment) return false;
   if (RecruitmentAvailable != null && !RecruitmentAvailable()) return false;
   // Facilities control available hiring interactions, never the person's origin or proficiency.
   return Facilities == null || Facilities.Facilities.Any(f => f.Id == (hiringFacilityId ?? candidate.FacilityId) && f.Available && f.Professions.Contains(profession) && (!candidate.IsTalentApplicant || f.ProfessionNeutral && CandidateFacilityAvailable(candidate)));
  }
  public Employee Hire(Candidate candidate, ProfessionalRole profession, string hiringFacilityId = null)
  {
   if (!CanHire(candidate, profession, hiringFacilityId) || !candidate.MarkHired()) return null;
   candidate.Person.Career.CompleteGoal(PersonCareerGoalType.GetHired, _time.CurrentTime.Year);
   candidate.Person.SetProfessionalRole(profession);
   var employee = new Employee(candidate.Person, ToEmployeeRole(profession), candidate.SalaryExpectation, 80);
   ReleaseWaiting(candidate);OnCandidateHired?.Invoke(candidate, employee);
   _router.ReleaseCandidate(candidate); _candidates.Remove(candidate); OnCandidateRemoved?.Invoke(candidate);
   return employee;
  }
  public bool Reject(Candidate candidate){if(candidate==null||!_candidates.Contains(candidate)||!candidate.BeginDeparture())return false;FinishStarterArrival(candidate,false);ReleaseWaiting(candidate);OnCandidateChanged?.Invoke(candidate);var result=_router.RouteToExit(candidate,(completed,unused)=>HandleDeparture(candidate,completed));if(result!=CandidateRouteResult.Started)HandleDeparture(candidate,result);return true;}
  private void HandleArrival(Candidate candidate,CandidateRouteResult result,int position){if(!_candidates.Contains(candidate)||candidate.Status!=CandidateStatus.Arriving)return;if(result==CandidateRouteResult.Started&&candidate.MarkWaiting(position)){FinishStarterArrival(candidate,true);OnCandidateChanged?.Invoke(candidate);return;}FinishStarterArrival(candidate,false);candidate.MarkGone();ReleaseWaiting(candidate);_router.ReleaseCandidate(candidate);_candidates.Remove(candidate);OnRoutingFailed?.Invoke(candidate,result);OnCandidateRemoved?.Invoke(candidate);}
  private void HandleDeparture(Candidate candidate,CandidateRouteResult result){if(!_candidates.Contains(candidate))return;FinishStarterArrival(candidate,false);candidate.MarkGone();ReleaseWaiting(candidate);_router.ReleaseCandidate(candidate);_candidates.Remove(candidate);if(result!=CandidateRouteResult.Started)OnRoutingFailed?.Invoke(candidate,result);OnCandidateRemoved?.Invoke(candidate);}
  private void HandleMinutePassed(SimulationDateTime time)
  {
   AdvanceTalentApplicants();AdvanceCategoryIntakes();
   for(int i=_candidates.Count-1;i>=0;i--){var c=_candidates[i];if(c.IsTalentApplicant||c.Status!=CandidateStatus.WaitingForRecruitment)continue;if(c.FacilityId!=null&&!CandidateFacilityAvailable(c)){FacilityUnavailable(c.FacilityId);continue;}c.AdvanceWaitingMinute();if(c.WaitingMinutes>=Configuration.MaximumWaitingMinutes)Reject(c);}
   // Existing unconfigured prototype facilities retain their legacy cadence.
   if(--_minutesUntilArrival<=0){TryGenerateArrival(null,true);_minutesUntilArrival=Configuration.ArrivalCadenceMinutes;}
  }

  private bool CandidateFacilityAvailable(Candidate candidate) => Facilities?.Facilities.Any(f=>f.Id==candidate.FacilityId&&f.Available)==true;
  private void ReleaseWaiting(Candidate candidate)
  {
   if(Facilities!=null)foreach(var f in Facilities.Facilities)f.Release(candidate.Person.Id);
   candidate.ReleaseWaiting();
  }
  private RecruitmentFacility SelectTalentFacility()
  {
   if(Facilities==null)return null;
   var all=Facilities.Facilities;
   for(int i=0;i<all.Count;i++){
    var f=all[(_nextTalentFacility+i)%all.Count];
    if(!f.ProfessionNeutral||!f.Available||!f.HasWaitingCapacity||f.IsReachable!=null&&!f.IsReachable())continue;
    _nextTalentFacility=(_nextTalentFacility+i+1)%all.Count;return f;
   }
   return null;
  }
  public Candidate TryGenerateTalentArrival()
  {
   if(RecruitmentAvailable!=null&&!RecruitmentAvailable())return null;
   if(_candidates.Count(c=>c.IsTalentApplicant)>=TalentSettings.MaximumActiveApplicants)return null;
   var facility=SelectTalentFacility();if(facility==null)return null;
   var candidate=_generator.GenerateTalentApplicant(_time.CurrentTime);
   if(!facility.TryReserve(candidate.Person.Id,out int index))return null;
   candidate.AssignFacility(facility);candidate.ReserveWaiting(index);
   Population.Register(candidate.Person);_history.Add(candidate);_candidates.Add(candidate);OnCandidateAdded?.Invoke(candidate);
   RouteTalent(candidate);return candidate;
  }
  private void RouteTalent(Candidate candidate)
  {
   string owner=candidate.FacilityId;
   var result=_router.RouteToRecruitmentLocation(candidate,(completed,position)=>{
    if(candidate.FacilityId==owner&&candidate.Status==CandidateStatus.Arriving)HandleArrival(candidate,completed,position);
   });
   if(result!=CandidateRouteResult.Started)HandleArrival(candidate,result,-1);
  }
  public void FacilityUnavailable(string id)
  {
   // Called by lifecycle events and reconciled each simulation minute as a safety net.
   foreach(var candidate in _candidates.Where(c=>c.FacilityId==id&&c.Status!=CandidateStatus.Departing).ToArray()){
    ReleaseWaiting(candidate);
    var next=candidate.IsTalentApplicant?SelectTalentFacility():Facilities?.Facilities.FirstOrDefault(f=>f.Id!=id&&f.Available&&f.Definition?.Category==candidate.IntakeCategory&&f.Professions.Contains(candidate.JobSought)&&f.HasWaitingCapacity&&
     (f.IsReachable==null||f.IsReachable()));
    if(next!=null&&next.Id!=id&&next.TryReserve(candidate.Person.Id,out int slot)){
     candidate.Reassign(next,slot);OnCandidateChanged?.Invoke(candidate);RouteTalent(candidate);
    }else Reject(candidate);
   }
  }
  private void AdvanceTalentApplicants()
  {
   foreach(var candidate in _candidates.Where(c=>c.IsTalentApplicant).ToArray()){
    if(candidate.Status!=CandidateStatus.Departing&&!CandidateFacilityAvailable(candidate)){FacilityUnavailable(candidate.FacilityId);continue;}
    if(candidate.IsCarried)continue;
    if(candidate.Status==CandidateStatus.WaitingForRecruitment){candidate.AdvanceWaitingMinute();if(candidate.WaitingMinutes>=TalentSettings.MaximumWaitingMinutes)Reject(candidate);}
    else if(candidate.Status==CandidateStatus.Arriving||candidate.Status==CandidateStatus.Departing){
     candidate.AdvanceTravelMinute();if(candidate.TravelMinutes>=TalentSettings.MaximumTravelMinutes){
      if(candidate.Status==CandidateStatus.Arriving)Reject(candidate);else HandleDeparture(candidate,CandidateRouteResult.NavigationRejected);
     }
    }
   }

  }
  private static EmployeeRole ToEmployeeRole(ProfessionalRole role)=>role switch{ProfessionalRole.Director=>EmployeeRole.Director,ProfessionalRole.Extra=>EmployeeRole.Extra,ProfessionalRole.Crew=>EmployeeRole.Crew,ProfessionalRole.Writer=>EmployeeRole.Writer,ProfessionalRole.ConstructionWorker=>EmployeeRole.ConstructionWorker,ProfessionalRole.Groundskeeper=>EmployeeRole.Groundskeeper,_=>EmployeeRole.Actor};
  public void Dispose(){_time.OnMinutePassed-=HandleMinutePassed;if(_facilities!=null)_facilities.Registered-=ObserveFacility;}
 }
}
