"""Blender regression check for open drainage bores and closed rolled rims.

Run with --background --python. Does not write the source library or exports.
"""
from pathlib import Path
import json
import math
import sys

sys.path.insert(0, str(Path(__file__).resolve().parent))
import geometry as G
from mathutils import Vector

G.init_materials()
G.group("SweepValidation")
points = [(0, 0, .36), (0, 0, .17), (0, -.04, .10),
          (0, -.18, .07), (0, -.29, .07)]
shoe = G.tube("ShoeProof", points, .049, wall=.003)
for k, point in enumerate(points):
    offsets = [shoe.data.vertices[k * 32 + j].co - Vector(point) for j in range(32)]
    assert all(abs(v.length - .049) < 1e-6 for v in offsets[:16])
    assert all(abs(v.length - .046) < 1e-6 for v in offsets[16:])
    if k:
        previous = shoe.data.vertices[(k - 1) * 32].co - Vector(points[k - 1])
        assert previous.normalized().dot(offsets[0].normalized()) > .95, "Pinched bore/frame flip"

circle = [(0, math.cos(i * math.tau / 32) * .2,
           math.sin(i * math.tau / 32) * .2) for i in range(33)]
rim = G.tube("ClosedRimProof", circle, .014)
vertices_per_ring = len(rim.data.vertices) // len(circle)
assert vertices_per_ring * len(circle) == len(rim.data.vertices)
for i in range(vertices_per_ring):
    assert (rim.data.vertices[i].co - rim.data.vertices[-vertices_per_ring + i].co).length < 1e-6, "Rolled rim seam"

report = {"shoeOuterRadius": .049, "shoeBoreRadius": .046,
          "crossSectionRings": len(points), "noCrossSectionFlip": True,
          "closedRimEndRingsCoincide": True}
output = Path(__file__).resolve().parents[2] / "ArtReview/StudioServices/Fidelity/sweep-validation.json"
output.write_text(json.dumps(report, indent=2), encoding="utf-8")
print("SWEEP_VALIDATION", report)
