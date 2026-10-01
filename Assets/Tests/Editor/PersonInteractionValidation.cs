using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace SilverScreen.Tests.EditMode
{
    public static class PersonInteractionValidation
    {
        private static TestRunnerApi _api;
        [MenuItem("SilverScreen/Person Interaction/Run focused validation")]
        public static void Run()
        {
            _api = ScriptableObject.CreateInstance<TestRunnerApi>();
            _api.RegisterCallbacks(new Results());
            _api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode, testNames = new[] {
                "SilverScreen.Tests.EditMode.PersonInteractionTests",
                "SilverScreen.Tests.EditMode.PersonManipulationRuntimeTests",
                "SilverScreen.Tests.RecruitmentTests",
                "SilverScreen.Tests.EditMode.StrategicWorkTests.CapacityChangesAccountForEarnedWorkBeforeChange",
                "SilverScreen.Tests.EditMode.StrategicWorkTests.SuspensionConditionsAndAssignmentChangesDoNotEarnUnavailableWork",
                "SilverScreen.Tests.EditMode.BuildingPlacementAndConstructionTests.ConstructionRequiresArrivalAndCompletionAloneUnlocksApplicants",
                "SilverScreen.Tests.EditMode.BuildingPlacementAndConstructionTests.StarterPoolContainsOnlyServiceProfessionsAndHonoursFacilityCapacity"
            }}));
        }
        private sealed class Results : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                Directory.CreateDirectory("Temp/PersonInteractionValidation");
                TestRunnerApi.SaveResultToFile(result, "Temp/PersonInteractionValidation/results.xml");
                Debug.Log($"Person interaction focused validation: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.");
                Object.DestroyImmediate(_api); _api = null;
            }
        }
    }
}
