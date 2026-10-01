using System;
using System.Linq;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Performance;
using SilverScreen.Domain.Time;
using SilverScreen.Domain.Writing;
using UnityEditor;
using UnityEngine;

namespace SilverScreen.Editor
{
    /// <summary>Disposable in-memory developer proof. No assets, scene objects, audio or footage are created.</summary>
    public sealed class MovieMakerAuthoringWindow : EditorWindow
    {
        private ScreenplayProject _screenplay;
        private MovieProject _adapted;
        private SceneSequenceCursor _cursor;
        private string _message;
        private int _soundIndex;
        private Vector2 _scroll;
        private string _actorA = "placeholder-performer-a", _actorB = "placeholder-performer-b", _director = "placeholder-director";
        public ScreenplayProject Screenplay => _screenplay;
        [MenuItem("SilverScreen/Movie Maker/Authoring Foundation Proof")]
        public static void Open() => GetWindow<MovieMakerAuthoringWindow>("Movie Maker Proof");
        private void OnEnable() { if (_screenplay == null) ResetExample(); }
        public void ResetExample()
        {
            _screenplay = MovieMakerAuthoringExample.Create(); _adapted = null; _cursor = null; _message = null; _soundIndex = 0;
            _actorA = "placeholder-performer-a"; _actorB = "placeholder-performer-b"; _director = "placeholder-director";
        }
        private void Apply(Action action)
        { try { action(); _message = "Updated creative intent."; _cursor = null; } catch (ArgumentException ex) { _message = ex.Message; } catch (InvalidOperationException ex) { _message = ex.Message; } }
        private void OnGUI()
        {
            if (_screenplay == null) ResetExample();
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.HelpBox("Developer authoring proof: in memory only; reset on domain reload. No filming, takes, budget, audio playback or final actor rendering.", MessageType.Info);
            if (GUILayout.Button("Reset example")) ResetExample();
            string title = EditorGUILayout.DelayedTextField("Movie title", _screenplay.Title);
            if (title != _screenplay.Title) Apply(() => _screenplay.Authoring.Rename(title));
            var scene = _screenplay.Scenes[0]; var plan = scene.Authoring;
            EditorGUILayout.LabelField(scene.Title + " / " + scene.Id, EditorStyles.boldLabel);
            var duration = (SceneDuration)EditorGUILayout.EnumPopup("Duration", plan.Duration);
            if (duration != plan.Duration) Apply(() => _screenplay.Authoring.UpdateScene(scene.Id, scene.Title, plan.WithDuration(duration)));
            foreach (var binding in plan.Bindings)
                EditorGUILayout.LabelField(binding.RoleId, _screenplay.Characters.Single(character => character.Id == binding.CharacterId).Name);
            _actorA = EditorGUILayout.TextField("Vincent's actor ID", _actorA);
            _actorB = EditorGUILayout.TextField("Evelyn's actor ID", _actorB);
            if (GUILayout.Button("Apply / clear visualization actors")) Apply(() => {
                if (string.IsNullOrWhiteSpace(_actorA)) _screenplay.Authoring.ClearActor("hale"); else _screenplay.Authoring.AssignActor("hale", _actorA);
                if (string.IsNullOrWhiteSpace(_actorB)) _screenplay.Authoring.ClearActor("hart"); else _screenplay.Authoring.AssignActor("hart", _actorB);
            });
            _director = EditorGUILayout.TextField("Director ID", _director);
            if (GUILayout.Button("Apply / clear director intent")) Apply(() => {
                if (string.IsNullOrWhiteSpace(_director)) _screenplay.Authoring.ClearDirector(); else _screenplay.Authoring.AssignDirector(_director);
            });
            EditorGUILayout.LabelField("Set intent", plan.Set.SemanticId + " / " + plan.Set.Selection.Kind);
            foreach (var line in plan.Dialogue)
            {
                string text = EditorGUILayout.DelayedTextField(line.CharacterId + " / " + line.Beat.Key, line.Text);
                if (text != line.Text) Apply(() => _screenplay.Authoring.UpdateScene(scene.Id, scene.Title,
                    plan.WithDialogue(plan.Dialogue.Select(item => item.Id == line.Id
                        ? new AuthoredDialogue(item.Id, item.Beat, item.CharacterId, text, item.Emotion, item.DeliveryDirection) : item))));
            }
            var sounds = SceneSoundPlanner.Resolve(plan);
            _soundIndex = Math.Clamp(_soundIndex, 0, sounds.Count - 1);
            _soundIndex = EditorGUILayout.Popup("Sound event", _soundIndex, sounds.Select(sound => sound.SemanticType + " / " + sound.Beat.Key).ToArray());
            var selected = sounds[_soundIndex]; var settings = selected.Settings;
            EditorGUILayout.LabelField("Semantic type", selected.SemanticType);
            EditorGUI.BeginChangeCheck();
            bool enabled = EditorGUILayout.Toggle("Enabled", settings.Enabled);
            float volume = EditorGUILayout.Slider("Volume", (float)settings.Volume, 0, 1);
            double offset = EditorGUILayout.DoubleField("Timing offset (seconds)", settings.TimingOffsetSeconds);
            double fadeIn = EditorGUILayout.DoubleField("Fade in (seconds)", settings.FadeInSeconds);
            double fadeOut = EditorGUILayout.DoubleField("Fade out (seconds)", settings.FadeOutSeconds);
            string asset = EditorGUILayout.DelayedTextField("Optional sound content ID", settings.AssetReferenceId ?? "");
            if (EditorGUI.EndChangeCheck()) Apply(() => _screenplay.Authoring.UpdateScene(scene.Id, scene.Title,
                plan.WithSound(selected.WithSettings(new SoundSettings(enabled, string.IsNullOrWhiteSpace(asset) ? null : asset, volume, offset, fadeIn, fadeOut, settings.SpatialIntent)))));
            foreach (var group in plan.BackgroundGroups)
                EditorGUILayout.LabelField("Background", group.SemanticId + " / " + group.MinimumCount + "–" + group.MaximumCount + " / " + group.BehaviorIntent);
            if (GUILayout.Button("Resolve preview (creative intent only)")) Apply(() => { });
            if (_cursor == null) _cursor = new SceneSequenceCursor(_screenplay.Authoring.Preview(scene.Id).Performance);
            EditorGUILayout.LabelField("Preview", _cursor.Current?.Definition.Id ?? "Sequence complete");
            if (GUILayout.Button("Next preview beat")) _cursor.Advance();
            foreach (var beat in _screenplay.Authoring.Preview(scene.Id).Performance.Beats)
                EditorGUILayout.LabelField(beat.Definition.Id, beat.PerformerId + " / " + beat.Definition.Action);
            if (GUILayout.Button("Submit and adapt isolated production copy")) Apply(() => {
                _screenplay.Authoring.Submit();
                var result = new ScreenplayProductionAdapter().Adapt(_screenplay, new SimulationDateTime(1930, 1, 1, 8, 0), "Drama");
                if (!result.Succeeded) throw new InvalidOperationException(result.Message);
                _adapted = result.Movie;
            });
            if (_adapted != null) EditorGUILayout.LabelField("Copied production", _adapted.CurrentState + " / " + _adapted.ProductionProgress.ToString("P0") + " / takes " + _adapted.Scenes.Sum(item => item.Takes.Count));
            if (_message != null) EditorGUILayout.HelpBox(_message, MessageType.Info);
            EditorGUILayout.EndScrollView();
        }
    }
}
