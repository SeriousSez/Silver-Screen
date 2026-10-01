using UnityEngine;

namespace SilverScreen.Presentation.Characters
{
    public enum CharacterPose { Idle, Walk, PurposefulWalk, Run, Start, Stop, Turn, RaisedArms, ElbowBend, KneeBend, Twist, Reach, Crouch }

    /// <summary>
    /// Gate motion studies retarget common trajectories to calibrated limb lengths.
    /// This is a deterministic presentation sampler, not movement or traversal authority.
    /// </summary>
    public static class CharacterMotion
    {
        public static void Sample(CharacterView view, CharacterPose pose, float seconds, Vector3? handleTarget = null)
        {
            view.ResetPose();
            var root = view.transform;
            float scale = view.StandingHeight / 1.75f;
            bool run = pose == CharacterPose.Run;
            bool moving = run || pose == CharacterPose.Walk || pose == CharacterPose.PurposefulWalk || pose == CharacterPose.Start || pose == CharacterPose.Stop;
            float envelope = pose == CharacterPose.Start ? Mathf.SmoothStep(0, 1, seconds / .45f) :
                pose == CharacterPose.Stop ? 1 - Mathf.SmoothStep(0, 1, seconds / .45f) : 1;
            float rate = run ? 1.45f : pose == CharacterPose.PurposefulWalk ? 1.05f : .82f;
            float cycle = seconds * rate;
            float stride = (run ? .65f : .36f) * scale * envelope;
            float crouch = pose == CharacterPose.Crouch ? .40f * scale : 0;
            var hips = view.Bone("Hips");
            hips.position += root.up * (-crouch + (moving ? Mathf.Cos(cycle * Mathf.PI * 4) * .012f * scale * envelope : Mathf.Sin(seconds * 1.6f) * .003f * scale));
            RotateWorld(view.Bone("Spine"), root.right, pose == CharacterPose.Crouch ? 20 : run ? 8 : 0);
            if (pose == CharacterPose.Twist) RotateWorld(view.Bone("Spine1"), root.up, 28);
            if (pose == CharacterPose.Turn)
            {
                float angle = Mathf.Sin(seconds * 2) * 25;
                RotateWorld(hips, root.up, angle * .55f);
                RotateWorld(view.Bone("Spine1"), root.up, angle * .45f);
            }
            for (int side = 0; side < 2; side++)
            {
                string prefix = side == 0 ? "Left" : "Right";
                float sign = side == 0 ? -1 : 1;
                var upper = view.Bone(prefix + "UpLeg"); var lower = view.Bone(prefix + "Leg"); var foot = view.Bone(prefix + "Foot");
                Quaternion footRotation = foot.rotation;
                float ankle = root.InverseTransformPoint(foot.position).y + crouch;
                float phase = Mathf.Repeat(cycle + side * .5f, 1);
                float z = 0, lift = 0;
                if (moving)
                {
                    // First 60% is ground contact; remaining 40% returns the foot.
                    if (phase < .6f) z = Mathf.Lerp(stride * .5f, -stride * .5f, phase / .6f);
                    else { float swing = (phase - .6f) / .4f; z = Mathf.Lerp(-stride * .5f, stride * .5f, Mathf.SmoothStep(0, 1, swing)); lift = Mathf.Sin(swing * Mathf.PI) * (run ? .19f : .085f) * scale * envelope; }
                }
                if (pose == CharacterPose.KneeBend && side == 0) { lift = .37f * scale; z = .18f * scale; }
                var target = root.TransformPoint(new Vector3(sign * .105f * scale, ankle + lift, z));
                SolveLimb(upper, lower, foot, target, root.forward);
                foot.rotation = footRotation;

                var arm = view.Bone(prefix + "Arm"); var elbow = view.Bone(prefix + "ForeArm"); var hand = view.Bone(prefix + "Hand");
                float armLength = Vector3.Distance(arm.position, elbow.position) + Vector3.Distance(elbow.position, hand.position);
                float swingAngle = moving ? Mathf.Sin((cycle + side * .5f) * Mathf.PI * 2) * (run ? 40 : 20) * envelope : Mathf.Sin(seconds * 1.6f) * .5f;
                var direction = Quaternion.AngleAxis(swingAngle, root.right) * (-root.up);
                var handTarget = arm.position + direction * armLength * .96f + root.right * sign * .035f * scale;
                var pole = root.forward + root.right * sign * .2f;
                if (run) handTarget = arm.position + direction * armLength * .65f + root.forward * .17f * scale;
                if (pose == CharacterPose.RaisedArms) { handTarget = arm.position + root.up * armLength * .91f + root.right * sign * .15f * scale; pole = -root.forward; }
                if (pose == CharacterPose.ElbowBend) handTarget = arm.position - root.up * .12f * scale + root.forward * .37f * scale;
                if (pose == CharacterPose.Crouch) handTarget += root.forward * .15f * scale;
                if (pose == CharacterPose.Reach && side == 1) handTarget = handleTarget ?? root.TransformPoint(new Vector3(.32f * scale, 1.08f, .48f * scale));
                SolveLimb(arm, elbow, hand, handTarget, pole);
                // A relaxed, articulated grasp; fingers remain individually addressable.
                foreach (string finger in new[] { "Index", "Middle", "Ring", "Pinky", "Thumb" })
                    for (int segment = 1; segment <= 3; segment++)
                    {
                        float curl = pose == CharacterPose.Reach && side == 1 ? (finger == "Index" ? 18 : 32) : 9;
                        view.Bone(prefix + "Hand" + finger + segment).Rotate(Vector3.right, curl, Space.Self);
                    }
            }
        }

        private static void RotateWorld(Transform bone, Vector3 axis, float angle) => bone.rotation = Quaternion.AngleAxis(angle, axis) * bone.rotation;

        public static void SolveLimb(Transform upper, Transform lower, Transform end, Vector3 target, Vector3 pole)
        {
            Vector3 start = upper.position;
            float a = Vector3.Distance(start, lower.position), b = Vector3.Distance(lower.position, end.position);
            Vector3 delta = target - start; float distance = Mathf.Clamp(delta.magnitude, Mathf.Abs(a - b) + .001f, a + b - .0005f);
            Vector3 forward = delta.sqrMagnitude > .000001f ? delta.normalized : Vector3.down;
            Vector3 bend = Vector3.ProjectOnPlane(pole, forward).normalized;
            if (bend.sqrMagnitude < .1f) bend = Vector3.Cross(forward, Vector3.right).normalized;
            float along = (a * a - b * b + distance * distance) / (2 * distance);
            Vector3 elbow = start + forward * along + bend * Mathf.Sqrt(Mathf.Max(0, a * a - along * along));
            upper.rotation = Quaternion.FromToRotation(lower.position - start, elbow - start) * upper.rotation;
            lower.rotation = Quaternion.FromToRotation(end.position - lower.position, target - lower.position) * lower.rotation;
        }
    }
}
