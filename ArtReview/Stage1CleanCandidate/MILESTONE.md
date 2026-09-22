# Stage 1 revision 04 — approved visual master

The user visually approved Stage 1 revision 04 as the production-quality visual master on **2026-09-22**, including the final finial clearance and gutter/eave corrections. This milestone checkpoints that approved work before any reusable-asset audit. No audit, optimization, prop extraction or gameplay migration is part of this commit.

## Included

- Complete independent Stage 1 source: both approved concepts, editable Blender master, generators, nine PBR texture sets, generation/material/defect reports, and identical export/Unity FBX copies.
- Unity candidate model, prefab, isolated review scene, 20 materials, textures/reflection, all associated metadata/GUIDs, and the import/review Editor companion.
- Instance-driven stage identity/sign scripts, glyph meshes, glyph source and import companion required by the master.
- Revision 01–04 review history, concept comparisons, final finial/gutter before/after captures and validation records. Focused source/Unity validation and capture scripts are retained under `ArtSource/Stage1CleanCandidate/Validation`.
- Applicable root/ArtSource/Environment instructions, project context, daylight documentation and this approval/scope record.
- Only referenced pre-existing visual context dependencies: Administration and fallback Stage imported models/materials/textures, environmental decal assets, daylight profile/sky and the PC URP settings used for the approved review. These are included so the saved review does not reopen with missing models, materials or different rendering settings. Their working contents were preserved, not redesigned. Unreferenced reference-kit work remains outside this milestone.

The complete exact-file allowlist and every deliberately excluded initial working-tree entry, with its reason, are in [commit_scope.json](commit_scope.json). The index was empty before staging.

## Preserved outside the commit

The live `Assets/Scenes/Studio.unity`, pre-existing `StudioWorldRouter.cs` changes, TextMesh Pro fallback font state, decade music additions, unused reference-kit/Administration sources and exports, general art-direction documents, unused context models/navigation/reflection, Python cache, and the local Studio recovery snapshot remain uncommitted. The recovery snapshot is intentionally excluded even though it sits beneath the candidate directory: it contains user scene state rather than the approved isolated review.

Initial Git status also flagged hundreds of files with no substantive diff because of index/stat or line-ending state. They were not staged or rewritten. No unrelated working file was reverted, normalized or deleted. The user scene was not saved or reloaded for this commit.

## Approval evidence and limits

[Current gutter/eave gallery](GutterClearance/review.html) and [validation](GutterClearance/REVIEW.md) capture the final master. The [finial gallery](FinialClearance/review.html) and [revision 04 review](REVIEW.md) retain earlier, explicitly labelled snapshots. Approval-pending/no-commit language in historical reports describes their state when captured; this approval record supersedes that workflow status without rewriting the evidence.

Latest recorded validation: 6,967 closed/outward source components, 668,668 triangles, 29 semantic export meshes, no degenerate faces; all finial/gutter clearance and drainage-path checks pass. The last Unity validation recorded zero errors and one UI font warning. This commit operation checked file/dependency integrity and preservation; it did not rerun visual approval or a player build.

Production gameplay/router registration, navigation, animated doors/blackouts, stair/catwalk traversal, LODs and many-building performance remain unvalidated/unintegrated. The approved visual master is retained under its stable candidate asset paths; approval does not imply those later integration tasks are complete.

Commit message: `feat(environment): rebuild Stage 1 production asset`. No push is authorized or performed. The reusable-asset audit has not begun.
