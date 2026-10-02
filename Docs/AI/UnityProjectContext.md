# SilverScreen Unity context

Inspected 2026-09-22 for the Stage 1 clean-candidate production geometry gate.
Repository: `C:/Repos/Unity/Silver Screen`; HEAD
`1c994a4653e157d3b9f68cea4098f1419fde9f93`. The tree already contained extensive
uncommitted work. This task adds isolated candidate/source/review files.

- Unity **6000.5.9f1**, URP **17.5.0**, Linear colour, active Windows 64-bit target.
- `Assets/Scenes/Studio.unity` is the sole enabled build scene. Main Camera is
  perspective, FOV 60, saved at `(0,18,-15)`, pitch 52 degrees.
- The active PC URP asset, native volume, procedural sky and directional light
  establish studio daylight. See `Docs/Rendering/StudioDaylightBaseline.md`.
- Input System 1.20 is used by the management camera. AI Navigation 2.0.14 and
  Unity Test Framework 1.7.0 are installed. No gameplay networking was confirmed;
  Multiplayer Center's presence alone is not evidence of multiplayer gameplay.
- First-party code uses Unity's default assemblies (no first-party asmdefs found):
  domain logic under `Assets/Scripts/Domain`, services/application logic separate
  from `Assets/Scripts/Presentation`, Editor tests under `Assets/Tests/Editor`.
  Namespaces begin `SilverScreen`; private serialized fields use `_camelCase`.
- `StudioBuildingView` provides building type and interaction anchor.
  `StudioWorldRouter` discovers active building views, registers facility IDs,
  rejects duplicate Stage numbers/IDs and prepares production presentation.
- `SoundStageIdentity` owns the Stage instance number and stable facility ID;
  `StageArchitecturalSign` assembles independent glyph meshes at runtime/Edit Mode.
- Production stations, actor marks, blocking points and slate positions are
  separate layout components. `PrototypeProductionCamera` and `LiveFilmingPlayback`
  supply Live Camera View. Their existing Stage coordinates need migration before
  integrating the clean candidate; see its review report.
- Art generators under `ArtSource` own source blends and exports. Production
  environment art is under `Assets/SilverScreen/Environment`. Follow root,
  Environment and ArtSource `AGENTS.md`. Preserve all asset GUIDs and references.
- Existing official Unity AI Assistant MCP provider: package **2.19.0-pre.2**,
  local relay `C:/Users/Sez/.unity/relay/relay_win.exe --mcp --project-path ...`.
  Unity connection, scene reads, C# execution, Console and Main Camera URP captures
  were verified. This session needed approved access outside the shell sandbox
  to connect to the local named pipe. No provider/package was installed.
- Stage candidate revision 04 preserves the approved production architecture and
  adds focused grounding, opening, service/lighting clearance, hardware, canvas,
  crown, blackout and roof-material corrections. Source GROUND=0 is now the
  lowest export surface; FLOOR=0.08. Seven personnel sweeps, fifteen wall-lamp
  checks and 290 roof-blackout coverage rays pass. See the review report.
  The candidate exports 29 semantic meshes, nine shared 1K PBR sets and about
  630K triangles. The 47-view gallery includes all seven service doors and eight
  low perimeter angles. Numbering remains instance-driven. Original Stage and
  Administration remain unchanged. No LOD/optimization or prop extraction.
  Candidate remains unregistered with the production router, with five broad
  colliders, 31 permanent worklights and a 128px interior reflection. Roof
  renderers use an exterior probe anchor. Repeated refresh/save is verified
  to retain one lighting root with 31 lights (a prefab-override duplication
  bug was fixed in the review companion). No Play Mode/build/navigation work.
- The user's Studio acquired an unsaved extra candidate placement during this
  pass. It remains open and dirty; it was neither saved nor reloaded. Revision 04
  seats that extra candidate as one Undo-recorded whole-root placement from
  Y=0.200003 to actual ground Y=0; child transforms are not offset. A snapshot
  is under the candidate's `ReviewSnapshots` folder. MCP validation and captures
  use `OpenPreviewReview()` so the working scene is preserved. Do not blindly
  run an old restore script or open a scene in Single mode over unsaved work.

Evidence: ProjectVersion, package manifest, EditorBuildSettings, camera/controller,
StudioBuildingView, StudioWorldRouter, Stage identity/sign components, production
layout/camera components, Studio daylight document, Administration generator export
conventions and MCP scene probes. This is focused art-task context, not a full
architecture or release-readiness audit.

## Fresh character direction — 2026-09-22

- The current character brief supersedes all prior character-art experiments.
  `ArtSource/Characters/Gate1`, `Canonical`, `ThirdParty` and Renderpeople content
  are rejected art sources. Do not use their geometry, textures, identity,
  clothes, hair or body to construct the new canonical character.
- Fresh source root: `ArtSource/Characters/SilverScreenAdult`. The copied user
  concept is authoritative. `production-contract.md` records one-character
  scope, complete underlying anatomy, independent modules, future anatomical
  customization, facial deformation, rig/clip and Unity visual gates.
- Current implementation is ONLY an authoring skeleton scaffold: 91 bones,
  55 semantic Humanoid mappings, root at feet, unit scale. Structural assertions
  passed in Blender 5.0.1. No meshes, weights or clips exist in this source yet;
  no character or visual gate has passed. See `authoring-check.json`.
- `CharacterView.CalibrateScale` uniformly scales prototype geometry and rest
  positions. That path does not meet the new anatomical customization contract.
  Keep it separate from future coordinated skeleton/mesh/garment fitting.
- Existing presentation time is
  `SilverScreen.Presentation.SimulationTime.LocalPresentationTime.Delta`.
  Strategic speed never scales local animation time. No Phase 2 travel work.
- Official Unity relay read verified this project and Editor version, Edit
  Mode, `Assets/Scenes/Studio.unity` open and dirty. It was not modified.
  Fresh mesh generation has not been submitted; the connected generation tool
  requires first-use consent. Tool/model discovery evidence is under
  `ArtReview/Characters/SilverScreenAdult`.

### Character correction — supersedes the preparation status above

The user explicitly rejected the subsequent locally constructed body/head as
primitive procedural anatomy, below the SilverScreen concept's quality target.
The failed meshes, fitted garment shells and renders are quarantined under
`ArtSource/Characters/SilverScreenAdult/Rejected/LocalAnatomy_20260922`; their
generator is disabled. Do not reuse, polish, retopologize, fit, skin or animate
that anatomy. Some fitted-shell renders existed before the stop instruction;
they are rejected too. No skinning or animation occurred.

Current gate is a **new head only**, with neutral front, three-quarter, profile,
close-up and wireframe views beside the approved concept, followed by user
review before further character work. See `HEAD_GATE.md`. Future body design
must use a neutral continuous pelvis/groin with **no modeled genitalia**.
The rig scaffold and independent accessory source were preserved byte-for-byte.
No accepted replacement head exists. The assistant reported that its local
scripted approach cannot reliably deliver the requested sculpt quality.

Unity generation consent was granted. One waistcoat source study completed;
other requests failed, including `InsufficientFunds`. The user selected local
authoring afterward. No new generation is running. The Editor-only generated
study was never integrated as canonical art. Studio and Stage 1 were not changed.

## Production lifecycle and scene template foundation — 2026-09-27

- Rechecked Unity 6000.5.9f1, URP 17.5.0 and the default first-party assemblies.
  Domain/Editor tests run through the installed Unity executable; this session
  exposed no Unity MCP tools. The official provider/package remains unchanged.
- `MovieProject.CurrentState` and completed-scene progress now derive from
  scene/cast/release facts. Do not reintroduce arbitrary project state/progress
  setters. `MovieScene` requires a completed kept take to finish; `CanRelease`
  also requires finalized quality. Strategic take work remains authoritative.
- Coordinator retries failed/unresolved production and ignores stale routing
  callbacks. UI distinguishes casting, completed scenes and active take work.
- Generic immutable template/role/beat/variant/semantic metadata lives in
  `Assets/Scripts/Domain/Performance`, without Unity or character dependencies.
  The first consumer is `HeatedArgumentTemplate` and its Editor silhouette preview
  under **SilverScreen > Scene Templates > Heated Argument Preview**.
- Production has no save/load implementation. No full Movie Maker or character,
  animation, IK, voice or advanced-camera integration was added.
- See `Docs/ProductionLifecycleAndSceneTemplates.md` for the pre-change audit,
  authoritative invariants, model boundaries, reproduction and validation evidence.


## Person interaction foundation � 2026-09-28

- Rechecked authoritative version: Unity **6000.6.3f1**, URP **17.6.0**, Input System 1.20, Test Framework 1.8.0. Existing official relay connection verified this project in Edit Mode.
- `PersonProfile` remains the identity owner. `Candidate.JobSought` records original intent; `RecruitmentCoordinator.Hire(candidate, profession, hiringFacilityId)` validates the target facility, never proficiency or applicant origin. Existing one-argument hiring, salary, registration and tutorial events remain.
- Selection owns hover/right-click/hold gestures through `PersonInteractionController`. `PersonDragSession` places via Warp with 0.15 m maximum NavMesh correction and lot/capsule checks. Employee task anchors survive; routing resumes later from the dropped position. Camera right dragging yields only to person gestures.
- Semantic `PersonInteractionSpot` anchors register with facility applicant areas. Nearby compatible labels appear only during pickup. Practice is `PersonPracticeService`, backed by existing work/reservation services and genre experience. Work availability prevents earning while displaced; external work assignments cancel practice.
- Prototype cards use provider urgency/relevance and world projection. Presentation is replaceable. No final art, new character models, scene/prefab serialization, or save framework.
- See `Docs/PersonInteractionFoundation.md` for the audit, behavior contract, developer proof and focused validation. Subjective interaction/visual approval remains with the user.


## Stage School A1 inspection - 2026-09-30

Current configuration: Unity 6000.6.3f1, URP 17.6.0, Input System 1.20.0, Test Framework 1.8.0. Official installed Unity relay verified Edit Mode with Studio.unity active and clean. Earlier version observations are historical. Stage School A1 uses separate source and the established Studio Services semantic mesh-packet workflow, sharing period materials and loose fixtures without changing them. See ArtSource/StageSchoolA1/design.md. No runtime integration.

## Purchased character evaluation - 2026-10-02

The current authorized character milestone supersedes the historical head-only
gates above for this evaluation. Purchased Male/Female/Boy/Girl sources remain
external and unchanged. They are four separate canonical mesh families: do not
unify topology/UVs or morph between them. Child hierarchy/export validation is a
required later task; no child runtime integration was performed.

One adult female supplied deform-bone FBX is an opt-in local prototype under
Assets/SilverScreen/Art/Characters/HumanBasePrototype (Git-ignored licensed
imports). Rebuild through HumanBasePrototypeTools; do not copy the whole pack.
Production capsules, recruitment prefabs and Studio serialization are unchanged.
The prototype uses existing EmployeeAgent navigation and HeldPersonPoseAdapter,
with valid Humanoid mapping and 29 imported mouth/cheek/tongue shapes. Blink,
brow and gaze are absent in this export. Supplied names differ by family;
a semantic facial mapping layer is recommended, not implemented as a final system.
Plain-grey URP captures are technical validation only, never art approval.

See Docs/CharacterBaseAudit.md for results, limitations, reproduction, source
inventory and next milestone. Unity remains 6000.6.3f1 / URP 17.6.0; the already
installed official local MCP relay was used. The prototype's actual movement,
click/hold/drop/cancel/contextual practice, facial deformation and two-person
Socialize scenarios passed isolated runtime tests.

## Adult Female facial investigation - 2026-10-02

See Docs/FemaleFaceRuntimeInvestigation.md and Docs/FemaleFaceBoneEvidence.md.
The source supports independent lid controls, gaze and brows, but the supplied
facial FBXs bind all 708 eye vertices to Head and lose the source gaze mechanism.
An isolated 95-bone hybrid preserves the 29 lower-face shapes and Humanoid body;
its reduced binding fails source blink parity and visual closure. It is rejected
for adoption. Do not replace the lightweight prototype or production capsules.
Future work must resolve the neutral/bind/deformation gate before choosing reduced
bones versus baked upper-face shapes plus two eyes. No next milestone is authorized
by this investigation. Male and child implementations remain separate and deferred.

## Added child shape-key sources - 2026-10-02

The user added 26 Boy/Girl source files; the current inventory is 85 files.
See Docs/CharacterBaseChildSourceUpdate.md and the corrected CharacterBaseAudit.md.
Both children now have supplied shape-key Blender/FBX/GLB variants; read-only
Blender inspection confirms 29 expression keys in both new FBX variants per child.
The new Boy shape-key files measure about 1.4951 m; the 1.8815 m observation
applies to the shape mesh inside the original facial-rig Blender file.
All four canonical families remain separate. Child hierarchy/export/Avatar
validation and integration remain deferred; no child runtime assets were imported.

## Adult Female baked BlinkBoth gate - 2026-10-02

See Docs/FemaleBakedUpperFaceExperiment.md. A separate generated Blender candidate
retains the actual lightweight FBX neutral, topology, UVs, normals, skin weights,
51 bones and 29 supplied shapes, and adds exactly one evaluated-source BlinkBoth.
Displacement transfer is accurate and neutral restores exactly, but closer matched
views and geometry ray tests reveal the source endpoint itself leaves about 11%
of each front-view eyeball exposure. The earlier complete-closure claim was
overstated and is corrected. The contact gate failed; no candidate Unity import,
independent blink, eye/gaze or brow expansion followed. Recommendation: continue
investigation only after separately authorizing an accepted source closure endpoint.


## Adult Female bald canonical-base check - 2026-10-02

See Docs/FemaleBaldBaseAudit.md. The separate source hair becomes four disconnected
islands in the shape-key variant; the underlying body/head is closed and has a
complete scalp. The isolated FemaleBaldPrototype removes only those islands.
Blender proves retained data preservation; its exported surface identifies a
native Unity subset preserving original BlendShape normals, weights and frames,
after a direct FBX round trip failed normal parity. The final prefab keeps the
validated 51-bone Humanoid setup and all 29 supplied shape data sets. Two targeted
Unity tests pass, but six original zero-frame-weight channels produce invalid
deformation and remain unaccepted. Previous upper-face acceptance failures remain.
Brow skin and source brow bones are retained; no separate eyebrow hair/texture was
found. Technical grey views are not art approval. Sources and previous prototypes
are unchanged; production spawning is untouched. No hairstyle system was added,
no commit/push was made, and other families/child integration remain deferred.


## Adult Female source blink endpoint calibration - 2026-10-02

See Docs/FemaleSourceBlinkCalibration.md. Original complete Blender rig only;
130 distinct new bilateral endpoints were explored with the established BVH
cameras, then finalists measured at 0.05 mm spacing. Near-total coverage is
achievable, but selected lid geometry has 20 exterior triangle crossing pairs
and a pinched close-up seam; the best tested crossing-free comparison still
leaves a 1.4 mm oblique opening. Shared left/right inputs suffice geometrically;
no independent blink was created. Source acceptance failed. Recommend a small
authored lid corrective; none was authored here. The proven bake implementation
and previous evidence remain unchanged. No re-bake or Unity investigation was
performed, no new runtime asset was created, and no further milestone was begun.
Source neutral resets exactly; all 85 purchased files and 96 preserved prior
files retain their hashes. Technical grey captures remain non-art-approval.
