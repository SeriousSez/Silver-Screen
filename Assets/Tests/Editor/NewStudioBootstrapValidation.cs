using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Finance;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Camera;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Finance;
using SilverScreen.Presentation.Movie;
using SilverScreen.Presentation.Recruitment;
using SilverScreen.Presentation.SimulationTime;
using SilverScreen.Presentation.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace SilverScreen.Tests.EditMode
{
    public static class NewStudioBootstrapValidation
    {
        private static TestRunnerApi _api;
        [MenuItem("SilverScreen/New Studio/Run focused validation")]
        public static void Run()=>Execute(new[]{
            "SilverScreen.Tests.RecruitmentTests.NewStudioOpeningWaveUsesNormalRecruitmentWithFourBuildersAndTwoGroundskeepers",
            "SilverScreen.Tests.EditMode.ContextualCarryStudioTests.StudioServicesFloorHiringKeepsArrivalIdentityPlacementAndCutaway",
            "SilverScreen.Tests.EditMode.SimulationSpeedTests.StudioHudScalesClockPeopleAndAnimationButNotCameraAndPreservesModalPause"
        });
        private static void Execute(string[] testNames)
        {
            _api=ScriptableObject.CreateInstance<TestRunnerApi>();_api.RegisterCallbacks(new Results());
            _api.Execute(new ExecutionSettings(new Filter{testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,testNames=testNames}));
        }
        private sealed class Results:ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun){}public void TestStarted(ITestAdaptor test){}public void TestFinished(ITestResultAdaptor result){}
            public void RunFinished(ITestResultAdaptor result){Directory.CreateDirectory("Temp/NewStudioBootstrap");TestRunnerApi.SaveResultToFile(result,"Temp/NewStudioBootstrap/results.xml");Debug.Log($"New Studio focused validation: {result.PassCount} passed, {result.FailCount} failed, {result.SkipCount} skipped.");Object.DestroyImmediate(_api);_api=null;}
        }
    }
}
