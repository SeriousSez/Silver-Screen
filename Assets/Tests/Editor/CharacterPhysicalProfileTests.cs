using System;
using NUnit.Framework;
using SilverScreen.Domain.Characters;
using SilverScreen.Presentation.Characters;
using UnityEditor;
using UnityEngine;

namespace SilverScreen.Tests.EditMode
{
    public sealed class CharacterPhysicalProfileTests
    {
        internal static CharacterFamilyDefinition Definition(CharacterFamily family) => AssetDatabase.LoadAssetAtPath<CharacterFamilyDefinition>(
            "Assets/SilverScreen/Characters/Definitions/" + family + ".asset");
        internal static CharacterPhysicalProfile Small(float height = 1.4f, float radius = .2f, float offset = .7f) => new CharacterPhysicalProfile(
            height, height, radius, offset, height, radius, new Vector3(0, 1.15f, -.05f), new Vector3(0, height + .08f, 0));
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)] [TestCase(0)] [TestCase(-1)]
        public void InvalidDimensionsFail(float value) => Assert.Throws<ArgumentException>(() => Small(value));
        [Test] public void EveryFiniteGuardAndBakeEnvelopeIsValidated()
        {
            Assert.That(default(CharacterPhysicalProfile).TryValidate(out _), Is.False);
            Assert.Throws<ArgumentException>(() => Small(.3f, .2f, .1f));
            Assert.Throws<ArgumentException>(() => Small(2.1f)); Assert.Throws<ArgumentException>(() => Small(1.4f, .36f));
            Assert.Throws<ArgumentException>(() => Small(offset: float.NaN));
            Assert.Throws<ArgumentException>(() => new CharacterPhysicalProfile(1, 1, .2f, .5f, 1, .2f, Vector3.zero, new Vector3(float.PositiveInfinity, 0, 0)));
            Assert.Throws<ArgumentException>(() => new CharacterPhysicalProfile(1, 1, .2f, .5f, 1, .2f, new Vector3(0, float.NaN, 0), Vector3.zero));
            Assert.Throws<ArgumentException>(() => new CharacterPhysicalProfile(1, 1, .2f, .5f, 1, .2f, Vector3.zero, Vector3.zero, carryLift: float.NaN));
            Assert.Throws<ArgumentException>(() => new CharacterPhysicalProfile(1, 1, .2f, .5f, 1, .2f, Vector3.zero, Vector3.zero, capsuleCenter: Vector3.up));
            Assert.Throws<ArgumentException>(() => new CharacterPhysicalProfile(1, 1, .2f, .5f, .3f, .2f, Vector3.zero, Vector3.zero));
        }
        [Test] public void NonzeroGroundAndNoncentralOffsetHaveConsistentGeometry()
        {
            var p = Small(offset: .6f); var ground = new Vector3(7, 3, -2); var root = ground + Vector3.up * .6f;
            Assert.That(p.FeetPoint(root), Is.EqualTo(ground));
            Assert.That((root + p.VisualLocalOrigin - ground).magnitude, Is.LessThan(.00001f));
            Assert.That(root.y + p.CapsuleCenter.y - p.CapsuleHeight / 2, Is.EqualTo(3).Within(.00001));
            Assert.That(root.y + p.SelectionLocalPosition.y, Is.EqualTo(3.02f).Within(.00001));
        }
        [TestCase(CharacterFamily.AdultFemale, 1.6753788f, .2161468f)]
        [TestCase(CharacterFamily.AdultMale, 1.84371471f, .223617554f)]
        [TestCase(CharacterFamily.Boy, 1.46625018f, .176805764f)]
        [TestCase(CharacterFamily.Girl, 1.46076977f, .181600973f)]
        public void FourIndependentMeasuredDefaults(CharacterFamily family, float height, float radius)
        {
            var d = Definition(family); Assert.That(d, Is.Not.Null); Assert.That(d.TryValidate(out var reason), Is.True, reason);
            var profile = d.DefaultPhysicalProfile;
            Assert.That(profile.BodyHeight, Is.EqualTo(height).Within(.000001)); Assert.That(profile.NavigationHeight, Is.EqualTo(height));
            Assert.That(profile.NavigationRadius, Is.EqualTo(radius).Within(.000001));
            Assert.That(profile.BaseOffset, Is.EqualTo(height / 2).Within(.000001));
            Assert.That(profile.CapsuleCenter, Is.EqualTo(Vector3.zero));
            Assert.That(CharacterPhysicalProfile.Legacy.NavigationHeight, Is.EqualTo(2));
        }
    }
}
