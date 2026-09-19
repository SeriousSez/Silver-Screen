using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Movie
{
    public enum MovieSceneStatus
    {
        Planned,
        Ready,
        Filming,
        Completed
    }

    public sealed class MovieScene
    {
        private readonly HashSet<string> _participatingPersonIds = new HashSet<string>();
        private readonly HashSet<string> _participatingRoleIds = new HashSet<string>();
        private readonly List<MovieTake> _takes = new List<MovieTake>();

        public string Id { get; }
        public int SceneNumber { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string SetLocationId { get; private set; }
        public MovieSceneStatus Status { get; private set; }
        public IReadOnlyCollection<string> ParticipatingPersonIds => _participatingPersonIds;
        public IReadOnlyCollection<string> ParticipatingRoleIds => _participatingRoleIds;
        public IReadOnlyList<MovieTake> Takes => _takes;
        public MovieTake SelectedTake => _takes.Find(take => take.IsSelectedForFinalCut);

        public event Action<MovieScene> OnSceneUpdated;

        public MovieScene(
            string id,
            int sceneNumber,
            string setLocationId,
            string title = null,
            string description = null)
        {
            if (sceneNumber < 1) throw new ArgumentOutOfRangeException(nameof(sceneNumber));
            if (string.IsNullOrWhiteSpace(setLocationId))
                throw new ArgumentException("A scene requires a set or location identifier.", nameof(setLocationId));

            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id.Trim();
            SceneNumber = sceneNumber;
            SetLocationId = setLocationId.Trim();
            Title = title?.Trim();
            Description = description?.Trim();
            Status = MovieSceneStatus.Planned;
        }

        public void UpdateDetails(string title, string description, string setLocationId)
        {
            if (string.IsNullOrWhiteSpace(setLocationId))
                throw new ArgumentException("A scene requires a set or location identifier.", nameof(setLocationId));

            Title = title?.Trim();
            Description = description?.Trim();
            SetLocationId = setLocationId.Trim();
            OnSceneUpdated?.Invoke(this);
        }

        public bool AddParticipant(string personId, string roleId = null)
        {
            bool changed = AddIdentifier(_participatingPersonIds, personId);
            changed |= AddIdentifier(_participatingRoleIds, roleId);
            if (changed) OnSceneUpdated?.Invoke(this);
            return changed;
        }

        public bool RemoveParticipant(string personId, string roleId = null)
        {
            bool changed = RemoveIdentifier(_participatingPersonIds, personId);
            changed |= RemoveIdentifier(_participatingRoleIds, roleId);
            if (changed) OnSceneUpdated?.Invoke(this);
            return changed;
        }

        public bool MarkReady() => TrySetStatus(MovieSceneStatus.Planned, MovieSceneStatus.Ready);

        public bool BeginFilming() => TrySetStatus(MovieSceneStatus.Ready, MovieSceneStatus.Filming);

        public bool CompleteFilming() => TrySetStatus(MovieSceneStatus.Filming, MovieSceneStatus.Completed);

        public MovieTake PrepareTake(string takeId = null)
        {
            var take = new MovieTake(takeId, _takes.Count + 1, MovieTakeStatus.Preparing);
            _takes.Add(take);
            OnSceneUpdated?.Invoke(this);
            return take;
        }

        public MovieTake BeginTake(string takeId = null)
        {
            var take = PrepareTake(takeId);
            take.BeginRecording();
            OnSceneUpdated?.Invoke(this);
            return take;
        }

        public bool BeginRecording(string takeId)
        {
            var take = GetTake(takeId);
            if (take == null || !take.BeginRecording()) return false;
            OnSceneUpdated?.Invoke(this);
            return true;
        }

        public bool CompleteTake(string takeId, TimeSpan filmedDuration)
        {
            var take = GetTake(takeId);
            if (take == null || !take.Complete(filmedDuration)) return false;
            OnSceneUpdated?.Invoke(this);
            return true;
        }

        public bool DiscardTake(string takeId)
        {
            var take = GetTake(takeId);
            if (take == null || !take.Discard()) return false;
            OnSceneUpdated?.Invoke(this);
            return true;
        }

        public bool SelectTake(string takeId)
        {
            var selected = GetTake(takeId);
            if (selected == null || selected.Status != MovieTakeStatus.Completed) return false;

            foreach (var take in _takes)
                take.SetSelectedForFinalCut(take == selected);

            OnSceneUpdated?.Invoke(this);
            return true;
        }

        public MovieTake GetTake(string takeId) =>
            string.IsNullOrWhiteSpace(takeId) ? null : _takes.Find(take => take.Id == takeId);

        internal void SetSceneNumber(int sceneNumber)
        {
            SceneNumber = sceneNumber;
        }

        private bool TrySetStatus(MovieSceneStatus expected, MovieSceneStatus next)
        {
            if (Status != expected) return false;
            Status = next;
            OnSceneUpdated?.Invoke(this);
            return true;
        }

        private static bool AddIdentifier(HashSet<string> identifiers, string value)
        {
            return !string.IsNullOrWhiteSpace(value) && identifiers.Add(value.Trim());
        }

        private static bool RemoveIdentifier(HashSet<string> identifiers, string value)
        {
            return !string.IsNullOrWhiteSpace(value) && identifiers.Remove(value.Trim());
        }
    }
}