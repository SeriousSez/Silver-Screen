using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Interaction;
using SilverScreen.Domain.Recruitment;
using UnityEngine;

namespace SilverScreen.Presentation.Interaction
{
    public enum PersonSpotActivity { Hire, Practice }
    /// <summary>Author one component per capacity slot. Position and facing come from its transform.</summary>
    public sealed class PersonInteractionSpot : MonoBehaviour, IPersonDropAction
    {
        [SerializeField] private string _semanticId;
        [SerializeField] private string _facilityId;
        [SerializeField] private PersonSpotActivity _activity;
        [SerializeField] private ProfessionalRole _profession;
        [SerializeField] private string _genreId = "comedy";
        [SerializeField] private string _label;
        [SerializeField] private float _revealDistance = 12f;
        [SerializeField] private Transform _animationAnchor;
        [SerializeField] private SilverScreen.Presentation.Buildings.BuildingCutawayController _interiorVisibility;
        public SilverScreen.Presentation.Buildings.BuildingCutawayController InteriorVisibility => _interiorVisibility;
        public void RequireInteriorReveal(SilverScreen.Presentation.Buildings.BuildingCutawayController owner) { _interiorVisibility = owner; GetComponent<ContextualDropTarget>()?.SetInterior(owner); }
        public string SemanticId => _semanticId;
        public string TargetId => _semanticId;
        public string FacilityId => _facilityId;
        public string Label => _label;
        public string GenreId => _genreId;
        public PersonSpotActivity Activity => _activity;
        public ProfessionalRole Profession => _profession;
        public int Capacity => 1;
        public Transform AnimationAnchor => _animationAnchor;
        private static readonly List<PersonInteractionSpot> _active = new List<PersonInteractionSpot>();
        public static IReadOnlyList<PersonInteractionSpot> Active => _active;
        private void OnEnable() { if (!_active.Contains(this)) _active.Add(this); if (Application.isPlaying) EnsureTarget(); }
        private void OnDisable() => _active.Remove(this);
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => _active.Clear();
        public void Configure(string id, string facilityId, PersonSpotActivity activity, ProfessionalRole profession, string label)
        { _semanticId = id; _facilityId = facilityId; _activity = activity; _profession = profession; _label = label; EnsureTarget(true); }
        private ContextualDropTarget _generatedTarget;
        private void EnsureTarget(bool refresh = false)
        {
            var target = GetComponent<ContextualDropTarget>();
            if (target != null && target.Action != null && !(refresh && target == _generatedTarget)) return;
            if (target == null) _generatedTarget = target = gameObject.AddComponent<ContextualDropTarget>();
            target.Configure(this, transform, _interiorVisibility,
                _activity == PersonSpotActivity.Hire ? FloorTargetShape.Zone : FloorTargetShape.Round,
                _activity == PersonSpotActivity.Hire ? new Vector2(1.8f, 1.8f) : new Vector2(1.2f, 1.2f),
                _activity == PersonSpotActivity.Practice ? FloorTargetSymbol.Performance : FloorTargetSymbol.None);
        }
        public bool CanExecute(PersonDropContext context) => IsRelevant(context.Candidate, context.Employee, context.Recruitment, context.Practice);
        public bool TryExecute(PersonDropContext context)
        {
            if (!CanExecute(context)) return false;
            return _activity == PersonSpotActivity.Hire
                ? context.Recruitment.Hire(context.Candidate, _profession, _facilityId) != null
                : context.Practice.TryStart(context.Employee, _semanticId, _genreId);
        }
        public bool IsRelevant(Candidate candidate, Employee employee, RecruitmentCoordinator recruitment, PersonPracticeService practice)
        {
            if (!isActiveAndEnabled || string.IsNullOrWhiteSpace(_semanticId)) return false;
            return _activity == PersonSpotActivity.Hire
                ? recruitment != null && recruitment.CanHire(candidate, _profession, _facilityId)
                : practice != null && practice.CanPracticeAt(employee, _semanticId);
        }
        public bool IsVisible(bool held, Vector3 cursor, Candidate candidate, Employee employee, RecruitmentCoordinator recruitment, PersonPracticeService practice)
            => held && (_interiorVisibility == null || _interiorVisibility.IsRevealed) && Vector3.Distance(cursor, transform.position) <= _revealDistance && IsRelevant(candidate, employee, recruitment, practice);

        // Prototype authoring uses the facility's own waiting-area frame, never coordinates inferred from its profession.
        public static void CreateHiringSpots(Transform parent, string facilityId, Transform entrance, IEnumerable<ProfessionalRole> professions)
        {
            int index = 0;
            foreach (var profession in professions)
            {
                string id = facilityId + ":hire:" + profession;
                var anchor = new GameObject(id).transform; anchor.SetParent(parent, false);
                anchor.position = entrance.position + parent.TransformDirection(new Vector3((index++ - 1) * 1.6f, 0, -3f));
                anchor.rotation = parent.rotation;
                anchor.gameObject.AddComponent<PersonInteractionSpot>().Configure(id, facilityId, PersonSpotActivity.Hire, profession, "Hire as " + profession);
            }
        }
    }
}
