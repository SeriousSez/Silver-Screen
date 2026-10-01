using System.Linq;
using SilverScreen.Domain;
using SilverScreen.Domain.Recruitment;
using UnityEngine;

namespace SilverScreen.Presentation.Interaction
{
    /// <summary>Hiring, reassignment and confirmation requests share a room, not employment mutation rules.</summary>
    public sealed class WorkforceRoomAction : MonoBehaviour, IContextualRoomAction, IPersonDropCompleted
    {
        public string TargetId { get; private set; }
        public string FacilityId { get; private set; }
        public ProfessionalRole Profession { get; private set; }
        public bool Dismissal { get; private set; }
        public Transform Exit { get; private set; }
        public void Configure(string id, string facilityId, ProfessionalRole profession, bool dismissal, Transform exit)
        { TargetId = id; FacilityId = facilityId; Profession = profession; Dismissal = dismissal; Exit = exit; }
        private bool FacilityAllows(PersonDropContext context) => context.Recruitment?.Facilities?.Facilities.Any(
            f => f.Id == FacilityId && (Dismissal || f.Professions.Contains(Profession))) == true;
        public bool IsRelevant(PersonDropContext context) => isActiveAndEnabled &&
            (context.Employee?.IsEmployed == true || !Dismissal && context.Candidate != null &&
                context.Candidate.Status != CandidateStatus.Hired && context.Candidate.Status != CandidateStatus.Gone);
        public bool IsCurrent(PersonDropContext context) => !Dismissal && context.Employee?.IsEmployed == true &&
            (int)context.Employee.Role == (int)Profession;
        public bool CanExecute(PersonDropContext context)
        {
            if (!IsRelevant(context) || !FacilityAllows(context)) return false;
            if (context.Employee != null) return !IsCurrent(context) && context.Workforce != null &&
                context.Workforce.Workforce.CanChangeEmployment(context.Employee) && (!Dismissal || context.RequestDismissal != null);
            return !Dismissal && context.Recruitment.CanHire(context.Candidate, Profession, FacilityId);
        }
        public string FloorLabel(PersonDropContext context)
        {
            string label = Dismissal ? "DISMISS EMPLOYEE" : Profession == ProfessionalRole.ConstructionWorker ? "CONSTRUCTION WORKER" : "GROUNDSKEEPER";
            return label + (IsCurrent(context) ? "\nCURRENT PROFESSION" : !CanExecute(context) ? "\nUNAVAILABLE" : "");
        }
        public bool TryExecute(PersonDropContext context)
        {
            if (!CanExecute(context)) return false;
            if (Dismissal) return context.RequestDismissal(this, context.Employee);
            if (context.Employee != null) return context.Workforce.TryReassign(context.Employee, (EmployeeRole)Profession);
            return context.Recruitment.Hire(context.Candidate, Profession, FacilityId) != null;
        }
        public void OnPlaced(PersonDropContext context, Bounds assignmentArea)
        {
            if (Dismissal) return;
            var building = GetComponent<ContextualDropTarget>()?.InteriorVisibility;
            if (building != null)
            {
                var local = building.LocalInterior;
                assignmentArea = new Bounds(building.transform.TransformPoint(local.center), Vector3.zero);
                for (int corner = 0; corner < 8; corner++)
                    assignmentArea.Encapsulate(building.transform.TransformPoint(new Vector3(
                        (corner & 1) == 0 ? local.min.x : local.max.x,
                        (corner & 2) == 0 ? local.min.y : local.max.y,
                        (corner & 4) == 0 ? local.min.z : local.max.z)));
                assignmentArea.Expand(new Vector3(1.4f,0,1.4f));
            }
            string id = context.Employee?.Id ?? context.Candidate?.Person.Id;
            context.Workforce?.GetAgent(id)?.ResumeAfterWorkforcePlacement(assignmentArea);
        }
    }
}
