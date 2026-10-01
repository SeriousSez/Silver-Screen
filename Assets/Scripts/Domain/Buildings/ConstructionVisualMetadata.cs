using System;
using System.Collections.Generic;
using System.Linq;

namespace SilverScreen.Domain.Buildings
{
    public enum ScaffoldSide { Front, Right, Rear, Left }
    public enum ConstructionActivityKind { GroundWork, FoundationWork, ScaffoldWork, WallWork, MaterialPickup, MaterialDrop, Inspection, Cleanup }

    /// <summary>Local ground centreline of a scaffold run; arbitrary segments need not form a rectangle.</summary>
    public sealed class ScaffoldSegment
    {
        public string Id { get; }
        public LotPoint Start { get; }
        public LotPoint End { get; }
        public float Height { get; }
        public ScaffoldSegment(string id, LotPoint start, LotPoint end, float height)
        {
            if (string.IsNullOrWhiteSpace(id) || height <= 0 || height > 100) throw new ArgumentException("Valid scaffold segment required.");
            Id = id; Start = start; End = end; Height = height;
        }
    }
    public sealed class ConstructionVisualMetadata
    {
        public float BuildingHeight { get; }
        public IReadOnlyList<ScaffoldSegment> Segments { get; }
        public IReadOnlyList<PlacementRect> Exclusions { get; }
        public IReadOnlyList<PlacementRect> MaterialZones { get; }
        public ConstructionVisualMetadata(float buildingHeight, IEnumerable<ScaffoldSegment> segments,
            IEnumerable<PlacementRect> exclusions = null, IEnumerable<PlacementRect> materialZones = null)
        {
            if (!(buildingHeight > 0) || buildingHeight > 100) throw new ArgumentException("Finite building height required.");
            BuildingHeight = buildingHeight;
            Segments = Array.AsReadOnly((segments ?? Array.Empty<ScaffoldSegment>()).ToArray());
            Exclusions = Array.AsReadOnly((exclusions ?? Array.Empty<PlacementRect>()).ToArray());
            MaterialZones = Array.AsReadOnly((materialZones ?? Array.Empty<PlacementRect>()).ToArray());
        }
        // Content authoring convenience, not a generator assumption about every building type.
        public static ConstructionVisualMetadata Rectangle(PlacementRect footprint, float height, params ScaffoldSide[] sides)
        {
            float l = footprint.X - footprint.Width / 2, r = footprint.X + footprint.Width / 2;
            float f = footprint.Z - footprint.Depth / 2, b = footprint.Z + footprint.Depth / 2;
            var segments = new List<ScaffoldSegment>();
            foreach (var side in sides.Distinct())
            {
                LotPoint start, end;
                switch (side)
                {
                    case ScaffoldSide.Front: start = new LotPoint(l+.3f,f-.5f); end = new LotPoint(r-.3f,f-.5f); break;
                    case ScaffoldSide.Right: start = new LotPoint(r+.5f,f+.3f); end = new LotPoint(r+.5f,b-.3f); break;
                    case ScaffoldSide.Rear: start = new LotPoint(r-.3f,b+.5f); end = new LotPoint(l+.3f,b+.5f); break;
                    default: start = new LotPoint(l-.5f,b-.3f); end = new LotPoint(l-.5f,f+.3f); break;
                }
                segments.Add(new ScaffoldSegment(side.ToString(), start, end, height));
            }
            return new ConstructionVisualMetadata(height, segments, materialZones: new[]
            {
                new PlacementRect(l-.55f,b-1.1f,.65f,1.25f),
                new PlacementRect(r+.55f,b-1.1f,.65f,1.25f)
            });
        }
    }
}
