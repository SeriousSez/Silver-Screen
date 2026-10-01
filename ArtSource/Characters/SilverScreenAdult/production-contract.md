# One canonical SilverScreen adult: authoring and acceptance contract

Recorded 2026-09-22 from the user's current request. This is an implementation
contract, not evidence that the requirements have been met.

**Current gate: FULLY ASSEMBLED UNRIGGED CANONICAL CHARACTER.** The user approved the
revised CC-derived male head as the anatomical/artistic foundation. Its neutral
basis is frozen in `Foundations`; future individual identities use controlled
targets above that basis, separately from expressions. The complete neutral-fit
male body is frozen in `Foundations` after the authorized final definition and
shoulder-relaxation pass. `CanonicalGate` contains the modular assembled review.
Stop before final skinning, animation or Unity integration. See `BODY_GATE.md` and
`Foundations/IDENTITY_TARGETS.md`. Preserve the 91-bone scaffold and accessories.
The rejected procedural anatomy remains rejected evidence only.
The user approved the body edge layout but rejected the first male body anatomy.
The current `male-anatomy-02` pass follows the newly supplied
`Reference/ApprovedMaleBodyReference.png`, coordinating rest landmarks and mesh.
The latest user instruction approved that revised body's proportions/silhouette
and authorized progression after one final restrained definition pass. It is
complete. The assembled canonical character still requires visual approval.

## Visual target and source

One original adult male matching the approved concept's stylized realism:
believable slim/fit proportions, strong brow and jaw, expressive brown eyes,
dark swept hair, ivory period shirt, dark tie and waistcoat, high-waisted dark
trousers, leather shoes, brown felt fedora. A replaceable suit jacket follows
the close-up reference; the waistcoat outfit remains the main full-body target.
Preserve the concept's facial planes, silhouette and tailored construction in
the eventual canonical identity preset and outfit. Do not destructively force
the approved neutral anatomical head into that exact identity.
No additional identities, demographic variants or alternate art directions in
this milestone. The user has explicitly authorized their supplied Character
Creator geometry as technical base topology, superseding the original blank
geometry requirement. Substantially reshape it into a new SilverScreen identity;
do not adopt its current proportions, styling or skeleton. No other downloaded
human, scan, rejected procedural character or Renderpeople source material.

Only two attached images were received: an annotated brief and the visual
concept sheet. No separate topology image was present. Topology references, if
provided later, govern mesh flow only, not identity or proportions.

## Art construction boundaries

| Module | Boundary |
| --- | --- |
| Anatomical body | Complete torso, limbs, articulated hands and feet beneath clothing, with a neutral continuous pelvis/groin surface; no modeled genitalia or garment geometry baked in |
| Head/face | Separate skinned head; matching neck seam positions, normals and weights with body |
| Left/right eyes | Independent geometry and gaze bones; separately controlled iris pigmentation |
| Hair | Replaceable scalp/hair module; head-weighted initially, helper bones only where deformation requires them |
| Facial hair | Independent optional slot; empty for the clean-shaven reference |
| Upper garments | Shirt, waistcoat and optional jacket remain separate fitted meshes |
| Lower garments | Trousers remain separate from anatomy and shoes |
| Shoes | Independent left/right fitted meshes, weighted to foot/toe motion |
| Hats | Independent fedora; hair compatibility is explicit |
| Accessories | Tie, belt and other accessories have independent binding and replacement boundaries |

All modules bind to the same instance skeleton. Module replacement remaps bone
references by validated semantic identifiers; never instantiate a second
animated skeleton per garment. Compatibility includes rig version, anatomical
foundation, fit revision, seam revision and required bones. Unsupported fits
must be rejected rather than silently stretched.

Body coverage is a non-destructive visibility mask per region/submesh. The
source always retains all anatomy. Removing a garment restores its hidden
regions; overlapping coverage composes correctly. Hiding an entire body renderer
is not an acceptable substitute. Occlusion and clearance must be checked in
every required pose.

The pelvis/groin must be anatomically plausible for underwear and clothing
deformation, without explicit genital geometry. The prior explicit design was
rejected. Complete underlying coverage does not require genital modeling.

## Rig and topology

Units are metres; origin is the midpoint between the feet on the ground.
Root scale stays one. The initial anatomical landmark height is 1.78 m, to be
refined against the approved silhouette; the hat is outside anatomical height.
Blender uses Z up and character forward -Y. FBX export uses -Z forward / Y up,
with the imported Unity character facing +Z verified in the Editor.

Use the existing semantic names Hips, Spine, Spine1, Spine2, Neck, Head,
Left/RightShoulder, Arm, ForeArm, Hand, UpLeg, Leg, Foot, ToeBase and all three
segments of Thumb/Index/Middle/Ring/Pinky. Add jaw, independent eyes, distributed
arm/forearm/thigh/calf twists and shoulder helpers. Root-relative interaction
and IK anchors are distinct from deformation bones. Semantic names are
compatible integration points; the old prototype's geometry and rest matrices
are not reused. HumanDescription mappings must be verified on the finished
mesh, not inferred from a successful bone-name check.

Facial flow requires concentric orbital and lip loops, eyelid thickness, a
mouth bag, mouth corners, nasolabial flow, jaw/chin support, ear attachment and
head/neck continuity. Avoid poles on eyelid margins, mouth corners and principal
creases. Independent lids must close over the eyeballs completely. Jaw opening
must preserve lip volume and expose an actual mouth interior. Use expression
correctives where bone deformation alone collapses volume.

The body needs loops through shoulder/armpit, elbow, wrist, finger knuckles,
hip/groin, knee and ankle. Verify linear skinning as used in Unity; Blender's
preserve-volume skinning alone does not establish runtime quality. Twists and
helpers must have meaningful weights/drivers, not just exist in the hierarchy.

Minimum face controls: gaze left/right independently, blink left/right,
jaw-open, lip seal, mouth width/pucker, smile/frown, brow raise/lower and basic
expression deformation. Expression channels are separate from identity
channels. A full viseme/performance library is deferred.

## Customization boundaries

Keep logical appearance data independent from Unity materials, meshes and
scene instances. Stable IDs identify anatomical foundation, topology/rig
revision and each module. Identity parameters, anatomical proportions, body
composition, pigment parameters and transient expressions are separate groups.
Do not expose unimplemented sliders as working capabilities.

| Future independent variation | Required source boundary |
| --- | --- |
| Skin tone / undertone | Neutral surface detail plus spatial melanin/vascular/undertone masks and calibrated material response; no RGB multiply of a baked identity |
| Eye color | Iris radial detail separated from pigment controls; sclera, pupil and cornea do not change with iris hue |
| Face / skull identity | Stable head topology and neutral rest state; identity targets can change silhouette, cranial volume and feature placement; expressions are refitted/corrected to that identity |
| Eye shape / spacing | Orbital/lid targets coordinated with eye positions and gaze pivots |
| Brows | Independent brow geometry/material and facial deformation binding |
| Nose, cheeks, jaw/chin, mouth/lips, ears | Distinct anatomical target groups with neighboring transitions, interior compatibility and seam preservation |
| Hair style / color; facial hair | Independent module slots, neutral fiber detail and pigment controls; scalp/face fit contract |
| Adult height | Coordinated pelvis/torso/limb/neck rest landmarks and corresponding mesh targets; rebuild bind poses, anchors and garment fit; no whole-character uniform scale |
| Shoulder/body frame and limb proportions | Skeleton rest offsets plus distributed mesh changes, joint-axis adjustment and fitted garments |
| Fat/weight and muscularity | Separate anatomical volume distributions and corrective targets; no global X/Z widening |
| Male/female adult foundations | Separate anatomy/topology where needed under compatible semantic rig and animation contracts; do not assume matching vertex order or identical pelvis/chest shapes |

Facial topology compatibility is per foundation/revision, not a promise that
all future adults share one mesh. Supporting another foundation must not
require replacing animation, semantic attachments or appearance persistence.
Morph ranges require tests for eye/lid contact, neck seams, mouth interior,
weights, corrective deformation and garment fits before becoming available.

Use non-directional PBR detail: authored UVs per module, independent skin,
iris, hair, cloth, leather and metal material boundaries. Do not bake hats,
hair, skin identity, lighting or costume into a single atlas. Keep identity
shapes separate from wrinkle/pores and texture color information.

## Animation and runtime acceptance

Deliver real editable/baked clips, with continuous contact-aware locomotion
and purposeful weight transfer. Required clips: neutral idle, casual walk,
purposeful walk, run, turn, start, stop, sit, arm raise, deep elbow bend, knee
bend, torso twist, head turn/look-at, hand/handle reach, and duck/crouch.
Check loop boundaries, start/stop continuity, foot contact, toe roll, arm swing,
finger articulation, garment motion and motion at several intermediate frames.
Static samples alone do not establish production-quality animation.

Runtime presentation must use the existing
`SilverScreen.Presentation.SimulationTime.LocalPresentationTime` contract.
Strategic pacing must not accelerate the Animator or clip time. Pause/focused
observation behavior stays explicit. Animation owns no employee location,
navigation path, route reservation or strategic arrival. Phase 2 travel is
excluded. The old uniform-height calibration path is not the new anatomy path.

Inspect actual imported skinned renderers and Animator/retargeting under the
project's Unity 6000.5.9f1 / URP 17.5 setup. Preserve the currently dirty Studio
scene. Use an isolated review scene/prefab until the new art passes review.

Required visual evidence: face front/three-quarter, full-body front/three-quarter/
side/back, topology, separated modules, body with garments removed, material
breakdown, facial controls and deformation sequences. Runtime evidence must
include idle, walk, run, turn, pose tests, Stage 1 exterior scale, Stage 1
interior and actual Live Camera close-up. A substitute camera is labeled as
such and does not pass Live Camera integration.

Inspect and record shoulders/armpits, elbows, wrists, every finger, neck, jaw,
torso, hips/groin, knees, ankles, garment intersections and hair/head contact.
Keep failed and untested checks visible. Neither a neutral render nor an
automatic-rig success response passes this gate.

## Current gate status

| Gate | Status |
| --- | --- |
| Approved reference preserved | Complete |
| Original anatomical/head meshes | Blank-mesh attempt rejected; professional-topology male head and complete body approved/frozen after the authorized final refinement |
| Costume meshes | Rejected-body shells remain rejected; new modular shirt, waistcoat and trousers fit the approved body, with separate hair and fitted copies of original accessories |
| Anatomical body completeness / modular replacement | Complete body retained without genitalia or hidden faces; assembled modules separate; dynamic replacement/deformation validation remains pending |
| Authoring armature | Scaffold built; 91 bones, 55 Humanoid mappings; structural checks passed; no skinning or Avatar validation |
| Facial topology and animation | CC head edge layout retained with lacrimal quad patches; 148 source shapes audited and archived, zero copied into candidate; expressions/animation deferred |
| Materials and UVs | Provisional concept palette, separate iris pigment and soft review lip mask; no baked source identity; garment UV layouts are provisional; final PBR/pigmentation deferred |
| Weighted modular character / retargeting | Not started |
| Validation animation set | Not started |
| Deformation review | Not run |
| Unity character import / runtime / Stage 1 / Live Camera | Not run |
| Unity Editor connection | Verified; Studio open and dirty; not modified |

The male anatomical foundation is approved under the latest instruction. The
canonical identity uses three persistent targets over the unchanged head basis;
expressions remain separate and absent. The complete body basis is unchanged in
assembly. No assembled-character approval or production readiness is claimed.
Structural checks do not replace the visual and deformation gates.

Future identity/proportion/composition targets remain separate from expressions.
One concept preset is authored; no slider library or additional identity exists.
STOP at the assembled unrigged character gate. No skinning, animation, Unity
import, Stage 1 work, commit or push is part of this gate.
