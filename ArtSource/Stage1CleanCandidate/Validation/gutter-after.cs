using UnityEngine;using SilverScreen.Editor;
internal class CommandScript:IRunCommand {public void Execute(ExecutionResult result){
Stage1CandidateReview.OpenPreviewReview();
foreach(int side in new[]{-1,1}) {
 string sn=side>0?"hero":"opposite";
 Stage1CandidateReview.Capture("GutterClearance/after_"+sn+"_run_front",new Vector3(side*13,10,-28),new Vector3(side*7.9f,6.9f,-13),46,1500,850,.02f,true);
 Stage1CandidateReview.Capture("GutterClearance/after_"+sn+"_run_rear",new Vector3(side*13,10,4),new Vector3(side*7.9f,6.9f,-11),46,1500,850,.02f,true);
 Stage1CandidateReview.Capture("GutterClearance/after_"+sn+"_midrun",new Vector3(side*10.1f,8,-12),new Vector3(side*8.05f,7.02f,-12),40,1200,1000,.02f,true);
 foreach(int end in new[]{-1,1}) {
  float z=-12+end*11.4f;string label=sn+(end<0?"_front":"_rear");
  Stage1CandidateReview.Capture("GutterClearance/after_"+label+"_oblique",new Vector3(side*10.1f,8.5f,z-end*2.3f),new Vector3(side*7.99f,7.02f,z),43,1200,1000,.02f,true);
  Stage1CandidateReview.Capture("GutterClearance/after_"+label+"_profile",new Vector3(side*10.2f,7.15f,z+end*.6f),new Vector3(side*8.15f,6.95f,z),40,1200,1000,.02f,true);
  Stage1CandidateReview.Capture("GutterClearance/after_"+label+"_outlet",new Vector3(side*8.65f,8.0f,z-end*.3f),new Vector3(side*8.2f,6.92f,z),38,1000,1000,.02f,true);
  Stage1CandidateReview.Capture("GutterClearance/after_"+label+"_downpipe",new Vector3(side*12.8f,3.6f,z+end*.7f),new Vector3(side*8.1f,3.5f,z),78,850,1300,.02f,true);
 }
}
Stage1CandidateReview.CaptureMatched("GutterClearance/");
result.Log("Both complete gutter runs, four transitions/outlets/downpipes and four exterior overviews captured; camera and layers restored.");
}}
