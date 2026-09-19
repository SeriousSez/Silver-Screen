using System.Collections.Generic;
using UnityEngine;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Finance;
using SilverScreen.Presentation.SimulationTime;

namespace SilverScreen.Presentation.Movie
{
    public class MovieProductionDriver : MonoBehaviour
    {
        [SerializeField] private SimulationTimeDriver _timeDriver;
        [SerializeField] private StudioWorldRouter _worldRouter;
        [SerializeField] private StudioEconomyDriver _economyDriver;
        [SerializeField] private List<GenreDefinition> _genres = new List<GenreDefinition>();

        private MovieProductionCoordinator _coordinator;
        private MovieReleaseService _releaseService;
        private IStudioFinanceService _financeService;

        public IMovieProductionService ProductionService => EnsureCoordinator();
        public MovieProductionCoordinator Coordinator => EnsureCoordinator();
        public IMovieReleaseService ReleaseService => EnsureReleaseService();

        private void EnsureDependencies()
        {
            if (_timeDriver == null) _timeDriver = FindAnyObjectByType<SimulationTimeDriver>();
            if (_worldRouter == null) _worldRouter = FindAnyObjectByType<StudioWorldRouter>();
            if (_economyDriver == null) _economyDriver = FindAnyObjectByType<StudioEconomyDriver>();

            if (_financeService == null && _timeDriver != null)
            {
                _financeService = _economyDriver?.FinanceService ??
                    new StudioFinances(_timeDriver.TimeService.CurrentTime);
            }
        }

        private MovieProductionCoordinator EnsureCoordinator()
        {
            if (_coordinator == null)
            {
                EnsureDependencies();

                if (_timeDriver != null && _worldRouter != null)
                {
                    _coordinator = new MovieProductionCoordinator(
                        _timeDriver.TimeService,
                        _worldRouter,
                        _genres,
                        _financeService);
                    _worldRouter.BindProductionService(_coordinator);
                }
            }
            return _coordinator;
        }

        private MovieReleaseService EnsureReleaseService()
        {
            if (_releaseService == null)
            {
                EnsureDependencies();
                if (_timeDriver != null && _financeService != null)
                {
                    _releaseService = new MovieReleaseService(_timeDriver.TimeService, _financeService);
                }
            }

            return _releaseService;
        }

        public void SetGenres(IEnumerable<GenreDefinition> genres)
        {
            _genres.Clear();
            if (genres != null) _genres.AddRange(genres);
            if (_coordinator != null) _coordinator.SetGenres(_genres);
        }

        private void Start()
        {
            EnsureCoordinator();
            EnsureReleaseService();
        }

        private void OnDestroy()
        {
            _coordinator?.Dispose();
            _releaseService?.Dispose();
        }
    }
}
