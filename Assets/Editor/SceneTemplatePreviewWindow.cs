using System;
using SilverScreen.Domain.Movie;
using SilverScreen.Domain.Performance;
using UnityEditor;
using UnityEngine;

namespace SilverScreen.Editor
{
    /// <summary>Temporary 2D silhouettes. No scene objects, animator, camera or production state are changed.</summary>
    public sealed class SceneTemplatePreviewWindow : EditorWindow
    {
        private const double BeatPreviewSeconds = 1.4;
        private static readonly string[] PlaceholderNames = { "Placeholder A", "Placeholder B" };
        private SceneTemplate _template;
        private SceneSequenceCursor _cursor;
        private SceneDuration _duration = SceneDuration.Medium;
        private int _aggressor;
        private int _defender = 1;
        private bool _playing;
        private double _beatStarted;
        private string _error;
        private Vector2 _scroll;

        [MenuItem("SilverScreen/Scene Templates/Heated Argument Preview")]
        public static void Open() => GetWindow<SceneTemplatePreviewWindow>("Scene Template Preview");

        private void OnEnable()
        {
            minSize = new Vector2(640, 580);
            _template = HeatedArgumentTemplate.Create();
            ResetSequence();
            EditorApplication.update += Tick;
        }

        private void OnDisable()
        {
            EditorApplication.update -= Tick;
            _playing = false;
        }

        private void ResetSequence()
        {
            _playing = false;
            _error = null;
            try
            {
                _cursor = new SceneSequenceCursor(new ScenePerformance(_template, _duration, new[]
                {
                    new PerformerBinding(HeatedArgumentTemplate.Aggressor, PlaceholderNames[_aggressor]),
                    new PerformerBinding(HeatedArgumentTemplate.Defender, PlaceholderNames[_defender])
                }));
            }
            catch (ArgumentException exception)
            {
                _cursor = null;
                _error = exception.Message;
            }
            _beatStarted = EditorApplication.timeSinceStartup;
            Repaint();
        }

        private void Tick()
        {
            if (!_playing || _cursor == null) return;
            double now = EditorApplication.timeSinceStartup;
            if (now - _beatStarted >= BeatPreviewSeconds)
            {
                _cursor.Advance();
                _beatStarted = now;
                if (_cursor.IsComplete) _playing = false;
            }
            Repaint();
        }

        private void OnGUI()
        {
            if (_template == null) return;
            EditorGUILayout.LabelField(_template.Name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(_template.Id + "  |  " + string.Join(", ", _template.Tags));
            EditorGUILayout.HelpBox("Semantic preview with temporary silhouettes. Each beat gets the same preview time; longer variants add authored beats. No footage or production progress is recorded.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            _duration = (SceneDuration)EditorGUILayout.EnumPopup("Duration", _duration);
            _aggressor = EditorGUILayout.Popup("Aggressor", _aggressor, PlaceholderNames);
            _defender = EditorGUILayout.Popup("Defender", _defender, PlaceholderNames);
            if (EditorGUI.EndChangeCheck()) ResetSequence();
            if (_error != null) EditorGUILayout.HelpBox(_error, MessageType.Warning);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_cursor == null))
                {
                    if (GUILayout.Button(_playing ? "Pause" : "Play"))
                    {
                        if (_cursor.IsComplete) ResetSequence();
                        _playing = !_playing;
                        _beatStarted = EditorApplication.timeSinceStartup;
                    }
                    if (GUILayout.Button("Next beat"))
                    {
                        _playing = false;
                        _cursor.Advance();
                        _beatStarted = EditorApplication.timeSinceStartup;
                    }
                }
                if (GUILayout.Button("Reset")) ResetSequence();
            }

            Rect stage = GUILayoutUtility.GetRect(600, 205, GUILayout.ExpandWidth(true));
            DrawStage(stage);
            if (_cursor == null) return;
            var current = _cursor.Current;
            EditorGUILayout.LabelField(current == null ? "Sequence complete" :
                $"Beat {_cursor.Index + 1}/{_cursor.Performance.Beats.Count}: {current.Definition.Id}", EditorStyles.boldLabel);
            if (current != null)
            {
                EditorGUILayout.LabelField(current.PerformerId + " — " + current.Definition.Action);
                EditorGUILayout.LabelField(current.Definition.Direction, EditorStyles.wordWrappedLabel);
                var camera = current.Definition.Camera;
                EditorGUILayout.LabelField("Camera suggestion: " + (camera == null ? "None" :
                    camera.Framing + (camera.SubjectRoleId == null ? "" : " / " + camera.SubjectRoleId)));
                EditorGUILayout.LabelField("Anchor: " + (current.Definition.AnchorId ?? "None"));
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _cursor.Performance.Beats.Count; i++)
            {
                var beat = _cursor.Performance.Beats[i];
                EditorGUILayout.LabelField($"{(i == _cursor.Index ? ">" : " ")} {i + 1}. {beat.Definition.Id} / {beat.PerformerId}");
            }
            foreach (var requirement in _template.Requirements)
                EditorGUILayout.LabelField($"{(requirement.Required ? "Required" : "Optional")} {requirement.Kind}: {requirement.SemanticId}");
            EditorGUILayout.EndScrollView();
        }

        private void DrawStage(Rect area)
        {
            EditorGUI.DrawRect(area, new Color(0.09f, 0.11f, 0.15f));
            EditorGUI.DrawRect(new Rect(area.x + 10, area.yMax - 35, area.width - 20, 2), Color.gray);
            if (_cursor == null) return;
            float progress = _playing ? Mathf.Clamp01((float)((EditorApplication.timeSinceStartup - _beatStarted) / BeatPreviewSeconds)) : 1f;
            DrawPerformer(area, HeatedArgumentTemplate.Aggressor, PlaceholderNames[_aggressor], 0.22f, progress, new Color(0.95f, 0.58f, 0.28f));
            DrawPerformer(area, HeatedArgumentTemplate.Defender, PlaceholderNames[_defender], 0.7f, progress, new Color(0.28f, 0.68f, 0.92f));
        }

        private void DrawPerformer(Rect area, string role, string name, float initialX, float progress, Color color)
        {
            float x = initialX;
            for (int i = 0; i < _cursor.Performance.Beats.Count && i <= _cursor.Index; i++)
            {
                var beat = _cursor.Performance.Beats[i].Definition;
                if (beat.RoleId != role) continue;
                float target = beat.Action == PerformanceAction.Approach ? 0.43f
                    : beat.Action == PerformanceAction.Exit ? 0.06f : x;
                x = Mathf.Lerp(x, target, i < _cursor.Index ? 1f : progress);
            }
            Vector2 center = new Vector2(area.x + x * area.width, area.y + 90);
            Handles.BeginGUI();
            Handles.color = color;
            Handles.DrawSolidDisc(center + Vector2.up * 29, Vector3.forward, 15);
            Handles.DrawSolidDisc(center + Vector2.down * 23, Vector3.forward, 18);
            Handles.EndGUI();
            EditorGUI.DrawRect(new Rect(center.x - 18, center.y - 8, 36, 45), color);
            float labelX = Mathf.Clamp(center.x - 65, area.x + 5, area.xMax - 145);
            GUI.Label(new Rect(labelX, area.yMax - 30, 140, 22), name + " / " + role, EditorStyles.whiteMiniLabel);
            var current = _cursor.Current;
            if (current != null && current.Definition.RoleId == role)
            {
                EditorGUI.DrawRect(new Rect(center.x - 52, area.y + 8, 104, 24), color * 0.7f);
                GUI.Label(new Rect(center.x - 46, area.y + 10, 100, 22), current.Definition.Action.ToString(), EditorStyles.whiteLabel);
            }
        }
    }
}
