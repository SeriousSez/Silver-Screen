# Canonical character — preserved technical foundation

## User decision, 22 September 2026

The Renderpeople-derived authored candidate is **accepted as the technical
foundation** for anatomy, topology, hands, garment construction and full-body
quality. **Keep it.** Its current photorealistic appearance fails the final
SilverScreen art direction. This is not visual approval or Gate 1 completion.

Do not return to the rejected MakeHuman/procedural body approach, replace this
source, perform another broad source search, or build multiple variants.
The next deliverable is ONE restrained stylization study of this same adult.
Do not add characters, expand the wardrobe, begin Phase 2, commit or push.

The weekly included usage allowance reported 100% consumed when checked after
this decision. No new stylization study was started. The integration below is
preserved for the next session.

## Authoritative files

- `ArtSource/Characters/Canonical/build_authored_candidate.py`: current reproducible recipe.
- `ArtSource/Characters/Canonical/Canonical_AuthoredCandidate.blend`: editable current source.
- `ArtSource/Characters/Canonical/generation_report.json`: measurements and source limitations.
- `ArtSource/Characters/ThirdParty/RenderpeopleEric/Original/`: unchanged publisher sources.
- `ArtSource/Characters/ThirdParty/RenderpeopleEric/source-manifest.json` and `PROVENANCE.md`: hashes, authorship and license.
- `Assets/SilverScreen/Art/Characters/Canonical/CanonicalAdult_VisualReview.prefab`: imported Unity candidate.
- `Assets/Editor/Characters/CanonicalCharacterReview.cs`: isolated Unity review/import utility.
- `ArtReview/Characters/Canonical/Captures/`: actual Unity URP review images.

The earlier `build_canonical.py` / `Canonical_Working.blend` experiment used
Blender CC0 anatomy with unsuccessful constructed clothing. It is an abandoned
source experiment, **not the accepted foundation**. The current recipe imports
only its generic Blender review-lighting helpers; it does not call its anatomy,
hair or garment construction functions. Separate those helpers if necessary,
without reviving that experiment.

## One controlled art-direction pass

Use the supplied SilverScreen concept as the visual target, especially the
suited actor and large portrait. The target has deliberately organized facial
planes, confident brow/eye shapes, appealing proportions, restrained surface
detail and readable cloth. It is stylized realism, without toon bands or outlines.

1. **Forms first.** Preserve authored topology, UVs and skin weights. Use one
   non-destructive sculpt/shape-key layer for the study. Explore a small head
   proportion adjustment (roughly 3–5% as a starting bound, not a requirement),
   clearer cheek/jaw planes and a simplified, intentional nose silhouette.
   Judge front, three-quarter and profile together. Keep ears and neck transitions credible.
2. **Eyes and expression.** Refine eyelid contours and brow rhythm, with only
   modest eye-aperture changes. Adjust globes, lids and rig landmarks together;
   enlarging eyeballs alone creates a stare or gaps. Retain natural iris scale,
   wet highlights and believable lid thickness. Avoid cartoon eye enlargement.
3. **Skin.** Author a less photographic surface treatment: reduce pore/stubble
   contrast and noisy normal detail while retaining broad warm/cool variation,
   lip definition and facial landmarks. Do not blur the entire shared atlas;
   preserve brows, lashes, garment seams and other important edges.
4. **Hair.** Organize the existing side-part into a few designed masses and
   softer highlights. Suppress the sharply photographic strand normal pattern;
   retain a convincing hairline and part rather than a smooth helmet.
5. **Clothing and hands.** Preserve the source's credible seams, collar, cuff,
   waistcoat and folds. Reduce weave noise and glossy photographic variation.
   Maintain the higher waist and fuller trouser-leg study; check its period
   silhouette against the supplied reference. Clarify hand planes/readability
   only if necessary; avoid exaggerated hands or a new wardrobe.
6. **Values in context.** Match Stage 1's restrained material treatment and
   overall contrast. Improve silhouette and face readability through forms and
   materials, not dramatic lighting, distance, depth of field or an outline shader.

Numbers above are conservative sculpting starting points, not validated settings.
Do not create a parameter sweep or multiple alternatives. Reuse the same adult,
pose and cameras for a direct before/after comparison. Retain the original
authored baseline and source textures so changes remain reversible.

## Review and validation state

Actual Unity captures exist for full-body front/side/three-quarter, head and
shoulders, facial three-quarter/profile/close-up, hands, Stage 1 exterior and
interior, management distance, and Live Camera wide/medium/close framing.
They are review evidence for the realistic foundation, not a stylization study.
Live shots use `ProductionCameraLayout.TryFrame`; production employee/playback
integration has not been changed or claimed complete.

The last successful reimport includes exact 1.75 m source height, feet origin,
separate eye material, a 45 mm higher waist adjustment and restrained lower-leg
ease. `unity-reimport.json` confirms success. The final recapture command was
interrupted, so **do not assume every existing PNG shows these last refinements**.
Recapture and inspect all requested views after the next actual art change.

The initial prefab import temporarily marked Studio dirty by instantiating then
removing an object. The importer now uses a separate preview scene. A diagnostic
copy of the active scene was byte-for-byte identical to saved Studio:
SHA256 `1ec4bb0925316abd4c02d82f374badb8c993482d9c9d37eff5463d169343e1d6`.
An attempt to call unavailable `EditorSceneManager.MarkSceneClean` failed only
in transient relay command compilation; it is not in repository code. Do not
discard any later user changes or blindly clear the dirty flag. See final
preservation evidence for the final Editor state. The isolated review was closed.
Clearing the flag through reflection was refused by the Unity relay's namespace
restriction; no reflection command executed. The safe follow-up only closed the
preview and compared scene bytes, leaving the flag untouched. Repository files
compile; do not mistake those transient command failures for C# project errors.

The source is a clothed scan with a shared atlas, not complete hidden anatomy
or a finished modular wardrobe. This limitation remains, but the user's choice
is to retain the successful authored foundation and establish its art direction
before doing further customization engineering.

No commit, push, added population, customization variants or Phase 2 work.
