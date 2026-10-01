using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
using SilverScreen.Editor.EnvironmentArt;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Selection;

namespace SilverScreen.Tests.Editor
{
 public class StageSchoolB2InputValidation
 {
  const string Report="ArtReview/StageSchoolB2/input-regression.txt";
  static void Note(string s){File.AppendAllText(Report,s+"\n");}
  public static void Run(){ScriptableObject.CreateInstance<TestRunnerApi>().Execute(new ExecutionSettings(new Filter{testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,testNames=new[]{"SilverScreen.Tests.Editor.StageSchoolB2InputValidation.HoverAndLods"}}));}
  [UnityTest] public IEnumerator HoverAndLods()
  {
   File.WriteAllText(Report,"Focused input regression after the full construction/navigation session.\n");
   Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty,Is.False);
   EditorSceneManager.OpenScene(StageSchoolB2Review.ScenePath);
   yield return new EnterPlayMode();yield return null;
   yield return InputProof();
   yield return new ExitPlayMode();
  }
  static IEnumerator Hold(Mouse mouse,Vector2 pixel,float seconds)
  {
   float until=Time.realtimeSinceStartup+seconds;
   while(Time.realtimeSinceStartup<until){InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel});yield return null;}
  }
  static IEnumerator InputProof()
  {
   var camera=Camera.main;Assert.That(camera,Is.Not.Null);camera.orthographic=false;camera.fieldOfView=43;
   var root=Object.FindAnyObjectByType<StageSchoolInfrastructure>().gameObject;root.transform.rotation=Quaternion.Euler(0,37,0);
   var focus=root.transform.TransformPoint(new Vector3(0,2,-1));camera.transform.position=focus+new Vector3(30,27,-34);camera.transform.LookAt(focus);
   var selection=new GameObject("Actual selection input regression").AddComponent<StudioSelectionController>();
   yield return null;
   var reveal=root.GetComponent<BuildingCutawayController>();reveal.Restore();
   var mouse=Mouse.current??InputSystem.AddDevice<Mouse>();var key=Keyboard.current??InputSystem.AddDevice<Keyboard>();
   var pixel=(Vector2)camera.WorldToScreenPoint(root.transform.TransformPoint(new Vector3(0,5,-1)));
   var ray=camera.ScreenPointToRay(pixel);Note("Ray intersects A4 local selection volume="+reveal.Intersects(ray,out var distance)+"; distance="+distance+"; pixel="+pixel);
   if(Physics.Raycast(ray,out var hit,distance+.01f,~0,QueryTriggerInteraction.Ignore))Note("Foreground physics hit="+hit.transform.name+" child="+hit.transform.IsChildOf(root.transform));
   InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel});yield return null;
   Note("Immediate reveal="+reveal.IsRevealed);
   // Repeat the held input each frame so native Editor pointer updates cannot replace it.
   yield return Hold(mouse,pixel,.8f);
   Note("Held pointer="+mouse.position.ReadValue()+"; reasons="+reveal.Reasons);
   Assert.That(reveal.Reasons.HasFlag(BuildingRevealReason.PointerHover),Is.True,"Actual sustained pointer must reveal");Note("PASS actual hover intent");
   InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel,buttons=1});yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=pixel});yield return null;
   Assert.That(selection.SelectedBuilding,Is.EqualTo(root.GetComponent<StudioBuildingView>()));Note("PASS click pin");
   var outside=new Vector2(-100,-100);yield return Hold(mouse,outside,.8f);
   Assert.That(reveal.IsRevealed,Is.True);Note("PASS pin survives pointer departure");
   var lod=root.GetComponent<LODGroup>();
   for(int i=0;i<3;i++){
    float screenHeight=new[]{.60f,.24f,.08f}[i];float range=lod.size*QualitySettings.lodBias/(2*Mathf.Tan(camera.fieldOfView*Mathf.Deg2Rad/2)*screenHeight);
    camera.transform.position=focus+Quaternion.Euler(0,i*120,0)*new Vector3(0,.62f,-.78f).normalized*range;camera.transform.LookAt(focus);
    yield return Hold(mouse,outside,.6f);
    Note("Automatic LOD "+i+" screen-relative height="+screenHeight+" lodBias="+QualitySettings.lodBias+" group enabled="+lod.enabled);
    Assert.That(lod.enabled,Is.True);Assert.That(reveal.Groups.Where(g=>g.Roof).SelectMany(g=>g.Renderers).All(r=>r.forceRenderingOff),Is.True);
   }
   Note("PASS orbit through close, management and distant LOD ranges preserves reveal");
   InputSystem.QueueStateEvent(key,new KeyboardState(Key.Escape));yield return null;InputSystem.QueueStateEvent(key,new KeyboardState());yield return Hold(mouse,outside,.9f);
   Assert.That(reveal.IsRevealed,Is.False);Note("PASS actual Escape release");
   Assert.That(root.GetComponentsInChildren<Renderer>().All(r=>!r.forceRenderingOff),Is.True);
   Note("PASS restored opaque materials; closed-building deep-detail enabled renderers="+root.GetComponentsInChildren<Renderer>().Count(r=>r.enabled));
   Note("PASS focused input regression complete");
  }
 }
}
