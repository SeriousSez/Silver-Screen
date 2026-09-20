using System;
using System.Collections.Generic;
using SilverScreen.Domain.Writing;

namespace SilverScreen.Domain.Movie
{
    public interface IMovieProductionService
    {
        StudioProductionSlate Slate { get; }
        MovieProject ActiveMovie { get; }
        MovieScene ActiveScene { get; }
        MovieTake ActiveTake { get; }
        ProductionEnvironmentResolution ActiveEnvironment { get; }
        MovieScene NextFilmableScene { get; }
        bool HasRemainingScenes { get; }
        string ActiveSlateMovieTitle { get; }
        int? ActiveSlateSceneNumber { get; }
        int? ActiveSlateTakeNumber { get; }
        string StatusMessage { get; }
        ProductionPhase CurrentProductionPhase { get; }
        bool CanKeepTake { get; }
        bool CanShootAgain { get; }

        IReadOnlyList<GenreDefinition> AvailableGenres { get; }
        IReadOnlyList<BudgetTier> AvailableBudgets { get; }

        MovieCreationResult CreateMovie(string title, string genreId, int budget, string protagonistName, List<string> supportingNames);
        ScreenplayGreenlightResult GreenlightScreenplay(ScreenplayProject screenplay);
        bool HasProductionForScreenplay(string screenplayId);

        bool CanAssignActorToRole(MovieProject project, MovieRole role, Employee actor);
        void AssignActorToRole(MovieProject project, MovieRole role, Employee actor);

        bool CanAssignDirector(MovieProject project, Employee director);
        void AssignDirector(MovieProject project, Employee director);

        bool SetProductionControlMode(MovieProject project, ProductionControlMode mode);
        bool KeepTake(string takeId);
        bool ShootAgain();
        bool RetryUnresolvedEnvironment();
        void HandleOwnedFacilityRemoved(string facilityId);

        event Action<MovieProject> OnActiveMovieChanged;
        event Action<string> OnProductionNotification;
    }
}

