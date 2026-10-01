using UnityEngine;
using UnityEditor.TestTools.TestRunner.Api;
namespace SilverScreen.Tests.EditMode
{
 public static class RecruitmentC11Validation
 {
  public static void RunDomain()=>Run(new[]{"SilverScreen.Tests.EditMode.RecruitmentStarterIntakeTests","SilverScreen.Tests.EditMode.StageSchoolApplicantTests","SilverScreen.Tests.RecruitmentTests"},"domain-results.xml");
  public static void Run(string[] names,string file){var api=ScriptableObject.CreateInstance<TestRunnerApi>();api.RegisterCallbacks(new Results(api,file));api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,testNames=names}));}
  sealed class Results:ICallbacks
  {
   readonly TestRunnerApi api;readonly string file;
   public Results(TestRunnerApi a,string f){api=a;file=f;}
   public void RunStarted(ITestAdaptor t){} public void TestStarted(ITestAdaptor t){} public void TestFinished(ITestResultAdaptor t){}
   public void RunFinished(ITestResultAdaptor r){TestRunnerApi.SaveResultToFile(r,"ArtReview/RecruitmentC11/"+file);Object.DestroyImmediate(api);}
  }
 }
}
