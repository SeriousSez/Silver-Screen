using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Writing
{
    public sealed class ScreenplayContentGenerationException : InvalidOperationException
    {
        public ScreenplayContentGenerationException(string message) : base(message) { }
    }

    public sealed class ScreenplayContent
    {
        public IReadOnlyList<ScreenplayCharacter> Characters { get; }
        public IReadOnlyList<ScreenplayScene> Scenes { get; }

        public ScreenplayContent(IReadOnlyList<ScreenplayCharacter> characters,
            IReadOnlyList<ScreenplayScene> scenes)
        {
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            Scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
        }
    }

    public sealed class ScreenplayContentGenerator
    {
        private static readonly string[] FirstNames =
            { "Evelyn", "Clara", "Margaret", "Vivian", "Jack", "Victor", "Thomas", "Arthur", "Rose", "Walter", "Miriam", "Frank" };
        private static readonly string[] LastNames =
            { "Cross", "Hale", "Shaw", "Mercer", "Vance", "Sterling", "Hart", "Langley", "Reed", "Vale", "Bennett", "Price" };
        private static readonly string[] FallbackArchetypes =
            { "investigator", "dreamer", "professional", "outsider", "guardian", "rival", "confidant" };
        private static readonly Dictionary<string, string[]> GenreSetPreferences =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                ["drama"] = new[] { SetDefinitionIds.LivingRoom, SetDefinitionIds.Bedroom, SetDefinitionIds.Office, SetDefinitionIds.RestaurantCafe },
                ["comedy"] = new[] { SetDefinitionIds.LivingRoom, SetDefinitionIds.RestaurantCafe, SetDefinitionIds.Office, SetDefinitionIds.Street },
                ["romance"] = new[] { SetDefinitionIds.RestaurantCafe, SetDefinitionIds.LivingRoom, SetDefinitionIds.Street },
                ["thriller"] = new[] { SetDefinitionIds.Office, SetDefinitionIds.Street, SetDefinitionIds.GenericInterior },
                ["action"] = new[] { SetDefinitionIds.Street, SetDefinitionIds.Office, SetDefinitionIds.GenericInterior },
                ["horror"] = new[] { SetDefinitionIds.Bedroom, SetDefinitionIds.GenericInterior, SetDefinitionIds.Street }
            };
        private static readonly Dictionary<string, string[]> SemanticLocationsBySet =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                [SetDefinitionIds.GenericInterior] = new[] { "hotel-lobby", "theatre-backstage", "private-club" },
                [SetDefinitionIds.Office] = new[] { "studio-executive-office", "detective-office", "newspaper-office" },
                [SetDefinitionIds.LivingRoom] = new[] { "family-apartment", "townhouse-parlor", "country-estate-sitting-room" },
                [SetDefinitionIds.Bedroom] = new[] { "hotel-bedroom", "upstairs-bedroom", "boarding-house-room" },
                [SetDefinitionIds.RestaurantCafe] = new[] { "neighborhood-cafe", "hotel-restaurant", "downtown-nightclub" },
                [SetDefinitionIds.Street] = new[] { "downtown-street", "studio-backlot-street", "rainy-side-street" }
            };
        private readonly IScreenplayTitleRandomSource _random;
        private readonly IStudioFilmingCapabilities _filmingCapabilities;

        public ScreenplayContentGenerator(IScreenplayTitleRandomSource random,
            IStudioFilmingCapabilities filmingCapabilities)
        {
            _random = random ?? throw new ArgumentNullException(nameof(random));
            _filmingCapabilities = filmingCapabilities ??
                throw new ArgumentNullException(nameof(filmingCapabilities));
        }

        public ScreenplayContent Generate(ScreenplayProject screenplay)
        {
            if (screenplay == null) throw new ArgumentNullException(nameof(screenplay));
            if (_filmingCapabilities.AvailableDefinitions.Count == 0)
                throw new ScreenplayContentGenerationException(
                    "The studio has no filming environments available for screenplay development.");

            int characterCount = _random.Next(2, 5);
            var characters = GenerateCharacters(screenplay, characterCount);
            int sceneCount = _random.Next(3, 8);
            var scenes = new List<ScreenplayScene>(sceneCount);

            for (int sceneIndex = 0; sceneIndex < sceneCount; sceneIndex++)
            {
                int number = sceneIndex + 1;
                string sceneId = $"{screenplay.Id}:scene:{number}";
                SetDefinition requiredSet = SelectRequiredSet(screenplay.PrimaryGenreId);
                string location = ResolveLocation(screenplay, sceneIndex, requiredSet.Id);
                var locationType = requiredSet.Id == SetDefinitionIds.Street
                    ? ScreenplaySceneLocation.Exterior
                    : ScreenplaySceneLocation.Interior;
                var time = sceneIndex % 2 == 0 ? ScreenplayTimeOfDay.Day : ScreenplayTimeOfDay.Night;
                var scene = new ScreenplayScene(sceneId, number, location, locationType, time,
                    BuildSceneTitle(screenplay, number), requiredSet.Id);

                ScreenplayCharacter protagonist = characters[0];
                scene.AddCharacter(protagonist.Id);
                ScreenplayCharacter partner = characters[1 + sceneIndex % (characters.Count - 1)];
                scene.AddCharacter(partner.Id);
                if (characters.Count > 2 && sceneIndex % 2 == 0)
                    scene.AddCharacter(characters[2].Id);

                AddPrototypeBeats(screenplay, scene, protagonist, partner);
                scenes.Add(scene);
            }

            return new ScreenplayContent(characters, scenes);
        }

        private List<ScreenplayCharacter> GenerateCharacters(ScreenplayProject screenplay, int count)
        {
            var characters = new List<ScreenplayCharacter>(count);
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            characters.Add(CreateCharacter(screenplay.Id, 1, ScreenplayCharacterRole.Protagonist,
                ChooseArchetype(screenplay.ProtagonistArchetypeId), usedNames));

            bool hasAntagonist = !string.IsNullOrEmpty(screenplay.AntagonistArchetypeId) ||
                                 _random.Next(0, 100) < 70;
            if (hasAntagonist && characters.Count < count)
                characters.Add(CreateCharacter(screenplay.Id, characters.Count + 1, ScreenplayCharacterRole.Antagonist,
                    ChooseArchetype(screenplay.AntagonistArchetypeId), usedNames));

            while (characters.Count < count)
                characters.Add(CreateCharacter(screenplay.Id, characters.Count + 1, ScreenplayCharacterRole.Supporting,
                    Pick(FallbackArchetypes), usedNames));
            return characters;
        }

        private ScreenplayCharacter CreateCharacter(string screenplayId, int number, ScreenplayCharacterRole role,
            string archetype, HashSet<string> usedNames)
        {
            string name;
            do name = $"{Pick(FirstNames)} {Pick(LastNames)}";
            while (!usedNames.Add(name));
            return new ScreenplayCharacter($"{screenplayId}:character:{number}", name, role, archetype,
                $"The {Display(archetype)} of the story.");
        }

        private void AddPrototypeBeats(ScreenplayProject screenplay, ScreenplayScene scene,
            ScreenplayCharacter protagonist, ScreenplayCharacter partner)
        {
            string theme = Display(screenplay.ThemeId);
            string genre = Display(screenplay.PrimaryGenreId);
            scene.AddBeat(new ScreenplayBeat($"{scene.Id}-beat-1", 1, ScreenplayBeatType.Action,
                $"{protagonist.Name} enters and searches the {Display(scene.LocationId)}.",
                protagonist.Id, blockingIntentionId: "enters-location", intendedIntensity: 0.55d));
            scene.AddBeat(new ScreenplayBeat($"{scene.Id}-beat-2", 2, ScreenplayBeatType.Dialogue,
                $"We cannot ignore what this means for {theme.ToLowerInvariant()}.",
                protagonist.Id, partner.Id, ScreenplayEmotion.Confident, intendedIntensity: 0.65d));
            scene.AddBeat(new ScreenplayBeat($"{scene.Id}-beat-3", 3, ScreenplayBeatType.Reaction,
                $"{partner.Name} absorbs the {genre.ToLowerInvariant()} revelation.",
                partner.Id, protagonist.Id, ReactionFor(screenplay.PrimaryGenreId), intendedIntensity: 0.6d));
        }

        private SetDefinition SelectRequiredSet(string genreId)
        {
            var weighted = new List<SetDefinition>();
            GenreSetPreferences.TryGetValue(genreId ?? string.Empty, out string[] preferences);
            foreach (SetDefinition available in _filmingCapabilities.AvailableDefinitions)
            {
                weighted.Add(available);
                if (preferences == null) continue;
                for (int index = 0; index < preferences.Length; index++)
                    if (string.Equals(preferences[index], available.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        weighted.Add(available);
                        weighted.Add(available);
                        break;
                    }
            }
            return Pick(weighted);
        }

        private string ResolveLocation(ScreenplayProject screenplay, int sceneIndex, string requiredSetDefinitionId)
        {
            if (!string.IsNullOrEmpty(screenplay.SettingId) && sceneIndex % 2 == 0)
                return screenplay.SettingId;
            if (SemanticLocationsBySet.TryGetValue(requiredSetDefinitionId, out string[] locations))
                return locations[sceneIndex % locations.Length];
            return $"{requiredSetDefinitionId}-location";
        }

        private string ChooseArchetype(string preferred) =>
            string.IsNullOrWhiteSpace(preferred) ? Pick(FallbackArchetypes) : preferred;

        private static string BuildSceneTitle(ScreenplayProject screenplay, int number) =>
            number == 1 ? "The Discovery" : number % 2 == 0 ? "Rising Trouble" : "The Decision";

        private static ScreenplayEmotion ReactionFor(string genreId) => genreId switch
        {
            "comedy" => ScreenplayEmotion.Happy,
            "romance" => ScreenplayEmotion.Romantic,
            "horror" => ScreenplayEmotion.Afraid,
            "action" => ScreenplayEmotion.Confident,
            _ => ScreenplayEmotion.Nervous
        };

        private T Pick<T>(IReadOnlyList<T> values) => values[_random.Next(0, values.Count)];

        private static string Display(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return "Unknown";
            string[] words = id.Split('-');
            for (int i = 0; i < words.Length; i++)
                if (words[i].Length > 0)
                    words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
            return string.Join(" ", words);
        }
    }
}
