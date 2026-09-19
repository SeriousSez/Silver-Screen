using System;

namespace SilverScreen.Domain.Movie
{
    public enum ShotType
    {
        Wide,
        Medium,
        CloseUp
    }

    public sealed class SceneShot
    {
        public string Id { get; }
        public int Order { get; private set; }
        public ShotType ShotType { get; }
        public string SubjectCharacterId { get; }
        public string ScreenplayBeatId { get; }

        public SceneShot(
            string id,
            int order,
            ShotType shotType,
            string subjectCharacterId = null,
            string screenplayBeatId = null)
        {
            if (order < 1) throw new ArgumentOutOfRangeException(nameof(order));

            Id = string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString() : id.Trim();
            Order = order;
            ShotType = shotType;
            SubjectCharacterId = NormalizeOptionalId(subjectCharacterId);
            ScreenplayBeatId = NormalizeOptionalId(screenplayBeatId);
        }

        internal void SetOrder(int order)
        {
            Order = order;
        }

        private static string NormalizeOptionalId(string value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
