# Stage School A2

Separate architectural candidate derived from refined A1. See design.md and ../../ArtReview/StageSchoolA1/completion_report.md for program, results and known defects. Final LOD0 approval pending.

From repository root, run Blender 5.0 with --background --factory-startup --python ArtSource/StageSchoolA2/generate_stage_school_a2.py. Then run verify_source.py with Blender's bundled Python. Generation reads A1 and shared kit/equipment libraries, saving A2 independently. Do not overwrite the preservation baseline during checking.

The existing SilverScreen/Art/Stage School A1 Editor menu now targets A2. Import, create/open the isolated review scene, allow rendering to initialize, then capture exteriors/interiors. Stable class name: StageSchoolA1Review. ArtReview/StageSchoolA1/a2_import.cs.txt records the executed import/reference check. Run ArtSource/StageSchoolA1/build_review.py after captures to regenerate the gallery. Unity metadata is retained across reimports.

Review output stays in the existing A1 review system. Do not promote to Studio, create LODs or begin Stage School B as part of regeneration.
