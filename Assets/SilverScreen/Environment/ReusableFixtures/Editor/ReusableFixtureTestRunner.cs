using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SilverScreen.Editor.ReusableAssets
{
    public static class ReusableFixtureTestRunner
    {
        private static TestRunnerApi _api;
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory(ReusableFixtureBuilder.ReviewRoot);
                TestRunnerApi.SaveResultToFile(result,ReusableFixtureBuilder.ReviewRoot+"/editmode-results.xml");
                Debug.Log("Reusable M1 tests: "+result.PassCount+" passed, "+result.FailCount+" failed, "+result.SkipCount+" skipped.");
                _api.UnregisterCallbacks(this);Object.DestroyImmediate(_api);_api=null;
            }
        }
        [MenuItem("SilverScreen/Art/Reusable Fixtures/3 Run fixture EditMode tests")]
        public static void Run()
        {
            if(_api!=null)throw new System.InvalidOperationException("Fixture tests already running.");
            _api=ScriptableObject.CreateInstance<TestRunnerApi>();_api.RegisterCallbacks(new Results());
            _api.Execute(new ExecutionSettings(new Filter{testMode=TestMode.EditMode,groupNames=new[]{"^SilverScreen.Tests.EditMode.(ReusableFixtureTests|DisplayArtworkTests)"}}));
        }
    }
}
