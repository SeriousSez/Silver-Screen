using System;
using NUnit.Framework;
using SilverScreen.Domain.Characters;

namespace SilverScreen.Tests.EditMode
{
    public sealed class ModularAppearanceTests
    {
        [TestCase(false, 0f)]
        [TestCase(true, 0f)]
        [TestCase(true, .7f)]
        public void OptionalGreyOverrideSurvivesUnitySerialization(bool present, float value)
        {
            var appearance = new ProductionAppearance
            {
                productionId = "film", personId = "canonical",
                greyFractionOverride = present ? value : (float?)null
            };
            var restored = UnityEngine.JsonUtility.FromJson<ProductionAppearance>(
                UnityEngine.JsonUtility.ToJson(appearance));
            Assert.That(restored.greyFractionOverride, Is.EqualTo(appearance.greyFractionOverride));
            restored.greyFractionOverride = null;
            Assert.That(restored.hasGreyFractionOverride, Is.False);
            Assert.That(restored.greyFractionOverride, Is.Null);
        }

        private static AppearanceState Person()=>new AppearanceState
        {
            identity=new VisualIdentity{personId="canonical",foundationId="authored-canonical",faceIdentityId="canonical-face",rigVersion="canonical-authored-v0",
                measurements=new PhysicalMeasurements{barefootHeightMetres=1.73f,eyeHeightMetres=1.61f,shoulderWidthMetres=.43f,footLengthMetres=.26f},anatomicalControls=new[]{.12f}},
            personalStyle=new PersonalStyle{hairstyleId="side-part",facialHairId="stubble",eyebrowsId="brow-canonical",outfit=new OutfitSelection{items=new[]{
                new WardrobeItem{slot=WardrobeSlot.Shirt,assetId="shirt"},new WardrobeItem{slot=WardrobeSlot.Waistcoat,assetId="waistcoat"},
                new WardrobeItem{slot=WardrobeSlot.Footwear,assetId="oxfords"}}}},revision=4
        };

        [Test] public void CostumeOverridesDoNotChangePersonalStyleOrIdentity()
        {
            var person=Person();var original=AppearanceResolver.Resolve(person);
            var costume=new ProductionAppearance{productionId="movie-a",personId="canonical",hairstyleOverride="",costumeOverrides=new OutfitSelection{items=new[]{
                new WardrobeItem{slot=WardrobeSlot.Headwear,assetId="fedora"},new WardrobeItem{slot=WardrobeSlot.Waistcoat,assetId=""}}}};
            var filmed=AppearanceResolver.Resolve(person,costume);
            Assert.That(filmed.Style.hairstyleId,Is.Empty);Assert.That(filmed.Style.outfit.Get(WardrobeSlot.Headwear),Is.EqualTo("fedora"));
            Assert.That(filmed.Style.outfit.Get(WardrobeSlot.Waistcoat),Is.Empty);Assert.That(filmed.Style.outfit.Get(WardrobeSlot.Shirt),Is.EqualTo("shirt"));
            Assert.That(filmed.Identity.faceIdentityId,Is.EqualTo(original.Identity.faceIdentityId));
            Assert.That(filmed.Identity.anatomicalControls,Is.EqualTo(original.Identity.anatomicalControls));
            var restored=AppearanceResolver.Resolve(person);
            Assert.That(restored.Style.hairstyleId,Is.EqualTo("side-part"));Assert.That(restored.Style.outfit.Get(WardrobeSlot.Waistcoat),Is.EqualTo("waistcoat"));
        }

        [Test] public void HistoricalMoviesRetainIndependentSnapshotsAfterPersonalChanges()
        {
            var person=Person();var movieA=AppearanceResolver.Resolve(person,new ProductionAppearance{productionId="a",personId="canonical"});
            person.personalStyle.hairstyleId="later-style";person.personalStyle.outfit.items[2].assetId="later-shoes";person.revision++;
            var movieB=AppearanceResolver.Resolve(person,new ProductionAppearance{productionId="b",personId="canonical"});
            Assert.That(movieA.Style.hairstyleId,Is.EqualTo("side-part"));Assert.That(movieA.Style.outfit.Get(WardrobeSlot.Footwear),Is.EqualTo("oxfords"));
            Assert.That(movieB.Style.hairstyleId,Is.EqualTo("later-style"));Assert.That(movieB.SourceRevision,Is.EqualTo(5));
            Assert.That(movieA.Identity.personId,Is.EqualTo(movieB.Identity.personId));
        }

        [Test] public void SnapshotCannotBeMutatedThroughReturnedNestedData()
        {
            var person=Person();var snapshot=AppearanceResolver.Resolve(person);
            snapshot.Identity.anatomicalControls[0]=.9f;snapshot.Identity.measurements.barefootHeightMetres=2;
            snapshot.Style.outfit.items[0].assetId="changed";person.identity.anatomicalControls[0]=.5f;
            Assert.That(snapshot.Identity.anatomicalControls[0],Is.EqualTo(.12f));
            Assert.That(snapshot.Identity.measurements.barefootHeightMetres,Is.EqualTo(1.73f));Assert.That(snapshot.Style.outfit.Get(WardrobeSlot.Shirt),Is.EqualTo("shirt"));
        }

        [Test] public void RejectsCostumeForAnotherPerson()=>Assert.Throws<ArgumentException>(()=>AppearanceResolver.Resolve(Person(),new ProductionAppearance{productionId="film",personId="someone-else"}));
        [Test] public void RejectsAmbiguousDuplicateSlots()
        {
            var person=Person();person.personalStyle.outfit.items=new[]{new WardrobeItem{slot=WardrobeSlot.Shirt,assetId="a"},new WardrobeItem{slot=WardrobeSlot.Shirt,assetId="b"}};
            Assert.Throws<ArgumentException>(()=>AppearanceResolver.Resolve(person));
        }
        [Test] public void ShoesAndGroomingDoNotChangeAnatomicalMeasurements()
        {
            var person=Person();var before=AppearanceResolver.Resolve(person);
            var after=AppearanceResolver.Resolve(person,new ProductionAppearance{personId="canonical",productionId="western",hairColorOverride="#888888",greyFractionOverride=.7f,
                facialHairOverride="",costumeOverrides=new OutfitSelection{items=new[]{new WardrobeItem{slot=WardrobeSlot.Footwear,assetId="boots"}}}});
            Assert.That(after.Identity.measurements.barefootHeightMetres,Is.EqualTo(before.Identity.measurements.barefootHeightMetres));
            Assert.That(after.Style.outfit.Get(WardrobeSlot.Footwear),Is.EqualTo("boots"));Assert.That(before.Style.greyFraction,Is.Zero);
        }
    }
}
