using System;
using System.Collections.Generic;
using System.Linq;
namespace SilverScreen.Domain.Recruitment
{
 public sealed partial class RecruitmentCoordinator
 {
  private sealed class IntakeState
  {
   public RecruitmentCategory Category;
   public RecruitmentDefinition Definition;
   public int Target,Remaining,Minutes,NextFacility;
   public readonly HashSet<string> InFlight=new HashSet<string>();
   public bool Complete=>Remaining==0&&InFlight.Count==0;
  }
  private readonly Dictionary<RecruitmentCategory,IntakeState> _intakes=new Dictionary<RecruitmentCategory,IntakeState>();
  private void ObserveFacility(RecruitmentFacility facility)
  {
   var definition=facility.Definition;
   if(definition==null||!facility.Available)return;
   if(_intakes.TryGetValue(definition.Category,out var known)){known.Definition??=definition;return;}
   definition.Validate();
   _intakes.Add(definition.Category,new IntakeState{Category=definition.Category,Definition=definition,Target=definition.StarterApplicantCount,
    Remaining=definition.StarterApplicantCount,Minutes=definition.StarterApplicantCount>0?definition.StarterInitialDelayMinutes:definition.NormalCadenceMinutes});
  }
  public int StarterRemaining(RecruitmentCategory category)=>_intakes.TryGetValue(category,out var s)?s.Remaining+s.InFlight.Count:0;
  public int IntakeMinutes(RecruitmentCategory category)=>_intakes.TryGetValue(category,out var s)?s.Minutes:0;
  public RecruitmentIntakeSnapshot[] CaptureIntakes()=>_intakes.Values.OrderBy(s=>s.Category).Select(s=>new RecruitmentIntakeSnapshot{
   Category=s.Category,Granted=true,StarterTarget=s.Target,RemainingToDispatch=s.Remaining,MinutesUntilArrival=s.Minutes,NextFacility=s.NextFacility,InFlightPersonIds=s.InFlight.OrderBy(id=>id,StringComparer.Ordinal).ToArray()}).ToArray();
  /// <summary>Restore category policy state before restoring profiles/applicants and registering facilities.
  /// In-flight IDs reconnect through the existing arrival callback; this is not a full save system.</summary>
  public void RestoreIntakes(IEnumerable<RecruitmentIntakeSnapshot> snapshots)
  {
   if(_candidates.Count>0||_intakes.Count>0)throw new InvalidOperationException("Restore intake state before recruitment starts.");
   if(snapshots==null)return;
   foreach(var item in snapshots){
    if(!item.Granted)continue;
    if(!Enum.IsDefined(typeof(RecruitmentCategory),item.Category)||item.StarterTarget<0||item.RemainingToDispatch<0||item.MinutesUntilArrival<0||item.NextFacility<0||
     item.RemainingToDispatch+(item.InFlightPersonIds?.Length??0)>item.StarterTarget)throw new ArgumentException("Invalid recruitment intake snapshot.");
    var state=new IntakeState{Category=item.Category,Target=item.StarterTarget,Remaining=item.RemainingToDispatch,Minutes=item.MinutesUntilArrival,NextFacility=item.NextFacility};
    if(item.InFlightPersonIds!=null)foreach(var id in item.InFlightPersonIds)if(string.IsNullOrWhiteSpace(id)||!state.InFlight.Add(id))throw new ArgumentException("Duplicate/missing in-flight identity.");
    _intakes.Add(item.Category,state);
   }
  }
  private RecruitmentFacility SelectIntakeFacility(IntakeState state)
  {
   var all=Facilities.Facilities;
   for(int i=0;i<all.Count;i++){
    int index=(state.NextFacility+i)%all.Count;var f=all[index];var d=f.Definition;
    if(d==null||d.Category!=state.Category||!f.Available||!f.HasWaitingCapacity||f.WaitingReservations.Count>=d.WaitingCapacity||f.IsReachable!=null&&!f.IsReachable())continue;
    // Preserve C1's initial active-applicant limit. This never reads the employee roster.
    if(f.ProfessionNeutral&&_candidates.Count(c=>c.IsTalentApplicant)>=TalentSettings.MaximumActiveApplicants)continue;
    state.NextFacility=(index+1)%all.Count;return f;
   }
   return null;
  }
  private void AdvanceCategoryIntakes()
  {
   if(Facilities==null)return;
   foreach(var f in Facilities.Facilities)ObserveFacility(f);
   foreach(var state in _intakes.Values){
    if(!Facilities.Facilities.Any(f=>f.Definition?.Category==state.Category&&f.Available))continue;
    if(state.Remaining==0&&state.InFlight.Count>0)continue; // Finish actual arrivals before normal cadence starts.
    if(--state.Minutes>0)continue;
    var definition=Facilities.Facilities.First(f=>f.Definition?.Category==state.Category&&f.Available).Definition;
    if(RecruitmentAvailable!=null&&!RecruitmentAvailable()){state.Minutes=definition.RetryMinutes;continue;}
    var facility=SelectIntakeFacility(state);
    if(facility==null){state.Minutes=definition.RetryMinutes;continue;}
    bool starter=state.Remaining>0;
    state.Minutes=starter?definition.StarterSpacingMinutes:definition.NormalCadenceMinutes;
    var c=BeginIntakeArrival(facility,state,starter);
    if(c==null)state.Minutes=definition.RetryMinutes;
   }
  }
  private Candidate BeginIntakeArrival(RecruitmentFacility facility,IntakeState state,bool starter)
  {
   var definition=facility.Definition;
   if(!facility.ProfessionNeutral&&facility.Professions.Count==0)return null;
   var role=facility.ProfessionNeutral?ProfessionalRole.Unassigned:facility.Professions[facility.NextProfession++%facility.Professions.Count];
   if(starter&&!facility.ProfessionNeutral&&definition.StarterProfessionSequence?.Length>0){
    var authored=definition.StarterProfessionSequence[(state.Target-state.Remaining)%definition.StarterProfessionSequence.Length];
    if(facility.Professions.Contains(authored))role=authored;
   }
   var candidate=facility.ProfessionNeutral?_generator.GenerateTalentApplicant(_time.CurrentTime):
    starter?_generator.GenerateEntryLevelForRole(_time.CurrentTime,role):_generator.GenerateForRole(_time.CurrentTime,role);
   if(!facility.TryReserve(candidate.Person.Id,out int slot))return null;
   candidate.AssignFacility(facility);candidate.ReserveWaiting(slot);
   if(starter){state.Remaining--;state.InFlight.Add(candidate.Person.Id);}
   Population.Register(candidate.Person);_history.Add(candidate);_candidates.Add(candidate);OnCandidateAdded?.Invoke(candidate);
   // Shared C1 world-entry/approach/wait routing. No direct waiting-anchor spawn.
   RouteTalent(candidate);
   return candidate;
  }
  private void FinishStarterArrival(Candidate candidate,bool arrived)
  {
   foreach(var state in _intakes.Values){
    if(!state.InFlight.Remove(candidate.Person.Id))continue;
    var definition=state.Definition;
    if(!arrived){state.Remaining++;state.Minutes=definition.RetryMinutes;}
    else if(state.Complete)state.Minutes=definition.NormalCadenceMinutes;
    return;
   }
  }
 }
}
