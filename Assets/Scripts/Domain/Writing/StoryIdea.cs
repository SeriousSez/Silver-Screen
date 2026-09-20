using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Writing
{
    public enum IdeaDevelopmentState { Traveling, Developing, Completed, Failed }

    [Serializable]
    public sealed class StoryIdea
    {
        private readonly List<string> _genreIds = new List<string>();

        public string Id { get; }
        public string CreatorWriterId { get; }
        public IdeaDevelopmentState State { get; private set; }
        public double Progress { get; private set; }
        public string Title { get; private set; }
        public IReadOnlyList<string> GenreIds => _genreIds;
        public string PrimaryGenreId => _genreIds.Count > 0 ? _genreIds[0] : string.Empty;
        public string SettingId { get; private set; }
        public string ProtagonistArchetypeId { get; private set; }
        public string AntagonistArchetypeId { get; private set; }
        public string ThemeId { get; private set; }

        public event Action<StoryIdea> Changed;

        public StoryIdea(string id, string creatorWriterId)
        {
            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id.Trim();
            CreatorWriterId = string.IsNullOrWhiteSpace(creatorWriterId)
                ? throw new ArgumentException("A creator Writer ID is required.", nameof(creatorWriterId))
                : creatorWriterId.Trim();
            State = IdeaDevelopmentState.Traveling;
            Title = "Developing New Idea";
        }

        public bool BeginDevelopment()
        {
            if (State != IdeaDevelopmentState.Traveling) return false;
            State = IdeaDevelopmentState.Developing;
            Changed?.Invoke(this);
            return true;
        }

        public bool AdvanceDevelopmentMinute(int writerSkill, int baseDurationMinutes)
        {
            if (State != IdeaDevelopmentState.Developing) return false;
            double contribution = 0.85d + Math.Clamp(writerSkill, 0, 100) * 0.003d;
            Progress = Math.Min(1d, Progress + contribution / Math.Max(1, baseDurationMinutes));
            Changed?.Invoke(this);
            return Progress >= 1d;
        }

        public bool Complete(string title, IEnumerable<string> genreIds, string settingId,
            string protagonistArchetypeId, string antagonistArchetypeId, string themeId)
        {
            if (State != IdeaDevelopmentState.Developing || Progress < 1d || genreIds == null) return false;

            var genres = new List<string>();
            foreach (string genreId in genreIds)
            {
                if (string.IsNullOrWhiteSpace(genreId)) return false;
                string normalized = genreId.Trim().ToLowerInvariant();
                if (genres.Contains(normalized) || genres.Count >= 2) return false;
                genres.Add(normalized);
            }
            if (genres.Count == 0) return false;

            _genreIds.AddRange(genres);
            Title = string.IsNullOrWhiteSpace(title) ? "Untitled Idea" : title.Trim();
            SettingId = settingId ?? string.Empty;
            ProtagonistArchetypeId = protagonistArchetypeId ?? string.Empty;
            AntagonistArchetypeId = antagonistArchetypeId ?? string.Empty;
            ThemeId = themeId ?? string.Empty;
            State = IdeaDevelopmentState.Completed;
            Changed?.Invoke(this);
            return true;
        }

        public void Fail()
        {
            if (State == IdeaDevelopmentState.Completed || State == IdeaDevelopmentState.Failed) return;
            State = IdeaDevelopmentState.Failed;
            Changed?.Invoke(this);
        }
    }
}
