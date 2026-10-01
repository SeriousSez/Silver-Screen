# Stage School A3.1 — final known-defect correction

The six requested areas were corrected at source, regenerated, imported, compiled and visually inspected. One corrective art pass followed the first targeted captures. Four categories are resolved; two remain partially resolved. No further art tuning was performed.

| Requested category | Result | Final evidence |
|---|---|---|
| 1. Roof drainage/supports | **Partially resolved.** Bracket direction, fascia fixings, pipe collars/wall rosettes, open shoes and ground gullies improved; window/banner/canopy conflicts corrected. **The front-right entrance pipe still enters the projecting corner pier: its upper offset disappears into the masonry before the vertical run reappears.** | `a3_15_entrance_close.png`, `a3_34_rear_drainage.png` |
| 2. Roof junctions/collection | **Partially resolved.** Roof-plane intersections trim the competing fields; five valley segments, stepped abutment sheets and Hall collectors added. Every roof has a source-traced route to a ground outlet, directly or over a lower roof. **The Hall/Audition collector corner still has a hip-cap/gutter interference and crowded adjoining outlet fittings rather than a clean completed junction.** | `a3_07_roof.png`, `a3_35_roof_junction.png`, `a31_hall_collector.png` |
| 3. Pendant lights | **Resolved.** All 12 roses meet their actual ceiling underside. Each continuous stem stops inside its rose; no upper rod remains. Shades, fixture placements and approved studio lighting equipment preserved. | `a3_33_hall_ceiling.png`, `a31_pendant_audition.png`, `a31_pendant_support.png`; numerical checks for all 12 |
| 4. Contextual Hall | **Resolved.** Closed the measured gap between the raised band and adjacent lower ceiling, finished the soffit return and added local clerestory sill/end/outer reveals. Windows and Hall architecture retained. | `a3_33_hall_ceiling.png`; source ray probe identified the gap before correction |
| 5. Entrance rail | **Resolved.** Moved both foremost post/flange centres fully onto the first tread, clear of the next nosing; railing design retained. | `a3_15_entrance_close.png` |
| 6. Restrooms | **Resolved.** Both shared cisterns clear the seats and sit on a ceramic rear shelf. Added fixed hinge/jamb bridges, strike/keeper, interior privacy bolts and exterior indicators. Mirrors use a polished metallic material and one shared static 128px HDR cubemap. Both fixtures inspected. | `a3_23_restrooms.png`, `a3_24_toilet.png`, `a31_toilet_second.png`, `a31_privacy_hardware.png`, `a3_25_basins.png` |

The remaining two roof/drainage defects prevent a manual-freeze recommendation. A successful source route graph is not proof that each visible junction is finished.

## Verification and preservation

- Final Unity import and dynamic compile/execution succeeded; all material shaders supported, zero missing meshes/materials. Approved studio lamps still share their six original meshes.
- All nine original roof definitions, footprint and foundation/floor coordinates preserved. Terracotta materials retained.
- Exact packet comparison: **223 mesh groups unchanged**, no changed group outside the permitted six areas; all shared furniture placements and approved audition equipment identical.
- All **502 protected files unchanged**, including Studio/A1/A2 and approved equipment. No Studio.unity save, prototype replacement, commit or push.
- Finite mesh data and valid indices; zero near-zero-area triangles, window/downpipe projection conflicts or roof triangle-centre candidates inside the tested occupied volumes. These are scoped source checks, not exhaustive collision or hydraulic certification.
- **13 targeted views** updated/added and inspected. The other **28 A3 captures** remain labelled context. This was not another broad room-by-room QA milestone.
- Mirror verification required the regular isolated scene: preview-scene rendering displayed only sky reflections. The regular-scene renderer selected the assigned interior cubemap, and the final basin image shows reflected interior surfaces. No extra art correction was made for that capture limitation. The cubemap is approximate, not a realtime planar mirror.
- One corrective art pass addressed pipe/banner/canopy conflicts, roof-collector clearance, the measured soffit gap, and privacy hardware orientation. The final two remaining defects above were recorded without a second corrective pass.
- No Play Mode, gameplay tests, broad profiling, LOD generation, cutaway, navigation, construction integration or gameplay work.

## Final candidate and review

**1,848,785 triangles · 2,706,767 vertices · 320 MeshRenderers · 37 unique materials.** Includes existing shared furniture/equipment. Unity 6000.6.3f1, URP PC_RPAsset.

[Existing review gallery](review.html). The earlier A3 result remains in [completion_report.md](completion_report.md); its capture references now include the targeted A3.1 updates.

Source: `ArtSource/StageSchoolA3/known_defects.py`, targeted changes in `production_finish.py` and `generate_stage_school_a3.py`, immutable `a31_baseline.json`, `verify_a31.py`, README and small inspection probes. Regenerated `StageSchool_A3.blend`, generation report and existing mesh packet. Unity: existing `StageSchoolA1Review.cs`, A3 meshes/prefab/review scene, new `SS_Mirror` material and `WashroomReflection.exr` with metadata. Gallery builder, this report, targeted images and validation logs updated. [Source-changed group inventory](a31_source_qa.json).

Evidence logs: `a31_import_result.json`, `a31_capture_result.json`, `a31_mirror_check.json`, `a31_source_qa.json`, `a3_source_qa.json`, `a3_unity_sanity.json`.

**Known production-art defects remain: entrance pipe/pier interference and the Hall/Audition collector junction. LOD0 should not yet be frozen.**
