"""Add curated coverage/test-set assessments and write the human-readable audit."""
from build_catalog import ROOT,CACHE,dump,textname
from pathlib import Path
import json,collections,re

EVERY='Mocap/Mocap_Pack_01_Everyday_Actions/mocap/Rokoko_Default/'
TALK='Mocap/Mocap_Pack_03_Talking_and_Interacting/mocap/Rokoko_Default/'
CLEAN='Mocap/Mocap_Pack_02_Cleaning/mocap/Rokoko_Default/'
TESTS=[
('Basic Locomotion Pack/walking.fbx','Neutral walk and foot-contact baseline'),
('Locomotion Pack/running.fbx','Faster gait and displacement contrast'),
('Basic Locomotion Pack/idle.fbx','Neutral standing baseline and potential loop seam'),
('Basic Locomotion Pack/left turn 90.fbx','Facing change and root-yaw handling'),
('Seated Idle.fbx','Chair height and seated baseline'),
('Talking.fbx','General conversation; confirm posture before assigning a standing ID'),
('Sitting Talking.fbx','Explicitly seated conversation'),
('Standing Arguing.fbx','Stronger standing dialogue performance'),
('Sitting And Pointing.fbx','Seated gesture and directional intent'),
('Sitting Angry.fbx','Seated emotional intensity'),
('Sitting Disapproval.fbx','Negative seated reaction'),
('Sitting Laughing.fbx','Positive seated emotional reaction'),
('Asking Question.fbx','Question gesture; seated applicability still needs inspection'),
('Agreeing.fbx','Conversational agreement; preserve distinct numbered variant'),
('Beckoning.fbx','Inviting gesture; hand and posture review'),
('Stand To Sit.fbx','Seat approach, pelvis placement and entry'),
('Sit To Stand.fbx','Seat exit and foot planting'),
('Taking Item.fbx','Generic item reach/grasp; handedness unknown'),
('Carrying.fbx','Sustained carried-prop pose and root behavior'),
('Opening Door Inwards.fbx','Door handle, swing direction and body clearance'),
(EVERY+'10_Close_Door.fbx','Door closing and alternate export hierarchy'),
('Writing.fbx','Pen, desk and wrist contact; posture unknown'),
('Drinking.fbx','Cup-to-mouth coordination; posture unknown'),
('Talking On Phone.fbx','Receiver-to-hand/head alignment'),
('Sit To Type.fbx','Chair-to-keyboard action and two-hand contact planning'),
(CLEAN+'12_Sweeping_01.fbx','Reusable work loop candidate and tool contact'),
(EVERY+'01_Open_Curtain_v1.fbx','Flexible environmental reach interaction'),
(TALK+'11_Sitting_Coffee_Conversation.fbx','Combined seated dialogue and prop handling'),
(TALK+'13_Sitting_Sofa_Conversation_Agree.fbx','Relaxed-seat conversation and agreement'),
('Shaking Hands 1.fbx','Paired-person contact planning; counterpart alignment unverified'),
('Sad Idle.fbx','Low-energy emotional baseline'),
('Crying.fbx','Strong emotional acting test'),
('Scary Zombie Pack/zombie walk.fbx','One deliberately specialized gait for film scenes'),
('Sitting Yell.fbx','Strong seated dialogue contrast'),
]

# Ratings judge breadth of the downloaded vocabulary, not production readiness.
COVERAGE=[
('Basic locomotion','STRONG',['Locomotion.Walk','Locomotion.Run'],'Multiple walk/run/strafe/turn packs; loopability and starts/stops not yet validated.'),
('Locomotion variation','STRONG',['Locomotion.Injured','Locomotion.Drunk','Locomotion.Crouch','Locomotion.Backward'],'Male/female, injured, drunk, backward and crouched variants; paired export copies excluded.'),
('Standing idles','STRONG',['Idle'],'Basic idle plus Regular light/medium/heavy and other idle performances. Many genre idles; not all are neutral.'),
('Seated idles','ADEQUATE',['Idle.Seated'],'Seated Idle, sitting idle recordings and sitting poses; still need neutral listening and loop/contact review.'),
('Standing conversation','STRONG',['Conversation.Talk','Conversation.Standing','Conversation.Performance'],'Talking variants, explicit standing conversations/arguments, and staged courtroom/TV performances. Unspecified posture stays unknown.'),
('Seated conversation','STRONG',['Conversation.Seated'],'Sitting Talking variants and chair/sofa/coffee conversation recordings; good expressive range.'),
('Listening/reactions','WEAK',['Conversation.Listen','Gesture.Agree','Gesture.Disagree','Emotion.Reaction'],'Agreement and strong reactions exist; no clearly named dedicated neutral listening clip identified.'),
('Gestures','STRONG',['Gesture'],'Point, nod, disagree, dismiss, beckon, clap and gesture packs; two Gestures folder downloads repeat motion data.'),
('Emotional acting','STRONG',['Emotion'],'Anger, sadness, crying, laughter, disbelief, fear, disappointment; subtle and transitional acting remains unproven.'),
('Social interactions','ADEQUATE',['Social'],'Greetings, bows, salutes and handshakes; sparse contact-rich paired behavior.'),
('Office/work','ADEQUATE',['Work.Write','Work.Type','Work.Read'],'Writing and computer administration candidates; no clearly named reading performance found. Continuity and reusable loops need checking.'),
('Telephone','ADEQUATE',['Work.Telephone'],'Talking On Phone and pacing cover ordinary calls; many other phone assets depict modern video/texting and need era filtering.'),
('Eating','WEAK',['DailyLife.Eat'],'SomethingBehindMe_Eating is a staged sequence; no clear general meal/utensil/bite loop set.'),
('Drinking','ADEQUATE',['DailyLife.Drink'],'Drinking and seated coffee/cup performances; no confirmed reusable sip/hold/put-down decomposition.'),
('Smoking','WEAK',['DailyLife.Smoke'],'One clearly named leaning/smoking recording; seated smoking and start/end gestures not established.'),
('Object handling','WEAK',['Interaction.TakeItem','Interaction.PickUp','Interaction.PutDown'],'Taking Item, dishes, plate/table sequence and a sports pickup; lacks a clearly reusable generic pickup/place/handoff suite.'),
('Doors/environment interactions','STRONG',['Interaction.DoorOpen','Interaction.DoorClose','Interaction.Curtain','Interaction.Window'],'Door opening/closing, curtains and windows; actual handle heights and handedness unmeasured.'),
('Sitting/standing transitions','ADEQUATE',['Transition.StandToSit','Transition.SitToStand'],'Explicit transitions plus chair/sofa recordings; chair types, approach directions and phase boundaries need validation.'),
('Carrying','WEAK',['Work.Carry'],'One explicitly named general Carrying motion; no established box/tray/one-hand/two-hand weight variants.'),
('Cleaning','STRONG',['Work.Clean'],'13 source actions include sweeping, vacuuming, dusting, glass cleaning and dishes; broad reusable work coverage.'),
('Crowd/background behavior','STRONG',['Gesture.Clap','Social.Celebrate'],'22 Crowd Reactions actions plus idles, clapping, cheering, looking and phone behaviors; quiet period-appropriate extras need curation.'),
('Romance/affection','WEAK',['Social.Romance','Social.Hug'],'Flirty performance exists; no clearly named hug/kiss/hand-holding partner set identified.'),
('Comfort','WEAK',['Social.Comfort'],'CradlingBaby exists; this does not cover comforting an adult, consoling, or a shoulder touch.'),
('Fighting','STRONG',['Action.Fight'],'Broad combat, boxing, swords and staged multi-person performances.'),
('Firearms','STRONG',['Action.Gun'],'Shooter/rifle packs, pistol/rifle variations and genre performances; historical weapon compatibility unverified.'),
('Injury','STRONG',['Action.Injury','Locomotion.Injured'],'Injured locomotion, hit/shot reactions and wounded performances.'),
('Falls/deaths','ADEQUATE',['Action.Fall','Action.Death'],'Falls, knockouts, dying and zombie deaths exist; generic direction/height coverage not established.'),
('Dancing','STRONG',['Genre.Dance'],'Breakdance plus Rokoko dance recordings; 1930s social/partner dances are a separate gap.'),
('Sports','STRONG',['Genre.Sports'],'Soccer, fitness, cricket and other sports; large breadth rather than immediate studio-life priority.'),
('Horror/zombies','STRONG',['Genre.Horror','Genre.Zombie'],'Two Mixamo zombie packs and Rokoko horror/mummy/zombie recordings.'),
('Fantasy/superhero','STRONG',['Genre.Fantasy','Genre.Superhero'],'Magic, superhero and franchise-named performances; preserve descriptive provenance and verify rights separately.'),
('Stunts','ADEQUATE',['Action.Stunt'],'Climbing, jumps, diving, circus and crashes; no promise of full traversal/contact coverage.'),
]

GAPS={
 'MUST HAVE FOR BASIC SILVERSCREEN':[
  'Quiet standing and seated listening loops, with small nods, attention shifts and neutral-to-react transitions.',
  'Generic object pickup, hold, handoff and put-down at desk/shelf/floor heights; left/right and one/two-hand variants.',
  'Reusable carrying: small item, papers, tray, box; locomotion starts/stops and turns without losing grip.',
  'Conversation continuity: neutral entry/exit, interruption, listening-to-speaking transitions, restrained seated disagreement and interest.',
  'Period office essentials: paper handling/reading, writing and typewriter use, desk telephone pickup/listen/hang-up; modern screen use does not substitute.',
  'Basic seated meal behavior: utensil/food pickup, bite, chew/pause and put-down; drinking cup hold/sip/return phases.',
  'Adult comfort and basic affection: shoulder touch, reassuring hand, hug; paired spatial relationships need an intentional set.'
 ],
 'NICE TO HAVE':[
  'Chair transitions for armchairs, sofas and dining chairs; different approach directions and seat heights.',
  'Subtle seated reactions: skeptical, bored, attentive, nervous, amused, disappointed; quiet restaurant/date/interview listening.',
  'Door/prop variations by hand, hinge side, push/pull direction and object height; current assets may adapt but coverage is unverified.',
  'Period background extras: queueing, waiting, idle conversation pairs, newspaper reading and seated smoking.',
  'Film-crew specifics: camera/tripod handling, boom pole, slate, lights, cables and directing gestures.',
  'Partner romance and adult comfort, calm exits/returns, continuity between emotional intensities.'
 ],
 'GENRE-SPECIFIC / LATER':[
  '1930s social/partner dancing and musical staging; plentiful breakdance does not fill this gap.',
  'Period-appropriate weapon handling, recoil/reload and paired stunt reactions; validate existing compatibility first.',
  'Matched fight choreography, falls by direction/height, climbing/traversal contacts and safety-oriented staging variants.',
  'Additional sports, magic and monster variations only after reusable everyday behavior is covered.'
 ]}

def main():
    cp=ROOT/'Docs/AnimationLibraryCatalog.json';dp=ROOT/'Docs/AnimationLibraryDuplicates.json'
    c=json.loads(cp.read_text());d=json.loads(dp.read_text());fs={f['id']:f for f in c['files']};paths={f['relative_path']:f for f in c['files']};ts={t['id']:t for t in c['takes']};ms={m['id']:m for m in c['motions']};tm={t:m for m in c['motions'] for t in m['take_ids']};s=c['summary']
    selection=[]
    for path,why in TESTS:
        f=paths[path];t=ts[f['take_ids'][0]];m=tm[t['id']]
        selection.append({'motion_id':m['id'],'file_id':f['id'],'source_file':path,'source_vendor':f['source_vendor'],'source_pack':f['source_pack'],'take_id':t['id'],'take_name':t['name'],'stack_index':t['stack_index'],'recommended_skeleton_variant':f['skeleton_export_variant'],'proposed_semantic_id':m['proposed_semantic_id'],'reason':why,'imported':False})
    assert len(selection)==34 and len({x['motion_id'] for x in selection})==34
    coverage=[]
    for area,rating,tags,reason in COVERAGE:
        matches=[m['id'] for m in c['motions'] if any(any(t==tag or t.startswith(tag+'.') for t in m['tags']) for tag in tags)]
        coverage.append({'area':area,'rating':rating,'assessment':reason,'supporting_motion_ids':matches,'tag_match_count':len(matches),'count_caution':'Candidate tag matches, not a verified number of production-ready performances'})
    submotions=[]
    compounds={
      '03_Open_Curtain_and_Window':['Interaction.Curtain.Open','Interaction.Window.Open'],
      '07_Close_Window_and_Curtain':['Interaction.Window.Close','Interaction.Curtain.Close'],
      '09_Open_and_Close_Door':['Interaction.Door.Open','Interaction.Door.Close'],
      '17_Walk_TakePlate_SetTable_Sit':['Locomotion.Walk','Interaction.Item.TakePlate','Interaction.Item.SetTable','Transition.StandToSit'],
      '04_Stand_Point_And_Walk':['Gesture.Point.Standing','Locomotion.Walk'],
      '05_Stand_Agree_And_Walk':['Gesture.Agree.Standing','Locomotion.Walk'],
      '06_Walk_Stop_Point_And_Continue':['Locomotion.Walk','Gesture.Point.Standing','Locomotion.Walk'],
      '07_Walk_Stop_Agree_And_Continue':['Locomotion.Walk','Gesture.Agree.Standing','Locomotion.Walk'],
      '14_Sitting_Sofa_Stand_Wave_And_Walk':['Idle.Seated','Transition.SitToStand','Social.Greeting','Locomotion.Walk'],
    }
    for m in c['motions']:
        name=re.sub(r'_(iclone|ue5)$','',m['display_name'],flags=re.I)
        if name in compounds:
            for i,family in enumerate(compounds[name]):submotions.append({'id':m['id']+':submotion:'+str(i),'source_motion_id':m['id'],'source_take_ids':m['take_ids'],'proposed_semantic_family':family,'start_seconds':None,'end_seconds':None,'evidence':'Explicit action names in vendor filename; boundaries and separability unverified','counted_as_independent_unique_motion':False})
    c.update(first_runtime_test_set=selection,coverage_assessment=coverage,prioritized_gaps=GAPS,compound_action_candidates=submotions)
    if (CACHE/'segment_checks.json').exists():d['partial_overlap_candidates']=json.loads((CACHE/'segment_checks.json').read_text())
    c['licensing_assessment']={'status':'LICENSE VERIFICATION REQUIRED for all source families','evidence':'6 byte-identical eight-page UE5 retarget guides, one Rokoko README; no local license grant found','guide_review':'All eight pages inspected in a rendered contact sheet; extracted text and PDF link annotations cached','network_verification_performed':False}
    # Physical safety verification uses metadata only; all content hashes were calculated during the scan.
    from fbx_scan import SOURCE
    now={p.relative_to(SOURCE).as_posix():p.stat() for p in SOURCE.rglob('*') if p.is_file()}
    unchanged=len(now)==len(c['files']) and all(f['relative_path'] in now and now[f['relative_path']].st_size==f['size_bytes'] and now[f['relative_path']].st_mtime_ns==f['mtime_ns'] for f in c['files'])
    assert unchanged
    c['validation']={'source_file_count_size_mtime_unchanged':unchanged,'all_file_ids_unique':len(fs)==len(c['files']),'all_take_ids_unique':len(ts)==len(c['takes']),'all_motions_nonempty':all(m['take_ids'] for m in ms.values()),'all_takes_assigned_once':len(tm)==len(ts) and sum(len(m['take_ids']) for m in ms.values())==len(ts),'first_set_unique_motions':len({x['motion_id'] for x in selection}),'source_contents_rehashed_at_end':False,'unity_run':False,'unity_tests_run':False,'project_compiled':False,'visual_animation_validation':False}
    assert all(t['file_id'] in fs for t in ts.values())
    assert all(x in ts for m in ms.values() for x in m['take_ids'])
    assert all(x['recommended_take_id'] in x['take_ids'] for x in ms.values())
    semantic=[m['proposed_semantic_id'] for m in ms.values() if m['proposed_semantic_id']]
    assert len(semantic)==len(set(semantic))
    for key in ['equivalent_motion_data','export_variants','unresolved_export_variants','individual_vs_pack']:
        assert all(i in ts for g in d[key] for i in g['take_ids'])
    dump(cp,c);dump(dp,d)
    lines=[]
    def add(x=''):lines.append(x)
    def table(headers,rows):
        add('| '+' | '.join(headers)+' |');add('| '+' | '.join('---' for _ in headers)+' |')
        for row in rows:add('| '+' | '.join(str(x).replace('|','/').replace('\n',' ') for x in row)+' |')
        add()
    add('# SilverScreen animation library audit');add();add('Audit date: 2026-09-28. Source: `ExternalSourceAssets/Animations`. This is an offline archive inventory and planning catalogue. No source files were changed, imported, renamed, moved or deleted. No Unity, tests, project compilation, runtime assets, gameplay edits, commits or pushes were used.');add()
    add('## Result and counting rules');add()
    add('The downloaded collection already provides broad raw animation vocabulary for studio life and filmmaking, but quantity exceeds readiness. Conversation, expressive seated acting, locomotion, environmental actions and film genres are well represented. Quiet listening, ordinary object workflows, restrained paired social behavior and period-specific office/meal continuity are the priority gaps. A defensible percentage of the complete future vocabulary cannot be calculated without a defined target vocabulary and visual/contact validation.');add()
    table(['Measure','Count / result'],[
      ['All source files scanned',s['source_files_scanned']],['Animation-capable FBX files',s['animation_capable_files']],['Other files','6 PDF + 1 TXT + 18 Unreal IK/retarget support assets'],['Animation stacks',s['animation_stacks_discovered']],['Estimated distinct whole-source-take groups',s['estimated_unique_source_take_motions']],['Estimated distinct skeletal motion groups',s['estimated_unique_skeletal_motions']],['Distinct facial-only animation files','6 (51 blendshape channels each)'],['Byte-identical FBX groups / redundant FBX copies',f"{s['exact_duplicate_fbx_groups']} / {s['exact_redundant_fbx_files']}"],['All-file byte-identical groups / redundant copies',f"{s['exact_duplicate_groups_all_files']} / {s['exact_redundant_files_all_files']}"],['Different-file exact local-curve groups',s['equivalent_curve_groups']],['Accepted cross-export evidence links',s['accepted_export_variant_links']],['Additional reduction beyond byte duplicates',s['nonbyte_equivalent_or_variant_reduction']],['Individual-vs-pack motion groups',s['individual_vs_pack_groups']],['Normalized skeleton hierarchies',s['skeleton_signatures']],['Corrupt/unreadable FBX','0 in this parser; not a full SDK/import validation'],['First future runtime test set',len(selection)]])
    add('A “unique motion” here means an estimated whole-take equivalence group. It is **not** a count of isolated reusable actions. Of the 883 groups, **873 are moving skeletal performances, four are static sitting poses, and six are facial-only recordings**. Export evidence is strong but not evaluated cross-rig pose proof. Unknown trimmed overlaps stay separate. Compound recordings can contain several actions whose frame boundaries are not known; they are listed as candidates without increasing the unique count. Duplicate categories overlap, so their group sizes must not be added. The arithmetic is 1,356 - 53 byte copies - 420 additional equivalent/variant copies = 883 groups.');add()
    table(['Priority','Estimated motion groups'],[(k,s['usefulness_counts'].get(k,0)) for k in ['ESSENTIAL','USEFUL','SPECIALIZED','LOW PRIORITY']])
    add('Priorities and tags are filename/folder-based editorial proposals. SPECIALIZED preserves film-genre value; LOW PRIORITY includes opaque names and facial diagnostics, not permission to discard them. Every group remains represented in the JSON.');add()
    add('## Method and reliability');add()
    add('- SHA-256 covers every complete source file. Size and last-write nanoseconds are recorded; a final metadata check found the same paths, sizes and last-write times. Access times may be filesystem-managed; they were not rewritten.');
    add('- A read-only binary-FBX parser follows Objects, Connections, animation stacks/layers/nodes and compressed key arrays. All 1,356 files parsed. Geometry payloads are skipped; skeleton hierarchy, export settings and static transforms are retained. This is not a full FBX SDK scene evaluator.');
    add('- Metadata includes FBX version, creator, skeleton hash/bones, stack name, declared FPS, inclusive nominal frame count, actual distinct key-time count, duration, keyed/varying channels, finger-curve variation and local root/hip translations. JSON null/unknown values are intentional.');
    add('- Frame count is round(duration × declared FPS) + 1. Declared FPS is not necessarily the rate of baked keys. Facial-only duration comes from deformation-curve key bounds when stack bounds are absent.');
    add('- Exact curve fingerprint: normalized bone/channel labels, time relative to stack start, float curve values and interpolation attributes; grouping also requires hierarchy and duration. Nine such groups have different static transform sets, so common local motion data does not prove identical evaluated poses.');
    add('- Secondary fingerprint: 121 linearly interpolated samples per local channel, translation origin removed, values quantized to 0.001. It is a search tool, not FBX cubic/pivot/pre-rotation evaluation. No additional groups were accepted solely by this fingerprint.');
    add('- Export grouping requires matching source-family action identity, duration within 0.04 seconds and sampled pelvis-speed correlation >0.98 with normalized speed RMSE <0.02. Labels alone are never enough. This validates common performance timing/trajectory, not retarget quality.');
    add('- Finger presence means varying named finger curves; it does not establish intentional finger capture. Local hip excursion can include body sway, and parent rotations can change axes. In-place/translating fields are candidates; no runtime root-motion extraction is claimed.');
    add('- Five named full/segment pairs received a targeted raw-curve containment check; none met the strict contiguous-sample test. They remain unresolved partial-overlap candidates, separately listed in the duplicate JSON. Arbitrary time-warped, resampled, trimmed, or different-rig duplicates outside identified export families may remain.');
    add('- No animation was visually inspected. Handedness, contacts, loopability, posture where unnamed and adaptation limits remain unverified. No network license research was performed.');add()
    add('## Actual Mixamo pack structures');add()
    add('Every listed pack is **C: a directory of separate FBX files**. Each FBX has exactly one stack. The observed archive therefore contains neither an A-style multi-stack pack file nor a B-style concatenated whole-pack file. A single action file can still be a compound performance; stack count alone does not prove atomicity. Each file/stack is catalogued individually with its pack provenance.');add()
    mixpacks=[p for p in c['packs'] if 'Mixamo' in p['vendor'] and p['name']!='Individual downloads']
    table(['Pack folder','FBX files','Stacks','Motion groups within folder'],[(p['name'],p['physical_fbx_files'],p['take_count'],p['estimated_unique_motions']) for p in mixpacks])
    add('Per-folder motion counts overlap with other folders. `Gestures Pack Basic` and `Gestures Pack Basic (1)` contain 15 matching local-curve motions across two downloads, although the corresponding files are not byte-identical. Basic/expanded locomotion, male/female and shooter/rifle packs also have some exact curve overlaps; use the duplicate JSON rather than summing this table.');add()
    add('## Other source packs and export variants');add()
    table(['Pack','Physical FBX files','Estimated source motions','Exports'],[(p['name'],p['physical_fbx_files'],p['estimated_unique_motions'],'iClone / Rokoko Default / UE5') for p in c['packs'] if p['name'].startswith('Mocap_Pack_')])
    table(['Export variant','Physical FBX files'],s['skeleton_export_variants'].items())
    add('The six Anderson Rohr packs contain 112 underlying actions in 336 files. The larger Rokoko archive includes modern Mixamo, HumanIK and Unreal exports, legacy HumanIK recordings, repeated combat exports inside unrelated Unreal folders, and six facial-only FBX files. No BVH, GLB, GLTF or compressed archives were present. The 18 `.uasset` files are named IK_Source, IK_Target and RTG_RKK_to_MH; they are catalogued as Unreal support assets, not counted as independent motions. Their binary internals were not decoded.');add()
    add('**Proposed Unity Humanoid preference:** start with Mixamo where the same motion has that export, then HumanIK. For the six packs without either, use Rokoko Default for the first test. This is a practical choice based on recognizable biped hierarchy and fewer engine-specific helper-bone assumptions, not a verified importer result. Keep iClone and Unreal variants for comparison/fallback, particularly if contact or shoulder orientation differs. Never choose an export by filename alone when the character test contradicts it.');add()
    table(['Skeleton hash prefix','Bones','Example physical file'],[(sk['signature'][:12],sk['bone_count'],next(f['relative_path'] for f in c['files'] if f.get('skeleton_signature')==sk['signature'])) for sk in c['skeletons']])
    add('## Duplicates and variation');add()
    add('Category A has 53 redundant FBX copies across 10 groups. Most are repeated Unreal combat exports in TV-category folders; ShadowBoxing and AirGuitar also have byte copies. The six identical PDF guides and repeated support assets contribute another 20 redundant non-animation files. Nothing was deleted.');add()
    add('Category B has 66 exact local-curve groups plus 346 accepted cross-export links. Links form groups and are not independent motion counts. Static bind/default-transform differences remain inspectable. Category C has one confirmed individual-vs-pack family: **Angry Gesture (1).fbx**, **Gestures Pack Basic/angry gesture.fbx**, and **Gestures Pack Basic (1)/angry gesture.fbx**. No claim is made that these are the only possible approximate/trimmed overlaps.');add()
    add('Category D is retained. Agreeing and Agreeing (1), the Talking performances, and numbered seated/emotional downloads are not collapsed by name. The duplicate JSON contains same-name families with multiple surviving motion IDs, as well as full source-path lookups for every evidence group.');add()
    add('## SilverScreen coverage');add()
    add('Ratings describe available candidate vocabulary. STRONG does not mean retargeted, licensed, loopable or production-ready. Evidence counts in JSON are overlapping tag matches, so contextual notes take precedence over raw counts.');add()
    table(['Area','Rating','Assessment'],[(x['area'],x['rating'],x['assessment']) for x in coverage])
    add('No broad requested area is wholly MISSING by filename evidence; several essential **subareas** are missing or unestablished. One staged action does not make an area adequate.');add()
    add('## Seated performance assessment');add()
    table(['Scenario','Assessment','Evidence and gap'],[
      ['Office conversations','ADEQUATE','Seated Idle, Sitting Talking, seated point/anger/disapproval/laugh/yell and writing/type candidates. Need quiet listening, paper handoff and desk continuity.'],
      ['Meetings','ADEQUATE','Meeting-named files, questioning, agreement and seated gestures; Asking Question and Having A Meeting posture not established by names alone. Need neutral attentive participants.'],
      ['Restaurant conversations','ADEQUATE for talk; WEAK for meals','Sitting Coffee Conversation and drinking candidates. Missing a dependable full meal/utensil sequence and subtle listener coverage.'],
      ['Interviews','WEAK','Talking and reactions are usable candidates, but restrained listening, turn-taking and interviewer notes are not established.'],
      ['Interrogations','ADEQUATE as staged material','GoodCop/BadCop/Suspect named recordings plus anger/yell/disbelief; seated posture and isolated phases require review.'],
      ['Romantic scenes','WEAK','General conversation/laughter and Flirty exist; intimate seated listening, partner contact and affection are not established.'],
      ['Waiting','ADEQUATE','Seated idles and sitting poses exist; long natural loops and bored/nervous micro-variation need review.'],
      ['Emotional seated reactions','STRONG','Angry, disapproval, disbelief, laughing, yell, pointing and clap variants. Quiet disappointment, attentive listening and soft emotion transitions remain gaps.']])
    add('The intentional seated-download list is substantially represented. **Writing, Drinking, Beckoning, Asking Question and generic Clapping are not automatically marked seated**: without visual evidence that would invent posture. Explicitly sitting/seated files receive that tag; compound stand/sit records also need phase review. Female Sitting Pose and the three Male Sitting Pose files contain no varying skeletal curves: they are static pose references, not moving idle coverage. Seated Idle and the two legacy Sitting_Idle recordings are moving candidates.');add()
    seated=[m for m in c['motions'] if m['posture']=='Seated']
    add('Explicitly named seated candidates: '+', '.join('`'+m['display_name']+'`' for m in seated)+'.');add()
    add('## Interaction assessment');add()
    add(f"{sum(m['interaction'] is not None for m in c['motions'])} motion groups have interaction-planning metadata. Each contains expected props, possible IK targets, root-motion relevance, handedness uncertainty and size-adaptation limits. These are planning suggestions, not measured contact frames. Generic Taking Item and Carrying filenames do not establish one- or two-handed use.");add()
    table(['Motion family','Expected prop / IK targets','Important limitation'],[
      ['Taking Item / pickup / placement','Item grip; hand(s), placement surface, feet','Generic pickup evidence is sparse; a cricket pickup does not replace ordinary handling.'],
      ['Carrying','Object grip(s), carry anchor','One generic candidate; weight, object size and hand use unknown.'],
      ['Door / curtain / window','Handle/latch/frame/curtain edge; active hand and feet','Hinge direction and approach root motion matter; bilateral coverage unknown.'],
      ['Sit / stand / chair','Seat/pelvis, floor/feet, optional armrests','Seat height, approach and contact windows require character testing.'],
      ['Writing / typing','Pen/page or keyboard; wrist(s), desk, seat','One hand likely active for writing; two for typing. Planning inference only.'],
      ['Drinking / telephone','Cup-to-mouth or receiver-to-head; hand grip','Prop shape/scale and grip articulation unknown; avoid treating video calls as 1930s telephones.'],
      ['Cleaning','Broom, cloth, brush, vacuum or squeegee; tool grip and work surface','Good varied motion sources; tool geometry and environmental contacts remain unmeasured.']])
    add('For useful interaction records, small anchor offsets are plausible IK candidates; no reliable maximum object-size adaptation can be inferred from the archive. Root/hip curves are measured, but maintaining feet, seat and grip contacts must be validated together later.');add()
    add('### Compound actions retained without splitting');add()
    table(['Source motion','Individually catalogued action candidates','Boundaries'],[(name,', '.join(actions),'unknown; no files split') for name,actions in compounds.items()])
    add('These semantic subrecords retain parent motion/take IDs. They are not additional verified clips and are excluded from the unique count. Other staged/compound takes retain filename hints rather than invented segment boundaries.');add()
    add('## Prioritized gaps');add()
    for title,items in GAPS.items():
        add('### '+title);add()
        for i,item in enumerate(items,1):add(f'{i}. {item}')
        add()
    add('## First future runtime test set (34 distinct motions)');add()
    add('Selection only: nothing was copied into Assets. Paths below are relative to the immutable source root. The named stack is the precise source take, always index 0 here. All are different estimated motion groups. Semantic IDs are proposals; `Unknown` posture is an explicit review requirement. Anderson Rohr selections use Rokoko Default; other selections use their available Mixamo export.');add()
    table(['#','Source file','Vendor / pack','Stack','Skeleton','Proposed semantic ID','Why'],[(i+1,x['source_file'],('Anderson Rohr' if 'Anderson' in x['source_vendor'] else 'Mixamo')+' / '+x['source_pack'],x['take_name'],x['recommended_skeleton_variant'],x['proposed_semantic_id'],x['reason']) for i,x in enumerate(selection)])
    add('## Local licensing and provenance');add()
    table(['Source family','Local evidence / URL','License status'],[
      ['Mixamo individual and pack downloads','User attribution plus FBX creator/skeleton information; no local README/license/source URL found','LICENSE VERIFICATION REQUIRED'],
      ['Anderson Rohr six action packs','Importing_into_UE5.pdf (six byte-identical copies); embedded links: https://andersonrohr.gumroad.com and https://www.linkedin.com/in/andersonrohr','LICENSE VERIFICATION REQUIRED'],
      ['Rokoko free archive','READ_ME_FIRST_.txt directs to www.youtube.com/rokokomotion; retained path and original filenames','LICENSE VERIFICATION REQUIRED'],
      ['Mocap/Crouch_Walk.fbx','Loose file; skeleton recorded, vendor/download URL unknown','LICENSE VERIFICATION REQUIRED']])
    add('The eight-page guide is about a UE5 retarget pose and links to the author; the README recommends a tutorial. Neither supplies a local license grant. No terms were inferred from “free,” vendor reputation, filename or memory. Local URL strings are provenance only and were not visited. Each physical catalogue entry preserves original filename, source pack, local document paths and license status. Franchise-named content has not received a separate rights review.');add()
    add('## Files requiring special attention');add()
    table(['Source file','Finding'],[(fs[ts[x['take_id']]['file_id']]['relative_path'],x['reason']) for x in c['suspicious_files_or_takes'] if 'take_id' in x])
    add('Other caveats: the Legacy archive contains opaque take names, segments and staged compound performances; six facial-only exports require a separate facial pipeline if ever used. Eleven legacy files use the FBX Model type Limb instead of LimbNode; these were explicitly recognized and their 53-bone hierarchies recovered. The remaining one-bone hierarchies belong to facial exports. Declared FPS varies across 24, 30, 60 and 100; actual key spacing can differ. FBX versions observed are 7500 and 7700. Metadata parsing cannot certify FBX import behavior.');add()
    add('## Deliverables and reproducibility');add()
    add('- `Docs/AnimationLibraryAudit.md`: this report, coverage, gaps and 34-motion test selection.');
    add('- `Docs/AnimationLibraryCatalog.json`: all physical files, skeleton/static-transform registries, stacks, estimated unique motions, classifications, interaction plans, compound candidates, coverage and first test set.');
    add('- `Docs/AnimationLibraryDuplicates.json`: byte duplicates, exact curve groups, export evidence, individual/pack overlap, retained semantic variations and unresolved partial overlaps.');
    add('- `Docs/AnimationAuditTools/`: scanner, cached enrichment, duplicate/segment analysis and report builders. These contain code only; no binary animation files are in Docs.');
    add('- `GeneratedAssets/AnimationAuditCache/`: derived metadata, small sampled-curve caches, extracted PDF provenance and guide contact sheet. This is outside both Assets and the source archive.');add()
    add('Run from the repository root with Python and NumPy: `python Docs/AnimationAuditTools/fbx_scan.py`, then `enrich_scan.py`, `build_catalog.py`, `check_segments.py`, and `write_report.py` in that same tool directory. The scanner reuses unchanged path/size/mtime entries and content-hash metadata caches; use a fresh cache when changing parser/fingerprint algorithms or when source timestamps cannot be trusted. The PDF provenance cache was extracted with the bundled pypdf runtime.');add()
    add('Validation passed: unique file/take/motion references; all takes assigned once; 34 distinct selected motion groups; semantic-ID uniqueness; duplicate cross-references; source paths, size and last-write-time preservation. No Unity tests or project suite were run. The source was not rehashed a second time unnecessarily.');add()
    add('**Recommended next step:** verify licenses for the source families, then approve this 34-motion selection for a separate, deliberately small Unity Humanoid validation milestone on the actual SilverScreen characters. That milestone should verify posture, avatar mapping, root motion, finger behavior, loops, seat/prop contacts and naming proposals before any larger import or runtime catalogue implementation.');add()
    (ROOT/'Docs/AnimationLibraryAudit.md').write_text('\n'.join(lines),encoding='utf8')
    print('Report written; validation',c['validation']);print('Report lines',len(lines),'Compound candidates',len(submotions))

if __name__=='__main__':main()
