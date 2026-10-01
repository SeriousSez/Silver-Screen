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
 public static class StageSchoolRuntimeBuilder
 {
  public const string Root="Assets/SilverScreen/Environment/StageSchoolB";
  // Historical A3 runtime: retain its GUID, but exclude it from Resources builds.
  public const string Prefab=Root+"/Archive/StageSchool_Runtime.prefab";
  [Serializable] public class DoorRecord {public string group,hinge;public float[] center;public float width,angle,swing;public bool world;}
  [Serializable] public class SourceReport {public DoorRecord[] doors;}
  [Serializable] public class Metric {public int level,triangles,vertices,renderers,materials,materialSlots;public long meshBytes;}
  [Serializable] public class Metrics {public Metric[] levels;public float[] thresholds;public int anchors,revealGroups;}
  static readonly Dictionary<Mesh,Material[]> MaterialsByMesh=new Dictionary<Mesh,Material[]>();
  static void RoofMaterials()
  {
   const string folder=Root+"/RoofSurfaces";
   foreach(var path in Directory.GetFiles(folder,"*_Albedo.png")){
    string name=System.IO.Path.GetFileNameWithoutExtension(path).Replace("_Albedo","");
    string albedo=folder+"/"+name+"_Albedo.png",normal=folder+"/"+name+"_Normal.png";
    var importer=(TextureImporter)AssetImporter.GetAtPath(normal);importer.textureType=TextureImporterType.NormalMap;importer.sRGBTexture=false;importer.mipmapEnabled=true;importer.wrapMode=TextureWrapMode.Clamp;importer.SaveAndReimport();
    var colour=(TextureImporter)AssetImporter.GetAtPath(albedo);colour.sRGBTexture=true;colour.mipmapEnabled=true;colour.wrapMode=TextureWrapMode.Clamp;colour.SaveAndReimport();
    string target=folder+"/B1Roof_"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(target);
    var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/SilverScreen/Environment/StageSchoolA1/Materials/SS_Tile.mat");
    if(material==null){material=new Material(source);AssetDatabase.CreateAsset(material,target);}else material.CopyPropertiesFromMaterial(source);
    material.name="B1Roof_"+name;material.SetTexture("_BaseMap",AssetDatabase.LoadAssetAtPath<Texture2D>(albedo));material.SetTexture("_BumpMap",AssetDatabase.LoadAssetAtPath<Texture2D>(normal));material.SetTextureScale("_BaseMap",Vector2.one);material.SetTextureOffset("_BaseMap",Vector2.zero);material.SetFloat("_BumpScale",1);material.EnableKeyword("_NORMALMAP");EditorUtility.SetDirty(material);
   }
  }
  static Transform Node(Transform root,string path){var at=root;foreach(var s in path.Split('/')){var t=at.Find(s);if(t==null){t=new GameObject(s).transform;t.SetParent(at,false);}at=t;}return at;}
  static string PathOf(Transform t,Transform root){return AnimationUtility.CalculateTransformPath(t,root);}
  static void Folder(string path){Directory.CreateDirectory(path);AssetDatabase.Refresh();}
  static Mesh Store(string path,Mesh m){m.name=System.IO.Path.GetFileNameWithoutExtension(path);var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null){AssetDatabase.CreateAsset(m,path);return m;}EditorUtility.CopySerialized(m,old);Object.DestroyImmediate(m);EditorUtility.SetDirty(old);return old;}
  static Dictionary<string,Mesh> Import(int level)
  {
   StageSchoolA1Review.Packet packet;using(var zip=new GZipStream(File.OpenRead("ArtExports/StageSchoolB/lod"+level+".json.gz"),CompressionMode.Decompress))using(var r=new StreamReader(zip))packet=JsonUtility.FromJson<StageSchoolA1Review.Packet>(r.ReadToEnd());
   var result=new Dictionary<string,Mesh>();Folder(Root+"/Meshes/LOD"+level);
   foreach(var p in packet.parts){var m=new Mesh{name=p.name+"_LOD"+level,indexFormat=IndexFormat.UInt32};
    m.vertices=Enumerable.Range(0,p.positions.Length/3).Select(i=>new Vector3(p.positions[i*3],p.positions[i*3+1],p.positions[i*3+2])).ToArray();
    m.normals=Enumerable.Range(0,p.normals.Length/3).Select(i=>new Vector3(p.normals[i*3],p.normals[i*3+1],p.normals[i*3+2])).ToArray();
    m.uv=Enumerable.Range(0,p.uv.Length/2).Select(i=>new Vector2(p.uv[i*2],p.uv[i*2+1])).ToArray();m.subMeshCount=p.submeshes.Length;
    for(int s=0;s<p.submeshes.Length;s++)m.SetTriangles(p.submeshes[s].indices,s,false);m.RecalculateBounds();m.RecalculateTangents();
    string source=AssetDatabase.GUIDToAssetPath(p.name);string folder=Root+"/Meshes/LOD"+level;
    if(source.Contains("ProductionEquipment1930Candidate")){folder="Assets/SilverScreen/Environment/ProductionEquipment1930Runtime/LOD"+level;if(!Directory.Exists(folder))Folder(folder);}
    result[p.name]=Store(folder+"/"+p.name+".asset",m);MaterialsByMesh[result[p.name]]=p.materials.Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray();
   }return result;
  }
  static string Semantic(string path)
  {
   var a=path.Split('/');
   if(a[0]=="Doors")return path;
   if(a[0]=="Roofs")return string.Join("/",a.Take(Math.Min(3,a.Length)));
   if(a[0]=="Ceilings")return path;
   if(path.Contains("CurtainSupports"))return path;
   if(a[0]=="Shell"||a[0]=="Partitions")return string.Join("/",a.Take(Math.Min(a[1]=="Raised"?3:2,a.Length)));
   if(a[0]=="Furnishings")return string.Join("/",a.Take(path.Contains("Notice")||path.Contains("PortraitGallery")||path.Contains("Curtain")||path.Contains("Clock")?3:2));
   return a[0]=="Interior"?"Floors":a[0];
  }
  static Bounds LocalBounds(Transform root,IEnumerable<Renderer> rs){var b=new Bounds();bool first=true;foreach(var r in rs){var v=r.localBounds;for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2){var p=root.InverseTransformPoint(r.transform.TransformPoint(v.center+Vector3.Scale(v.extents,new Vector3(x,y,z))));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}}return b;}
  static BoxCollider Box(Transform parent,string name,Vector3 center,Vector3 size){var go=Node(parent,name);go.localPosition=center;var b=go.gameObject.AddComponent<BoxCollider>();b.size=size;return b;}
  [MenuItem("SilverScreen/Stage School B/Build runtime prefab")]
  public static void Build()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required");
   Folder(Root+"/Archive");RoofMaterials();MaterialsByMesh.Clear();var meshes=Enumerable.Range(0,3).Select(Import).ToArray();var preview=EditorSceneManager.NewPreviewScene();
   try{
    var master=AssetDatabase.LoadAssetAtPath<GameObject>(StageSchoolA1Review.Prefab);var root=new GameObject("Stage School");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
    var levels=new GameObject[3];var all=new Dictionary<string,List<Renderer>>();var construction=new Dictionary<ConstructionVisualGroup,List<Renderer>>();var deep=new List<Renderer>();
    for(int l=0;l<3;l++){
     levels[l]=(GameObject)PrefabUtility.InstantiatePrefab(master,preview);PrefabUtility.UnpackPrefabInstance(levels[l],PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);levels[l].name="LOD"+l;levels[l].transform.SetParent(root.transform,false);
     foreach(var b in levels[l].GetComponentsInChildren<MonoBehaviour>(true))Object.DestroyImmediate(b);
     foreach(var c in levels[l].GetComponentsInChildren<Collider>(true))Object.DestroyImmediate(c);
     if(l>0)foreach(var light in levels[l].GetComponentsInChildren<Light>(true))Object.DestroyImmediate(light.gameObject);
     foreach(var f in levels[l].GetComponentsInChildren<MeshFilter>(true)){
      string key=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(f.sharedMesh));f.sharedMesh=meshes[l][key];f.GetComponent<MeshRenderer>().sharedMaterials=MaterialsByMesh[f.sharedMesh];
     }
     if(l>0) Batch(levels[l],l);
     foreach(var f in levels[l].GetComponentsInChildren<MeshFilter>(true)){
      var r=f.GetComponent<MeshRenderer>();string path=PathOf(f.transform,levels[l].transform);string sem=Semantic(path);
      if(!all.ContainsKey(sem))all[sem]=new List<Renderer>();all[sem].Add(r);
      if(l==2&&(path.StartsWith("Furnishings")||path.StartsWith("Ceilings/Pendant")))r.shadowCastingMode=ShadowCastingMode.Off;
      if(path.StartsWith("Furnishings/Staff")||path.StartsWith("Furnishings/Washroom"))deep.Add(r);
      var phase=path.StartsWith("Structure/Foundation")||path.StartsWith("Interior/Floors")?ConstructionVisualGroup.Foundation:path.StartsWith("Partitions")?ConstructionVisualGroup.Structure:path.StartsWith("Roofs")||path.StartsWith("Ceilings")?ConstructionVisualGroup.Roof:path.StartsWith("Shell")?ConstructionVisualGroup.Walls:path.StartsWith("Doors")?ConstructionVisualGroup.Openings:ConstructionVisualGroup.Fixtures;
      if(!construction.ContainsKey(phase))construction[phase]=new List<Renderer>();construction[phase].Add(r);
     }
    }
    var lod=root.AddComponent<LODGroup>();float[] thresholds={.70f,.28f,.008f};lod.SetLODs(levels.Select((g,i)=>new LOD(thresholds[i],g.GetComponentsInChildren<Renderer>(true)){fadeTransitionWidth=.12f}).ToArray());lod.fadeMode=LODFadeMode.CrossFade;lod.animateCrossFading=false;lod.RecalculateBounds();lod.size=7.61f;lod.localReferencePoint=new Vector3(0,3.8f,2.5f);
    var groups=new List<BuildingCutawayController.VisibilityGroup>();
    foreach(var pair in all){string id=pair.Key;var b=LocalBounds(root.transform,pair.Value);var g=new BuildingCutawayController.VisibilityGroup{Id=id,Renderers=pair.Value.ToArray(),LocalSection=b};
     if(id.StartsWith("Roofs")){var a=id.Split('/');g.Roof=true;if(a.Length>2)g.SupportGroupId=string.Join("/",a.Take(2));}
     else if(id.StartsWith("Ceilings")){g.Roof=true;}
     else if(id.StartsWith("Shell/Raised")){g.SupportGroupId=all.Keys.Where(k=>k.StartsWith("Roofs/")&&k.Count(c=>c=='/')==1).OrderBy(k=>(LocalBounds(root.transform,all[k]).center-b.center).sqrMagnitude).First();}
     else if(id.StartsWith("Shell")){g.OutwardNormal=id.Contains("Front")?Vector3.back:id.Contains("Rear")?Vector3.forward:id.Contains("Left")?Vector3.left:Vector3.right;}
     else if(id.StartsWith("Partitions")){g.SightlineOnly=true;}
     else if(id.StartsWith("Doors")){g.SupportGroupId=id.Contains("Entrance")?"Shell/Front":id.Contains("RearService")?"Shell/Rear":null;g.SightlineOnly=g.SupportGroupId==null;}
     else if(id.Contains("Notice")||id.Contains("PortraitGallery")){g.SupportGroupId=all.Keys.Where(k=>k.StartsWith("Shell/")&&!k.Contains("Raised")||k.StartsWith("Partitions/")).OrderBy(k=>LocalBounds(root.transform,all[k]).SqrDistance(b.center)).First();}
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
    foreach(var curtain in groups.Where(g=>g.Id.StartsWith("Furnishings/")&&g.Id.Contains("Curtains")))curtain.SupportGroupId="Shell/Rear/Audition/CurtainSupports";
    var curtainSupport=groups.FirstOrDefault(g=>g.Id=="Shell/Rear/Audition/CurtainSupports");if(curtainSupport!=null)curtainSupport.SupportGroupId="Shell/Rear";
    var reveal=root.AddComponent<BuildingCutawayController>();reveal.Configure(groups.ToArray(),new Bounds(new Vector3(0,2,2.5f),new Vector3(28,4,24)));
    reveal.SetInteriorFocusPoints(new[]{new Vector3(0,1,-6),new Vector3(-7,1,-4),new Vector3(-7,1,1),new Vector3(8,1,8),new Vector3(0,1,8),new Vector3(-10,1,6),new Vector3(-5,1,6),new Vector3(-8,1,11)});
    root.AddComponent<BuildingInteriorVisibility>().Configure(reveal,deep.ToArray());
    var phaseVisual=root.AddComponent<ConstructionPhaseVisuals>();var so=new SerializedObject(phaseVisual);var array=so.FindProperty("_groups");array.arraySize=construction.Count;int ci=0;
    foreach(var p in construction){var e=array.GetArrayElementAtIndex(ci++);e.FindPropertyRelative("Group").enumValueIndex=(int)p.Key;var rr=e.FindPropertyRelative("Renderers");rr.arraySize=p.Value.Count;for(int i=0;i<p.Value.Count;i++)rr.GetArrayElementAtIndex(i).objectReferenceValue=p.Value[i];}so.ApplyModifiedPropertiesWithoutUndo();
    Collision(root,levels[0]);Anchors(root,levels[0]);Doors(root,levels);
    PrefabUtility.SaveAsPrefabAsset(root,Prefab);
    var metrics=new Metrics{thresholds=thresholds,anchors=root.GetComponent<StageSchoolInfrastructure>().Anchors.Count,revealGroups=groups.Count,levels=levels.Select((g,l)=>Measure(g,l)).ToArray()};
    Directory.CreateDirectory("ArtReview/StageSchoolB");File.WriteAllText("ArtReview/StageSchoolB/runtime_metrics.json",JsonUtility.ToJson(metrics,true));AssetDatabase.SaveAssets();
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
   foreach(var f in full.GetComponentsInChildren<MeshFilter>(true)){
    string p=PathOf(f.transform,full.transform);
    if(p.StartsWith("Shell")||p.StartsWith("Partitions")||p.StartsWith("Interior/Floors")||p.StartsWith("Structure/EntranceSteps")||p.Contains("ServiceSteps")){
     // Permanent shell/floor collider copies are independent of rendering LOD/reveal.
     if(p.Contains("Raised")||p.Contains("Signage")||p.Contains("Banners")||p.Contains("Lantern")||p.Contains("Cornice"))continue;
     var t=Node(physical,p);t.SetPositionAndRotation(f.transform.position,f.transform.rotation);t.localScale=f.transform.lossyScale; t.gameObject.AddComponent<MeshCollider>().sharedMesh=f.sharedMesh;
    }
    else if(p.StartsWith("Furnishings")&&(p.Contains("Counter")||p.Contains("Desk")||p.Contains("Table")||p.Contains("Bookcase")||p.Contains("Rack")||p.Contains("Cabinet")||p.Contains("Rostrum"))){
     var b=LocalBounds(root.transform,new[]{f.GetComponent<Renderer>()});if(b.size.x<.3f||b.size.z<.3f)continue;Box(physical,"Furniture/"+p,b.center,b.size);
    }
   }
   var selection=Node(root.transform,"SelectionOnly");selection.gameObject.AddComponent<NavMeshModifier>().ignoreFromBuild=true;var c=Box(selection,"RoofHit",new Vector3(0,6,2.5f),new Vector3(28,.1f,24));c.isTrigger=true;
  }
  static void Anchors(GameObject root,GameObject full)
  {
   var anchors=Node(root.transform,"Anchors");var list=new List<Transform>();
   Transform Add(string n,float x,float z,float y=.36f){var t=Node(anchors,n);t.localPosition=new Vector3(x,y,z);list.Add(t);return t;}
   Add("entrance.approach",0,-12,0);Add("entrance.primary",0,-9);var reception=Add("reception.approach",0,-6.5f);Add("exit",0,-12.5f,0);Add("service.threshold",-7.6f,14);Add("service.approach",-7.6f,16,0);
   for(int i=0;i<8;i++){var chair=full.transform.Find("Furnishings/Waiting/Chair"+i.ToString("00"));if(chair==null)throw new InvalidOperationException("Missing waiting chair");var b=LocalBounds(root.transform,chair.GetComponentsInChildren<Renderer>());var a=Add("waiting.seated."+i,b.center.x,b.center.z,.89f);var forward=i<3?Vector3.right:i<6?Vector3.left:Vector3.back;a.localRotation=Quaternion.LookRotation(forward);Add("waiting.approach."+i,b.center.x+forward.x*.8f,b.center.z+forward.z*.8f,.36f);}
   Add("waiting.overflow.0",-4,-4);Add("waiting.overflow.1",-4,-2.5f);
   Add("audition.performer.a",7.6f,13.2f,.60f);Add("audition.performer.b",9.4f,13.2f,.60f);Add("audition.operator",8.5f,7);Add("audition.evaluator.0",7.5f,4.2f);Add("audition.evaluator.1",9.5f,4.2f);Add("audition.crew.left",5.2f,10);Add("audition.crew.right",11.8f,10);
   Add("interview.candidate",0,7.4f);Add("evaluation.candidate",-10.4f,4.15f);Add("staff.approach",-5,5.5f);Add("records.approach",-10.4f,11);Add("equipment.approach",-4.5f,11);Add("restroom.approach",8,-5.5f);Add("service.passage",-7.6f,10);
   root.AddComponent<StudioBuildingView>().Initialize(BuildingType.StageSchool,"Stage School",reception);
   root.AddComponent<StageSchoolInfrastructure>().Configure(list.ToArray(),new[]{
    Region("ACTOR",8.5f,11,6,4),Region("DIRECTOR",0,8,4,4),Region("EXTRA",-10,6,5,3),Region("CREATE_IMPORT_TALENT",-7,1,5,3),Region("DISMISS_TALENT",0,-4,4,2)});
  }
  static StageSchoolInfrastructure.Region Region(string id,float x,float z,float w,float d)=>new StageSchoolInfrastructure.Region{Id=id,LocalBounds=new Bounds(new Vector3(x,.37f,z),new Vector3(w,.02f,d))};
  static void Doors(GameObject root,GameObject[] levels)
  {
   var data=JsonUtility.FromJson<SourceReport>(File.ReadAllText("ArtSource/StageSchoolA3/generation_report.json"));
   foreach(var d in data.doors.Where(d=>d.world)){
    var leaves=levels.Select(l=>l.transform.Find(d.group)).Where(t=>t!=null).ToArray();if(leaves.Length!=3)continue;
    var node=Node(root.transform,"DoorState/"+d.group);node.localPosition=new Vector3(d.center[0],.36f,d.center[1]);node.localRotation=Quaternion.Euler(0,-d.angle*Mathf.Rad2Deg,0);
    var b=Box(node,"ClosedBarrier",new Vector3(0,1.1f,0),new Vector3(d.width,2.2f,.08f));var mod=b.gameObject.AddComponent<NavMeshModifier>();mod.ignoreFromBuild=true;
    var obstacle=b.gameObject.AddComponent<UnityEngine.AI.NavMeshObstacle>();obstacle.shape=UnityEngine.AI.NavMeshObstacleShape.Box;obstacle.size=b.size;obstacle.carving=true;
    node.gameObject.AddComponent<AuthoredBuildingDoor>().Configure(leaves,-d.swing,Mathf.Abs(d.swing)>1?-d.swing:d.hinge=="right"?90:-90,b,obstacle);
    var portal=node.gameObject.AddComponent<NavMeshLink>();portal.startPoint=new Vector3(0,0,-.85f);portal.endPoint=new Vector3(0,0,.85f);portal.width=Mathf.Max(.1f,d.width-.72f);portal.bidirectional=true;portal.agentTypeID=0;
   }
  }
 }
}
