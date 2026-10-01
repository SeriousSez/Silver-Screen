using System;
using System.Linq;
using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Presentation.Recruitment;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Authored facility bindings; employment permissions remain in RecruitmentCoordinator.</summary>
    public sealed class StudioServicesFacility : MonoBehaviour
    {
        [SerializeField] private Transform _anchors;
        [SerializeField] private RecruitmentDefinition _recruitmentDefinition=RecruitmentDefinitions.StudioServices();
        [SerializeField] private PlacementRect _reservation = new PlacementRect(2.7f, -1.3f, 20.6f, 13.6f);
        public string FacilityId => StudioBuildingDefinitions.Service;
        public Transform Anchor(string name) => _anchors != null ? _anchors.Find(name) : null;
        public PlacementRect ReservationAt(Vector3 origin) => new PlacementRect(origin.x + _reservation.X, origin.z + _reservation.Z, _reservation.Width, _reservation.Depth);
        public PlacementRect ReservationAt(Vector3 origin, Quaternion rotation)
        {
            var center = origin + rotation * new Vector3(_reservation.X, 0, _reservation.Z);
            var width = rotation * new Vector3(_reservation.Width, 0, 0);
            var depth = rotation * new Vector3(0, 0, _reservation.Depth);
            return new PlacementRect(center.x, center.z,
                Mathf.Abs(width.x) + Mathf.Abs(depth.x), Mathf.Abs(width.z) + Mathf.Abs(depth.z));
        }
        public ConstructionVisualMetadata ConstructionMetadata => new ConstructionVisualMetadata(5.35f,
            new[] { new ScaffoldSegment("Left service wall", new LotPoint(-7.5f,-3.8f),new LotPoint(-7.5f,3.8f),4.22f),
                new ScaffoldSegment("Rear service wall",new LotPoint(-6.4f,5),new LotPoint(6.4f,5),4.22f) },
            new[] { new PlacementRect(-5.3f,-5.9f,4.2f,3.4f),new PlacementRect(3.8f,-5,5.5f,2.5f),new PlacementRect(8.8f,-3.2f,3.6f,3) },
            new[] {new PlacementRect(11.5f,1.2f,1.1f,4.1f)});
        public void Configure(Transform anchors) => _anchors = anchors;
        private void Awake() => StudioServicesContextualRooms.Install(this);
        public CandidateWaitingAreaView Register(FacilityApplicantPool facilities)
        {
            if (_anchors == null) throw new InvalidOperationException("Studio Services requires authored local anchors.");
            var area = GetComponent<CandidateWaitingAreaView>() ?? gameObject.AddComponent<CandidateWaitingAreaView>();
            var waits = Enumerable.Range(0,6).Select(i => Anchor(_recruitmentDefinition.WaitingAnchorPrefix+i.ToString("00"))).ToArray();
            if (waits[5] == null)
            {
                // Revision 1 of the art prefab authored five positions. Keep the visual
                // source untouched and add the sixth semantic position beside the row.
                waits[5] = new GameObject("ApplicantWaiting_05").transform;
                waits[5].SetParent(_anchors, false);
                waits[5].localPosition = new Vector3(-2.2f, 0f, -6.45f);
            }
            if (waits.Any(t=>t==null)) throw new InvalidOperationException("Studio Services waiting anchors incomplete.");
            area.ConfigureFacility(FacilityId, RecruitmentDestination.ServiceFacility, Anchor("OfficeApproach"), Anchor("ApplicantArrival"), Anchor("ApplicantExit"), waits);
            var recruitment=new RecruitmentFacility(FacilityId,RecruitmentDestination.ServiceFacility,new[] {ProfessionalRole.ConstructionWorker,ProfessionalRole.Groundskeeper},waits.Length,false,_recruitmentDefinition);
            recruitment.IsAvailable=()=>this!=null&&isActiveAndEnabled;
            area.BindReservations(recruitment);
            facilities.Register(recruitment);
            return area;
        }
    }
}
