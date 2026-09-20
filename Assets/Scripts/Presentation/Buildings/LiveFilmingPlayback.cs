using System;
using System.Collections.Generic;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Writing;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Employees;
using UnityEngine;
using UnityEngine.InputSystem;

namespace SilverScreen.Presentation.Buildings
{
    [DisallowMultipleComponent]
    public sealed class LiveFilmingPlayback : MonoBehaviour
    {
        private enum PlaybackPhase
        {
            Moving,
            Performing,
            BetweenBeats
        }

        private sealed class LiveFilmingPlaybackClock
        {
            public float ElapsedSeconds { get; private set; }
            public void Advance(float unscaledDeltaSeconds) => ElapsedSeconds += Math.Max(0f, unscaledDeltaSeconds);
            public void Reset() => ElapsedSeconds = 0f;
        }

        private sealed class PlaybackActor
        {
            public string CharacterId;
            public GameObject Root;
            public Transform Body;
            public Transform LeftArm;
            public Transform RightArm;
            public Vector3 StartPosition;
            public Quaternion StartRotation;
        }

        private const float BlockingDurationSeconds = 1.25f;
        private const float BetweenBeatSeconds = 0.25f;

        private readonly Dictionary<string, PlaybackActor> _actors = new Dictionary<string, PlaybackActor>();
        private readonly Dictionary<string, BeatPerformanceResult> _results = new Dictionary<string, BeatPerformanceResult>();
        private readonly Dictionary<Renderer, bool> _sourceRendererStates = new Dictionary<Renderer, bool>();
        private readonly LiveFilmingPlaybackClock _playbackClock = new LiveFilmingPlaybackClock();

        private IMovieProductionService _productionService;
        private ISimulationTimeService _simulationTime;
        private StudioEmployeeManager _employeeManager;
        private ActorSceneMarkLayout _sceneMarks;
        private SetBlockingPointLayout _blockingPoints;
        private PrototypeProductionCamera _productionCamera;
        private MovieProject _movie;
        private MovieScene _scene;
        private MovieTake _take;
        private int _beatIndex;
        private PlaybackPhase _phase;
        private PlaybackActor _performer;
        private ScreenplayBeat _beat;
        private BeatPerformanceResult _result;
        private Vector3 _movementOrigin;
        private Vector3 _movementTarget;
        private float _phaseDuration;
        private SimulationSpeed _capturedSpeed;
        private bool _capturedPaused;

        public bool IsViewing { get; private set; }

        public void Initialize(
            ISimulationTimeService simulationTime,
            StudioEmployeeManager employeeManager,
            ActorSceneMarkLayout sceneMarks,
            SetBlockingPointLayout blockingPoints,
            PrototypeProductionCamera productionCamera)
        {
            _simulationTime = simulationTime;
            _employeeManager = employeeManager;
            _sceneMarks = sceneMarks;
            _blockingPoints = blockingPoints;
            _productionCamera = productionCamera;
        }

        public void BindProductionService(IMovieProductionService productionService)
        {
            _productionService = productionService;
        }

        public bool TryEnter()
        {
            if (IsViewing) return true;
            if (_productionService == null || _simulationTime == null || _employeeManager == null ||
                _productionService.CurrentProductionPhase != ProductionPhase.Filming)
            {
                return false;
            }

            _movie = _productionService.ActiveMovie;
            _scene = _productionService.ActiveScene;
            _take = _productionService.ActiveTake;
            if (_movie == null || _scene == null || _take == null ||
                _take.Status != MovieTakeStatus.Recording || _scene.Beats.Count == 0)
            {
                ClearReferences();
                return false;
            }

            _results.Clear();
            foreach (var result in _take.PerformanceResults)
                _results[result.ScreenplayBeatId] = result;
            foreach (var beat in _scene.Beats)
            {
                if (!_results.ContainsKey(beat.Id))
                {
                    Debug.LogWarning("[LiveFilmingPlayback] The active take is not ready for observational playback.");
                    ClearReferences();
                    return false;
                }
            }

            _capturedSpeed = _simulationTime.CurrentSpeed;
            _capturedPaused = _simulationTime.IsPaused;
            _simulationTime.OnSpeedChanged += KeepAuthoritativeSimulationPaused;
            _simulationTime.SetSpeed(SimulationSpeed.Paused);

            if (!BuildPlaybackActors() || _productionCamera == null || !_productionCamera.BeginSequence())
            {
                Exit();
                return false;
            }

            IsViewing = true;
            _beatIndex = 0;
            BeginBeat();
            return true;
        }

        public void Exit()
        {
            bool wasViewingOrCaptured = IsViewing || _simulationTime != null && _movie != null;
            IsViewing = false;
            RestoreSourceRenderers();
            DestroyPlaybackActors();
            _productionCamera?.EndSequence();

            if (_simulationTime != null)
            {
                _simulationTime.OnSpeedChanged -= KeepAuthoritativeSimulationPaused;
                if (wasViewingOrCaptured)
                    _simulationTime.SetSpeed(_capturedPaused ? SimulationSpeed.Paused : _capturedSpeed);
            }

            ClearReferences();
        }

        private void Update()
        {
            if (!IsViewing) return;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Exit();
                return;
            }

            if (!IsObservedProductionValid())
            {
                Exit();
                return;
            }

            _playbackClock.Advance(UnityEngine.Time.unscaledDeltaTime);
            switch (_phase)
            {
                case PlaybackPhase.Moving:
                    UpdateMovement();
                    break;
                case PlaybackPhase.Performing:
                    UpdatePerformance();
                    break;
                default:
                    if (_playbackClock.ElapsedSeconds >= _phaseDuration) AdvanceBeat();
                    break;
            }
        }

        private bool BuildPlaybackActors()
        {
            var orderedCharacters = new List<string>();
            foreach (var beat in _scene.Beats)
            {
                AddCharacterIfMissing(orderedCharacters, beat.PerformingCharacterId);
                AddCharacterIfMissing(orderedCharacters, beat.TargetCharacterId);
            }

            for (int index = 0; index < orderedCharacters.Count; index++)
            {
                string characterId = orderedCharacters[index];
                if (!TryResolveAgent(characterId, out EmployeeAgent source)) return false;

                ActorSceneMarkType mark = index switch
                {
                    0 => ActorSceneMarkType.ActorMarkA,
                    1 => ActorSceneMarkType.ActorMarkB,
                    _ => ActorSceneMarkType.ActorMarkC
                };
                Vector3 startPosition = source.transform.position;
                if (_sceneMarks != null && _sceneMarks.TryGetPosition(mark, out Vector3 markPosition))
                    startPosition = markPosition;

                PlaybackActor actor = CreatePlaybackActor(characterId, source, startPosition);
                _actors.Add(characterId, actor);
                foreach (var renderer in source.GetComponentsInChildren<Renderer>(true))
                {
                    _sourceRendererStates[renderer] = renderer.enabled;
                    renderer.enabled = false;
                }
            }

            return _actors.Count > 0;
        }

        private PlaybackActor CreatePlaybackActor(string characterId, EmployeeAgent source, Vector3 position)
        {
            var root = new GameObject($"LivePlayback_{characterId}");
            root.transform.SetParent(transform, true);
            root.transform.position = position;
            root.transform.rotation = source.transform.rotation;

            var bodyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            bodyObject.name = "Body";
            bodyObject.transform.SetParent(root.transform, false);
            bodyObject.transform.localPosition = Vector3.up;
            Destroy(bodyObject.GetComponent<Collider>());

            Renderer sourceRenderer = source.GetComponentInChildren<Renderer>();
            if (sourceRenderer != null) bodyObject.GetComponent<Renderer>().sharedMaterial = sourceRenderer.sharedMaterial;

            Transform leftArm = CreateArm(root.transform, "LeftArm", new Vector3(-0.55f, 1.25f, 0f), sourceRenderer);
            Transform rightArm = CreateArm(root.transform, "RightArm", new Vector3(0.55f, 1.25f, 0f), sourceRenderer);
            return new PlaybackActor
            {
                CharacterId = characterId,
                Root = root,
                Body = bodyObject.transform,
                LeftArm = leftArm,
                RightArm = rightArm,
                StartPosition = position,
                StartRotation = root.transform.rotation
            };
        }

        private static Transform CreateArm(Transform parent, string name, Vector3 localPosition, Renderer sourceRenderer)
        {
            var arm = GameObject.CreatePrimitive(PrimitiveType.Cube);
            arm.name = name;
            arm.transform.SetParent(parent, false);
            arm.transform.localPosition = localPosition;
            arm.transform.localScale = new Vector3(0.18f, 0.7f, 0.18f);
            UnityEngine.Object.Destroy(arm.GetComponent<Collider>());
            if (sourceRenderer != null) arm.GetComponent<Renderer>().sharedMaterial = sourceRenderer.sharedMaterial;
            return arm.transform;
        }

        private void BeginBeat()
        {
            if (_beatIndex >= _scene.Beats.Count)
            {
                ResetLoop();
                return;
            }

            _beat = _scene.Beats[_beatIndex];
            _results.TryGetValue(_beat.Id, out _result);
            _actors.TryGetValue(_beat.PerformingCharacterId, out _performer);
            if (_performer == null || _result == null)
            {
                Exit();
                return;
            }

            PresentShot(_beat);
            if (_beat.BeatType == ScreenplayBeatType.Action &&
                !string.IsNullOrWhiteSpace(_beat.BlockingTargetId) &&
                _blockingPoints != null &&
                _blockingPoints.TryGetPosition(_beat.BlockingTargetId, out _movementTarget))
            {
                _movementOrigin = _performer.Root.transform.position;
                BeginPhase(PlaybackPhase.Moving, BlockingDurationSeconds);
                return;
            }

            BeginPerformance();
        }

        private void PresentShot(ScreenplayBeat beat)
        {
            SceneShot shot = _scene.GetShotForBeat(beat.Id);
            if (shot == null) return;

            Transform subject = null;
            if (shot.SubjectCharacterId != null && _actors.TryGetValue(shot.SubjectCharacterId, out PlaybackActor actor))
                subject = actor.Root.transform;
            _productionCamera.Present(shot, subject);
        }

        private void UpdateMovement()
        {
            float normalized = Mathf.Clamp01(_playbackClock.ElapsedSeconds / _phaseDuration);
            _performer.Root.transform.position = Vector3.Lerp(_movementOrigin, _movementTarget, normalized);
            if (normalized >= 1f) BeginPerformance();
        }

        private void BeginPerformance()
        {
            float duration = _beat.BeatType switch
            {
                ScreenplayBeatType.Dialogue => 2.2f,
                ScreenplayBeatType.Reaction => 1.8f,
                _ => 1.6f
            };
            BeginPhase(PlaybackPhase.Performing, duration);
        }

        private void UpdatePerformance()
        {
            float normalized = Mathf.Clamp01(_playbackClock.ElapsedSeconds / _phaseDuration);
            float hesitation = Mathf.Lerp(0.16f, 0f, (float)_result.Confidence);
            float performedTime = Mathf.Clamp01((normalized - hesitation) / Mathf.Max(0.01f, 1f - hesitation));
            float tempo = Mathf.Lerp(0.82f, 1.18f, (float)_result.Timing);
            float pulse = Mathf.Sin(performedTime * Mathf.PI * 4f * tempo);
            float amplitude = Mathf.Lerp(
                0.42f,
                1.35f,
                (float)(_result.DeliveredIntensity * 0.4d + _result.Confidence * 0.2d + _result.Expressiveness * 0.4d));

            ApplyGesture(_performer, _beat.BeatType, _result.DeliveredEmotion, pulse, amplitude, performedTime);
            if (normalized >= 1f) BeginPhase(PlaybackPhase.BetweenBeats, BetweenBeatSeconds);
        }

        private static void ApplyGesture(
            PlaybackActor actor,
            ScreenplayBeatType beatType,
            ScreenplayEmotion emotion,
            float pulse,
            float amplitude,
            float performedTime)
        {
            float spread = beatType == ScreenplayBeatType.Dialogue ? 30f : 45f;
            float pitch = 0f;
            if (beatType == ScreenplayBeatType.Reaction)
            {
                spread = emotion switch
                {
                    ScreenplayEmotion.Happy => 65f,
                    ScreenplayEmotion.Sad => 8f,
                    ScreenplayEmotion.Angry => 50f,
                    ScreenplayEmotion.Afraid => 42f,
                    ScreenplayEmotion.Confident => 58f,
                    ScreenplayEmotion.Nervous => 22f,
                    _ => 28f
                };
                pitch = emotion == ScreenplayEmotion.Sad ? 14f : emotion == ScreenplayEmotion.Afraid ? -12f : 0f;
            }

            float sweep = Mathf.Sin(performedTime * Mathf.PI);
            actor.LeftArm.localRotation = Quaternion.Euler(0f, 0f, -spread - sweep * 25f * amplitude);
            actor.RightArm.localRotation = Quaternion.Euler(0f, 0f, spread + sweep * 25f * amplitude);
            actor.Body.localRotation = Quaternion.Euler(pitch * amplitude, pulse * 10f * amplitude, 0f);
        }

        private void AdvanceBeat()
        {
            ResetActorPose(_performer);
            _beatIndex++;
            BeginBeat();
        }

        private void ResetLoop()
        {
            foreach (var actor in _actors.Values)
            {
                actor.Root.transform.position = actor.StartPosition;
                actor.Root.transform.rotation = actor.StartRotation;
                ResetActorPose(actor);
            }
            _beatIndex = 0;
            BeginBeat();
        }

        private static void ResetActorPose(PlaybackActor actor)
        {
            if (actor == null) return;
            actor.Body.localRotation = Quaternion.identity;
            actor.LeftArm.localRotation = Quaternion.identity;
            actor.RightArm.localRotation = Quaternion.identity;
        }

        private void BeginPhase(PlaybackPhase phase, float duration)
        {
            _phase = phase;
            _phaseDuration = duration;
            _playbackClock.Reset();
        }

        private bool IsObservedProductionValid()
        {
            return _productionService != null &&
                   _productionService.CurrentProductionPhase == ProductionPhase.Filming &&
                   _productionService.ActiveMovie == _movie &&
                   _productionService.ActiveScene == _scene &&
                   _productionService.ActiveTake == _take &&
                   _take.Status == MovieTakeStatus.Recording;
        }

        private bool TryResolveAgent(string characterId, out EmployeeAgent agent)
        {
            agent = null;
            MovieRole role = _movie?.GetCastRole(characterId);
            if (role == null || string.IsNullOrWhiteSpace(role.AssignedActorId)) return false;
            agent = _employeeManager.GetAgent(role.AssignedActorId);
            return agent != null;
        }

        private void KeepAuthoritativeSimulationPaused(SimulationSpeed speed)
        {
            if (IsViewing && speed != SimulationSpeed.Paused)
                _simulationTime.SetSpeed(SimulationSpeed.Paused);
        }

        private void RestoreSourceRenderers()
        {
            foreach (var entry in _sourceRendererStates)
            {
                if (entry.Key != null) entry.Key.enabled = entry.Value;
            }
            _sourceRendererStates.Clear();
        }

        private void DestroyPlaybackActors()
        {
            foreach (var actor in _actors.Values)
            {
                if (actor.Root != null) Destroy(actor.Root);
            }
            _actors.Clear();
        }

        private void ClearReferences()
        {
            _movie = null;
            _scene = null;
            _take = null;
            _beat = null;
            _result = null;
            _performer = null;
            _results.Clear();
            _playbackClock.Reset();
        }

        private static void AddCharacterIfMissing(List<string> characters, string characterId)
        {
            if (!string.IsNullOrWhiteSpace(characterId) && !characters.Contains(characterId))
                characters.Add(characterId);
        }

        private void OnDisable() => Exit();
        private void OnDestroy() => Exit();
    }
}
