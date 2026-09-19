using System;

namespace SilverScreen.Domain.Movie
{
    public enum MovieTakeStatus
    {
        Preparing,
        Recording,
        Completed,
        Discarded
    }

    public sealed class MovieTake
    {
        public string Id { get; }
        public int TakeNumber { get; }
        public MovieTakeStatus Status { get; private set; }
        public TimeSpan FilmedDuration { get; private set; }
        public bool IsSelectedForFinalCut { get; private set; }

        internal MovieTake(string id, int takeNumber, MovieTakeStatus initialStatus)
        {
            if (takeNumber < 1) throw new ArgumentOutOfRangeException(nameof(takeNumber));

            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id.Trim();
            TakeNumber = takeNumber;
            Status = initialStatus;
            FilmedDuration = TimeSpan.Zero;
        }

        internal bool BeginRecording()
        {
            if (Status != MovieTakeStatus.Preparing) return false;
            Status = MovieTakeStatus.Recording;
            return true;
        }

        internal bool Complete(TimeSpan filmedDuration)
        {
            if (Status != MovieTakeStatus.Recording || filmedDuration < TimeSpan.Zero) return false;
            FilmedDuration = filmedDuration;
            Status = MovieTakeStatus.Completed;
            return true;
        }

        internal bool Discard()
        {
            if (Status == MovieTakeStatus.Discarded) return false;
            Status = MovieTakeStatus.Discarded;
            IsSelectedForFinalCut = false;
            return true;
        }

        internal void SetSelectedForFinalCut(bool selected)
        {
            IsSelectedForFinalCut = selected;
        }
    }
}