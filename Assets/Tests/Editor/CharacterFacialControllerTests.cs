using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain.Characters;
using SilverScreen.Presentation.Characters;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class CharacterFacialControllerTests
    {
        private Scene _scene; private readonly List<Object> _owned = new List<Object>();
        [SetUp] public void Setup() { _scene = EditorSceneManager.NewPreviewScene(); }
        [TearDown] public void Teardown() { for (int i = _owned.Count - 1; i >= 0; i--) if (_owned[i] != null) Object.DestroyImmediate(_owned[i]); _owned.Clear(); EditorSceneManager.ClosePreviewScene(_scene); }
        internal static Mesh MeshWith(params string[] channels)
        {
            var mesh = new Mesh { vertices = new[] { Vector3.zero, Vector3.right, Vector3.up }, triangles = new[] { 0, 1, 2 } };
            mesh.RecalculateNormals();
            foreach (var channel in channels) mesh.AddBlendShapeFrame(channel, 100, new[] { Vector3.forward * .1f, Vector3.zero, Vector3.zero }, new Vector3[3], new Vector3[3]);
            return mesh;
        }
        private SkinnedMeshRenderer Face(params string[] channels)
        {
            var go = new GameObject("Synthetic face"); SceneManager.MoveGameObjectToScene(go, _scene); _owned.Add(go);
            var face = go.AddComponent<SkinnedMeshRenderer>(); face.sharedMesh = MeshWith(channels); _owned.Add(face.sharedMesh); return face;
        }
        [TestCase(false)] [TestCase(true)] public void ExactAliasesWorkRegardlessOfRawOrder(bool reverse)
        {
            var names = new[] { "mothShrugtLower", "SmileRaw", "BlinkBoth" }; if (reverse) Array.Reverse(names);
            var face = Face(names); var controller = new CharacterFacialController(face, new CharacterFacialBinding(
                new FacialBindingEntry(FacialSemantic.MouthShrugLower, "mothShrugtLower"), new FacialBindingEntry(FacialSemantic.SmileLeft, "SmileRaw")));
            Assert.That(controller.TrySet(FacialSemantic.MouthShrugLower, .25f)); Assert.That(controller.TrySet(FacialSemantic.SmileLeft, .75f));
            Assert.That(face.GetBlendShapeWeight(Array.IndexOf(names, "mothShrugtLower")), Is.EqualTo(25));
            Assert.That(face.GetBlendShapeWeight(Array.IndexOf(names, "SmileRaw")), Is.EqualTo(75));
            controller.Reset(FacialSemantic.MouthShrugLower); Assert.That(face.GetBlendShapeWeight(Array.IndexOf(names, "SmileRaw")), Is.EqualTo(75));
            controller.ResetAll(); Assert.That(face.GetBlendShapeWeight(Array.IndexOf(names, "SmileRaw")), Is.Zero);
        }
        [Test] public void FiniteClampedRemappingAndMissingRejectedAreSafe()
        {
            var face = Face("raw", "rejected"); var controller = new CharacterFacialController(face, new CharacterFacialBinding(
                new FacialBindingEntry(FacialSemantic.BlinkBoth, "raw", normalizedMinimum: .1f, normalizedMaximum: .9f),
                new FacialBindingEntry(FacialSemantic.JawOpen, "absent"), new FacialBindingEntry(FacialSemantic.DimpleLeft, "rejected", FacialCapability.Rejected, "Unsafe source")));
            Assert.That(controller.TrySet(FacialSemantic.BlinkBoth, -2)); Assert.That(face.GetBlendShapeWeight(0), Is.EqualTo(10));
            controller.TrySet(FacialSemantic.BlinkBoth, .5f); Assert.That(face.GetBlendShapeWeight(0), Is.EqualTo(50));
            controller.TrySet(FacialSemantic.BlinkBoth, 2); Assert.That(face.GetBlendShapeWeight(0), Is.EqualTo(90));
            foreach (float invalid in new[] { float.NaN, float.NegativeInfinity, float.PositiveInfinity }) Assert.That(controller.TrySet(FacialSemantic.BlinkBoth, invalid), Is.False);
            Assert.That(face.GetBlendShapeWeight(0), Is.EqualTo(90));
            Assert.That(controller.TrySet(FacialSemantic.DimpleLeft, 1), Is.False); Assert.That(face.GetBlendShapeWeight(1), Is.Zero);
            Assert.That(controller.Capability(FacialSemantic.JawOpen), Is.EqualTo(FacialCapability.Missing));
            Assert.That(controller.Supports((FacialSemantic)1000), Is.False); Assert.That(controller.TrySet(FacialSemantic.JawOpen, 1), Is.False);
        }
        [Test] public void BindingIsCachedAndTwoRenderersDoNotShareWeightsOrMutateMesh()
        {
            var first = Face("raw"); var second = Face("raw"); var unused = second.sharedMesh; second.sharedMesh = first.sharedMesh;
            var entry = new FacialBindingEntry(FacialSemantic.BlinkBoth, "raw"); var binding = new CharacterFacialBinding(entry);
            var a = new CharacterFacialController(first, binding); var b = new CharacterFacialController(second, binding);
            var originalVertices = first.sharedMesh.vertices; var mesh = first.sharedMesh;
            for (int i = 0; i < 1000; i++) Assert.That(a.TrySet(FacialSemantic.BlinkBoth, .5f));
            Assert.That(second.GetBlendShapeWeight(0), Is.Zero); Assert.That(first.sharedMesh, Is.SameAs(mesh));
            Assert.That(mesh.vertices, Is.EqualTo(originalVertices)); Assert.That(mesh.blendShapeCount, Is.EqualTo(1));
            first.sharedMesh = unused; Assert.That(a.TrySet(FacialSemantic.BlinkBoth, 1), Is.False, "Changing mesh requires explicit rebind; cached indices never target another mesh.");
            Assert.That(b.TrySet(FacialSemantic.BlinkBoth, .25f)); Assert.That(second.GetBlendShapeWeight(0), Is.EqualTo(25));
        }
        [Test] public void AllSixFemaleRejectedChannelsRemainUndrivenWhileSafeChannelsCoexist()
        {
            var binding = CharacterPhysicalProfileTests.Definition(CharacterFamily.AdultFemale).FacialBinding;
            var entries = binding.Entries; var face = Face(entries.Select(e => e.RawChannel).Reverse().ToArray());
            var controller = new CharacterFacialController(face, binding);
            Assert.That(entries.Count(e => e.Capability == FacialCapability.Rejected), Is.EqualTo(6));
            foreach (var entry in entries)
            {
                bool supported = entry.Capability == FacialCapability.Supported;
                Assert.That(controller.Supports(entry.Semantic), Is.EqualTo(supported)); Assert.That(controller.TrySet(entry.Semantic, .5f), Is.EqualTo(supported));
                Assert.That(face.GetBlendShapeWeight(face.sharedMesh.GetBlendShapeIndex(entry.RawChannel)), Is.EqualTo(supported ? 50 : 0));
            }
            Assert.That(controller.Supports(FacialSemantic.DimpleRight)); Assert.That(controller.Supports(FacialSemantic.DimpleLeft), Is.False);
            controller.ResetAll(); foreach (var e in entries) Assert.That(face.GetBlendShapeWeight(face.sharedMesh.GetBlendShapeIndex(e.RawChannel)), Is.Zero);
        }
        [Test] public void InvalidDuplicateBindingsFailBeforeDriving()
        {
            var entry = new FacialBindingEntry(FacialSemantic.BlinkBoth, "raw");
            Assert.That(new CharacterFacialBinding(entry, entry).TryValidate(out _), Is.False);
            Assert.That(new CharacterFacialBinding(entry, new FacialBindingEntry(FacialSemantic.JawOpen, "raw")).TryValidate(out _), Is.False);
            Assert.That(new CharacterFacialBinding(new FacialBindingEntry(FacialSemantic.BlinkBoth, "raw", normalizedMaximum: float.NaN)).TryValidate(out _), Is.False);
        }
        [Test] public void RepeatedSemanticWeightsDoNotAllocateManagedMemory()
        {
            var face = Face("raw"); var controller = new CharacterFacialController(face,
                new CharacterFacialBinding(new FacialBindingEntry(FacialSemantic.BlinkBoth, "raw")));
            for (int i = 0; i < 100; i++) controller.TrySet(FacialSemantic.BlinkBoth, .5f);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) controller.TrySet(FacialSemantic.BlinkBoth, .5f);
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(bytes, Is.Zero, "Binding allocations belong to construction, not facial weight updates.");
        }
    }
}
