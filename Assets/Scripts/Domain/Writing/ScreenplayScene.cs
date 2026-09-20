using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Writing
{
    public enum ScreenplaySceneLocation
    {
        Interior,
        Exterior
    }

    public enum ScreenplayTimeOfDay
    {
        Day,
        Night,
        Dawn,
        Dusk
    }

    [Serializable]
    public sealed class ScreenplayScene
    {
        private readonly List<string> _participatingCharacterIds = new List<string>();
        private readonly List<ScreenplayBeat> _beats = new List<ScreenplayBeat>();

        public string Id { get; }
        public int SceneNumber { get; internal set; }
        public string Title { get; }
        public string LocationId { get; }
        public ScreenplaySceneLocation LocationType { get; }
        public ScreenplayTimeOfDay TimeOfDay { get; }
        public IReadOnlyList<string> ParticipatingCharacterIds => _participatingCharacterIds;
        public IReadOnlyList<ScreenplayBeat> Beats => _beats;

        public ScreenplayScene(string id, int sceneNumber, string locationId,
            ScreenplaySceneLocation locationType, ScreenplayTimeOfDay timeOfDay, string title = null)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("A screenplay scene requires a stable ID.", nameof(id));
            if (sceneNumber < 1) throw new ArgumentOutOfRangeException(nameof(sceneNumber));
            if (string.IsNullOrWhiteSpace(locationId))
                throw new ArgumentException("A screenplay scene requires a semantic location ID.", nameof(locationId));

            Id = id.Trim();
            SceneNumber = sceneNumber;
            LocationId = locationId.Trim();
            LocationType = locationType;
            TimeOfDay = timeOfDay;
            Title = string.IsNullOrWhiteSpace(title) ? string.Empty : title.Trim();
        }

        public bool AddCharacter(string characterId)
        {
            string id = NormalizeId(characterId);
            if (id == null || _participatingCharacterIds.Contains(id)) return false;
            _participatingCharacterIds.Add(id);
            return true;
        }

        public bool AddBeat(ScreenplayBeat beat)
        {
            if (beat == null || _beats.Exists(existing => existing.Id == beat.Id) ||
                !ReferencesParticipant(beat.PerformingCharacterId) ||
                !ReferencesParticipant(beat.TargetCharacterId)) return false;
            _beats.Add(beat);
            RenumberBeats();
            return true;
        }

        public ScreenplayBeat GetBeat(string beatId) =>
            string.IsNullOrWhiteSpace(beatId) ? null : _beats.Find(beat => beat.Id == beatId.Trim());

        private bool ReferencesParticipant(string characterId) =>
            characterId == null || _participatingCharacterIds.Contains(characterId);

        private void RenumberBeats()
        {
            for (int i = 0; i < _beats.Count; i++) _beats[i].SetOrder(i + 1);
        }

        private static string NormalizeId(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
