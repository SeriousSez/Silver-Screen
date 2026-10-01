using UnityEngine;

namespace SilverScreen.Presentation.Interaction
{
    public enum HeldPersonPresentationState { Normal, Held, GroundSettle, InteractionEntry }
    public enum PersonReleasePresentation { Ground, Contextual, Cancel }

    public readonly struct HeldPersonPoseFrame
    {
        public readonly HeldPersonPresentationState State;
        public readonly Quaternion LocalRotation;
        public readonly Vector3 LocalOffset;
        public readonly float VerticalScale;
        public readonly float Weight;
        public HeldPersonPoseFrame(HeldPersonPresentationState state, Quaternion rotation, Vector3 offset, float verticalScale, float weight)
        { State = state; LocalRotation = rotation; LocalOffset = offset; VerticalScale = verticalScale; Weight = weight; }
    }

    /// <summary>Future animation hook. Own only visual children: never root motion, routing or domain state.
    /// Begin/End must suspend/restore this character's ordinary pose driver. Reaction IDs are presentation hints.</summary>
    public abstract class HeldPersonPoseAdapter : MonoBehaviour
    {
        public abstract void BeginPose(string reactionId);
        public abstract void ApplyPose(HeldPersonPoseFrame frame);
        public abstract void EndPose();
    }
}
