# SilverScreen animation library audit

Audit date: 2026-09-28. Source: `ExternalSourceAssets/Animations`. This is an offline archive inventory and planning catalogue. No source files were changed, imported, renamed, moved or deleted. No Unity, tests, project compilation, runtime assets, gameplay edits, commits or pushes were used.

## Result and counting rules

The downloaded collection already provides broad raw animation vocabulary for studio life and filmmaking, but quantity exceeds readiness. Conversation, expressive seated acting, locomotion, environmental actions and film genres are well represented. Quiet listening, ordinary object workflows, restrained paired social behavior and period-specific office/meal continuity are the priority gaps. A defensible percentage of the complete future vocabulary cannot be calculated without a defined target vocabulary and visual/contact validation.

| Measure | Count / result |
| --- | --- |
| All source files scanned | 1381 |
| Animation-capable FBX files | 1356 |
| Other files | 6 PDF + 1 TXT + 18 Unreal IK/retarget support assets |
| Animation stacks | 1356 |
| Estimated distinct whole-source-take groups | 883 |
| Estimated distinct skeletal motion groups | 877 |
| Distinct facial-only animation files | 6 (51 blendshape channels each) |
| Byte-identical FBX groups / redundant FBX copies | 10 / 53 |
| All-file byte-identical groups / redundant copies | 14 / 73 |
| Different-file exact local-curve groups | 66 |
| Accepted cross-export evidence links | 346 |
| Additional reduction beyond byte duplicates | 420 |
| Individual-vs-pack motion groups | 1 |
| Normalized skeleton hierarchies | 9 |
| Corrupt/unreadable FBX | 0 in this parser; not a full SDK/import validation |
| First future runtime test set | 34 |

A “unique motion” here means an estimated whole-take equivalence group. It is **not** a count of isolated reusable actions. Of the 883 groups, **873 are moving skeletal performances, four are static sitting poses, and six are facial-only recordings**. Export evidence is strong but not evaluated cross-rig pose proof. Unknown trimmed overlaps stay separate. Compound recordings can contain several actions whose frame boundaries are not known; they are listed as candidates without increasing the unique count. Duplicate categories overlap, so their group sizes must not be added. The arithmetic is 1,356 - 53 byte copies - 420 additional equivalent/variant copies = 883 groups.

| Priority | Estimated motion groups |
| --- | --- |
| ESSENTIAL | 230 |
| USEFUL | 76 |
| SPECIALIZED | 505 |
| LOW PRIORITY | 72 |

Priorities and tags are filename/folder-based editorial proposals. SPECIALIZED preserves film-genre value; LOW PRIORITY includes opaque names and facial diagnostics, not permission to discard them. Every group remains represented in the JSON.

## Method and reliability

- SHA-256 covers every complete source file. Size and last-write nanoseconds are recorded; a final metadata check found the same paths, sizes and last-write times. Access times may be filesystem-managed; they were not rewritten.
- A read-only binary-FBX parser follows Objects, Connections, animation stacks/layers/nodes and compressed key arrays. All 1,356 files parsed. Geometry payloads are skipped; skeleton hierarchy, export settings and static transforms are retained. This is not a full FBX SDK scene evaluator.
- Metadata includes FBX version, creator, skeleton hash/bones, stack name, declared FPS, inclusive nominal frame count, actual distinct key-time count, duration, keyed/varying channels, finger-curve variation and local root/hip translations. JSON null/unknown values are intentional.
- Frame count is round(duration × declared FPS) + 1. Declared FPS is not necessarily the rate of baked keys. Facial-only duration comes from deformation-curve key bounds when stack bounds are absent.
- Exact curve fingerprint: normalized bone/channel labels, time relative to stack start, float curve values and interpolation attributes; grouping also requires hierarchy and duration. Nine such groups have different static transform sets, so common local motion data does not prove identical evaluated poses.
- Secondary fingerprint: 121 linearly interpolated samples per local channel, translation origin removed, values quantized to 0.001. It is a search tool, not FBX cubic/pivot/pre-rotation evaluation. No additional groups were accepted solely by this fingerprint.
- Export grouping requires matching source-family action identity, duration within 0.04 seconds and sampled pelvis-speed correlation >0.98 with normalized speed RMSE <0.02. Labels alone are never enough. This validates common performance timing/trajectory, not retarget quality.
- Finger presence means varying named finger curves; it does not establish intentional finger capture. Local hip excursion can include body sway, and parent rotations can change axes. In-place/translating fields are candidates; no runtime root-motion extraction is claimed.
- Five named full/segment pairs received a targeted raw-curve containment check; none met the strict contiguous-sample test. They remain unresolved partial-overlap candidates, separately listed in the duplicate JSON. Arbitrary time-warped, resampled, trimmed, or different-rig duplicates outside identified export families may remain.
- No animation was visually inspected. Handedness, contacts, loopability, posture where unnamed and adaptation limits remain unverified. No network license research was performed.

## Actual Mixamo pack structures

Every listed pack is **C: a directory of separate FBX files**. Each FBX has exactly one stack. The observed archive therefore contains neither an A-style multi-stack pack file nor a B-style concatenated whole-pack file. A single action file can still be a compound performance; stack count alone does not prove atomicity. Each file/stack is catalogued individually with its pack provenance.

| Pack folder | FBX files | Stacks | Motion groups within folder |
| --- | --- | --- | --- |
| Basic Locomotion Pack | 7 | 7 | 7 |
| Basic Shooter Pack | 16 | 16 | 16 |
| Breakdance Pack | 34 | 34 | 34 |
| Farming Pack | 25 | 25 | 25 |
| Female Basic Locomotion Pack | 12 | 12 | 12 |
| Female Locomotion Pack | 10 | 10 | 10 |
| Gestures Pack Basic | 15 | 15 | 15 |
| Gestures Pack Basic (1) | 15 | 15 | 15 |
| Lite Rifle Pack | 14 | 14 | 14 |
| Locomotion Pack | 12 | 12 | 12 |
| Male Drunk Pack | 12 | 12 | 12 |
| Male Injured Pack | 20 | 20 | 20 |
| Male Locomotion Pack | 10 | 10 | 10 |
| Not So Scary Zombie Pack | 24 | 24 | 24 |
| Rifle 8-Way Locomotion Pack | 49 | 49 | 49 |
| Scary Zombie Pack | 12 | 12 | 12 |
| Shooter Pack | 15 | 15 | 15 |
| Soccer Game Pack | 54 | 54 | 54 |

Per-folder motion counts overlap with other folders. `Gestures Pack Basic` and `Gestures Pack Basic (1)` contain 15 matching local-curve motions across two downloads, although the corresponding files are not byte-identical. Basic/expanded locomotion, male/female and shooter/rifle packs also have some exact curve overlaps; use the duplicate JSON rather than summing this table.

## Other source packs and export variants

| Pack | Physical FBX files | Estimated source motions | Exports |
| --- | --- | --- | --- |
| Mocap_Pack_01_Everyday_Actions | 51 | 17 | iClone / Rokoko Default / UE5 |
| Mocap_Pack_02_Cleaning | 39 | 13 | iClone / Rokoko Default / UE5 |
| Mocap_Pack_03_Talking_and_Interacting | 51 | 17 | iClone / Rokoko Default / UE5 |
| Mocap_Pack_04_Crowd_Reactions | 66 | 22 | iClone / Rokoko Default / UE5 |
| Mocap_Pack_05_Soccer | 63 | 21 | iClone / Rokoko Default / UE5 |
| Mocap_Pack_06_Fitness_and_Workout | 66 | 22 | iClone / Rokoko Default / UE5 |

| Export variant | Physical FBX files |
| --- | --- |
| Mixamo | 583 |
| iClone | 112 |
| Rokoko Default | 112 |
| Unreal UE5 | 112 |
| HumanIK | 319 |
| Unreal | 112 |
| unknown | 6 |

The six Anderson Rohr packs contain 112 underlying actions in 336 files. The larger Rokoko archive includes modern Mixamo, HumanIK and Unreal exports, legacy HumanIK recordings, repeated combat exports inside unrelated Unreal folders, and six facial-only FBX files. No BVH, GLB, GLTF or compressed archives were present. The 18 `.uasset` files are named IK_Source, IK_Target and RTG_RKK_to_MH; they are catalogued as Unreal support assets, not counted as independent motions. Their binary internals were not decoded.

**Proposed Unity Humanoid preference:** start with Mixamo where the same motion has that export, then HumanIK. For the six packs without either, use Rokoko Default for the first test. This is a practical choice based on recognizable biped hierarchy and fewer engine-specific helper-bone assumptions, not a verified importer result. Keep iClone and Unreal variants for comparison/fallback, particularly if contact or shoulder orientation differs. Never choose an export by filename alone when the character test contradicts it.

| Skeleton hash prefix | Bones | Example physical file |
| --- | --- | --- |
| 16b0d47f549c | 65 | Agreeing (1).fbx |
| 2588ca74829e | 66 | Mocap/Crouch_Walk.fbx |
| cf6cc78f2a0b | 76 | Mocap/Mocap_Pack_01_Everyday_Actions/mocap/Rokoko_Default/01_Open_Curtain_v1.fbx |
| 7fa6034ff0c4 | 80 | Mocap/Mocap_Pack_01_Everyday_Actions/mocap/UE5/01_Open_Curtain_v1_ue5.fbx |
| e615cb25e5a1 | 76 | Mocap/Mocap_Pack_03_Talking_and_Interacting/mocap/Rokoko_Default/01_Standing_Looking_Around.fbx |
| d0d971472af2 | 75 | Mocap/Rokoko_Free_Mocap_FBX_263/Rokoko Studio (Mocap)/RokokoTVContest_MocapAssets/Breaking4thWall_Docu/HumanIK/Documentary_PsstToCamera2_hik.fbx |
| 184c8f550deb | 61 | Mocap/Rokoko_Free_Mocap_FBX_263/Rokoko Studio (Mocap)/RokokoTVContest_MocapAssets/Breaking4thWall_Docu/Unreal/Documentary_PsstToCamera2_ue.fbx |
| 5c43ad287fd7 | 53 | Mocap/Rokoko_Free_Mocap_FBX_263/Rokoko Studio Legacy Mocap (older)/Combat/Boxing_GettingKnockedOut_HUMANIK_WHS.fbx |
| de065c135878 | 1 | Mocap/Rokoko_Free_Mocap_FBX_263/Rokoko Studio Legacy Mocap (older)/Movement/Junkie_FloatinginSpace_face_P_jgbZmX.fbx |

## Duplicates and variation

Category A has 53 redundant FBX copies across 10 groups. Most are repeated Unreal combat exports in TV-category folders; ShadowBoxing and AirGuitar also have byte copies. The six identical PDF guides and repeated support assets contribute another 20 redundant non-animation files. Nothing was deleted.

Category B has 66 exact local-curve groups plus 346 accepted cross-export links. Links form groups and are not independent motion counts. Static bind/default-transform differences remain inspectable. Category C has one confirmed individual-vs-pack family: **Angry Gesture (1).fbx**, **Gestures Pack Basic/angry gesture.fbx**, and **Gestures Pack Basic (1)/angry gesture.fbx**. No claim is made that these are the only possible approximate/trimmed overlaps.

Category D is retained. Agreeing and Agreeing (1), the Talking performances, and numbered seated/emotional downloads are not collapsed by name. The duplicate JSON contains same-name families with multiple surviving motion IDs, as well as full source-path lookups for every evidence group.

## SilverScreen coverage

Ratings describe available candidate vocabulary. STRONG does not mean retargeted, licensed, loopable or production-ready. Evidence counts in JSON are overlapping tag matches, so contextual notes take precedence over raw counts.

| Area | Rating | Assessment |
| --- | --- | --- |
| Basic locomotion | STRONG | Multiple walk/run/strafe/turn packs; loopability and starts/stops not yet validated. |
| Locomotion variation | STRONG | Male/female, injured, drunk, backward and crouched variants; paired export copies excluded. |
| Standing idles | STRONG | Basic idle plus Regular light/medium/heavy and other idle performances. Many genre idles; not all are neutral. |
| Seated idles | ADEQUATE | Seated Idle, sitting idle recordings and sitting poses; still need neutral listening and loop/contact review. |
| Standing conversation | STRONG | Talking variants, explicit standing conversations/arguments, and staged courtroom/TV performances. Unspecified posture stays unknown. |
| Seated conversation | STRONG | Sitting Talking variants and chair/sofa/coffee conversation recordings; good expressive range. |
| Listening/reactions | WEAK | Agreement and strong reactions exist; no clearly named dedicated neutral listening clip identified. |
| Gestures | STRONG | Point, nod, disagree, dismiss, beckon, clap and gesture packs; two Gestures folder downloads repeat motion data. |
| Emotional acting | STRONG | Anger, sadness, crying, laughter, disbelief, fear, disappointment; subtle and transitional acting remains unproven. |
| Social interactions | ADEQUATE | Greetings, bows, salutes and handshakes; sparse contact-rich paired behavior. |
| Office/work | ADEQUATE | Writing and computer administration candidates; no clearly named reading performance found. Continuity and reusable loops need checking. |
| Telephone | ADEQUATE | Talking On Phone and pacing cover ordinary calls; many other phone assets depict modern video/texting and need era filtering. |
| Eating | WEAK | SomethingBehindMe_Eating is a staged sequence; no clear general meal/utensil/bite loop set. |
| Drinking | ADEQUATE | Drinking and seated coffee/cup performances; no confirmed reusable sip/hold/put-down decomposition. |
| Smoking | WEAK | One clearly named leaning/smoking recording; seated smoking and start/end gestures not established. |
| Object handling | WEAK | Taking Item, dishes, plate/table sequence and a sports pickup; lacks a clearly reusable generic pickup/place/handoff suite. |
| Doors/environment interactions | STRONG | Door opening/closing, curtains and windows; actual handle heights and handedness unmeasured. |
| Sitting/standing transitions | ADEQUATE | Explicit transitions plus chair/sofa recordings; chair types, approach directions and phase boundaries need validation. |
| Carrying | WEAK | One explicitly named general Carrying motion; no established box/tray/one-hand/two-hand weight variants. |
| Cleaning | STRONG | 13 source actions include sweeping, vacuuming, dusting, glass cleaning and dishes; broad reusable work coverage. |
| Crowd/background behavior | STRONG | 22 Crowd Reactions actions plus idles, clapping, cheering, looking and phone behaviors; quiet period-appropriate extras need curation. |
| Romance/affection | WEAK | Flirty performance exists; no clearly named hug/kiss/hand-holding partner set identified. |
| Comfort | WEAK | CradlingBaby exists; this does not cover comforting an adult, consoling, or a shoulder touch. |
| Fighting | STRONG | Broad combat, boxing, swords and staged multi-person performances. |
| Firearms | STRONG | Shooter/rifle packs, pistol/rifle variations and genre performances; historical weapon compatibility unverified. |
| Injury | STRONG | Injured locomotion, hit/shot reactions and wounded performances. |
| Falls/deaths | ADEQUATE | Falls, knockouts, dying and zombie deaths exist; generic direction/height coverage not established. |
| Dancing | STRONG | Breakdance plus Rokoko dance recordings; 1930s social/partner dances are a separate gap. |
| Sports | STRONG | Soccer, fitness, cricket and other sports; large breadth rather than immediate studio-life priority. |
| Horror/zombies | STRONG | Two Mixamo zombie packs and Rokoko horror/mummy/zombie recordings. |
| Fantasy/superhero | STRONG | Magic, superhero and franchise-named performances; preserve descriptive provenance and verify rights separately. |
| Stunts | ADEQUATE | Climbing, jumps, diving, circus and crashes; no promise of full traversal/contact coverage. |

No broad requested area is wholly MISSING by filename evidence; several essential **subareas** are missing or unestablished. One staged action does not make an area adequate.

## Seated performance assessment

| Scenario | Assessment | Evidence and gap |
| --- | --- | --- |
| Office conversations | ADEQUATE | Seated Idle, Sitting Talking, seated point/anger/disapproval/laugh/yell and writing/type candidates. Need quiet listening, paper handoff and desk continuity. |
| Meetings | ADEQUATE | Meeting-named files, questioning, agreement and seated gestures; Asking Question and Having A Meeting posture not established by names alone. Need neutral attentive participants. |
| Restaurant conversations | ADEQUATE for talk; WEAK for meals | Sitting Coffee Conversation and drinking candidates. Missing a dependable full meal/utensil sequence and subtle listener coverage. |
| Interviews | WEAK | Talking and reactions are usable candidates, but restrained listening, turn-taking and interviewer notes are not established. |
| Interrogations | ADEQUATE as staged material | GoodCop/BadCop/Suspect named recordings plus anger/yell/disbelief; seated posture and isolated phases require review. |
| Romantic scenes | WEAK | General conversation/laughter and Flirty exist; intimate seated listening, partner contact and affection are not established. |
| Waiting | ADEQUATE | Seated idles and sitting poses exist; long natural loops and bored/nervous micro-variation need review. |
| Emotional seated reactions | STRONG | Angry, disapproval, disbelief, laughing, yell, pointing and clap variants. Quiet disappointment, attentive listening and soft emotion transitions remain gaps. |

The intentional seated-download list is substantially represented. **Writing, Drinking, Beckoning, Asking Question and generic Clapping are not automatically marked seated**: without visual evidence that would invent posture. Explicitly sitting/seated files receive that tag; compound stand/sit records also need phase review. Female Sitting Pose and the three Male Sitting Pose files contain no varying skeletal curves: they are static pose references, not moving idle coverage. Seated Idle and the two legacy Sitting_Idle recordings are moving candidates.

Explicitly named seated candidates: `Female Sitting Pose`, `Male Sitting Pose (1)`, `Male Sitting Pose (2)`, `Male Sitting Pose`, `10_Sitting_Chair_Conversation_Gesturing`, `11_Sitting_Coffee_Conversation`, `12_Sitting_Sofa_Conversation_Look_Laugh`, `13_Sitting_Sofa_Conversation_Agree`, `14_Sitting_Sofa_Stand_Wave_And_Walk`, `15_Sitting_Sofa_Video_Call_Phone`, `01_Sitting_Clapping`, `13_Sitting_Phone_Video`, `19_Sitting_Texting_And_Video`, `19_Seated_Row`, `SitDown_UseLaptop_mixamo`, `SittingTalking_Gossip_mixamo`, `Destiny_SittingPlayingCards_HUMANIK_769`, `Sitting_Idle01_HUMANIK_769`, `Sitting_Idle02_HUMANIK_769`, `Seated Idle`, `Sit To Stand`, `Sit To Type`, `Sitting And Pointing`, `Sitting Angry`, `Sitting Clap (1)`, `Sitting Clap (2)`, `Sitting Clap`, `Sitting Disapproval`, `Sitting Disbelief`, `Sitting Laughing`, `Sitting Rubbing Arm`, `Sitting Talking (1)`, `Sitting Talking`, `Sitting Yell`, `Sitting`.

## Interaction assessment

92 motion groups have interaction-planning metadata. Each contains expected props, possible IK targets, root-motion relevance, handedness uncertainty and size-adaptation limits. These are planning suggestions, not measured contact frames. Generic Taking Item and Carrying filenames do not establish one- or two-handed use.

| Motion family | Expected prop / IK targets | Important limitation |
| --- | --- | --- |
| Taking Item / pickup / placement | Item grip; hand(s), placement surface, feet | Generic pickup evidence is sparse; a cricket pickup does not replace ordinary handling. |
| Carrying | Object grip(s), carry anchor | One generic candidate; weight, object size and hand use unknown. |
| Door / curtain / window | Handle/latch/frame/curtain edge; active hand and feet | Hinge direction and approach root motion matter; bilateral coverage unknown. |
| Sit / stand / chair | Seat/pelvis, floor/feet, optional armrests | Seat height, approach and contact windows require character testing. |
| Writing / typing | Pen/page or keyboard; wrist(s), desk, seat | One hand likely active for writing; two for typing. Planning inference only. |
| Drinking / telephone | Cup-to-mouth or receiver-to-head; hand grip | Prop shape/scale and grip articulation unknown; avoid treating video calls as 1930s telephones. |
| Cleaning | Broom, cloth, brush, vacuum or squeegee; tool grip and work surface | Good varied motion sources; tool geometry and environmental contacts remain unmeasured. |

For useful interaction records, small anchor offsets are plausible IK candidates; no reliable maximum object-size adaptation can be inferred from the archive. Root/hip curves are measured, but maintaining feet, seat and grip contacts must be validated together later.

### Compound actions retained without splitting

| Source motion | Individually catalogued action candidates | Boundaries |
| --- | --- | --- |
| 03_Open_Curtain_and_Window | Interaction.Curtain.Open, Interaction.Window.Open | unknown; no files split |
| 07_Close_Window_and_Curtain | Interaction.Window.Close, Interaction.Curtain.Close | unknown; no files split |
| 09_Open_and_Close_Door | Interaction.Door.Open, Interaction.Door.Close | unknown; no files split |
| 17_Walk_TakePlate_SetTable_Sit | Locomotion.Walk, Interaction.Item.TakePlate, Interaction.Item.SetTable, Transition.StandToSit | unknown; no files split |
| 04_Stand_Point_And_Walk | Gesture.Point.Standing, Locomotion.Walk | unknown; no files split |
| 05_Stand_Agree_And_Walk | Gesture.Agree.Standing, Locomotion.Walk | unknown; no files split |
| 06_Walk_Stop_Point_And_Continue | Locomotion.Walk, Gesture.Point.Standing, Locomotion.Walk | unknown; no files split |
| 07_Walk_Stop_Agree_And_Continue | Locomotion.Walk, Gesture.Agree.Standing, Locomotion.Walk | unknown; no files split |
| 14_Sitting_Sofa_Stand_Wave_And_Walk | Idle.Seated, Transition.SitToStand, Social.Greeting, Locomotion.Walk | unknown; no files split |

These semantic subrecords retain parent motion/take IDs. They are not additional verified clips and are excluded from the unique count. Other staged/compound takes retain filename hints rather than invented segment boundaries.

## Prioritized gaps

### MUST HAVE FOR BASIC SILVERSCREEN

1. Quiet standing and seated listening loops, with small nods, attention shifts and neutral-to-react transitions.
2. Generic object pickup, hold, handoff and put-down at desk/shelf/floor heights; left/right and one/two-hand variants.
3. Reusable carrying: small item, papers, tray, box; locomotion starts/stops and turns without losing grip.
4. Conversation continuity: neutral entry/exit, interruption, listening-to-speaking transitions, restrained seated disagreement and interest.
5. Period office essentials: paper handling/reading, writing and typewriter use, desk telephone pickup/listen/hang-up; modern screen use does not substitute.
6. Basic seated meal behavior: utensil/food pickup, bite, chew/pause and put-down; drinking cup hold/sip/return phases.
7. Adult comfort and basic affection: shoulder touch, reassuring hand, hug; paired spatial relationships need an intentional set.

### NICE TO HAVE

1. Chair transitions for armchairs, sofas and dining chairs; different approach directions and seat heights.
2. Subtle seated reactions: skeptical, bored, attentive, nervous, amused, disappointed; quiet restaurant/date/interview listening.
3. Door/prop variations by hand, hinge side, push/pull direction and object height; current assets may adapt but coverage is unverified.
4. Period background extras: queueing, waiting, idle conversation pairs, newspaper reading and seated smoking.
5. Film-crew specifics: camera/tripod handling, boom pole, slate, lights, cables and directing gestures.
6. Partner romance and adult comfort, calm exits/returns, continuity between emotional intensities.

### GENRE-SPECIFIC / LATER

1. 1930s social/partner dancing and musical staging; plentiful breakdance does not fill this gap.
2. Period-appropriate weapon handling, recoil/reload and paired stunt reactions; validate existing compatibility first.
3. Matched fight choreography, falls by direction/height, climbing/traversal contacts and safety-oriented staging variants.
4. Additional sports, magic and monster variations only after reusable everyday behavior is covered.

## First future runtime test set (34 distinct motions)

Selection only: nothing was copied into Assets. Paths below are relative to the immutable source root. The named stack is the precise source take, always index 0 here. All are different estimated motion groups. Semantic IDs are proposals; `Unknown` posture is an explicit review requirement. Anderson Rohr selections use Rokoko Default; other selections use their available Mixamo export.

| # | Source file | Vendor / pack | Stack | Skeleton | Proposed semantic ID | Why |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Basic Locomotion Pack/walking.fbx | Mixamo / Basic Locomotion Pack | mixamo.com | Mixamo | Locomotion.Walk.12 | Neutral walk and foot-contact baseline |
| 2 | Locomotion Pack/running.fbx | Mixamo / Locomotion Pack | mixamo.com | Mixamo | Locomotion.Run.10 | Faster gait and displacement contrast |
| 3 | Basic Locomotion Pack/idle.fbx | Mixamo / Basic Locomotion Pack | mixamo.com | Mixamo | Idle.Unknown.Neutral.08 | Neutral standing baseline and potential loop seam |
| 4 | Basic Locomotion Pack/left turn 90.fbx | Mixamo / Basic Locomotion Pack | mixamo.com | Mixamo | Locomotion.Turn.09 | Facing change and root-yaw handling |
| 5 | Seated Idle.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Idle.Seated.Neutral.03 | Chair height and seated baseline |
| 6 | Talking.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Conversation.Unknown.Neutral.22 | General conversation; confirm posture before assigning a standing ID |
| 7 | Sitting Talking.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Conversation.Seated.Neutral.06 | Explicitly seated conversation |
| 8 | Standing Arguing.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Conversation.Standing.Argue.02 | Stronger standing dialogue performance |
| 9 | Sitting And Pointing.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Gesture.Point.Seated.01 | Seated gesture and directional intent |
| 10 | Sitting Angry.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Emotion.Angry.Seated.01 | Seated emotional intensity |
| 11 | Sitting Disapproval.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Gesture.Disagree.Seated.01 | Negative seated reaction |
| 12 | Sitting Laughing.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Emotion.Laughing.Seated.01 | Positive seated emotional reaction |
| 13 | Asking Question.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Conversation.Unknown.Question.01 | Question gesture; seated applicability still needs inspection |
| 14 | Agreeing.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Gesture.Agree.Unknown.02 | Conversational agreement; preserve distinct numbered variant |
| 15 | Beckoning.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Gesture.Beckon.Unknown.01 | Inviting gesture; hand and posture review |
| 16 | Stand To Sit.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Transition.StandToSit.05 | Seat approach, pelvis placement and entry |
| 17 | Sit To Stand.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Transition.SitToStand.02 | Seat exit and foot planting |
| 18 | Taking Item.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Interaction.TakeItem.02 | Generic item reach/grasp; handedness unknown |
| 19 | Carrying.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Work.Carry.01 | Sustained carried-prop pose and root behavior |
| 20 | Opening Door Inwards.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Interaction.DoorOpen.04 | Door handle, swing direction and body clearance |
| 21 | Mocap/Mocap_Pack_01_Everyday_Actions/mocap/Rokoko_Default/10_Close_Door.fbx | Anderson Rohr / Mocap_Pack_01_Everyday_Actions | clip | Rokoko Default | Interaction.DoorClose.01 | Door closing and alternate export hierarchy |
| 22 | Writing.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Work.Write.01 | Pen, desk and wrist contact; posture unknown |
| 23 | Drinking.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | DailyLife.Drink.01 | Cup-to-mouth coordination; posture unknown |
| 24 | Talking On Phone.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Work.Telephone.12 | Receiver-to-hand/head alignment |
| 25 | Sit To Type.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Work.Type.04 | Chair-to-keyboard action and two-hand contact planning |
| 26 | Mocap/Mocap_Pack_02_Cleaning/mocap/Rokoko_Default/12_Sweeping_01.fbx | Anderson Rohr / Mocap_Pack_02_Cleaning | clip | Rokoko Default | Work.Clean.10 | Reusable work loop candidate and tool contact |
| 27 | Mocap/Mocap_Pack_01_Everyday_Actions/mocap/Rokoko_Default/01_Open_Curtain_v1.fbx | Anderson Rohr / Mocap_Pack_01_Everyday_Actions | clip | Rokoko Default | None | Flexible environmental reach interaction |
| 28 | Mocap/Mocap_Pack_03_Talking_and_Interacting/mocap/Rokoko_Default/11_Sitting_Coffee_Conversation.fbx | Anderson Rohr / Mocap_Pack_03_Talking_and_Interacting | clip | Rokoko Default | DailyLife.Drink.02 | Combined seated dialogue and prop handling |
| 29 | Mocap/Mocap_Pack_03_Talking_and_Interacting/mocap/Rokoko_Default/13_Sitting_Sofa_Conversation_Agree.fbx | Anderson Rohr / Mocap_Pack_03_Talking_and_Interacting | clip | Rokoko Default | Conversation.Seated.Neutral.03 | Relaxed-seat conversation and agreement |
| 30 | Shaking Hands 1.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Social.Handshake.01 | Paired-person contact planning; counterpart alignment unverified |
| 31 | Sad Idle.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Emotion.Sad.Unknown.03 | Low-energy emotional baseline |
| 32 | Crying.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Emotion.Crying.Unknown.01 | Strong emotional acting test |
| 33 | Scary Zombie Pack/zombie walk.fbx | Mixamo / Scary Zombie Pack | mixamo.com | Mixamo | Genre.Horror.ZombieWalk.09 | One deliberately specialized gait for film scenes |
| 34 | Sitting Yell.fbx | Mixamo / Individual downloads | mixamo.com | Mixamo | Conversation.Seated.Yell.01 | Strong seated dialogue contrast |

## Local licensing and provenance

| Source family | Local evidence / URL | License status |
| --- | --- | --- |
| Mixamo individual and pack downloads | User attribution plus FBX creator/skeleton information; no local README/license/source URL found | LICENSE VERIFICATION REQUIRED |
| Anderson Rohr six action packs | Importing_into_UE5.pdf (six byte-identical copies); embedded links: https://andersonrohr.gumroad.com and https://www.linkedin.com/in/andersonrohr | LICENSE VERIFICATION REQUIRED |
| Rokoko free archive | READ_ME_FIRST_.txt directs to www.youtube.com/rokokomotion; retained path and original filenames | LICENSE VERIFICATION REQUIRED |
| Mocap/Crouch_Walk.fbx | Loose file; skeleton recorded, vendor/download URL unknown | LICENSE VERIFICATION REQUIRED |

The eight-page guide is about a UE5 retarget pose and links to the author; the README recommends a tutorial. Neither supplies a local license grant. No terms were inferred from “free,” vendor reputation, filename or memory. Local URL strings are provenance only and were not visited. Each physical catalogue entry preserves original filename, source pack, local document paths and license status. Franchise-named content has not received a separate rights review.

## Files requiring special attention

| Source file | Finding |
| --- | --- |
| Female Sitting Pose.fbx | Static keyed pose; no varying skeletal channels. Not a moving idle performance. |
| Male Sitting Pose (1).fbx | Static keyed pose; no varying skeletal channels. Not a moving idle performance. |
| Male Sitting Pose (2).fbx | Static keyed pose; no varying skeletal channels. Not a moving idle performance. |
| Male Sitting Pose.fbx | Static keyed pose; no varying skeletal channels. Not a moving idle performance. |
| Mocap/Rokoko_Free_Mocap_FBX_263/Rokoko Studio Legacy Mocap (older)/Movement/Junkie_FloatinginSpace_face_P_jgbZmX.fbx | Facial blendshape-only animation confirmed; not corrupt, not a Humanoid body motion |
| Mocap/Rokoko_Free_Mocap_FBX_263/Rokoko Studio Legacy Mocap (older)/Movement/Junkie_Injecting_face_P_jgbZmX.fbx | Facial blendshape-only animation confirmed; not corrupt, not a Humanoid body motion |
| Mocap/Rokoko_Free_Mocap_FBX_263/Rokoko Studio Legacy Mocap (older)/Movement/Junkie_LandingonCouch_face_P_jgbZmX.fbx | Facial blendshape-only animation confirmed; not corrupt, not a Humanoid body motion |
| Mocap/Rokoko_Free_Mocap_FBX_263/Rokoko Studio Legacy Mocap (older)/Movement/LookBehind_LetsGo_face_P_jgbZmX.fbx | Facial blendshape-only animation confirmed; not corrupt, not a Humanoid body motion |
| Mocap/Rokoko_Free_Mocap_FBX_263/Rokoko Studio Legacy Mocap (older)/Movement/PogFace_face_P_jgbZmX.fbx | Facial blendshape-only animation confirmed; not corrupt, not a Humanoid body motion |
| Mocap/Rokoko_Free_Mocap_FBX_263/Rokoko Studio Legacy Mocap (older)/Movement/SmilesandFrownss_face_P_jgbZmX.fbx | Facial blendshape-only animation confirmed; not corrupt, not a Humanoid body motion |

Other caveats: the Legacy archive contains opaque take names, segments and staged compound performances; six facial-only exports require a separate facial pipeline if ever used. Eleven legacy files use the FBX Model type Limb instead of LimbNode; these were explicitly recognized and their 53-bone hierarchies recovered. The remaining one-bone hierarchies belong to facial exports. Declared FPS varies across 24, 30, 60 and 100; actual key spacing can differ. FBX versions observed are 7500 and 7700. Metadata parsing cannot certify FBX import behavior.

## Deliverables and reproducibility

- `Docs/AnimationLibraryAudit.md`: this report, coverage, gaps and 34-motion test selection.
- `Docs/AnimationLibraryCatalog.json`: all physical files, skeleton/static-transform registries, stacks, estimated unique motions, classifications, interaction plans, compound candidates, coverage and first test set.
- `Docs/AnimationLibraryDuplicates.json`: byte duplicates, exact curve groups, export evidence, individual/pack overlap, retained semantic variations and unresolved partial overlaps.
- `Docs/AnimationAuditTools/`: scanner, cached enrichment, duplicate/segment analysis and report builders. These contain code only; no binary animation files are in Docs.
- `GeneratedAssets/AnimationAuditCache/`: derived metadata, small sampled-curve caches, extracted PDF provenance and guide contact sheet. This is outside both Assets and the source archive.

Run from the repository root with Python and NumPy: `python Docs/AnimationAuditTools/fbx_scan.py`, then `enrich_scan.py`, `build_catalog.py`, `check_segments.py`, and `write_report.py` in that same tool directory. The scanner reuses unchanged path/size/mtime entries and content-hash metadata caches; use a fresh cache when changing parser/fingerprint algorithms or when source timestamps cannot be trusted. The PDF provenance cache was extracted with the bundled pypdf runtime.

Validation passed: unique file/take/motion references; all takes assigned once; 34 distinct selected motion groups; semantic-ID uniqueness; duplicate cross-references; source paths, size and last-write-time preservation. No Unity tests or project suite were run. The source was not rehashed a second time unnecessarily.

**Recommended next step:** verify licenses for the source families, then approve this 34-motion selection for a separate, deliberately small Unity Humanoid validation milestone on the actual SilverScreen characters. That milestone should verify posture, avatar mapping, root motion, finger behavior, loops, seat/prop contacts and naming proposals before any larger import or runtime catalogue implementation.
