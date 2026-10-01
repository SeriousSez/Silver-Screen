"""Build SilverScreen audit deliverables from the read-only FBX scan cache."""
from fbx_scan import ROOT, SOURCE, CACHE, digest
from pathlib import Path
import json, re, collections, itertools
import numpy as np

def dump(p,v): p.write_text(json.dumps(v,indent=2,ensure_ascii=False),encoding='utf8')
def stable(prefix,s): return prefix+digest(s.encode())[:16]
def textname(s): return re.sub(r'[^a-z0-9]+',' ',re.sub(r'([a-z])([A-Z])',r'\1 \2',s).lower()).strip()
def base(s): return re.sub(r'_(mixamo|hik|ue5?|iclone)$','',Path(s).stem,flags=re.I).lower()
def variant(f):
    s=f['relative_path'].lower()
    for token,label in [('iclone','iClone'),('humanik','HumanIK'),('human ik','HumanIK'),('_hik','HumanIK'),('ue5','Unreal UE5'),('unreal','Unreal'),('_ue.','Unreal'),('mixamo','Mixamo'),('rokoko_default','Rokoko Default')]:
        if token in s: return label
    return f.get('fbx',{}).get('skeleton_name_inference','unknown')

# Rules are proposed semantics from vendor filenames/folders, never observed acting.
RULES=[
 ('Locomotion.Walk',r'walk|strafe'),('Locomotion.Run',r'\b(?:run|running|jog|jogging|sprint|sprinting)\b'),('Locomotion.Crouch',r'crouch'),('Locomotion.Turn',r'turn'),('Locomotion.Backward',r'backward|backwards|backpedal'),('Locomotion.Injured',r'injured|limp'),('Locomotion.Drunk',r'drunk'),
 ('Idle',r'idle|pose(?: \d+)?$|looking around|watch tv|waiting'),('Idle.Bored',r'bored'),('Idle.Waiting',r'wait'),('Idle.Tired',r'tired|exhaust'),('Idle.Nervous',r'nervous|anxious'),
 ('Conversation.Talk',r'talk|conversation|meeting|argu|yell|question|secret|explain'),('Conversation.Listen',r'listen'),('Conversation.Question',r'question'),('Conversation.Argue',r'argu'),('Conversation.Yell',r'yell|shout'),('Conversation.Secret',r'secret|psst'),('Conversation.Meeting',r'meeting'),
 ('Gesture.Point',r'point'),('Gesture.Agree',r'agree|nod|acknowledg'),('Gesture.Disagree',r'disagree|head shake|disapproval'),('Gesture.Shrug',r'shrug'),('Gesture.Beckon',r'beckon|come here'),('Gesture.Explain',r'explain'),('Gesture.Dismiss',r'dismiss'),('Gesture.Clap',r'clap|applaud'),('Gesture',r'gesture|salute'),
 ('Emotion.Angry',r'angry|anger'),('Emotion.Sad',r'\bsad\b'),('Emotion.Crying',r'cry'),('Emotion.Laughing',r'laugh'),('Emotion.Afraid',r'afraid|fear|scared'),('Emotion.Terrified',r'terrified'),('Emotion.Disappointed',r'disappoint|reject'),('Emotion.Thankful',r'thank'),('Emotion.Disbelief',r'disbelief'),
 ('Social.Greeting',r'greet|\bwave\b|bow'),('Social.Handshake',r'shak.*hand|handshake'),('Social.Hug',r'hug'),('Social.Comfort',r'comfort|console|cradl'),('Social.Celebrate',r'celebrat|cheer'),('Social.Romance',r'kiss|romantic|romance|affection'),
 ('Work.Write',r'writ'),('Work.Type',r'typ|computer|keyboard'),('Work.Read',r'\bread(?:ing)?\b'),('Work.Telephone',r'\bphone\b|telephone|cellphone'),('Work.Clean',r'clean|mop|sweep|vacuum|dust|scrub'),('Work.Carry',r'carry'),('Work.Farm',r'farm|rake|shovel|\bhoe\b|water plant'),
 ('Interaction.TakeItem',r'taking item|take item|take plate'),('Interaction.PickUp',r'pick up|pickup|picking up'),('Interaction.PutDown',r'put down|putting down|set table'),('Interaction.DoorOpen',r'open.*door'),('Interaction.DoorClose',r'close.*door'),('Interaction.Door',r'\bdoor\b'),('Interaction.Curtain',r'curtain'),('Interaction.Window',r'window'),('Interaction.Chair',r'chair|\b(?:sit|sitting|seated)\b|sitdown|sofa'),
 ('DailyLife.Eat',r'\beat|eating'),('DailyLife.Drink',r'drink|coffee|holding cup'),('DailyLife.Smoke',r'smok'),('DailyLife.Sleep',r'sleep'),
 ('Action.Fight',r'fight|combat|punch|boxing|sword|martial|chok|kick.*balls'),('Action.Gun',r'gun|rifle|pistol|shoot|musket|blaster|canon|cannon'),('Action.Injury',r'injur|wound|struck|shot in|getting kicked|knock.*out'),('Action.Fall',r'fall|knock.*down|knock.*out'),('Action.Death',r'death|dying|dead|die\b|seppuku'),
 ('Genre.Horror',r'horror|zombie|mummy|monster|scare|scary'),('Genre.Zombie',r'zombie'),('Genre.Dance',r'danc|breakdance|macarena|ballet'),('Genre.Sports',r'soccer|sports|cricket|football|fitness|workout|squat|push up|pushup|plank|exercise|jumping jack|swim|ping pong'),('Genre.Fantasy',r'magic|fantasy|star wars|destiny|force|lightsaber|jedi'),('Genre.Superhero',r'superhero|super hero|batman|superman|iron man|homelander'),('Action.Stunt',r'stunt|flip|dive|diving|climb|jump|vault|circus|car crash'),
 ('Transition.StandToSit',r'stand to sit|^sitting$|^\d+ sit(?: |$)'),('Transition.SitToStand',r'sit to stand|stand up|sit.*stand.*wave'),
]

def classify(f):
    s=textname(Path(f['original_filename']).stem)
    tags=[tag for tag,rule in RULES if re.search(rule,s)]
    context=f['relative_path'].lower()
    if 'stand up comedian' in s:tags=[t for t in tags if t!='Transition.SitToStand']+['Work.Perform']
    extra_rules=[('Emotion.Thinking',r'thinking'),('Gesture.Disagree',r'shaking head no'),('Emotion.Relief',r'relieved sigh'),('Emotion.Cocky',r'being cocky'),('Idle',r'weight shift'),('Interaction.PutDown',r'putting dishes'),('Work.Telephone',r'video call|texting|selfie|scrolling phone'),('Action.Injury',r'hit reaction|shotin|blood|medic'),('Action.Gun',r'reloading|grenade|rpg launcher'),('Social.Romance',r'flirty'),('Emotion.Reaction',r'double takes|face palms|head tilts|fist pumps'),('Conversation.Interrogation',r'interogation|interrogation'),('Work.Cook',r'chef cooking'),('Work.Perform',r'playing drums|playing guitar|playing piano|playing trumpet|playing sax|singer|rapping|air guitar'),('Work.Build',r'hammer|sawzall|\bsaw\b'),('Social.Comfort',r'cradling baby'),('Interaction.DoorOpen',r'burst through door'),('Action.Stunt',r'juggling|knockedinto|knocked into|ninja rise|crawling'),('DailyLife.Dress',r'undressing|takingoff suit|taking off suit|take off helmet|suit half')]
    for tag,rule in extra_rules:
        if re.search(rule,s):tags.append(tag)
    for token,tag in [('zombie','Genre.Zombie'),('breakdance','Genre.Dance'),('/dancing/','Genre.Dance'),('/sports/','Genre.Sports'),('soccer','Genre.Sports'),('fitness','Genre.Sports'),('/magic/','Genre.Fantasy'),('/starwars/','Genre.Fantasy'),('/superhero/','Genre.Superhero')]:
        if token in context: tags.append(tag)
    for token,tag in [('farming pack','Work.Farm'),('shooter pack','Action.Gun'),('rifle','Action.Gun'),('/combat/','Action.Fight'),('/guns/','Action.Gun'),('idlesmocappack','Idle'),('/music/','Work.Perform'),('/destiny/','Genre.Fantasy')]:
        if token in context:tags.append(tag)
    if any(x in context for x in ['/courtroom/','/game show/','/homeshopping/','/reality tv/']):tags.append('Conversation.Performance')
    posture='Seated' if re.search(r'sitting|seated|^sit |sit down|sitdown',s) else ('Standing' if re.search(r'stand|walk|run|strafe',s) else 'Unknown')
    if f['original_filename'].lower().startswith(('having a meeting','asking question','writing','drinking','beckoning')) and posture=='Unknown':
        # The request lists these as desired seated material, but filenames alone do not establish posture.
        pass
    if 'Idle' in tags and posture!='Unknown': tags.append('Idle.'+posture)
    if 'Conversation.Talk' in tags and posture!='Unknown': tags.append('Conversation.'+posture)
    genre=any(t.startswith('Genre.') or t in ('Action.Gun','Action.Fight','Action.Stunt','Work.Farm') for t in tags)
    essentials=any(t.startswith(('Conversation.','Idle','Locomotion.','Work.','Interaction.','Transition.')) for t in tags)
    tier='SPECIALIZED' if genre else ('ESSENTIAL' if essentials else ('USEFUL' if tags else 'LOW PRIORITY'))
    if '/meme/' in context or 'range of motion' in s: tier='LOW PRIORITY'
    family=None
    for head in ['Transition.','Interaction.Door','Interaction.Take','Interaction.Pick','Interaction.Put','Work.','DailyLife.','Conversation.','Emotion.','Gesture.','Social.','Idle','Locomotion.','Genre.','Action.']:
        matches=[t for t in tags if t.startswith(head)]
        if matches:
            family=matches[0]; break
    if family and family.startswith('Conversation.'):
        action=next((x.split('.')[1] for x in tags if x in ('Conversation.Argue','Conversation.Yell','Conversation.Question','Conversation.Secret')),'Neutral')
        family='Conversation.'+posture+'.'+action
    elif family=='Idle' or (family and family.startswith('Idle.')): family='Idle.'+posture+'.Neutral'
    elif family and family.startswith(('Gesture.','Emotion.')): family+='.'+posture
    if 'Genre.Zombie' in tags:family='Genre.Horror.Zombie'+('Walk' if 'Locomotion.Walk' in tags else ('Run' if 'Locomotion.Run' in tags else 'Performance'))
    return {'usefulness':tier,'tags':sorted(set(tags)),'posture':posture,'semantic_family':family,'semantic_evidence':'Filename and source-folder interpretation; no visual performance validation','semantic_confidence':'medium' if tags else 'unknown'}

def interaction(m):
    tags=m['tags']; kinds=[]; props=[]; targets=[]
    mapping=[('Interaction.Door','door',['hand: handle','feet: floor']),('Interaction.Curtain','curtain',['hand: curtain edge or pull']),('Interaction.Window','window',['hand: latch/frame']),('Interaction.Chair','chair/sofa',['pelvis: seat','feet: floor','optional hands: armrests']),('Work.Write','pen and desk',['writing hand: page','off hand: desk']),('Work.Type','keyboard/desk',['both hands: keyboard','pelvis: seat if seated']),('Work.Telephone','telephone',['hand: receiver','head: receiver proximity']),('Work.Clean','cleaning tool/surface',['tool hand(s): tool grip','tool: work surface']),('Work.Carry','carried object',['hand(s): grip','object: carry anchor']),('Interaction.TakeItem','item',['hand(s): item grip']),('Interaction.PickUp','item',['hand(s): pickup anchor','feet: floor']),('Interaction.PutDown','item/surface',['hand(s): placement anchor']),('DailyLife.Drink','cup/glass',['hand: cup grip','cup: mouth']),('DailyLife.Eat','food/utensil',['hand: utensil','utensil: mouth']),('DailyLife.Smoke','cigarette/pipe',['hand: prop','prop: mouth'])]
    for prefix,prop,ik in mapping:
        if any(t.startswith(prefix) for t in tags): kinds.append(prefix); props.append(prop); targets+=ik
    if not kinds:return None
    name=textname(m['display_name'])
    hands='unknown'
    if re.search(r'two hand|both hand',name):hands='two-handed (filename evidence)'
    elif re.search(r'one hand|single hand',name):hands='one-handed (filename evidence)'
    expected='likely one active hand' if any(t in tags for t in ['Work.Write','Work.Telephone','DailyLife.Drink','DailyLife.Smoke']) else ('likely two hands' if 'Work.Type' in tags else 'unknown; inspect contact phases')
    return {'types':kinds,'handedness':hands,'handedness_planning_inference':expected,'expected_props':props,'proposed_ik_targets':sorted(set(targets)),'root_motion_importance':'High for approach, seat alignment and transitions; inspect trajectory' if any(t.startswith(('Transition.','Locomotion.','Interaction.Chair','Interaction.Door')) for t in tags) else 'Maintain planted feet and contact; trajectory validation required','object_size_adaptability':'unknown; modest offsets are candidates for IK, larger reach/seat-height changes require validation','contact_frame_ranges':None,'basis':'Proposed interaction plan from semantics, not measured contact or hand pose'}

class Union:
    def __init__(self,ids): self.p={i:i for i in ids}
    def find(self,x):
        if self.p[x]!=x:self.p[x]=self.find(self.p[x])
        return self.p[x]
    def join(self,a,b): self.p[self.find(b)]=self.find(a)

def main():
    raw=json.loads((CACHE/'scan.json').read_text())['files']
    files=[]; skels={}; takes=[]; fby={}; tby={}; static_rigs={}
    for source in raw:
        f={k:v for k,v in source.items() if k!='fbx'}; f['id']=stable('file_',f['relative_path'])
        path=f['relative_path']; parts=path.split('/')
        if 'Mocap_Pack_' in path:
            pack=next(x for x in parts if x.startswith('Mocap_Pack_')); vendor='Anderson Rohr (local guide provenance)'
            docs=[x['relative_path'] for x in raw if pack in x['relative_path'] and x['extension']=='.pdf']
            urls=['https://andersonrohr.gumroad.com','https://www.linkedin.com/in/andersonrohr']
        elif 'Rokoko_Free' in path:
            pack=' / '.join(parts[1:-1]); vendor='Rokoko (archive path and README)'; docs=['Mocap/Rokoko_Free_Mocap_FBX_263/READ_ME_FIRST_.txt'];urls=['www.youtube.com/rokokomotion']
        elif parts[0]!='Mocap':
            pack=parts[0] if len(parts)>1 else 'Individual downloads';vendor='Mixamo (user attribution; skeleton/creator evidence where available)';docs=[];urls=[]
        else: pack='Unassigned Mocap';vendor='unknown';docs=[];urls=[]
        f.update(source_vendor=vendor,source_pack=pack,source_urls_locally_present=urls,local_provenance_documents=docs,local_license_documents=[],license_status='LICENSE VERIFICATION REQUIRED',skeleton_export_variant=variant(source) if source['extension']=='.fbx' else None)
        meta=source.get('fbx')
        if meta:
            if 'static_transform_signature' in meta:static_rigs[meta['static_transform_signature']]=meta['static_model_transforms']
            sig=meta['skeleton_signature'];skels[sig]={'signature':sig,'bone_count':meta['bone_count'],'hierarchy':meta['skeleton_hierarchy']}
            f.update({k:v for k,v in meta.items() if k not in ('takes','skeleton_hierarchy','static_model_transforms')})
            f['take_ids']=[]
            for index,c in enumerate(meta['takes']):
                t={k:v for k,v in c.items() if k!='channels'};t.update(id=f['id']+':take:'+str(index),file_id=f['id'],stack_index=index,skeleton_signature=sig)
                t['static_pose_only']=bool(t['keyed_channel_count'] and not t['varying_channel_count'])
                f['take_ids'].append(t['id']); takes.append(t); tby[t['id']]=t
        files.append(f); fby[f['id']]=f
    u=Union(tby); exact=[]; equivalents=[]; variants=[]; candidate_variants=[]
    def groups(items,key):
        d=collections.defaultdict(list)
        for x in items:
            k=key(x)
            if k is not None:d[k].append(x)
        return [v for v in d.values() if len(v)>1]
    for group in groups(files,lambda f:f['sha256']):
        g={'category':'A_BYTE_IDENTICAL','sha256':group[0]['sha256'],'file_ids':[f['id'] for f in group],'redundant_files':len(group)-1,'confidence':'exact'};exact.append(g)
        for index in range(len(group[0].get('take_ids',[]))):
            for f in group[1:]:u.join(group[0]['take_ids'][index],f['take_ids'][index])
    for group in groups(takes,lambda t:(t['skeleton_signature'],t['duration_seconds'],t['curve_fingerprint']) if t['curve_fingerprint'] else None):
        if len({fby[t['file_id']]['sha256'] for t in group})<2:continue
        equivalents.append({'category':'B_EXACT_LOCAL_CURVE_DATA','take_ids':[t['id'] for t in group],'confidence':'high for curve identity; evaluated pose equivalence unverified','evidence':'Same normalized skeleton hierarchy, duration, relative key times, local curve values and key interpolation attributes. Static bone transforms/FBX evaluation not compared.'})
        for t in group[1:]:u.join(group[0]['id'],t['id'])
    # Quantized fingerprints discover re-encodings; verify numerical arrays rather than trust bins.
    def sample(t):return np.load(CACHE/(fby[t['file_id']]['sha256']+'_'+str(t['stack_index'])+'.npz'))
    for group in groups(takes,lambda t:(t['skeleton_signature'],round(t['duration_seconds'],5),t['sample_fingerprint']) if t['sample_fingerprint'] and t['duration_seconds'] is not None else None):
        reps={u.find(t['id']):t for t in group}
        if len(reps)<2:continue
        equivalents.append({'category':'B_SAMPLED_LOCAL_CURVE_EQUIVALENCE','take_ids':[t['id'] for t in group],'confidence':'medium-high','evidence':'121 linear-interpolated local-curve samples match after 0.001 quantization and translation-origin removal; same hierarchy and duration. Not full FBX evaluation.'})
        for t in group[1:]:u.join(group[0]['id'],t['id'])
    # Export-family proposals require shared provenance and stripped export suffix, then numeric evidence.
    def variant_key(t):
        f=fby[t['file_id']];p=f['relative_path']
        if 'Mocap_Pack_' in p:return (f['source_pack'],base(p))
        if 'RokokoTVContest_MocapAssets/' in p:return ('RokokoTVContest',base(p))
        return None
    def root_speed(t):
        z=sample(t);labs=z['labels'];v=z['values']
        for bone in ('hips','hip','pelvis'):
            ix=[i for i,l in enumerate(labs) if l[0]==bone and l[1]=='Lcl Translation']
            if len(ix)==3:return np.linalg.norm(np.diff(v[ix],axis=1),axis=0)
        return None
    for group in groups(takes,variant_key):
        reps=list({u.find(t['id']):t for t in group}.values())
        if len(reps)<2:continue
        for a,b in itertools.combinations(reps,2):
            if u.find(a['id'])==u.find(b['id']):continue
            fa,fb=fby[a['file_id']],fby[b['file_id']]
            if fa['skeleton_export_variant']==fb['skeleton_export_variant']:continue
            da,db=a['duration_seconds'],b['duration_seconds']
            corr=None;speed_error=None
            if da is not None and db is not None and abs(da-db)<0.04:
                sa,sb=root_speed(a),root_speed(b)
                if sa is not None and sb is not None and np.std(sa)>1e-5 and np.std(sb)>1e-5:
                    corr=float(np.corrcoef(sa,sb)[0,1]);speed_error=float(np.sqrt(np.mean((sa/np.linalg.norm(sa)-sb/np.linalg.norm(sb))**2)))
            g={'category':'B_EXPORT_VARIANT','take_ids':[a['id'],b['id']],'same_source_motion_name':base(fa['relative_path']),'duration_difference_seconds':abs(da-db) if da is not None and db is not None else None,'root_speed_correlation':corr,'normalized_root_speed_rmse':speed_error,'confidence':'high' if corr is not None and corr>.995 else 'medium','evidence':'Shared pack/export naming, duration and sampled pelvis-speed comparison. Cross-rig pose equivalence not established.'}
            if corr is not None and corr>.98 and speed_error<.02:
                variants.append(g);u.join(a['id'],b['id'])
            else:
                g['category']='UNRESOLVED_EXPORT_VARIANT_CANDIDATE';g['confidence']='unresolved';candidate_variants.append(g)
    motiongroups=collections.defaultdict(list)
    for t in takes:motiongroups[u.find(t['id'])].append(t)
    motions=[]; semcounts=collections.Counter(); take_to_motion={}
    def rank(t):
        f=fby[t['file_id']];v=f['skeleton_export_variant'];return ({'Mixamo':0,'HumanIK':1,'Rokoko Default':2,'Rokoko':3,'iClone':4,'Unreal UE5':5,'Unreal':6}.get(v,9),len(f['relative_path']),f['relative_path'])
    for group in sorted(motiongroups.values(),key=lambda g:fby[min(g,key=rank)['file_id']]['relative_path'].lower()):
        t=min(group,key=rank);f=fby[t['file_id']]; m=classify(f)
        mid=stable('motion_',min(x['id'] for x in group));family=m.pop('semantic_family')
        m['static_pose_only']=all(x.get('static_pose_only') for x in group)
        if m['static_pose_only']:
            m['tags']=[tag for tag in m['tags'] if not tag.startswith('Idle')]+['Pose.'+m['posture']]
            family='Pose.'+m['posture']+'.Neutral'
        if family:semcounts[family]+=1
        m.update(id=mid,display_name=Path(f['original_filename']).stem,take_ids=[x['id'] for x in group],recommended_take_id=t['id'],recommended_skeleton_variant=f['skeleton_export_variant'],proposed_semantic_id=family+'.'+str(semcounts[family]).zfill(2) if family else None,segment_start_seconds=None,segment_end_seconds=None,segmentation_status='Whole source take; compound actions are not independently counted without boundaries',submotion_hints=[],equivalence_status='single source take' if len(group)==1 else 'analysis grouping; see duplicates evidence')
        if re.search(r'and|_to_|To|Scene|Various|LongTake',m['display_name']):m['submotion_hints']=['Potential compound performance; filename: '+m['display_name']+'; timing boundaries unknown']
        m['interaction']=interaction(m)
        motions.append(m)
        for x in group:take_to_motion[x['id']]=mid
    packdupes=[]
    for m in motions:
        group=[fby[tby[x]['file_id']] for x in m['take_ids']]
        if any(f['source_pack']=='Individual downloads' for f in group) and any(f['source_pack'].endswith('Pack') or 'Pack' in f['source_pack'] for f in group):
            packdupes.append({'category':'C_INDIVIDUAL_VS_PACK','motion_id':m['id'],'take_ids':m['take_ids'],'confidence':'Same accepted curve/equivalence group; see category B evidence'})
    semantic_similar=[]
    for group in groups([f for f in files if f.get('take_ids')],lambda f:re.sub(r' \(\d+\)$','',Path(f['original_filename']).stem.lower())):
        mids=sorted({take_to_motion[t] for f in group for t in f['take_ids']})
        if len(mids)>1:semantic_similar.append({'category':'D_SIMILAR_NAMES_NOT_DEDUPLICATED','name_family':re.sub(r' \(\d+\)$','',Path(group[0]['original_filename']).stem.lower()),'motion_ids':mids,'file_ids':[f['id'] for f in group],'reason':'Different retained motion-data groups; names alone do not establish equivalence.'})
    packs=[]
    for pack in sorted({f['source_pack'] for f in files if f['extension']=='.fbx'}):
        fs=[f for f in files if f['source_pack']==pack and f['extension']=='.fbx']; ids=[t for f in fs for t in f.get('take_ids',[])]
        packs.append({'name':pack,'vendor':fs[0]['source_vendor'],'file_ids':[f['id'] for f in fs],'physical_fbx_files':len(fs),'take_count':len(ids),'estimated_unique_motions':len({take_to_motion[t] for t in ids}),'structure':'C: directory of separate FBX files, each with one animation stack' if all(f.get('take_count')==1 for f in fs) and len(fs)>1 else 'See per-file stack counts','segmentation':'No source-internal action boundaries established; each stack catalogued separately'})
    summary={'source_files_scanned':len(files),'file_extensions':dict(collections.Counter(f['extension'] for f in files)),'animation_capable_files':sum(f['extension']=='.fbx' for f in files),'unreal_support_assets':sum(f['extension']=='.uasset' for f in files),'parsed_fbx':sum(f['status']=='parsed' for f in files),'animation_stacks_discovered':len(takes),'estimated_unique_source_take_motions':len(motions),'exact_duplicate_groups_all_files':len(exact),'exact_redundant_files_all_files':sum(g['redundant_files'] for g in exact),'exact_duplicate_fbx_groups':sum(fby[g['file_ids'][0]]['extension']=='.fbx' for g in exact),'exact_redundant_fbx_files':sum(g['redundant_files'] for g in exact if fby[g['file_ids'][0]]['extension']=='.fbx'),'equivalent_curve_groups':len(equivalents),'accepted_export_variant_links':len(variants),'unresolved_export_variant_links':len(candidate_variants),'individual_vs_pack_groups':len(packdupes),'skeleton_signatures':len(skels),'skeleton_export_variants':dict(collections.Counter(f['skeleton_export_variant'] for f in files if f['extension']=='.fbx')),'usefulness_counts':dict(collections.Counter(m['usefulness'] for m in motions))}
    summary['nonbyte_equivalent_or_variant_reduction']=len(takes)-summary['exact_redundant_fbx_files']-len(motions)
    suspicious=[]
    for f in files:
        if f['status']=='unreadable':suspicious.append({'file_id':f['id'],'reason':f['error']})
    for t in takes:
        if t.get('content_type')=='facial_blendshapes':suspicious.append({'take_id':t['id'],'reason':'Facial blendshape-only animation confirmed; not corrupt, not a Humanoid body motion'})
        elif t.get('static_pose_only'):suspicious.append({'take_id':t['id'],'reason':'Static keyed pose; no varying skeletal channels. Not a moving idle performance.'})
        elif not t['keyed_channel_count'] or t['duration_seconds'] is None or t['duration_seconds']<=0:suspicious.append({'take_id':t['id'],'reason':'No skeletal curves or invalid/zero duration; inspect static pose or unsupported connections'})
        if t['duration_seconds'] and t['duration_seconds']>120:suspicious.append({'take_id':t['id'],'reason':'Long take over 120 seconds; possible compound performance, not corruption'})
    catalog={'schema_version':'1.0','source_root':str(SOURCE),'unknown_value_policy':'null or explicit unknown; no inference of licensing or visual performance','summary':summary,'methodology':{'scanner':'Docs/AnimationAuditTools/fbx_scan.py','builder':'Docs/AnimationAuditTools/build_catalog.py','scope':'Read-only binary FBX skeletal curve/stack inspection, whole-file hashes, local documents; no Unity or animation SDK evaluation','frame_count':'Inclusive round(duration * declared frame rate) + 1; unique_key_time_count separately records actual skeletal curve sample times','finger_animation':'Varying named finger local curves; does not prove intentional expressive finger capture','root_motion':'Raw local root/hip translations only; parent orientations, scale, pivots and global motion not evaluated','unique_estimate':'Whole-take equivalence groups, not independently usable subclips; unresolved variants remain separate','semantic_ids':'Proposals; Unknown posture is deliberately explicit and must be reviewed before import'},'files':files,'skeletons':list(skels.values()),'takes':takes,'motions':motions,'packs':packs,'suspicious_files_or_takes':suspicious}
    duplicates={'schema_version':'1.0','source_root':str(SOURCE),'counting_policy':'Groups overlap between categories; reductions use transitive union, never sum group sizes. Category D and unresolved proposals are not merged. No files deleted.','summary':summary,'file_lookup':{f['id']:f['relative_path'] for f in files},'take_lookup':{t['id']:{'path':fby[t['file_id']]['relative_path'],'name':t['name']} for t in takes},'byte_identical':exact,'equivalent_motion_data':equivalents,'export_variants':variants,'unresolved_export_variants':candidate_variants,'individual_vs_pack':packdupes,'semantically_similar_not_deduplicated':semantic_similar}
    catalog['static_transform_sets']=static_rigs
    summary['content_types']=dict(collections.Counter(t.get('content_type','unknown') for t in takes))
    summary['estimated_unique_skeletal_motions']=sum(any(tby[x].get('content_type')=='skeletal' for x in m['take_ids']) for m in motions)
    summary['static_pose_only_take_groups']=sum(all(tby[x].get('static_pose_only') for x in m['take_ids']) for m in motions)
    summary['estimated_unique_moving_skeletal_motions']=sum(any(tby[x].get('content_type')=='skeletal' and not tby[x].get('static_pose_only') for x in m['take_ids']) for m in motions)
    for g in equivalents:
        sigs={fby[tby[x]['file_id']].get('static_transform_signature') for x in g['take_ids']}
        g['identical_static_transform_sets']=len(sigs)==1
        if len(sigs)>1:g['static_transform_note']='Static transform sets differ; identical local curves are evidence of common source motion, not guaranteed identical evaluated poses.'
    for f in files:
        if f['extension']=='.uasset':f['animation_capability']='Unreal IK/retarget support asset by filename; binary internals not decoded; not counted as motion'
    dump(ROOT/'Docs/AnimationLibraryCatalog.json',catalog);dump(ROOT/'Docs/AnimationLibraryDuplicates.json',duplicates)
    print(json.dumps(summary,indent=2));print('Suspicious',len(suspicious))

if __name__=='__main__':main()
