using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Interaction;
using SilverScreen.Presentation.ReusableAssets;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.EnvironmentArt
{
    public static class StudioServicesProductionReview
    {
        public const string Review="ArtReview/StudioServices/Production";
        public const string ScenePath="Assets/SilverScreen/Environment/StudioServices/Production/StudioServices_ProductionReview.unity";
        [Serializable] public class Check {public string name,evidence;public bool passed;}
        [Serializable] public class Report {public string unityVersion;public Check[] checks;public int kitCount,fixtureInstances,triangles;}
        public static void Capture(Camera camera,string file,Vector3 position,Vector3 target,float fov,bool ortho=false,float size=7)
        {
            camera.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));camera.fieldOfView=fov;camera.orthographic=ortho;camera.orthographicSize=size;camera.aspect=1.6f;
            var previous=RenderTexture.active;var rt=RenderTexture.GetTemporary(1920,1200,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Texture2D image=null;
            try
            {
                var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};
                for(int i=0;i<3;i++)RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=rt;image=new Texture2D(1920,1200,TextureFormat.RGB24,false,false);image.ReadPixels(new Rect(0,0,1920,1200),0,0);image.Apply();File.WriteAllBytes(file,image.EncodeToPNG());
            }
            finally{RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);if(image!=null)Object.DestroyImmediate(image);}
        }
        [MenuItem("SilverScreen/Art/Studio Services/4 Capture and check production")]
        public static void Run()=>Run(true);
        public static void Run(bool saveReviewScene)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            if(saveReviewScene&&Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).path==ScenePath))throw new InvalidOperationException("Close production review before regeneration.");
            Directory.CreateDirectory(Review);var original=SceneManager.GetActiveScene();var checks=new List<Check>();
            void Check(string n,bool p,string e){checks.Add(new Check{name=n,passed=p,evidence=e});}
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            NavMeshSurface nav=null;
            try
            {
                var environment=new GameObject("Scoped review environment");
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StudioServicesProductionBuilder.PrefabPath),scene);root.transform.SetParent(environment.transform,true);
                var ground=GameObject.CreatePrimitive(PrimitiveType.Cube);ground.name="Review ground";ground.transform.SetParent(environment.transform,false);ground.transform.position=new Vector3(2,-.22f,0);ground.transform.localScale=new Vector3(38,.25f,28);
                ground.GetComponent<Renderer>().sharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(StudioServicesProductionBuilder.Kit+"/Materials/Concrete.mat");
                var sun=new GameObject("Review daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2;sun.color=new Color(1,.95f,.86f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(47,-33,0);
                RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.52f,.57f,.64f);RenderSettings.ambientEquatorColor=new Color(.35f,.35f,.33f);RenderSettings.ambientGroundColor=new Color(.19f,.17f,.14f);
                var camera=new GameObject("Production review camera").AddComponent<Camera>();camera.scene=scene;camera.nearClipPlane=.03f;camera.farClipPlane=150;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.32f,.39f,.43f);camera.GetUniversalAdditionalCameraData().renderPostProcessing=false;
                int layer=Enumerable.Range(8,24).Reverse().First(l=>string.IsNullOrEmpty(LayerMask.LayerToName(l))&&!Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene).SelectMany(s=>s.GetRootGameObjects()).SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).Any(t=>t.gameObject.layer==l));
                foreach(var t in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Transform>(true)))t.gameObject.layer=layer;
                foreach(var l in scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Light>(true)))l.cullingMask=1<<layer;camera.cullingMask=1<<layer;
                var cameraData=camera.GetUniversalAdditionalCameraData();cameraData.volumeLayerMask=1<<layer;
                cameraData.renderPostProcessing=true;cameraData.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
                void Shot(string name,Vector3 p,Vector3 t,float f=39,bool o=false,float size=7)=>Capture(camera,Review+"/"+name+".png",p,t,f,o,size);
                var cut=root.GetComponent<BuildingCutawayController>();var renderers=root.GetComponentsInChildren<Renderer>(true);var states=renderers.Select(r=>r.enabled).ToArray();var colliders=root.GetComponentsInChildren<Collider>(true);var collision=colliders.Select(c=>c.enabled).ToArray();
                Shot("01_front_three_quarter",new Vector3(18,8,-24),new Vector3(1.5f,2.3f,0));
                Shot("02_front",new Vector3(2.3f,2.6f,-30),new Vector3(2.3f,2.6f,0),43,true,7.1f);
                Shot("03_rear_yard",new Vector3(24,14,24),new Vector3(2.3f,1.8f,0),44);
                Shot("04_management",new Vector3(22,25,-24),new Vector3(1.4f,0,0),42);
                Shot("05_employment",new Vector3(-10,4,-13),new Vector3(-4.8f,1.65f,-4.25f),41);
                Shot("06_workshop_doors",new Vector3(7,3,-12),new Vector3(3.8f,1.8f,-4.4f),40);
                Shot("07_working_yard",new Vector3(17,5,-9),new Vector3(9.6f,1.25f,1.3f),47);
                Shot("15_drainage_corner",new Vector3(10,6.5f,7),new Vector3(7.0f,3.65f,3.9f),39);
                Shot("16_left_eave",new Vector3(-11,6,7),new Vector3(-7,3.7f,3.9f),39);
                var eye=new Vector3(19,24,-24);cut.SetReveal(BuildingRevealReason.DeveloperPreview,true,eye);
                Shot("08_cutaway",eye,new Vector3(0,0,0));Shot("09_plan",new Vector3(0,30,-.001f),Vector3.zero,44,true,8.2f);
                cut.SetReveal(BuildingRevealReason.DeveloperPreview,true,new Vector3(-9,8,-9));
                Shot("10_office",new Vector3(-9,8,-9),new Vector3(-3.6f,.65f,-1.3f),43);
                cut.SetReveal(BuildingRevealReason.DeveloperPreview,true,new Vector3(10,8,-8));
                Shot("11_workshop",new Vector3(10,8,-8),new Vector3(3.3f,.8f,1.4f),46);
                cut.SetReveal(BuildingRevealReason.DeveloperPreview,true,new Vector3(-15,22,22));
                Shot("12_storage",new Vector3(-9,7,8),new Vector3(-3.7f,.8f,2.6f),48);
                Check("All visibility groups have renderers",cut.Groups.All(g=>g.Renderers.Length>0),string.Join(", ",cut.Groups.Select(g=>g.Id)));
                Check("Cutaway retains finished floor and partitions",root.transform.Find("Floor").GetComponent<Renderer>().enabled&&root.transform.Find("InteriorPartitions").GetComponent<Renderer>().enabled,"Floor, interior-facing wall linings and partition caps are authored geometry");
                cut.SetReveal(BuildingRevealReason.BuildingFocus,true,eye);cut.SetReveal(BuildingRevealReason.DeveloperPreview,false,eye);
                Check("Independent reveal owners",cut.IsRevealed,"Building focus remains after preview release");
                cut.SetReveal(BuildingRevealReason.BuildingFocus,false,eye);
                Check("Exact restoration",renderers.Select((r,i)=>r.enabled==states[i]).All(x=>x),renderers.Length+" renderer states restored");
                Check("Physical collision remains active",colliders.Select((c,i)=>c.enabled==collision[i]).All(x=>x),colliders.Length+" collider states unchanged");
                Shot("13_restored",new Vector3(18,8,-24),new Vector3(1.5f,2.3f,0));
                Check("Exact rendered restoration",File.ReadAllBytes(Review+"/01_front_three_quarter.png").SequenceEqual(File.ReadAllBytes(Review+"/13_restored.png")),"PNG byte comparison before and after multiple reveal reasons");
                Physics.SyncTransforms();var anchors=root.transform.Find("SemanticAnchors");
                foreach(Transform a in anchors)
                {
                    var blockers=Physics.OverlapCapsule(a.position+Vector3.up*.41f,a.position+Vector3.up*1.65f,.35f,1<<layer,QueryTriggerInteraction.Ignore).Where(c=>c.transform.IsChildOf(root.transform)).Select(c=>c.name).ToArray();
                    Check("Anchor clearance: "+a.name,blockers.Length==0,blockers.Length==0?"0.70 m diameter, 2.00 m height clear":string.Join(", ",blockers));
                }
                var fixtures=root.GetComponentsInChildren<ReusableFixture>(true);
                Check("Canonical prefab references",fixtures.All(f=>f.Definition!=null&&PrefabUtility.GetCorrespondingObjectFromSource(f.gameObject)!=null),fixtures.Length+" instances reference independent reusable assets");
                Check("Live identity",root.GetComponent<StudioServicesFacility>().FacilityId=="building.studio-service"&&root.GetComponent<StudioBuildingView>().BuildingType==SilverScreen.Domain.BuildingType.StudioServices,"Dedicated building type, no Stage or Headquarters identity");
                Check("Hiring uses interior reveal",root.GetComponentsInChildren<PersonInteractionSpot>(true).Length==2&&root.GetComponentsInChildren<PersonInteractionSpot>(true).All(s=>s.InteriorVisibility==cut),"Two authored compatible hiring slots; ordinary play hides them");
                environment.transform.position=new Vector3(1000,0,1000);Physics.SyncTransforms();nav=environment.AddComponent<NavMeshSurface>();nav.collectObjects=CollectObjects.Children;nav.useGeometry=NavMeshCollectGeometry.PhysicsColliders;nav.overrideVoxelSize=true;nav.voxelSize=.025f;nav.layerMask=1<<layer;nav.BuildNavMesh();
                var filter=new NavMeshQueryFilter{agentTypeID=nav.agentTypeID,areaMask=NavMesh.AllAreas};bool sourceValid=NavMesh.SamplePosition(anchors.Find("OfficeApproach").position,out var source,.15f,filter);
                foreach(string name in new[]{"OfficeEntrance","HireConstructionWorker","HireGroundskeeper","WorkshopEntrance","YardAccess","ConstructionMaterialPickup","ToolPickup","WorkshopWork","GroundskeeperSupply"})
                {
                    var path=new NavMeshPath();bool valid=sourceValid&&NavMesh.SamplePosition(anchors.Find(name).position,out var hit,.15f,filter)&&NavMesh.CalculatePath(source.position,hit.position,filter,path)&&path.status==NavMeshPathStatus.PathComplete;
                    Check("Navigation: "+name,valid,path.status+"; "+path.corners.Length+" corners, isolated 0.025 m collider bake with current employee agent");
                }
                nav.RemoveData();Object.DestroyImmediate(nav.navMeshData);Object.DestroyImmediate(nav);nav=null;environment.transform.position=Vector3.zero;Physics.SyncTransforms();
                var lod=root.GetComponent<LODGroup>();
                int tris=lod!=null ? lod.GetLODs()[0].renderers.Sum(r=>r.GetComponent<MeshFilter>().sharedMesh.triangles.Length/3) : root.GetComponentsInChildren<MeshFilter>(true).Sum(m=>m.sharedMesh.triangles.Length/3);
                var report=new Report{unityVersion=Application.unityVersion,checks=checks.ToArray(),kitCount=AssetDatabase.FindAssets("t:ReusableAssetDefinition",new[]{StudioServicesProductionBuilder.Kit}).Length,fixtureInstances=fixtures.Length,triangles=tris};
                File.WriteAllText(Review+"/checks.json",JsonUtility.ToJson(report,true));
                if(saveReviewScene)EditorSceneManager.SaveScene(scene,ScenePath);
                Debug.Log("Studio Services production checks "+checks.Count(c=>c.passed)+"/"+checks.Count+"; "+tris+" rendered triangles.");
            }
            finally{if(nav!=null){nav.RemoveData();if(nav.navMeshData!=null)Object.DestroyImmediate(nav.navMeshData);}SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
