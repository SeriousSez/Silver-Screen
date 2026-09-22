# Stage 1 visual-master validation reproduction

These are the focused scripts used for revision 04 and its finial/gutter follow-ups, retained from the local validation workspace. They complement the generator's built-in assertions and the Unity review companion. Their historical results are in `ArtReview/Stage1CleanCandidate`; copying these scripts into source control does not imply that validation was rerun during the milestone commit.

## Blender

From the repository root, run `validate_source.py` through Blender's `--background --factory-startup --python` option. It opens the current editable blend, checks closed/outward meshes and matching export copies, and updates `ArtReview/Stage1CleanCandidate/source_validation.json`.

For the focused geometry reports, open `ArtSource/Stage1CleanCandidate/Stage1_CleanCandidate_A.blend` in background Blender and run `finial-audit.py` or `gutter-audit.py` with `--python`, followed by `-- <output-json-path>`. The gutter report includes the intended thin apron/roof weather lap; it is not an assertion that every contact is a defect. Authoritative clearance and flow assertions also run during normal source generation.

## Unity

The `.cs` files are command snippets for the installed Unity MCP provider's `IRunCommand` / `ExecutionResult` interface, not scripts to place under `Assets`. Run them sequentially with the project's existing MCP command runner in Edit Mode:

1. Open the isolated preview with `SilverScreen.Editor.Stage1CandidateReview.OpenPreviewReview()` and allow scene initialization.
2. Run `revision04-validate.cs` for imported mesh/material/collider/sign/reflection checks.
3. Run `finial-after.cs` and/or `gutter-after.cs` for the fixed close-up and overview camera sets.
4. Run `revision03-cleanup.cs` to close only the temporary preview.

The capture commands overwrite the respective `after_*.png` review images. Preserve historical galleries before recapturing. The final gutter command includes the wider full-height downpipe cameras. The earlier finial gallery predates the gutter correction; reproducing its cameras now shows the current master.

The standard 47-view revision 04 set is reproduced through menu **SilverScreen > Art > Stage 1 Candidate > 6 Capture final correction review**. Never save or reload the user's Studio scene to reproduce review evidence. These asset checks do not validate gameplay, navigation, a player build, hydraulic capacity or many-building performance. Historical command log wording should be read with the dated review reports, which record the actual Editor state.
