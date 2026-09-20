using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Writing;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Employees;
using UnityEngine;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class PrototypeScreenplayBeatSequence : MonoBehaviour
    {
        private const float ActionDurationSeconds = 1.6f;
        private const float DialogueDurationSeconds = 2.2f;
        private const float ReactionDurationSeconds = 1.8f;
        private const float CompletionGraceSeconds = 2f;
        private const float MovementTimeoutSeconds = 20f;

        private readonly List<ScreenplayBeat> _beats = new List<ScreenplayBeat>();
        private readonly HashSet<EmployeeAgent> _participantAgents = new HashSet<EmployeeAgent>();
        private readonly Dictionary<string, BeatPerformanceResult> _performances = new Dictionary<string, BeatPerformanceResult>();
        private StudioEmployeeManager _employeeManager;
        private ISimulationTimeService _timeService;
        private SetBlockingPointLayout _blockingPointLayout;
        private BeatPerformanceGenerator _performanceGenerator;
        private PrototypeProductionCamera _productionCamera;
        private string _facilityId;
        private MovieProject _activeMovie;
        private MovieScene _activeScene;
        private MovieTake _activeTake;
        private int _currentBeatIndex;
        private bool _beatExecuting;
        private float _beatElapsed;
        private float _beatTimeout;
        private bool _awaitingBlockingMovement;
        private EmployeeAgent _movingPerformer;
        private ScreenplayBeat _movingBeat;
        private BeatPerformanceResult _movingPerformance;
        private Action _onCompleted;
        private Action<StudioRouteResult> _onFailed;

        public bool IsRunning => _activeScene != null;
        public MovieScene ActiveScene => _activeScene;
        public MovieTake ActiveTake => _activeTake;
        public int CurrentBeatIndex => IsRunning ? _currentBeatIndex : -1;
        public bool IsBeatExecuting => _beatExecuting;

        public void Initialize(
            StudioEmployeeManager employeeManager,
            ISimulationTimeService timeService,
            SetBlockingPointLayout blockingPointLayout,
            BeatPerformanceGenerator performanceGenerator,
            PrototypeProductionCamera productionCamera,
            string facilityId)
        {
            _employeeManager = employeeManager;
            _timeService = timeService;
            _blockingPointLayout = blockingPointLayout;
            _performanceGenerator = performanceGenerator;
            _productionCamera = productionCamera;
            _facilityId = facilityId;
        }

        public StudioRouteResult TryBegin(
            MovieProject movie,
            MovieScene scene,
            MovieTake take,
            Action onCompleted,
            Action<StudioRouteResult> onFailed)
        {
            if (IsRunning) return StudioRouteResult.SequenceInProgress;
            if (movie == null || scene == null || take == null || scene.Beats.Count == 0)
                return StudioRouteResult.PerformanceMissing;
            if (_employeeManager == null) return StudioRouteResult.AgentMissing;

            _activeMovie = movie;
            _activeScene = scene;
            _activeTake = take;
            _beats.Clear();
            _beats.AddRange(scene.Beats);
            foreach (var beat in _beats)
            {
                if (!TryResolveAgent(beat.PerformingCharacterId, out EmployeeAgent performer))
                {
                    ResetSequence();
                    return StudioRouteResult.AgentMissing;
                }
                _participantAgents.Add(performer);

                if (beat.TargetCharacterId != null)
                {
                    if (!TryResolveAgent(beat.TargetCharacterId, out EmployeeAgent target))
                    {
                        ResetSequence();
                        return StudioRouteResult.AgentMissing;
                    }
                    _participantAgents.Add(target);
                }

                if (_performanceGenerator == null)
                {
                    ResetSequence();
                    return StudioRouteResult.PerformanceMissing;
                }

                BeatPerformanceResult performance = _performanceGenerator.Generate(
                    beat,
                    performer.Employee,
                    movie.AssignedDirector,
                    movie.GenreId);
                if (!take.RecordPerformanceResult(performance))
                {
                    ResetSequence();
                    return StudioRouteResult.PerformanceMissing;
                }
                _performances[beat.Id] = performance;
            }

            foreach (var participant in _participantAgents)
                participant.SetGenericFilmingPresentationEnabled(false);
            _currentBeatIndex = 0;
            _onCompleted = onCompleted;
            _onFailed = onFailed;

            StudioRouteResult result = TryBeginCurrentBeat();
            if (result != StudioRouteResult.Started) ResetSequence();
            return result;
        }

        private StudioRouteResult TryBeginCurrentBeat()
        {
            if (_currentBeatIndex >= _beats.Count)
            {
                Complete();
                return StudioRouteResult.Started;
            }

            ScreenplayBeat beat = _beats[_currentBeatIndex];
            if (!TryResolveAgent(beat.PerformingCharacterId, out EmployeeAgent performer))
            {
                return StudioRouteResult.AgentMissing;
            }

            if (!_performances.TryGetValue(beat.Id, out BeatPerformanceResult performance))
                return StudioRouteResult.PerformanceMissing;

            PresentShotForBeat(beat);

            Transform target = null;
            if (beat.TargetCharacterId != null)
            {
                if (!TryResolveAgent(beat.TargetCharacterId, out EmployeeAgent targetAgent))
                {
                    return StudioRouteResult.AgentMissing;
                }
                target = targetAgent.transform;
            }

            if (beat.BeatType == ScreenplayBeatType.Action &&
                !string.IsNullOrWhiteSpace(beat.BlockingTargetId))
            {
                return BeginBlockingAction(performer, beat, performance, target);
            }

            return BeginBeatPresentation(performer, beat, performance, target);
        }

        private StudioRouteResult BeginBlockingAction(
            EmployeeAgent performer,
            ScreenplayBeat beat,
            BeatPerformanceResult performance,
            Transform target)
        {
            if (_blockingPointLayout == null ||
                !_blockingPointLayout.TryGetPosition(beat.BlockingTargetId, out Vector3 destination))
            {
                Debug.LogWarning(
                    $"[PrototypeScreenplayBeatSequence] Blocking point '{beat.BlockingTargetId}' was not found; using the Action gesture fallback.");
                return BeginBeatPresentation(performer, beat, performance, target);
            }

            _beatExecuting = true;
            _awaitingBlockingMovement = true;
            _movingPerformer = performer;
            _movingBeat = beat;
            _movingPerformance = performance;
            _beatElapsed = 0f;
            _beatTimeout = MovementTimeoutSeconds;
            var intent = new EmployeeIntent(
                EmployeeIntentPurpose.PerformTask,
                $"Moving to blocking point — {beat.BlockingTargetId}",
                $"{_facilityId}:{beat.BlockingTargetId}");
            bool started = performer.TryAssignTaskDestination(
                destination,
                intent,
                () => HandleBlockingArrival(performer, beat, performance, target));
            if (started) return StudioRouteResult.Started;

            Debug.LogWarning(
                $"[PrototypeScreenplayBeatSequence] Actor could not reach blocking point '{beat.BlockingTargetId}'; using the Action gesture fallback.");
            _awaitingBlockingMovement = false;
            _movingPerformer = null;
            _movingBeat = null;
            _movingPerformance = null;
            return BeginBeatPresentation(performer, beat, performance, target);
        }

        private void HandleBlockingArrival(
            EmployeeAgent performer,
            ScreenplayBeat beat,
            BeatPerformanceResult performance,
            Transform target)
        {
            if (!IsRunning || !_awaitingBlockingMovement) return;

            _awaitingBlockingMovement = false;
            _movingPerformer = null;
            _movingBeat = null;
            _movingPerformance = null;
            performer.Employee.SetState(EmployeeState.Filming);
            performer.Employee.SetIntent(new EmployeeIntent(
                EmployeeIntentPurpose.PerformTask,
                "Performing screenplay action",
                _facilityId));
            StudioRouteResult result = BeginBeatPresentation(performer, beat, performance, target);
            if (result != StudioRouteResult.Started)
                Fail(result);
        }

        private StudioRouteResult BeginBeatPresentation(
            EmployeeAgent performer,
            ScreenplayBeat beat,
            BeatPerformanceResult performance,
            Transform target)
        {
            float duration = GetDuration(beat.BeatType);
            _beatExecuting = true;
            _beatElapsed = 0f;
            _beatTimeout = duration + CompletionGraceSeconds;
            if (!performer.TryBeginBeatPerformance(
                    beat.BeatType,
                    performance,
                    target,
                    duration,
                    HandleBeatCompleted))
            {
                return StudioRouteResult.PerformanceMissing;
            }

            return StudioRouteResult.Started;
        }

        private void Update()
        {
            if (!_beatExecuting || (_timeService != null && _timeService.IsPaused)) return;

            float speedMultiplier = _timeService != null ? _timeService.TimeScaleMultiplier : 1f;
            _beatElapsed += UnityEngine.Time.deltaTime * speedMultiplier;
            if (_beatElapsed >= _beatTimeout)
            {
                if (_awaitingBlockingMovement)
                {
                    FallBackFromBlockingMovement();
                }
                else
                {
                    Fail(StudioRouteResult.PerformanceMissing);
                }
            }
        }

        private void FallBackFromBlockingMovement()
        {
            EmployeeAgent performer = _movingPerformer;
            ScreenplayBeat beat = _movingBeat;
            BeatPerformanceResult performance = _movingPerformance;
            _awaitingBlockingMovement = false;
            _movingPerformer = null;
            _movingBeat = null;
            _movingPerformance = null;
            if (performer == null || beat == null || performance == null)
            {
                Fail(StudioRouteResult.PerformanceMissing);
                return;
            }

            Debug.LogWarning(
                $"[PrototypeScreenplayBeatSequence] Movement to '{beat.BlockingTargetId}' timed out; using the Action gesture fallback.");
            performer.ClearTaskDestination();
            performer.Employee.SetState(EmployeeState.Filming);
            performer.Employee.SetIntent(new EmployeeIntent(
                EmployeeIntentPurpose.PerformTask,
                "Performing screenplay action",
                _facilityId));
            StudioRouteResult result = BeginBeatPresentation(performer, beat, performance, null);
            if (result != StudioRouteResult.Started)
                Fail(result);
        }

        private void HandleBeatCompleted()
        {
            if (!IsRunning) return;
            _beatExecuting = false;
            _currentBeatIndex++;
            StudioRouteResult result = TryBeginCurrentBeat();
            if (result != StudioRouteResult.Started)
                Fail(result);
        }

        private bool TryResolveAgent(string characterId, out EmployeeAgent agent)
        {
            agent = null;
            if (string.IsNullOrWhiteSpace(characterId)) return false;

            MovieRole role = _activeMovie.GetCastRole(characterId);
            if (role == null || string.IsNullOrWhiteSpace(role.AssignedActorId)) return false;
            Employee employee = _employeeManager.GetEmployee(role.AssignedActorId);
            if (employee == null || employee.Role != EmployeeRole.Actor) return false;
            agent = _employeeManager.GetAgent(employee);
            return agent != null;
        }

        private void PresentShotForBeat(ScreenplayBeat beat)
        {
            if (_productionCamera == null || !_productionCamera.IsActive) return;

            SceneShot shot = _activeScene.GetShotForBeat(beat.Id);
            if (shot == null) return;

            Transform subject = null;
            if (shot.SubjectCharacterId != null)
            {
                if (TryResolveAgent(shot.SubjectCharacterId, out EmployeeAgent subjectAgent))
                {
                    subject = subjectAgent.transform;
                }
                else
                {
                    Debug.LogWarning(
                        $"[PrototypeScreenplayBeatSequence] Shot subject '{shot.SubjectCharacterId}' could not be resolved; using Wide framing.");
                }
            }

            _productionCamera.Present(shot, subject);
        }

        private static float GetDuration(ScreenplayBeatType type)
        {
            return type switch
            {
                ScreenplayBeatType.Dialogue => DialogueDurationSeconds,
                ScreenplayBeatType.Reaction => ReactionDurationSeconds,
                _ => ActionDurationSeconds
            };
        }

        private void Complete()
        {
            var callback = _onCompleted;
            ResetSequence();
            callback?.Invoke();
        }

        private void Fail(StudioRouteResult result)
        {
            var callback = _onFailed;
            ResetSequence();
            callback?.Invoke(result);
        }

        private void ResetSequence()
        {
            foreach (var participant in _participantAgents)
            {
                if (participant != null)
                    participant.SetGenericFilmingPresentationEnabled(true);
            }
            _participantAgents.Clear();
            _activeMovie = null;
            _activeScene = null;
            _activeTake = null;
            _beats.Clear();
            _performances.Clear();
            _currentBeatIndex = 0;
            _beatExecuting = false;
            _awaitingBlockingMovement = false;
            _movingPerformer = null;
            _movingBeat = null;
            _movingPerformance = null;
            _beatElapsed = 0f;
            _beatTimeout = 0f;
            _onCompleted = null;
            _onFailed = null;
        }

        private void OnDestroy()
        {
            ResetSequence();
        }
    }
}
