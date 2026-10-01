using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using SilverScreen.Domain;
using SilverScreen.Domain.ReusableAssets;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.ReusableAssets;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.EnvironmentArt
{
    /// <summary>Explicit source-packet import. Only owns the period kit and Studio Services production outputs.</summary>
    public static class StudioServicesProductionBuilder
    {
        public const string Kit = "Assets/SilverScreen/Environment/PeriodEnvironment1930";
        public const string Root = "Assets/SilverScreen/Environment/StudioServices/Production";
        public const string PrefabPath = "Assets/SilverScreen/Environment/StudioServices/Resources/StudioServices_Live.prefab";
        [Serializable] public class Surface { public string name, texture; public float[] linearRGB; public float metallic, roughness; }
        [Serializable] public class Obstacle { public string name, group; public float[] position, size; }
        [Serializable] public class Anchor { public string name; public float[] position; }
        [Serializable] public class Record { public string name, family, surface, id; public float clearance; public float[] boundsMin,boundsMax; public Obstacle[] colliders; public Anchor[] anchors; }
        [Serializable] public class Fixture { public string asset, group; public float[] position; public float yaw; public bool existing; }
        [Serializable] public class Packet
        {
            public StudioServicesMassingBuilder.Part[] parts;
            public Surface[] materials; public Record[] assets; public Fixture[] fixtures;
            public StudioServicesMassingBuilder.Marker[] anchors; public Obstacle[] colliders;
        }
        public static Vector3 V(float[] a) => new Vector3(a[0],a[1],a[2]);
        public static Packet Read(string path)
        {
            using var f=File.OpenRead(path); using var zip=new GZipStream(f,CompressionMode.Decompress); using var r=new StreamReader(zip);
            return JsonUtility.FromJson<Packet>(r.ReadToEnd());
        }
        static void Folder(string path)
        { string at="Assets"; foreach(var part in path.Split('/').Skip(1)) { if(!AssetDatabase.IsValidFolder(at+"/"+part))AssetDatabase.CreateFolder(at,part);at+="/"+part; } }
        static T Asset<T>(string path,Func<T> create) where T:Object
        { var a=AssetDatabase.LoadAssetAtPath<T>(path);if(a==null){a=create();AssetDatabase.CreateAsset(a,path);}return a; }
        static void Save(Object o){EditorUtility.SetDirty(o);AssetDatabase.SaveAssetIfDirty(o);}
        static void Edit(Object o,Action<SerializedObject> edit){var s=new SerializedObject(o);edit(s);s.ApplyModifiedPropertiesWithoutUndo();}
        static GameObject Child(string name,Transform parent){var o=new GameObject(name);o.transform.SetParent(parent,false);return o;}
        static void Strings(SerializedProperty p,IEnumerable<string> input){var a=input.ToArray();p.arraySize=a.Length;for(int i=0;i<a.Length;i++)p.GetArrayElementAtIndex(i).stringValue=a[i];}
        static Dictionary<string,Material> Materials(Surface[] surfaces)
        {
            var result=new Dictionary<string,Material>();
            foreach(var s in surfaces)
            {
                var m=Asset(Kit+"/Materials/"+s.name+".mat",()=>new Material(Shader.Find("Universal Render Pipeline/Lit")));
                var c=V(s.linearRGB);m.SetColor("_BaseColor",new Color(c.x,c.y,c.z).gamma);
                m.SetFloat("_Metallic",s.metallic);m.SetFloat("_Smoothness",1-s.roughness);
                string family=s.texture=="canvas"?"cloth":s.texture;
                var albedo=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SilverScreen/Environment/Stage1CleanCandidate/Textures/"+family+"_albedo.png");
                var normal=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SilverScreen/Environment/Stage1CleanCandidate/Textures/"+family+"_normal.png");
                bool patina=family.StartsWith("weather:",StringComparison.Ordinal);
                if(patina)albedo=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/SilverScreen/Environment/Weathering/"+family.Substring(8)+".png");
                m.SetTexture("_BaseMap",albedo);m.SetTexture("_BumpMap",normal);m.SetFloat("_BumpScale",.35f);
                if(normal!=null)m.EnableKeyword("_NORMALMAP");else m.DisableKeyword("_NORMALMAP");
                if(s.name=="Glass"||patina)
                {
                    var glass=m.GetColor("_BaseColor");glass.a=patina?.38f:.24f;m.SetColor("_BaseColor",glass);
                    m.SetFloat("_Surface",1);m.SetFloat("_ZWrite",0);m.SetFloat("_SrcBlend",(float)BlendMode.SrcAlpha);m.SetFloat("_DstBlend",(float)BlendMode.OneMinusSrcAlpha);
                    m.SetFloat("_Cull",0);m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");m.renderQueue=3000;m.SetOverrideTag("RenderType","Transparent");m.SetShaderPassEnabled("ShadowCaster",false);
                }
                Save(m);result.Add(s.name,m);
            }
            return result;
        }
        static void Geometry(GameObject go,StudioServicesMassingBuilder.Part p,string folder,Dictionary<string,Material> materials)
        {
            var mesh=Asset(folder+"/"+p.name+".asset",()=>new Mesh());mesh.Clear();mesh.name=p.name;
            mesh.indexFormat=p.positions.Length/3>65535?IndexFormat.UInt32:IndexFormat.UInt16;
            mesh.vertices=Enumerable.Range(0,p.positions.Length/3).Select(i=>new Vector3(p.positions[i*3],p.positions[i*3+1],p.positions[i*3+2])).ToArray();
            mesh.normals=Enumerable.Range(0,p.normals.Length/3).Select(i=>new Vector3(p.normals[i*3],p.normals[i*3+1],p.normals[i*3+2])).ToArray();
            mesh.uv=Enumerable.Range(0,p.uv.Length/2).Select(i=>new Vector2(p.uv[i*2],p.uv[i*2+1])).ToArray();
            mesh.subMeshCount=p.submeshes.Length;for(int i=0;i<p.submeshes.Length;i++)mesh.SetTriangles(p.submeshes[i].indices,i,false);
            mesh.RecalculateBounds();mesh.RecalculateTangents();Save(mesh);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;go.AddComponent<MeshRenderer>().sharedMaterials=p.materials.Select(n=>materials[n]).ToArray();
        }
        static BoxCollider Collider(GameObject root,Obstacle data)
        {var c=Child(data.name,root.transform).AddComponent<BoxCollider>();c.center=V(data.position);c.size=V(data.size);return c;}
        [MenuItem("SilverScreen/Art/Studio Services/3 Build production and independent kit")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            foreach(var f in new[]{Kit+"/Materials",Kit+"/Meshes",Kit+"/Prefabs",Kit+"/Definitions",Root+"/Meshes",Path.GetDirectoryName(PrefabPath).Replace('\\','/')})Folder(f);
            var kit=Read("ArtExports/PeriodEnvironment1930/kit_meshes.json.gz");var building=Read("ArtExports/StudioServices/Production/production_meshes.json.gz");
            var materials=Materials(kit.materials);var preview=EditorSceneManager.NewPreviewScene();
            try
            {
                foreach(var r in kit.assets)
                {
                    var root=new GameObject(r.name);SceneManager.MoveGameObjectToScene(root,preview);
                    var part=kit.parts.Single(p=>p.name==r.name);Geometry(Child("Geometry",root.transform),part,Kit+"/Meshes",materials);
                    foreach(var c in r.colliders??Array.Empty<Obstacle>())
                    {
                        var physical=Collider(root,c);
                        if((r.family=="Workshop"||r.family=="Fences")&&r.surface=="Ground"&&physical.size.y>.5f)
                        {
                            // As for masonry below, guard workshop and thin fence corners during a turn.
                            // Native steering can shave the rasterized corner by millimetres.
                            // Physical collision and employee dimensions stay unchanged.
                            var guard=physical.gameObject.AddComponent<NavMeshModifierVolume>();
                            guard.area=1;guard.center=physical.center;guard.size=physical.size+new Vector3(.12f,.04f,.12f);
                        }
                    }
                    var anchorRoot=Child("Anchors",root.transform).transform;var mount=Child("Mount",anchorRoot);mount.AddComponent<FixtureAnchor>();
                    foreach(var a in r.anchors??Array.Empty<Anchor>())
                    {
                        var t=Child(a.name,anchorRoot);t.transform.localPosition=V(a.position);
                        Edit(t.AddComponent<FixtureAnchor>(),s=>s.FindProperty("_kind").enumValueIndex=(int)FixtureAnchorKind.Socket);
                    }
                    var def=Asset(Kit+"/Definitions/"+r.name+".asset",ScriptableObject.CreateInstance<ReusableAssetDefinition>);
                    var bounds=new Bounds((V(r.boundsMax)+V(r.boundsMin))/2,V(r.boundsMax)-V(r.boundsMin));
                    Edit(def,s=>{
                        s.FindProperty("_stableId").stringValue=r.id;s.FindProperty("_displayName").stringValue=r.name.Replace('_',' ');
                        s.FindProperty("_category").stringValue=r.surface=="Ground"?"Props":"Architecture";s.FindProperty("_subcategory").stringValue=r.family;
                        s.FindProperty("_playerCatalogItem").boolValue=true;s.FindProperty("_availableFromYear").intValue=1930;s.FindProperty("_hasAvailableUntilYear").boolValue=false;
                        s.FindProperty("_placementSurface").enumValueIndex=(int)Enum.Parse<PlacementSurface>(r.surface);
                        s.FindProperty("_suitability").enumValueIndex=(int)FixtureSuitability.Both;s.FindProperty("_role").enumValueIndex=(int)FixtureRole.Both;
                        s.FindProperty("_visibleBounds").boundsValue=bounds;s.FindProperty("_functionalClearance").boundsValue=new Bounds(new Vector3(0,bounds.center.y,bounds.max.z+r.clearance/2),new Vector3(bounds.size.x,bounds.size.y,r.clearance));
                        Strings(s.FindProperty("_materialRoles"),part.materials);Strings(s.FindProperty("_environmentTags"),new[]{"Industrial","Office","Workshop","Period and later settings"});
                        s.FindProperty("_optionalInteraction").stringValue="Authored anchors; future interaction only";
                    });
                    Edit(root.AddComponent<ReusableFixture>(),s=>s.FindProperty("_definition").objectReferenceValue=def);
                    var prefab=PrefabUtility.SaveAsPrefabAsset(root,Kit+"/Prefabs/"+r.name+".prefab");
                    Edit(def,s=>s.FindProperty("_prefab").objectReferenceValue=prefab);Save(def);Object.DestroyImmediate(root);
                }
                BuildBuilding(building,materials,preview);
            }
            finally {EditorSceneManager.ClosePreviewScene(preview);}
            if (StudioServicesLodBuilder.HasExports) StudioServicesLodBuilder.Build();
            Debug.Log("Studio Services production built with "+kit.assets.Length+" independent kit assets and "+building.fixtures.Length+" linked prefab placements.");
        }
        static void BuildBuilding(Packet data,Dictionary<string,Material> materials,Scene preview)
        {
            var root=new GameObject("StudioServices_Live");SceneManager.MoveGameObjectToScene(root,preview);
            var groups=new Dictionary<string,GameObject>();var obstacles=new Dictionary<string,List<Collider>>();
            GameObject Group(string name){if(!groups.ContainsKey(name))groups.Add(name,Child(name,root.transform));return groups[name];}
            foreach(var p in data.parts)Geometry(Group(p.name),p,Root+"/Meshes",materials);
            foreach(var f in data.fixtures)
            {
                string path=(f.existing?"Assets/SilverScreen/Environment/ReusableFixtures":Kit)+"/Prefabs/"+f.asset+".prefab";
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(prefab==null)throw new InvalidOperationException("Missing shared prefab "+path);
                var o=(GameObject)PrefabUtility.InstantiatePrefab(prefab,Group(f.group).transform);o.transform.localPosition=V(f.position);o.transform.localRotation=Quaternion.Euler(0,f.yaw,0);
            }
            var physical=Child("PhysicalCollision",root.transform);
            foreach(var c in data.colliders)
            {
                if(!obstacles.ContainsKey(c.group))obstacles.Add(c.group,new List<Collider>());
                var obstacle=Collider(physical,c);obstacles[c.group].Add(obstacle);
                if((c.group.EndsWith("Wall",StringComparison.Ordinal)||c.group=="InteriorPartitions")&&obstacle.size.y>.5f)
                {
                    // Keep real collision exact. A small bake-only guard prevents native
                    // steering from shaving concave jamb corners during a moving turn.
                    var guard=obstacle.gameObject.AddComponent<NavMeshModifierVolume>();
                    guard.area=1;guard.center=obstacle.center;guard.size=obstacle.size+new Vector3(.12f,.04f,.12f);
                }
            }
            var anchors=Child("SemanticAnchors",root.transform);
            foreach(var m in data.anchors){var t=Child(m.name,anchors.transform).transform;t.localPosition=V(m.position);t.localRotation=Quaternion.Euler(0,m.yaw,0);}
            var visibility=new List<BuildingCutawayController.VisibilityGroup>();
            void VisibleGroup(string id,bool roof,Vector3 normal,params string[] other)
            {
                var names=new[]{id}.Concat(other).ToArray();
                visibility.Add(new BuildingCutawayController.VisibilityGroup{Id=id,Roof=roof,OutwardNormal=normal,
                    Renderers=names.SelectMany(n=>Group(n).GetComponentsInChildren<Renderer>(true)).ToArray(),
                    Occluders=names.SelectMany(n=>Group(n).GetComponentsInChildren<Collider>(true)).Concat(names.Where(obstacles.ContainsKey).SelectMany(n=>obstacles[n])).Distinct().ToArray()});
            }
            VisibleGroup("Roof",true,Vector3.zero,"EmploymentCanopy","OfficeAwning","YardShelterRoof");
            VisibleGroup("FrontWall",false,Vector3.back,"WorkshopDoors","OfficeDoor","EmploymentBoard");
            VisibleGroup("RightWall",false,Vector3.right,"YardDoor");VisibleGroup("LeftWall",false,Vector3.left);VisibleGroup("RearWall",false,Vector3.forward);
            var cutaway=root.AddComponent<BuildingCutawayController>();cutaway.Configure(visibility.ToArray(),new Bounds(new Vector3(0,2.1f,0),new Vector3(14,4.2f,9)));
            var select=Child("SelectionOnly",root.transform).AddComponent<BoxCollider>();select.isTrigger=true;select.center=new Vector3(0,2.1f,0);select.size=new Vector3(14,4.2f,9);
            root.AddComponent<StudioBuildingView>().Initialize(BuildingType.StudioServices,"Studio Services",anchors.transform.Find("OfficeApproach"));
            root.AddComponent<StudioServicesFacility>().Configure(anchors.transform);
            foreach(var role in new[]{ProfessionalRole.ConstructionWorker,ProfessionalRole.Groundskeeper})
            {
                var spot=anchors.transform.Find("Hire"+role).gameObject.AddComponent<PersonInteractionSpot>();
                spot.Configure("building.studio-service:hire:"+role,"building.studio-service",PersonSpotActivity.Hire,role,"Hire as "+(role==ProfessionalRole.ConstructionWorker?"Construction Worker":"Groundskeeper"));spot.RequireInteriorReveal(cutaway);
                spot.GetComponent<ContextualDropTarget>().Configure(spot,spot.transform,cutaway,FloorTargetShape.Zone,
                    new Vector2(2f,1.65f),role==ProfessionalRole.ConstructionWorker?FloorTargetSymbol.Hammer:FloorTargetSymbol.Leaf);
            }
            PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);Object.DestroyImmediate(root);
        }
    }
}
