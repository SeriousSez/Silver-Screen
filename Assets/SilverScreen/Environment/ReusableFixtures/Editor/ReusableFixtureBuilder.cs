using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SilverScreen.Domain.ReusableAssets;
using SilverScreen.Domain;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.ReusableAssets;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.ReusableAssets
{
    /// <summary>Explicit, scoped importer for the independent M1 packet. Never regenerates Stage 1.</summary>
    public static class ReusableFixtureBuilder
    {
        public const string Root = "Assets/SilverScreen/Environment/ReusableFixtures";
        public const string ExportRoot = "ArtExports/ReusableFixtures";
        public const string ReviewRoot = "ArtReview/ReusableFixtures/Milestone1";
        public const string ScenePath = Root + "/Validation/ReusableFixtures_Milestone1.unity";
        private const string SourceMaterials = "Assets/SilverScreen/Environment/Stage1CleanCandidate/Materials/";
        [Serializable] public class Member { public string name, semanticGroup, uvSha256, positionSha256; public int vertices, triangles; }
        [Serializable] public class Part { public string name; public float[] positions, normals, uv; public string[] materials; public Submesh[] submeshes; public Member[] members; public int vertices, triangles; }
        [Serializable] public class Submesh { public int[] indices; }
        [Serializable] public class PacketAsset { public string name; public Part[] parts; }
        [Serializable] public class Packet { public PacketAsset[] assets; }
        [Serializable] public class Anchor { public string name, kind; public float[] position, forward; public float diameter; }
        [Serializable] public class Record
        {
            public string name, label, family, id, surface, suitability, hand, status, interfaceAddition;
            public bool player;
            public float clearance;
            public float[] origin, basisRows, boundsMin, boundsMax, dimensions;
            public Part[] parts;
            public Anchor[] anchors;
        }
        [Serializable] public class Manifest { public int schema; public string masterCommit, source, sourceSha256; public Record[] assets; }
        public static Manifest ReadManifest() => JsonUtility.FromJson<Manifest>(File.ReadAllText(ExportRoot+"/milestone1_manifest.json"));
        public static Packet ReadPacket()
        {
            using var file = File.OpenRead(ExportRoot+"/milestone1_meshes.json.gz");
            using var zip = new GZipStream(file, CompressionMode.Decompress);
            using var reader = new StreamReader(zip);
            return JsonUtility.FromJson<Packet>(reader.ReadToEnd());
        }
        public static Vector3 V(float[] a) => new Vector3(a[0],a[1],a[2]);
        private static Vector3[] Vectors(float[] values) => Enumerable.Range(0,values.Length/3).Select(i=>new Vector3(values[i*3],values[i*3+1],values[i*3+2])).ToArray();
        public static string PrefabPath(string name) => Root+"/Prefabs/"+name+".prefab";
        public static string MeshPath(string name,string part) => Root+"/Meshes/"+name+"_"+part+".asset";
        private static void Folder(string relative)
        {
            var parts=relative.Split('/');string path=parts[0];
            foreach(var part in parts.Skip(1)) { if(!AssetDatabase.IsValidFolder(path+"/"+part)) AssetDatabase.CreateFolder(path,part);path+="/"+part; }
        }
        private static T Asset<T>(string path, Func<T> create) where T:Object
        {
            var asset=AssetDatabase.LoadAssetAtPath<T>(path);
            if(asset==null) { asset=create();AssetDatabase.CreateAsset(asset,path); }
            return asset;
        }
        private static void Edit(Object obj,Action<SerializedObject> action)
        { var so=new SerializedObject(obj);action(so);so.ApplyModifiedPropertiesWithoutUndo();EditorUtility.SetDirty(obj); }
        private static void Strings(SerializedProperty prop,IEnumerable<string> values)
        { var a=values.ToArray();prop.arraySize=a.Length;for(int i=0;i<a.Length;i++)prop.GetArrayElementAtIndex(i).stringValue=a[i]; }
        private static GameObject Child(string name,Transform parent,Vector3 position=default)
        { var o=new GameObject(name);o.transform.SetParent(parent,false);o.transform.localPosition=position;return o; }
        private static Material Material(string role)
        {
            var mat=AssetDatabase.LoadAssetAtPath<Material>(SourceMaterials+role+".mat");
            if(mat==null)throw new InvalidOperationException("Missing approved material "+role);
            return mat;
        }
        private static Bounds BoundsOf(Record r) => new Bounds((V(r.boundsMin)+V(r.boundsMax))/2,V(r.boundsMax)-V(r.boundsMin));

        [MenuItem("SilverScreen/Art/Reusable Fixtures/1 Build Milestone 1 assets")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Build in Edit Mode.");
            foreach(var folder in new[]{"Meshes","Prefabs","Definitions","Materials","Textures","Editor/Provenance","Validation"})Folder(Root+"/"+folder);
            var manifest=ReadManifest();var packet=ReadPacket();
            if(manifest.masterCommit!="927ad371a96c4044689286473b542febac6a7265")throw new InvalidOperationException("Unexpected visual master.");
            using(var sha=System.Security.Cryptography.SHA256.Create())
            using(var source=File.OpenRead(manifest.source))
                if(BitConverter.ToString(sha.ComputeHash(source)).Replace("-","").ToLowerInvariant()!=manifest.sourceSha256)
                    throw new InvalidOperationException("Source changed since extraction; explicit new review required.");
            BuildDisplayMaterial();
            foreach(string texture in new[]{"StudioPoster","StudioNotice"})
            {
                var importer=AssetImporter.GetAtPath(Root+"/Textures/"+texture+".png") as TextureImporter;
                if(importer!=null) {importer.npotScale=TextureImporterNPOTScale.None;importer.wrapMode=TextureWrapMode.Clamp;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();}
            }
            var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                foreach(var r in manifest.assets)
                {
                    var data=packet.assets.Single(p=>p.name==r.name);
                    var root=new GameObject(r.name);SceneManager.MoveGameObjectToScene(root,preview);
                    var geometry=Child("Geometry",root.transform);
                    foreach(var p in data.parts)
                    {
                        var mesh=Asset(MeshPath(r.name,p.name),()=>new Mesh());mesh.Clear();mesh.name=r.name+"_"+p.name;
                        mesh.indexFormat=p.positions.Length/3>65535?IndexFormat.UInt32:IndexFormat.UInt16;
                        mesh.vertices=Vectors(p.positions);mesh.normals=Vectors(p.normals);
                        mesh.uv=Enumerable.Range(0,p.uv.Length/2).Select(i=>new Vector2(p.uv[2*i],p.uv[2*i+1])).ToArray();
                        mesh.subMeshCount=p.submeshes.Length;
                        for(int i=0;i<p.submeshes.Length;i++)mesh.SetTriangles(p.submeshes[i].indices,i,false);
                        mesh.RecalculateBounds();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
                        var child=Child(p.name,geometry.transform);child.AddComponent<MeshFilter>().sharedMesh=mesh;
                        child.AddComponent<MeshRenderer>().sharedMaterials=p.materials.Select(Material).ToArray();
                    }
                    var anchors=Child("Anchors",root.transform);
                    bool hinge=r.name.StartsWith("PersonnelHinge");
                    AddAnchor(anchors.transform,new Anchor{name="Mount",kind=hinge?"Pivot":"Mount",position=new float[]{0,0,0},forward=hinge||r.surface=="Ceiling"?new float[]{0,1,0}:r.surface=="Ground"?new float[]{0,-1,0}:new float[]{0,0,-1}});
                    foreach(var anchor in r.anchors)AddAnchor(anchors.transform,anchor);
                    AddColliders(root,r);
                    if(r.family=="Lighting")AddLight(root,r);
                    if(r.family=="Displays")AddDisplay(root,r);
                    var definition=Asset(Root+"/Definitions/"+r.name+".asset",ScriptableObject.CreateInstance<ReusableAssetDefinition>);
                    ConfigureDefinition(definition,r);
                    Edit(root.AddComponent<ReusableFixture>(),so=>so.FindProperty("_definition").objectReferenceValue=definition);
                    var prefab=PrefabUtility.SaveAsPrefabAsset(root,PrefabPath(r.name));
                    Edit(definition,so=>so.FindProperty("_prefab").objectReferenceValue=prefab);AssetDatabase.SaveAssetIfDirty(definition);
                    var provenance=Asset(Root+"/Editor/Provenance/"+r.name+".asset",ScriptableObject.CreateInstance<ReusableAssetProvenance>);
                    Edit(provenance,so=>{
                        so.FindProperty("_masterCommit").stringValue=manifest.masterCommit;so.FindProperty("_sourcePath").stringValue=manifest.source;
                        so.FindProperty("_sourceSha256").stringValue=manifest.sourceSha256;so.FindProperty("_canonicalId").stringValue=r.id;
                        so.FindProperty("_recordJson").stringValue=JsonUtility.ToJson(r,true);so.FindProperty("_prefab").objectReferenceValue=prefab;
                    });AssetDatabase.SaveAssetIfDirty(provenance);Object.DestroyImmediate(root);
                }
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
            Debug.Log("Reusable M1 built "+manifest.assets.Length+" independent prefabs; approved master is read-only.");
        }
        private static void ConfigureDefinition(ReusableAssetDefinition definition,Record r)
        {
            Edit(definition,so=>{
                so.FindProperty("_stableId").stringValue=r.id;so.FindProperty("_displayName").stringValue=r.label;
                so.FindProperty("_category").stringValue=r.family=="DoorHardware"||r.family=="ControlHardware"?"Authoring Subassemblies":"Fixtures";
                so.FindProperty("_subcategory").stringValue=r.family;
                so.FindProperty("_playerCatalogItem").boolValue=r.player;so.FindProperty("_availableFromYear").intValue=1930;
                so.FindProperty("_hasAvailableUntilYear").boolValue=false;
                so.FindProperty("_placementSurface").enumValueIndex=(int)Enum.Parse<PlacementSurface>(r.surface);
                so.FindProperty("_suitability").enumValueIndex=(int)Enum.Parse<FixtureSuitability>(r.suitability);
                so.FindProperty("_handedness").enumValueIndex=(int)Enum.Parse<FixtureHandedness>(r.hand);
                so.FindProperty("_role").enumValueIndex=(int)(r.player?FixtureRole.Both:FixtureRole.Architectural);
                so.FindProperty("_visibleBounds").boundsValue=BoundsOf(r);
                // Service clearances are authoring guidance, not blocking colliders.
                so.FindProperty("_functionalClearance").boundsValue=r.clearance>0?new Bounds(new Vector3(0,0,r.boundsMax[2]+r.clearance/2),new Vector3(Mathf.Max(r.dimensions[0],.45f),Mathf.Max(r.dimensions[1],.45f),r.clearance)):new Bounds(Vector3.zero,Vector3.zero);
                var roles=r.parts.SelectMany(p=>p.materials);
                if(r.family=="Displays")roles=roles.Concat(new[]{"DisplayInsert","C1_RoofTimber"});
                Strings(so.FindProperty("_materialRoles"),roles.Distinct());
                Strings(so.FindProperty("_environmentTags"),new[]{"Studio","Industrial","Utility","Period and later settings"});
                so.FindProperty("_optionalInteraction").stringValue=r.family=="Lighting"?"Representational light switch":r.family=="Displays"?"Per-instance artwork and insert swap":"None; controls remain representational";
            });
        }
        private static void AddAnchor(Transform parent,Anchor data)
        {
            var obj=Child(data.name,parent,V(data.position));var forward=V(data.forward).normalized;
            obj.transform.localRotation=Quaternion.LookRotation(forward,Mathf.Abs(Vector3.Dot(forward,Vector3.up))>.99f?Vector3.forward:Vector3.up);
            Edit(obj.AddComponent<FixtureAnchor>(),so=>{so.FindProperty("_kind").enumValueIndex=(int)Enum.Parse<FixtureAnchorKind>(data.kind);so.FindProperty("_nominalDiameter").floatValue=data.diameter;});
        }
        private static void AddColliders(GameObject root,Record r)
        {
            if(!r.player)return; // Hardware receives collision from its eventual doorway/control assembly.
            var obj=Child("Collision",root.transform);
            if(r.family=="SiteProtection")
            {
                var post=obj.AddComponent<CapsuleCollider>();post.direction=1;post.radius=.075f;post.height=1.31f;post.center=new Vector3(0,.655f,0);
                var foot=obj.AddComponent<BoxCollider>();foot.size=new Vector3(.26f,.06f,.26f);foot.center=new Vector3(0,.03f,0);
            }
            else if(r.family=="Lighting")
            {
                var mount=obj.AddComponent<BoxCollider>();mount.center=r.name.StartsWith("Goose")?new Vector3(0,0,.03f):new Vector3(0,-.0325f,0);
                mount.size=r.name.StartsWith("Goose")?new Vector3(.13f,.27f,.06f):new Vector3(.10f,.065f,.11f);
                // Simple shade volume; no expensive mesh collider or false wall-to-shade slab.
                var shade=Child("ShadeCollision",obj.transform).AddComponent<BoxCollider>();
                shade.center=r.name.StartsWith("Goose")?new Vector3(0,.055f,.46f):new Vector3(0,-.5485f,0);
                shade.size=r.name.StartsWith("Goose")?new Vector3(.39f,.21f,.39f):new Vector3(.508f,.282f,.508f);
            }
            else
            {
                var bounds=BoundsOf(r);var box=obj.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;
                // The pullbox sleeve penetrates the host wall; collision covers only its enclosure.
                if(r.name=="PullBox_1930") {box.center=new Vector3(0,0,.095f);box.size=new Vector3(.31f,.40f,.19f);}
            }
        }
        private static void AddLight(GameObject root,Record r)
        {
            var anchor=r.anchors.Single(a=>a.kind=="Light");var obj=Child("OptionalLight",root.transform,V(anchor.position));
            obj.transform.localRotation=Quaternion.LookRotation(V(anchor.forward));var light=obj.AddComponent<Light>();
            light.type=LightType.Spot;light.color=new Color(1,.78f,.53f);light.intensity=r.name.StartsWith("Goose")?7:32;
            light.range=r.name.StartsWith("Goose")?6:12;light.spotAngle=r.name.StartsWith("Goose")?110:115;light.shadows=LightShadows.None;light.enabled=false;
            Edit(obj.AddComponent<FixtureLightSwitch>(),so=>so.FindProperty("_light").objectReferenceValue=light);
        }
        private static void BuildDisplayMaterial()
        {
            var mat=Asset(Root+"/Materials/DisplayInsert.mat",()=>new Material(Shader.Find("Universal Render Pipeline/Lit")));
            mat.SetColor("_BaseColor",Color.white);mat.SetFloat("_Metallic",0);mat.SetFloat("_Smoothness",.18f);
            EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
            var quad=Asset(Root+"/Meshes/DisplayInsert_UnitQuad.asset",()=>new Mesh());quad.Clear();quad.name="DisplayInsert_UnitQuad";
            quad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(-.5f,.5f,0),new Vector3(.5f,.5f,0),new Vector3(.5f,-.5f,0)};
            // Viewed from the +Z mounting front, Unity camera-right is local -X.
            quad.normals=Enumerable.Repeat(Vector3.forward,4).ToArray();quad.uv=new[]{Vector2.right,Vector2.one,Vector2.up,Vector2.zero};
            quad.triangles=new[]{0,2,1,0,3,2};quad.RecalculateBounds();quad.RecalculateTangents();EditorUtility.SetDirty(quad);AssetDatabase.SaveAssetIfDirty(quad);
        }
        private static MeshRenderer Plane(string name,Transform parent,float z)
        {
            var obj=Child(name,parent,new Vector3(0,0,z));obj.AddComponent<MeshFilter>().sharedMesh=AssetDatabase.LoadAssetAtPath<Mesh>(Root+"/Meshes/DisplayInsert_UnitQuad.asset");
            var renderer=obj.AddComponent<MeshRenderer>();renderer.sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"/Materials/DisplayInsert.mat");return renderer;
        }
        private static void AddDisplay(GameObject root,Record r)
        {
            bool dual=r.name.Contains("Double");var geo=root.transform.Find("Geometry");
            var frame=root.AddComponent<DisplayFrame>();var slots=new List<DisplayInsertSlot>();
            Transform backing=geo.Find("Backing");
            if(!dual)
            {
                // Removable canonical graphic backing; source acoustic mesh/frame remain exact.
                var board=GameObject.CreatePrimitive(PrimitiveType.Cube);board.name="GraphicBacking";board.transform.SetParent(geo,false);
                board.transform.localPosition=new Vector3(0,0,.07f);board.transform.localScale=new Vector3(2.65f,2.05f,.12f);
                Object.DestroyImmediate(board.GetComponent<Collider>());board.GetComponent<MeshRenderer>().sharedMaterial=Material("C1_RoofTimber");backing=board.transform;
            }
            foreach(var anchor in r.anchors.Where(a=>a.kind=="Insert"))
            {
                int index=slots.Count;var socket=Child(anchor.name+"_Socket",root.transform,V(anchor.position));
                var mat=Plane("Mat",socket.transform,0);var art=Plane("Artwork",socket.transform,.0008f);
                var acoustic=geo.Find(dual?(index==0?"AcousticLeft":"AcousticRight"):"Acoustic");
                var slot=socket.AddComponent<DisplayInsertSlot>();
                Edit(slot,so=>{
                    so.FindProperty("_aperture").vector2Value=dual?new Vector2(1.081f,1.725f):new Vector2(2.615f,2.05f);
                    so.FindProperty("_insertionDepth").floatValue=dual?.024f:.054f;
                    so.FindProperty("_acousticInsert").objectReferenceValue=acoustic.gameObject;
                    if(!dual)so.FindProperty("_graphicBacking").objectReferenceValue=backing.gameObject;
                    so.FindProperty("_matRenderer").objectReferenceValue=mat;so.FindProperty("_artworkRenderer").objectReferenceValue=art;
                });slot.Refresh();slots.Add(slot);
            }
            Edit(frame,so=>{
                so.FindProperty("_backing").objectReferenceValue=backing;so.FindProperty("_outerFrame").objectReferenceValue=geo.Find("OuterFrame");
                so.FindProperty("_optionalDivider").objectReferenceValue=geo.Find("Divider");
                var array=so.FindProperty("_slots");array.arraySize=slots.Count;for(int i=0;i<slots.Count;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=slots[i];
            });
        }

        private static GameObject Instantiate(string name,Scene scene,Vector3 position)
        { var obj=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath(name)),scene);obj.transform.position=position;return obj; }
        private static void Cube(Scene scene,string name,Vector3 position,Vector3 size,string material)
        {
            var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);SceneManager.MoveGameObjectToScene(obj,scene);obj.name=name;obj.transform.position=position;obj.transform.localScale=size;obj.GetComponent<MeshRenderer>().sharedMaterial=Material(material);
        }
        [MenuItem("SilverScreen/Art/Reusable Fixtures/2 Build and capture isolated validation scene")]
        public static void BuildValidation()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Validate in Edit Mode.");
            if(SceneManager.GetSceneByPath(ScenePath).isLoaded)throw new InvalidOperationException("Close the reusable validation scene before rebuilding it.");
            Directory.CreateDirectory(ReviewRoot);
            ReusableArtworkValidation.BuildTemplates();
            var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(scene);
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.46f,.53f,.62f);RenderSettings.ambientEquatorColor=new Color(.31f,.32f,.34f);RenderSettings.ambientGroundColor=new Color(.18f,.17f,.15f);
                Cube(scene,"Validation wall",new Vector3(0,2.9f,-.15f),new Vector3(16,5.8f,.30f),"C1_Stucco");
                Cube(scene,"Ground",new Vector3(0,-.1f,2),new Vector3(20,.2f,12),"C1_Floor");
                Cube(scene,"Pendant support",new Vector3(3.25f,3.25f,.45f),new Vector3(.22f,.16f,.9f),"C1_StructuralSteel");
                var placements=new[]{("MeterCabinet_1930",new Vector3(-6.3f,1.45f,0)),("DistributionPanel_1930",new Vector3(-4.6f,1.4f,0)),("IntakeCabinet_1930",new Vector3(-2.7f,1.3f,0)),("Disconnect_1930",new Vector3(-1.55f,1.7f,0)),("JunctionBox_Shallow_1930",new Vector3(-.8f,2,0)),("JunctionBox_Deep_1930",new Vector3(-.35f,2,0)),("PullBox_1930",new Vector3(-.6f,1.2f,0)),("GooseneckLamp_1930",new Vector3(1.45f,2.4f,0)),("PendantWorkLamp_1930",new Vector3(3.25f,3.17f,.60f)),("Bollard_1930",new Vector3(1.1f,0,1.3f)),("Bollard_1930",new Vector3(3.7f,0,1.3f))};
                foreach(var p in placements)Instantiate(p.Item1,scene,p.Item2);
                // Hardware is mounted on a dedicated leaf/frame coupon at original mechanical spacing.
                Cube(scene,"Hardware leaf coupon",new Vector3(5.7f,1.2f,-.005f),new Vector3(1,.2f+2.1f,.22f),"C1_Timber");
                Cube(scene,"Hardware jamb coupon",new Vector3(6.265f,1.2f,-.03f),new Vector3(.13f,2.4f,.26f),"C1_Steel");
                Cube(scene,"Strike jamb coupon",new Vector3(5.145f,1.2f,-.035f),new Vector3(.09f,2.4f,.27f),"C1_Steel");
                Instantiate("DoorPull_1930",scene,new Vector3(5.4f,1.15f,.105f));
                Instantiate("PersonnelLockset_1930",scene,new Vector3(5.4f,.82f,.105f));
                foreach(float y in new[]{.35f,1.18f,1.98f})Instantiate("PersonnelHinge_180_1930",scene,new Vector3(6.212f,y,.175f));
                var acoustic=Instantiate("DisplayFrame_Double_1930",scene,new Vector3(-5.4f,4.4f,0));acoustic.name="Double frame - approved acoustic inserts";
                var graphic=Instantiate("DisplayFrame_Double_1930",scene,new Vector3(-2.4f,4.4f,0));graphic.name="Double frame - per-slot artwork";
                var identity=new StudioIdentity("Aurora Pictures");
                var clock=new SimulationClock(1937,12,31,23,59);
                var playerContext=ReusableArtworkValidation.Context(identity.Name,"1937","THE SILVER LINING");
                using var stateBinding=new StudioArtworkBinding(playerContext,identity,clock);
                var posterPresenter=ReusableArtworkValidation.Attach(graphic.GetComponent<DisplayFrame>().Slots[0],"OpenHouse",InsertRole.AdvertisingSignage,playerContext);
                var noticePresenter=ReusableArtworkValidation.Attach(graphic.GetComponent<DisplayFrame>().Slots[1],"StudioNotice",InsertRole.StudioNotice,playerContext);
                var rearAcoustic=Instantiate("DisplayFrame_Single_1930",scene,new Vector3(.8f,4.45f,0));rearAcoustic.name="Single frame - approved acoustic insert";
                var rearGraphic=Instantiate("DisplayFrame_Single_1930",scene,new Vector3(4.4f,4.45f,0));rearGraphic.name="Single frame - fitted artwork with mat";
                var rivalContext=ReusableArtworkValidation.Context("Majestic Pictures","1937","THE NIGHT EXPRESS");
                var rivalPresenter=ReusableArtworkValidation.Attach(rearGraphic.GetComponent<DisplayFrame>().Slots[0],"OpenHouse",InsertRole.AdvertisingSignage,rivalContext);
                var sun=new GameObject("Validation daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2.4f;sun.color=new Color(1,.94f,.83f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(42,-150,0);RenderSettings.sun=sun;
                var camera=new GameObject("Validation camera").AddComponent<Camera>();camera.scene=scene;camera.nearClipPlane=.015f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.23f,.27f,.32f);
                camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                // Camera.scene alone does not exclude additive scene renderers in URP.
                // Change only objects owned by this validation scene.
                int reviewLayer=Enumerable.Range(8,24).Reverse().First(layer=>string.IsNullOrEmpty(LayerMask.LayerToName(layer)) &&
                    !Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene).SelectMany(s=>s.GetRootGameObjects()).SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).Any(t=>t.gameObject.layer==layer));
                foreach(var transform in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)))transform.gameObject.layer=reviewLayer;
                camera.cullingMask=1<<reviewLayer;sun.cullingMask=1<<reviewLayer;
                foreach(var light in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light>(true)))light.cullingMask=1<<reviewLayer;
                Capture(camera,"50_template_before",new Vector3(-2.4f,4.4f,3.05f),new Vector3(-2.4f,4.4f,0),40,1600,1200);
                ReusableArtworkValidation.SaveTexture(posterPresenter.RenderedArtwork,"53_poster_before");
                ReusableArtworkValidation.SaveTexture(noticePresenter.RenderedArtwork,"55_notice_before");
                var rivalTexture=rivalPresenter.RenderedArtwork;
                identity.TryRename("Crescent Film Company");clock.Advance(1);
                playerContext.Set(ArtworkContext.ProductionTitle,"MIDNIGHT ON THE COAST");
                if(playerContext.Get(ArtworkContext.CurrentYear)!="1938" || rivalPresenter.RenderedArtwork!=rivalTexture)
                    throw new InvalidOperationException("Studio/year binding or rival isolation failed.");
                Capture(camera,"51_template_after",new Vector3(-2.4f,4.4f,3.05f),new Vector3(-2.4f,4.4f,0),40,1600,1200);
                ReusableArtworkValidation.SaveTexture(posterPresenter.RenderedArtwork,"54_poster_after");
                ReusableArtworkValidation.SaveTexture(noticePresenter.RenderedArtwork,"56_notice_after");
                Capture(camera,"52_rival_unchanged",new Vector3(4.4f,4.45f,3.8f),new Vector3(4.4f,4.45f,0),40,1400,1100);
                identity.TryRename("Aurora Pictures");clock.RestoreState(new SimulationDateTime(1937,12,31,23,59),SimulationSpeed.Normal,SimulationSpeed.Normal);
                playerContext.Set(ArtworkContext.ProductionTitle,"THE SILVER LINING");
                Capture(camera,"01_overview",new Vector3(10,7,16),new Vector3(0,2.7f,0),49,1920,1080);
                Capture(camera,"02_electrical",new Vector3(-2.7f,2.3f,5.3f),new Vector3(-4.1f,1.35f,0),46,1920,1080);
                Capture(camera,"03_controls",new Vector3(-4.05f,1.57f,1.85f),new Vector3(-4.6f,1.43f,.32f),42,1200,1400);
                Capture(camera,"04_lamps",new Vector3(3.9f,2.8f,4.6f),new Vector3(2.25f,2.85f,.3f),42,1600,1200);
                Capture(camera,"05_frames",new Vector3(-3.9f,4.45f,5.7f),new Vector3(-3.9f,4.45f,0),51,1920,1080);
                Capture(camera,"06_single_frames",new Vector3(2.6f,4.6f,6.2f),new Vector3(2.6f,4.45f,0),54,1920,1080);
                Capture(camera,"07_hardware",new Vector3(6.8f,1.45f,2.2f),new Vector3(5.72f,1.15f,.1f),42,1400,1400);
                // Record a real same-instance swap, then restore the scene's representative poster.
                rearGraphic.GetComponent<DisplayFrame>().Slots[0].SetInsert(InsertRole.StudioNotice,noticePresenter.RenderedArtwork,ArtworkFit.Crop);
                Capture(camera,"08_instance_swap",new Vector3(4.4f,4.45f,3.8f),new Vector3(4.4f,4.45f,0),40,1400,1100);
                rivalPresenter.Refresh();
                foreach(var fixtureLight in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<FixtureLightSwitch>()))fixtureLight.SetOn(true);
                Capture(camera,"09_optional_lights",new Vector3(3.9f,2.8f,4.6f),new Vector3(2.25f,2.85f,.3f),42,1600,1200);
                camera.transform.position=new Vector3(10,7,16);camera.transform.LookAt(new Vector3(0,2.7f,0));camera.fieldOfView=49;
                if(!EditorSceneManager.SaveScene(scene,ScenePath))throw new IOException("Could not save independent validation scene.");
            }
            finally { SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true); }
            Debug.Log("Reusable M1 validation scene and native URP captures saved; original active scene restored.");
        }
        public static void Capture(Camera camera,string name,Vector3 position,Vector3 target,float fov,int width,int height)
        {
            var old=RenderTexture.active;var rt=RenderTexture.GetTemporary(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Texture2D image=null;
            try
            {
                camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));camera.fieldOfView=fov;camera.aspect=(float)width/height;
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};
                for(int i=0;i<3;i++)RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=rt;image=new Texture2D(width,height,TextureFormat.RGB24,false,false);image.ReadPixels(new Rect(0,0,width,height),0,0);image.Apply();File.WriteAllBytes(ReviewRoot+"/"+name+".png",image.EncodeToPNG());
            }
            finally {RenderTexture.active=old;RenderTexture.ReleaseTemporary(rt);if(image!=null)Object.DestroyImmediate(image);}
        }
    }
}
