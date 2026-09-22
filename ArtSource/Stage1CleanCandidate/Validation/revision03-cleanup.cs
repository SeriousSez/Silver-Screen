using UnityEngine;using UnityEditor;using UnityEditor.SceneManagement;using UnityEngine.SceneManagement;using SilverScreen.Editor;
internal class CommandScript:IRunCommand{public void Execute(ExecutionResult result){
var review=Stage1CandidateReview.GetReviewScene();if(review.IsValid()&&EditorSceneManager.IsPreviewScene(review))EditorSceneManager.ClosePreviewScene(review);
var active=SceneManager.GetActiveScene();result.Log("Temporary candidate preview closed. Active scene="+active.path+" dirty="+active.isDirty+" playing="+EditorApplication.isPlaying+". Original Studio was not saved or reloaded.");
}}
