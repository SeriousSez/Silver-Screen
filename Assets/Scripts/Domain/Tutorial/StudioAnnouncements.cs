using System;
using System.Collections.Generic;
using SilverScreen.Domain.Time;

namespace SilverScreen.Domain.Tutorial
{
    public enum AnnouncementPriority { Low, Normal, Important, Urgent }
    public enum AnnouncementType { ProductionNeedsActor, ProductionNeedsDirector, ProductionReady, ProductionCompleted, BuildingCompleted }

    public sealed class AnnouncementRequest
    {
        public AnnouncementType Type { get; }
        public AnnouncementPriority Priority { get; }
        public string EntityId { get; }
        public long GameTimeSeconds { get; }
        public long CooldownSeconds { get; }
        public string TextFallback { get; }
        public string VoiceCueId { get; }
        public AnnouncementRequest(AnnouncementType type, AnnouncementPriority priority, string entityId,
            long gameTimeSeconds, long cooldownSeconds = 21600, string textFallback = null, string voiceCueId = null)
        {
            if (string.IsNullOrWhiteSpace(entityId)) throw new ArgumentException("Announcements need an entity ID.");
            if (cooldownSeconds < 0 || gameTimeSeconds < 0) throw new ArgumentOutOfRangeException(nameof(cooldownSeconds));
            Type = type; Priority = priority; EntityId = entityId; GameTimeSeconds = gameTimeSeconds;
            CooldownSeconds = cooldownSeconds; TextFallback = textFallback; VoiceCueId = voiceCueId;
        }
    }

    /// <summary>No simulation control dependency: delivery cannot acquire a pause.</summary>
    public sealed class StudioAnnouncementQueue
    {
        private readonly List<AnnouncementRequest> _pending = new List<AnnouncementRequest>();
        private readonly Dictionary<(AnnouncementType, string), long> _last = new Dictionary<(AnnouncementType, string), long>();
        public int PendingCount => _pending.Count;
        public event Action Changed;
        public bool Request(AnnouncementRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            var key = (request.Type, request.EntityId);
            int index = _pending.FindIndex(item => item.Type == request.Type && item.EntityId == request.EntityId);
            if (index >= 0)
            {
                if (request.Priority <= _pending[index].Priority) return false;
                _pending[index] = request; Changed?.Invoke(); return true;
            }
            if (_last.TryGetValue(key, out long last) && request.GameTimeSeconds - last < request.CooldownSeconds) return false;
            _last[key] = request.GameTimeSeconds;
            _pending.Add(request); Changed?.Invoke(); return true;
        }
        public AnnouncementRequest TakeNext()
        {
            if (_pending.Count == 0) return null;
            int best = 0;
            for (int i = 1; i < _pending.Count; i++) if (_pending[i].Priority > _pending[best].Priority) best = i;
            var next = _pending[best]; _pending.RemoveAt(best); Changed?.Invoke(); return next;
        }
        public void Resolve(AnnouncementType type, string entityId)
        {
            // A resolved condition can announce immediately if it genuinely recurs later.
            _last.Remove((type, entityId));
            if (_pending.RemoveAll(item => item.Type == type && item.EntityId == entityId) > 0) Changed?.Invoke();
        }
    }

    /// <summary>Fallback presentation catalog. Semantic type is authoritative, never these English strings.</summary>
    public static class StudioAnnouncementPresentation
    {
        private static readonly Dictionary<AnnouncementType, string> Text = new Dictionary<AnnouncementType, string>
        {
            [AnnouncementType.BuildingCompleted] = "Construction is complete. A new studio facility is operational.",
            [AnnouncementType.ProductionNeedsActor] = "Attention. An actor is needed for a production. Visit the Casting Office.",
            [AnnouncementType.ProductionNeedsDirector] = "A director is required for a production.",
            [AnnouncementType.ProductionReady] = "A production is ready to film. Check its assigned production facility.",
            [AnnouncementType.ProductionCompleted] = "Production has been completed. Review the movie for release."
        };
        public static string ResolveText(AnnouncementRequest request) => request.TextFallback ?? Text[request.Type];
        public static string ResolveVoice(AnnouncementRequest request) => request.VoiceCueId ?? "StudioPA." + request.Type;
    }
}
