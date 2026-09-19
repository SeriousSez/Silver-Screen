using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Writing
{
    public enum ScreenplayStatus { Assigned, Writing, Completed }
    public enum WriterParticipationStatus { Assigned, Traveling, Writing, Unavailable, Credited }
    public enum ScreenplayAcquisitionSource { Commissioned, Purchased, PlayerCreated }

    [Serializable]
    public sealed class ScreenplayWriterContributor
    {
        public string WriterId { get; }
        public WriterParticipationStatus Status { get; private set; }
        public bool HasContributed { get; private set; }

        internal ScreenplayWriterContributor(string writerId)
        {
            WriterId = writerId;
            Status = WriterParticipationStatus.Assigned;
        }

        internal void SetStatus(WriterParticipationStatus status)
        {
            Status = status;
            if (status == WriterParticipationStatus.Writing) HasContributed = true;
        }
    }

    [Serializable]
    public sealed class ScreenplayProject
    {
        private readonly List<ScreenplayWriterContributor> _contributors = new List<ScreenplayWriterContributor>();
        private readonly List<string> _creditedWriterIds = new List<string>();
        private readonly List<string> _genreIds = new List<string>();

        public string Id { get; }
        public string Title { get; }
        public IReadOnlyList<string> GenreIds => _genreIds;
        public string PrimaryGenreId => _genreIds[0];
        public ScreenplayAcquisitionSource AcquisitionSource { get; }
        public ScreenplayStatus Status { get; private set; }
        public double Progress { get; private set; }
        public IReadOnlyList<ScreenplayWriterContributor> Contributors => _contributors;
        public IReadOnlyList<string> CreditedWriterIds => _creditedWriterIds;
        public event Action<ScreenplayProject> Changed;

        public ScreenplayProject(string id, string title, IEnumerable<string> writerIds,
            IEnumerable<string> genreIds, ScreenplayAcquisitionSource acquisitionSource = ScreenplayAcquisitionSource.Commissioned)
        {
            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id.Trim();
            Title = string.IsNullOrWhiteSpace(title) ? "Untitled Screenplay" : title.Trim();
            AcquisitionSource = acquisitionSource;
            if (genreIds == null) throw new ArgumentNullException(nameof(genreIds));
            foreach (string genreId in genreIds)
            {
                if (string.IsNullOrWhiteSpace(genreId)) throw new ArgumentException("Genre IDs cannot be empty.", nameof(genreIds));
                string normalized = genreId.Trim().ToLowerInvariant();
                if (_genreIds.Contains(normalized)) throw new ArgumentException("Duplicate genre IDs are not allowed.", nameof(genreIds));
                _genreIds.Add(normalized);
            }
            if (_genreIds.Count == 0) throw new ArgumentException("A screenplay requires at least one genre.", nameof(genreIds));
            if (writerIds == null) throw new ArgumentNullException(nameof(writerIds));
            foreach (string writerId in writerIds)
            {
                if (string.IsNullOrWhiteSpace(writerId) || GetContributor(writerId) != null) continue;
                _contributors.Add(new ScreenplayWriterContributor(writerId.Trim()));
            }
            if (_contributors.Count == 0) throw new ArgumentException("A screenplay requires at least one writer.", nameof(writerIds));
            Status = ScreenplayStatus.Assigned;
        }

        public bool TryAddGenre(string genreId)
        {
            if (string.IsNullOrWhiteSpace(genreId)) return false;
            string normalized = genreId.Trim().ToLowerInvariant();
            if (_genreIds.Contains(normalized)) return false;
            _genreIds.Add(normalized);
            Changed?.Invoke(this);
            return true;
        }

        public ScreenplayWriterContributor GetContributor(string writerId) =>
            _contributors.Find(item => item.WriterId == writerId);

        public bool SetParticipation(string writerId, WriterParticipationStatus status)
        {
            if (Status == ScreenplayStatus.Completed) return false;
            var contributor = GetContributor(writerId);
            if (contributor == null) return false;
            contributor.SetStatus(status);
            if (status == WriterParticipationStatus.Writing) Status = ScreenplayStatus.Writing;
            Changed?.Invoke(this);
            return true;
        }

        public bool AdvanceWritingMinute(IReadOnlyDictionary<string, int> activeWriterSkills)
        {
            if (Status == ScreenplayStatus.Completed || activeWriterSkills == null || activeWriterSkills.Count == 0)
                return false;

            var contributions = new List<double>();
            foreach (var contributor in _contributors)
            {
                if (contributor.Status != WriterParticipationStatus.Writing ||
                    !activeWriterSkills.TryGetValue(contributor.WriterId, out int skill)) continue;
                contributions.Add(0.85d + Math.Clamp(skill, 0, 100) * 0.003d);
            }
            if (contributions.Count == 0) return false;
            contributions.Sort((a, b) => b.CompareTo(a));
            double[] collaborationWeights = { 1d, 0.65d, 0.4d, 0.25d };
            double effort = 0d;
            for (int i = 0; i < contributions.Count; i++)
                effort += contributions[i] * collaborationWeights[Math.Min(i, collaborationWeights.Length - 1)];

            Progress = Math.Min(1d, Progress + effort / 480d);
            if (Progress >= 1d) Complete();
            Changed?.Invoke(this);
            return true;
        }

        private void Complete()
        {
            Progress = 1d;
            Status = ScreenplayStatus.Completed;
            _creditedWriterIds.Clear();
            foreach (var contributor in _contributors)
            {
                if (!contributor.HasContributed) continue;
                contributor.SetStatus(WriterParticipationStatus.Credited);
                _creditedWriterIds.Add(contributor.WriterId);
            }
        }
    }
}
