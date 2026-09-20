using System;
using System.Collections.Generic;
using SilverScreen.Domain.Writing;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Movie
{
    public class MovieProductionCoordinator : IMovieProductionService
    {
        private readonly ISimulationTimeService _timeService;
        private readonly IStudioWorldRouter _router;
        private readonly StudioProductionSlate _slate;
        private readonly IStudioFinanceService _finances;
        private readonly IMovieQualityCalculator _qualityCalculator;
        private readonly List<GenreDefinition> _genres = new List<GenreDefinition>();

        private string _statusMessage = "No active project";
        private const int FilmingDurationMinutes = 480; // 8 hours
        private int _filmingMinutesElapsed;
        private string _routingFailureMessage;
        private readonly HashSet<string> _actorsAtWaitingStations = new HashSet<string>();
        private readonly HashSet<string> _actorsAtSceneMarks = new HashSet<string>();
        private readonly List<RequiredActor> _requiredActors = new List<RequiredActor>();
        private MovieScene _activeScene;
        private MovieTake _activeTake;
        private bool _beatSequenceActive;

        public StudioProductionSlate Slate => _slate;
        public MovieProject ActiveMovie => _slate.ActiveMovie;
        public MovieScene ActiveScene => _activeScene;
        public MovieTake ActiveTake => _activeTake;
        public MovieScene NextFilmableScene => ActiveMovie?.GetNextFilmableScene();
        public bool HasRemainingScenes => ActiveMovie?.HasUnfinishedScenes ?? false;
        public string ActiveSlateMovieTitle => ActiveMovie?.Title;
        public int? ActiveSlateSceneNumber => _activeScene?.SceneNumber;
        public int? ActiveSlateTakeNumber => _activeTake?.TakeNumber;
        public string StatusMessage => _statusMessage;
        public ProductionPhase CurrentProductionPhase { get; private set; } = ProductionPhase.Inactive;
        public bool CanKeepTake =>
            CurrentProductionPhase == ProductionPhase.AwaitingTakeDecision &&
            _activeScene != null &&
            _activeScene.Takes.Count > 0;
        public bool CanShootAgain => CanKeepTake;
        public IReadOnlyList<GenreDefinition> AvailableGenres => _genres;
        public IReadOnlyList<BudgetTier> AvailableBudgets => BudgetTier.DefaultTiers;

        public event Action<MovieProject> OnActiveMovieChanged;
        public event Action<string> OnProductionNotification;
        public event Action<ProductionPhase> OnProductionPhaseChanged;

        public MovieProductionCoordinator(
            ISimulationTimeService timeService,
            IStudioWorldRouter router,
            IEnumerable<GenreDefinition> genres = null,
            IStudioFinanceService finances = null,
            IMovieQualityCalculator qualityCalculator = null)
        {
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _router = router ?? throw new ArgumentNullException(nameof(router));
            _finances = finances ?? new StudioFinances(_timeService.CurrentTime);
            _qualityCalculator = qualityCalculator ?? new MovieQualityCalculator();
            _slate = new StudioProductionSlate();

            if (genres != null)
            {
                _genres.AddRange(genres);
            }

            _slate.OnActiveMovieChanged += HandleSlateActiveMovieChanged;
            _timeService.OnMinutePassed += HandleMinutePassed;

            UpdateStatus();
        }

        public void SetGenres(IEnumerable<GenreDefinition> genres)
        {
            _genres.Clear();
            if (genres != null) _genres.AddRange(genres);
        }

        public bool SetProductionControlMode(MovieProject project, ProductionControlMode mode)
        {
            if (project == null || project != ActiveMovie ||
                CurrentProductionPhase == ProductionPhase.Filming ||
                CurrentProductionPhase == ProductionPhase.Slating ||
                CurrentProductionPhase == ProductionPhase.AwaitingTakeDecision)
            {
                return false;
            }

            bool changed = project.SetProductionControlMode(mode);
            if (changed)
            {
                UpdateStatus();
                OnActiveMovieChanged?.Invoke(project);
            }
            return changed;
        }

        public void Dispose()
        {
            if (_slate != null)
            {
                _slate.OnActiveMovieChanged -= HandleSlateActiveMovieChanged;
            }

            if (_timeService != null)
            {
                _timeService.OnMinutePassed -= HandleMinutePassed;
            }
        }

        private void HandleSlateActiveMovieChanged(MovieProject movie)
        {
            ClearActiveSceneAndTake();
            _requiredActors.Clear();
            _actorsAtWaitingStations.Clear();
            _actorsAtSceneMarks.Clear();
            SetProductionPhase(ProductionPhase.Inactive);
            _filmingMinutesElapsed = 0;
            UpdateStatus();
            OnActiveMovieChanged?.Invoke(movie);
        }

        public MovieCreationResult CreateMovie(
            string title,
            string genreId,
            int budget,
            string protagonistName,
            List<string> supportingNames)
        {
            if (ActiveMovie != null && !IsTerminal(ActiveMovie.CurrentState))
            {
                return MovieCreationResult.Failed(
                    MovieCreationFailure.ActiveProductionInProgress,
                    $"Cannot create a new movie while \"{ActiveMovie.Title}\" is in production.");
            }

            string movieTitle = string.IsNullOrWhiteSpace(title) ? "Untitled Project" : title.Trim();
            string movieGenreId = string.IsNullOrWhiteSpace(genreId) ? "drama" : genreId.Trim();
            var genreDefinition = _genres.Find(g => g != null && string.Equals(g.Id, movieGenreId, StringComparison.OrdinalIgnoreCase));
            string movieGenreDisplayName = genreDefinition != null ? genreDefinition.DisplayName : FormatGenreDisplayName(movieGenreId);
            int movieBudget = budget > 0 ? budget : 50000;
            Money budgetCost = Money.FromDollars(movieBudget);

            if (!_finances.CanAfford(budgetCost))
            {
                return MovieCreationResult.Failed(
                    MovieCreationFailure.InsufficientFunds,
                    $"Insufficient studio funds. {budgetCost} is required, but only {_finances.CurrentCash} is available.");
            }

            var movie = new MovieProject(
                id: Guid.NewGuid().ToString(),
                title: movieTitle,
                genreId: movieGenreId,
                genreDisplayName: movieGenreDisplayName,
                budget: movieBudget,
                createdDate: _timeService.CurrentTime,
                budgetTierId: BudgetTier.GetIdForAmount(movieBudget)
            );

            // Add required Protagonist role
            string protName = string.IsNullOrWhiteSpace(protagonistName) ? "Lead Character" : protagonistName.Trim();
            movie.AddRole(new MovieRole(Guid.NewGuid().ToString(), MovieRoleType.Protagonist, protName));

            // Add optional Supporting roles (0 to 2)
            if (supportingNames != null)
            {
                int count = Math.Min(supportingNames.Count, 2);
                for (int i = 0; i < count; i++)
                {
                    string suppName = string.IsNullOrWhiteSpace(supportingNames[i]) ? $"Supporting Character {i + 1}" : supportingNames[i].Trim();
                    movie.AddRole(new MovieRole(Guid.NewGuid().ToString(), MovieRoleType.Supporting, suppName));
                }
            }

            if (!_finances.TryRecordExpense(
                    budgetCost,
                    FinancialTransactionCategory.ProductionBudget,
                    _timeService.CurrentTime,
                    $"Production: {movie.Title}",
                    movie.Id))
            {
                return MovieCreationResult.Failed(
                    MovieCreationFailure.InsufficientFunds,
                    $"Insufficient studio funds. {budgetCost} is required, but only {_finances.CurrentCash} is available.");
            }

            _slate.AddProject(movie, setAsActive: true);
            _routingFailureMessage = null;
            UpdateStatus();
            OnActiveMovieChanged?.Invoke(movie);
            return MovieCreationResult.Success(movie);
        }

        private static bool IsTerminal(MovieProductionState state)
        {
            return state == MovieProductionState.Completed || state == MovieProductionState.Released;
        }

        private static string FormatGenreDisplayName(string genreId)
        {
            if (string.IsNullOrWhiteSpace(genreId)) return "Drama";
            string trimmed = genreId.Trim();
            return char.ToUpperInvariant(trimmed[0]) + trimmed.Substring(1);
        }

        public bool CanAssignActorToRole(MovieProject project, MovieRole role, Employee actor)
        {
            if (project == null || role == null || actor == null) return false;
            if (actor.Role != EmployeeRole.Actor) return false;
            if (project.CurrentState == MovieProductionState.Completed || project.CurrentState == MovieProductionState.Released) return false;
            if (project.CurrentState == MovieProductionState.Filming) return false;

            // Enforce: an actor may only occupy one role in the same movie
            if (project.IsActorAssignedAnyRole(actor)) return false;

            return true;
        }

        public void AssignActorToRole(MovieProject project, MovieRole role, Employee actor)
        {
            if (!CanAssignActorToRole(project, role, actor)) return;

            project.AssignActorToRole(role.Id, actor);

            if (project.CurrentState == MovieProductionState.Draft)
            {
                project.SetState(MovieProductionState.Casting);
            }

            var intent = new EmployeeIntent(
                EmployeeIntentPurpose.ReportToCasting,
                $"Walking to Casting Office ({role.CharacterName})",
                "CastingOffice");

            var routeResult = _router.SendEmployeeToBuilding(actor, BuildingType.CastingOffice, intent, () =>
            {
                OnActorArrivedAtCasting(project, role, actor);
            });

            if (routeResult != StudioRouteResult.Started)
            {
                role.ClearActor();
                actor.SetState(EmployeeState.Idle);
                actor.SetIntent(EmployeeIntent.None);
                _routingFailureMessage = $"Could not send {actor.Name} to the Casting Office ({routeResult}).";
                UpdateStatus();
                OnActiveMovieChanged?.Invoke(project);
                OnProductionNotification?.Invoke(_routingFailureMessage);
                return;
            }

            _routingFailureMessage = null;
            UpdateStatus();
            OnActiveMovieChanged?.Invoke(project);
        }

        private void OnActorArrivedAtCasting(MovieProject project, MovieRole role, Employee actor)
        {
            actor.SetState(EmployeeState.Casting);
            actor.SetIntent(new EmployeeIntent(
                EmployeeIntentPurpose.PerformTask,
                $"Casting — {project.Title} ({role.CharacterName})",
                "CastingOffice"));

            role.StartCasting();

            UpdateStatus();
            OnActiveMovieChanged?.Invoke(project);
        }

        public bool CanAssignDirector(MovieProject project, Employee director)
        {
            if (project == null || director == null) return false;
            if (director.Role != EmployeeRole.Director) return false;
            if (project.CurrentState == MovieProductionState.Completed || project.CurrentState == MovieProductionState.Released) return false;
            if (project.CurrentState == MovieProductionState.Filming) return false;

            return true;
        }

        public void AssignDirector(MovieProject project, Employee director)
        {
            if (!CanAssignDirector(project, director)) return;

            project.AssignDirector(director);

            // If all casting was already finished, transition to ReadyToFilm
            if (project.ReadyForFilming && project.CurrentState == MovieProductionState.Casting)
            {
                TransitionToReadyToFilm(project);
            }

            UpdateStatus();
            OnActiveMovieChanged?.Invoke(project);
        }

        private void HandleMinutePassed(SimulationDateTime dateTime)
        {
            var movie = ActiveMovie;
            if (movie == null) return;

            if (movie.CurrentState == MovieProductionState.Casting)
            {
                bool anyCastingActive = false;
                foreach (var role in movie.Roles)
                {
                    if (role.IsCastingActive && !role.CastingCompleted)
                    {
                        anyCastingActive = true;
                        role.AdvanceCastingMinute();

                        if (role.CastingCompleted && role.AssignedActor != null)
                        {
                            role.AssignedActor.SetState(EmployeeState.Working);
                            role.AssignedActor.SetIntent(new EmployeeIntent(
                                EmployeeIntentPurpose.None,
                                $"Waiting for production — {movie.Title}",
                                "CastingOffice"));
                        }
                    }
                }

                if (movie.ReadyForFilming)
                {
                    TransitionToReadyToFilm(movie);
                }
                else if (anyCastingActive)
                {
                    UpdateStatus();
                    OnActiveMovieChanged?.Invoke(movie);
                }
            }
            else if (CurrentProductionPhase == ProductionPhase.Filming && movie.CurrentState == MovieProductionState.Filming)
            {
                if (_beatSequenceActive) return;

                _filmingMinutesElapsed++;
                movie.SetProgress(CalculateProductionProgress(movie));

                UpdateStatus();

                if (_filmingMinutesElapsed >= FilmingDurationMinutes)
                {
                    CompleteFilming(movie);
                }
            }
        }

        private void TransitionToReadyToFilm(MovieProject movie)
        {
            _routingFailureMessage = null;
            _actorsAtWaitingStations.Clear();
            _actorsAtSceneMarks.Clear();
            _requiredActors.Clear();
            SetProductionPhase(ProductionPhase.MovingToStations);
            movie.SetState(MovieProductionState.ReadyToFilm);
            movie.SetDirectorArrivedAtStage(false);

            foreach (var role in movie.Roles)
            {
                role.ResetForProduction();
            }

            if (!TryActivateScene(movie))
            {
                FailProduction(movie, "No eligible movie scene is available for filming.");
                return;
            }

            if (!TryResolveRequiredActors(movie, out string actorResolutionFailure))
            {
                FailProduction(movie, actorResolutionFailure);
                return;
            }

            var stageIntent = new EmployeeIntent(
                EmployeeIntentPurpose.ReportToStage,
                $"Walking to Sound Stage 1 ({movie.Title})",
                "SoundStage");

            if (movie.AssignedDirector != null)
            {
                var director = movie.AssignedDirector;
                var routeResult = _router.SendEmployeeToBuilding(
                    director,
                    BuildingType.SoundStage,
                    stageIntent,
                    () => RouteToProductionStation(
                        movie,
                        director,
                        ProductionStationType.Director,
                        () =>
                        {
                            movie.SetDirectorArrivedAtStage(true);
                            director.SetState(EmployeeState.Working);
                            director.SetIntent(new EmployeeIntent(
                                EmployeeIntentPurpose.PerformTask,
                                $"At director station — {movie.Title}",
                                "SoundStage"));
                            CheckProductionStationsReady(movie);
                        }));

                if (routeResult != StudioRouteResult.Started)
                {
                    HandleStageRoutingFailure(movie, director, routeResult);
                    return;
                }
            }

            int actorStationIndex = 0;
            foreach (var participant in _requiredActors)
            {
                var capturedParticipant = participant;
                var actor = participant.Actor;
                var stationType = GetActorStation(actorStationIndex++);
                var routeResult = _router.SendEmployeeToBuilding(
                    actor,
                    BuildingType.SoundStage,
                    stageIntent,
                    () => RouteToProductionStation(
                        movie,
                        actor,
                        stationType,
                        () =>
                        {
                            _actorsAtWaitingStations.Add(actor.Id);
                            actor.SetState(EmployeeState.Working);
                            actor.SetIntent(new EmployeeIntent(
                                EmployeeIntentPurpose.PerformTask,
                                $"Waiting at {stationType} — {movie.Title} ({capturedParticipant.CharacterNames})",
                                "SoundStage"));
                            CheckProductionStationsReady(movie);
                        }));

                if (routeResult != StudioRouteResult.Started)
                {
                    HandleStageRoutingFailure(movie, actor, routeResult);
                    return;
                }
            }

            UpdateStatus();
            OnActiveMovieChanged?.Invoke(movie);
        }

        private void BeginBeatSequence(MovieProject movie)
        {
            _beatSequenceActive = true;
            var routeResult = _router.StartBeatSequence(
                movie,
                _activeScene,
                _activeTake,
                () =>
                {
                    if (movie == ActiveMovie &&
                        CurrentProductionPhase == ProductionPhase.Filming &&
                        _activeScene != null &&
                        _activeTake != null)
                    {
                        _beatSequenceActive = false;
                        _filmingMinutesElapsed = FilmingDurationMinutes;
                        movie.SetProgress(CalculateProductionProgress(movie));
                        CompleteFilming(movie);
                    }
                },
                failure =>
                {
                    _beatSequenceActive = false;
                    FailProduction(movie, $"The screenplay performance could not complete ({failure}).");
                });

            if (routeResult != StudioRouteResult.Started)
            {
                _beatSequenceActive = false;
                FailProduction(movie, $"The screenplay performance could not start ({routeResult}).");
            }
        }

        private void RouteToProductionStation(
            MovieProject movie,
            Employee employee,
            ProductionStationType stationType,
            Action onArrival)
        {
            var stationIntent = new EmployeeIntent(
                EmployeeIntentPurpose.ReportToStage,
                $"Moving to {stationType} — {movie.Title}",
                $"SoundStage:{stationType}");

            var routeResult = _router.SendEmployeeToProductionStation(
                employee,
                BuildingType.SoundStage,
                stationType,
                stationIntent,
                onArrival);

            if (routeResult != StudioRouteResult.Started)
            {
                HandleStageRoutingFailure(movie, employee, routeResult);
            }
        }

        private void CheckProductionStationsReady(MovieProject movie)
        {
            if (movie.CurrentState != MovieProductionState.ReadyToFilm ||
                CurrentProductionPhase != ProductionPhase.MovingToStations) return;

            bool directorReady = movie.AssignedDirector == null || movie.DirectorArrivedAtStage;
            bool actorsReady = true;
            foreach (var participant in _requiredActors)
            {
                if (!_actorsAtWaitingStations.Contains(participant.Actor.Id))
                {
                    actorsReady = false;
                    break;
                }
            }

            if (!directorReady || !actorsReady)
            {
                UpdateStatus();
                OnActiveMovieChanged?.Invoke(movie);
                return;
            }

            SetProductionPhase(ProductionPhase.AtStations);
            SetProductionPhase(ProductionPhase.Blocking);
            RouteActorsToSceneMarks(movie);
        }

        private void RouteActorsToSceneMarks(MovieProject movie)
        {
            int actorMarkIndex = 0;
            foreach (var participant in _requiredActors)
            {
                var capturedParticipant = participant;
                var actor = participant.Actor;
                var markType = GetActorMark(actorMarkIndex++);
                var markIntent = new EmployeeIntent(
                    EmployeeIntentPurpose.MovingToMark,
                    $"Moving to {markType} — {movie.Title} ({capturedParticipant.CharacterNames})",
                    $"SoundStage:{markType}");

                var routeResult = _router.SendEmployeeToSceneMark(
                    actor,
                    BuildingType.SoundStage,
                    markType,
                    markIntent,
                    () =>
                    {
                        _actorsAtSceneMarks.Add(actor.Id);
                        foreach (var role in capturedParticipant.Roles) role.SetArrivedAtStage(true);
                        actor.SetState(EmployeeState.Working);
                        actor.SetIntent(new EmployeeIntent(
                            EmployeeIntentPurpose.PerformTask,
                            $"On {markType} — {movie.Title} ({capturedParticipant.CharacterNames})",
                            "SoundStage"));
                        CheckFilmingReadiness(movie);
                    });

                if (routeResult != StudioRouteResult.Started)
                {
                    HandleStageRoutingFailure(movie, actor, routeResult);
                    return;
                }
            }

            UpdateStatus();
            OnActiveMovieChanged?.Invoke(movie);
        }

        private static ProductionStationType GetActorStation(int actorIndex)
        {
            return actorIndex switch
            {
                0 => ProductionStationType.ActorWaitingA,
                1 => ProductionStationType.ActorWaitingB,
                _ => ProductionStationType.ActorWaitingC
            };
        }

        private static ActorSceneMarkType GetActorMark(int actorIndex)
        {
            return actorIndex switch
            {
                0 => ActorSceneMarkType.ActorMarkA,
                1 => ActorSceneMarkType.ActorMarkB,
                _ => ActorSceneMarkType.ActorMarkC
            };
        }
        private void HandleStageRoutingFailure(MovieProject movie, Employee employee, StudioRouteResult routeResult)
        {
            FailProduction(movie, $"Could not position {employee.Name} for filming ({routeResult}).");
        }

        private void HandleSlateFailure(MovieProject movie, StudioRouteResult routeResult)
        {
            FailProduction(movie, $"The slate sequence could not complete ({routeResult}).");
        }

        private void FailProduction(MovieProject movie, string message)
        {
            _routingFailureMessage = message;
            _actorsAtWaitingStations.Clear();
            _actorsAtSceneMarks.Clear();
            DiscardActiveTake();
            SetProductionPhase(ProductionPhase.Failed);

            if (movie.AssignedDirector != null)
            {
                _router.ReleaseEmployee(movie.AssignedDirector);
                movie.AssignedDirector.SetState(EmployeeState.Idle);
                movie.AssignedDirector.SetIntent(EmployeeIntent.None);
                movie.SetDirectorArrivedAtStage(false);
            }

            foreach (var participant in _requiredActors)
            {
                _router.ReleaseEmployee(participant.Actor);
                participant.Actor.SetState(EmployeeState.Idle);
                participant.Actor.SetIntent(EmployeeIntent.None);
                foreach (var role in participant.Roles) role.SetArrivedAtStage(false);
            }
            _requiredActors.Clear();

            UpdateStatus();
            OnActiveMovieChanged?.Invoke(movie);
            OnProductionNotification?.Invoke(_routingFailureMessage);
        }

        private void CheckFilmingReadiness(MovieProject movie)
        {
            bool actorsReady = _requiredActors.TrueForAll(
                participant => _actorsAtSceneMarks.Contains(participant.Actor.Id));
            if ((movie.AssignedDirector == null || movie.DirectorArrivedAtStage) && actorsReady &&
                movie.CurrentState == MovieProductionState.ReadyToFilm &&
                CurrentProductionPhase == ProductionPhase.Blocking)
            {
                SetProductionPhase(ProductionPhase.ReadyForTake);
                _activeTake = _activeScene?.PrepareTake();
                if (_activeTake == null)
                {
                    FailProduction(movie, "A take could not be prepared for the active scene.");
                    return;
                }

                BeginSlateSequence(movie);
            }
            else
            {
                UpdateStatus();
                OnActiveMovieChanged?.Invoke(movie);
            }
        }

        private void BeginSlateSequence(MovieProject movie)
        {
            if (_activeScene == null ||
                _activeTake == null ||
                !_activeScene.BeginRecording(_activeTake.Id))
            {
                FailProduction(movie, "The active take could not begin recording.");
                return;
            }

            SetProductionPhase(ProductionPhase.Slating);
            var routeResult = _router.StartSlateSequence(
                BuildingType.SoundStage,
                () =>
                {
                    if (movie == ActiveMovie &&
                        (movie.CurrentState == MovieProductionState.ReadyToFilm ||
                         movie.CurrentState == MovieProductionState.Filming) &&
                        CurrentProductionPhase == ProductionPhase.Slating)
                    {
                        BeginFilming(movie);
                    }
                },
                failure => HandleSlateFailure(movie, failure));

            if (routeResult != StudioRouteResult.Started)
            {
                HandleSlateFailure(movie, routeResult);
            }
        }

        private void BeginFilming(MovieProject movie)
        {
            if (_activeScene == null || !_activeScene.BeginFilming())
            {
                FailProduction(movie, "The active scene could not begin filming.");
                return;
            }

            movie.SetState(MovieProductionState.Filming);
            SetProductionPhase(ProductionPhase.Filming);
            _filmingMinutesElapsed = 0;

            if (movie.AssignedDirector != null)
            {
                movie.AssignedDirector.SetState(EmployeeState.Filming);
                movie.AssignedDirector.SetIntent(new EmployeeIntent(
                    EmployeeIntentPurpose.PerformTask,
                    $"Filming — {movie.Title}",
                    "SoundStage"));
            }

            foreach (var participant in _requiredActors)
            {
                participant.Actor.SetState(EmployeeState.Filming);
                participant.Actor.SetIntent(new EmployeeIntent(
                    EmployeeIntentPurpose.PerformTask,
                    $"Filming — {movie.Title} ({participant.CharacterNames})",
                    "SoundStage"));
            }

            UpdateStatus();
            OnActiveMovieChanged?.Invoke(movie);

            if (_activeScene.Beats.Count > 0)
            {
                BeginBeatSequence(movie);
            }
        }

        private void CompleteFilming(MovieProject movie)
        {
            _beatSequenceActive = false;
            if (_activeScene == null ||
                _activeTake == null ||
                !_activeScene.CompleteTake(_activeTake.Id, TimeSpan.FromMinutes(_filmingMinutesElapsed)))
            {
                FailProduction(movie, "The active take could not be completed.");
                return;
            }

            if (movie.ProductionControlMode == ProductionControlMode.Manual)
            {
                AwaitTakeDecision(movie);
                return;
            }

            if (!_activeScene.SelectTake(_activeTake.Id))
            {
                FailProduction(movie, "The completed take could not be selected.");
                return;
            }

            CompleteActiveScene(movie);
        }

        public bool KeepTake(string takeId)
        {
            var movie = ActiveMovie;
            if (!CanKeepTake || movie == null ||
                string.IsNullOrWhiteSpace(takeId) ||
                !_activeScene.SelectTake(takeId))
            {
                return false;
            }

            CompleteActiveScene(movie);
            return true;
        }

        public bool ShootAgain()
        {
            var movie = ActiveMovie;
            if (!CanShootAgain || movie == null || !_activeScene.PrepareForRetake())
            {
                return false;
            }

            _activeTake = _activeScene.PrepareTake();
            if (_activeTake == null)
            {
                FailProduction(movie, "A new take could not be prepared.");
                return false;
            }

            SetProductionPhase(ProductionPhase.ReadyForTake);
            ResetActorsForRetake(movie);
            return CurrentProductionPhase != ProductionPhase.Failed;
        }

        private void ResetActorsForRetake(MovieProject movie)
        {
            _actorsAtSceneMarks.Clear();
            if (_requiredActors.Count == 0)
            {
                BeginSlateSequence(movie);
                return;
            }

            for (int index = 0; index < _requiredActors.Count; index++)
            {
                var participant = _requiredActors[index];
                var actor = participant.Actor;
                ActorSceneMarkType markType = GetActorMark(index);
                var intent = new EmployeeIntent(
                    EmployeeIntentPurpose.ReportToStage,
                    $"Returning to {markType} for retake — {movie.Title}",
                    $"SoundStage:{markType}");
                StudioRouteResult routeResult = _router.SendEmployeeToSceneMark(
                    actor,
                    BuildingType.SoundStage,
                    markType,
                    intent,
                    () =>
                    {
                        if (movie != ActiveMovie ||
                            CurrentProductionPhase != ProductionPhase.ReadyForTake)
                        {
                            return;
                        }

                        _actorsAtSceneMarks.Add(actor.Id);
                        actor.SetState(EmployeeState.Working);
                        actor.SetIntent(new EmployeeIntent(
                            EmployeeIntentPurpose.PerformTask,
                            $"Ready at {markType} for retake — {movie.Title}",
                            "SoundStage"));
                        if (_actorsAtSceneMarks.Count == _requiredActors.Count)
                            BeginSlateSequence(movie);
                    });

                if (routeResult != StudioRouteResult.Started)
                {
                    HandleStageRoutingFailure(movie, actor, routeResult);
                    return;
                }
            }

            UpdateStatus();
            OnActiveMovieChanged?.Invoke(movie);
        }

        private void AwaitTakeDecision(MovieProject movie)
        {
            SetProductionPhase(ProductionPhase.AwaitingTakeDecision);
            if (movie.AssignedDirector != null)
            {
                movie.AssignedDirector.SetState(EmployeeState.Working);
                movie.AssignedDirector.SetIntent(new EmployeeIntent(
                    EmployeeIntentPurpose.PerformTask,
                    $"Awaiting take decision — {movie.Title}",
                    "SoundStage"));
            }

            foreach (var participant in _requiredActors)
            {
                participant.Actor.SetState(EmployeeState.Working);
                participant.Actor.SetIntent(new EmployeeIntent(
                    EmployeeIntentPurpose.PerformTask,
                    $"Awaiting take decision — {movie.Title}",
                    "SoundStage"));
            }

            UpdateStatus();
            OnActiveMovieChanged?.Invoke(movie);
        }

        private void CompleteActiveScene(MovieProject movie)
        {
            if (_activeScene == null || _activeScene.SelectedTake == null || !_activeScene.CompleteFilming())
            {
                FailProduction(movie, "The active scene could not be completed.");
                return;
            }

            int completedSceneNumber = _activeScene.SceneNumber;

            if (movie.AssignedDirector != null)
            {
                _router.ReleaseEmployee(movie.AssignedDirector);
                movie.AssignedDirector.SetState(EmployeeState.Idle);
                movie.AssignedDirector.SetIntent(EmployeeIntent.None);
            }

            foreach (var participant in _requiredActors)
            {
                _router.ReleaseEmployee(participant.Actor);
                participant.Actor.SetState(EmployeeState.Idle);
                participant.Actor.SetIntent(EmployeeIntent.None);
                foreach (var role in participant.Roles) role.SetArrivedAtStage(false);
            }

            movie.SetDirectorArrivedAtStage(false);
            movie.SetProgress(CalculateProductionProgress(movie));
            ClearActiveSceneAndTake();
            _requiredActors.Clear();
            _actorsAtWaitingStations.Clear();
            _actorsAtSceneMarks.Clear();

            if (movie.AllScenesCompleted)
            {
                SetProductionPhase(ProductionPhase.Completed);
                movie.SetProgress(1.0f);
                movie.TrySetProductionResult(_qualityCalculator.Calculate(movie));
                movie.SetState(MovieProductionState.Completed);
                OnProductionNotification?.Invoke(
                    $"{movie.Title} has finished filming!{Environment.NewLine}" +
                    $"Production Quality: {movie.ProductionResult.OverallQuality}/100");
            }
            else
            {
                SetProductionPhase(ProductionPhase.AwaitingNextScene);
                movie.SetState(MovieProductionState.ReadyToFilm);
                var nextScene = movie.GetNextFilmableScene();
                string nextDescription = nextScene != null
                    ? $"Scene {nextScene.SceneNumber} is next."
                    : "Another unfinished scene remains.";
                OnProductionNotification?.Invoke(
                    $"Scene {completedSceneNumber} of {movie.Title} is complete. {nextDescription}");
            }

            UpdateStatus();
            OnActiveMovieChanged?.Invoke(movie);
        }

        private float CalculateProductionProgress(MovieProject movie)
        {
            if (movie.Scenes.Count == 0) return 0f;

            int completedScenes = 0;
            foreach (var scene in movie.Scenes)
            {
                if (scene.Status == MovieSceneStatus.Completed) completedScenes++;
            }

            float activeSceneProgress = _activeScene != null &&
                                        _activeScene.Status == MovieSceneStatus.Filming
                ? Math.Clamp((float)_filmingMinutesElapsed / FilmingDurationMinutes, 0f, 1f)
                : 0f;
            return Math.Clamp((completedScenes + activeSceneProgress) / movie.Scenes.Count, 0f, 1f);
        }

        private bool TryResolveRequiredActors(MovieProject movie, out string failureMessage)
        {
            var resolved = new List<RequiredActor>();
            var byActorId = new Dictionary<string, RequiredActor>();
            bool hasExplicitCharacters = _activeScene.ParticipatingCharacterIds.Count > 0;

            if (hasExplicitCharacters)
            {
                foreach (string characterId in _activeScene.ParticipatingCharacterIds)
                {
                    if (movie.GetCastRole(characterId) == null)
                    {
                        failureMessage = $"Scene {_activeScene.SceneNumber} references a character that is no longer in the movie cast.";
                        return false;
                    }
                }
            }

            foreach (var role in movie.CastRoles)
            {
                if (hasExplicitCharacters && !_activeScene.HasCharacter(role.Id)) continue;
                if (string.IsNullOrWhiteSpace(role.AssignedActorId))
                {
                    failureMessage = $"{role.CharacterName} requires an assigned actor before filming can begin.";
                    return false;
                }

                var actor = _router.ResolveEmployee(role.AssignedActorId, role.AssignedActor);
                if (actor == null)
                {
                    failureMessage = $"The actor assigned to {role.CharacterName} is no longer employed by the studio.";
                    return false;
                }
                if (actor.Role != EmployeeRole.Actor)
                {
                    failureMessage = $"{actor.Name} is no longer eligible to perform {role.CharacterName}.";
                    return false;
                }

                if (!byActorId.TryGetValue(actor.Id, out var participant))
                {
                    participant = new RequiredActor(actor);
                    byActorId.Add(actor.Id, participant);
                    resolved.Add(participant);
                }
                participant.Roles.Add(role);
            }

            if (resolved.Count > 3)
            {
                failureMessage = $"Scene {_activeScene.SceneNumber} requires {resolved.Count} actors, but the current Sound Stage supports only 3.";
                return false;
            }

            _requiredActors.AddRange(resolved);
            failureMessage = null;
            return true;
        }

        private sealed class RequiredActor
        {
            public RequiredActor(Employee actor) => Actor = actor;
            public Employee Actor { get; }
            public List<MovieRole> Roles { get; } = new List<MovieRole>();
            public string CharacterNames => string.Join(", ", Roles.ConvertAll(role => role.CharacterName));
        }

        private bool TryActivateScene(MovieProject movie)
        {
            ClearActiveSceneAndTake();

            _activeScene = movie.GetNextFilmableScene();

            if (_activeScene == null && movie.Scenes.Count == 0)
            {
                var defaultScene = new MovieScene(
                    Guid.NewGuid().ToString(),
                    1,
                    "sound-stage-1",
                    "Scene 1");
                if (movie.AddScene(defaultScene))
                {
                    foreach (var castRole in movie.CastRoles)
                    {
                        defaultScene.AddCharacter(castRole.Id);
                    }

                    AddPrototypeBeats(movie, defaultScene);
                    _activeScene = defaultScene;
                }
            }

            if (_activeScene == null) return false;
            if (_activeScene.Status == MovieSceneStatus.Planned && !_activeScene.MarkReady()) return false;
            return _activeScene.Status == MovieSceneStatus.Ready;
        }

        private static void AddPrototypeBeats(MovieProject movie, MovieScene scene)
        {
            if (movie.CastRoles.Count == 0) return;

            MovieRole performer = movie.CastRoles[0];
            MovieRole target = movie.CastRoles.Count > 1 ? movie.CastRoles[1] : null;
            var action = new ScreenplayBeat(
                Guid.NewGuid().ToString(),
                1,
                ScreenplayBeatType.Action,
                "Performs a simple action.",
                performer.Id,
                blockingTargetId: SceneBlockingPointIds.StageLeft);
            var dialogue = new ScreenplayBeat(
                Guid.NewGuid().ToString(),
                2,
                ScreenplayBeatType.Dialogue,
                target != null ? "Addresses the other character." : "Delivers a short line.",
                performer.Id,
                target?.Id);
            var reaction = new ScreenplayBeat(
                Guid.NewGuid().ToString(),
                3,
                ScreenplayBeatType.Reaction,
                "Reacts to the moment.",
                target?.Id ?? performer.Id,
                target != null ? performer.Id : null,
                target != null ? ScreenplayEmotion.Nervous : ScreenplayEmotion.Confident);

            movie.AddBeatToScene(scene.Id, action);
            movie.AddBeatToScene(scene.Id, dialogue);
            movie.AddBeatToScene(scene.Id, reaction);
            movie.AddShotToScene(
                scene.Id,
                new SceneShot(Guid.NewGuid().ToString(), 1, ShotType.Wide, screenplayBeatId: action.Id));
            movie.AddShotToScene(
                scene.Id,
                new SceneShot(Guid.NewGuid().ToString(), 2, ShotType.Medium, performer.Id, dialogue.Id));
            movie.AddShotToScene(
                scene.Id,
                new SceneShot(
                    Guid.NewGuid().ToString(),
                    3,
                    ShotType.CloseUp,
                    target?.Id ?? performer.Id,
                    reaction.Id));
        }

        private void DiscardActiveTake()
        {
            if (_activeScene != null &&
                _activeTake != null &&
                _activeTake.Status != MovieTakeStatus.Completed &&
                _activeTake.Status != MovieTakeStatus.Discarded)
            {
                _activeScene.DiscardTake(_activeTake.Id);
            }

            ClearActiveSceneAndTake();
        }

        private void ClearActiveSceneAndTake()
        {
            _beatSequenceActive = false;
            _activeScene = null;
            _activeTake = null;
        }

        private void SetProductionPhase(ProductionPhase phase)
        {
            if (CurrentProductionPhase == phase) return;
            CurrentProductionPhase = phase;
            OnProductionPhaseChanged?.Invoke(phase);
        }

        private void UpdateStatus()
        {
            var movie = ActiveMovie;
            if (!string.IsNullOrEmpty(_routingFailureMessage))
            {
                _statusMessage = _routingFailureMessage;
                return;
            }

            if (movie == null)
            {
                _statusMessage = "No active project";
                return;
            }

            switch (movie.CurrentState)
            {
                case MovieProductionState.Draft:
                    if (!movie.AllRolesCast && !movie.HasDirector)
                        _statusMessage = "Awaiting cast & director";
                    else if (!movie.AllRolesCast)
                        _statusMessage = "Casting required";
                    else if (!movie.HasDirector)
                        _statusMessage = "Director required";
                    else
                        _statusMessage = "Ready for casting";
                    break;

                case MovieProductionState.Casting:
                    int castingActiveCount = 0;
                    int uncastCount = 0;
                    int castingRemainingMin = 0;

                    foreach (var role in movie.Roles)
                    {
                        if (role.AssignedActor == null) uncastCount++;
                        else if (role.IsCastingActive && !role.CastingCompleted)
                        {
                            castingActiveCount++;
                            int rem = Math.Max(0, 60 - role.CastingMinutesElapsed);
                            if (rem > castingRemainingMin) castingRemainingMin = rem;
                        }
                    }

                    if (uncastCount > 0)
                    {
                        _statusMessage = $"Casting required ({uncastCount} role{(uncastCount > 1 ? "s" : "")} uncast)";
                    }
                    else if (castingActiveCount > 0)
                    {
                        _statusMessage = $"Casting in progress ({castingRemainingMin}m remaining)";
                    }
                    else if (!movie.HasDirector)
                    {
                        _statusMessage = "Casting complete — waiting for director";
                    }
                    else
                    {
                        _statusMessage = "Casting complete";
                    }
                    break;

                case MovieProductionState.ReadyToFilm:
                    if (CurrentProductionPhase == ProductionPhase.AwaitingNextScene)
                    {
                        var nextScene = movie.GetNextFilmableScene();
                        _statusMessage = nextScene != null
                            ? $"Scene {nextScene.SceneNumber} awaiting production"
                            : "Another unfinished scene remains";
                        break;
                    }

                    int unarrived = 0;
                    if (!movie.DirectorArrivedAtStage) unarrived++;
                    foreach (var participant in _requiredActors)
                    {
                        bool arrived = CurrentProductionPhase == ProductionPhase.Blocking
                            ? _actorsAtSceneMarks.Contains(participant.Actor.Id)
                            : _actorsAtWaitingStations.Contains(participant.Actor.Id);
                        if (!arrived) unarrived++;
                    }

                    if (CurrentProductionPhase == ProductionPhase.Blocking && unarrived > 0)
                        _statusMessage = FormatActiveSceneStatus(
                            $"Actors moving to scene marks ({unarrived} remaining)");
                    else if (CurrentProductionPhase == ProductionPhase.Slating)
                        _statusMessage = FormatActiveSceneStatus("Slating");
                    else
                        _statusMessage = FormatActiveSceneStatus(
                            unarrived > 0
                                ? $"Cast & crew moving to production stations ({unarrived} remaining)"
                                : "Ready to film");
                    break;

                case MovieProductionState.Filming:
                    if (CurrentProductionPhase == ProductionPhase.AwaitingTakeDecision)
                    {
                        _statusMessage = FormatActiveSceneStatus("Awaiting Take Decision");
                        break;
                    }
                    int pct = (int)Math.Round(movie.ProductionProgress * 100f);
                    _statusMessage = FormatActiveSceneStatus($"Filming — {pct}%");
                    break;

                case MovieProductionState.Completed:
                    _statusMessage = "Production completed";
                    break;

                default:
                    _statusMessage = movie.CurrentState.ToString();
                    break;
            }
        }

        private string FormatActiveSceneStatus(string phase)
        {
            if (_activeScene == null) return phase;

            string sceneLabel = string.IsNullOrWhiteSpace(_activeScene.Title)
                ? $"Scene {_activeScene.SceneNumber}"
                : $"Scene {_activeScene.SceneNumber} — {_activeScene.Title}";
            return $"{sceneLabel}{Environment.NewLine}{phase}";
        }
    }
}
