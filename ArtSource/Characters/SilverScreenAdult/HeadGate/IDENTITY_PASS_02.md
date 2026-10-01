# Identity pass 02 — visual review pending

The user approved the previous CC-derived head as the technical foundation and
requested one focused identity pass. The approved file, authoring script and
report are preserved in `ApprovedTechnicalFoundation`; matching original renders
are preserved under the review directory's folder of the same name.

The current `SS_Head_Candidate.blend` is the refined head. `build_head.py` remains
the entry point and now delegates to `refine_approved_head.py`, which starts from
the exact approved Blender file. The original generator is archived with the
baseline. Repeated builds do not accumulate sculpt offsets.

## Art direction

- Broaden the upper cranial vault and lower the crown slightly.
- Strengthen the brow/orbital roof while opening the upper-lid contour modestly.
- Give the bridge, tip and alae a more coherent, stronger nose shape.
- Define the zygomatic and lower-cheek planes without hollowing the face severely.
- Clarify the mandibular angle; broaden and project the chin slightly.
- Improve upper/lower lip volume, mouth-corner rhythm and upper-neck support.

The largest displacement from the approved foundation is approximately 3.45 mm.
The pass preserves anatomical balance and uses the same neutral material,
lighting, camera angles, framing and scale as the approved review. The independent
eyeballs keep their exact geometry and position. The removable eyebrow study is
refitted to the revised brow surface.

## Preservation and checks

Every mesh retains its original polygon connectivity, UV coordinates and source
vertex mapping. The head remains 4,211 vertices / 4,186 quads, with the same
40-edge open neck seam. That seam is fixed to the approved positions. No new
degenerate faces or edges with more than two incident faces were found.

`head-report.json` records these checks and per-module displacements.
`identity-pass-02-deltas.npz` stores the rest-shape change separately from the
archived source expressions. No source expression key was promoted or refitted
as part of this pass. Blink closure, facial performance and runtime deformation
remain unvalidated.

The review page provides matched before/after front, three-quarter and profile
views, plus the approved concept, close-up, wireframe and unobscured clay view.
There is no supplied close-up head profile reference; the profile comparison uses
the approved baseline and retains the concept front portrait as identity context.

STOP here for art-direction approval. The technical foundation approval does not
authorize starting the body before this refinement is reviewed. No body or
wardrobe construction, rigging, animation, Unity integration, commit or push.
