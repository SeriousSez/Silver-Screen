using System;
using SilverScreen.Domain.Performance;
using SilverScreen.Domain.Writing;

namespace SilverScreen.Domain.Movie
{
    public static class MovieMakerAuthoringExample
    {
        public static ScreenplayProject Create()
        {
            var screenplay = ScreenplayProject.CreatePlayerAuthored("midnight-over-manhattan", "Midnight Over Manhattan", new[] { "drama" });
            var authoring = screenplay.Authoring;
            authoring.AddCharacter(new ScreenplayCharacter("hale", "Vincent Hale", ScreenplayCharacterRole.Protagonist));
            authoring.AddCharacter(new ScreenplayCharacter("hart", "Evelyn Hart", ScreenplayCharacterRole.Supporting));
            authoring.AddCharacter(new ScreenplayCharacter("clerk", "Office Clerk", ScreenplayCharacterRole.Bit));
            authoring.AddScene("argument", "Heated Argument", new ScreenplaySceneAuthoring(
                HeatedArgumentTemplate.Create(), SceneDuration.Medium,
                new[] { new CharacterRoleBinding("aggressor", "hale"), new CharacterRoleBinding("defender", "hart") },
                new SceneSetIntent("office"),
                dialogue: new[] {
                    new AuthoredDialogue("accusation", new AuthoredBeatReference("confront"), "hale", "You knew about this?", ScreenplayEmotion.Angry),
                    new AuthoredDialogue("defense", new AuthoredBeatReference("respond"), "hart", "I was trying to protect you.", ScreenplayEmotion.Afraid) },
                costumes: new[] { new CostumeIntent("hale", "1930s-detective-suit"), new CostumeIntent("hart", "1930s-office-attire") },
                backgroundGroups: new[] { new BackgroundGroup("office-workers", "office-worker", 1, 3, categoryRequirement: "adult", wardrobeIntent: "office-attire", behaviorIntent: "quiet-desk-work") },
                direction: "A restrained accusation becomes a confrontation.", notes: "Keep the office workers in the background."));
            authoring.AssignActor("hale", "placeholder-performer-a");
            authoring.AssignActor("hart", "placeholder-performer-b");
            authoring.AssignDirector("placeholder-director");
            return screenplay;
        }
    }
}
