using SilverScreen.Domain;
using SilverScreen.Presentation.SimulationTime;
using UnityEngine;

namespace SilverScreen.Presentation.Employees
{
    public sealed class AutonomousActivityOpportunityView : MonoBehaviour
    {
        [SerializeField] private string _opportunityId;
        [SerializeField] private string _displayName;
        [SerializeField] private PersonAutonomousActivity _activity = PersonAutonomousActivity.Rest;
        [SerializeField, Min(1)] private int _capacity = 1;
        [SerializeField, Range(0, 100)] private int _suitability;
        [SerializeField] private Transform _destination;
        private SimulationTimeDriver _driver;
        private bool _registered;

        public string OpportunityId => _opportunityId;

        private void OnEnable()
        {
            if (Application.isPlaying) Register();
        }

        private void Start()
        {
            if (Application.isPlaying && _driver == null) Register();
        }

        private void OnDisable()
        {
            if (_registered && _driver != null)
                _driver.AutonomyOpportunities.Unregister(_opportunityId);
            _registered = false;
            _driver = null;
        }

        public void Configure(string id, string displayName, PersonAutonomousActivity activity,
            int capacity = 1, int suitability = 0, Transform destination = null)
        {
            if (_registered && _driver != null)
                _driver.AutonomyOpportunities.Unregister(_opportunityId);
            _registered = false;
            _opportunityId = id;
            _displayName = displayName;
            _activity = activity;
            _capacity = capacity;
            _suitability = suitability;
            _destination = destination;
            if (isActiveAndEnabled && Application.isPlaying) Register();
        }

        public bool SetAvailable(bool available) =>
            _registered && _driver != null &&
            _driver.AutonomyOpportunities.SetAvailable(_opportunityId, available);

        private void Register()
        {
            if (string.IsNullOrWhiteSpace(_opportunityId))
            {
                Debug.LogError("Autonomous activity opportunity requires a stable ID.", this);
                return;
            }

            if (_driver == null) _driver = FindAnyObjectByType<SimulationTimeDriver>();
            if (_driver == null) return;
            var point = _destination != null ? _destination.position : transform.position;
            var opportunity = new PersonActivityOpportunity(_opportunityId,
                string.IsNullOrWhiteSpace(_displayName) ? _opportunityId : _displayName,
                _activity, new AutonomyPosition(point.x, point.y, point.z), _capacity, _suitability);
            _registered = _driver.AutonomyOpportunities.Register(opportunity);
            if (!_registered)
                Debug.LogError("Duplicate autonomous activity opportunity ID: " + _opportunityId, this);
        }
    }
}
