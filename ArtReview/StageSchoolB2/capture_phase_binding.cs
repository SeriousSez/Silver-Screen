var scene=UnityEditor.SceneManagement.EditorSceneManager.OpenPreviewScene(SilverScreen.Editor.EnvironmentArt.StageSchoolB2Review.ScenePath);
try{
 var roots=scene.GetRootGameObjects();
 var root=System.Linq.Enumerable.First(roots,g=>g.GetComponent<SilverScreen.Presentation.Buildings.StageSchoolInfrastructure>()!=null);
 var camera=System.Linq.Enumerable.First(System.Linq.Enumerable.SelectMany(roots,g=>g.GetComponentsInChildren<UnityEngine.Camera>()));camera.scene=scene;
 root.GetComponent<UnityEngine.LODGroup>().enabled=false;
 for(int i=0;i<3;i++)root.transform.Find("LOD"+i).gameObject.SetActive(i==1);
 var phase=root.GetComponent<SilverScreen.Presentation.Buildings.ConstructionPhaseVisuals>();phase.Initialize(root,"content.stage-school.1930",5.95f);phase.Apply(SilverScreen.Domain.Buildings.ConstructionPhase.Structure);
 SilverScreen.Editor.EnvironmentArt.StageSchoolB2Review.Shot(camera,"11_final_phase_binding_lod1",new UnityEngine.Vector3(27,19,-30),new UnityEngine.Vector3(0,2,-1));
 result.Log("Final LOD1 structure-phase floors captured; presentation-only check, not another completion run.");
}finally{UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);}
