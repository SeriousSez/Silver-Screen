# Studio Services LOD validation — 2026-09-29

Completed in Unity 6000.6.3f1, with the project's URP setup. Approved source,
LOD0, shared materials, gameplay components and saved scenes are preserved.

## Representation cost

These are assembled representation counts, not measured player draw calls or FPS.

| Representation | Triangles | Vertices | Renderers | Material slots | Unique materials |
|---|---:|---:|---:|---:|---:|
| LOD0 | 790,108 | 995,872 | 198 | 623 | 37 |
| LOD1 | 265,109 | 448,652 | 97 | 97 | 37 |
| LOD2 | 110,875 | 184,093 | 94 | 94 | 36 |

Relative to LOD0, LOD1 reduces triangles by 66.45%, vertices by 54.95%,
renderers by 51.01%, and material slots by 84.43%. LOD2 reduces those counts
by 85.97%, 81.51%, 52.53%, and 84.91%. No duplicated materials are introduced.
Shadow-casting renderer flags fall from 198 to 85 at either lower level; this
is not a shadow-pass draw-count measurement.

LOD1 removes tiny fasteners and mechanical details and simplifies curved/bevel
geometry. LOD2 removes further small hardware, surface details and tiny props.
Both preserve major architecture, openings, glazing, signs, roof, canopy and
usable cutaway interiors. LOD0 has not been simplified. A fourth LOD was not
justified for this pass.

## Integration and reusable content

One building LODGroup owns all renderers. Lower geometry is combined within
each existing cutaway group and shared material, with transparent geometry
kept separate. This avoids the incorrect colours observed in experimental
multi-material combined renderers. The final material-homogeneous batches
were inspected in Unity and have a regression test.

The repository contains **63**, rather than the request's 61, canonical kit
assemblies. All 63 have independent LOD prefab variants; their approved base
prefabs, definitions, materials and meshes are unchanged. New standalone
placements can use these variants. Existing catalog references are not
silently redirected. Building-combined derivatives are reused per building
prefab and have no duplicate gameplay, navigation or collision state.

Screen-relative thresholds are .72/.28/.008 for the building and
.48/.14/.012 for standalone kit variants. The building reference size is
5.619 m architectural height rather than yard width. Native distance-based
cross-fade is configured, with no global LOD-bias, camera, shader or quality
setting changes. Existing PC LOD bias is 2. Actual renderer bounds remain
geometry-derived. Purely transparent lower-detail renderers do not cast
opaque shadows. No new per-frame LOD script is needed.

Reproduction, provenance and rollback instructions are in
`ArtSource/StudioServices/LOD_PIPELINE.md`. Blender masters remain untouched;
lower representations have separate derivative exports, source/generator
hashes, omission manifests, and Unity import/build tooling.

## Validation evidence

- `export-validation.json`: both source hashes unchanged; all derivative mesh
  packets have valid attributes, indices and nondegenerate geometry.
- `lod0-equivalence.json`: **8/8 matched LOD0 PNGs byte-identical** to the captured
  pre-LOD baseline, including close, gameplay and cutaway views.
- `metrics.json`: all levels respect reveal ownership/restoration; collider
  states and semantic-anchor transforms unchanged; all 63 kit variants present.
- `building-checks.json`: **37/37 checks passed**, including 19 clear employee
  anchors and nine complete routes through the isolated production NavMesh bake.
- **51 distinct focused tests have passing results:** nine new LOD tests and
  42 existing building, interaction, recruitment and construction tests. The
  latest full suite was **50 passed / 1 failed** (`tests.xml`); the failed runtime
  case subsequently passed in isolation (`runtime-tests.xml`). Do not describe
  this as one clean 51/51 run. Earlier failures are retained in
  `tests-first-run.xml` and `tests-second-run.xml`.
- First runtime attempt timed out during applicant arrival. Second completed
  arrival, the first hire and interior routes, but failed at the second carry's
  reveal assertion. The test has real-time deadlines and focus-sensitive input;
  `runtime-observation.txt` records editor focus changes during the investigation.
  Focus/timing is a plausible explanation, not a proven diagnosis of both prior
  failures. No gameplay or navigation changes were made to force a pass.
- The successful actual Studio Play Mode test covered empty-studio startup,
  physical applicant arrival, original-release carry persistence, Escape restore,
  contextual reveal, separate-click immediate authored hiring into both
  professions, preserved person identity, employee entry and routing to workshop
  and storage, and per-frame capsule collision checks. See `runtime.txt`.
- `Sweep/` and `camera-sweep.mp4`: 120 actual URP camera renders over separate
  Editor updates, native automatic LOD selection, 12–100 m range, 360-degree
  orbit, 15–52-degree elevation, repeated rapid zoom reversals, reveal and restore.
  This is a scripted sampled camera path, not a shipping-frame-rate benchmark.
- Matched LOD captures and representative sweep frames were visually inspected,
  including near cutaway and restored exterior. No missing major architecture,
  exposed shell holes, broken placement or incorrect material assignments were
  observed in the final inspected views.

## Remaining limits and manual review

Fine fence wires and corrugated roof detail still shimmer at distance. Native
dither fading is visible in individual transition frames. Review the sweep at
the intended display resolution and camera settings before tuning thresholds
further. Forced LOD2 close-ups are diagnostic stress views, not its intended
viewing distance. Visual inspection is not proof that every angle is perfect.

LOD0 remains expensive; lower meshes add resident asset memory, and inactive
LOD renderer objects still exist. This pass reduces active rendering cost,
not gameplay object/collider count. Shared-material count is mostly retained
to preserve appearance; fewer material slots do not equal fewer unique materials.
Batching trades fine-grained renderer culling for fewer submissions. Buildings
remain placeable; no global static batching or project setting was changed.

Target-hardware player GPU timings, multi-building population stress tests and
shipping build validation were not performed. The existing input-driven runtime
proof remains intermittently sensitive to desktop execution conditions.

`change-manifest.json` records exact new/changed files against the captured
worktree baseline. No commit or push was made. No next milestone was started.
