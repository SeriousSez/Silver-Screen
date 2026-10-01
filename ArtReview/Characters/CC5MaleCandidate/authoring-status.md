# New SilverScreen male: incomplete authoring checkpoint

2026-09-23. No master has been approved, and no saveable, visually validated candidate has been delivered.

## Completed

- Preserved `CC5/character1.ccProject` as rejected/test work. SHA-256 remains `ACD1AC540BF26A05C97133860C51D7F6C057546E803053E45F563EFA01911CB9`.
- Copied the exact supplied front, profile, and body images to `CC5/SilverScreenMaleCandidate/References` and the original SilverScreen concept from `ArtSource/Characters/SilverScreenAdult/Reference/ApprovedConcept.png`.
- Started a new native CC5 scene and used the supplied images in Headshot 3. No rejected character geometry or previous character study was used as an input.
- Disabled generated hair; used basic boxer coverage. Reconstruction settings used Male / Slim / Normal body proportions. The second reconstruction used the Square Face emphasis.
- Verified that reconstruction produces a visible native CC3+ character in the current CC5 session.

## Blocking evidence

The running applications identify themselves as Character Creator 5 Trial and Headshot 3 Trial. Native File > Save Project As displayed:

> This feature is not available in the trial version.

The dialog requests upgrading Headshot to continue production. [Actual CC5 screenshot](headshot-trial-save-block.png).

[Reallusion's Headshot trial comparison](https://www.reallusion.com/character-creator/headshot/download.html) confirms that preview rendering is supported but saving CC projects, avatars, skins and exporting characters are excluded. [The separate CC5 trial comparison](https://www.reallusion.com/character-creator/download.html) lists project saving as available. Therefore a new, non-Headshot native-morph approach is a potential alternative; it has not yet been save/reload tested locally.

The earlier `SS_Male_01_HeadshotCheckpoint.ccProject` was created by the native SDK before this explicit restriction was discovered. Its body was not visible after reload. It is failed diagnostic work, not a recoverable master or a successful save. The cause of that visibility failure was not established. No attempt should be made to use the SDK to bypass the now-known trial restriction.

The temporary native authoring sessions are stopped. The second reconstructed preview is unsaved and remains open in CC5.

## Visual assessment so far

The supplied head images have hair, despite their description as bald. No hair object was generated, but the photo projection transferred a forelock trace and pronounced glabellar/brow shading onto the skin. Those artifacts must be removed before the character meets the bald, relaxed-neutral-face requirement. Geometry and clean skin need separate evaluation; the current screenshot is not approval evidence.

The initial front reconstruction also needed more angular jaw/chin structure and closer control of eye shape and nose definition to match the supplied references and concept. Square Face emphasis was tested in the second reconstruction, but has not been comprehensively assessed from all requested views.

Height of approximately 1.78 m, relaxed shoulder/body proportions, clean independent pigmentation controls, and the final six-view comparison have not been validated. No claim of completed identity matching or style approval is made.

## Next decision and validation gate

Continue with activated Headshot, or restart from the installed native Neutral_M CC base using available non-Headshot morphs. Verify a normal UI save/reload before further refinement. Then author the clean bald candidate, refine the face and physique, measure height, apply neutral CC5 lighting, and capture facial and full-body front/three-quarter/profile views.

Stop for the user's visual approval before any Blender round trip. No Unity content, final hair, clothing, shoes, or accessories have been authored. No commit or push was performed.

The prior GoB validation remains valid: Blender 4.5.14 LTS with CC/iC Tools 2.4.0 is the authoritative installation. See [pipeline validation report](../CC5Pipeline/validation-report.md); Blender 5.0 was not modified.
