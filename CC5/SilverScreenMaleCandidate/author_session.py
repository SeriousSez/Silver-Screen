"""Temporary, bounded CC5-native authoring session. Load through CC5 Script menu.

No arbitrary code execution, network, mesh replacement, export or existing-project
load. Requests affect the single candidate present when the session is started.
The timer is explicitly stopped when the review package is complete.
"""
import json
import math
import time
import traceback
from pathlib import Path
import RLPy
from PySide2.QtCore import QTimer

ROOT = Path(r"C:\Repos\Unity\Silver Screen\CC5\SilverScreenMaleCandidate")
TEMPLATES = Path(r"C:\Users\Public\Documents\Reallusion\Reallusion Templates")
timer = None
last_request = None
candidate_id = None
started = 0

def vector(v):
    return [v.x, v.y, v.z]

def status(s):
    return None if s is None else str(s)

def bounds(avatar):
    high, center, low = RLPy.RVector3(), RLPy.RVector3(), RLPy.RVector3()
    avatar.GetBounds(high, center, low)
    return dict(min=vector(low), max=vector(high), height_cm=high.z-low.z)

def tick():
    global last_request
    if time.monotonic() - started > 7200:
        timer.stop()
        return
    request_path = ROOT / "session-request.json"
    if not request_path.exists():
        return
    try:
        request = json.loads(request_path.read_text(encoding="utf-8-sig"))
    except (ValueError, OSError):
        return
    if request.get("id") == last_request:
        return
    last_request = request.get("id")
    result = {"id": last_request, "operations": []}
    try:
        avatars = RLPy.RScene.GetAvatars()
        assert len(avatars) == 1 and avatars[0].GetID() == candidate_id, "Candidate scene changed; refusing request"
        avatar = avatars[0]
        shaping = avatar.GetAvatarShapingComponent()
        for op in request.get("operations", []):
            kind = op["type"]
            entry = dict(op)
            if kind == "rename":
                entry["status"] = status(avatar.SetName("SS_Male_CC5_Candidate_v01"))
            elif kind == "morph":
                key, value = op["morph_id"], float(op["value"])
                assert key in shaping.GetShapingMorphIDs("Actor"), "Unknown native actor morph"
                limits = shaping.GetShapingMorphMinMax(key)
                assert limits.first <= value <= limits.second, "Outside native morph range"
                entry["before"] = shaping.GetShapingMorphWeight(key)
                entry["status"] = status(shaping.SetShapingMorphWeight(key, value))
                RLPy.RGlobal.ObjectModified(avatar, RLPy.EObjectModifiedType_MorphWeight)
                entry["after"] = shaping.GetShapingMorphWeight(key)
            elif kind == "camera":
                camera = RLPy.RScene.GetCurrentCamera()
                angle = math.radians(float(op.get("angle", 0)))
                z = float(op["target_z"])
                distance = float(op["distance"])
                q = RLPy.RQuaternion(RLPy.RVector3(0, 0, 1), angle) * RLPy.RQuaternion(RLPy.RVector3(1, 0, 0), math.pi/2)
                t = RLPy.RTransform(RLPy.RVector3(1,1,1), q,
                    RLPy.RVector3(math.sin(angle)*distance, -math.cos(angle)*distance, z))
                entry["status"] = status(camera.GetControl("Transform").SetValue(RLPy.RTime(0), t))
                camera.SetFocalLength(RLPy.RTime(0), float(op.get("focal_length", 85)))
                camera.Update()
            elif kind == "load_template":
                path = (TEMPLATES / op["relative_path"]).resolve()
                assert TEMPLATES.resolve() in path.parents and path.suffix.lower() in (".ccskin", ".cclightroom", ".iatm", ".cceye", ".rlhair", ".rlpose", ".imotion", ".imtlplus"), "Unsupported template"
                assert path.is_file()
                RLPy.RScene.SelectObject(avatar)
                entry["status"] = status(RLPy.RFileIO.LoadFile(str(path)))
            elif kind == "save":
                path = (ROOT / op["filename"]).resolve()
                assert path.parent == ROOT and path.suffix.lower() == ".ccproject"
                assert not path.exists(), "Refusing to overwrite an existing checkpoint"
                entry["status"] = status(RLPy.RFileIO.SaveProject(str(path)))
            elif kind == "stop":
                timer.stop()
            elif kind == "refresh_shape":
                key = "2018-09-14-22-07-28_character scale"
                weight = shaping.GetShapingMorphWeight(key)
                shaping.SetShapingMorphWeight(key, weight + 0.001)
                shaping.SetShapingMorphWeight(key, weight)
                RLPy.RGlobal.ObjectModified(avatar, RLPy.EObjectModifiedType_MorphWeight)
                avatar.Update()
            elif kind == "inspect_meshes":
                mc = avatar.GetMaterialComponent()
                entry["meshes"] = []
                for mesh in avatar.GetMeshes():
                    entry["meshes"].append({"name": mesh.GetName(), "faces": mesh.GetFacesCount()})
                entry["opacity"] = {name: {mat: mc.GetOpacity(name, mat) for mat in mc.GetMaterialNames(name)} for name in avatar.GetMeshNames()}
            else:
                raise ValueError("Unsupported operation " + kind)
            result["operations"].append(entry)
        result["bounds"] = bounds(avatar)
        result["name"] = avatar.GetName()
        result["clothes"] = [o.GetName() for o in avatar.GetClothes()]
        result["hairs"] = [o.GetName() for o in avatar.GetHairs()]
        result["generation"] = int(avatar.GetGeneration())
    except Exception:
        result["error"] = traceback.format_exc()
    (ROOT / "session-result.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
    with (ROOT / "session-log.jsonl").open("a", encoding="utf-8") as f:
        f.write(json.dumps(result) + "\n")

def run_script():
    global timer, candidate_id, started
    avatars = RLPy.RScene.GetAvatars()
    assert len(avatars) == 1 and avatars[0].GetName() in ("CC3_Base_Plus", "SS_Male_CC5_Candidate_v01")
    candidate_id = avatars[0].GetID()
    started = time.monotonic()
    timer = QTimer()
    timer.timeout.connect(tick)
    timer.start(1000)
    (ROOT / "session-ready.txt").write_text("Active for the current candidate only.", encoding="utf-8")

