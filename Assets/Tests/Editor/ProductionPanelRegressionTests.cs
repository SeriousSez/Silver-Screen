using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using SilverScreen.Domain;
using SilverScreen.Domain.Movie;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Movie;
using SilverScreen.Presentation.UI;

namespace SilverScreen.Tests.EditMode
{
    public sealed class ProductionPanelRegressionTests
    {
        private static T Field<T>(object target, string name) =>
            (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);

        [UnityTest]
        public IEnumerator InactivePanelInitializesOnceAndTracksDisplayedProject()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
            var savedPanel = Object.FindAnyObjectByType<MovieProjectUI>(FindObjectsInactive.Include);
            Assert.That(savedPanel.gameObject.activeSelf, Is.False, "Exercise the saved inactive panel");
            yield return new EnterPlayMode();
            // Create captured locals after the domain reload, like the existing runtime fixtures.
            yield return CheckDisplayedProject();
            yield return new ExitPlayMode();
        }

        private static IEnumerator CheckDisplayedProject()
        {
            yield return null;
            yield return null;

            var panel = Object.FindAnyObjectByType<MovieProjectUI>(FindObjectsInactive.Include);
            var hud = Object.FindAnyObjectByType<StudioHud>();
            var detail = Field<GameObject>(hud, "_detailRoot");
            var service = Object.FindAnyObjectByType<MovieProductionDriver>().Coordinator;
            var created = service.CreateMovie("Panel regression", "drama", 25000, "Lead", new List<string>());
            Assert.That(created.Succeeded, Is.True);
            var movie = created.Project;
            detail.SetActive(true);
            panel.AttachToHost(detail.transform);
            panel.AttachToHost(detail.transform);
            panel.ShowProject(movie);
            yield return null;

            var picker = Object.FindAnyObjectByType<CandidatePickerUI>(FindObjectsInactive.Include);
            var assign = Field<Button>(panel, "_assignDirectorButton");
            assign.onClick.Invoke();
            Assert.That(picker.IsOpen, Is.True, "Assign Director must work on the first opening");
            Assert.That(Field<Transform>(picker, "_candidateListContainer").childCount, Is.EqualTo(1),
                "Repeated attachment must not populate the picker multiple times in one click");
            picker.Close();

            var employees = Object.FindAnyObjectByType<StudioEmployeeManager>().AllEmployees;
            Assert.That(employees, Has.No.Null);
            var director = employees.First(e => e.Role == EmployeeRole.Director);
            var actor = employees.First(e => e.Role == EmployeeRole.Actor);
            movie.AssignDirector(director);
            Assert.That(Field<TextMeshProUGUI>(panel, "_directorNameText"), Is.Not.Null);
            Assert.That(Field<TextMeshProUGUI>(panel, "_directorNameText").text, Does.Contain(director.Name));
            movie.SetProductionControlMode(ProductionControlMode.Manual);
            Assert.That(Field<Button>(panel, "_manualModeButton"), Is.Not.Null);
            Assert.That(Field<Button>(panel, "_manualModeButton").GetComponent<Image>().color,
                Is.EqualTo(new Color(.95f, .70f, .18f, 1f)));
            movie.AssignActorToCastRole(movie.Roles[0].Id, actor);
            Assert.That(Field<Transform>(panel, "_rolesContainer").GetComponentsInChildren<TextMeshProUGUI>()
                .Any(t => t.text.Contains(actor.Name)), Is.True, "Cast changes must refresh without reopening");

            var other = new MovieProject("pinned-other", "Other project", "comedy", "Comedy", 25000, movie.CreatedDate);
            panel.SetViewVisible(false);
            panel.ShowProject(other);
            yield return null;
            assign.onClick.Invoke();
            Assert.That(picker.IsOpen, Is.True);
            Assert.That(Field<MovieProject>(picker, "_targetMovie"), Is.SameAs(other),
                "Director picker must target the displayed project, not the active production");
            picker.Close();
            movie.SetProductionControlMode(ProductionControlMode.Automatic);
            Assert.That(Field<TextMeshProUGUI>(panel, "_titleText").text, Is.EqualTo(other.Title));
            other.AssignDirector(director);
            Assert.That(Field<TextMeshProUGUI>(panel, "_directorNameText").text, Does.Contain(director.Name),
                "Pinned non-active projects must observe their own updates");
            panel.ShowActiveProject();
            Assert.That(panel.DisplayedProject, Is.SameAs(movie));
        }

        [UnityTest]
        public IEnumerator TakeDecisionLayoutSeparatesModesActionsAndFooter()
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Studio.unity");
            yield return new EnterPlayMode();
            yield return null;
            yield return null;
            var panel = Object.FindAnyObjectByType<MovieProjectUI>(FindObjectsInactive.Include);
            var detail = Field<GameObject>(Object.FindAnyObjectByType<StudioHud>(), "_detailRoot");
            detail.SetActive(true);
            var service = Object.FindAnyObjectByType<MovieProductionDriver>().Coordinator;
            var result = service.CreateMovie("Layout regression", "drama", 25000, "Lead", new List<string>());
            Assert.That(result.Succeeded, Is.True);
            panel.ShowProject(result.Project);

            // Isolate layout from work scheduling; the full runtime test exercises real take decisions.
            Field<GameObject>(panel, "_takeDecisionRoot").SetActive(true);
            var keep = Field<Button>(panel, "_keepTakeButton");
            var again = Field<Button>(panel, "_shootAgainButton");
            keep.GetComponentInChildren<TextMeshProUGUI>().text = "KEEP SELECTED TAKE 12";
            again.GetComponentInChildren<TextMeshProUGUI>().text = "SHOOT ANOTHER TAKE";
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(Field<GameObject>(panel, "_activeCardRoot").GetComponent<RectTransform>());
            var controls = new[] {keep.transform, again.transform,
                Field<Button>(panel, "_automaticModeButton").transform,
                Field<Button>(panel, "_manualModeButton").transform,
                Field<TextMeshProUGUI>(panel, "_statusText").transform,
                Field<Image>(panel, "_progressBarFill").transform};
            for (int i = 0; i < controls.Length; i++)
                for (int j = i + 1; j < controls.Length; j++)
                    Assert.That(Bounds(controls[i]).Overlaps(Bounds(controls[j])), Is.False,
                        controls[i].name + " overlaps " + controls[j].name);
            yield return new ExitPlayMode();
        }

        private static Rect Bounds(Transform transform)
        {
            var corners = new Vector3[4];
            ((RectTransform)transform).GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (Application.isPlaying) yield return new ExitPlayMode();
        }
    }
}
