using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using SilverScreen.Presentation.Buildings;
using Object=UnityEngine.Object;

namespace SilverScreen.Editor.EnvironmentArt
{
 public static class StageSchoolB2Review
 {
  public const string Out="ArtReview/StageSchoolB2";
  public const string ScenePath=StageSchoolB2Builder.Root+"/StageSchool_B2Review.unity";
  public static void Capture()
  {
   var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
   if(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath).isLoaded)throw new InvalidOperationException("Close the generated B2 review scene before recapturing.");
   File.Copy(StageSchoolA4Review.ScenePath,ScenePath,true);AssetDatabase.ImportAsset(ScenePath);
   var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);
   UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
   try{
    var old=scene.GetRootGameObjects().First(o=>o.name.StartsWith("StageSchool_A4"));Object.DestroyImmediate(old);
    PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StageSchoolB2Builder.Prefab),scene);
    EditorSceneManager.SaveScene(scene,ScenePath);
   }finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);EditorSceneManager.CloseScene(scene,true);}
   // Render in an isolated preview scene so an open Studio/A4 scene cannot add
   // overlapping geometry or a second directional light to the comparisons.
   scene=EditorSceneManager.OpenPreviewScene(ScenePath);
   try{
    var runtime=scene.GetRootGameObjects().First(o=>o.GetComponent<StageSchoolInfrastructure>()!=null);runtime.SetActive(false);
    var master=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(StageSchoolA4Review.Prefab),scene);
    var camera=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Camera>()).First();camera.scene=scene;
    Shot(camera,"01_master",new Vector3(27,19,-30),new Vector3(0,2,-1));
    master.SetActive(false);
    runtime.SetActive(true);
    var lod=runtime.GetComponent<LODGroup>();lod.enabled=false;
    void Level(int i){for(int j=0;j<3;j++)runtime.transform.Find("LOD"+j).gameObject.SetActive(i==j);}
    Level(0);Shot(camera,"02_runtime_lod0",new Vector3(27,19,-30),new Vector3(0,2,-1));
    Shot(camera,"07_forecourt",new Vector3(15,10,-20),new Vector3(4.1f,.7f,-9.5f),54);
    Level(1);Shot(camera,"03_lod1_management",new Vector3(30,26,-34),new Vector3(0,2,-1));
    Level(2);Shot(camera,"04_lod2_distant",new Vector3(52,46,-60),new Vector3(0,2,-1));
    Level(0);var reveal=runtime.GetComponent<BuildingCutawayController>();
    reveal.SetReveal(BuildingRevealReason.DeveloperPreview,true,new Vector3(21,27,-27));
    Shot(camera,"05_revealed",new Vector3(21,27,-27),new Vector3(0,.7f,-1));
    reveal.SetReveal(BuildingRevealReason.DeveloperPreview,true,new Vector3(-23,25,25));
    Shot(camera,"06_opposite_reveal",new Vector3(-23,25,25),new Vector3(0,.7f,-1));
    reveal.Restore();for(int j=0;j<3;j++)runtime.transform.Find("LOD"+j).gameObject.SetActive(true);lod.enabled=true;
    Object.DestroyImmediate(master);
    camera.transform.position=new Vector3(27,19,-30);camera.transform.LookAt(new Vector3(0,2,-1));
   }finally{EditorSceneManager.ClosePreviewScene(scene);}
  }
  public static void Shot(Camera c,string name,Vector3 position,Vector3 target,float fov=43)
  {
   c.transform.SetPositionAndRotation(position,Quaternion.LookRotation(target-position));c.fieldOfView=fov;c.orthographic=false;c.aspect=1.6f;
   var prior=RenderTexture.active;var rt=RenderTexture.GetTemporary(1440,900,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);Texture2D image=null;
   try{
    var request=new UniversalRenderPipeline.SingleCameraRequest{destination=rt};
    for(int i=0;i<3;i++)RenderPipeline.SubmitRenderRequest(c,request);
    RenderTexture.active=rt;image=new Texture2D(1440,900,TextureFormat.RGB24,false,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();
    File.WriteAllBytes(Out+"/"+name+".png",image.EncodeToPNG());
   }finally{RenderTexture.active=prior;RenderTexture.ReleaseTemporary(rt);if(image!=null)Object.DestroyImmediate(image);}
  }
 }
}
