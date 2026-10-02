using SilverScreen.Domain;
using SilverScreen.Domain.Recruitment;
using UnityEngine;
namespace SilverScreen.Presentation.Interaction
{
    /// <summary>Direct applicant hiring only; never reassigns or dismisses employees.</summary>
    public sealed class ApplicantHiringAction : MonoBehaviour,IContextualRoomAction,IPersonDropCompleted
    {
        public string TargetId { get; private set; }
        public string FacilityId { get; private set; }
        public ProfessionalRole Profession { get; private set; }
        public bool FutureOnly { get; private set; }
        public void Configure(string id,string facilityId,ProfessionalRole profession,bool futureOnly=false)
        { TargetId=id;FacilityId=facilityId;Profession=profession;FutureOnly=futureOnly; }
        public bool IsRelevant(PersonDropContext context)=>isActiveAndEnabled&&context.Employee==null&&context.Candidate!=null&&context.Recruitment!=null&&
            (FutureOnly?context.Recruitment.CanOfferHiringAt(context.Candidate,FacilityId):context.Recruitment.CanHire(context.Candidate,Profession,FacilityId));
        public bool CanExecute(PersonDropContext context)=>!FutureOnly&&IsRelevant(context)&&context.Recruitment.CanHire(context.Candidate,Profession,FacilityId);
        public bool IsCurrent(PersonDropContext context)=>false;
        public string FloorLabel(PersonDropContext context)=>FutureOnly?"CREATE / IMPORT TALENT\nNOT YET AVAILABLE":Profession.ToString().ToUpperInvariant();
        public bool TryExecute(PersonDropContext context)=>CanExecute(context)&&context.Recruitment.Hire(context.Candidate,Profession,FacilityId)!=null;
        public void OnPlaced(PersonDropContext context,Bounds assignmentArea)
            =>context.Workforce?.GetAgent(context.Candidate.Person.Id)?.ResumeAfterWorkforcePlacement(assignmentArea);
    }
}
