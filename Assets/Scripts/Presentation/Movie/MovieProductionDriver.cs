using System.Collections.Generic;
using UnityEngine;
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

        public IMovieProductionService ProductionService => EnsureCoordinator();
        public MovieProductionCoordinator Coordinator => EnsureCoordinator();

        private MovieProductionCoordinator EnsureCoordinator()
        {
            if (_coordinator == null)
            {
                if (_timeDriver == null) _timeDriver = FindAnyObjectByType<SimulationTimeDriver>();
                if (_worldRouter == null) _worldRouter = FindAnyObjectByType<StudioWorldRouter>();
                if (_economyDriver == null) _economyDriver = FindAnyObjectByType<StudioEconomyDriver>();

                if (_timeDriver != null && _worldRouter != null)
                {
                    _coordinator = new MovieProductionCoordinator(
                        _timeDriver.TimeService,
                        _worldRouter,
                        _genres,
                        _economyDriver?.FinanceService);
                }
            }
            return _coordinator;
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
        }

        private void OnDestroy()
        {
            _coordinator?.Dispose();
        }
    }
}
