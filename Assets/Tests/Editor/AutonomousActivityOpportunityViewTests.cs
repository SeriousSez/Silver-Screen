using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using SilverScreen.Domain;
using SilverScreen.Domain.Interaction;
using SilverScreen.Domain.Recruitment;
using SilverScreen.Domain.Time;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.SimulationTime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace SilverScreen.Tests.EditMode
{
    public sealed class AutonomousActivityOpportunityViewTests
    {
        private const string SceneKey = "SilverScreen.AutonomyOpportunity.PreviousScene";

        [UnityTearDown]
        public IEnumerator RestoreScene()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
            string path = SessionState.GetString(SceneKey, "");
            SessionState.EraseString(SceneKey);
            if (!string.IsNullOrEmpty(path)) EditorSceneManager.OpenScene(path);
        }

        [Test]
        public void AuthoredStudioSeatsRegisterWithStableIdsAndCapacityOne()
        {
            var prefab = Resources.Load<GameObject>("StudioServices_Live");
            Assert.That(prefab, Is.Not.Null);
            var instance = Object.Instantiate(prefab);
            var driverObject = new GameObject("Opportunity registration test driver");
            var driver = driverObject.AddComponent<SimulationTimeDriver>();
            driver.enabled = false;
            try
            {
                var views = instance.GetComponentsInChildren<AutonomousActivityOpportunityView>(true);
                Assert.That(views, Has.Length.EqualTo(2));
                CollectionAssert.AreEquivalent(new[]
                {
                    "studio-services:rest:chair-01",
                    "studio-services:rest:chair-02"
                }, new[] { views[0].OpportunityId, views[1].OpportunityId });

                foreach (var view in views)
                {
                    Assert.That(view.RegisterWith(driver), Is.True);
                    var opportunity = driver.AutonomyOpportunities.Find(view.OpportunityId);
                    Assert.That(opportunity, Is.Not.Null);
                    Assert.That(opportunity.Activity, Is.EqualTo(PersonAutonomousActivity.Rest));
                    Assert.That(opportunity.Capacity, Is.EqualTo(1));
                    Assert.That(opportunity.Position.X, Is.EqualTo(view.transform.position.x).Within(.001f));
                    Assert.That(opportunity.Position.Z, Is.EqualTo(view.transform.position.z).Within(.001f));

                    string stableId = view.OpportunityId;
                    Assert.That(view.SetAvailable(false), Is.True);
                    Assert.That(driver.AutonomyOpportunities.Find(stableId).IsAvailable, Is.False);
                    Assert.That(view.SetAvailable(true), Is.True);
                    Assert.That(view.OpportunityId, Is.EqualTo(stableId));
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(driverObject);
            }
        }

        [Test]
        public void UnavailableAuthoredOpportunityReleasesItsReservation()
        {
            var prefab = Resources.Load<GameObject>("StudioServices_Live");
            var instance = Object.Instantiate(prefab);
            var driverObject = new GameObject("Opportunity interruption test driver");
            var driver = driverObject.AddComponent<SimulationTimeDriver>();
            driver.enabled = false;
            try
            {
                var view = StudioServicesChairOne(instance);
                Assert.That(view.RegisterWith(driver), Is.True);
                var employee = CreateLowEnergyEmployee("disabled-opportunity-person");

                driver.Autonomy.Register(employee);

                Assert.That(driver.AutonomyOpportunities.Find(view.OpportunityId).ReservationCount, Is.EqualTo(1));
                Assert.That(view.SetAvailable(false), Is.True);
                Assert.That(driver.AutonomyOpportunities.Find(view.OpportunityId).IsAvailable, Is.False);
                Assert.That(employee.CurrentState, Is.EqualTo(EmployeeState.Idle));
                Assert.That(employee.CurrentIntent.Purpose, Is.EqualTo(EmployeeIntentPurpose.None));
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(driverObject);
            }
        }

        [Test]
        public void ActivityCardShowsAutonomyIntentOpportunityAndBroadActivity()
        {
            var employee = CreateLowEnergyEmployee("activity-card-person");
            employee.SetState(EmployeeState.Walking);
            employee.SetIntent(new EmployeeIntent(EmployeeIntentPurpose.AutonomousActivity,
                "Rest: Studio Services Chair 1", "studio-services:rest:chair-01"));
            var cards = new List<PersonInformation>();
            new ExistingPersonInformationProvider().Collect(new PersonInformationContext
            {
                Person = employee.Person,
                Employee = employee,
                Date = new SimulationDateTime(1930, 1, 1, 8, 0)
            }, cards);

            var activity = cards.Single(card => card.Category == "Activity");
            StringAssert.Contains("Opportunity ID: studio-services:rest:chair-01", activity.Expanded);
            StringAssert.Contains("Broad activity: Idle", activity.Expanded);
            Assert.That(cards.Any(card => card.Category == "Work"), Is.False);
        }

        [UnityTest]
        public IEnumerator PersonMustArriveBeforeRestBeginsAndCompletionReleasesTheSeat()
        {
            Assert.That(SceneManager.GetActiveScene().isDirty, Is.False,
                "Save the scene before running this isolated runtime proof.");
            SessionState.SetString(SceneKey, SceneManager.GetActiveScene().path);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            yield return new EnterPlayMode();

            var objects = new List<GameObject>();
            GameObject Keep(GameObject gameObject) { objects.Add(gameObject); return gameObject; }
            var center = new Vector3(1000, 0, 1000);
            var source = new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                size = new Vector3(40, .2f, 40),
                transform = Matrix4x4.TRS(center - Vector3.up * .1f, Quaternion.identity, Vector3.one),
                area = 0
            };
            var data = NavMeshBuilder.BuildNavMeshData(NavMesh.GetSettingsByIndex(0),
                new List<NavMeshBuildSource> { source }, new Bounds(center, new Vector3(42, 4, 42)),
                Vector3.zero, Quaternion.identity);
            var navigation = NavMesh.AddNavMeshData(data);
            var driverObject = Keep(new GameObject("SIM4.1 proof simulation driver"));
            var driver = driverObject.AddComponent<SimulationTimeDriver>();
            var prefab = Resources.Load<GameObject>("StudioServices_Live");
            var facility = Keep(Object.Instantiate(prefab, center, Quaternion.identity));
            var view = StudioServicesChairOne(facility);
            var opportunityId = view.OpportunityId;
            driver.enabled = false;

            var person = CreateLowEnergyEmployee("physical-rest-person");
            var employeeObject = Keep(new GameObject("Physical rest employee"));
            employeeObject.transform.position = center + Vector3.right * 7 + Vector3.up;
            var navAgent = employeeObject.AddComponent<NavMeshAgent>();
            navAgent.baseOffset = 1;
            navAgent.speed = 3.5f;
            var employeeAgent = employeeObject.AddComponent<EmployeeAgent>();
            employeeAgent.BindDomain(person);
            employeeAgent.BindAutonomy(driver.Autonomy);
            employeeAgent.BindTimeService(driver.TimeService);
            yield return null;
            Assert.That(navAgent.isOnNavMesh, Is.True, "The employee must be attached to the built NavMesh.");
            driver.Wellbeing.Register(person.Person);
            driver.Autonomy.Register(person);

            try
            {
                Assert.That(driver.AutonomyOpportunities.Find(opportunityId), Is.Not.Null);
                Assert.That(driver.AutonomyOpportunities.Find(opportunityId).ReservationCount, Is.EqualTo(1));
                Assert.That(person.CurrentState, Is.EqualTo(EmployeeState.Walking));
                Assert.That(person.Person.Wellbeing.Activity, Is.EqualTo(PersonWellbeingActivity.Idle));

                int energyBeforeArrival = person.Person.Wellbeing.Energy;
                driver.Clock.AdvanceTo(driver.Clock.Now + SimulationDuration.FromMinutes(60));
                Assert.That(person.Person.Wellbeing.Energy, Is.EqualTo(energyBeforeArrival),
                    "An en-route employee must not receive Rest wellbeing progression.");

                float arrivalTimeout = Time.realtimeSinceStartup + 10f;
                while (person.CurrentState != EmployeeState.Resting &&
                    Time.realtimeSinceStartup < arrivalTimeout)
                    yield return new WaitForSecondsRealtime(.05f);

                Assert.That(person.CurrentState, Is.EqualTo(EmployeeState.Resting),
                    "NavMesh status=" + navAgent.pathStatus + ", pending=" + navAgent.pathPending +
                    ", hasPath=" + navAgent.hasPath + ", stopped=" + navAgent.isStopped +
                    ", speed=" + navAgent.speed + ", velocity=" + navAgent.velocity +
                    ", remaining=" + navAgent.remainingDistance + ", timeScale=" + Time.timeScale +
                    ", paused=" + driver.Clock.IsPaused + ", position=" + employeeObject.transform.position +
                    ", destination=" + navAgent.destination);
                Assert.That(person.Person.Wellbeing.Activity, Is.EqualTo(PersonWellbeingActivity.Resting));
                var destination = new Vector3(
                    driver.AutonomyOpportunities.Find(opportunityId).Position.X,
                    driver.AutonomyOpportunities.Find(opportunityId).Position.Y,
                    driver.AutonomyOpportunities.Find(opportunityId).Position.Z);
                var feet = employeeObject.transform.position - Vector3.up * navAgent.baseOffset;
                Assert.That(Vector3.Distance(feet, destination), Is.LessThanOrEqualTo(navAgent.stoppingDistance + .25f));

                driver.Clock.AdvanceTo(driver.Clock.Now + SimulationDuration.FromMinutes(60));
                Assert.That(person.Person.Wellbeing.Energy, Is.GreaterThan(energyBeforeArrival));
                Assert.That(person.CurrentState, Is.EqualTo(EmployeeState.Idle));
                Assert.That(driver.AutonomyOpportunities.Find(opportunityId).ReservationCount, Is.Zero);

                driver.Clock.AdvanceTo(driver.Clock.Now + SimulationDuration.FromMinutes(20));
                Assert.That(driver.AutonomyOpportunities.Find(opportunityId).ReservationCount, Is.EqualTo(1));
                view.gameObject.SetActive(false);
                Assert.That(driver.AutonomyOpportunities.Find(opportunityId), Is.Null);
                Assert.That(driver.AutonomyOpportunities.Find("studio-services:rest:chair-02"), Is.Not.Null);
                Assert.That(person.CurrentState, Is.EqualTo(EmployeeState.Idle));
                Assert.That(person.CurrentIntent.Purpose, Is.EqualTo(EmployeeIntentPurpose.None));
            }
            finally
            {
                foreach (var gameObject in objects) Object.DestroyImmediate(gameObject);
                navigation.Remove();
                Object.DestroyImmediate(data);
            }
        }

        private static Employee CreateLowEnergyEmployee(string id)
        {
            var person = new PersonProfile(id, id, new SimulationDateTime(1900, 1, 1, 0, 0),
                ProfessionalRole.Actor, new TalentProfile(50, 50));
            var wellbeing = person.CaptureWellbeing();
            wellbeing.Energy = 0;
            person.RestoreWellbeing(wellbeing);
            return new Employee(person, EmployeeRole.Actor, 1000);
        }

        private static AutonomousActivityOpportunityView StudioServicesChairOne(GameObject facility)
        {
            foreach (var view in facility.GetComponentsInChildren<AutonomousActivityOpportunityView>(true))
                if (view.OpportunityId == "studio-services:rest:chair-01") return view;
            return null;
        }
    }
}
