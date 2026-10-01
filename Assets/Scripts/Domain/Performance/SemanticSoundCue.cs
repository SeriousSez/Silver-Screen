namespace SilverScreen.Domain.Performance
{
    /// <summary>A reusable event, not an audio clip or a timed recording.</summary>
    public sealed class SemanticSoundCue
    {
        public string Id { get; }
        public string SemanticType { get; }
        public string TimingEvent { get; }
        public bool Optional { get; }
        public SemanticSoundCue(string id, string semanticType, string timingEvent = "beat-start", bool optional = true)
        {
            Id = TemplateValidation.Id(id);
            SemanticType = TemplateValidation.Id(semanticType);
            TimingEvent = TemplateValidation.Id(timingEvent);
            Optional = optional;
        }
    }
}
