using System;
using System.Collections.Generic;
using System.Linq;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Resources;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Work;

namespace SilverScreen.Domain.Buildings
{
    [Serializable]
    public sealed class MaintenanceAssignmentSnapshot
    {
        public string EmployeeId, BuildingId;
        public MaintenanceTaskKind Kind;
        public bool Arrived;
    }

    public sealed class ConstructionSiteStatus
    {
        public decimal TotalWorkRequired { get; }
        public decimal WorkCompleted { get; }
        public ConstructionPhase Phase { get; }
        public int ActiveWorkers { get; }
        public int UsefulWorkerCapacity { get; }
        public decimal RelativeProductivity { get; }
        public ConstructionSiteStatus(decimal total, decimal completed, ConstructionPhase phase, int activeWorkers, int capacity, decimal productivity)
        { TotalWorkRequired = total; WorkCompleted = completed; Phase = phase; ActiveWorkers = activeWorkers; UsefulWorkerCapacity = capacity; RelativeProductivity = productivity; }
    }

    public sealed class BuildingConstructionService : IOperationalBuildings, IDisposable
    {
        private sealed class Assignment
        {
            public Employee Employee;
            public PlacedBuilding Building;
            public bool Arrived;
            public long ReservationId;
            public int WorkSpotIndex;
        }
        public const string WorkMeasure = "construction-work";
        public const decimal ConstructionExperiencePerSkillPoint = 8m;
        private readonly Dictionary<string, BuildingDefinition> _definitions;
        private readonly List<PlacedBuilding> _buildings = new List<PlacedBuilding>();
        private readonly Dictionary<string, Assignment> _assignments = new Dictionary<string, Assignment>();
        private readonly WorkService _work;
        private readonly SimulationClock _clock;
        private readonly IStudioFinanceService _finances;
        private readonly ResourceReservationBook _resources;
        private readonly Func<string, bool> _allowed;
        private readonly Func<BuildingDefinition, BuildingPose, bool> _entranceReachable;
        private readonly Func<BuildingDefinition, BuildingPose, string> _worldValidation;
        private readonly PlacementRect _lot;
        private readonly List<PlacementRect> _forbidden;
        private int _nextStageNumber = 1;
        private bool _placing;
        public IReadOnlyList<BuildingDefinition> Definitions { get; }
        public IReadOnlyList<PlacedBuilding> Buildings => _buildings.AsReadOnly();
        public event Action<PlacedBuilding> SitePlaced;
        public event Action<PlacedBuilding> Changed;
        public event Action<PlacedBuilding> Operational;
        public event Action<PlacedBuilding> Cancelled;
        public event Action<string> Established;
        public BuildingConstructionService(IEnumerable<BuildingDefinition> definitions, PlacementRect lot,
            IEnumerable<PlacementRect> forbidden, SimulationClock clock, WorkService work,
            IStudioFinanceService finances, ResourceReservationBook resources,
            Func<string, bool> allowed = null, Func<BuildingDefinition, BuildingPose, bool> entranceReachable = null,
            Func<BuildingDefinition, BuildingPose, string> worldValidation = null)
        {
            _definitions = definitions.ToDictionary(d => d.Id);
            Definitions = Array.AsReadOnly(_definitions.Values.ToArray()); _lot = lot;
            _forbidden = new List<PlacementRect>(forbidden ?? Array.Empty<PlacementRect>());
            _clock = clock; _work = work; _finances = finances; _resources = resources;
            _allowed = allowed ?? (_ => true); _entranceReachable = entranceReachable ?? ((d, p) => true);
            _worldValidation = worldValidation;
            _work.Changed += WorkChanged;
        }
        public bool IsEstablished(string id) => _buildings.Any(b => b.Definition.Id == id && b.IsOperational);
        public bool IsAvailable(string id) => _definitions.TryGetValue(id, out var d) &&
            _clock.CurrentTime.Year >= d.AvailableFromYear && _allowed(id);
        public string AvailabilityReason(string id)
        {
            if (!_definitions.TryGetValue(id, out var d)) return "Content unavailable";
            if (!IsAvailable(id)) return "Unavailable / tutorial locked";
            return _finances.CanAfford(d.Cost) ? null : "Insufficient funds";
        }
        public string Validate(string id, BuildingPose pose)
        {
            if (!pose.IsValid) return "Invalid position or rotation.";
            if (!_definitions.TryGetValue(id, out var d)) return "Unknown building definition.";
            if (!IsAvailable(id)) return "Building unavailable or tutorial locked.";
            foreach (var area in Areas(d))
            {
                if (area.Corners(pose).Any(p => !_lot.Contains(p))) return "Outside the owned buildable lot.";
                foreach (var forbidden in _forbidden)
                    if (PlacementRect.Overlaps(area, pose, forbidden, default)) return "Reserved or forbidden zone.";
                foreach (var building in _buildings)
                    if (building.State != BuildingLifecycle.Cancelled)
                        foreach (var other in Areas(building.Definition))
                            if (PlacementRect.Overlaps(area, pose, other, building.Pose)) return "Overlaps a building, entrance or construction reservation.";
            }
            var worldError = _worldValidation?.Invoke(d, pose);
            if (worldError != null) return worldError;
            if (!_entranceReachable(d, pose)) return "Entrance cannot connect to the exterior navigation.";
            if (!_finances.CanAfford(d.Cost)) return "Insufficient funds.";
            return null;
        }
        private static IEnumerable<PlacementRect> Areas(BuildingDefinition d)
        { return d.PlacementAreas; }

        public PlacedBuilding Place(string definitionId, BuildingPose pose, out string error, string instanceId = null)
        {
            error = null;
            if (!_clock.IsBoundaryComplete) { error = "Simulation is processing; try again."; return null; }
            if (_placing) { error = "Placement already in progress."; return null; }
            string id = string.IsNullOrWhiteSpace(instanceId) ? Guid.NewGuid().ToString("N") : instanceId;
            if (_buildings.Any(b => b.Id == id)) { error = "Duplicate instance ID."; return null; }
            error = Validate(definitionId, pose); if (error != null) return null;
            var d = _definitions[definitionId];
            _placing = true;
            try
            {
                if (d.Cost > Money.Zero && !_finances.TryRecordExpense(d.Cost, FinancialTransactionCategory.BuildingConstruction,
                    _clock.CurrentTime, "Construct " + d.DisplayName, id)) { error = "Insufficient funds."; return null; }
                var building = new PlacedBuilding(id, d, pose, d.Id == Tutorial.StarterFeatureIds.Stage ? _nextStageNumber++ : 0);
                _buildings.Add(building); CreatePhaseWork(building); SitePlaced?.Invoke(building); return building;
            }
            finally { _placing = false; }
        }
        public bool HasAssignment(string employeeId) => _assignments.ContainsKey(employeeId);
        public int AssignedCount(string buildingId) => _assignments.Values.Count(a => a.Building.Id == buildingId);
        public int ActiveWorkerCount(string buildingId) => _assignments.Values.Count(a => a.Building.Id == buildingId && a.Arrived);
        public int CurrentWorkerCapacity(PlacedBuilding building) => building.IsOperational || building.State == BuildingLifecycle.Cancelled
            ? 0 : building.Definition.Construction.CapacityFor(building.Phase);
        public ConstructionSiteStatus GetStatus(PlacedBuilding building)
        {
            if (building == null || !_buildings.Contains(building)) throw new ArgumentException("Construction site is not managed by this service.");
            var crew = ProductiveAssignments(building);
            return new ConstructionSiteStatus(building.Definition.RequiredWork, CompletedWork(building), building.Phase,
                crew.Length, CurrentWorkerCapacity(building), TeamProductivity(crew.Select(a => a.Employee)));
        }
        public bool TryGetWorkSpot(string employeeId, out int workSpotIndex)
        {
            if (_assignments.TryGetValue(employeeId, out var assignment)) { workSpotIndex = assignment.WorkSpotIndex; return true; }
            workSpotIndex = -1; return false;
        }
        public bool AssignWorker(PlacedBuilding building, Employee employee) => AssignWorker(building, employee, out _);
        public bool AssignWorker(PlacedBuilding building, Employee employee, out int workSpotIndex)
        {
            workSpotIndex = -1;
            if (!_buildings.Contains(building) || building.State == BuildingLifecycle.Cancelled || building.IsOperational ||
                employee?.IsEmployed != true || employee.Role != EmployeeRole.ConstructionWorker || HasAssignment(employee.Id)) return false;
            int capacity = CurrentWorkerCapacity(building);
            var used = new HashSet<int>(_assignments.Values.Where(a => a.Building == building).Select(a => a.WorkSpotIndex));
            for (int i = 0; i < capacity; i++) if (!used.Contains(i)) { workSpotIndex = i; break; }
            if (workSpotIndex < 0) return false;
            if (!_resources.TryAcquire(building.Id, new[] { new ResourceKey("person", employee.Id), WorkSpot(building, workSpotIndex) }, out var claim, out _))
            { workSpotIndex = -1; return false; }
            _assignments.Add(employee.Id, new Assignment { Employee = employee, Building = building, ReservationId = claim.Id, WorkSpotIndex = workSpotIndex });
            return true;
        }
        public bool WorkerArrived(string employeeId)
        {
            if (!_assignments.TryGetValue(employeeId, out var a) || a.Arrived) return false;
            if (!a.Employee.IsEmployed || a.Employee.Role != EmployeeRole.ConstructionWorker) { ReleaseWorker(employeeId); return false; }
            AccrueCurrentWork(a.Building);
            a.Arrived = true; a.Building.State = BuildingLifecycle.UnderConstruction;
            UpdateCapacity(a.Building); Changed?.Invoke(a.Building); return true;
        }
        public void ReleaseWorker(string employeeId)
        {
            if (!_assignments.TryGetValue(employeeId, out var a)) return;
            AccrueCurrentWork(a.Building);
            _assignments.Remove(employeeId); _resources.Release(a.ReservationId);
            if (!a.Building.IsOperational && a.Building.State != BuildingLifecycle.Cancelled) UpdateCapacity(a.Building);
        }
        private void UpdateCapacity(PlacedBuilding building)
        {
            AccrueCurrentWork(building);
            var arrived = ProductiveAssignments(building);
            _work.SetAssignments(building.WorkId, arrived.Select(a => new WorkAssignment(new ResourceKey("person", a.Employee.Id), "maintenance.construction")));
            decimal rate = TeamProductivity(arrived.Select(a => a.Employee));
            _work.RefreshCapacity(building.WorkId, new WorkRate(WorkQuantity.FromUnits(WorkMeasure, rate), new SimulationDuration(3600)));
        }
        public static decimal ConstructionProficiencyMultiplier(int proficiency) => .65m + Math.Min(100, Math.Max(0, proficiency)) * .007m;
        public static decimal TeamProductivity(IEnumerable<Employee> workers)
        {
            if (workers == null) return 0;
            var ordered = workers.Where(e => e != null && e.IsEmployed && e.Role == EmployeeRole.ConstructionWorker)
                .OrderByDescending(e => e.GetProfessionSkill(EmployeeRole.ConstructionWorker)).ThenBy(e => e.Id, StringComparer.Ordinal).ToArray();
            decimal productivity = 0;
            for (int i = 0; i < ordered.Length; i++)
                productivity += ConstructionProficiencyMultiplier(ordered[i].GetProfessionSkill(EmployeeRole.ConstructionWorker)) * (decimal)Math.Pow(i + 1, -.45d);
            return productivity;
        }
        private Assignment[] ProductiveAssignments(PlacedBuilding building) => _assignments.Values
            .Where(a => a.Building == building && a.Arrived && a.Employee.IsEmployed && a.Employee.Role == EmployeeRole.ConstructionWorker)
            .OrderByDescending(a => a.Employee.GetProfessionSkill(EmployeeRole.ConstructionWorker)).ThenBy(a => a.Employee.Id, StringComparer.Ordinal)
            .Take(CurrentWorkerCapacity(building)).ToArray();
        private static ResourceKey WorkSpot(PlacedBuilding building, int slot) =>
            new ResourceKey("construction-spot", building.Id + ":" + building.PhaseIndex + ":" + slot);
        private void AccrueCurrentWork(PlacedBuilding building)
        {
            if (building == null || building.IsOperational || building.State == BuildingLifecycle.Cancelled) return;
            AccrueExperience(building, _work.GetSnapshot(building.WorkId).Completed.Units);
        }
        private void AccrueExperience(PlacedBuilding building, decimal completedInPhase)
        {
            decimal delta = completedInPhase - building.AccountedPhaseWork;
            if (delta <= 0) return;
            var crew = ProductiveAssignments(building);
            decimal productivity = TeamProductivity(crew.Select(a => a.Employee));
            if (productivity <= 0) { building.AccountedPhaseWork = completedInPhase; return; }
            for (int i = 0; i < crew.Length; i++)
            {
                decimal weighted = ConstructionProficiencyMultiplier(crew[i].Employee.GetProfessionSkill(EmployeeRole.ConstructionWorker)) *
                    (decimal)Math.Pow(i + 1, -.45d);
                crew[i].Employee.AddProfessionExperience(EmployeeRole.ConstructionWorker, delta * weighted / productivity,
                    ConstructionExperiencePerSkillPoint);
            }
            building.AccountedPhaseWork = completedInPhase;
        }
        private void CreatePhaseWork(PlacedBuilding b)
        {
            b.Phase = b.Definition.Phases[b.PhaseIndex];
            b.AccountedPhaseWork = 0;
            _work.Create(new WorkDefinition(b.WorkId, b.Id, "maintenance.construction",
                WorkQuantity.FromUnits(WorkMeasure, b.Definition.Construction.WorkFor(b.Phase))));
        }
        private void WorkChanged(WorkSnapshot snapshot)
        {
            var b = _buildings.Find(item => item.WorkId == snapshot.Definition.Id);
            if (b == null || snapshot.Status != WorkStatus.Completed || b.IsOperational || b.State == BuildingLifecycle.Cancelled) return;
            AccrueExperience(b, snapshot.Completed.Units);
            b.FinishedWork += snapshot.Completed.Units;
            b.PhaseIndex++;
            if (b.PhaseIndex < b.Definition.Phases.Count)
            { CreatePhaseWork(b); ReassignPhaseSpots(b); UpdateCapacity(b); Changed?.Invoke(b); return; }
            b.Phase = ConstructionPhase.Complete; b.State = BuildingLifecycle.Operational;
            foreach (var id in _assignments.Where(p => p.Value.Building == b).Select(p => p.Key).ToArray()) ReleaseWorker(id);
            Operational?.Invoke(b); Established?.Invoke(b.Definition.Id); Changed?.Invoke(b);
        }
        public decimal Progress(PlacedBuilding b)
        {
            if (b.IsOperational) return 1;
            if (!_clock.IsBoundaryComplete) return b.FinishedWork / b.Definition.RequiredWork;
            decimal earned = b.FinishedWork + _work.GetSnapshot(b.WorkId).Completed.Units;
            return Math.Min(1, earned / b.Definition.RequiredWork);
        }
        private decimal CompletedWork(PlacedBuilding building) => building.IsOperational ? building.Definition.RequiredWork :
            Math.Min(building.Definition.RequiredWork, building.FinishedWork + (_clock.IsBoundaryComplete ? _work.GetSnapshot(building.WorkId).Completed.Units : 0));
        private void ReassignPhaseSpots(PlacedBuilding building)
        {
            int capacity = CurrentWorkerCapacity(building);
            foreach (var pair in _assignments.Where(p => p.Value.Building == building).OrderBy(p => p.Value.WorkSpotIndex).ToArray())
            {
                var assignment = pair.Value;
                if (assignment.WorkSpotIndex >= capacity) { ReleaseWorker(pair.Key); continue; }
                if (_resources.TryReplace(assignment.ReservationId, building.Id,
                    new[] { new ResourceKey("person", assignment.Employee.Id), WorkSpot(building, assignment.WorkSpotIndex) }, out var claim, out _))
                    assignment.ReservationId = claim.Id;
                else ReleaseWorker(pair.Key);
            }
        }
        public bool Cancel(PlacedBuilding b)
        {
            if (!_clock.IsBoundaryComplete) return false;
            if (!_buildings.Contains(b) || b.IsOperational || b.State == BuildingLifecycle.Cancelled) return false;
            b.State = BuildingLifecycle.Cancelled; _work.Cancel(b.WorkId);
            foreach (var id in _assignments.Where(p => p.Value.Building == b).Select(p => p.Key).ToArray()) ReleaseWorker(id);
            // Committed materials are non-refundable in this foundation. Preview cancellation never charges.
            Cancelled?.Invoke(b); Changed?.Invoke(b); return true;
        }
        public PlacedBuildingSnapshot Capture(PlacedBuilding b) => new PlacedBuildingSnapshot
        {
            InstanceId = b.Id, DefinitionId = b.Definition.Id, Pose = b.Pose, State = b.State, Phase = b.Phase,
            CompletedWorkMicroUnits = WorkQuantity.FromUnits(WorkMeasure, Progress(b) * b.Definition.RequiredWork).MicroUnits, StageNumber = b.StageNumber
        };
        public MaintenanceAssignmentSnapshot[] CaptureAssignments() => _assignments.Values.Select(a => new MaintenanceAssignmentSnapshot
        { EmployeeId = a.Employee.Id, BuildingId = a.Building.Id, Kind = MaintenanceTaskKind.Construction, Arrived = a.Arrived }).ToArray();
        public void Dispose()
        {
            _work.Changed -= WorkChanged;
            if (!_work.IsDisposed && _clock.IsBoundaryComplete)
                foreach (var b in _buildings.Where(b => !b.IsOperational && b.State != BuildingLifecycle.Cancelled)) _work.Cancel(b.WorkId);
            foreach (var a in _assignments.Values) _resources.Release(a.ReservationId);
            _assignments.Clear();
        }
    }
}
