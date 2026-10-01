using SilverScreen.Domain.Tutorial;
using SilverScreen.Presentation.Bootstrap;
using SilverScreen.Presentation.Tutorial;
using SilverScreen.Presentation.SimulationTime;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEngine;

namespace SilverScreen.Editor
{
    public sealed class StudioGuidanceWindow : EditorWindow
    {
        private string _proof;
        [MenuItem("SilverScreen/Tutorial and Studio PA")]
        public static void Open() => GetWindow<StudioGuidanceWindow>("Studio Guidance");
        private void OnInspectorUpdate() => Repaint();
        private void OnGUI()
        {
            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Choose before entering Play. New Studio begins with zero employees and a service facility. Hire a builder and use Build Mode to place construction sites. These settings persist if you save the scene. Leave New Studio off to retain the production sandbox.", MessageType.Info);
                var bootstrap = Object.FindAnyObjectByType<StudioBootstrap>();
                if (bootstrap == null) { EditorGUILayout.LabelField("Open a scene with StudioBootstrap."); return; }
                var serialized = new SerializedObject(bootstrap);
                serialized.Update();
                EditorGUILayout.PropertyField(serialized.FindProperty("_startNewStudio"), new GUIContent("New Studio"));
                EditorGUILayout.PropertyField(serialized.FindProperty("_newStudioOptions").FindPropertyRelative("TutorialEnabled"), new GUIContent("Tutorial ON"));
                serialized.ApplyModifiedProperties();
                return;
            }
            var driver = Object.FindAnyObjectByType<StudioGuidanceDriver>();
            if (driver?.Tutorial == null) { EditorGUILayout.LabelField("Waiting for StudioBootstrap."); return; }
            var tutorial = driver.Tutorial;
            EditorGUILayout.LabelField("Tutorial", tutorial.Status.ToString());
            EditorGUILayout.LabelField("Step", tutorial.CurrentStep?.Id ?? "None");
            if (tutorial.IsActive)
            {
                EditorGUILayout.HelpBox(tutorial.CurrentStep.Message.Body, MessageType.Info);
                EditorGUILayout.LabelField("Voice", tutorial.CurrentStep.Message.VoiceCueId ?? "None");
                EditorGUILayout.LabelField("Focus target", tutorial.CurrentStep.Message.FocusTargetId ?? "None");
                if (GUILayout.Button(tutorial.OpenMessage != null ? "Continue guide" : "Reopen guide"))
                { if (tutorial.OpenMessage != null) tutorial.Continue(); else tutorial.ReopenMessage(); }
                if (GUILayout.Button("Skip tutorial (preserve studio)")) tutorial.Skip();
            }
            var build = Object.FindAnyObjectByType<StudioBuildMode>();
            if (build != null && GUILayout.Button("Open Build Mode / Workforce")) build.Open();
            EditorGUILayout.HelpBox("Use Staff to hire, Screenplays to commission and greenlight, and Productions to cast, assign a director, film, keep takes and release. Continue closes a message; it never fakes an objective.", MessageType.None);
            var time = Object.FindAnyObjectByType<SimulationTimeDriver>();
            EditorGUILayout.LabelField("Strategic pause", time.Clock.IsPaused.ToString());
            EditorGUILayout.LabelField("Pending PA", driver.Announcements.PendingCount.ToString());
            if (GUILayout.Button("PA proof: actor needed twice + urgent director"))
            {
                var queue = driver.Announcements;
                queue.Resolve(AnnouncementType.ProductionNeedsActor, "developer-proof");
                queue.Resolve(AnnouncementType.ProductionNeedsDirector, "developer-proof");
                bool pausedBefore = time.Clock.IsPaused;
                var actor = new AnnouncementRequest(AnnouncementType.ProductionNeedsActor, AnnouncementPriority.Normal, "developer-proof", time.Clock.Now.Seconds);
                bool first = queue.Request(actor), duplicate = queue.Request(actor);
                bool urgent = queue.Request(new AnnouncementRequest(AnnouncementType.ProductionNeedsDirector, AnnouncementPriority.Urgent, "developer-proof", time.Clock.Now.Seconds));
                _proof = $"Actor accepted: {first}; immediate duplicate: {duplicate}; urgent director: {urgent}; pause unchanged: {pausedBefore == time.Clock.IsPaused}. Game view shows PA text in priority order.";
            }
            if (_proof != null) EditorGUILayout.HelpBox(_proof, MessageType.Info);
        }
    }
}
