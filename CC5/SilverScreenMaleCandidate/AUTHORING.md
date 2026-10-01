# SilverScreen male CC5 candidate

Status: incomplete; Headshot trial prevents saving the reconstructed character. No usable or approved master has been produced. Blender round trip is prohibited until the user approves the six requested CC5 views.

This is a new native CC character created from an empty CC5 scene. `CC5/character1.ccProject` is rejected/test work and must remain untouched. Its initial SHA-256 is `ACD1AC540BF26A05C97133860C51D7F6C057546E803053E45F563EFA01911CB9`.

## References

- `References/FrontIdentity.png`: user's supplied front image, primary facial identity.
- `References/ProfileIdentity.png`: user's supplied side image, profile/depth.
- `References/BodyProportions.png`: user's supplied body image, anatomy/proportions.
- `References/SilverScreenConcept.png`: byte copy of the original concept at `ArtSource/Characters/SilverScreenAdult/Reference/ApprovedConcept.png`, authoritative style direction.

The attached face images include hair despite the request describing bald references. Their facial anatomy is used; generated hair is disabled. The scalp contour hidden by reference hair cannot be matched directly. No rejected character geometry, textures, or morph deltas are sources for this candidate.

## Authoring intent

Approximately 1.78 m, slim/fit male, moderate chest, lean waist, masculine pelvis, relaxed shoulders and expression. Keep CC native shaping, skin and eye materials, skeleton, and independent wardrobe/attachment systems. Basic boxer coverage only. No final hair, facial hair, wardrobe, Unity changes, commit, push, or Blender round trip.

Initial Headshot 3 inputs: front and profile images, body reconstruction from body reference with Male / Slim / Normal physique; Generate Hair off. Automatic reconstruction is a starting point, not visual approval.

Recovery note: SS_Male_01_HeadshotCheckpoint.ccProject failed visual reload verification (body not rendered). Do not treat it as a usable candidate. The first temporary RLPy session is stopped. Reconstruction is being repeated from the supplied images through the native Headshot UI, with native Square Face emphasis and the same Male / Slim / Normal body settings. No failed checkpoint geometry is used as a source.

The second reconstruction completed visibly. Native File > Save Project As displayed a Headshot 3 Trial restriction: saving is unavailable in the trial. No further scripted save/export attempt is authorized as a way around this restriction. All temporary author-session timers are stopped. The current reconstruction remains unsaved in CC5; do not close or replace it if intending to activate Headshot and continue from the preview.

The first checkpoint was written before the UI trial restriction was discovered; its failed reload is not proof of a specific SDK bug. Keep it as failed diagnostic work only. Do not reuse its geometry.

Continuation requires either an activated Headshot installation, or a fresh native CC5 base authored using available non-Headshot morphs. Native CC5 project saving is documented as available in the CC5 trial, but this alternative has not yet been validated locally. This is a workflow choice, not visual approval.

See ../../ArtReview/Characters/CC5MaleCandidate/authoring-status.md for evidence and remaining work.
