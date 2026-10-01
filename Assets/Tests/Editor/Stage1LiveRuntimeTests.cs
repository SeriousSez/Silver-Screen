using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Buildings;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Movie;
using SilverScreen.Presentation.SimulationTime;

namespace SilverScreen.Tests.EditMode
{
    public sealed class Stage1LiveRuntimeTests
    {
        private static string Output => Stage1LiveValidation.Output;
        private const string BackgroundKey = "SilverScreen.Stage1Validation.RunInBackground";
        private const string InputFocusKey = "SilverScreen.Stage1Validation.InputBackgroundBehavior";
        private const string EditorInputFocusKey = "SilverScreen.Stage1Validation.EditorInputBehavior";
        private static InputDevice[] _mutedDevices;
        private static void PrepareRuntimeValidation()
        {
            SessionState.SetBool(BackgroundKey, Application.runInBackground);
            Application.runInBackground = true;
        }
        private static void IsolateHardwareInput()
        {
            Application.runInBackground = true;
            // Batch Editor has no focused Game view. Synthetic UI and world input
            // must use the same focus policy for this fixture; restore it on cleanup.
            SessionState.SetInt(InputFocusKey, (int)InputSystem.settings.backgroundBehavior);
            SessionState.SetInt(EditorInputFocusKey, (int)InputSystem.settings.editorInputBehaviorInPlayMode);
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _mutedDevices = InputSystem.devices.Where(d => d.enabled && (d is Keyboard || d is Mouse)).ToArray();
            foreach (var device in _mutedDevices) InputSystem.DisableDevice(device);
        }
        private static void RestoreHardwareInput()
        {
            if (_mutedDevices != null)
                foreach (var device in _mutedDevices) if (device.added) InputSystem.EnableDevice(device);
            _mutedDevices = null;
            int inputBackground = SessionState.GetInt(InputFocusKey, -1);
            if (inputBackground >= 0)
            {
                InputSystem.settings.backgroundBehavior = (InputSettings.BackgroundBehavior)inputBackground;
                SessionState.EraseInt(InputFocusKey);
            }
            int editorInput = SessionState.GetInt(EditorInputFocusKey, -1);
            if (editorInput >= 0)
            {
                InputSystem.settings.editorInputBehaviorInPlayMode = (InputSettings.EditorInputBehaviorInPlayMode)editorInput;
                SessionState.EraseInt(EditorInputFocusKey);
            }
        }
        private static void Record(string message) => File.AppendAllText(Path.Combine(Output,"live-runtime.txt"),DateTime.UtcNow.ToString("O")+" "+message+Environment.NewLine);

        [MenuItem("SilverScreen/Stage 1 Live/6 Run continuous Studio validation")]
        public static void RunTests()
        {
            var api=ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(new Filter {testMode=UnityEditor.TestTools.TestRunner.Api.TestMode.EditMode,testNames=new[]{typeof(Stage1LiveRuntimeTests).FullName}}));
        }

        [UnityTest, Timeout(600000)]
        public IEnumerator EmployeesTraverseDoorAndReachInteriorWithoutPenetration()
        {
            Assert.That(SceneManager.GetActiveScene().isDirty,Is.False);
            EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
            Directory.CreateDirectory(Output);File.WriteAllText(Path.Combine(Output,"live-runtime.txt"),"Stage 1 continuous Unity runtime validation\n");
            PrepareRuntimeValidation();
            yield return new EnterPlayMode(); yield return RunRoutes(); RestoreHardwareInput(); yield return new ExitPlayMode();
        }
        private static IEnumerator RunRoutes()
        {
            IsolateHardwareInput();
            yield return null;yield return null;
            var stage=UnityEngine.Object.FindObjectsByType<SoundStageIdentity>().Single(s=>s.FacilityId=="stage-1");
            var root=stage.transform;var time=UnityEngine.Object.FindAnyObjectByType<SimulationTimeDriver>();
            var manager=UnityEngine.Object.FindAnyObjectByType<StudioEmployeeManager>();
            var agents=manager.AllAgents.ToArray();var arrived=new HashSet<EmployeeAgent>();var crossed=new HashSet<EmployeeAgent>();
            // Startup idle movement may already have taken an employee inside. Establish
            // the entrance-test precondition with ordinary routes, never by teleporting.
            var outside=new HashSet<EmployeeAgent>();
            for(int i=0;i<agents.Length;i++)
            {
                var agent=agents[i];
                Assert.That(NavMesh.SamplePosition(root.TransformPoint(new Vector3(i*1.5f,0,-18)),out var start,1f,NavMesh.AllAreas),Is.True);
                Assert.That(agent.TryAssignTaskDestination(start.position,EmployeeIntent.IdleWander,
                    ()=>{agent.Employee.SetState(EmployeeState.Working);outside.Add(agent);}),Is.True);
            }
            yield return WaitFor(()=>outside.Count==agents.Length,100,()=>"Routing all employees outside before measured entrance traversal");
            Assert.That(agents.All(a=>root.InverseTransformPoint(a.transform.position).z<-14),Is.True);
            var previous=agents.ToDictionary(a=>a,a=>root.InverseTransformPoint(a.transform.position));
            var stations=stage.GetComponent<ProductionStationLayout>();
            var camera=UnityEngine.Camera.main;var controller=camera.GetComponentInParent<SilverScreen.Presentation.Camera.StudioCameraController>();
            if(controller!=null)controller.enabled=false;
            camera.transform.position=root.TransformPoint(new Vector3(11,4,-18));camera.transform.LookAt(root.TransformPoint(new Vector3(5.7f,1,-12)));
            for(int i=0;i<agents.Length;i++)
            {
                var agent=agents[i];var nav=agent.GetComponent<NavMeshAgent>();
                // Renderer.bounds is a rotated world AABB, not the body's diameter.
                var bodyBounds=agent.GetComponent<MeshFilter>().sharedMesh.bounds;
                Assert.That(bodyBounds.size.x,Is.EqualTo(.6f).Within(.001));
                Assert.That(bodyBounds.size.y,Is.EqualTo(2f).Within(.001));
                Assert.That(nav.radius,Is.EqualTo(.35f));Assert.That(agent.GetComponent<CapsuleCollider>().radius,Is.EqualTo(.35f));
                Assert.That(stations.TryGetPose((ProductionStationType)i,out var pose),Is.True);
                Assert.That(agent.TryAssignTaskDestination(pose.position,EmployeeIntent.IdleWander,()=>{agent.transform.rotation=pose.rotation;agent.Employee.SetState(EmployeeState.Working);arrived.Add(agent);}),Is.True);
            }
            var overlaps=new Collider[128];double begin=UnityEngine.Time.timeAsDouble,realBegin=UnityEngine.Time.realtimeSinceStartupAsDouble,nextDiagnostic=realBegin;
            float maxPenetration=0,minimumSeparation=float.MaxValue;
            var movement = new EntranceMovementEvidence(root, agents);
            while(arrived.Count<agents.Length)
            {
                movement.Sample();
                if(UnityEngine.Time.realtimeSinceStartupAsDouble>=nextDiagnostic)
                {RecordNavigation();nextDiagnostic+=5;}
                // Navigation advances on local Unity time. A background Editor can spend
                // seconds between frames; that is not an employee congestion timeout.
                Assert.That(UnityEngine.Time.timeAsDouble-begin,Is.LessThan(100),"Route timeout: "+string.Join(",",agents.Where(a=>!arrived.Contains(a)).Select(a=>a.name+" "+root.InverseTransformPoint(a.transform.position))));
                int speedIndex=(int)((UnityEngine.Time.timeAsDouble-begin)/8)%3;
                time.Clock.SetSpeed(new[]{SimulationSpeed.Normal,SimulationSpeed.Fast,SimulationSpeed.VeryFast}[speedIndex]);
                foreach(var agent in agents)
                {
                    var nav=agent.GetComponent<NavMeshAgent>();Assert.That(nav.speed,Is.EqualTo(3.5f));Assert.That(nav.acceleration,Is.EqualTo(14));
                    var local=root.InverseTransformPoint(agent.transform.position);
                    if(previous[agent].z<-12 && local.z>=-12)
                    {Assert.That(local.x,Is.InRange(5.35f,6.25f),"Crossed facade outside personnel opening");crossed.Add(agent);Record("Threshold crossing "+agent.name+" "+local.ToString("F3"));}
                    previous[agent]=local;
                    if(Mathf.Abs(local.x)<8.5f && local.z<12.7f && local.z>-14)
                    {
                        var capsule=agent.GetComponent<CapsuleCollider>();var centre=agent.transform.position;float r=capsule.radius;
                        int count=Physics.OverlapCapsuleNonAlloc(centre+Vector3.up*(1-r),centre-Vector3.up*(1-r),r,overlaps,~0,QueryTriggerInteraction.Ignore);
                        for(int h=0;h<count;h++)
                        {
                            var other=overlaps[h];if(!other.transform.IsChildOf(root))continue;
                            if(Physics.ComputePenetration(capsule,centre,agent.transform.rotation,other,other.transform.position,other.transform.rotation,out _,out float depth))
                            {
                                // NavMesh step climbing interpolates over the 8 cm walkable apron.
                                // This is floor contact, unlike penetration of a door or wall.
                                if(other.name=="FrontRightEntryThresholdApron")
                                    Assert.That(depth,Is.LessThan(.08f),"Excessive threshold step contact");
                                else
                                {maxPenetration=Mathf.Max(maxPenetration,depth);Assert.That(depth,Is.LessThan(.005f),agent.name+" penetrated "+other.name+" at "+local.ToString("F3")+" by "+depth);}
                            }
                        }
                    }
                    foreach(var other in agents.Where(a=>a!=agent))
                    {var d=agent.transform.position-other.transform.position;d.y=0;
                        if(d.magnitude<.69f)
                        {
                            RecordSpacingFailure(root, agent, other, d.magnitude);
                            movement.Closest(agent,other,d.magnitude);
                            movement.Write();
                            Record("Stopped at first spacing failure. Maximum physical penetration="+maxPenetration.ToString("R")+
                                " minimum employee spacing="+Mathf.Min(minimumSeparation,d.magnitude).ToString("R"));
                            Assert.That(d.magnitude,Is.GreaterThanOrEqualTo(.69f),"Stop at the first failing sample; see pair diagnostics.");
                        }
                        if(d.magnitude<minimumSeparation) movement.Closest(agent,other,d.magnitude);
                        minimumSeparation=Mathf.Min(minimumSeparation,d.magnitude);}
                }
                yield return null;
            }
            Assert.That(crossed.Count,Is.EqualTo(agents.Length));Assert.That(minimumSeparation,Is.GreaterThanOrEqualTo(.69f));
            movement.Write();
            Assert.That(movement.MaximumHorizontalSpeed, Is.LessThanOrEqualTo(3.55f), "Real displacement must respect ordinary 3.5 m/s walking speed (5 cm/s sampling allowance)");
            Record("Five employees arrived inside through personnel door. Maximum physical penetration="+maxPenetration+" minimum employee spacing="+minimumSeparation+"; natural speed at all current strategic speeds. Local seconds="+(UnityEngine.Time.timeAsDouble-begin)+" real seconds="+(UnityEngine.Time.realtimeSinceStartupAsDouble-realBegin));
            camera.transform.position=root.TransformPoint(new Vector3(0,3,-8.5f));camera.transform.LookAt(root.TransformPoint(new Vector3(0,1.2f,1.5f)));
            yield return Capture("interior-production-stations");
            // An ordinary route back to the lot must use the same opening, never a closed door.
            var route=new NavMeshPath();Assert.That(NavMesh.CalculatePath(agents[0].transform.position,root.TransformPoint(new Vector3(0,0,-16)),NavMesh.AllAreas,route),Is.True);
            Assert.That(route.status,Is.EqualTo(NavMeshPathStatus.PathComplete));Assert.That(route.corners.Any(p=>root.InverseTransformPoint(p).x>5),Is.True);
        }

        // Measure real displacement, independently of NavMeshAgent.velocity. Keep
        // only extrema in memory so diagnostics do not change traversal timing.
        private sealed class EntranceMovementEvidence
        {
            private readonly Transform root;
            private readonly EmployeeAgent[] agents;
            private readonly Vector3[] previous;
            private readonly bool[] previousLink;
            private double sampledAt;
            private float maxHorizontal, maxSpatial, maxReported;
            private string fastestName, closestFirst, closestSecond;
            private Vector3 fastestPosition, closestA, closestB;
            private bool fastestLink, closestALink, closestBLink;
            private float closest = float.MaxValue;
            public float MaximumHorizontalSpeed => maxHorizontal;
            public EntranceMovementEvidence(Transform root,EmployeeAgent[] agents)
            {
                this.root=root;this.agents=agents;
                previous=agents.Select(a=>a.transform.position).ToArray();
                previousLink=new bool[agents.Length];sampledAt=UnityEngine.Time.timeAsDouble;
            }
            public void Sample()
            {
                double now=UnityEngine.Time.timeAsDouble;float dt=(float)(now-sampledAt);
                if(dt<=0)return;
                for(int i=0;i<agents.Length;i++)
                {
                    var agent=agents[i];var nav=agent.GetComponent<NavMeshAgent>();
                    Vector3 delta=(agent.transform.position-previous[i])/dt;
                    float horizontal=new Vector2(delta.x,delta.z).magnitude;
                    if(horizontal>maxHorizontal){maxHorizontal=horizontal;fastestName=agent.name;fastestPosition=root.InverseTransformPoint(agent.transform.position);fastestLink=nav.isOnOffMeshLink||previousLink[i];}
                    maxSpatial=Mathf.Max(maxSpatial,delta.magnitude);maxReported=Mathf.Max(maxReported,nav.velocity.magnitude);
                    previous[i]=agent.transform.position;previousLink[i]=nav.isOnOffMeshLink;
                }
                sampledAt=now;
            }
            public void Closest(EmployeeAgent first,EmployeeAgent second,float distance)
            {
                closest=distance;closestFirst=first.name;closestSecond=second.name;
                closestA=root.InverseTransformPoint(first.transform.position);closestB=root.InverseTransformPoint(second.transform.position);
                closestALink=first.GetComponent<NavMeshAgent>().isOnOffMeshLink;closestBLink=second.GetComponent<NavMeshAgent>().isOnOffMeshLink;
            }
            public void Write()
            {
                Record("Movement maximumHorizontal="+maxHorizontal.ToString("R")+" maximumSpatial="+maxSpatial.ToString("R")+" maximumReported="+maxReported.ToString("R")+" fastest="+fastestName+" local="+fastestPosition.ToString("F6")+" usingLink="+fastestLink);
                Record("Closest pair="+closestFirst+" / "+closestSecond+" separation="+closest.ToString("R")+" first="+closestA.ToString("F6")+" second="+closestB.ToString("F6")+" firstLink="+closestALink+" secondLink="+closestBLink);
            }
        }

        // Capture the failing sample before agents advance; periodic route logs alone
        // cannot identify a transient convergence or automatic off-mesh traversal.
        private static void RecordSpacingFailure(Transform root, EmployeeAgent first, EmployeeAgent second, float separation)
        {
            Record("Spacing failure sample separation="+separation.ToString("R")+
                " frame="+UnityEngine.Time.frameCount+" deltaTime="+UnityEngine.Time.deltaTime.ToString("R"));
            foreach(var agent in new[]{first,second})
            {
                var nav=agent.GetComponent<NavMeshAgent>();
                var local=root.InverseTransformPoint(agent.transform.position);
                var velocity=root.InverseTransformDirection(nav.velocity);
                Record(agent.name+" local="+local.ToString("F6")+
                    " velocity="+velocity.ToString("F6")+
                    " desiredVelocity="+root.InverseTransformDirection(nav.desiredVelocity).ToString("F6")+
                    " destination="+root.InverseTransformPoint(nav.destination).ToString("F6")+
                    " nextPosition="+root.InverseTransformPoint(nav.nextPosition).ToString("F6")+
                    " state="+agent.Employee.CurrentState+" path="+nav.pathStatus+
                    " pending="+nav.pathPending+" stopped="+nav.isStopped+" onMesh="+nav.isOnNavMesh+
                    " onLink="+nav.isOnOffMeshLink+" autoTraverse="+nav.autoTraverseOffMeshLink+
                    " avoidance="+nav.obstacleAvoidanceType+" priority="+nav.avoidancePriority+
                    " radius="+nav.radius+" speed="+nav.speed+" acceleration="+nav.acceleration+
                    " remaining="+nav.remainingDistance+" stoppingDistance="+nav.stoppingDistance+
                    " corners="+string.Join(";",nav.path.corners.Select(p=>root.InverseTransformPoint(p).ToString("F6"))));
                if(nav.isOnOffMeshLink)
                {
                    var link=nav.currentOffMeshLinkData;
                    Record(agent.name+" linkStart="+root.InverseTransformPoint(link.startPos).ToString("F6")+
                        " linkEnd="+root.InverseTransformPoint(link.endPos).ToString("F6")+" type="+link.linkType);
                }
                var capsule=agent.GetComponent<CapsuleCollider>();
                foreach(var obstacle in root.GetComponentsInChildren<Collider>().Where(c=>c.enabled&&!c.isTrigger))
                    if(Physics.ComputePenetration(capsule,agent.transform.position,agent.transform.rotation,
                        obstacle,obstacle.transform.position,obstacle.transform.rotation,out var direction,out float depth))
                        Record(agent.name+" contact="+obstacle.name+" depth="+depth.ToString("R")+" direction="+direction.ToString("F6"));
            }
            var firstCapsule=first.GetComponent<CapsuleCollider>();var secondCapsule=second.GetComponent<CapsuleCollider>();
            bool overlap=Physics.ComputePenetration(firstCapsule,first.transform.position,first.transform.rotation,
                secondCapsule,second.transform.position,second.transform.rotation,out var pairDirection,out float pairDepth);
            var hinge=root.Find("InteriorNavigation/FrontRightPersonnelHinge");
            Record("Pair physicalOverlap="+overlap+" depth="+(overlap?pairDepth:0).ToString("R")+
                " direction="+pairDirection.ToString("F6")+" doorAngle="+hinge.localEulerAngles.ToString("F6")+
                " doorPassable="+root.GetComponent<HeldOpenStageAccess>().IsPassable);
        }

        [UnityTest, Timeout(300000)]
        public IEnumerator TwoSceneProductionLiveInteractionRetakeReleaseAndPayouts()
        {
            Assert.That(SceneManager.GetActiveScene().isDirty,Is.False);
            EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
            PrepareRuntimeValidation();
            yield return new EnterPlayMode();yield return RunProduction();RestoreHardwareInput();yield return new ExitPlayMode();
        }
        private static IEnumerator RunProduction()
        {
            IsolateHardwareInput();
            yield return null;yield return null;
            var time=UnityEngine.Object.FindAnyObjectByType<SimulationTimeDriver>();var driver=UnityEngine.Object.FindAnyObjectByType<MovieProductionDriver>();
            var people=UnityEngine.Object.FindAnyObjectByType<StudioEmployeeManager>();var coordinator=driver.Coordinator;
            Assert.That(time.Clock.RealSecondsPerSimulatedMinute,Is.EqualTo(1));Assert.That(UnityEngine.Time.timeScale,Is.EqualTo(1));
            var actors=people.AllEmployees.Where(p=>p.Role==EmployeeRole.Actor).Take(2).ToArray();Assert.That(actors.Length,Is.EqualTo(2));
            var director=people.AllEmployees.First(p=>p.Role==EmployeeRole.Director);
            var greenlit=coordinator.GreenlightScreenplay(MovieReleaseServiceTests.CreateCompletedScreenplay(2));Assert.That(greenlit.Succeeded,Is.True);
            var movie=greenlit.Movie;coordinator.SetProductionControlMode(movie,ProductionControlMode.Manual);
            for(int i=0;i<2;i++)coordinator.AssignActorToRole(movie,movie.Roles[i],actors[i]);
            coordinator.AssignDirector(movie,director);
            yield return WaitFor(()=>movie.Roles.All(r=>r.IsCastingActive),50,()=>coordinator.StatusMessage);
            time.Clock.AdvanceTo(time.Clock.Now+SimulationDuration.FromMinutes(60));yield return Drain(time);
            yield return WaitFor(()=>coordinator.CurrentProductionPhase==ProductionPhase.Filming,100,()=>coordinator.StatusMessage);
            var stage=UnityEngine.Object.FindObjectsByType<SoundStageIdentity>().Single(s=>s.FacilityId=="stage-1");var root=stage.transform;
            var slate=stage.GetComponent<PrototypeSlateSequence>();var beats=stage.GetComponent<PrototypeScreenplayBeatSequence>();var live=stage.GetComponent<LiveFilmingPlayback>();
            var take=coordinator.ActiveTake;var stored=take.PerformanceResults.ToArray();Assert.That(stored.Length,Is.GreaterThan(0));
            Assert.That(slate.IsRunning,Is.True);Record("Two-scene movie reached actual interior filming; slate active. Work="+coordinator.ActiveTakeWork.Completed.Units);
            foreach(var actor in actors) {var p=root.InverseTransformPoint(people.GetAgent(actor).transform.position);Assert.That(p.z,Is.InRange(1,2));Assert.That(Mathf.Abs(p.x),Is.LessThan(2.4));}
            var camera=UnityEngine.Camera.main;var controller=camera.GetComponentInParent<SilverScreen.Presentation.Camera.StudioCameraController>();if(controller!=null)controller.enabled=false;
            camera.transform.position=root.TransformPoint(new Vector3(0,3,-8.5f));camera.transform.LookAt(root.TransformPoint(new Vector3(0,1.25f,1.5f)));
            yield return Capture("slate-interior");
            yield return WaitFor(()=>beats.IsRunning,40,()=>"Slate="+slate.IsRunning+" "+coordinator.StatusMessage);yield return Capture("screenplay-beat-interior");
            yield return WaitFor(()=>!beats.IsRunning,60,()=>"Beat="+beats.CurrentBeatIndex);
            Assert.That(take.Status,Is.EqualTo(MovieTakeStatus.Recording));Assert.That(coordinator.ActiveTakeWork.Completed.Units,Is.LessThan(480));
            Record("Slate and all beats ended without completing strategic work. Stored results="+stored.Length);
            // Drive the actual input/raycast/building double-click path, without calling Live.TryEnter.
            camera.transform.position=root.TransformPoint(new Vector3(15,12,-21));camera.transform.LookAt(root.TransformPoint(new Vector3(0,5,-5)));
            var screen=camera.WorldToScreenPoint(root.TransformPoint(new Vector3(0,6,-12)));var ray=camera.ScreenPointToRay(screen);
            Assert.That(Physics.Raycast(ray,out var hit,300),Is.True);Assert.That(hit.collider.GetComponentInParent<StudioBuildingView>(),Is.SameAs(stage.GetComponent<StudioBuildingView>()));
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                // Use the slate's actual rectangle: its overlap with the projected
                // building point varies with the Editor Game view resolution.
                var panel=GameObject.Find("ProductionsPanel").GetComponent<RectTransform>();
                var coveredScreen=RectTransformUtility.WorldToScreenPoint(null,panel.TransformPoint(panel.rect.center));
                InputSystem.QueueStateEvent(mouse,new MouseState{position=coveredScreen});yield return null;yield return null;
                Assert.That(UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(), Is.True);
                for(int i=0;i<2;i++)
                {InputSystem.QueueStateEvent(mouse,new MouseState{position=coveredScreen}.WithButton(MouseButton.Left));yield return null;InputSystem.QueueStateEvent(mouse,new MouseState{position=coveredScreen});yield return null;}
                Assert.That(live.IsViewing, Is.False, "UI-covered clicks must not enter Live Camera");
                var navigation=UnityEngine.Object.FindAnyObjectByType<SilverScreen.Presentation.UI.ManagementNavigation>();
                navigation.transform.Find("ProductionsButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
                InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});
                yield return null;yield return null;
                Assert.That(navigation.ActivePanel, Is.EqualTo(SilverScreen.Presentation.UI.ManagementPanel.None));
                Assert.That(UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(), Is.False);
                for(int i=0;i<2;i++)
                {
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=screen}.WithButton(MouseButton.Left));yield return null;
                    InputSystem.QueueStateEvent(mouse,new MouseState{position=screen});yield return null;
                    if(i==0)Assert.That(live.IsViewing,Is.False,"Single click must not enter Live Camera");
                }
                yield return WaitFor(()=>live.IsViewing,3,()=>"Actual building double-click did not enter Live Camera View");
                var date=time.Clock.Now;var work=coordinator.ActiveTakeWork.Completed.Units;
                yield return Capture("live-camera-building-double-click");yield return new WaitForSecondsRealtime(3);
                Assert.That(time.Clock.Now,Is.EqualTo(date));Assert.That(coordinator.ActiveTakeWork.Completed.Units,Is.EqualTo(work));CollectionAssert.AreEqual(stored,take.PerformanceResults);
                time.Clock.SetSpeed(SimulationSpeed.VeryFast);time.Clock.SetUserPaused(true);live.Exit();Assert.That(time.Clock.IsPaused,Is.True);
                time.Clock.SetSpeed(SimulationSpeed.Normal);Assert.That(live.TryEnter(),Is.True);live.enabled=false;Assert.That(time.Clock.IsPaused,Is.False);live.enabled=true;
                Record("Actual building double-click, Live Camera, immutable performance and owned pause restoration passed.");
            }
            finally {InputSystem.RemoveDevice(mouse);}
            time.Clock.AdvanceTo(time.Clock.Now+SimulationDuration.FromMinutes(480));yield return Drain(time);
            Assert.That(coordinator.CanKeepTake,Is.True);Assert.That(take.FilmedDuration,Is.EqualTo(TimeSpan.FromHours(8)));
            long claim=time.Reservations.GetClaims()[0].Id;Assert.That(coordinator.ShootAgain(),Is.True);
            yield return WaitFor(()=>coordinator.CurrentProductionPhase==ProductionPhase.Filming,40,()=>coordinator.StatusMessage);
            Assert.That(coordinator.ActiveTake.Id,Is.Not.EqualTo(take.Id));Assert.That(time.Reservations.GetClaims()[0].Id,Is.EqualTo(claim));
            time.Clock.AdvanceTo(time.Clock.Now+SimulationDuration.FromMinutes(480));yield return Drain(time);
            Assert.That(coordinator.KeepTake(coordinator.ActiveTake.Id),Is.True);
            yield return WaitFor(()=>coordinator.CurrentProductionPhase==ProductionPhase.Filming&&coordinator.ActiveScene.SceneNumber==2,60,()=>coordinator.StatusMessage);
            Record("Manual retake/keep passed; second screenplay scene is filming inside Stage 1.");
            time.Clock.AdvanceTo(time.Clock.Now+SimulationDuration.FromMinutes(480));yield return Drain(time);
            Assert.That(coordinator.KeepTake(coordinator.ActiveTake.Id),Is.True);Assert.That(movie.CurrentState,Is.EqualTo(MovieProductionState.Completed));Assert.That(movie.AllScenesCompleted,Is.True);
            Assert.That(time.Reservations.Count,Is.Zero);Assert.That(driver.ReleaseService.ReleaseMovie(movie).Succeeded,Is.True);
            var other=new MovieProject("live-other-release","Concurrent release","drama","Drama",25000,time.Clock.CurrentTime);
            ProductionLifecycleRegressionTests.CompleteMovie(other);
            other.TrySetProductionResult(new MovieProductionResult(60,60,60,60));Assert.That(driver.ReleaseService.ReleaseMovie(other).Succeeded,Is.True);
            time.Clock.AdvanceTo(time.Clock.Now+SimulationDuration.FromDays(28));yield return Drain(time);
            Assert.That(movie.TheatricalRun.CurrentDay,Is.EqualTo(28));Assert.That(movie.TheatricalRun.WeeksPaid,Is.EqualTo(4));Assert.That(other.TheatricalRun.WeeksPaid,Is.EqualTo(4));
            var revenue=movie.TheatricalRun.TotalStudioRevenue;time.Clock.AdvanceTo(time.Clock.Now+SimulationDuration.FromDays(2));yield return Drain(time);
            Assert.That(movie.TheatricalRun.TotalStudioRevenue,Is.EqualTo(revenue));Assert.That(time.Clock.RealSecondsPerSimulatedMinute,Is.EqualTo(1));
            Record("PASS: two scenes, retake, movie completion, release, two concurrent 28-day runs/four weekly payouts, no duplicate revenue, legacy pacing retained.");
        }
        private static IEnumerator WaitFor(Func<bool> condition,float timeout,Func<string> diagnostic)
        {double start=UnityEngine.Time.realtimeSinceStartupAsDouble,next=start;while(!condition()){if(UnityEngine.Time.realtimeSinceStartupAsDouble>=next){Record(diagnostic());RecordNavigation();next+=5;}if(UnityEngine.Time.realtimeSinceStartupAsDouble-start>timeout){Record("FAIL "+diagnostic());Assert.Fail(diagnostic());}yield return null;}}
        private static void RecordNavigation()
        {
            var stage=UnityEngine.Object.FindObjectsByType<SoundStageIdentity>().Single(s=>s.FacilityId=="stage-1");
            var root=stage.transform;var clock=UnityEngine.Object.FindAnyObjectByType<SimulationTimeDriver>().Clock;
            var link=stage.GetComponentInChildren<Unity.AI.Navigation.NavMeshLink>();
            Record("Clock paused="+clock.IsPaused+" speed="+clock.CurrentSpeed+" link="+(link!=null && link.activated)+" editorPaused="+EditorApplication.isPaused+" timeScale="+UnityEngine.Time.timeScale+" delta="+UnityEngine.Time.deltaTime+" frame="+UnityEngine.Time.frameCount);
            foreach(var a in UnityEngine.Object.FindAnyObjectByType<StudioEmployeeManager>().AllAgents)
            {
                var n=a.GetComponent<NavMeshAgent>();
                Record(a.name+" state="+a.Employee.CurrentState+" pos="+root.InverseTransformPoint(a.transform.position).ToString("F3")+" dest="+root.InverseTransformPoint(n.destination).ToString("F3")+" path="+n.pathStatus+" pending="+n.pathPending+" stopped="+n.isStopped+" onNav="+n.isOnNavMesh+" link="+n.isOnOffMeshLink+" remaining="+n.remainingDistance+" stop="+n.stoppingDistance+" velocity="+n.velocity.ToString("F3")+" desired="+n.desiredVelocity.ToString("F3")+" avoidance="+n.obstacleAvoidanceType+" corners="+string.Join(";",n.path.corners.Select(p=>root.InverseTransformPoint(p).ToString("F3"))));
            }
        }
        private static IEnumerator Drain(SimulationTimeDriver time)
        {int frames=0;while(time.Clock.HasPendingAdvance&&frames++<100)yield return null;Assert.That(time.Clock.HasPendingAdvance,Is.False);Assert.That(time.Clock.AdvanceFailure,Is.Null);}
        private static IEnumerator Capture(string name)
        {
            // Render the real runtime camera explicitly: WaitForEndOfFrame can stall
            // indefinitely when the Editor Game view is not foregrounded.
            yield return null;
            var target=RenderTexture.GetTemporary(1600,1000,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            var previous=RenderTexture.active;Texture2D image=null;
            try
            {
                var request=new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest {destination=target};
                // Live Camera disables the tagged management camera. Capture the camera
                // actually rendering the game, including that existing production view.
                var camera=UnityEngine.Camera.main ?? UnityEngine.Camera.allCameras
                    .Single(c=>c.isActiveAndEnabled && c.cameraType==CameraType.Game && c.targetTexture==null);
                Assert.That(camera,Is.Not.Null,"An active runtime camera is required for the capture");
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,request);
                RenderTexture.active=target;image=new Texture2D(1600,1000,TextureFormat.RGB24,false,false);
                image.ReadPixels(new Rect(0,0,1600,1000),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Output,name+".png"),image.EncodeToPNG());
            }
            finally {RenderTexture.active=previous;RenderTexture.ReleaseTemporary(target);if(image!=null)UnityEngine.Object.Destroy(image);}
        }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            RestoreHardwareInput();
            if(Application.isPlaying)yield return new ExitPlayMode();
            Application.runInBackground = SessionState.GetBool(BackgroundKey, false);
            SessionState.EraseBool(BackgroundKey);
        }
    }
}
