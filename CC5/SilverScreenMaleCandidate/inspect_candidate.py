"""Run inside CC5 via Script > Load Python; reads native candidate controls."""
import json
import traceback
from pathlib import Path
import RLPy

ROOT = Path(r"C:\Repos\Unity\Silver Screen\CC5\SilverScreenMaleCandidate")

def vec(v):
    return [v.x, v.y, v.z]

def run_script():
    result = {}
    try:
        avatars = RLPy.RScene.GetAvatars()
        result["avatar_count"] = len(avatars)
        result["avatars"] = []
        for avatar in avatars:
            high, center, low = RLPy.RVector3(), RLPy.RVector3(), RLPy.RVector3()
            avatar.GetBounds(high, center, low)
            shaping = avatar.GetAvatarShapingComponent()
            categories = list(shaping.GetShapingMorphCatergoryNames())
            morphs = []
            for category in categories:
                ids = shaping.GetShapingMorphIDs(category)
                names = shaping.GetShapingMorphDisplayNames(category)
                for key, name in zip(ids, names):
                    limits = shaping.GetShapingMorphMinMax(key)
                    morphs.append(dict(category=category, id=key, name=name,
                                       weight=shaping.GetShapingMorphWeight(key),
                                       minimum=limits.first, maximum=limits.second))
            result["avatars"].append(dict(name=avatar.GetName(), generation=int(avatar.GetGeneration()),
                avatar_type=int(avatar.GetAvatarType()), bounds=dict(min=vec(low), max=vec(high), center=vec(center)),
                categories=categories, morphs=morphs, meshes=list(avatar.GetMeshNames()),
                clothes=[o.GetName() for o in avatar.GetClothes()], hairs=[o.GetName() for o in avatar.GetHairs()]))
        camera = RLPy.RScene.GetCurrentCamera()
        transform = camera.WorldTransform()
        result["camera"] = dict(name=camera.GetName(), position=vec(transform.T()),
                                 rotation=[transform.R().x, transform.R().y, transform.R().z, transform.R().w],
                                 focal_length=camera.GetFocalLength(RLPy.RGlobal.GetTime()))
    except Exception:
        result["error"] = traceback.format_exc()
    (ROOT / "native-inventory.json").write_text(json.dumps(result, indent=2), encoding="utf-8")
