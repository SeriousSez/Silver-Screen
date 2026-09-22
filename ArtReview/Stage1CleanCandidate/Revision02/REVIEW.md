# Stage 1 — architectural revision 02

**Stopped for architectural review. Production detailing has not begun.**

[Open the concept/Unity comparison gallery](review.html). The four exterior views, Administration comparison and interior views were rendered in Unity and inspected on 2026-09-22. Revision 01 is retained in `Revision01/` for comparison.

## Architectural changes

| Area | Revision 02 |
|---|---|
| Arch and silhouette | Replaced the elliptical arch with a circular segment, reducing the roof height and giving the shoulders a continuous slope into the eaves. |
| Main portal | Reduced the leaf height and rebuilt the broad masonry surround, projecting face frame, inner rebate and restrained lintel ledge. |
| Front pilasters | Revised shaft width, projection, base, neck and stepped capitals to establish a clear hierarchy against the portal. |
| Personnel entrances | Smaller, offset openings and recessed surrounds; right entrance has a sloping rain hood. |
| Side mass and openings | Lower clerestories restore the blank wall band beneath the eave. The hero side has three openings; the opposite side has two unequal longer openings, as shown in the concept. |
| Bay rhythm | Shallow intermediate side piers are subordinate to the end pilasters. Interior roof frames establish a separate regular structural rhythm. |
| Roof/eave | Continuous eave course, shallower roof seat, revised arch coping and glazing ribbons following the new curvature. |
| Height against Administration | Roof crown reduced from 12.00 to 10.77 m; coping from 12.10 to 11.01 m. Administration's highest point remains approximately 11.02 m. Small vent caps reach 11.44 m. |
| Rear | Reproportioned central service opening and pitched hood, high ventilation allowances and matching end-pier/arch hierarchy. |

The front, front-three-quarter, opposite-side and rear captures use corresponding concept viewpoints and aspect ratios. They are approximate perspective matches, not a camera-calibrated reconstruction. The gallery displays original concept excerpts beside unaltered Unity captures. The approved illustration varies between its views, so the model resolves them as one coherent building.

The Administration comparison uses existing scene positions and both actual assets. Its tower and the revised Stage coping are now at a similar physical height. The concept gives the Administration tower greater apparent prominence; the current context view exposes that remaining difference for review rather than rearranging or modifying Administration.

## Major interior planning

- Five clear-span bowstring roof trusses with bottom ties, webs, perimeter columns and longitudinal purlins.
- A distinct orthogonal rigging grid suspended from four truss ties, ending before the rear circulation zone.
- Side catwalks carried by edge beams, column cantilevers and knee braces; rear crosswalk carried by service-edge supports.
- Two inline 14-riser stair flights with an intermediate landing, top landing and open turn onto the rear crosswalk. Plain guards show circulation boundaries.
- A central 12 × 18.95 m filming-floor reservation, without columns or stair supports within it.

The enclosed interior capture shows the space under the actual roof. Two explicitly labelled cutaways expose the roof/rigging/catwalk relationships and the stair connection; their enclosure meshes are hidden only during rendering.

| Planning measurement | Metres |
|---|---:|
| Exterior wall footprint | 15.8 × 24.0 |
| Clear interior between wall faces | 15.16 × 23.36 |
| Eaves / roof crown / coping crown | 7.10 / 10.77 / 11.01 |
| Portal opening / outer framing width | 7.2 × 6.9 / 9.16 |
| Catwalk deck / truss tie centre / rigging grid centre | 4.95 / 7.32 / 6.48 |
| Stair rise / going / flight width | 0.174 / 0.28 / 1.20 |
| Minimum sampled circulation headroom | 2.27 |
| Minimum sampled height over reserved filming floor | 6.325 |

These checks establish geometric feasibility at the planning stage. They do not validate structural loads, final connections, access regulations, walkable navigation, shutter travel or finished acoustic performance. Stair width is the current deck reservation; final rail/connection detailing may reduce clear width.

## Unity validation and preservation

- All eleven final Unity views inspected under the existing PC URP renderer, light, sky and volume. No added fill lights, AI renders, retouching or material brightening.
- Import and Editor command compilation passed. Zero missing scripts, meshes or materials; 20 renderers including the dynamic sign; 22,140 triangles including glyphs.
- Five review mesh colliders; central horizontal ray clear, floor hit at +0.08 m, no masonry blocking the three front apertures. Catwalk/stair walkable collision remains deferred.
- Stage 1 → 2 → 1 lettering check passed. Candidate is not registered for gameplay.
- Generator: 634 closed outward components; 19 export groups; 18,104 architectural triangles; zero degenerate faces. 161 circulation headroom samples and 40 clear-floor samples passed.
- Export FBX and Unity model copy have identical SHA256 hashes.
- Hash comparison found zero changes across 483 protected original files, including existing Stage/Administration assets and sources, original scenes/scripts, project settings and packages.
- Final Console reads before and after validation returned zero warnings/errors. An earlier capture attempted immediately after scene opening encountered URP initialization errors; rendering after initialization succeeded. No Console clearing was requested.

`source_validation.json`, `unity_validation.json` and `protected_files_validation.json` retain evidence. No Play Mode, navigation rebake, player build or gameplay integration was performed.

## Deferred work and review boundary

The palette and plain leaves remain study materials/forms. No final surface maps, UVs, door construction/hardware, roof seams, lamps, utilities, weathering, loose props or fine structural fittings were added. Fine shadow artifacts are visible along some narrow trim and door edges in the existing render setup; this is not a finished-render validation.

The source generator and editable blend are in `ArtSource/Stage1CleanCandidate/`. The revised FBX, prefab and isolated scene retain their existing candidate paths under `Assets/SilverScreen/Environment/Stage1CleanCandidate/`. The original Stage and Administration remain unchanged. Existing production anchors/camera layouts and navigation will still need an explicit integration pass after architectural approval; their prior integration findings are retained in `Revision01/REVIEW.md`.

**Awaiting architectural approval. No production detailing, live Stage replacement, commit or push.**
