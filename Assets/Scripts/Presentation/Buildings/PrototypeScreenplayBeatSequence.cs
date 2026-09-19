using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
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

        private readonly List<ScreenplayBeat> _beats = new List<ScreenplayBeat>();
        private readonly HashSet<EmployeeAgent> _participantAgents = new HashSet<EmployeeAgent>();
        private StudioEmployeeManager _employeeManager;
        private ISimulationTimeService _timeService;
        private MovieProject _activeMovie;
        private MovieScene _activeScene;
        private MovieTake _activeTake;
        private int _currentBeatIndex;
        private bool _beatExecuting;
        private float _beatElapsed;
        private float _beatTimeout;
        private Action _onCompleted;
        private Action<StudioRouteResult> _onFailed;

        public bool IsRunning => _activeScene != null;
        public MovieScene ActiveScene => _activeScene;
        public MovieTake ActiveTake => _activeTake;
        public int CurrentBeatIndex => IsRunning ? _currentBeatIndex : -1;
        public bool IsBeatExecuting => _beatExecuting;

        public void Initialize(
            StudioEmployeeManager employeeManager,
            ISimulationTimeService timeService)
        {
            _employeeManager = employeeManager;
            _timeService = timeService;
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

            Transform target = null;
            if (beat.TargetCharacterId != null)
            {
                if (!TryResolveAgent(beat.TargetCharacterId, out EmployeeAgent targetAgent))
                {
                    return StudioRouteResult.AgentMissing;
                }
                target = targetAgent.transform;
            }

            float duration = GetDuration(beat.BeatType);
            _beatExecuting = true;
            _beatElapsed = 0f;
            _beatTimeout = duration + CompletionGraceSeconds;
            if (!performer.TryBeginBeatPerformance(
                    beat.BeatType,
                    beat.EmotionalIntention,
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
                Fail(StudioRouteResult.PerformanceMissing);
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
            _currentBeatIndex = 0;
            _beatExecuting = false;
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
