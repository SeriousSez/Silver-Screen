using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Movie
{
    public class StudioProductionSlate
    {
        private readonly List<MovieProject> _projects = new List<MovieProject>();
        private MovieProject _activeMovie;

        public IReadOnlyList<MovieProject> AllProjects => _projects;
        public MovieProject ActiveMovie => _activeMovie;

        public event Action<MovieProject> OnActiveMovieChanged;
        public event Action<MovieProject> OnProjectAdded;

        public void AddProject(MovieProject project, bool setAsActive = true)
        {
            if (project == null || _projects.Contains(project)) return;

            _projects.Add(project);
            OnProjectAdded?.Invoke(project);

            if (setAsActive || _activeMovie == null)
            {
                SetActiveMovie(project);
            }
        }

        public void SetActiveMovie(MovieProject project)
        {
            if (_activeMovie == project) return;
            _activeMovie = project;
            OnActiveMovieChanged?.Invoke(_activeMovie);
        }

        public void ClearActiveMovie()
        {
            if (_activeMovie == null) return;
            _activeMovie = null;
            OnActiveMovieChanged?.Invoke(null);
        }
    }
}
