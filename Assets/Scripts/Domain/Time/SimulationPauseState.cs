using System;
using System.Collections.Generic;

namespace SilverScreen.Domain.Time
{
    /// <summary>Session-owned holds; presentation leases are reconstructed, never restored as orphaned holds.</summary>
    public sealed class SimulationPauseState
    {
        private readonly Dictionary<long, string> _holds = new Dictionary<long, string>();
        private long _nextId;
        public bool UserPaused { get; private set; }
        public bool IsPaused => UserPaused || _holds.Count != 0;
        public event Action Changed;

        public void SetUserPaused(bool paused)
        {
            if (UserPaused == paused) return;
            UserPaused = paused;
            Changed?.Invoke();
        }

        public IDisposable Acquire(string ownerId, string reason)
        {
            if (string.IsNullOrWhiteSpace(ownerId) || string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("Pause holds require an owner and reason.");
            long id = checked(++_nextId);
            _holds.Add(id, ownerId + ": " + reason);
            Changed?.Invoke();
            return new Lease(this, id);
        }

        private sealed class Lease : IDisposable
        {
            private SimulationPauseState _owner;
            private readonly long _id;
            public Lease(SimulationPauseState owner, long id) { _owner = owner; _id = id; }
            public void Dispose()
            {
                var owner = _owner;
                if (owner == null) return;
                _owner = null;
                owner._holds.Remove(_id);
                owner.Changed?.Invoke();
            }
        }
    }
}
