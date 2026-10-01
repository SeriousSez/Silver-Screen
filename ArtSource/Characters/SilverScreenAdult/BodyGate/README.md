# Male body foundation candidate

Current pass: **male-anatomy-02; visual review pending**. The user approved the
professional topology but rejected the previous anatomy. That exact checkpoint
and its authoring script/reports are retained in `TechnicalFoundation_20260923`.
`Reference/ApprovedMaleBodyReference.png` is the user-supplied body concept and
visual acceptance target, not merely a general anatomy reference.

One complete neutral-fit male anatomical body derived from the user-authorized
CC professional topology. The frozen approved male head, independent eyes,
teeth and tongue are assembled with it but remain separate objects.

`SS_MaleBody_Foundation_Candidate.blend` is the editable body gate. Rendered review
views are in `ArtReview/Characters/SilverScreenAdult/BodyGate`. The body remains
unapproved pending the user's visual review. No clothing, body-region hiding,
skin binding, performance controls, animation or Unity export is present.

## Authoring and provenance

`extract_body.py` reads only the explicitly supplied
`C:/Users/Sez/Downloads/Blender/Blender 1/blender.Fbx`. It extracts torso/limbs,
hands, feet and nails while retaining topology, source vertex IDs and UVs.
The source head and eyelashes are excluded. The originals are unchanged.
`source-body-topology.json` and `source-body-data.npz` retain this technical input.

`build_body.py` derives a new male ribcage, chest, back, waist and pelvis, then
coordinates limb lengths, girths, hands and feet around explicit landmarks.
The thigh/lower-leg balance and torso length change independently; the result
is approximately 1.777 m tall with soles at zero and object scale one. It is not
a uniform whole-body scale or a flattened female chest with wider shoulders.
The lower pelvis stays continuous and neutral without genital geometry.

Source skin weights are read solely as static regional selectors to author
rest geometry. No CC armature, vertex groups or source skin binding is copied
into the candidate. The preserved SilverScreen 91-bone architecture is the future
target. The A-pose landmarks in the body report are not a replacement rig contract.
Rig rest placement and skinning require later work after body approval.

`male-anatomy-profile-v2.json` now drives the mesh's segment placement and torso
sections together. Shoulder joint-centre separation changes from 35.4 to 41.0 cm;
hip joint-centre separation changes from 18.2 to 17.6 cm. These are skeletal
landmarks, not external skin widths. The ribcage is broader/deeper, the waist-to-hip
transition straighter, and the pelvis/thigh origins narrower. Pectoral planes,
abdominal/oblique forms, scapulae/lats, glute distribution and limb volumes are
reshaped regionally. Height is unchanged and no global body scale is used.

The lower review A-pose and forearm orientation approach the supplied reference.
Its posterior axillary web is corrected in the rest mesh. No pose animation or
skin binding is created. `male-rest-landmarks-v2.json` names the proposed fitting
segments using the preserved 91-bone contract. The hidden viewport collection
`SS_AnatomicalLandmarks_ReviewOnly` contains inspectable joint guides. These are
unbound anatomy guides, not a completed fitted/weighted skeleton.

The shoulder/collar region is faired into the frozen head's 40-vertex boundary,
with its body-side tangent adjusted using fitted collar distances.
Every head, eye, tooth and tongue mesh coordinate, face, UV and source ID is checked
against the frozen file. No further identity sculpt is performed on the head.

## Rebuild and inspect

From the repository root:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.0/blender.exe' --background --factory-startup --disable-autoexec --python-exit-code 1 --python 'ArtSource/Characters/SilverScreenAdult/BodyGate/build_body.py'
& 'C:/Program Files/Blender Foundation/Blender 5.0/blender.exe' --background --factory-startup --disable-autoexec --python-exit-code 1 --python 'ArtSource/Characters/SilverScreenAdult/BodyGate/validate_body.py'
```

The builder checks the frozen head hash before use. `body-report.json` records
dimensions, landmarks and scope. `validation-report.json` inspects the saved
candidate: 9,309 body vertices, 9,288 quads, source UV/index correspondence, an
exact neck seam, and a closed anatomical shell in a temporary seam-weld check.
Saved head/body modules remain separate. The file opens at a full-body view.

## Current limits

Visual inspection finds a broader male torso, narrower pelvis than the original
technical checkpoint, revised thigh distribution and a stance closer to the
body concept. Surface definition is still softer than the concept, particularly
through shoulders and back. This candidate is not claimed to meet the concept's
sculpt quality; the comparison page exposes that remaining gap for this gate.
The assembled neutral render also shows a small neck shading seam despite exact
boundary positions; normal continuity across the separate modules is unfinished.

Neutral front, three-quarter, profile, back, torso, neck, pelvis, hand/foot and
wireframe renders are provided, with additional head-hidden views and a back
close-up. The body reference and new candidate are shown together. The previous
checkpoint uses a different arm pose and broader neutral fill lighting; its
archive is historical evidence, not an identical-pose comparison. These are mesh
review views, not visual
approval or deformation tests. No skin-to-skin cage intersections or degenerate
faces were found. The source and candidate both report 29 nail/skin triangle
contacts near tips; they remain documented cleanup work before production.
Subdivision-surface intersection testing, seam normals under motion, weights,
joint correctives, UV relaxation/density, final materials and Unity behavior are
not validated. Retained UV correspondence does not guarantee ideal new texel density.

The rig scaffold, rig contract, accessory file and dirty Studio scene are
hash-checked unchanged. Stage 1 is not modified. Stop here for body visual review;
do not fit garments, skin or animate before approval. No commit or push.
