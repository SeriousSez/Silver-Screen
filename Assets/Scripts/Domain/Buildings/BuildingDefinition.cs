using System;
using System.Collections.Generic;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Tutorial;

namespace SilverScreen.Domain.Buildings
{
    [Serializable]
    public struct LotPoint
    {
        public float X, Z;
        public LotPoint(float x, float z) { X = x; Z = z; }
    }
    [Serializable]
    public struct BuildingPose
    {
        public float X, Z, Yaw;
        public BuildingPose(float x, float z, float yaw) { X = x; Z = z; Yaw = yaw; }
        public bool IsValid => Finite(X) && Finite(Z) && Finite(Yaw);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        public LotPoint Transform(LotPoint local)
        {
            double angle = Yaw * Math.PI / 180;
            return new LotPoint(X + (float)(local.X * Math.Cos(angle) + local.Z * Math.Sin(angle)),
                Z + (float)(-local.X * Math.Sin(angle) + local.Z * Math.Cos(angle)));
        }
    }
    [Serializable]
    public struct PlacementRect
    {
        public float X, Z, Width, Depth;
        public PlacementRect(float x, float z, float width, float depth)
        {
            if (!(width > 0) || !(depth > 0)) throw new ArgumentException("Positive rectangle dimensions required.");
            X = x; Z = z; Width = width; Depth = depth;
        }
        public LotPoint[] Corners(BuildingPose pose) => new[]
        {
            pose.Transform(new LotPoint(X - Width / 2, Z - Depth / 2)), pose.Transform(new LotPoint(X + Width / 2, Z - Depth / 2)),
            pose.Transform(new LotPoint(X + Width / 2, Z + Depth / 2)), pose.Transform(new LotPoint(X - Width / 2, Z + Depth / 2))
        };
        public bool Contains(LotPoint p) => p.X >= X - Width / 2 && p.X <= X + Width / 2 && p.Z >= Z - Depth / 2 && p.Z <= Z + Depth / 2;
        public static bool Overlaps(PlacementRect a, BuildingPose ap, PlacementRect b, BuildingPose bp)
        {
            var ac = a.Corners(ap); var bc = b.Corners(bp);
            foreach (var corners in new[] { ac, bc })
                for (int i = 0; i < 2; i++)
                {
                    float dx = corners[i + 1].X - corners[i].X, dz = corners[i + 1].Z - corners[i].Z;
                    float amin = float.MaxValue, amax = float.MinValue, bmin = float.MaxValue, bmax = float.MinValue;
                    foreach (var p in ac) { float v = p.X * -dz + p.Z * dx; amin = Math.Min(amin, v); amax = Math.Max(amax, v); }
                    foreach (var p in bc) { float v = p.X * -dz + p.Z * dx; bmin = Math.Min(bmin, v); bmax = Math.Max(bmax, v); }
                    if (amax <= bmin || bmax <= amin) return false;
                }
            return true;
        }
    }

    public enum BuildingLifecycle { Planned, UnderConstruction, Operational, Cancelled }
    public enum ConstructionPhase { SitePreparation, Foundation, Structure, Exterior, Finishing, Complete }
    public enum MaintenanceTaskKind { Construction, Repair, Renovation }

    public sealed class ConstructionPhaseRequirement
    {
        public ConstructionPhase Phase { get; }
        public decimal WorkShare { get; }
        public int UsefulWorkerCapacity { get; }
        public ConstructionPhaseRequirement(ConstructionPhase phase, decimal workShare, int usefulWorkerCapacity)
        {
            if (phase == ConstructionPhase.Complete || workShare <= 0 || usefulWorkerCapacity < 1)
                throw new ArgumentException("An active phase requires positive work and capacity.");
            Phase = phase; WorkShare = workShare; UsefulWorkerCapacity = usefulWorkerCapacity;
        }
    }

    /// <summary>Authored labor requirements, independent of footprint size and presentation geometry.</summary>
    public sealed class ConstructionRequirements
    {
        private readonly Dictionary<ConstructionPhase, ConstructionPhaseRequirement> _byPhase =
            new Dictionary<ConstructionPhase, ConstructionPhaseRequirement>();
        public decimal TotalWork { get; }
        public int RecommendedWorkers { get; }
        public int MaximumUsefulWorkers { get; }
        public IReadOnlyList<ConstructionPhaseRequirement> Phases { get; }

        public ConstructionRequirements(decimal totalWork, int recommendedWorkers, int maximumUsefulWorkers,
            params ConstructionPhaseRequirement[] phases)
        {
            if (totalWork <= 0 || recommendedWorkers < 1 || maximumUsefulWorkers < recommendedWorkers || phases == null || phases.Length == 0)
                throw new ArgumentException("Construction requires positive work, a useful crew and authored phases.");
            decimal totalShare = 0;
            foreach (var phase in phases)
            {
                if (phase == null || phase.UsefulWorkerCapacity > maximumUsefulWorkers || _byPhase.ContainsKey(phase.Phase))
                    throw new ArgumentException("Construction phases must be unique and remain within site capacity.");
                _byPhase.Add(phase.Phase, phase); totalShare += phase.WorkShare;
            }
            if (Math.Abs(totalShare - 1m) > .0001m) throw new ArgumentException("Construction phase work shares must total one.");
            TotalWork = totalWork; RecommendedWorkers = recommendedWorkers; MaximumUsefulWorkers = maximumUsefulWorkers;
            Phases = Array.AsReadOnly((ConstructionPhaseRequirement[])phases.Clone());
        }

        public ConstructionPhaseRequirement ForPhase(ConstructionPhase phase) =>
            _byPhase.TryGetValue(phase, out var requirement) ? requirement : throw new ArgumentOutOfRangeException(nameof(phase));
        public decimal WorkFor(ConstructionPhase phase) => TotalWork * ForPhase(phase).WorkShare;
        public int CapacityFor(ConstructionPhase phase) => ForPhase(phase).UsefulWorkerCapacity;
    }

    public interface IOperationalBuildings
    {
        bool IsEstablished(string definitionId);
        event Action<string> Established;
    }

    /// <summary>Immutable architectural/content data. All coordinates are local, never a lot position.</summary>
    public sealed class BuildingDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string ContentId { get; }
        public PlacementRect Footprint { get; }
        public PlacementRect ConstructionClearance { get; }
        public PlacementRect EntranceClearance { get; }
        public PlacementRect DecorativeOverhang { get; }
        public IReadOnlyList<PlacementRect> PlacementAreas { get; }
        public bool HasCompoundFootprint { get; }
        public LotPoint Entrance { get; }
        public LotPoint ExteriorApproach { get; }
        public LotPoint? ServiceEntrance { get; }
        public string InteractionAnchorId => "entrance.primary";
        public string NavigationPolicyId => "continuous-lot-physics";
        public Money Cost { get; }
        public ConstructionRequirements Construction { get; }
        public decimal RequiredWork => Construction.TotalWork;
        public int RecommendedWorkers => Construction.RecommendedWorkers;
        public int WorkerCapacity => Construction.MaximumUsefulWorkers;
        public int AvailableFromYear { get; }
        public string UpgradeFamilyId { get; }
        public ConstructionVisualMetadata ConstructionVisuals { get; }
        public IReadOnlyList<string> Capabilities { get; }
        public IReadOnlyList<ProfessionalRole> Applicants { get; }
        public IReadOnlyList<ConstructionPhase> Phases { get; }
        public BuildingDefinition(string id, string name, string contentId, PlacementRect footprint,
            PlacementRect constructionClearance, PlacementRect entranceClearance, LotPoint entrance, LotPoint approach,
            Money cost, ConstructionRequirements construction, string[] capabilities, ProfessionalRole[] applicants,
            int availableFromYear = 1930, string upgradeFamilyId = null,
            LotPoint? serviceEntrance = null, PlacementRect? decorativeOverhang = null,
            ConstructionVisualMetadata constructionVisuals = null, PlacementRect[] placementAreas = null)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(contentId) || construction == null || cost < Money.Zero)
                throw new ArgumentException("Valid building identity, cost and work required.");
            Id = id; DisplayName = name; ContentId = contentId; Footprint = footprint;
            ConstructionClearance = constructionClearance; EntranceClearance = entranceClearance;
            Entrance = entrance; ExteriorApproach = approach; Cost = cost; Construction = construction;
            Capabilities = Array.AsReadOnly((string[])(capabilities ?? Array.Empty<string>()).Clone());
            Applicants = Array.AsReadOnly((ProfessionalRole[])(applicants ?? Array.Empty<ProfessionalRole>()).Clone());
            var phases = new ConstructionPhase[construction.Phases.Count];
            for (int i = 0; i < phases.Length; i++) phases[i] = construction.Phases[i].Phase;
            Phases = Array.AsReadOnly(phases); AvailableFromYear = availableFromYear; UpgradeFamilyId = upgradeFamilyId;
            ServiceEntrance = serviceEntrance; DecorativeOverhang = decorativeOverhang ?? footprint;
            ConstructionVisuals = constructionVisuals ?? new ConstructionVisualMetadata(3, Array.Empty<ScaffoldSegment>());
            PlacementAreas = Array.AsReadOnly(placementAreas == null ? new[] { footprint, constructionClearance, entranceClearance } : (PlacementRect[])placementAreas.Clone());
            HasCompoundFootprint = placementAreas != null;
        }
    }

    public static class StudioBuildingDefinitions
    {
        public const string Service = "building.studio-service";
        public const string StageSchool = "building.stage-school";
        // Authored structural dimensions checked against the existing content, not runtime renderer bounds.
        public static IReadOnlyList<BuildingDefinition> Create() => Array.AsReadOnly(new[]
        {
            new BuildingDefinition(StarterFeatureIds.Headquarters, "Studio Headquarters", "content.headquarters.1930",
                new PlacementRect(0,0,9.4f,6.9f), new PlacementRect(0,0,11.4f,8.9f), new PlacementRect(0,-7,7,6),
                new LotPoint(0,-4.45f), new LotPoint(0,-6.5f), Money.FromDollars(18000),
                Requirements(30,3,5, (.12m,4),(.20m,4),(.30m,5),(.20m,3),(.18m,4)),
                new[]{"studio.management"}, Array.Empty<ProfessionalRole>(), constructionVisuals:
                ConstructionVisualMetadata.Rectangle(new PlacementRect(0,0,9.4f,6.9f),8.82f,ScaffoldSide.Left,ScaffoldSide.Right,ScaffoldSide.Rear)),
            new BuildingDefinition(StarterFeatureIds.Casting, "Casting Office", "content.casting-office.1930",
                new PlacementRect(0,0,8.3f,6.2f), new PlacementRect(0,0,10.3f,8.2f), new PlacementRect(0,-6.7f,7,6),
                new LotPoint(0,-4.2f), new LotPoint(0,-6), Money.FromDollars(12000),
                Requirements(16,2,3, (.18m,3),(.22m,3),(.25m,3),(.20m,2),(.15m,2)),
                new[]{"recruitment.casting"}, new[]{ProfessionalRole.Actor,ProfessionalRole.Director,ProfessionalRole.Extra}, constructionVisuals:
                ConstructionVisualMetadata.Rectangle(new PlacementRect(0,0,8.3f,6.2f),4.92f,ScaffoldSide.Left,ScaffoldSide.Right,ScaffoldSide.Front)),
            new BuildingDefinition(StarterFeatureIds.Stage, "Sound Stage", "content.stage1-live.1930",
                new PlacementRect(0,0,16.2f,25.8f), new PlacementRect(0,0,18.2f,27.8f), new PlacementRect(5.7f,-14,4,4),
                new LotPoint(5.7f,-10.7f), new LotPoint(5.7f,-15.2f), Money.FromDollars(50000),
                Requirements(72,5,8, (.12m,6),(.18m,7),(.32m,8),(.23m,5),(.15m,6)),
                new[]{SetDefinitionIds.GenericInterior,SetDefinitionIds.Office,SetDefinitionIds.LivingRoom,SetDefinitionIds.Bedroom},
                Array.Empty<ProfessionalRole>(), serviceEntrance:new LotPoint(0,12.5f), constructionVisuals:
                ConstructionVisualMetadata.Rectangle(new PlacementRect(0,0,16.2f,25.8f),11.43f,ScaffoldSide.Left,ScaffoldSide.Right,ScaffoldSide.Rear,ScaffoldSide.Front)),
            new BuildingDefinition(StageSchool, "Stage School", "content.stage-school.1930",
                new PlacementRect(0,-1,22,16), new PlacementRect(0,-1.875f,24,20.55f), new PlacementRect(0,-10.7f,7.6f,2.9f),
                new LotPoint(0,-9), new LotPoint(0,-12.8f), Money.FromDollars(42000),
                Requirements(32,3,5, (.12m,3),(.22m,4),(.26m,5),(.20m,3),(.20m,4)),
                new[]{"facility.stage-school"}, Array.Empty<ProfessionalRole>(),
                constructionVisuals:ConstructionVisualMetadata.Rectangle(new PlacementRect(0,-1,22,16),5.95f,ScaffoldSide.Left,ScaffoldSide.Right,ScaffoldSide.Rear),
                // Body maintenance margin, entrance clearance and two compact waiting pockets.
                placementAreas:new[]{new PlacementRect(0,-.45f,24,17.7f),new PlacementRect(0,-10.7f,7.6f,2.9f),new PlacementRect(-7.2f,-10.2f,6.75f,2.9f),new PlacementRect(7.2f,-10.2f,6.75f,2.9f)})
        });

        private static ConstructionRequirements Requirements(decimal work, int recommended, int maximum,
            params (decimal share, int capacity)[] phases)
        {
            var order = new[] { ConstructionPhase.SitePreparation, ConstructionPhase.Foundation,
                ConstructionPhase.Structure, ConstructionPhase.Exterior, ConstructionPhase.Finishing };
            if (phases == null || phases.Length != order.Length) throw new ArgumentException("Every construction phase requires authored data.");
            var requirements = new ConstructionPhaseRequirement[order.Length];
            for (int i = 0; i < order.Length; i++) requirements[i] = new ConstructionPhaseRequirement(order[i], phases[i].share, phases[i].capacity);
            return new ConstructionRequirements(work, recommended, maximum, requirements);
        }
    }

    [Serializable]
    public sealed class PlacedBuildingSnapshot
    {
        public int Version = 1;
        public string InstanceId, DefinitionId;
        public BuildingPose Pose;
        public BuildingLifecycle State;
        public ConstructionPhase Phase;
        public long CompletedWorkMicroUnits;
        public int StageNumber;
    }
    public sealed class PlacedBuilding
    {
        public string Id { get; }
        public BuildingDefinition Definition { get; }
        public BuildingPose Pose { get; }
        public int StageNumber { get; }
        public BuildingLifecycle State { get; internal set; } = BuildingLifecycle.Planned;
        public ConstructionPhase Phase { get; internal set; } = ConstructionPhase.SitePreparation;
        internal int PhaseIndex;
        internal decimal FinishedWork;
        internal decimal AccountedPhaseWork;
        internal string WorkId => Id + ":construction:" + PhaseIndex;
        public bool IsOperational => State == BuildingLifecycle.Operational;
        internal PlacedBuilding(string id, BuildingDefinition definition, BuildingPose pose, int stageNumber)
        { Id = id; Definition = definition; Pose = pose; StageNumber = stageNumber; }
    }
}
