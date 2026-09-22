using System;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.ReusableAssets;
using SilverScreen.Domain.Time;
using SilverScreen.Editor.ReusableAssets;
using SilverScreen.Presentation.ReusableAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public sealed class DisplayArtworkTests
    {
        [Test]
        public void IdentityYearRolloverAndRestoredClockUpdateOnlyTheirBoundContext()
        {
            var studio = new StudioIdentity("Aurora Pictures");
            var clock = new SimulationClock(1937, 12, 31, 23, 59);
            var context = new ArtworkContext();
            var rival = ReusableArtworkValidation.Context("Majestic Pictures", "1942", "A rival production");
            using (new StudioArtworkBinding(context, studio, clock))
            {
                Assert.That(context.Get(ArtworkContext.StudioName), Is.EqualTo("Aurora Pictures"));
                Assert.That(context.Get(ArtworkContext.CurrentYear), Is.EqualTo("1937"));
                studio.TryRename("Crescent Film Company"); clock.Advance(1);
                Assert.That(context.Get(ArtworkContext.StudioName), Is.EqualTo(studio.Name));
                Assert.That(context.Get(ArtworkContext.CurrentYear), Is.EqualTo("1938"));
                clock.RestoreState(new SimulationDateTime(1946, 1, 1, 8, 0), SimulationSpeed.Normal, SimulationSpeed.Normal);
                Assert.That(context.Get(ArtworkContext.CurrentYear), Is.EqualTo("1946"));
                Assert.That(rival.Get(ArtworkContext.StudioName), Is.EqualTo("Majestic Pictures"));
                Assert.That(rival.Get(ArtworkContext.CurrentYear), Is.EqualTo("1942"));
            }
            studio.TryRename("Detached"); clock.RestoreState(new SimulationDateTime(1955, 1, 1, 8, 0), SimulationSpeed.Normal, SimulationSpeed.Normal);
            Assert.That(context.Get(ArtworkContext.StudioName), Is.EqualTo("Crescent Film Company"));
            Assert.That(context.Get(ArtworkContext.CurrentYear), Is.EqualTo("1946"));
        }

        [Test]
        public void ArbitraryFieldsHaveNoImplicitPlayerOrYearDefaultsAndIgnoreUnchangedValues()
        {
            var context = new ArtworkContext(); int changes = 0; context.Changed += () => changes++;
            Assert.That(context.Get(ArtworkContext.StudioName), Is.Empty);
            Assert.That(context.Get(ArtworkContext.CurrentYear), Is.Empty);
            context.Set("screeningRoom", "The Blue Room"); context.Set("screeningRoom", "The Blue Room");
            context.Set(ArtworkContext.ProductionTitle, "A <literal> title");
            Assert.That(changes, Is.EqualTo(2));
            Assert.That(context.Get("screeningRoom"), Is.EqualTo("The Blue Room"));
            Assert.That(context.Get(ArtworkContext.ProductionTitle), Is.EqualTo("A <literal> title"));
            Assert.Throws<ArgumentException>(() => context.Set("", "invalid"));
        }

        [Test]
        public void RealRenderedPixelsChangeForNameYearAndTitleWhileSharedAssetsAndRivalStayUnchanged()
        {
            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ReusableFixtureBuilder.PrefabPath("DisplayFrame_Double_1930"));
                var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                var slots = obj.GetComponent<DisplayFrame>().Slots;
                var context = ReusableArtworkValidation.Context("Aurora Pictures", "1937", "THE SILVER LINING");
                var rivalContext = ReusableArtworkValidation.Context("Majestic Pictures", "1941", "NIGHT EXPRESS");
                var a = ReusableArtworkValidation.Attach(slots[0], "StudioNotice", InsertRole.StudioNotice, context);
                var b = ReusableArtworkValidation.Attach(slots[1], "StudioNotice", InsertRole.StudioNotice, rivalContext);
                var shared = slots[0].ArtworkRenderer.sharedMaterial;
                var materialJson = EditorJsonUtility.ToJson(shared);
                var template = AssetDatabase.LoadAssetAtPath<DisplayArtworkTemplate>(ReusableFixtureBuilder.Root + "/Artwork/StudioNotice.asset");
                var templateJson = EditorJsonUtility.ToJson(template);
                var fontJson = EditorJsonUtility.ToJson(template.Font);
                var rivalPixels = Pixels(b.RenderedArtwork);
                var before = Pixels(a.RenderedArtwork);
                // The dynamic title uses the same authored color as the fixed notice heading.
                // Catch accidental double conversion of MaterialPropertyBlock.SetColor.
                int titlePixels = before.Where((pixel, i) => i / a.RenderedArtwork.width >= 173 && i / a.RenderedArtwork.width < 243)
                    .Count(pixel => Math.Abs(pixel.r - 39) <= 1 && Math.Abs(pixel.g - 72) <= 1 && Math.Abs(pixel.b - 80) <= 1);
                Assert.That(titlePixels, Is.GreaterThan(500), "Composed text must retain the template's sRGB color.");
                context.Set(ArtworkContext.StudioName, "Crescent Film Company");
                var renamed = Pixels(a.RenderedArtwork);
                Assert.That(Difference(before, renamed), Is.GreaterThan(500), "Name must change visible pixels.");
                context.Set(ArtworkContext.CurrentYear, "1938");
                var nextYear = Pixels(a.RenderedArtwork);
                Assert.That(Difference(renamed, nextYear), Is.Zero, "The quiet notice deliberately has no year field.");
                context.Set(ArtworkContext.ProductionTitle, "MIDNIGHT ON THE COAST");
                var retitled = Pixels(a.RenderedArtwork);
                Assert.That(Difference(nextYear, retitled), Is.GreaterThan(500), "Production title must change visible pixels.");
                Assert.That(Difference(rivalPixels, Pixels(b.RenderedArtwork)), Is.Zero);
                Assert.That(slots[0].ArtworkRenderer.sharedMaterial, Is.SameAs(slots[1].ArtworkRenderer.sharedMaterial));
                Assert.That(EditorJsonUtility.ToJson(shared), Is.EqualTo(materialJson));
                Assert.That(EditorJsonUtility.ToJson(template), Is.EqualTo(templateJson));
                Assert.That(EditorJsonUtility.ToJson(template.Font), Is.EqualTo(fontJson));
                a.enabled = false;
                Assert.That(a.RenderedArtwork, Is.Null);
                context.Set(ArtworkContext.StudioName, "Pacific Pictures");
                Assert.That(a.RenderedArtwork, Is.Null, "Disabled displays must not compose.");
                a.enabled = true;
                Assert.That(Difference(retitled, Pixels(a.RenderedArtwork)), Is.GreaterThan(20));
                a.Initialize(rivalContext);
                var rebound = a.RenderedArtwork;
                context.Set(ArtworkContext.CurrentYear, "1960");
                Assert.That(a.RenderedArtwork, Is.SameAs(rebound), "Old context must be unsubscribed.");

                // The open-house poster demonstrates the date-bearing template on the same frame slot.
                a.Configure(slots[0], AssetDatabase.LoadAssetAtPath<DisplayArtworkTemplate>(ReusableFixtureBuilder.Root + "/Artwork/OpenHouse.asset"),
                    InsertRole.AdvertisingSignage, ArtworkFit.FitWithMat, null);
                a.Initialize(context);
                var posterBefore = Pixels(a.RenderedArtwork);
                context.Set(ArtworkContext.CurrentYear, "1961");
                Assert.That(Difference(posterBefore, Pixels(a.RenderedArtwork)), Is.GreaterThan(20), "Poster year must change visible pixels.");
            }
            finally { EditorSceneManager.ClosePreviewScene(scene); }
        }

        [Test]
        public void HistoricalLiteralDateStaysFixedAndFutureTemplateFieldRenders()
        {
            var original = AssetDatabase.LoadAssetAtPath<DisplayArtworkTemplate>(ReusableFixtureBuilder.Root + "/Artwork/OpenHouse.asset");
            var template = Object.Instantiate(original);
            RenderTexture first = null, second = null, third = null;
            try
            {
                template.Fields = new[] {
                    new ArtworkTextField { Literal = "1924", Rect = new Rect(60, 40, 480, 65), FontSize = 30 },
                    new ArtworkTextField { Key = "screeningRoom", Rect = new Rect(45, 735, 510, 105), FontSize = 42 } };
                var context = new ArtworkContext(); context.Set("screeningRoom", "The Blue Room");
                first = DisplayArtworkComposer.Compose(template, context);
                context.Set(ArtworkContext.CurrentYear, "1955");
                second = DisplayArtworkComposer.Compose(template, context);
                Assert.That(Difference(Pixels(first), Pixels(second)), Is.Zero, "Historical literal must not follow game year.");
                context.Set("screeningRoom", "Main Theatre");
                third = DisplayArtworkComposer.Compose(template, context);
                Assert.That(Difference(Pixels(second), Pixels(third)), Is.GreaterThan(500));
            }
            finally
            {
                foreach (var rt in new[] { first, second, third }) if (rt != null) { rt.Release(); Object.DestroyImmediate(rt); }
                Object.DestroyImmediate(template);
            }
        }
        private static int Difference(Color32[] a, Color32[] b) => a.Zip(b, (x, y) => x.Equals(y) ? 0 : 1).Sum();
        private static Color32[] Pixels(RenderTexture source)
        {
            var old = RenderTexture.active; var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false);
            try { RenderTexture.active = source; texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); texture.Apply(); return texture.GetPixels32(); }
            finally { RenderTexture.active = old; Object.DestroyImmediate(texture); }
        }
    }
}
