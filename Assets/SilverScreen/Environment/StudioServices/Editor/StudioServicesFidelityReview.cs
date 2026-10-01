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
    public static class StudioServicesFidelityReview
    {
        public const string Output = "ArtReview/StudioServices/Fidelity";
        [MenuItem("SilverScreen/Art/Studio Services/5 Capture fidelity and all canonical assets")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
            Directory.CreateDirectory(Output+"/Assets");
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
                void Shot(string n,Vector3 p,Vector3 t,float f=38)=>StudioServicesProductionReview.Capture(camera,Output+"/"+n+".png",p,t,f);
                var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StudioServicesProductionBuilder.PrefabPath),scene);Layers(root);
                Shot("18_window_exterior",new Vector3(-2.2f,2.1f,-7.5f),new Vector3(-1.525f,1.9f,-4.43f),48);
                Shot("19_window_interior",new Vector3(-.7f,2.0f,-1.3f),new Vector3(-1.525f,1.9f,-4.43f),48);
                Shot("20_notice_and_awning",new Vector3(-8,2.4f,-9),new Vector3(-5.3f,1.7f,-4.4f),48);
                Shot("21_parapet_left",new Vector3(-7,5.6f,-8),new Vector3(-4.2f,4.83f,-4.36f),40);
                Shot("22_parapet_right",new Vector3(7,5.6f,-8),new Vector3(4.2f,4.83f,-4.36f),40);
                Shot("23_rooflights",new Vector3(4,11,-10),new Vector3(0,4.5f,-1),48);
                Shot("24_drain_right_low",new Vector3(9.3f,.9f,5.2f),new Vector3(7.22f,.35f,3.8f),42);
                Shot("25_drain_left_low",new Vector3(-9.3f,.9f,5.2f),new Vector3(-7.22f,.35f,3.8f),42);
                Shot("26_gate_left",new Vector3(9,2,-6.3f),new Vector3(7.15f,1,-3.46f),50);
                Shot("27_gate_right",new Vector3(13,2,-7),new Vector3(10.6f,1,-5.14f),50);
                Shot("28_fence_join",new Vector3(15,2.2f,-1),new Vector3(12.35f,1,.7f),45);
                Shot("29_sign_lamp",new Vector3(.6f,5.6f,-6.7f),new Vector3(0,5.2f,-4.55f),38);
                Shot("31_shelter_drain",new Vector3(12.8f,2.4f,-6.2f),new Vector3(10.72f,1.45f,-3.73f),48);
                Shot("32_rear_electrical",new Vector3(7.7f,3.6f,8),new Vector3(5.6f,2.65f,4.55f),44);
                Shot("34_roof_hip_close",new Vector3(-7,6.0f,-4.7f),new Vector3(-4.9f,4.55f,-2.1f),34);
                Object.DestroyImmediate(root);
                var paths=Directory.GetFiles(StudioServicesProductionBuilder.Kit+"/Prefabs","*.prefab").OrderBy(p=>p,StringComparer.Ordinal).ToArray();
                File.WriteAllLines(Output+"/asset-capture-order.txt",paths.Select(Path.GetFileNameWithoutExtension));
                for(int i=0;i<paths.Length;i++)
                {
                    var go=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(paths[i].Replace('\\','/')),scene);Layers(go);
                    var rs=go.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs.Skip(1))b.Encapsulate(r.bounds);
                    float distance=Mathf.Max(b.size.x,b.size.y,b.size.z)*2.1f;var direction=new Vector3(.65f,.48f,1).normalized;
                    Shot("Assets/"+Path.GetFileNameWithoutExtension(paths[i]),b.center+direction*distance,b.center,38);
                    if(go.name.StartsWith("WindowSteel_195"))Shot("30_canonical_window_interior",b.center+new Vector3(-.45f,.25f,-1).normalized*distance,b.center,38);
                    if(go.name.StartsWith("WorkbenchJoiner"))Shot("33_bench_vise",new Vector3(-1.40f,1.50f,1.65f),new Vector3(-.78f,1.08f,.57f),35);
                    Object.DestroyImmediate(go);
                }
                // Small review contact sheets; full-resolution originals remain separately available.
                for(int page=0;page<(paths.Length+11)/12;page++)
                {
                    var sheet=new Texture2D(1600,750,TextureFormat.RGB24,false);
                    for(int j=0;j<12;j++)
                    {
                        int index=page*12+j;if(index>=paths.Length)break;
                        var input=new Texture2D(2,2);input.LoadImage(File.ReadAllBytes(Output+"/Assets/"+Path.GetFileNameWithoutExtension(paths[index])+".png"));
                        var rt=RenderTexture.GetTemporary(400,250,0,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);var previous=RenderTexture.active;
                        Graphics.Blit(input,rt);RenderTexture.active=rt;sheet.ReadPixels(new Rect(0,0,400,250),(j%4)*400,(2-j/4)*250);RenderTexture.active=previous;RenderTexture.ReleaseTemporary(rt);Object.DestroyImmediate(input);
                    }
                    sheet.Apply();File.WriteAllBytes(Output+"/catalog_"+(page+1)+".png",sheet.EncodeToPNG());Object.DestroyImmediate(sheet);
                }
                Debug.Log("Fidelity review captured "+paths.Length+" canonical assets and installed connection/detail views.");
            }
            finally{SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);}
        }
    }
}
