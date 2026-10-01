using NUnit.Framework;
using SilverScreen.Presentation.Interaction;
using UnityEngine;

namespace SilverScreen.Tests.EditMode
{
    public sealed class HeldPersonPresentationTests
    {
        [Test] public void PlaceholderIsVisualOnlyAndEveryReleaseRestoresRenderer()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            try
            {
                var position = new Vector3(2, 3, 4); go.transform.position = position;
                var originalMesh = go.GetComponent<MeshFilter>().sharedMesh;
                var renderer = go.GetComponent<MeshRenderer>();
                var presentation = go.AddComponent<HeldPersonPresentation>();
                presentation.BeginHeld();
                presentation.AdvancePresentation(.08f); presentation.AdvancePresentation(.08f);
                Assert.That(presentation.State, Is.EqualTo(HeldPersonPresentationState.Held));
                Assert.That(renderer.enabled, Is.False);
                Assert.That(Quaternion.Angle(presentation.Visual.localRotation, Quaternion.identity), Is.GreaterThan(5));
                Assert.That(go.transform.position, Is.EqualTo(position));
                Assert.That(go.transform.rotation, Is.EqualTo(Quaternion.identity));
                Assert.That(go.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(go.GetComponent<Collider>().enabled, Is.True);
                presentation.Release(PersonReleasePresentation.Contextual);
                Assert.That(presentation.State, Is.EqualTo(HeldPersonPresentationState.InteractionEntry));
                presentation.AdvancePresentation(.1f); presentation.AdvancePresentation(.1f);
                Assert.That(presentation.State, Is.EqualTo(HeldPersonPresentationState.Normal));
                Assert.That(renderer.enabled, Is.True);
                presentation.BeginHeld(); presentation.Release(PersonReleasePresentation.Ground);
                Assert.That(presentation.State, Is.EqualTo(HeldPersonPresentationState.GroundSettle));
                for (int i = 0; i < 4; i++) presentation.AdvancePresentation(.1f);
                Assert.That(renderer.enabled, Is.True);
                presentation.BeginHeld(); presentation.Release(PersonReleasePresentation.Cancel);
                Assert.That(presentation.State, Is.EqualTo(HeldPersonPresentationState.Normal));
                Assert.That(presentation.Visual.gameObject.activeSelf, Is.False);
                Assert.That(go.GetComponent<MeshFilter>().sharedMesh, Is.SameAs(originalMesh));
                renderer.enabled = false;
                presentation.BeginHeld(); presentation.enabled = false;
                Assert.That(renderer.enabled, Is.False, "Restore the prior enabled flag, not a guessed default.");
            }
            finally { Object.DestroyImmediate(go); }
        }
        [Test] public void MotionIsBoundedTrailsAndSettlesAtDifferentFrameRates()
        {
            Vector3 Run(int fps)
            {
                var go = new GameObject("Held motion proof");
                try
                {
                    var presentation = go.AddComponent<HeldPersonPresentation>(); presentation.BeginHeld();
                    float dt = 1f / fps;
                    for (int i = 0; i < fps; i++)
                    {
                        go.transform.position += Vector3.right * 6 * dt;
                        presentation.AdvancePresentation(dt);
                        Assert.That(presentation.SwayDegrees.magnitude, Is.LessThanOrEqualTo(16.001f));
                    }
                    var response = presentation.SwayDegrees;
                    Assert.That(response.z, Is.LessThan(-1), "Body trails horizontal travel.");
                    for (int i = 0; i < fps / 2; i++)
                    { go.transform.position -= Vector3.right * 6 * dt; presentation.AdvancePresentation(dt); }
                    Assert.That(presentation.SwayDegrees.z, Is.GreaterThan(0), "Direction change responds after damping.");
                    for (int i = 0; i < fps * 3; i++) presentation.AdvancePresentation(dt);
                    Assert.That(presentation.SwayDegrees.magnitude, Is.LessThan(.03f));
                    return response;
                }
                finally { Object.DestroyImmediate(go); }
            }
            Assert.That(Vector3.Distance(Run(30), Run(120)), Is.LessThan(.25f));
        }
    }
}
