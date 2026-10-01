using System;
using System.Collections.Generic;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Writing;

namespace SilverScreen.Domain.Tutorial
{
    /// <summary>Event adapter over existing authoritative objects. No frame or tick polling.</summary>
    public sealed class TutorialGameEvents : IDisposable
    {
        private readonly TutorialSession _tutorial;
        private readonly SilverScreen.Domain.Buildings.IOperationalBuildings _buildings;
        private bool _builderHired;
        private readonly StudioProductionSlate _slate;
        private readonly ScreenplayWritingCoordinator _writing;
        private readonly StudioAnnouncementQueue _announcements;
        private readonly IStrategicClock _clock;
        private readonly HashSet<MovieProject> _movies = new HashSet<MovieProject>();
        private readonly HashSet<ScreenplayProject> _screenplays = new HashSet<ScreenplayProject>();
        private readonly HashSet<string> _hires = new HashSet<string>();
        private readonly HashSet<(AnnouncementType, string)> _conditions = new HashSet<(AnnouncementType, string)>();
        private bool _evaluating;

        public TutorialGameEvents(TutorialSession tutorial, SilverScreen.Domain.Buildings.IOperationalBuildings buildings,
            StudioProductionSlate slate, StudioAnnouncementQueue announcements, IStrategicClock clock,
            ScreenplayWritingCoordinator writing = null)
        {
            _tutorial = tutorial; _buildings = buildings; _slate = slate; _announcements = announcements; _clock = clock; _writing = writing;
            _tutorial.Changed += Evaluate;
            _buildings.Established += OnEstablished;
            _slate.OnProjectAdded += ObserveMovie;
            foreach (var movie in _slate.AllProjects) ObserveMovie(movie);
            if (_writing != null)
            {
                _writing.ScreenplayAdded += ObserveScreenplay;
                foreach (var screenplay in _writing.Library) ObserveScreenplay(screenplay);
            }
            Evaluate();
        }
        public void EmployeeHired(Employee employee)
        { if (employee == null) return; if (employee.Role == EmployeeRole.ConstructionWorker) _builderHired = true; if (_hires.Add(employee.Id)) Evaluate(); }
        private void OnEstablished(string id) => Evaluate();
        public void ObserveScreenplay(ScreenplayProject screenplay)
        {
            if (screenplay == null || !_screenplays.Add(screenplay)) return;
            screenplay.Changed += ScreenplayChanged; Evaluate();
        }
        private void ScreenplayChanged(ScreenplayProject screenplay) => Evaluate();
        private void ObserveMovie(MovieProject movie)
        {
            if (movie == null || !_movies.Add(movie)) return;
            movie.OnProjectUpdated += MovieChanged; MovieChanged(movie);
        }
        private void MovieChanged(MovieProject movie)
        {
            bool unfinished = !movie.IsProductionComplete && !movie.ReleaseDate.HasValue;
            Condition(AnnouncementType.ProductionNeedsActor, movie, unfinished && movie.CastRoles.Count > 0 && !movie.AllRolesCast, AnnouncementPriority.Important);
            Condition(AnnouncementType.ProductionNeedsDirector, movie, unfinished && !movie.HasDirector, AnnouncementPriority.Important);
            Condition(AnnouncementType.ProductionReady, movie, movie.ReadyForFilming, AnnouncementPriority.Normal);
            Condition(AnnouncementType.ProductionCompleted, movie, movie.CanRelease, AnnouncementPriority.Important);
            Evaluate();
        }
        private void Condition(AnnouncementType type, MovieProject movie, bool active, AnnouncementPriority priority)
        {
            var key = (type, movie.Id);
            if (!active)
            {
                if (_conditions.Remove(key)) _announcements.Resolve(type, movie.Id);
                return;
            }
            // Edge-triggered: progress notifications never schedule repeated PA speech.
            if (_conditions.Add(key)) _announcements.Request(new AnnouncementRequest(type, priority, movie.Id, _clock.Now.Seconds));
        }
        private void Evaluate()
        {
            if (_evaluating || !_tutorial.IsActive) return;
            _evaluating = true;
            try
            {
                string before;
                do
                {
                    before = _tutorial.CurrentStep?.Id;
                    var facts = new HashSet<string>();
                    if (_builderHired) facts.Add("construction-worker.hired");
                    if (_buildings.IsEstablished(StarterFeatureIds.Headquarters)) facts.Add("headquarters.established");
                    if (_buildings.IsEstablished(StarterFeatureIds.Casting)) facts.Add("casting.established");
                    if (_buildings.IsEstablished(StarterFeatureIds.Stage)) facts.Add("stage.established");
                    if (_hires.Count > 0) facts.Add("employee.hired");
                    foreach (var screenplay in _screenplays)
                        if (screenplay.ContentStatus == ScreenplayContentStatus.Ready && screenplay.Status == ScreenplayStatus.Completed &&
                            _tutorial.TrackScreenplay(screenplay.Id)) facts.Add("screenplay.finalized");
                    foreach (var movie in _movies)
                    {
                        if (!_tutorial.TrackMovie(movie.Id, movie.SourceScreenplayId)) continue;
                        if (movie.ReadyForFilming || movie.IsFilming || movie.IsProductionComplete || movie.ReleaseDate.HasValue) facts.Add("movie.prepared");
                        if (movie.CanRelease || movie.ReleaseDate.HasValue) facts.Add("movie.completed");
                        if (movie.ReleaseDate.HasValue) facts.Add("movie.released");
                    }
                    if (_tutorial.CurrentStep != null && facts.Contains(_tutorial.CurrentStep.CompletionConditionId))
                        _tutorial.CompleteCondition(_tutorial.CurrentStep.CompletionConditionId);
                } while (_tutorial.IsActive && _tutorial.CurrentStep.Id != before);
            }
            finally { _evaluating = false; }
        }
        public void Dispose()
        {
            _tutorial.Changed -= Evaluate; _buildings.Established -= OnEstablished; _slate.OnProjectAdded -= ObserveMovie;
            if (_writing != null) _writing.ScreenplayAdded -= ObserveScreenplay;
            foreach (var movie in _movies) movie.OnProjectUpdated -= MovieChanged;
            foreach (var screenplay in _screenplays) screenplay.Changed -= ScreenplayChanged;
        }
    }
}
