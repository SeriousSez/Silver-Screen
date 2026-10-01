using SilverScreen.Domain.Performance;

namespace SilverScreen.Domain.Movie
{
    /// <summary>First SilverScreen-authored consumer of the reusable performance vocabulary.</summary>
    public static class HeatedArgumentTemplate
    {
        public const string TemplateId = "heated-argument.v1";
        public const string Aggressor = "aggressor";
        public const string Defender = "defender";

        public static SceneTemplate Create()
        {
            return new SceneTemplate(TemplateId, "Heated Argument",
                "Two performers confront one another, exchange accusations, and part.",
                new[] { new PerformerRoleSlot(Aggressor, "Aggressor"), new PerformerRoleSlot(Defender, "Defender") },
                new[]
                {
                    new PerformanceBeat("approach", PerformanceAction.Approach, "Approach the other performer.", Aggressor, Defender,
                        "standing-a", new CameraSuggestion(CameraFraming.Establishing),
                        soundCues: new[] { new SemanticSoundCue("door-open", "Door.Open", "entry") }),
                    new PerformanceBeat("confront", PerformanceAction.Speak, "Demand an explanation.", Aggressor, Defender,
                        camera: new CameraSuggestion(CameraFraming.TwoShot)),
                    new PerformanceBeat("respond", PerformanceAction.Speak, "Refuse the accusation.", Defender, Aggressor,
                        camera: new CameraSuggestion(CameraFraming.CloseUp, Defender)),
                    new PerformanceBeat("react", PerformanceAction.React, "Take in the unexpected answer.", Aggressor, Defender),
                    new PerformanceBeat("second-exchange", PerformanceAction.Interrupt, "Interrupt with a second accusation.", Aggressor, Defender,
                        camera: new CameraSuggestion(CameraFraming.OverShoulder, Aggressor, Defender)),
                    new PerformanceBeat("escalate", PerformanceAction.Gesture, "Gesture in frustration.", Defender, Aggressor),
                    new PerformanceBeat("final-reaction", PerformanceAction.React, "Turn away, disappointed.", Aggressor, Defender),
                    new PerformanceBeat("exit", PerformanceAction.Exit, "Leave the conversation.", Aggressor,
                        anchorId: "exit-position", camera: new CameraSuggestion(CameraFraming.Wide),
                        soundCues: new[] { new SemanticSoundCue("door-close", "Door.Close", "exit-complete") })
                },
                new[]
                {
                    new SceneTemplateVariant(SceneDuration.Short, new[] { "confront", "respond", "exit" }),
                    new SceneTemplateVariant(SceneDuration.Medium, new[] { "approach", "confront", "respond", "escalate", "exit" }),
                    new SceneTemplateVariant(SceneDuration.Long, new[] { "approach", "confront", "respond", "react", "second-exchange", "escalate", "final-reaction", "exit" })
                },
                tags: new[] { "conversation", "conflict" },
                requirements: new[]
                {
                    new SceneRequirement("playing-space", SceneRequirementKind.Environment, "standing-space-for-two"),
                    new SceneRequirement("table", SceneRequirementKind.Prop, "table-for-two", required: false)
                },
                anchors: new[]
                {
                    new InteractionAnchor("standing-a", "standing-position", "playing-space"),
                    new InteractionAnchor("standing-b", "standing-position", "playing-space"),
                    new InteractionAnchor("exit-position", "standing-position", "playing-space")
                });
        }
    }
}
