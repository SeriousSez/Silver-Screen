# Male foundation and future identity targets

The user approved the revised CC-derived male head as the anatomical and artistic
foundation. `SS_MaleHead_Foundation_v1.blend` is an exact frozen copy of that
approved head, eyes, teeth and tongue. `male-head-v1.json` records its approval,
file hash and geometry/topology/UV signatures. Its historical internal review
labels predate approval; the manifest records the current status.

The neutral foundation is not the concept character's final identity preset.
Do not continue changing this basis to force a likeness. Future identities are
versioned delta assets above this basis. The current body candidate references
the same untouched head; it does not introduce a second head identity.

## Target boundaries

`identity-target-strategy.json` is an authoring plan, not a working slider library.
No new identity target, expression, preset or customization UI is implemented.
Target families cover cranial shape, facial frame, forehead, brows, orbits and
eyes, cheeks, nose, philtrum, mouth/lips, jaw/chin, ears and restrained asymmetry.

Each future target must declare its foundation/topology revision, local vertex
deltas, affected regions, dependent modules, landmark/pivot changes, and validated
combination envelope. Store deltas against the frozen basis; do not accumulate
edits on an already morphed mesh. Retain `CC_source_vertex` only as provenance and
correspondence within this topology revision, not as a global population ID.
The head's 20 authored lacrimal vertices have source ID -1 and must also be included
by their foundation vertex index. A topology revision requires explicit remapping.

Apply coarse cranial/face proportions before local feature targets; then apply
restrained asymmetry and combination correctives. Keep both sides coordinated
unless an asymmetry target explicitly owns the difference. Regional deltas must
blend through neighboring anatomy, not leave isolated displaced features.

- Eye placement and shape require coordinated orbital/lid targets, independent
  eyeball transforms and gaze pivots. Test lid contact and complete closure.
- Mouth, jaw and chin changes include lip seal, mouth bag, teeth/tongue placement
  and future jaw pivot/correctives. Exterior-only deltas are insufficient.
- Cranial, forehead and ear changes carry scalp/hair/headwear fit information.
- Neck changes use a shared seam contract with the body. Preserve the seam or
  provide matched head/body deltas; never silently pull only one module apart.

Expression targets live in a separate namespace and are fitted to the resolved
neutral identity. Identity is persistent rest anatomy; expression is transient
performance. Expression correctives may depend on identity but are not saved as
identity weights. The existing 148-source-shape audit remains authoritative:
118 facial-action/viseme candidates, 12 gaze/neck corrective candidates, 18
source-specific head-pose/eyelash shapes; zero supplied body/identity targets.
All remain archival. Refit only selected useful expressions after neutral
anatomy approval; do not reactivate the complete supplied shape library.

The future concept preset should reference a foundation revision and authored
identity target weights, then separate skin/iris pigmentation and module choices.
It must not replace the basis or store expression values. It remains unauthored.
Skin pigmentation and iris pigmentation remain material-data concerns separate
from identity geometry and from one another.

## Body proportions and composition

The current neutral-fit body is one candidate, with professional source topology
and explicit segment landmarks. The height change is distributed through pelvis,
torso, upper/lower limbs and feet; object scale stays one. Source rig weights serve
only as static regional selectors during this authoring pass. No CC skeleton or
skin binding enters the candidate.

Future height profiles should coordinate pelvis/hip height, femur/tibia balance,
torso length, shoulder/neck placement and arm reach with rest-bone offsets under
the preserved 91-bone semantic contract. Record joint-axis, IK-anchor and bind-pose
changes together with mesh and garment-fit changes. Root stays at the soles.
Independent shoulder, ribcage, waist, pelvis and limb proportions need bounded
anatomical targets; changing height must not uniformly scale the character.

Structural frame, fat distribution and muscle volume are separate target layers.
Fat targets distribute volume through abdomen, flanks, hips, limbs and other
appropriate regions. Muscle targets follow muscle groups and joint transitions.
Neither is a global width scale. Interacting targets require volume/contact and
pose checks before ranges are exposed. No variation library is claimed now.

Future male/female foundations may use different topology and UVs. Share semantic
bone meanings, animation/attachment contracts and appearance-data boundaries,
not assumed vertex order or one mandatory chest/pelvis mesh.

## Promotion gate

Keep neutral round-trip, topology/UV signatures, independent eye/mouth modules,
neck seam, symmetry intent and anatomical contact checks reproducible. Validate
target extremes and meaningful combinations, then expression/rig deformation and
module fits. A range is unavailable until these checks and visual review pass.
The current task stops at the male body foundation visual gate, before garment
fitting, skinning, animation or Unity integration.
