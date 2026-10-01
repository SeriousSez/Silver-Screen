using SilverScreen.Domain;
using SilverScreen.Domain.Movie;

namespace SilverScreen.Presentation.Buildings
{
    /// <summary>Local slate then beats. Completion and failure have no production/work callback.</summary>
    internal sealed class StudioTakePresentationSession : IPresentationSession
    {
        private readonly PrototypeSlateSequence _slate;
        private readonly PrototypeScreenplayBeatSequence _beats;
        private readonly TakePresentationSnapshot _snapshot;
        public PresentationStatus Status { get; private set; } = PresentationStatus.Running;

        public StudioTakePresentationSession(TakePresentationSnapshot snapshot, PrototypeSlateSequence slate, PrototypeScreenplayBeatSequence beats)
        {
            _snapshot = snapshot; _slate = slate; _beats = beats;
            if (_slate == null || _beats == null) { Status = PresentationStatus.Failed; return; }
            var started = _slate.TryBegin(snapshot.MovieTitle, snapshot.SceneNumber, snapshot.TakeNumber, BeginBeats, _ => Fail());
            if (started != StudioRouteResult.Started) Status = PresentationStatus.Failed;
        }

        private void BeginBeats()
        {
            if (Status != PresentationStatus.Running) return;
            if (_snapshot.Beats.Count == 0) { Status = PresentationStatus.Completed; return; }
            var started = _beats.TryBegin(_snapshot, () => { if (Status == PresentationStatus.Running) Status = PresentationStatus.Completed; }, _ => Fail());
            if (started != StudioRouteResult.Started) Fail();
        }
        private void Fail() { if (Status == PresentationStatus.Running) Status = PresentationStatus.Failed; }
        public void Dispose()
        {
            if (Status == PresentationStatus.Canceled) return;
            if (Status == PresentationStatus.Running)
            {
                Status = PresentationStatus.Canceled;
                if (_slate != null) _slate.Cancel();
                if (_beats != null) _beats.Cancel();
            }
        }
    }
}
