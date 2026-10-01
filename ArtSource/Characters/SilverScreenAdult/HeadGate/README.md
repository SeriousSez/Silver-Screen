# SilverScreen head — technical foundation approved, identity pass 02 pending

The user approved the first CC-derived head as the technical foundation. The
current candidate includes the requested focused identity refinement. Read
`IDENTITY_PASS_02.md` for the changes and matched before/after review. The approved
baseline is preserved in `ApprovedTechnicalFoundation`.

This is a head-only sculpt derived from the user-authorized Character Creator
topology. It replaces neither the SilverScreen rig architecture nor the visual
authority of `../Reference/ApprovedConcept.png`. No new human model was downloaded.
The rejected blank-mesh experiments were not read as geometry or reused.

Open `SS_Head_Candidate.blend` for the editable head, independent left/right eyes,
teeth, tongue, removable eyebrow study, neutral lighting and review camera.
The candidate is not rigged. The head includes the source mouth interior.
The 40-edge open lower-neck seam is intentional; its final matching body seam
must be authored after head approval. The source body is absent from this file.

## Authoring and provenance

`inspect_and_extract.py` reads the specifically authorized `Blender 1/blender.Fbx`
without changing it. Its SHA-256 is recorded in `shape-key-audit.json` and in the
Blender scene. Extracted source coordinates, polygon/UV layouts and archived
shape deltas are technical inputs, not a second canonical identity.

The original `build_head.py` (archived with the approved baseline) applied anatomical deformation fields to this topology. The
fields reshape the skull/temples, brow/orbits, eyelids, nose, cheek planes,
perioral region, jaw/chin, ears and neck. A small resting asymmetry is included.
Two original lacrimal skin openings are closed with shallow quad patches; those
20 new vertices have source correspondence `-1` and interpolated UVs. Existing
source vertex correspondence is retained in the `CC_source_vertex` attribute.
No inherited skin weights or CC armatures are imported into the candidate.

This is editable 3D geometry, not an image-generation result. The renders are
direct Blender outputs. The temporary eye and eyebrow appearance exists only
to make the head review readable; it is not a finished pigmentation/hair system.

To regenerate the editable candidate and its six neutral review images:

```powershell
& 'C:/Program Files/Blender Foundation/Blender 5.0/blender.exe' --background --factory-startup --disable-autoexec --python 'ArtSource/Characters/SilverScreenAdult/HeadGate/build_head.py'
```

`build_head.py` remains the regeneration entry point. It now delegates to
`refine_approved_head.py`, applying the focused identity pass to the preserved
approved Blender foundation with exact topology/UV correspondence. Reconcile future manual sculpt edits with
that source before running it again; do not overwrite an approved manual pass.

## Shape-key decisions

All 148 non-basis source shapes were audited by name, actual displacement and
affected region. Every original head-region delta remains available in
`archival-only-source-deltas.npz`, outside the candidate. None is automatically
considered valid on the new rest shape, and none has been copied into its key list.

| Category | Count | Decision |
| --- | ---: | --- |
| Dedicated identity/body customization | 0 | Build future identity/proportion targets separately from expressions; local nose/ear actions are not a complete identity system |
| Facial expressions/actions/visemes | 118 | Preserve source data for selective refitting; defer the large performance/viseme library |
| Gaze/neck corrective candidates | 12 | Refit only against actual SilverScreen eye and neck bones; source driver behavior is not verified |
| Source-specific head-pose and eyelash controls | 18 | Archive only; head pose belongs to semantic bones, and source eyelash-card controls are irrelevant to the new skin |

The corrective classification is an intended destination, not evidence of a
validated corrective driver. The JSON contains per-shape decisions, measured
affected counts, maximum deltas and bounds, including this qualification.

After identity approval, the first useful refit subset is independent blinks,
brow raise/drop, squint, lip seal, smile/frown and a small pucker/funnel range.
Use `Jaw_Open` only as a study for lip/jaw soft-tissue motion; the semantic jaw
bone must not receive duplicated jaw motion from an unexamined source morph.
Gaze follow and eyelid closure require validation against the newly fitted eyes.

## Gate and validation limits

Review front, three-quarter, profile, topology and close-up beside the concept
at `ArtReview/Characters/SilverScreenAdult/HeadGate/index.html`. A second front
render hides the eyebrow study. The supplied source head appears separately for
provenance comparison. Nothing in that comparison implies visual approval.

The geometry report counts 4,211 head vertices / 4,186 quad faces, one intentional
neck boundary, no degenerate head faces and no edges with more than two faces.
These are structural checks, not a proof of deformation quality. The teeth and
tongue retain their source boundaries and require later mouth/jaw fit testing.
No blink, expression, skinning, animation, Humanoid, runtime or Unity validation
has been claimed at this head gate.

Body adaptation is deliberately deferred. The supplied quad layout includes
torso, arms, legs, hands and feet, but this head work does not establish that its
chest, pelvis, shoulders or joints can become the required male foundation
without local retopology. That anatomical evaluation belongs to the body pass
after head approval. Future male/female foundations share semantic rig contracts,
not a requirement for identical topology. Future groin anatomy remains neutral
and continuous, without modeled genitalia.

STOP at this gate. Do not build the male body, fit clothes, skin, animate or
integrate into Unity before user approval. Keep the 91-bone rig, accessories,
dirty Studio scene and Stage 1 unchanged. No commit or push.
