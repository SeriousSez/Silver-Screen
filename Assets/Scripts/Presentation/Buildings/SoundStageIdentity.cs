using System;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>
    /// Instance data, independent of the architectural asset.
    /// Future construction must allocate a unique facility ID and historical stage
    /// number, and save both. Demolition must not renumber surviving stages.
    /// Unity serialization is not a runtime save/load implementation.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SoundStageIdentity : MonoBehaviour
    {
        [SerializeField] private string _facilityId;
        [SerializeField, Min(1)] private int _stageNumber;
        [SerializeField] private BuildingSign _sign;
        [SerializeField] private StageArchitecturalSign _architecturalSign;

        public string FacilityId => _facilityId;
        public int StageNumber => _stageNumber;

        public void Initialize(string facilityId, int stageNumber, BuildingSign sign)
        {
            if (string.IsNullOrWhiteSpace(facilityId))
                throw new ArgumentException("A stage requires a stable facility ID.", nameof(facilityId));
            if (stageNumber < 1) throw new ArgumentOutOfRangeException(nameof(stageNumber));
            if (!string.IsNullOrEmpty(_facilityId) &&
                (_facilityId != facilityId || _stageNumber != stageNumber))
                throw new InvalidOperationException("An existing stage identity cannot be reassigned.");
            _facilityId = facilityId;
            _stageNumber = stageNumber;
            _sign = sign;
            RefreshSign();
        }

        private void Awake() => RefreshSign();

        public void RefreshSign()
        {
            if (_architecturalSign != null)
            {
                _architecturalSign.SetNumber(_stageNumber);
                return;
            }
            if (_stageNumber > 0 && _sign != null)
                _sign.SetText("STAGE " + _stageNumber);
        }
    }
}
