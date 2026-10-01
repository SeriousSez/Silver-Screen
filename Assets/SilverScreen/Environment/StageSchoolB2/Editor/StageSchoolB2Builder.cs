using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using SilverScreen.Domain;
using SilverScreen.Domain.Buildings;
using SilverScreen.Presentation.Buildings;
using Object=UnityEngine.Object;

namespace SilverScreen.Editor.EnvironmentArt
{
 public static class StageSchoolB2Builder
 {
  public const string Root="Assets/SilverScreen/Environment/StageSchoolB2";
  public const string Prefab=Root+"/Resources/StageSchool_A4_Runtime.prefab";
  [Serializable] public class DoorRecord {public string group,hinge;public float[] center;public float width,angle,swing;public bool world;}
  [Serializable] public class SourceReport {public DoorRecord[] doors;}
  [Serializable] public class Metric {public int level,triangles,vertices,renderers,materials,materialSlots;public long meshBytes;}
  [Serializable] public class Metrics {public Metric[] levels;public float[] thresholds;public int anchors,revealGroups;}
  static readonly Dictionary<Mesh,Material[]> MaterialsByMesh=new Dictionary<Mesh,Material[]>();
  static Transform Node(Transform root,string path){var at=root;foreach(var s in path.Split('/')){var t=at.Find(s);if(t==null){t=new GameObject(s).transform;t.SetParent(at,false);}at=t;}return at;}
  static string PathOf(Transform t,Transform root){return AnimationUtility.CalculateTransformPath(t,root);}
  static void Folder(string path){Directory.CreateDirectory(path);AssetDatabase.Refresh();}
  static Mesh Store(string path,Mesh m){m.name=System.IO.Path.GetFileNameWithoutExtension(path);var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null){AssetDatabase.CreateAsset(m,path);return m;}EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);EditorUtility.SetDirty(old);return old;}
  static Dictionary<string,Mesh> Import(int level)
  {
   StageSchoolA1Review.Packet packet;using(var zip=new GZipStream(File.OpenRead("ArtExports/StageSchoolB2/lod"+level+".json.gz"),CompressionMode.Decompress))using(var r=new StreamReader(zip))packet=JsonUtility.FromJson<StageSchoolA1Review.Packet>(r.ReadToEnd());
   var result=new Dictionary<string,Mesh>();Folder(Root+"/Meshes/LOD"+level);
   foreach(var p in packet.parts){var m=new Mesh{name=p.name+"_LOD"+level,indexFormat=IndexFormat.UInt32};
    m.vertices=Enumerable.Range(0,p.positions.Length/3).Select(i=>new Vector3(p.positions[i*3],p.positions[i*3+1],p.positions[i*3+2])).ToArray();
    m.normals=Enumerable.Range(0,p.normals.Length/3).Select(i=>new Vector3(p.normals[i*3],p.normals[i*3+1],p.normals[i*3+2])).ToArray();
    m.uv=Enumerable.Range(0,p.uv.Length/2).Select(i=>new Vector2(p.uv[i*2],p.uv[i*2+1])).ToArray();m.subMeshCount=p.submeshes.Length;
    for(int s=0;s<p.submeshes.Length;s++)m.SetTriangles(p.submeshes[s].indices,s,false);m.RecalculateBounds();m.RecalculateTangents();
    string source=AssetDatabase.GUIDToAssetPath(p.name);string folder=Root+"/Meshes/LOD"+level;
    result[p.name]=Store(folder+"/"+p.name+".asset",m);MaterialsByMesh[result[p.name]]=p.materials.Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray();
   }return result;
  }
  static string Semantic(string path)
  {
   var a=path.Split('/');
   if(a[0]=="Doors")return path;
   if(a[0]=="Roofs")return string.Join("/",a.Take(Math.Min(3,a.Length)));
   if(a[0]=="Ceilings")return path;
   if(a[0]=="Structure")return path;
   if(path.Contains("CurtainSupports"))return path;
   if(a[0]=="Shell"||a[0]=="Partitions")return string.Join("/",a.Take(Math.Min(a[1]=="Raised"?3:2,a.Length)));
   if(a[0]=="Furnishings")return path;
   return a[0]=="Interior"?"Floors":a[0];
  }
  static Bounds LocalBounds(Transform root,IEnumerable<Renderer> rs){var b=new Bounds();bool first=true;foreach(var r in rs){var v=r.localBounds;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var p=root.InverseTransformPoint(r.transform.TransformPoint(v.center+Vector3.Scale(v.extents,new Vector3(x,y,z))));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;}
  static BoxCollider Box(Transform parent,string name,Vector3 center,Vector3 size){var go=Node(parent,name);go.localPosition=center;var b=go.gameObject.AddComponent<BoxCollider>();b.size=size;return b;}
  [MenuItem("SilverScreen/Stage School B2/Build runtime prefab")]
  public static void Build()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
   Folder(Root+"/Resources");MaterialsByMesh.Clear();var meshes=Enumerable.Range(0,3).Select(Import).ToArray();var preview=EditorSceneManager.NewPreviewScene();
   try{
    var master=AssetDatabase.LoadAssetAtPath<GameObject>(StageSchoolA4Review.Prefab);var root=new GameObject("Stage School");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
    var levels=new GameObject[3];var all=new Dictionary<string,List<Renderer>>();var construction=new Dictionary<ConstructionVisualGroup,List<Renderer>>();var deep=new List<Renderer>();
    for(int l=0;l<3;l++){
     levels[l]=(GameObject)PrefabUtility.InstantiatePrefab(master,preview);PrefabUtility.UnpackPrefabInstance(levels[l],PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);levels[l].name="LOD"+l;levels[l].transform.SetParent(root.transform,false);
     foreach(var b in levels[l].GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
     foreach(var c in levels[l].GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
     foreach(var light in levels[l].GetComponentsInChildren<Light>(true))Object.DestroyImmediate(light.gameObject);
     foreach(var f in levels[l].GetComponentsInChildren<MeshFilter>(true)){
      string key=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(f.sharedMesh));f.sharedMesh=meshes[l][key];f.GetComponent<MeshRenderer>().sharedMaterials=MaterialsByMesh[f.sharedMesh];
     }
     if(l>0) Batch(levels[l],l);
     foreach(var f in levels[l].GetComponentsInChildren<MeshFilter>(true)){
      var r=f.GetComponent<MeshRenderer>();string path=PathOf(f.transform,levels[l].transform);string sem=Semantic(path);
      if(!all.ContainsKey(sem))all[sem]=new List<Renderer>();all[sem].Add(r);
      if(l==2&&(path.StartsWith("Furnishings")||path.StartsWith("Ceilings/Pendant")))r.shadowCastingMode=ShadowCastingMode.Off;
      if(path.Contains("EvaluationMaterials")||path.Contains("Camera/")||path.Contains("StudioLamp")||path.Contains("DeskAccessories"))deep.Add(r);
      var phase=path.StartsWith("Structure/Foundation")||path.StartsWith("Interior/Floors")||path=="Floors"?ConstructionVisualGroup.Foundation:path.StartsWith("Partitions")?ConstructionVisualGroup.Structure:path.StartsWith("Roofs")||path.StartsWith("Ceilings")?ConstructionVisualGroup.Roof:path.StartsWith("Shell")?ConstructionVisualGroup.Walls:path.StartsWith("Doors")?ConstructionVisualGroup.Openings:ConstructionVisualGroup.Fixtures;
      if(!construction.ContainsKey(phase))construction[phase]=new List<Renderer>();construction[phase].Add(r);
     }
    }
    var lod=root.AddComponent<LODGroup>();float[] thresholds={.38f,.16f,.008f};lod.SetLODs(levels.Select((g,i)=>new LOD(thresholds[i],g.GetComponentsInChildren<Renderer>(true)){fadeTransitionWidth=.12f}).ToArray());lod.fadeMode=LODFadeMode.CrossFade;lod.animateCrossFading=false;lod.RecalculateBounds();lod.size=5.95f;lod.localReferencePoint=new Vector3(0,2.98f,-1);
    var groups=new List<BuildingCutawayController.VisibilityGroup>();
    foreach(var pair in all){string id=pair.Key;var b=LocalBounds(root.transform,pair.Value);var g=new BuildingCutawayController.VisibilityGroup{Id=id,Renderers=pair.Value.ToArray(),LocalSection=b};
     if(id.StartsWith("Roofs")){g.Roof=true;}
     else if(id.StartsWith("Ceilings")){g.Roof=true;}
     else if(id.StartsWith("Shell/Raised")){g.SupportGroupId=all.Keys.Where(k=>k.StartsWith("Roofs/")&&k.Count(c=>c=='/')==1).OrderBy(k=>(LocalBounds(root.transform,all[k]).center-b.center).sqrMagnitude).First();}
     else if(id.StartsWith("Shell")){g.OutwardNormal=id.Contains("Front")?Vector3.back:id.Contains("Rear")?Vector3.forward:id.Contains("Left")?Vector3.left:Vector3.right;}
     else if(id.StartsWith("Partitions")){g.SightlineOnly=true;}
     else if(id.StartsWith("Doors")){g.SupportGroupId=id.Contains("Entrance")?"Shell/Front":id.Contains("RearService")?"Shell/Rear":null;g.SightlineOnly=g.SupportGroupId==null;}
     else if(id.Contains("Notice")||id.Contains("PortraitGallery")||id.Contains("Clock")||id.Contains("WallLantern")){g.SupportGroupId=all.Keys.Where(k=>k.StartsWith("Shell/")&&!k.Contains("Raised")||k.StartsWith("Partitions/")).OrderBy(k=>LocalBounds(root.transform,all[k]).SqrDistance(b.center)).First();}
     groups.Add(g);
    }
    // Explicit support chains remain stable across LODs. Mount is an ownership
    // node: mount hardware and fixture share their authored renderer, with no
    // duplicated draw or independent floating visibility state.
    foreach(var pendant in groups.Where(g=>g.Id.StartsWith("Ceilings/")&&g.Id.Contains("Pendant")).ToArray()){
     var ceiling=groups.Where(g=>g.Id.StartsWith("Ceilings/")&&!g.Id.Contains("Pendant")).OrderBy(g=>g.LocalSection.SqrDistance(pendant.LocalSection.center)).First();
     var mount=new BuildingCutawayController.VisibilityGroup{Id=pendant.Id+"/Mount",SupportGroupId=ceiling.Id,LocalSection=pendant.LocalSection};
     groups.Add(mount);pendant.Roof=false;pendant.SupportGroupId=mount.Id;
    }
    groups.Add(new BuildingCutawayController.VisibilityGroup{Id="Audition/CurtainMount",SupportGroupId="Shell/Rear"});
    foreach(var curtain in groups.Where(g=>g.Id.Contains("Curtains")||g.Id.Contains("Backdrop")))curtain.SupportGroupId="Audition/CurtainMount";
    foreach(var door in groups.Where(g=>g.Id=="Doors/AuditionA4"||g.Id=="Doors/InterviewA4"))door.SupportGroupId=door.Id.Contains("Audition")?"Partitions/HallAudition":"Partitions/HallInterview";
    var reveal=root.AddComponent<BuildingCutawayController>();reveal.Configure(groups.ToArray(),new Bounds(new Vector3(0,2.5f,-1),new Vector3(22,5,16)));
    reveal.SetInteriorFocusPoints(new[]{new Vector3(-7,1,-5),new Vector3(0,1,-5),new Vector3(7,1,-5),new Vector3(-4,1,3),new Vector3(6.6f,1,3)});
    root.AddComponent<BuildingInteriorVisibility>().Configure(reveal,deep.ToArray());
    var phaseVisual=root.AddComponent<ConstructionPhaseVisuals>();var so=new SerializedObject(phaseVisual);var array=so.FindProperty("_groups");array.arraySize=construction.Count;int ci=0;
    foreach(var p in construction){var e=array.GetArrayElementAtIndex(ci++);e.FindPropertyRelative("Group").enumValueIndex=(int)p.Key;var rr=e.FindPropertyRelative("Renderers");rr.arraySize=p.Value.Count;for(int i=0;i<p.Value.Count;i++)rr.GetArrayElementAtIndex(i).objectReferenceValue=p.Value[i];}so.ApplyModifiedPropertiesWithoutUndo();
    Collision(root,levels[0]);Anchors(root,levels[0]);Doors(root,levels);
    PrefabUtility.SaveAsPrefabAsset(root,Prefab);
    var metrics=new Metrics{thresholds=thresholds,anchors=root.GetComponent<StageSchoolInfrastructure>().Anchors.Count,revealGroups=groups.Count,levels=levels.Select((g,l)=>Measure(g,l)).ToArray()};
    Directory.CreateDirectory("ArtReview/StageSchoolB2");File.WriteAllText("ArtReview/StageSchoolB2/runtime_metrics.json",JsonUtility.ToJson(metrics,true));AssetDatabase.SaveAssets();
   }finally{EditorSceneManager.ClosePreviewScene(preview);}
  }
  static Metric Measure(GameObject g,int l){var rs=g.GetComponentsInChildren<MeshRenderer>(true);var ms=g.GetComponentsInChildren<MeshFilter>(true).Select(f=>f.sharedMesh).ToArray();return new Metric{level=l,triangles=ms.Sum(m=>m.triangles.Length/3),vertices=ms.Sum(m=>m.vertexCount),renderers=rs.Length,materials=rs.SelectMany(r=>r.sharedMaterials).Distinct().Count(),materialSlots=rs.Sum(r=>r.sharedMaterials.Length),meshBytes=ms.Distinct().Sum(m=>(long)m.vertexCount*48+m.triangles.Length*4)};}
  static void Batch(GameObject level,int index)
  {
   // Reversible semantic batches only. Door pivots and reusable production equipment stay independent.
   var candidates=level.GetComponentsInChildren<MeshRenderer>(true).Where(r=>!PathOf(r.transform,level.transform).StartsWith("Doors")&&!PathOf(r.transform,level.transform).Contains("StudioCamera")&&!PathOf(r.transform,level.transform).Contains("StudioLamp")).ToArray();
   foreach(var bucket in candidates.GroupBy(r=>Semantic(PathOf(r.transform,level.transform))).Where(g=>g.Count()>1)){
    var sources=bucket.ToArray();var materials=sources.SelectMany(r=>r.sharedMaterials).Distinct().ToArray();var temp=new List<Mesh>();
    foreach(var mat in materials){var combine=new List<CombineInstance>();foreach(var r in sources){var mesh=r.GetComponent<MeshFilter>().sharedMesh;for(int s=0;s<r.sharedMaterials.Length;s++)if(r.sharedMaterials[s]==mat)combine.Add(new CombineInstance{mesh=mesh,subMeshIndex=s,transform=level.transform.worldToLocalMatrix*r.transform.localToWorldMatrix});}
     var part=new Mesh{indexFormat=IndexFormat.UInt32};part.CombineMeshes(combine.ToArray(),true,true);temp.Add(part);}
    var final=new Mesh{indexFormat=IndexFormat.UInt32,name=bucket.Key};final.CombineMeshes(temp.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray(),false,false);
    foreach(var r in sources){Object.DestroyImmediate(r.GetComponent<MeshFilter>());Object.DestroyImmediate(r);}
    var t=Node(level.transform,bucket.Key);t.localPosition=Vector3.zero;t.localRotation=Quaternion.identity;
    t.gameObject.AddComponent<MeshFilter>().sharedMesh=Store(Root+"/Meshes/LOD"+index+"/Batch_"+bucket.Key.Replace('/','_')+".asset",final);t.gameObject.AddComponent<MeshRenderer>().sharedMaterials=materials;
    foreach(var m in temp)Object.DestroyImmediate(m);
   }
  }
  static void Collision(GameObject root,GameObject full)
  {
   var physical=Node(root.transform,"Navigation/Physical");
   Box(physical,"MainSlab",new Vector3(0,.19f,-1),new Vector3(22,.38f,16));
   Box(physical,"EntranceForecourt",new Vector3(0,.025f,-11.12f),new Vector3(7.55f,.05f,.86f));
   foreach(float x in new[]{-7.2f,7.2f})Box(physical,"ApplicantPocket"+x,new Vector3(x,.025f,-10.2f),new Vector3(6.45f,.05f,2.7f));
   Box(physical,"Left",new Vector3(-10.84f,2.6f,-.98f),new Vector3(.32f,4.6f,15.64f));
   Box(physical,"Right",new Vector3(10.84f,2.6f,-.98f),new Vector3(.32f,4.6f,15.64f));
   Box(physical,"Rear",new Vector3(0,2.6f,6.84f),new Vector3(22,.0f+4.6f,.32f));
   // Simple wall proxies retain the three real circulation openings.
   void Wall(string n,float a,float b,float z,float h=4.4f,float thickness=.18f){Box(physical,n,new Vector3((a+b)/2,.38f+h/2,z),new Vector3(b-a,h,thickness));}
   Wall("FrontLeft",-11,-1.57f,-8.92f,4.6f,.32f);Wall("FrontRight",1.57f,11,-8.92f,4.6f,.32f);
   Box(physical,"EntranceHeader",new Vector3(0,4.1f,-8.92f),new Vector3(3.14f,2.2f,.32f));
   Wall("AuditionLeft",-10.84f,-.29f,-1.5f);Wall("AuditionRight",1.31f,2.6f,-1.5f);
   Wall("InterviewLeft",2.6f,4.925f,-1.5f);Wall("InterviewRight",6.275f,10.84f,-1.5f);
   Box(physical,"SpecialistDivider",new Vector3(2.6f,2.55f,2.67f),new Vector3(.18f,4.34f,8.34f));
   Box(physical,"AuditionHeader",new Vector3(.51f,3.95f,-1.5f),new Vector3(1.6f,1.5f,.18f));
   Box(physical,"InterviewHeader",new Vector3(5.6f,3.95f,-1.5f),new Vector3(1.35f,1.5f,.18f));
   // Three shallow stair treads, independent of ornate rail/render geometry.
   for(int i=0;i<3;i++)Box(physical,"Stair"+i,new Vector3(0,(i+1)*.06f,-9.85f+i*.3f),new Vector3(6.4f-i*.4f,(i+1)*.12f,1.7f));
   Box(physical,"Landing",new Vector3(0,.19f,-9.05f),new Vector3(3.8f,.38f,1.0f));
   foreach(var f in full.GetComponentsInChildren<MeshFilter>(true)){
    string p=PathOf(f.transform,full.transform);
    if(!(p.StartsWith("Furnishings")||p.StartsWith("Exterior")))continue;
    if(!(p.Contains("Desk")&&!p.Contains("Accessories")||p.Contains("Table")||p.Contains("Bookcase")||p.Contains("Rostrum")||p.Contains("Bench")||p.Contains("Cabinet")))continue;
    var b=LocalBounds(root.transform,new[]{f.GetComponent<Renderer>()});if(b.size.x<.3f||b.size.z<.3f)continue;
    // Furniture is a ground-connected blocker, not a floating tabletop collider.
    float floor=p.StartsWith("Exterior")?.05f:.38f;b.SetMinMax(new Vector3(b.min.x,floor,b.min.z),b.max);
    Box(physical,"Furniture/"+p,b.center,b.size);
   }
   foreach(float x in new[]{-10.18f,-4.15f,4.15f,10.18f})Box(physical,"Pot"+x,new Vector3(x,.43f,-9.35f),new Vector3(.74f,.76f,.74f));
   var selection=Node(root.transform,"SelectionOnly");selection.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild=true;
   Box(selection,"RoofHit",new Vector3(0,5,-1),new Vector3(22,.1f,16)).isTrigger=true;
  }
  static void Anchors(GameObject root,GameObject full)
  {
   var anchors=Node(root.transform,"Anchors");var list=new List<Transform>();
   Transform Add(string n,float x,float z,float y=.38f){var t=Node(anchors,n);t.localPosition=new Vector3(x,y,z);list.Add(t);return t;}
   Add("entrance.approach",0,-12.8f,0);Add("entrance.primary",0,-9);Add("exit",0,-12.8f,0);
   // Passive attachment metadata: the future path network owns spline behavior.
   Add("path.connection.main",0,-11.55f,.05f).localRotation=Quaternion.Euler(0,180,0);
   var hall=Add("hiringhall.approach",0,-5);
   Add("audition.performer.a",-4.85f,4.7f,.73f);Add("audition.performer.b",-3.15f,4.7f,.73f);
   Add("audition.camera",-4,1.1f,.39f);Add("audition.operator",-4,-.05f,.39f);
   Add("audition.evaluator.0",-8.85f,-.55f,.39f);Add("audition.evaluator.1",-8,-.55f,.39f);Add("audition.evaluator.2",-7.15f,-.55f,.39f);
   Add("audition.activity.screen-test",-.7f,1.25f);Add("audition.crew.left",-7.4f,3.3f);Add("audition.crew.right",-.6f,3.3f);
   Add("interview.applicant",5.85f,2.3f);Add("interview.applicant.1",7.35f,2.3f);
   Add("interview.interviewer",6.6f,4.6f,.39f);Add("interview.activity",4.6f,1.4f);
   float[] xs={-9.2f,-7.2f,-5.2f,5.2f,7.2f,9.2f};for(int i=0;i<xs.Length;i++)Add("applicant.exterior."+i,xs[i],-10.55f,.05f);
   root.AddComponent<StudioBuildingView>().Initialize(BuildingType.StageSchool,"Stage School",hall);
   string[] names={"ACTOR","DIRECTOR","EXTRA","TALENT_CONTEXT"};
   var regions=names.Select((n,i)=>new StageSchoolInfrastructure.Region{Id=n,LocalBounds=new Bounds(new Vector3(-7.2f+4.8f*i,.38f,-5.45f),new Vector3(3.4f,.02f,3.2f)),ApplicantContext=i==3?"CREATE_IMPORT_TALENT":n,EmployeeContext=i==3?"DISMISS":n}).ToArray();
   root.AddComponent<StageSchoolInfrastructure>().Configure(list.ToArray(),regions);
  }
  static void Doors(GameObject root,GameObject[] levels)
  {
   var source=JsonUtility.FromJson<SourceReport>(File.ReadAllText("ArtSource/StageSchoolA4/generation_report.json"));
   var entrance=JsonUtility.FromJson<SourceReport>(File.ReadAllText("ArtSource/StageSchoolA3/generation_report.json"));
   foreach(var d in source.doors.Concat(entrance.doors.Where(d=>d.group.Contains("Entrance"))).Where(d=>d.world)){
    var leaves=levels.Select(l=>l.transform.Find(d.group)).Where(t=>t!=null).ToArray();if(leaves.Length!=3)throw new InvalidOperationException("Missing A4 door "+d.group);
    var node=Node(root.transform,"DoorState/"+d.group);node.localPosition=new Vector3(d.center[0],.38f,d.center[1]);node.localRotation=Quaternion.Euler(0,-d.angle*Mathf.Rad2Deg,0);
    var b=Box(node,"ClosedBarrier",new Vector3(0,1.1f,0),new Vector3(d.width,2.2f,.08f));b.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild=true;
    var obstacle=b.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;obstacle.size=b.size;obstacle.carving=true;
    var door=node.gameObject.AddComponent<AuthoredBuildingDoor>();door.Configure(leaves,-d.swing,Mathf.Abs(d.swing)>1?-d.swing:d.hinge=="right"?90:-90,b,obstacle);door.SetOpen(false);
    var portal=node.gameObject.AddComponent<NavMeshLink>();portal.startPoint=new Vector3(0,0,-.85f);portal.endPoint=new Vector3(0,0,.85f);portal.width=Mathf.Max(.1f,d.width-.72f);portal.bidirectional=true;portal.agentTypeID=0;
   }
  }
 }
}
