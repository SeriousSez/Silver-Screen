using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Tutorial
{
    [Serializable]
    public sealed class NewStudioOptions
    {
        public bool TutorialEnabled = true;
    }

    public enum TutorialStatus { Disabled, Active, Skipped, Completed }
    public enum GuidePausePolicy { None, StrategicWhileOpen }
    public enum GuideContinueBehavior { Dismiss, CompleteObjective }

    /// <summary>Save DTO. Contains semantic progress only, never gameplay objects or pause leases.</summary>
    [Serializable]
    public sealed class TutorialProgress
    {
        public int Version = 1;
        public TutorialStatus Status;
        public string CurrentStepId;
        public string ScreenplayId;
        public string MovieId;
        public bool MessageOpen;
        public List<string> CompletedStepIds = new List<string>();
        public List<string> UnlockedIds = new List<string>();
    }

    public sealed class GuideMessage
    {
        public string Title { get; }
        public string Body { get; }
        public string GuideIdentityId { get; }
        public string VoiceCueId { get; }
        public string FocusTargetId { get; }
        public GuidePausePolicy PausePolicy { get; }
        public GuideContinueBehavior ContinueBehavior { get; }
        public int Priority { get; }

        public GuideMessage(string title, string body, string voiceCueId = null, string focusTargetId = null,
            GuidePausePolicy pausePolicy = GuidePausePolicy.StrategicWhileOpen,
            GuideContinueBehavior continueBehavior = GuideContinueBehavior.Dismiss,
            string guideIdentityId = "guide.studio-adviser", int priority = 100)
        {
            Title = title; Body = body; VoiceCueId = voiceCueId; FocusTargetId = focusTargetId;
            PausePolicy = pausePolicy; ContinueBehavior = continueBehavior;
            GuideIdentityId = guideIdentityId; Priority = priority;
        }
    }

    public sealed class TutorialStep
    {
        public string Id { get; }
        public GuideMessage Message { get; }
        public string CompletionConditionId { get; }
        public string NextStepId { get; }
        public IReadOnlyList<string> EnterUnlocks { get; }
        public IReadOnlyList<string> CompleteUnlocks { get; }
        public TutorialStep(string id, GuideMessage message, string conditionId, string nextStepId,
            string[] enterUnlocks = null, string[] completeUnlocks = null)
        {
            Id = id; Message = message; CompletionConditionId = conditionId; NextStepId = nextStepId;
            EnterUnlocks = Array.AsReadOnly(enterUnlocks ?? Array.Empty<string>());
            CompleteUnlocks = Array.AsReadOnly(completeUnlocks ?? Array.Empty<string>());
        }
    }

    public static class StarterFeatureIds
    {
        public const string Headquarters = "building.headquarters";
        public const string Casting = "building.casting-office";
        public const string Stage = "building.stage-1";
        public const string SpecializedSets = "construction.specialized-sets";
    }

    public static class InitialTutorialSequence
    {
        public static IReadOnlyList<TutorialStep> Create() => Array.AsReadOnly(new[]
        {
            new TutorialStep("welcome", new GuideMessage("Welcome to SilverScreen",
                "Your studio begins in 1930 with no employees. Explore the lot with the camera, inspect the strategic clock, and name your studio in Studio. Continue to recruit your first builder.",
                "Tutorial.Welcome", "hud.studio", continueBehavior: GuideContinueBehavior.CompleteObjective), "guide.continue", "hire-builder"),
            new TutorialStep("hire-builder", new GuideMessage("Hire a construction worker",
                "Continue to let applicants arrive at the starter service facility. Open Build / Workforce [B] and hire a waiting Construction Worker. Groundskeepers are available for future cleaning work.",
                "Tutorial.HireBuilder", "workforce.service"), "construction-worker.hired", "headquarters"),
            new TutorialStep("headquarters", new GuideMessage("Establish headquarters",
                "Enter Build Mode [B], select Headquarters, move and rotate its preview, then place it on the lot. Your hired worker travels to the site and builds it. Continue closes this message so time can run.",
                "Tutorial.BuildHQ", "construction.headquarters"), "headquarters.established", "casting", new[] { StarterFeatureIds.Headquarters }),
            new TutorialStep("casting", new GuideMessage("Open the Casting Office",
                "Place and construct the Casting Office in Build Mode. Only its completion attracts actors, directors and extras. Headquarters provides the starter writing workplace. Candidates arrive as strategic time passes.",
                "Tutorial.BuildCasting", "construction.casting"), "casting.established", "hire", new[] { StarterFeatureIds.Casting }),
            new TutorialStep("hire", new GuideMessage("Hire your first employee",
                "Continue to let time run, then hire a waiting candidate in Staff. You will need actors, a director, and a writer to commission your first screenplay.",
                "Tutorial.HireEmployee", "hud.staff"), "employee.hired", "stage"),
            new TutorialStep("stage", new GuideMessage("Establish Stage 1",
                "Place and construct Stage 1, the first production facility. Its supported interior sets are reused by screenplay generation and filming.",
                "Tutorial.BuildStage", "construction.stage-1"), "stage.established", "screenplay", new[] { StarterFeatureIds.Stage }),
            new TutorialStep("screenplay", new GuideMessage("Create a screenplay",
                "In Screenplays, commission a screenplay from an available writer and choose its genre. Let writing finish, then inspect its characters and scenes. Existing player-authored screenplay submission also counts.",
                "Tutorial.CreateScreenplay", "hud.screenplays"), "screenplay.finalized", "prepare", new[] { StarterFeatureIds.SpecializedSets }),
            new TutorialStep("prepare", new GuideMessage("Prepare the movie",
                "Greenlight your guided screenplay. Assign a director and cast each role. Real casting and travel must finish before Ready to Film; production may then start automatically.",
                "Tutorial.CastMovie", "hud.productions"), "movie.prepared", "film"),
            new TutorialStep("film", new GuideMessage("Film the movie",
                "Continue to allow the crew to route to the stage and film. In Manual mode, use Keep Take or Shoot Again. Finish every scene; final quality must be calculated before release.",
                "Tutorial.StartFilming", "production.takes"), "movie.completed", "release"),
            new TutorialStep("release", new GuideMessage("Release the movie",
                "Release your completed guided movie in Productions. Its theatrical run and finance ledger track box-office receipts over time. Release concludes the guide and removes its remaining gates.",
                "Tutorial.ReleaseMovie", "production.release"), "movie.released", null)
        });
    }

    /// <summary>Optional overlay. Conditions are supplied from authoritative state by TutorialGameEvents.</summary>
    public sealed class TutorialSession : IDisposable
    {
        private readonly Dictionary<string, TutorialStep> _steps = new Dictionary<string, TutorialStep>();
        private readonly HashSet<string> _gated = new HashSet<string>();
        private readonly ISimulationControl _time;
        private readonly TutorialProgress _progress;
        private IDisposable _pause;
        private bool _presentationActive = true;
        public TutorialStatus Status => _progress.Status;
        public bool IsActive => Status == TutorialStatus.Active;
        public TutorialStep CurrentStep => IsActive ? _steps[_progress.CurrentStepId] : null;
        public GuideMessage OpenMessage => IsActive && _progress.MessageOpen ? CurrentStep.Message : null;
        public string ScreenplayId => _progress.ScreenplayId;
        public string MovieId => _progress.MovieId;
        public event Action Changed;

        public TutorialSession(NewStudioOptions options, ISimulationControl time,
            TutorialProgress saved = null, IReadOnlyList<TutorialStep> definitions = null)
        {
            _time = time ?? throw new ArgumentNullException(nameof(time));
            var sequence = definitions ?? InitialTutorialSequence.Create();
            if (sequence.Count == 0) throw new ArgumentException("A tutorial needs steps.");
            foreach (var step in sequence)
            {
                _steps.Add(step.Id, step);
                foreach (string id in step.EnterUnlocks) _gated.Add(id);
                foreach (string id in step.CompleteUnlocks) _gated.Add(id);
            }
            foreach (var step in sequence)
                if (step.NextStepId != null && !_steps.ContainsKey(step.NextStepId)) throw new ArgumentException("Unknown next step.");
            _progress = saved == null ? new TutorialProgress
            {
                Status = (options ?? throw new ArgumentNullException(nameof(options))).TutorialEnabled ? TutorialStatus.Active : TutorialStatus.Disabled,
                CurrentStepId = sequence[0].Id, MessageOpen = options.TutorialEnabled
            } : Copy(saved);
            if (_progress.Version != 1 || !Enum.IsDefined(typeof(TutorialStatus), Status) ||
                (IsActive && !_steps.ContainsKey(_progress.CurrentStepId))) throw new ArgumentException("Unsupported tutorial progress.");
            if (IsActive) Unlock(CurrentStep.EnterUnlocks);
            RefreshPause();
        }

        public bool Allows(string featureId) => !IsActive || !_gated.Contains(featureId) || _progress.UnlockedIds.Contains(featureId);
        public bool IsAvailable(string featureId, bool normallyAvailable) => normallyAvailable && Allows(featureId);
        public TutorialProgress Capture() => Copy(_progress);
        public void SetPresentationActive(bool active) { _presentationActive = active; RefreshPause(); }
        private static TutorialProgress Copy(TutorialProgress p) => new TutorialProgress
        {
            Version = p.Version, Status = p.Status, CurrentStepId = p.CurrentStepId,
            ScreenplayId = p.ScreenplayId, MovieId = p.MovieId, MessageOpen = p.MessageOpen,
            CompletedStepIds = new List<string>(p.CompletedStepIds ?? new List<string>()),
            UnlockedIds = new List<string>(p.UnlockedIds ?? new List<string>())
        };
        internal bool TrackScreenplay(string id)
        {
            if (!IsActive || string.IsNullOrEmpty(id)) return false;
            if (_progress.ScreenplayId == null) _progress.ScreenplayId = id;
            return _progress.ScreenplayId == id;
        }
        internal bool TrackMovie(string id, string screenplayId)
        {
            if (!IsActive || ScreenplayId == null || screenplayId != ScreenplayId) return false;
            if (_progress.MovieId == null) _progress.MovieId = id;
            return MovieId == id;
        }
        public void Continue()
        {
            if (OpenMessage == null) return;
            bool completes = OpenMessage.ContinueBehavior == GuideContinueBehavior.CompleteObjective;
            _progress.MessageOpen = false;
            RefreshPause();
            if (completes) CompleteCondition("guide.continue"); else Changed?.Invoke();
        }
        public void ReopenMessage()
        {
            if (!IsActive) return;
            _progress.MessageOpen = true; RefreshPause(); Changed?.Invoke();
        }
        internal void CompleteCondition(string condition)
        {
            if (!IsActive || CurrentStep.CompletionConditionId != condition) return;
            var step = CurrentStep;
            _progress.CompletedStepIds.Add(step.Id);
            Unlock(step.CompleteUnlocks);
            _progress.CurrentStepId = step.NextStepId;
            if (step.NextStepId == null) { _progress.Status = TutorialStatus.Completed; _progress.MessageOpen = false; }
            else { Unlock(CurrentStep.EnterUnlocks); _progress.MessageOpen = true; }
            RefreshPause(); Changed?.Invoke();
        }
        public void Skip()
        {
            if (!IsActive) return;
            _progress.Status = TutorialStatus.Skipped; _progress.MessageOpen = false;
            RefreshPause(); Changed?.Invoke();
        }
        private void Unlock(IReadOnlyList<string> ids)
        { foreach (string id in ids) if (!_progress.UnlockedIds.Contains(id)) _progress.UnlockedIds.Add(id); }
        private void RefreshPause()
        {
            _pause?.Dispose(); _pause = null;
            if (_presentationActive && OpenMessage?.PausePolicy == GuidePausePolicy.StrategicWhileOpen)
                _pause = _time.AcquirePause("tutorial.guide", CurrentStep.Id);
        }
        public void Dispose() { _presentationActive = false; _pause?.Dispose(); _pause = null; }
    }
}
