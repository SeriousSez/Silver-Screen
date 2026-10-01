using System;
using System.Linq;
using SilverScreen.Domain;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Interaction;
using UnityEngine;

namespace SilverScreen.Presentation.Recruitment
{
    /// <summary>Maps an operational building instance's authored metadata to generic recruitment and carry systems.</summary>
    public sealed class StageSchoolApplicantFacility : MonoBehaviour
    {
        [SerializeField] private RecruitmentDefinition _definition=RecruitmentDefinitions.Talent();
        private FacilityApplicantPool _pool;
        private RecruitmentDriver _recruitment;
        private RecruitmentFacility _facility;
        private CandidateWaitingAreaView _area;
        private Func<bool> _operational;
        public void Initialize(string id,StageSchoolInfrastructure infrastructure,FacilityApplicantPool pool,RecruitmentDriver recruitment,Func<bool> operational)
        {
            if(_facility!=null)return;
            _pool=pool;_recruitment=recruitment;_operational=operational;
            var waits=infrastructure.Anchors.Where(a=>a!=null&&a.name.StartsWith(_definition.WaitingAnchorPrefix,StringComparison.Ordinal)).OrderBy(a=>a.name,StringComparer.Ordinal).ToArray();
            _facility=new RecruitmentFacility(id,RecruitmentDestination.StageSchool,new[]{ProfessionalRole.Actor,ProfessionalRole.Director,ProfessionalRole.Extra},waits.Length,true,_definition);
            _facility.IsAvailable=()=>this!=null&&isActiveAndEnabled&&_operational();
            _area=gameObject.AddComponent<CandidateWaitingAreaView>();
            var approach=infrastructure.Anchor("entrance.approach");
            _area.ConfigureFacility(id,RecruitmentDestination.StageSchool,approach,approach,infrastructure.Anchor("exit"),waits);
            _area.BindReservations(_facility);
            _facility.IsReachable=()=>_recruitment!=null&&_recruitment.WorldRouter!=null&&_recruitment.WorldRouter.CanReceive(_area);
            var cut=infrastructure.GetComponent<BuildingCutawayController>();
            foreach(var region in infrastructure.Regions)
            {
                bool future=region.Id=="TALENT_CONTEXT";
                if(!future&&!Enum.TryParse<ProfessionalRole>(region.Id,true,out _))continue;
                var anchor=new GameObject("HiringRegion_"+region.Id).transform;anchor.SetParent(infrastructure.transform,false);anchor.localPosition=region.LocalBounds.center;
                var role=future?ProfessionalRole.Unassigned:(ProfessionalRole)Enum.Parse(typeof(ProfessionalRole),region.Id,true);
                var action=anchor.gameObject.AddComponent<ApplicantHiringAction>();action.Configure(id+":hire:"+region.Id,id,role,future);
                var target=anchor.gameObject.AddComponent<ContextualDropTarget>();
                var size=region.LocalBounds.size;
                target.ConfigureRoom(action,anchor,cut,new[]{new Rect(-size.x/2,-size.z/2,size.x,size.z)},Vector2.zero,size.x*.85f);
            }
            Register();
        }
        private void Register()
        {
            if(_facility==null||!isActiveAndEnabled||!_operational())return;
            _pool.Register(_facility);_recruitment.WorldRouter.RegisterArea(_area);
        }
        private void OnEnable()=>Register();
        private void OnDisable()
        {
            if(_facility==null)return;
            _pool.Remove(_facility.Id);
            _recruitment?.Coordinator?.FacilityUnavailable(_facility.Id);
            _recruitment?.WorldRouter?.UnregisterArea(_facility.Id);
            foreach(var id in _facility.WaitingReservations.Keys.ToArray())_facility.Release(id);
        }
    }
}
