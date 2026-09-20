using System;
using System.Collections.Generic;
using SilverScreen.Domain.Writing;

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
        private readonly HashSet<string> _participatingCharacterIds = new HashSet<string>();
        private readonly List<MovieTake> _takes = new List<MovieTake>();
        private readonly List<ScreenplayBeat> _beats = new List<ScreenplayBeat>();
        private readonly List<SceneShot> _shots = new List<SceneShot>();

        public string Id { get; }
        public int SceneNumber { get; private set; }
        public string Title { get; private set; }
        public string Description { get; private set; }
        public string SetLocationId { get; private set; }
        public string RequiredSetDefinitionId { get; }
        public string SourceScreenplaySceneId { get; }
        public string LocationTypeId { get; }
        public string TimeOfDayId { get; }
        public MovieSceneStatus Status { get; private set; }
        public IReadOnlyCollection<string> ParticipatingPersonIds => _participatingPersonIds;
        public IReadOnlyCollection<string> ParticipatingCharacterIds => _participatingCharacterIds;
        // Compatibility alias for scenes created before fictional cast roles were explicit.
        public IReadOnlyCollection<string> ParticipatingRoleIds => _participatingCharacterIds;
        public IReadOnlyList<MovieTake> Takes => _takes;
        public IReadOnlyList<ScreenplayBeat> Beats => _beats;
        public IReadOnlyList<SceneShot> Shots => _shots;
        public MovieTake SelectedTake => _takes.Find(take => take.IsSelectedForFinalCut);

        public event Action<MovieScene> OnSceneUpdated;

        public MovieScene(
            string id,
            int sceneNumber,
            string setLocationId,
            string title = null,
            string description = null,
            string sourceScreenplaySceneId = null,
            string locationTypeId = null,
            string timeOfDayId = null,
            string requiredSetDefinitionId = SetDefinitionIds.GenericInterior)
        {
            if (sceneNumber < 1) throw new ArgumentOutOfRangeException(nameof(sceneNumber));
            if (string.IsNullOrWhiteSpace(setLocationId))
                throw new ArgumentException("A scene requires a set or location identifier.", nameof(setLocationId));
            if (string.IsNullOrWhiteSpace(requiredSetDefinitionId))
                throw new ArgumentException("A scene requires a set definition identifier.", nameof(requiredSetDefinitionId));

            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id.Trim();
            SceneNumber = sceneNumber;
            SetLocationId = setLocationId.Trim();
            RequiredSetDefinitionId = requiredSetDefinitionId.Trim().ToLowerInvariant();
            Title = title?.Trim();
            Description = description?.Trim();
            SourceScreenplaySceneId = NormalizeOptional(sourceScreenplaySceneId);
            LocationTypeId = NormalizeOptional(locationTypeId);
            TimeOfDayId = NormalizeOptional(timeOfDayId);
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

        public bool AddCharacter(string characterId)
        {
            bool changed = AddIdentifier(_participatingCharacterIds, characterId);
            if (changed) OnSceneUpdated?.Invoke(this);
            return changed;
        }

        public bool RemoveCharacter(string characterId)
        {
            string normalizedId = string.IsNullOrWhiteSpace(characterId) ? null : characterId.Trim();
            if (normalizedId == null ||
                _beats.Exists(beat =>
                    beat.PerformingCharacterId == normalizedId ||
                    beat.TargetCharacterId == normalizedId) ||
                _shots.Exists(shot => shot.SubjectCharacterId == normalizedId))
            {
                return false;
            }

            bool changed = RemoveIdentifier(_participatingCharacterIds, normalizedId);
            if (changed) OnSceneUpdated?.Invoke(this);
            return changed;
        }

        public bool HasCharacter(string characterId) =>
            !string.IsNullOrWhiteSpace(characterId) &&
            _participatingCharacterIds.Contains(characterId.Trim());

        public bool AddBeat(ScreenplayBeat beat)
        {
            if (beat == null ||
                _beats.Exists(existing => existing.Id == beat.Id) ||
                !ReferencesParticipatingCharacters(beat))
            {
                return false;
            }

            int insertionIndex = Math.Clamp(beat.Order - 1, 0, _beats.Count);
            _beats.Insert(insertionIndex, beat);
            RenumberBeats();
            OnSceneUpdated?.Invoke(this);
            return true;
        }

        public bool RemoveBeat(string beatId)
        {
            var beat = GetBeat(beatId);
            if (beat == null || _shots.Exists(shot => shot.ScreenplayBeatId == beat.Id)) return false;

            _beats.Remove(beat);
            RenumberBeats();
            OnSceneUpdated?.Invoke(this);
            return true;
        }

        public bool ReorderBeat(string beatId, int newOrder)
        {
            var beat = GetBeat(beatId);
            if (beat == null || newOrder < 1 || newOrder > _beats.Count) return false;

            int currentIndex = _beats.IndexOf(beat);
            int newIndex = newOrder - 1;
            if (currentIndex == newIndex) return true;

            _beats.RemoveAt(currentIndex);
            _beats.Insert(newIndex, beat);
            RenumberBeats();
            OnSceneUpdated?.Invoke(this);
            return true;
        }

        public ScreenplayBeat GetBeat(string beatId) =>
            string.IsNullOrWhiteSpace(beatId) ? null : _beats.Find(beat => beat.Id == beatId);

        public bool AddShot(SceneShot shot)
        {
            if (shot == null ||
                _shots.Exists(existing => existing.Id == shot.Id) ||
                (shot.SubjectCharacterId != null && !HasCharacter(shot.SubjectCharacterId)) ||
                (shot.ScreenplayBeatId != null && GetBeat(shot.ScreenplayBeatId) == null))
            {
                return false;
            }

            int insertionIndex = Math.Clamp(shot.Order - 1, 0, _shots.Count);
            _shots.Insert(insertionIndex, shot);
            RenumberShots();
            OnSceneUpdated?.Invoke(this);
            return true;
        }

        public bool RemoveShot(string shotId)
        {
            var shot = GetShot(shotId);
            if (shot == null) return false;

            _shots.Remove(shot);
            RenumberShots();
            OnSceneUpdated?.Invoke(this);
            return true;
        }

        public bool ReorderShot(string shotId, int newOrder)
        {
            var shot = GetShot(shotId);
            if (shot == null || newOrder < 1 || newOrder > _shots.Count) return false;

            int currentIndex = _shots.IndexOf(shot);
            int newIndex = newOrder - 1;
            if (currentIndex == newIndex) return true;

            _shots.RemoveAt(currentIndex);
            _shots.Insert(newIndex, shot);
            RenumberShots();
            OnSceneUpdated?.Invoke(this);
            return true;
        }

        public SceneShot GetShot(string shotId) =>
            string.IsNullOrWhiteSpace(shotId) ? null : _shots.Find(shot => shot.Id == shotId);

        public SceneShot GetShotForBeat(string beatId) =>
            string.IsNullOrWhiteSpace(beatId)
                ? null
                : _shots.Find(shot => shot.ScreenplayBeatId == beatId);

        public bool AddParticipant(string personId, string roleId = null)
        {
            bool changed = AddIdentifier(_participatingPersonIds, personId);
            changed |= AddIdentifier(_participatingCharacterIds, roleId);
            if (changed) OnSceneUpdated?.Invoke(this);
            return changed;
        }

        public bool RemoveParticipant(string personId, string roleId = null)
        {
            bool changed = RemoveIdentifier(_participatingPersonIds, personId);
            changed |= RemoveIdentifier(_participatingCharacterIds, roleId);
            if (changed) OnSceneUpdated?.Invoke(this);
            return changed;
        }

        public bool MarkReady() => TrySetStatus(MovieSceneStatus.Planned, MovieSceneStatus.Ready);

        public bool BeginFilming() => TrySetStatus(MovieSceneStatus.Ready, MovieSceneStatus.Filming);

        public bool PrepareForRetake() => TrySetStatus(MovieSceneStatus.Filming, MovieSceneStatus.Ready);

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

        private bool ReferencesParticipatingCharacters(ScreenplayBeat beat)
        {
            return (beat.PerformingCharacterId == null || HasCharacter(beat.PerformingCharacterId)) &&
                   (beat.TargetCharacterId == null || HasCharacter(beat.TargetCharacterId));
        }

        private void RenumberBeats()
        {
            for (int index = 0; index < _beats.Count; index++)
                _beats[index].SetOrder(index + 1);
        }

        private void RenumberShots()
        {
            for (int index = 0; index < _shots.Count; index++)
                _shots[index].SetOrder(index + 1);
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

        private static string NormalizeOptional(string value) =>
            string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }
}
