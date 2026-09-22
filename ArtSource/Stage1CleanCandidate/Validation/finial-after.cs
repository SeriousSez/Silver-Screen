using UnityEngine;
using SilverScreen.Editor;
internal class CommandScript:IRunCommand {
 public void Execute(ExecutionResult result) {
  Stage1CandidateReview.OpenPreviewReview();
  foreach(int face in new[]{-1,1}) foreach(int side in new[]{-1,1}) {
   float x=side*7.54f,z=face<0?-24.04f:.04f;
   string corner=(face<0?"front":"rear")+(side<0?"_left":"_right");
   Stage1CandidateReview.Capture("FinialClearance/after_"+corner+"_oblique",new Vector3(x+side*1.1f,8.1f,z+face*2.5f),new Vector3(x,7.55f,z+face*.18f),36,1000,1000,.02f,true);
   Stage1CandidateReview.Capture("FinialClearance/after_"+corner+"_side",new Vector3(x+side*2.6f,7.8f,z+face*.35f),new Vector3(x,7.52f,z+face*.16f),36,1000,1000,.02f,true);
   Stage1CandidateReview.Capture("FinialClearance/after_"+corner+"_roofward",new Vector3(x+side*1.6f,8.7f,z-face*1.2f),new Vector3(x,7.55f,z+face*.20f),36,1000,1000,.02f,true);
  }
  Stage1CandidateReview.CaptureMatched("FinialClearance/");
  result.Log("Four finials captured from three directions each, plus four exterior comparison views; camera and layers restored.");
 }
}
