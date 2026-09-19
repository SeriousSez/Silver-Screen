using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Movie
{
    public interface IMovieProductionService
    {
        StudioProductionSlate Slate { get; }
        MovieProject ActiveMovie { get; }
        MovieScene ActiveScene { get; }
        MovieTake ActiveTake { get; }
        string ActiveSlateMovieTitle { get; }
        int? ActiveSlateSceneNumber { get; }
        int? ActiveSlateTakeNumber { get; }
        string StatusMessage { get; }

        IReadOnlyList<GenreDefinition> AvailableGenres { get; }
        IReadOnlyList<BudgetTier> AvailableBudgets { get; }

        MovieCreationResult CreateMovie(string title, string genreId, int budget, string protagonistName, List<string> supportingNames);

        bool CanAssignActorToRole(MovieProject project, MovieRole role, Employee actor);
        void AssignActorToRole(MovieProject project, MovieRole role, Employee actor);

        bool CanAssignDirector(MovieProject project, Employee director);
        void AssignDirector(MovieProject project, Employee director);

        event Action<MovieProject> OnActiveMovieChanged;
        event Action<string> OnProductionNotification;
    }
}

