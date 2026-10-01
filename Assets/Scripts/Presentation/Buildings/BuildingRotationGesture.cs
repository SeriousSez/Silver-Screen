using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Distinguishes exact tap rotation from an R-held horizontal drag.</summary>
    public sealed class BuildingRotationGesture
    {
        private readonly float _holdSeconds;
        private readonly float _dragThreshold;
        private readonly float _degreesPerPixel;
        private readonly float _magnetInterval;
        private readonly float _magnetRange;
        private float _startedAt;
        private float _startPointerX;
        private float _startYaw;
        private float _lotYaw;
        private float _tapIncrement;
        private bool _reverseTap;

        private const float TapSnapTolerance = .01f;

        public bool IsActive { get; private set; }
        public bool IsFreeRotating { get; private set; }
        public float Yaw { get; private set; }

        public BuildingRotationGesture(float holdSeconds = .18f, float dragThreshold = 5f,
            float degreesPerPixel = .35f, float magnetInterval = 15f, float magnetRange = 2f)
        {
            _holdSeconds = holdSeconds;
            _dragThreshold = dragThreshold;
            _degreesPerPixel = degreesPerPixel;
            _magnetInterval = magnetInterval;
            _magnetRange = magnetRange;
        }

        public void Begin(float yaw, float pointerX, bool reverseTap, float unscaledTime, float lotYaw = 0f, float tapIncrement = 90f)
        {
            IsActive = true;
            IsFreeRotating = false;
            _startedAt = unscaledTime;
            _startPointerX = pointerX;
            _startYaw = Mathf.Repeat(yaw, 360f);
            _lotYaw = Mathf.Repeat(lotYaw, 360f);
            _tapIncrement = tapIncrement > 0f ? tapIncrement : 90f;
            _reverseTap = reverseTap;
            Yaw = _startYaw;
        }

        public float Update(float pointerX, float unscaledTime)
        {
            if (!IsActive) return Yaw;
            float drag = pointerX - _startPointerX;
            if (!IsFreeRotating && unscaledTime - _startedAt >= _holdSeconds && Mathf.Abs(drag) >= _dragThreshold)
                IsFreeRotating = true;
            if (!IsFreeRotating) return Yaw;
            float raw = Mathf.Repeat(_startYaw + drag * _degreesPerPixel, 360f);
            float snap = Mathf.Round(raw / _magnetInterval) * _magnetInterval;
            Yaw = Mathf.Abs(Mathf.DeltaAngle(raw, snap)) <= _magnetRange ? Mathf.Repeat(snap, 360f) : raw;
            return Yaw;
        }

        public float End(float pointerX, float unscaledTime)
        {
            Update(pointerX, unscaledTime);
            if (!IsFreeRotating) Yaw = SnapTapToLotQuarterTurn(_startYaw, _lotYaw, _tapIncrement, _reverseTap);
            IsActive = false;
            IsFreeRotating = false;
            return Yaw;
        }

        public void Cancel()
        {
            IsActive = false;
            IsFreeRotating = false;
        }

        /// <summary>Returns the adjacent lot-aligned turn; free rotation remains unconstrained.</summary>
        public static float SnapTapToLotQuarterTurn(float yaw, float lotYaw, float increment, bool reverse)
        {
            increment = increment > 0f ? increment : 90f;
            float relativeYaw = Mathf.Repeat(yaw - lotYaw, 360f);
            float nearestTurn = Mathf.Round(relativeYaw / increment) * increment;
            bool alreadyAligned = Mathf.Abs(Mathf.DeltaAngle(relativeYaw, nearestTurn)) <= TapSnapTolerance;
            float snappedTurn;
            if (reverse)
                snappedTurn = alreadyAligned ? nearestTurn - increment : Mathf.Floor(relativeYaw / increment) * increment;
            else
                snappedTurn = alreadyAligned ? nearestTurn + increment : Mathf.Ceil(relativeYaw / increment) * increment;
            return Mathf.Repeat(lotYaw + snappedTurn, 360f);
        }
    }
}
