using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Writing
{
    public sealed class StoryIdeaConcept
    {
        public string Title { get; }
        public IReadOnlyList<string> GenreIds { get; }
        public string SettingId { get; }
        public string ProtagonistId { get; }
        public string AntagonistId { get; }
        public string ThemeId { get; }

        public StoryIdeaConcept(string title, IReadOnlyList<string> genres, string setting,
            string protagonist, string antagonist, string theme)
        {
            Title = title;
            GenreIds = genres;
            SettingId = setting;
            ProtagonistId = protagonist;
            AntagonistId = antagonist;
            ThemeId = theme;
        }
    }

    public sealed class StoryIdeaGenerator
    {
        private readonly IScreenplayTitleRandomSource _random;
        private readonly ScreenplayTitleGenerator _titles;
        private readonly List<string> _availableGenres;
        private static readonly string[] Settings = { "modern-american-city", "small-american-town", "european-capital", "remote-village", "wild-west", "tropical-island", "country-estate", "ocean-liner", "mountain-community" };
        private static readonly string[] Protagonists = { "detective", "daring-adventurer", "journalist", "doctor", "soldier", "outcast", "inventor", "artist", "small-town-dreamer", "charismatic-criminal" };
        private static readonly string[] Antagonists = { "corrupt-official", "criminal-mastermind", "rival", "wealthy-industrialist", "mysterious-stranger", "gang-leader", "monster" };
        private static readonly string[] Themes = { "revenge", "forbidden-love", "hidden-identity", "survival", "ambition", "redemption", "conspiracy", "family-conflict", "discovery", "betrayal" };
        private static readonly Dictionary<string, string[]> CompatibleSecondary = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["action"] = new[] { "thriller", "comedy", "drama" },
            ["drama"] = new[] { "romance", "thriller", "horror" },
            ["comedy"] = new[] { "romance", "action" },
            ["horror"] = new[] { "thriller", "drama" },
            ["romance"] = new[] { "comedy", "drama" },
            ["thriller"] = new[] { "action", "drama", "horror" }
        };

        public StoryIdeaGenerator(IScreenplayTitleRandomSource random, ScreenplayTitleGenerator titles,
            IEnumerable<string> availableGenres)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _titles = titles ?? throw new ArgumentNullException(nameof(titles));
            _availableGenres = new List<string>();
            if (availableGenres != null)
            {
                foreach (string genre in availableGenres)
                {
                    if (!string.IsNullOrWhiteSpace(genre) &&
                        !_availableGenres.Contains(genre.Trim().ToLowerInvariant()))
                        _availableGenres.Add(genre.Trim().ToLowerInvariant());
                }
            }
            if (_availableGenres.Count == 0)
                throw new ArgumentException("At least one available genre is required.", nameof(availableGenres));
        }

        public StoryIdeaConcept Generate(IEnumerable<string> existingTitles)
        {
            string primary = Pick(_availableGenres);
            var genres = new List<string> { primary };
            if (_random.Next(0, 100) < 35 &&
                CompatibleSecondary.TryGetValue(primary, out var compatible))
            {
                var available = new List<string>();
                foreach (string candidate in compatible)
                    if (_availableGenres.Contains(candidate)) available.Add(candidate);
                if (available.Count > 0) genres.Add(Pick(available));
            }

            string antagonist = _random.Next(0, 100) < 70 ? Pick(Antagonists) : string.Empty;
            return new StoryIdeaConcept(_titles.GenerateUnique(primary, existingTitles), genres,
                Pick(Settings), Pick(Protagonists), antagonist, Pick(Themes));
        }

        private string Pick(IReadOnlyList<string> values) => values[_random.Next(0, values.Count)];
    }
}
