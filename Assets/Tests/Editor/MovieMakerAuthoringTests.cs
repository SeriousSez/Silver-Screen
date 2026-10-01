using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Performance;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Writing;

namespace SilverScreen.Tests.EditMode
{
    public sealed class MovieMakerAuthoringTests
    {
        private static ScreenplayProject Example() => MovieMakerAuthoringExample.Create();
        private static ScreenplaySceneAuthoring Plan(ScreenplayProject project) => project.Scenes[0].Authoring;
        private static MovieProject Adapt(ScreenplayProject project)
        {
            project.Authoring.Submit();
            var result = new ScreenplayProductionAdapter().Adapt(project, new SimulationDateTime(1930,1,1,8,0), "Drama");
            Assert.That(result.Succeeded, Is.True, result.Message); return result.Movie;
        }
        private static SceneTemplate TemplateWith(IEnumerable<PerformerRoleSlot> roles = null,
            IEnumerable<SceneRequirement> requirements = null, IEnumerable<PerformanceBeat> beats = null,
            IEnumerable<SceneTemplateVariant> variants = null)
        {
            var source = HeatedArgumentTemplate.Create();
            return new SceneTemplate("test-template", source.Name, source.Description, roles ?? source.Roles,
                beats ?? source.Beats, variants ?? source.Variants, requirements: requirements ?? source.Requirements,
                anchors: source.Anchors);
        }
        private static ScreenplaySceneAuthoring MakePlan(SceneTemplate template = null, IEnumerable<CharacterRoleBinding> bindings = null,
            IEnumerable<PropIntent> props = null, IEnumerable<AuthoredDialogue> dialogue = null,
            IEnumerable<BackgroundGroup> groups = null, IEnumerable<CostumeIntent> costumes = null)
            => new ScreenplaySceneAuthoring(template ?? HeatedArgumentTemplate.Create(), SceneDuration.Medium,
                bindings ?? new[] { new CharacterRoleBinding("aggressor","hale"), new CharacterRoleBinding("defender","hart") },
                new SceneSetIntent("office"), props: props, dialogue: dialogue, backgroundGroups: groups, costumes: costumes);

        [Test]
        public void PlayerDraftOwnsOneScreenplayAndHasNoInventedWriterEvaluation()
        {
            var project = Example();
            Assert.That(project.AcquisitionSource, Is.EqualTo(ScreenplayAcquisitionSource.PlayerCreated));
            Assert.That(project.Contributors, Is.Empty); Assert.That(project.Evaluation, Is.Null);
            project.Authoring.Submit();
            Assert.That(project.Status, Is.EqualTo(ScreenplayStatus.Completed));
            Assert.That(project.ContentStatus, Is.EqualTo(ScreenplayContentStatus.Ready));
            Assert.That(project.Evaluation, Is.Null);
        }
        [Test]
        public void CommissionedWritingGateIsPreserved()
        {
            Assert.Throws<ArgumentException>(() => new ScreenplayProject("id","title",Array.Empty<string>(),new[]{"drama"}));
            var screenplay = new ScreenplayProject("id","title",new[]{"writer"},new[]{"drama"});
            Assert.That(screenplay.Authoring, Is.Null);
            Assert.That(new ScreenplayProductionAdapter().Adapt(screenplay,default,"Drama").Failure,Is.EqualTo(ScreenplayGreenlightFailure.NotReady));
        }
        [Test]
        public void EmptyDraftCannotSubmit()
        { Assert.Throws<InvalidOperationException>(()=>ScreenplayProject.CreatePlayerAuthored("p","title",new[]{"drama"}).Authoring.Submit()); }
        [Test]
        public void AuthoringEditsInvalidateSubmissionAndFreshAdaptation()
        {
            var project=Example(); Adapt(project); project.Authoring.Rename("Revised title");
            Assert.That(project.Authoring.IsSubmitted,Is.False);
            Assert.That(new ScreenplayProductionAdapter().Adapt(project,default,"Drama").Failure,Is.EqualTo(ScreenplayGreenlightFailure.NotReady));
            project.Authoring.Submit(); Assert.That(project.Title,Is.EqualTo("Revised title"));
        }
        [Test]
        public void StableSceneIdentitySurvivesEditingAndReordering()
        {
            var project=Example(); project.Authoring.AddScene("second","Second",Plan(project));
            project.Authoring.MoveScene("second",0);
            Assert.That(project.Scenes.Select(scene=>scene.Id),Is.EqualTo(new[]{"second","argument"}));
            Assert.That(project.Scenes.Select(scene=>scene.SceneNumber),Is.EqualTo(new[]{1,2}));
            project.Authoring.UpdateScene("argument","Renamed",Plan(project).WithDuration(SceneDuration.Long));
            Assert.That(project.Scenes[1].Id,Is.EqualTo("argument")); Assert.That(project.Scenes[1].Title,Is.EqualTo("Renamed"));
            project.Authoring.RemoveScene("second"); Assert.That(project.Scenes[0].SceneNumber,Is.EqualTo(1));
        }
        [Test]
        public void DuplicateSceneAndInvalidOrderLeaveDraftUnchanged()
        {
            var project=Example();
            Assert.Throws<ArgumentException>(()=>project.Authoring.AddScene("argument","duplicate",Plan(project)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>project.Authoring.MoveScene("argument",2));
            Assert.That(project.Scenes.Count,Is.EqualTo(1));
        }
        [TestCase(SceneDuration.Short,3)]
        [TestCase(SceneDuration.Medium,5)]
        [TestCase(SceneDuration.Long,8)]
        public void DurationSelectsAuthoredSequence(SceneDuration duration,int count)
        {
            var project=Example(); project.Authoring.UpdateScene("argument","Argument",Plan(project).WithDuration(duration));
            var preview=project.Authoring.Preview("argument");
            Assert.That(preview.Performance.Beats.Count,Is.EqualTo(count));
            Assert.That(project.Scenes[0].Beats.Count,Is.EqualTo(count));
            Assert.That(preview.Performance.Beats.Select(beat=>beat.Definition.Id),Is.EqualTo(HeatedArgumentTemplate.Create().ResolveBeats(duration).Select(beat=>beat.Id)));
        }
        [Test]
        public void TemplateSelectionIsPerSceneAndDoesNotMutateDefinition()
        {
            var project=Example(); var previous=Plan(project); var replacement=MakePlan(TemplateWith());
            project.Authoring.UpdateScene("argument","Alternative",replacement);
            Assert.That(Plan(project).TemplateId,Is.EqualTo("test-template"));
            Assert.That(previous.TemplateId,Is.EqualTo(HeatedArgumentTemplate.TemplateId));
        }
        [TestCase("missing")][TestCase("extra")][TestCase("duplicate-role")][TestCase("duplicate-character")]
        public void InvalidMappingsAreRejected(string mode)
        {
            var bindings=new List<CharacterRoleBinding>{new CharacterRoleBinding("aggressor","hale"),new CharacterRoleBinding("defender","hart")};
            if(mode=="missing")bindings.RemoveAt(1);
            if(mode=="extra")bindings.Add(new CharacterRoleBinding("extra","third"));
            if(mode=="duplicate-role")bindings[1]=new CharacterRoleBinding("aggressor","hart");
            if(mode=="duplicate-character")bindings[1]=new CharacterRoleBinding("defender","hale");
            Assert.Throws<ArgumentException>(()=>MakePlan(bindings:bindings));
        }
        [Test]
        public void UnknownCharacterCannotEnterOwnedScene()
        {
            var project=Example(); var plan=MakePlan(bindings:new[]{new CharacterRoleBinding("aggressor","unknown"),new CharacterRoleBinding("defender","hart")});
            Assert.Throws<ArgumentException>(()=>project.Authoring.UpdateScene("argument","Bad",plan));
            Assert.That(project.Scenes[0].ParticipatingCharacterIds,Does.Contain("hale"));
        }
        [Test]
        public void CharacterActorAndTemplateRoleRemainDistinct()
        {
            var project=Example(); var preview=project.Authoring.Preview("argument");
            Assert.That(preview.Performance.Bindings.Single(binding=>binding.RoleId=="aggressor").PerformerId,Is.EqualTo("hale"));
            Assert.That(preview.IntendedActors.Single(actor=>actor.CharacterId=="hale").PersonId,Is.EqualTo("placeholder-performer-a"));
            Assert.That(Plan(project).Template.Roles.Select(role=>role.Id),Does.Not.Contain("placeholder-performer-a"));
        }
        [Test]
        public void ActorCanBeReplacedClearedAndLeftUnresolved()
        {
            var project=Example(); project.Authoring.AssignActor("hale","real-person");
            Assert.That(project.Authoring.Preview("argument").ReferenceWarnings,Does.Contain("Unresolved performer: real-person"));
            project.Authoring.ClearActor("hale"); project.Authoring.ClearActor("hart");
            Assert.That(project.Authoring.Preview("argument").IntendedActors,Is.Empty); Adapt(project);
        }
        [Test]
        public void UnavailableActorDoesNotCorruptCreativeSource()
        {
            var project=Example(); var preview=project.Authoring.Preview("argument",id=>new PerformerPreviewInfo(true,false));
            Assert.That(preview.ReferenceWarnings.Count,Is.EqualTo(2)); Assert.That(preview.Performance.Beats.Count,Is.EqualTo(5)); Adapt(project);
        }
        [Test]
        public void DuplicateAndKnownNonActorAssignmentsAreRejectedAtomically()
        {
            var project=Example();
            Assert.Throws<ArgumentException>(()=>project.Authoring.AssignActor("hale","placeholder-performer-b"));
            Assert.Throws<ArgumentException>(()=>project.Authoring.AssignActor("hale","director",id=>new PerformerPreviewInfo(false,true)));
            Assert.That(project.Authoring.IntendedActors.Single(actor=>actor.CharacterId=="hale").PersonId,Is.EqualTo("placeholder-performer-a"));
        }
        [Test]
        public void KnownCapabilitiesAreCheckedAndLaterLossIsReported()
        {
            var project=Example(); var template=TemplateWith(roles:new[]{new PerformerRoleSlot("aggressor","A",new[]{"standing"}),new PerformerRoleSlot("defender","B")});
            project.Authoring.UpdateScene("argument","Argument",MakePlan(template));
            Assert.Throws<ArgumentException>(()=>project.Authoring.AssignActor("hale","actor",id=>new PerformerPreviewInfo(true,true)));
            project.Authoring.AssignActor("hale","actor",id=>new PerformerPreviewInfo(true,true,new[]{"standing"}));
            Assert.That(project.Authoring.Preview("argument",id=>new PerformerPreviewInfo(true,true)).ReferenceWarnings,Does.Contain("Incompatible performer: actor"));
        }
        [Test]
        public void DirectorReferenceIsOptionalAndDoesNotCastProduction()
        {
            var project=Example(); project.Authoring.AssignDirector("director-two");
            var movie=Adapt(project); Assert.That(movie.AuthoringIntent.IntendedDirectorId,Is.EqualTo("director-two"));
            Assert.That(movie.AssignedDirector,Is.Null); project.Authoring.ClearDirector();
            Assert.That(project.Authoring.IntendedDirectorId,Is.Null); Assert.That(movie.AuthoringIntent.IntendedDirectorId,Is.EqualTo("director-two"));
        }
        [Test]
        public void SupportingAndBitCharactersAreNotBackgroundPopulation()
        {
            var project=Example(); var movie=Adapt(project);
            Assert.That(movie.CastRoles.Single(role=>role.SourceScreenplayCharacterId=="clerk").Prominence,Is.EqualTo(MovieRoleProminence.Minor));
            Assert.That(movie.CastRoles.Single(role=>role.SourceScreenplayCharacterId=="hart").Prominence,Is.EqualTo(MovieRoleProminence.Supporting));
            Assert.That(movie.CastRoles.Count,Is.EqualTo(3)); Assert.That(movie.Scenes[0].AuthoringIntent.BackgroundGroups.Single().MaximumCount,Is.EqualTo(3));
        }
        [Test]
        public void BackgroundPopulationCapturesConstraintsDefensively()
        {
            var people=new List<string>{"background-person"};
            var group=new BackgroundGroup("patrons","restaurant-patrons",1,4,true,people,"adult","formal","conversation"); people.Clear();
            Assert.That(group.SelectedPersonIds,Is.EqualTo(new[]{"background-person"}));
            Assert.That(group.CategoryRequirement,Is.EqualTo("adult")); Assert.That(group.WardrobeIntent,Is.EqualTo("formal"));
            Assert.Throws<ArgumentException>(()=>new BackgroundGroup("g","s",2,4,false,new[]{"one"}));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new BackgroundGroup("g","s",4,2));
        }
        [Test]
        public void BackgroundCannotDuplicatePrincipalVisualizationAssignment()
        {
            var project=Example();var plan=MakePlan(groups:new[]{new BackgroundGroup("g","patrons",1,1,false,new[]{"placeholder-performer-a"})});
            Assert.Throws<ArgumentException>(()=>project.Authoring.UpdateScene("argument","Bad",plan));
        }
        [TestCase(ContentSelectionKind.Automatic,null)]
        [TestCase(ContentSelectionKind.OwnedSet,"stage2-office03")]
        [TestCase(ContentSelectionKind.SavedSet,"my-office")]
        [TestCase(ContentSelectionKind.Location,"future-location")]
        public void SemanticSetSurvivesIndependentPhysicalSelection(ContentSelectionKind kind,string reference)
        {
            var project=Example(); project.Authoring.UpdateScene("argument","Office",Plan(project).WithSet(new SceneSetIntent("office",new ContentSelection(kind,reference))));
            var snapshot=Adapt(project).Scenes[0].AuthoringIntent;
            Assert.That(snapshot.Set.SemanticId,Is.EqualTo("office")); Assert.That(snapshot.Set.Selection.ReferenceId,Is.EqualTo(reference));
        }
        [Test]
        public void RequiredPropsResolveAutomaticallyAndCannotBeDisabledOrChangedSemantically()
        {
            var baseTemplate=HeatedArgumentTemplate.Create();
            var template=TemplateWith(requirements:baseTemplate.Requirements.Concat(new[]{new SceneRequirement("glass",SceneRequirementKind.Prop,"drinking-glass")}));
            // Preserve anchors because the existing beats still reference them.
            template=new SceneTemplate(template.Id,template.Name,template.Description,template.Roles,template.Beats,template.Variants,requirements:template.Requirements,anchors:baseTemplate.Anchors);
            var plan=MakePlan(template);
            Assert.That(plan.ResolveProps().Single(prop=>prop.RequirementId=="glass").Selection.Kind,Is.EqualTo(ContentSelectionKind.Automatic));
            Assert.Throws<ArgumentException>(()=>MakePlan(template,props:new[]{new PropIntent("glass","drinking-glass","glass",enabled:false)}));
            Assert.Throws<ArgumentException>(()=>MakePlan(template,props:new[]{new PropIntent("glass","chair","glass")}));
        }
        [Test]
        public void OptionalPropsCanBeSelectedDisabledAddedAndRemoved()
        {
            var plan=MakePlan(props:new[]{new PropIntent("table-choice","table-for-two","table",new ContentSelection(ContentSelectionKind.Content,"table-03")),new PropIntent("flowers","vase")});
            Assert.That(plan.ResolveProps().Count,Is.EqualTo(2));
            plan=plan.WithProps(new[]{new PropIntent("table-choice","table-for-two","table",enabled:false)});
            Assert.That(plan.ResolveProps().Single().Enabled,Is.False);
            plan=plan.WithProps(Array.Empty<PropIntent>()); Assert.That(plan.ResolveProps().Single().Selection.Kind,Is.EqualTo(ContentSelectionKind.Automatic));
        }
        [Test]
        public void CostumeIntentSupportsAutomaticSemanticAndSpecificReferences()
        {
            var costume=new CostumeIntent("hale","detective-suit","wardrobe-1930-01");
            var plan=MakePlan(costumes:new[]{costume,new CostumeIntent("hart")});
            Assert.That(plan.Costumes[0].WardrobeReferenceId,Is.EqualTo("wardrobe-1930-01")); Assert.That(plan.Costumes[1].Automatic,Is.True);
            Assert.Throws<ArgumentException>(()=>MakePlan(costumes:new[]{new CostumeIntent("unknown")}));
        }
        [Test]
        public void AutomaticSoundsComeFromSemanticCuesAndMovement()
        {
            var sounds=SceneSoundPlanner.Resolve(Plan(Example()));
            Assert.That(sounds.Select(sound=>sound.SemanticType),Does.Contain("Door.Open"));
            Assert.That(sounds.Select(sound=>sound.SemanticType),Does.Contain("Door.Close"));
            Assert.That(sounds.Count(sound=>sound.SemanticType=="Footsteps"),Is.EqualTo(2));
            Assert.That(sounds.All(sound=>sound.Automatic&&sound.Settings.Enabled&&sound.Settings.AssetReferenceId==null),Is.True);
        }
        [Test]
        public void SoundOverridesAreIndependentAndCanBeReset()
        {
            var plan=Plan(Example());var original=SceneSoundPlanner.Resolve(plan);
            var close=original.Single(sound=>sound.SemanticType=="Door.Close");
            plan=plan.WithSound(close.WithSettings(new SoundSettings(true,"my-door-close",.35,-.2,.1,.4,SoundSpatialIntent.Scene)));
            var sounds=SceneSoundPlanner.Resolve(plan);var changed=sounds.Single(sound=>sound.Id==close.Id);
            Assert.That(changed.Settings.Volume,Is.EqualTo(.35)); Assert.That(changed.Settings.AssetReferenceId,Is.EqualTo("my-door-close"));
            Assert.That(changed.Settings.TimingOffsetSeconds,Is.EqualTo(-.2)); Assert.That(changed.Settings.FadeInSeconds,Is.EqualTo(.1));
            Assert.That(changed.Settings.FadeOutSeconds,Is.EqualTo(.4)); Assert.That(changed.Settings.SpatialIntent,Is.EqualTo(SoundSpatialIntent.Scene));
            Assert.That(sounds.Where(sound=>sound.Id!=close.Id).All(sound=>sound.Settings.Volume==1),Is.True);
            plan=plan.WithoutSoundEdit(close.Id);Assert.That(SceneSoundPlanner.Resolve(plan).Single(sound=>sound.Id==close.Id).Settings.Volume,Is.EqualTo(1));
        }
        [Test]
        public void OptionalSoundCanBeDisabledAndCustomSoundAdded()
        {
            var plan=Plan(Example());var close=SceneSoundPlanner.Resolve(plan).Single(sound=>sound.SemanticType=="Door.Close");
            plan=plan.WithSound(close.WithSettings(new SoundSettings(false))).WithSound(PlannedSound.Custom("glass",new AuthoredBeatReference("respond"),"Glass.Contact"));
            Assert.That(SceneSoundPlanner.Resolve(plan).Single(sound=>sound.Id==close.Id).Settings.Enabled,Is.False);
            Assert.That(SceneSoundPlanner.Resolve(plan).Single(sound=>sound.SemanticType=="Glass.Contact").Automatic,Is.False);
            Assert.That(SceneSoundPlanner.Resolve(plan.WithoutSoundEdit("custom:glass")).Count,Is.EqualTo(4));
        }
        [TestCase(-.01)][TestCase(1.01)][TestCase(double.NaN)][TestCase(double.PositiveInfinity)]
        public void InvalidVolumesAreRejected(double value) { Assert.Throws<ArgumentOutOfRangeException>(()=>new SoundSettings(volume:value)); }
        [TestCase(0)][TestCase(1)] public void VolumeEndpointsAreValid(double value) { Assert.That(new SoundSettings(volume:value).Volume,Is.EqualTo(value)); }
        [Test]
        public void InvalidSoundTimingAndFadesAreRejected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(()=>new SoundSettings(timingOffsetSeconds:double.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new SoundSettings(fadeInSeconds:-1));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new SoundSettings(fadeOutSeconds:61));
        }
        [Test]
        public void DialogueBelongsToFictionalCharacterAndSpecificBeat()
        {
            var project=Example();var scene=project.Scenes[0];var line=scene.Authoring.Dialogue[0];
            Assert.That(line.CharacterId,Is.EqualTo("hale")); Assert.That(line.Emotion,Is.EqualTo(ScreenplayEmotion.Angry));
            Assert.That(scene.Beats.Single(beat=>beat.Id=="argument:confront#1").Content,Is.EqualTo("You knew about this?"));
            Assert.Throws<ArgumentException>(()=>MakePlan(dialogue:new[]{new AuthoredDialogue("wrong",new AuthoredBeatReference("confront"),"hart","Wrong speaker")}));
            Assert.Throws<ArgumentException>(()=>MakePlan(dialogue:new[]{new AuthoredDialogue("unknown",new AuthoredBeatReference("missing"),"hale","Missing beat")}));
        }
        [Test]
        public void InactiveVariantEditsAreRetainedWithoutPlayingThem()
        {
            var plan=Plan(Example());var cue=SceneSoundPlanner.Resolve(plan).Single(sound=>sound.SemanticType=="Door.Open");
            plan=plan.WithSound(cue.WithSettings(new SoundSettings(volume:.4))).WithDuration(SceneDuration.Short);
            Assert.That(SceneSoundPlanner.Resolve(plan).Any(sound=>sound.SemanticType=="Door.Open"),Is.False);
            Assert.That(SceneSoundPlanner.Resolve(plan.WithDuration(SceneDuration.Medium)).Single(sound=>sound.SemanticType=="Door.Open").Settings.Volume,Is.EqualTo(.4));
        }
        [Test]
        public void RepeatedBeatOccurrencesHaveSeparateDialogueAndSoundIdentity()
        {
            var variants=Enum.GetValues(typeof(SceneDuration)).Cast<SceneDuration>().Select(duration=>new SceneTemplateVariant(duration,new[]{"confront","confront","exit"}));
            var plan=MakePlan(TemplateWith(variants:variants),dialogue:new[]{new AuthoredDialogue("second",new AuthoredBeatReference("confront",2),"hale","Second accusation")});
            plan=plan.WithSound(PlannedSound.Custom("contact",new AuthoredBeatReference("confront",2),"Glass.Contact"));
            Assert.That(plan.ResolveBeatReferences().Select(reference=>reference.Key),Is.EqualTo(new[]{"confront#1","confront#2","exit#1"}));
            Assert.That(SceneSoundPlanner.Resolve(plan).Single(sound=>!sound.Automatic).Beat.Key,Is.EqualTo("confront#2"));
            var project=Example();project.Authoring.UpdateScene("argument","Repeat",plan);
            Assert.That(project.Scenes[0].Beats[1].Content,Is.EqualTo("Second accusation"));
            Assert.That(project.Scenes[0].Beats[0].Content,Is.Not.EqualTo("Second accusation"));
        }
        [Test]
        public void AdaptationCopiesPlanAndProvenanceWithoutCastingOrFootage()
        {
            var project=Example();var movie=Adapt(project);var scene=movie.Scenes.Single();var snapshot=scene.AuthoringIntent;
            Assert.That(movie.SourceScreenplayId,Is.EqualTo(project.Id));Assert.That(scene.SourceScreenplaySceneId,Is.EqualTo("argument"));
            Assert.That(snapshot.TemplateId,Is.EqualTo(HeatedArgumentTemplate.TemplateId));Assert.That(snapshot.Duration,Is.EqualTo(SceneDuration.Medium));
            foreach(var binding in snapshot.Bindings)Assert.That(movie.CastRoles.Single(role=>role.Id==binding.MovieRoleId).SourceScreenplayCharacterId,Is.EqualTo(binding.SourceCharacterId));
            Assert.That(snapshot.Beats.All(beat=>movie.CastRoles.Any(role=>role.Id==beat.PerformerRoleId)),Is.True);
            Assert.That(snapshot.Dialogue[0].CharacterId,Is.EqualTo("hale"));Assert.That(snapshot.Set.SemanticId,Is.EqualTo("office"));
            Assert.That(snapshot.Sounds.Count,Is.EqualTo(4));Assert.That(snapshot.Costumes.Count,Is.EqualTo(2));Assert.That(snapshot.BackgroundGroups.Count,Is.EqualTo(1));
            Assert.That(movie.CastRoles.All(role=>role.AssignedActorId==null),Is.True);Assert.That(scene.Takes,Is.Empty);
        }
        [Test]
        public void PreviewAndLaterEditsCannotMutateProductionResultsOrCollections()
        {
            var project=Example();var movie=Adapt(project);var scene=movie.Scenes.Single();var oldSounds=scene.AuthoringIntent.Sounds;
            var preview=project.Authoring.Preview("argument");var cursor=new SceneSequenceCursor(preview.Performance);
            while(cursor.Advance()){} cursor.Reset();
            project.Authoring.UpdateScene("argument","Edited",Plan(project).WithDuration(SceneDuration.Short));
            project.Authoring.ClearActor("hale");project.Authoring.Rename("Changed");project.Authoring.RemoveScene("argument");
            Assert.That(movie.Title,Is.EqualTo("Midnight Over Manhattan"));Assert.That(scene.AuthoringIntent.Duration,Is.EqualTo(SceneDuration.Medium));
            Assert.That(scene.AuthoringIntent.Sounds,Is.SameAs(oldSounds));Assert.That(scene.Beats.Count,Is.EqualTo(5));
            Assert.That(movie.CurrentState,Is.EqualTo(MovieProductionState.Draft));Assert.That(movie.ProductionProgress,Is.Zero);
            Assert.That(movie.Budget,Is.Zero);Assert.That(movie.ProductionResult,Is.Null);Assert.That(movie.ReleaseDate,Is.Null);Assert.That(scene.Takes,Is.Empty);
            Assert.That(scene.CompleteFilming(),Is.False);
        }
        [Test]
        public void OwnedCollectionsCannotBeMutatedThroughCastsOrInputLists()
        {
            var dialogue=new List<AuthoredDialogue>{new AuthoredDialogue("line",new AuthoredBeatReference("confront"),"hale","Hello")};
            var plan=MakePlan(dialogue:dialogue); dialogue.Clear(); Assert.That(plan.Dialogue.Count,Is.EqualTo(1));
            Assert.Throws<NotSupportedException>(()=>((IList<AuthoredDialogue>)plan.Dialogue).Clear());
            var project=Example();Assert.Throws<NotSupportedException>(()=>((IList<ScreenplayScene>)project.Scenes).Clear());
            Assert.That(project.Scenes[0].AddCharacter("unowned"),Is.False);
            Assert.That(project.Scenes[0].AddBeat(new ScreenplayBeat("injected",1,ScreenplayBeatType.Action,"Injected")),Is.False);
            var snapshot=Adapt(project).Scenes[0].AuthoringIntent;
            Assert.Throws<NotSupportedException>(()=>((IList<PlannedSound>)snapshot.Sounds).Clear());
        }

        [Test]
        public void RequiredSoundCannotBeDisabledAndUnknownOccurrenceIsRejected()
        {
            var source = HeatedArgumentTemplate.Create();
            var beats = source.Beats.Select(beat => beat.Id == "confront"
                ? new PerformanceBeat(beat.Id, beat.Action, beat.Direction, beat.RoleId, beat.TargetRoleId,
                    soundCues: new[] { new SemanticSoundCue("required", "Signal", optional: false) }) : beat);
            var plan = MakePlan(TemplateWith(beats: beats));
            var required = SceneSoundPlanner.Resolve(plan).Single(sound => sound.SemanticType == "Signal");
            Assert.Throws<ArgumentException>(() => required.WithSettings(new SoundSettings(false)));
            Assert.Throws<ArgumentException>(() => plan.WithSound(PlannedSound.Custom("bad", new AuthoredBeatReference("confront", 2), "Signal")));
            Assert.Throws<ArgumentException>(() => MakePlan().WithSound(required));
        }

        [Test]
        public void ThreeMeaningfulRolesResolveWithoutInventingBackgroundCharacters()
        {
            var source = HeatedArgumentTemplate.Create();
            var template = TemplateWith(roles: source.Roles.Concat(new[] { new PerformerRoleSlot("witness", "Witness") }));
            var plan = MakePlan(template, bindings: new[] { new CharacterRoleBinding("aggressor", "hale"),
                new CharacterRoleBinding("defender", "hart"), new CharacterRoleBinding("witness", "clerk") });
            var project = Example(); project.Authoring.UpdateScene("argument", "Three roles", plan);
            Assert.That(project.Authoring.Preview("argument").Performance.Bindings.Count, Is.EqualTo(3));
            Assert.That(Adapt(project).Scenes[0].AuthoringIntent.Bindings.Count, Is.EqualTo(3));
        }

        [Test]
        public void CharacterRemovalProtectsReferencesAndReplacementPreservesIdentity()
        {
            var project = Example();
            Assert.Throws<InvalidOperationException>(() => project.Authoring.RemoveCharacter("hale"));
            var original = project.Characters.Single(character => character.Id == "hale");
            project.Authoring.ReplaceCharacter(original);
            Assert.That(project.Scenes[0].ParticipatingCharacterIds, Does.Contain("hale"));
            project.Authoring.AssignActor("clerk", "clerk-person"); project.Authoring.RemoveCharacter("clerk");
            Assert.That(project.Authoring.IntendedActors.Any(actor => actor.CharacterId == "clerk"), Is.False);
        }

        [Test]
        public void StrategicWritingCannotCompleteOrEvaluatePlayerDraft()
        {
            var project = Example();
            Assert.That(project.AdvanceWritingMinute(new Dictionary<string, int> { { "writer", 100 } }), Is.False);
            Assert.That(project.Progress, Is.Zero);
            project.Authoring.Submit(); Assert.That(project.MarkEvaluationFailed(), Is.False);
            project.TryAddGenre("mystery"); Assert.That(project.Authoring.IsSubmitted, Is.False);
        }
    }
}
