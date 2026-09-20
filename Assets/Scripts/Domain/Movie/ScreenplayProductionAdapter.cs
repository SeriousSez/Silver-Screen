using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Writing;

namespace SilverScreen.Domain.Movie
{
    public enum ScreenplayGreenlightFailure
    {
        None,
        InvalidScreenplay,
        NotReady,
        InvalidContent,
        AlreadyGreenlit,
        ActiveProductionInProgress
    }

    public sealed class ScreenplayGreenlightResult
    {
        private ScreenplayGreenlightResult(MovieProject movie, ScreenplayGreenlightFailure failure, string message)
        {
            Movie = movie;
            Failure = failure;
            Message = message ?? string.Empty;
        }

        public MovieProject Movie { get; }
        public ScreenplayGreenlightFailure Failure { get; }
        public string Message { get; }
        public bool Succeeded => Movie != null && Failure == ScreenplayGreenlightFailure.None;

        public static ScreenplayGreenlightResult Success(MovieProject movie) =>
            new ScreenplayGreenlightResult(movie, ScreenplayGreenlightFailure.None, "Production created.");

        public static ScreenplayGreenlightResult Failed(ScreenplayGreenlightFailure failure, string message) =>
            new ScreenplayGreenlightResult(null, failure, message);
    }

    public sealed class ScreenplayProductionAdapter
    {
        public ScreenplayGreenlightResult Adapt(
            ScreenplayProject screenplay,
            SimulationDateTime createdDate,
            string genreDisplayName)
        {
            ScreenplayGreenlightResult validation = Validate(screenplay);
            if (validation != null) return validation;

            string movieId = Guid.NewGuid().ToString();
            var movie = new MovieProject(
                movieId,
                screenplay.Title,
                screenplay.PrimaryGenreId,
                genreDisplayName,
                0,
                createdDate,
                budgetTierId: "unbudgeted",
                sourceScreenplayId: screenplay.Id);

            var roleIdsByCharacterId = new Dictionary<string, string>();
            for (int index = 0; index < screenplay.Characters.Count; index++)
            {
                ScreenplayCharacter source = screenplay.Characters[index];
                string roleId = $"{movieId}:role:{index + 1}";
                var prominence = source.Role == ScreenplayCharacterRole.Protagonist
                    ? MovieRoleProminence.Lead
                    : MovieRoleProminence.Supporting;
                var role = new MovieRole(
                    roleId,
                    prominence,
                    source.Name,
                    source.Description,
                    source.Id,
                    source.Role.ToString().ToLowerInvariant(),
                    source.ArchetypeId);
                movie.AddCastRole(role);
                roleIdsByCharacterId.Add(source.Id, role.Id);
            }

            foreach (ScreenplayScene source in screenplay.Scenes)
            {
                string sceneId = $"{movieId}:scene:{source.SceneNumber}";
                var scene = new MovieScene(
                    sceneId,
                    source.SceneNumber,
                    source.LocationId,
                    source.Title,
                    sourceScreenplaySceneId: source.Id,
                    locationTypeId: source.LocationType.ToString().ToLowerInvariant(),
                    timeOfDayId: source.TimeOfDay.ToString().ToLowerInvariant(),
                    requiredSetDefinitionId: source.RequiredSetDefinitionId);
                if (!movie.AddScene(scene))
                    return ScreenplayGreenlightResult.Failed(
                        ScreenplayGreenlightFailure.InvalidContent,
                        $"Scene {source.SceneNumber} could not be adapted.");

                foreach (string characterId in source.ParticipatingCharacterIds)
                    movie.AddCharacterToScene(scene.Id, roleIdsByCharacterId[characterId]);

                foreach (ScreenplayBeat sourceBeat in source.Beats)
                {
                    var beat = new ScreenplayBeat(
                        sourceBeat.Id,
                        sourceBeat.Order,
                        sourceBeat.BeatType,
                        sourceBeat.Content,
                        MapOptionalRoleId(sourceBeat.PerformingCharacterId, roleIdsByCharacterId),
                        MapOptionalRoleId(sourceBeat.TargetCharacterId, roleIdsByCharacterId),
                        sourceBeat.EmotionalIntention,
                        intendedIntensity: sourceBeat.IntendedIntensity,
                        blockingIntentionId: sourceBeat.BlockingIntentionId);
                    if (!movie.AddBeatToScene(scene.Id, beat))
                        return ScreenplayGreenlightResult.Failed(
                            ScreenplayGreenlightFailure.InvalidContent,
                            $"Beat {sourceBeat.Order} in scene {source.SceneNumber} could not be adapted.");
                    if (!movie.AddShotToScene(scene.Id, CreateShot(scene, beat)))
                        return ScreenplayGreenlightResult.Failed(
                            ScreenplayGreenlightFailure.InvalidContent,
                            $"Shot {sourceBeat.Order} in scene {source.SceneNumber} could not be adapted.");
                }
            }

            return ScreenplayGreenlightResult.Success(movie);
        }

        private static ScreenplayGreenlightResult Validate(ScreenplayProject screenplay)
        {
            if (screenplay == null)
                return ScreenplayGreenlightResult.Failed(ScreenplayGreenlightFailure.InvalidScreenplay, "No screenplay was selected.");
            if (screenplay.Status != ScreenplayStatus.Completed ||
                screenplay.ContentStatus != ScreenplayContentStatus.Ready ||
                screenplay.EvaluationStatus != ScreenplayEvaluationStatus.Ready ||
                screenplay.Evaluation == null)
                return ScreenplayGreenlightResult.Failed(
                    ScreenplayGreenlightFailure.NotReady,
                    "The screenplay must be completed, finalized, and evaluated before greenlight.");
            if (screenplay.Characters.Count == 0 || screenplay.Scenes.Count == 0)
                return ScreenplayGreenlightResult.Failed(ScreenplayGreenlightFailure.InvalidContent, "The screenplay has no adaptable content.");

            var characterIds = new HashSet<string>();
            foreach (ScreenplayCharacter character in screenplay.Characters)
                if (character == null || !characterIds.Add(character.Id))
                    return ScreenplayGreenlightResult.Failed(ScreenplayGreenlightFailure.InvalidContent, "The screenplay has invalid characters.");

            foreach (ScreenplayScene scene in screenplay.Scenes)
            {
                if (scene == null || scene.Beats.Count == 0)
                    return ScreenplayGreenlightResult.Failed(ScreenplayGreenlightFailure.InvalidContent, "The screenplay has an invalid scene.");
                var participants = new HashSet<string>();
                foreach (string id in scene.ParticipatingCharacterIds)
                    if (!characterIds.Contains(id) || !participants.Add(id))
                        return ScreenplayGreenlightResult.Failed(ScreenplayGreenlightFailure.InvalidContent, "A scene has invalid character participation.");
                foreach (ScreenplayBeat beat in scene.Beats)
                    if (beat == null || !ReferencesParticipant(beat.PerformingCharacterId, participants) ||
                        !ReferencesParticipant(beat.TargetCharacterId, participants))
                        return ScreenplayGreenlightResult.Failed(ScreenplayGreenlightFailure.InvalidContent, "A scene beat has an invalid character reference.");
            }
            return null;
        }

        private static bool ReferencesParticipant(string characterId, HashSet<string> participants) =>
            characterId == null || participants.Contains(characterId);

        private static string MapOptionalRoleId(string sourceId, Dictionary<string, string> roleIdsByCharacterId) =>
            sourceId == null ? null : roleIdsByCharacterId[sourceId];

        private static SceneShot CreateShot(MovieScene scene, ScreenplayBeat beat)
        {
            ShotType shotType;
            string subjectId;
            switch (beat.BeatType)
            {
                case ScreenplayBeatType.Dialogue:
                    shotType = ShotType.Medium;
                    subjectId = beat.PerformingCharacterId;
                    break;
                case ScreenplayBeatType.Reaction:
                    shotType = ShotType.CloseUp;
                    subjectId = beat.PerformingCharacterId;
                    break;
                default:
                    shotType = ShotType.Wide;
                    subjectId = null;
                    break;
            }
            return new SceneShot($"{scene.Id}:shot:{beat.Id}", beat.Order, shotType, subjectId, beat.Id);
        }
    }
}
