# Stage School A4 — compact architectural candidate

A separate LOD0 art source using the approved A3.2 modular library. The user's
compact three-room brief and subsequent roof correction are authoritative:
**one low flat roof behind parapets**, with a raised entrance façade. The latest
roof instruction supersedes the earlier request for a terracotta roof.

Main floor slab: 22 × 16 m, 352 m² gross. Three rooms total 321.93 m² inside wall
faces: Hiring Hall 21.36 × 7.05 m; Audition 13.19 × 8.09 m; Interview
7.99 × 8.09 m. Façade projections, steps and exterior forecourt extend beyond the
slab. No separate reception, waiting, support, administration or storage rooms.

## Authoritative workflow

- `generate_stage_school_a4.py` reads the original A3.2 `.blend` and packet as
  read-only dependencies, reuses approved component functions, and saves only A4.
- `StageSchool_A4.blend` is the reproducible editable candidate. Period furniture,
  camera and studio lamps remain linked source collections.
- `ArtExports/StageSchoolA4/stage_school_meshes.json.gz` uses the established
  semantic mesh-packet workflow. No new parallel FBX export was introduced.
- `StageSchoolA4Review.cs` imports into the separate A4 prefab, reusing the
  original Unity mesh/material GUIDs for translated A3.2 assemblies and shared
  period/equipment prefabs. New architecture gets A4 meshes. Reimport preserves
  their existing `.meta` files and GUIDs.
- `authoring_readiness.json` documents future positions only. It does not create
  runtime anchors, visible markers, hiring labels or gameplay components.

Regenerate from the repository root:

```powershell
& 'C:\Program Files\Blender Foundation\Blender 5.0\blender.exe' --background --factory-startup --python ArtSource/StageSchoolA4/generate_stage_school_a4.py
python ArtSource/StageSchoolA4/verify_source.py
python ArtSource/StageSchoolA4/preserve.py
```

`verify_source.py` uses the existing Python NumPy/SciPy environment. The immutable
`preservation_baseline.json` covers 4,036 pre-existing files. Never replace it to
make a preservation check pass. No dependency installation is required.

In Unity Edit Mode use **SilverScreen > Art > Stage School A4**:

1. **Import compact candidate**.
2. **Create isolated review scene**, only when a new review scene is needed.
   This reads the Studio daylight environment additively, then restores the
   original scene. It saves only the A4 review scene.
3. **Capture review views**, in a subsequent editor invocation so URP is
   initialized. A preview scene isolates captures while preserving the working
   scene. Roof/ceiling/front visibility changes are review-only and restored.

Then run `python ArtSource/StageSchoolA4/build_review.py`. The gallery is
`ArtReview/StageSchoolA4/review.html`; it uses actual Unity captures, never
generated concept renders as validation evidence.

The forecourt follow-up turns both bench instances toward the public walk and
adds eighteen curved leaves with connected petioles and fine midribs per planter.
Three A4-local foliage materials keep leaf shading separate from shared upholstery.
The gallery includes bench and foliage close-ups in addition to the ten main views.

Both bench origins are at Z = -9.58 m, 20 cm farther forward than the initial
outward-facing placement. A Unity mesh-envelope check against front-wall triangles
overlapping each bench's width and height measures 117.6 mm minimum separation
from the projecting facade strips. Review views 13–14 show both clearances.
The repeatable relay check is `ArtReview/StageSchoolA4/measure_bench_clearance.cs`;
it rejects clearances below 100 mm without adding runtime colliders or behavior.

## Preservation and milestone boundary

A3.2 remains the historical production master. Unused reception furniture,
restroom fixtures and cubicle doors, archive/storage furniture, service
architecture, lounge chairs and all old roof assemblies remain in the original
sources and Unity assets. An instance omitted from A4 is not a deleted asset.

No Stage School B/B1/B2 work, runtime registration, LODs, navigation, recruiting,
queues, profession assignment, evaluation, dismissal or talent creation/import
is part of A4. Room lights are review presentation only. Manual architecture/art
approval is required before beginning a later runtime milestone. No commit or push.

Validation is bounded: source mesh integrity, rigid reuse, floor-region fit,
roof/ceiling separation, material/mesh references, imported counts, preservation
hashes and actual URP visual review. There is no general collision proof,
hydraulic simulation, gameplay test, Play Mode test or player build.
