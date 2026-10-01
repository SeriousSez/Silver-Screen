using System;
using System.Collections.Generic;
using SilverScreen.Domain.Resources;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Work;

namespace SilverScreen.Domain.Movie
{
    /// <summary>The temporary 480-unit workload is isolated here, not tied to animation length or an employment role.</summary>
    public sealed class LegacyFilmingWorkAdapter : IDisposable
    {
        public const string MeasureId = "legacy-filming-work";
        public const int UnitsPerTake = 480;
        private readonly WorkService _work;
        private readonly ResourceReservationBook _reservations;
        private ResourceReservation _claim;
        public string ActiveWorkId { get; private set; }
        public event Action<WorkSnapshot> Completed;

        public LegacyFilmingWorkAdapter(WorkService work, ResourceReservationBook reservations)
        {
            _work = work ?? throw new ArgumentNullException(nameof(work));
            _reservations = reservations ?? throw new ArgumentNullException(nameof(reservations));
            _work.Changed += HandleChanged;
        }

        public bool TryStart(string movieId, string takeId, string facilityId, IEnumerable<WorkAssignment> team, out string failure)
        {
            var assignments = new List<WorkAssignment>(team);
            var keys = new List<ResourceKey> { new ResourceKey("facility", facilityId) };
            foreach (var assignment in assignments) keys.Add(assignment.Resource);
            string id = "filming:" + takeId;
            IReadOnlyList<ResourceConflict> conflicts;
            ResourceReservation next;
            bool acquired = _claim == null
                ? _reservations.TryAcquire(id, keys, out next, out conflicts)
                : _reservations.TryReplace(_claim.Id, id, keys, out next, out conflicts);
            if (!acquired)
            {
                failure = "Resource " + conflicts[0].Resource + " is reserved by " + conflicts[0].OwnerId;
                return false;
            }
            _claim = next;
            ActiveWorkId = id;
            _work.Create(new WorkDefinition(id, movieId, "principal-photography-take", WorkQuantity.FromUnits(MeasureId, UnitsPerTake)));
            _work.SetAssignments(id, assignments);
            _work.RefreshCapacity(id, new WorkRate(WorkQuantity.FromUnits(MeasureId, 1), SimulationDuration.FromMinutes(1)));
            failure = null;
            return true;
        }

        public WorkSnapshot GetSnapshot() => ActiveWorkId == null ? null : _work.GetSnapshot(ActiveWorkId);
        public void Release()
        {
            var id = ActiveWorkId;
            ActiveWorkId = null;
            if (id != null && !_work.IsDisposed) _work.Cancel(id);
            if (_claim != null) _reservations.Release(_claim.Id);
            _claim = null;
        }
        private void HandleChanged(WorkSnapshot snapshot)
        {
            if (snapshot.Definition.Id == ActiveWorkId && snapshot.Status == WorkStatus.Completed) Completed?.Invoke(snapshot);
        }
        public void Dispose() { _work.Changed -= HandleChanged; Release(); Completed = null; }
    }
}
