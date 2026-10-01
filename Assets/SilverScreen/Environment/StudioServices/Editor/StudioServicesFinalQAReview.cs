using System;
using System.IO;
using System.Linq;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.EnvironmentArt
{
    /// <summary>Isolated canonical asset captures and installed connection close-ups.</summary>
    public static class StudioServicesFinalQAReview
    {
        public const string Output = "ArtReview/StudioServices/FinalQA";
        [MenuItem("SilverScreen/Art/Studio Services/6 Capture final assembled QA")]
        public static void CaptureFinal()=>Run("After");
        public static void Run(string pass, bool collisionOnly=false)
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
            string folder=Output+"/"+pass;Directory.CreateDirectory(folder);
            var shots=new System.Collections.Generic.List<string>();
            var original=SceneManager.GetActiveScene();
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            SceneManager.SetActiveScene(scene);
            try
            {
                int layer=Enumerable.Range(8,24).Reverse().First(l=>string.IsNullOrEmpty(LayerMask.LayerToName(l))&&!Enumerable.Range(0,SceneManager.sceneCount).Select(SceneManager.GetSceneAt).Where(s=>s!=scene).SelectMany(s=>s.GetRootGameObjects()).SelectMany(o=>o.GetComponentsInChildren<Transform>(true)).Any(t=>t.gameObject.layer==l));
                void Layers(GameObject o){foreach(var t in o.GetComponentsInChildren<Transform>(true))t.gameObject.layer=layer;}
                var sun=new GameObject("Fidelity daylight").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2;sun.color=new Color(1,.95f,.86f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(47,-33,0);sun.cullingMask=1<<layer;
                RenderSettings.sun=sun;RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.52f,.57f,.64f);RenderSettings.ambientEquatorColor=new Color(.35f,.35f,.33f);RenderSettings.ambientGroundColor=new Color(.19f,.17f,.14f);
                var camera=new GameObject("Fidelity camera").AddComponent<Camera>();camera.scene=scene;camera.cullingMask=1<<layer;camera.nearClipPlane=.02f;camera.farClipPlane=100;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.24f,.28f,.30f);
                var data=camera.GetUniversalAdditionalCameraData();data.volumeLayerMask=1<<layer;data.renderPostProcessing=true;data.antialiasing=AntialiasingMode.FastApproximateAntialiasing;
                void Shot(string n,Vector3 p,Vector3 t,float f=42){StudioServicesProductionReview.Capture(camera,folder+"/"+n+".png",p,t,f);shots.Add(n);}
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StudioServicesProductionBuilder.PrefabPath),scene);Layers(root);
                var fixtures=root.GetComponentsInChildren<Transform>().Where(t=>t.parent!=null&&t.parent.parent==root.transform).ToArray();
                if(!collisionOnly)
                {
                int index=0;
                foreach(var fixture in fixtures)
                {
                    var name=fixture.name;
                    bool window=name.StartsWith("WindowSteel"),fence=name.StartsWith("Fence"),tool=name.Contains("ToolRack")||name.Contains("ToolTray");
                    bool furniture=name.StartsWith("Workbench")||name.StartsWith("Wheelbarrow")||name.StartsWith("DeskPedestal")||name.StartsWith("Telephone")||name.StartsWith("BenchSlatted")||name.StartsWith("NoticeBoard");
                    if(!(window||fence||tool||furniture||name.StartsWith("Gooseneck")))continue;
                    var rs=fixture.GetComponentsInChildren<Renderer>();if(rs.Length==0)continue;
                    var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
                    float d=Mathf.Max(b.size.x,b.size.y,b.size.z)*1.6f;
                    var front=fixture.forward;var right=fixture.right;
                    string id=(index++).ToString("D2")+"_"+name;
                    if(window){Shot(id+"_outside",b.center+front*d+right*.18f,b.center,48);Shot(id+"_inside",b.center-front*d-right*.18f,b.center,48);}
                    else if(fence){Shot(id+"_faceA",b.center+front*d+right*.35f+Vector3.up*.3f,b.center,48);Shot(id+"_faceB",b.center-front*d-right*.35f+Vector3.up*.3f,b.center,48);}
                    else{Shot(id+"_front",b.center+front*d+Vector3.up*(tool?.18f:.6f),b.center,46);Shot(id+"_oblique",b.center+front*d*.78f+right*d*.65f+Vector3.up*.5f,b.center,46);}
                }
                Shot("roof_above",new Vector3(0,17,0),new Vector3(0,4.2f,0),56);
                Shot("roof_front_left",new Vector3(-9,8,-11),new Vector3(0,4.6f,-1.5f),45);
                Shot("roof_front_right",new Vector3(9,8,-11),new Vector3(0,4.6f,-1.5f),45);
                Shot("roof_rear_left",new Vector3(-10,8,10),new Vector3(0,4.6f,1.5f),45);
                Shot("roof_rear_right",new Vector3(10,8,10),new Vector3(0,4.6f,1.5f),45);
                for(int side=-1;side<=1;side+=2){
                    Shot("drain_"+side+"_outlet",new Vector3(side*8.4f,4.5f,5.2f),new Vector3(side*7.1f,3.9f,3.8f),43);
                    Shot("drain_"+side+"_shoe",new Vector3(side*8.6f,.65f,4.7f),new Vector3(side*7.2f,.32f,3.8f),43);
                    Shot("eave_"+side+"_front",new Vector3(side*9.5f,5.3f,-6),new Vector3(side*6.9f,4.1f,-3.8f),42);
                }
                Shot("shelter_drain_outlet",new Vector3(12.6f,3.7f,-5),new Vector3(10.77f,2.5f,-3.72f),40);
                Shot("shelter_drain_shoe",new Vector3(12,.7f,-4.7f),new Vector3(10.85f,.3f,-3.72f),42);
                Shot("canopy_side",new Vector3(-6.1f,3.1f,-5.1f),new Vector3(-4.15f,3,-4.8f),50);
                Shot("canopy_above",new Vector3(-4.2f,5.3f,-6.3f),new Vector3(-4.15f,3,-4.7f),48);
                Shot("awning_mount_under",new Vector3(-3.1f,2.1f,-6),new Vector3(-4.15f,3,-4.55f),44);
                Shot("workshop_joinery_low",new Vector3(3.2f,.45f,1.4f),new Vector3(5.7f,.45f,2.7f),55);
                Shot("workshop_bench_oblique",new Vector3(3.8f,1.6f,1.1f),new Vector3(5.6f,.95f,2.75f),52);
                Shot("workshop_bench_rear",new Vector3(1.9f,1.5f,1.7f),new Vector3(2.9f,.85f,3.65f),52);
                Shot("installed_vise",new Vector3(3.1f,1.5f,2.05f),new Vector3(3.68f,1.07f,3.03f),38);
                Shot("yard_bench_oblique",new Vector3(9.8f,1.55f,1.45f),new Vector3(8.6f,.9f,3.52f),52);
                Shot("yard_materials_grounding",new Vector3(10.3f,.75f,-.4f),new Vector3(11.6f,.75f,1.3f),62);
                Shot("yard_barrels_grounding",new Vector3(8.8f,.65f,-.7f),new Vector3(7.5f,.4f,.85f),52);
                Shot("yard_crates_grounding",new Vector3(8.8f,.8f,1.4f),new Vector3(7.5f,.45f,2.65f),52);
                Shot("yard_sacks_grounding",new Vector3(10.35f,.8f,2.65f),new Vector3(11.15f,.4f,4.03f),57);
                Shot("fence_corner_front",new Vector3(13.5f,2.5f,-5.3f),new Vector3(12.35f,1.45f,-4.3f),48);
                Shot("fence_corner_rear",new Vector3(13.7f,2.5f,5.6f),new Vector3(12.35f,1.45f,4.35f),48);
                Shot("fence_wall_termination",new Vector3(8.2f,2.0f,5.4f),new Vector3(7.08f,1.4f,4.35f),48);
                Shot("wheelbarrow_handles_A",new Vector3(8.7f,1.3f,2.2f),new Vector3(8.5f,.59f,.70f),45);
                Shot("wheelbarrow_handles_B",new Vector3(10.55f,1.2f,-1.10f),new Vector3(11.25f,.56f,-2.35f),45);
                Shot("office_chairs",new Vector3(-3.0f,1.4f,-2.3f),new Vector3(-1.7f,.6f,-.65f),57);
                Shot("office_cabinet",new Vector3(-4.7f,1.6f,-1.5f),new Vector3(-6.38f,.8f,-.25f),50);
                Shot("storage_tools_front",new Vector3(-2.55f,1.4f,2.85f),new Vector3(-2.55f,1.15f,.6f),49);
                Shot("storage_tools_side",new Vector3(-3.65f,1.3f,1.8f),new Vector3(-2.55f,1.15f,.6f),65);
                Shot("carpenter_tools_front",new Vector3(2,2.0f,2.55f),new Vector3(0,2.0f,2.55f),49);
                Shot("carpenter_tools_oblique",new Vector3(1.5f,1.9f,1.65f),new Vector3(0,2.0f,2.55f),55);
                Shot("saw_grip_teeth",new Vector3(1.10f,1.88f,2.25f),new Vector3(.035f,1.76f,2.13f),44);
                Shot("saw_plate_edge",new Vector3(.65f,1.8f,1.50f),new Vector3(.035f,1.76f,2.13f),45);
                Shot("grounds_heads",new Vector3(-2.70f,.9f,1.8f),new Vector3(-2.55f,.45f,.63f),51);
                Shot("phone_rotary_receiver",new Vector3(-6.01f,1.20f,-2.03f),new Vector3(-6.05f,.935f,-1.48f),44);
                Shot("phone_side_cradle",new Vector3(-6.48f,1.17f,-1.65f),new Vector3(-6.05f,.93f,-1.48f),48);
                Shot("banker_lamp_front",new Vector3(-4.70f,1.16f,-2.20f),new Vector3(-4.70f,1.04f,-1.50f),48);
                Shot("banker_lamp_side",new Vector3(-4.05f,1.24f,-1.90f),new Vector3(-4.70f,1.12f,-1.50f),44);
                Shot("banker_lamp_under",new Vector3(-4.45f,.99f,-2.10f),new Vector3(-4.70f,1.15f,-1.50f),44);
                Shot("try_square_front",new Vector3(.73f,1.61f,3.15f),new Vector3(.035f,1.53f,3.15f),43);
                Shot("shelter_drain_wide",new Vector3(12.9f,3.5f,-7.3f),new Vector3(10.6f,2.55f,-2.7f),48);
                Shot("shelter_gutter_along",new Vector3(11.8f,3.45f,-5.1f),new Vector3(10.85f,2.83f,-1.85f),48);
                Shot("hand_plane_in_tray",new Vector3(3.40f,1.42f,3.05f),new Vector3(3.1f,1.07f,3.65f),43);
                Shot("gate_left_pull_exterior",new Vector3(8.05f,1.4f,-2.6f),new Vector3(7.22f,1.12f,-2.74f),43);
                Shot("gate_right_latch_yard",new Vector3(9.6f,1.4f,-6.05f),new Vector3(10.51f,1.12f,-6.1f),43);
                Shot("gate_right_pull_exterior",new Vector3(11.45f,1.55f,-6.75f),new Vector3(10.68f,1.12f,-6.04f),48);
                // The inward-open leaf's yard face is adjacent to the wall. Diagnostic cutaway
                // reveals this otherwise occluded face, leaving the actual hardware in place.
                var wall=root.transform.Find("RightWall").GetComponentsInChildren<Renderer>();
                var wallStates=wall.Select(r=>r.enabled).ToArray();foreach(var r in wall)r.enabled=false;
                Shot("gate_left_keeper_yard_cutaway",new Vector3(6.25f,1.4f,-2.7f),new Vector3(7.06f,1.12f,-2.74f),43);
                for(int i=0;i<wall.Length;i++)wall[i].enabled=wallStates[i];
                Shot("yard_mounting",new Vector3(9,2.1f,1.4f),new Vector3(8.6f,1.7f,4.1f),52);
                Shot("office_phone",new Vector3(-5.35f,1.35f,-2.8f),new Vector3(-5.8f,1,-1.4f),42);
                Shot("office_underdesk",new Vector3(-3.8f,.4f,-2.4f),new Vector3(-5.4f,.4f,-1.4f),60);
                Shot("yard_rear_low",new Vector3(9.5f,.65f,6.8f),new Vector3(9.5f,.7f,2.5f),65);
                Shot("whole_front",new Vector3(18,8,-24),new Vector3(1.5f,2.3f,0),39);
                Shot("whole_management",new Vector3(22,25,-24),new Vector3(1.4f,0,0),42);
                }
                Shot("collision_storage_front",new Vector3(-2.55f,1.7f,2.1f),new Vector3(-3.60f,.42f,3.35f),54);
                Shot("collision_storage_above",new Vector3(-3.1f,2.9f,2.6f),new Vector3(-3.70f,.2f,3.40f),54);
                Shot("collision_storage_clearance",new Vector3(-3.5f,2.3f,1.5f),new Vector3(-4.50f,.35f,3.62f),57);
                Shot("collision_yard_vise",new Vector3(10.05f,1.95f,1.2f),new Vector3(9.42f,.80f,2.9f),49);
                Shot("collision_yard_crates",new Vector3(8.60f,1.7f,1.45f),new Vector3(7.75f,.75f,2.80f),51);
                Shot("collision_yard_spacing",new Vector3(8.9f,2.70f,2.40f),new Vector3(8.6f,.4f,2.70f),67);
                Shot("collision_yard_sill",new Vector3(8.25f,2.55f,1.1f),new Vector3(7.34f,.95f,2.35f),54);
                Shot("collision_awning_front",new Vector3(-4.15f,2.9f,-7.6f),new Vector3(-4.15f,2.95f,-4.65f),48);
                Shot("collision_awning_left",new Vector3(-5.5f,2.48f,-5.95f),new Vector3(-4.95f,2.64f,-4.57f),42);
                Shot("collision_awning_right",new Vector3(-2.9f,2.50f,-5.85f),new Vector3(-3.35f,2.64f,-4.57f),42);
                Shot("collision_awning_above",new Vector3(-4.15f,4.05f,-6.30f),new Vector3(-4.15f,2.95f,-4.95f),51);
                Shot("collision_bench_top",new Vector3(-2.8f,1.55f,-5.7f),new Vector3(-2.43f,.53f,-4.96f),51);
                Shot("collision_bench_back",new Vector3(-3.35f,.85f,-4.57f),new Vector3(-2.42f,.64f,-4.69f),51);
                Shot("collision_door_bolts",new Vector3(3.775f,.77f,-5.90f),new Vector3(3.775f,.30f,-4.57f),42);
                Shot("collision_door_bolts_oblique",new Vector3(4.60f,.75f,-5.6f),new Vector3(3.775f,.30f,-4.57f),47);
                File.WriteAllLines(folder+"/capture-order.txt",shots);
                Object.DestroyImmediate(root);
                Debug.Log("Final assembled QA captured "+shots.Count+" views: "+pass);
            }
            finally{SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
