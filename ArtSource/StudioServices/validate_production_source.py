"""Blender read-only construction checks on the generated production source."""
import bpy
import json
from pathlib import Path

root = Path(__file__).resolve().parents[2]
bpy.ops.wm.open_mainfile(filepath=str(root / "ArtSource/StudioServices/StudioServices_Production.blend"))
panels = [o for o in bpy.data.objects if o.name.startswith("Front roofing panel")]
assert panels
for panel in panels:
    vertices = [panel.matrix_world @ v.co for v in panel.data.vertices]
    delta = [v.z - (4.18 + (v.y + 4.1) * .97 / 4.3) for v in vertices]
    assert max(delta) < .00001, (panel.name, "Thickness projects above approved plane", max(delta))
    assert min(delta) < -.07, (panel.name, "Missing roof thickness")
    assert min(delta) > -.08, (panel.name, "Unexpected roof thickness")

report = {"frontRoofPanels": len(panels), "allThicknessBelowApprovedPlane": True,
          "rooflightPlanes": "Both curbs fit the front roof plane (generator assertions)",
          "sourceFile": "ArtSource/StudioServices/StudioServices_Production.blend"}
path = root / "ArtReview/StudioServices/Fidelity/construction-validation.json"
path.write_text(json.dumps(report, indent=2), encoding="utf-8")
print("CONSTRUCTION_VALIDATION", report)
