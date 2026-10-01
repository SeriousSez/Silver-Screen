using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Performance;

namespace SilverScreen.Tests.EditMode
{
    public sealed class SceneTemplateTests
    {
        private static PerformerBinding[] Bindings() => new[]
        {
            new PerformerBinding(HeatedArgumentTemplate.Aggressor, "placeholder-a"),
            new PerformerBinding(HeatedArgumentTemplate.Defender, "placeholder-b")
        };

        [Test]
        public void IdentityAndRolesAreStableAcrossIndependentDefinitions()
        {
            var a = HeatedArgumentTemplate.Create();
            var b = HeatedArgumentTemplate.Create();
            Assert.That(a, Is.Not.SameAs(b));
            Assert.That(a.Id, Is.EqualTo(HeatedArgumentTemplate.TemplateId));
            Assert.That(a.Id, Is.EqualTo(b.Id));
            Assert.That(a.Roles.Select(role => role.Id), Is.EqualTo(new[] { "aggressor", "defender" }));
            Assert.That(a.Roles.Select(role => role.Name), Is.EqualTo(new[] { "Aggressor", "Defender" }));
            Assert.That(a.Tags, Is.EqualTo(new[] { "conversation", "conflict" }));
        }

        [TestCase(SceneDuration.Short, "confront,respond,exit")]
        [TestCase(SceneDuration.Medium, "approach,confront,respond,escalate,exit")]
        [TestCase(SceneDuration.Long, "approach,confront,respond,react,second-exchange,escalate,final-reaction,exit")]
        public void DurationSelectsAuthoredOrderedBeats(SceneDuration duration, string expected)
        {
            var template = HeatedArgumentTemplate.Create();
            var performance = new ScenePerformance(template, duration, Bindings());
            Assert.That(performance.Beats.Select(beat => beat.Definition.Id), Is.EqualTo(expected.Split(',')));
            var confront = performance.Beats.Single(beat => beat.Definition.Id == "confront");
            Assert.That(confront.PerformerId, Is.EqualTo("placeholder-a"));
            Assert.That(confront.TargetPerformerId, Is.EqualTo("placeholder-b"));
            Assert.That(confront.Definition, Is.SameAs(template.Beats.Single(beat => beat.Id == "confront")));
        }

        [Test]
        public void MetadataAndRequiredOptionalSemanticsSurviveResolution()
        {
            var performance = new ScenePerformance(HeatedArgumentTemplate.Create(), SceneDuration.Long, Bindings());
            Assert.That(performance.Template.Requirements.Single(item => item.Required).SemanticId, Is.EqualTo("standing-space-for-two"));
            Assert.That(performance.Template.Requirements.Single(item => !item.Required).Kind, Is.EqualTo(SceneRequirementKind.Prop));
            Assert.That(performance.Template.Anchors.Single(item => item.Id == "standing-a").RequirementId, Is.EqualTo("playing-space"));
            var shoulder = performance.Beats.Single(item => item.Definition.Id == "second-exchange").Definition.Camera;
            Assert.That(shoulder.Framing, Is.EqualTo(CameraFraming.OverShoulder));
            Assert.That(shoulder.SubjectRoleId, Is.EqualTo("aggressor"));
            Assert.That(shoulder.OtherRoleId, Is.EqualTo("defender"));
        }

        [TestCase("missing")]
        [TestCase("extra")]
        [TestCase("duplicate-role")]
        [TestCase("duplicate-performer")]
        public void InvalidBindingsFailPredictably(string invalid)
        {
            var bindings = Bindings().ToList();
            switch (invalid)
            {
                case "missing": bindings.RemoveAt(1); break;
                case "extra": bindings.Add(new PerformerBinding("unknown", "third")); break;
                case "duplicate-role": bindings[1] = new PerformerBinding("aggressor", "third"); break;
                case "duplicate-performer": bindings[1] = new PerformerBinding("defender", "placeholder-a"); break;
            }
            Assert.Throws<ArgumentException>(() => new ScenePerformance(HeatedArgumentTemplate.Create(), SceneDuration.Short, bindings));
        }

        [Test]
        public void CapabilityRequirementsAreCheckedWithoutKnowingActorTypes()
        {
            var source = HeatedArgumentTemplate.Create();
            var roles = new[] { new PerformerRoleSlot("aggressor", "Aggressor", new[] { "standing" }), source.Roles[1] };
            var template = Copy(source, roles: roles);
            Assert.Throws<ArgumentException>(() => new ScenePerformance(template, SceneDuration.Short, Bindings()));
            var bindings = Bindings();
            bindings[0] = new PerformerBinding("aggressor", "a-capsule", new[] { "standing" });
            Assert.That(new ScenePerformance(template, SceneDuration.Short, bindings).Beats[0].PerformerId, Is.EqualTo("a-capsule"));
        }

        [Test]
        public void CallerCollectionsCannotMutateDefinitionsOrResolvedPerformance()
        {
            var source = HeatedArgumentTemplate.Create();
            var roles = source.Roles.ToArray();
            var beats = source.Beats.ToArray();
            var variants = source.Variants.ToArray();
            var requirements = source.Requirements.ToArray();
            var anchors = source.Anchors.ToArray();
            var template = new SceneTemplate(source.Id, source.Name, source.Description, roles, beats, variants,
                requirements: requirements, anchors: anchors);
            var bindings = Bindings();
            var performance = new ScenePerformance(template, SceneDuration.Short, bindings);
            roles[0] = null; beats[0] = null; variants[0] = null; requirements[0] = null; anchors[0] = null; bindings[0] = null;
            Assert.That(template.Roles[0], Is.Not.Null);
            Assert.That(template.Beats[0], Is.Not.Null);
            Assert.That(template.Requirements[0], Is.Not.Null);
            Assert.That(template.Anchors[0], Is.Not.Null);
            Assert.That(performance.Bindings[0].PerformerId, Is.EqualTo("placeholder-a"));
            Assert.Throws<NotSupportedException>(() => ((IList<PerformanceBeat>)template.Beats).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<string>)template.Variants[0].BeatIds).Clear());
            Assert.Throws<NotSupportedException>(() => ((IList<ResolvedPerformanceBeat>)performance.Beats).Clear());
        }

        [Test]
        public void NestedInputListsAreDefensivelyCopied()
        {
            var ids = new[] { "confront", "respond", "exit" };
            var variant = new SceneTemplateVariant(SceneDuration.Short, ids);
            var capabilities = new[] { "standing" };
            var role = new PerformerRoleSlot("role", "Role", capabilities);
            var binding = new PerformerBinding("role", "person", capabilities);
            ids[0] = "changed";
            capabilities[0] = "changed";
            Assert.That(variant.BeatIds[0], Is.EqualTo("confront"));
            Assert.That(role.RequiredCapabilities[0], Is.EqualTo("standing"));
            Assert.That(binding.Capabilities[0], Is.EqualTo("standing"));
        }

        [TestCase("duplicate-role")]
        [TestCase("duplicate-beat")]
        [TestCase("missing-variant")]
        [TestCase("duplicate-variant")]
        [TestCase("unknown-beat")]
        [TestCase("unknown-role")]
        [TestCase("unknown-anchor")]
        [TestCase("unknown-camera-role")]
        [TestCase("unknown-requirement")]
        [TestCase("duplicate-anchor")]
        [TestCase("duplicate-requirement")]
        [TestCase("null-beat")]
        public void InvalidDefinitionReferencesFailAtConstruction(string invalid)
        {
            var source = HeatedArgumentTemplate.Create();
            var roles = source.Roles.ToList();
            var beats = source.Beats.ToList();
            var variants = source.Variants.ToList();
            var anchors = source.Anchors.ToList();
            var requirements = source.Requirements.ToList();
            switch (invalid)
            {
                case "duplicate-role": roles.Add(roles[0]); break;
                case "duplicate-beat": beats.Add(beats[0]); break;
                case "missing-variant": variants.RemoveAt(0); break;
                case "duplicate-variant": variants[1] = variants[0]; break;
                case "unknown-beat": variants[0] = new SceneTemplateVariant(SceneDuration.Short, new[] { "missing" }); break;
                case "unknown-role": beats[0] = new PerformanceBeat("approach", PerformanceAction.Approach, "Move", "missing"); break;
                case "unknown-anchor": beats[0] = new PerformanceBeat("approach", PerformanceAction.Approach, "Move", "aggressor", anchorId: "missing"); break;
                case "unknown-camera-role": beats[0] = new PerformanceBeat("approach", PerformanceAction.Approach, "Move", "aggressor", camera: new CameraSuggestion(CameraFraming.CloseUp, "missing")); break;
                case "unknown-requirement": anchors[0] = new InteractionAnchor("standing-a", "seat", "missing"); break;
                case "duplicate-anchor": anchors.Add(anchors[0]); break;
                case "duplicate-requirement": requirements.Add(requirements[0]); break;
                case "null-beat": beats[0] = null; break;
            }
            Assert.Throws<ArgumentException>(() => new SceneTemplate(source.Id, source.Name, source.Description,
                roles, beats, variants, requirements: requirements, anchors: anchors));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        public void BlankIdentityIsRejectedInsteadOfRandomized(string id)
        {
            var template = HeatedArgumentTemplate.Create();
            Assert.Throws<ArgumentException>(() => new SceneTemplate(id, template.Name, template.Description,
                template.Roles, template.Beats, template.Variants, requirements: template.Requirements, anchors: template.Anchors));
        }

        [Test]
        public void EmptySequencesAndUnknownEnumValuesAreRejected()
        {
            Assert.Throws<ArgumentException>(() => new SceneTemplateVariant(SceneDuration.Short, Array.Empty<string>()));
            Assert.Throws<ArgumentOutOfRangeException>(() => HeatedArgumentTemplate.Create().ResolveBeats((SceneDuration)99));
            Assert.Throws<ArgumentOutOfRangeException>(() => new PerformanceBeat("beat", (PerformanceAction)99, "Action", "role"));
            Assert.Throws<ArgumentException>(() => new CameraSuggestion(CameraFraming.CloseUp));
            Assert.Throws<ArgumentException>(() => new CameraSuggestion(CameraFraming.OverShoulder, "same", "same"));
        }

        [TestCase(SceneDuration.Short, 3)]
        [TestCase(SceneDuration.Medium, 5)]
        [TestCase(SceneDuration.Long, 8)]
        public void PlaceholderExecutionVisitsEveryBeatOnceAndCanReset(SceneDuration duration, int expected)
        {
            var performance = new ScenePerformance(HeatedArgumentTemplate.Create(), duration, Bindings());
            var cursor = new SceneSequenceCursor(performance);
            var visited = new List<string>();
            while (!cursor.IsComplete)
            {
                visited.Add(cursor.Current.Definition.Id);
                Assert.That(cursor.Current.PerformerId, Is.EqualTo("placeholder-a").Or.EqualTo("placeholder-b"));
                Assert.That(cursor.Advance(), Is.True);
            }
            Assert.That(visited.Count, Is.EqualTo(expected));
            Assert.That(visited, Is.EqualTo(performance.Beats.Select(beat => beat.Definition.Id)));
            Assert.That(cursor.Current, Is.Null);
            Assert.That(cursor.Advance(), Is.False);
            cursor.Reset();
            Assert.That(cursor.Current, Is.SameAs(performance.Beats[0]));
            Assert.That(new SceneSequenceCursor(performance).Index, Is.Zero);
        }

        private static SceneTemplate Copy(SceneTemplate source, IEnumerable<PerformerRoleSlot> roles) =>
            new SceneTemplate(source.Id, source.Name, source.Description, roles, source.Beats, source.Variants,
                source.Tags, source.Requirements, source.Anchors);
    }
}
