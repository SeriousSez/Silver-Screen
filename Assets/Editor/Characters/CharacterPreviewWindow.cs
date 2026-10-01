using System;
using System.IO;
using SilverScreen.Domain.Characters;
using SilverScreen.Presentation.Characters;
using UnityEditor;
using UnityEngine;

namespace SilverScreen.Editor.Characters
{
    public sealed class CharacterPreviewWindow : EditorWindow
    {
        private int _candidate;
        private CharacterPose _pose;
        private float _seconds, _blink, _jaw, _smile, _frown;
        private Vector2 _gaze;
        private bool _play, _hair = true;
        private double _lastTime;
        private CharacterAppearance _profile;
        [MenuItem("SilverScreen/Characters/Gate 1/Character Preview and Validation")]
        public static void ShowWindow() => GetWindow<CharacterPreviewWindow>("Adult Gate 1");
        private void OnEnable() { EditorApplication.update += Tick; _lastTime = EditorApplication.timeSinceStartup; }
        private void OnDisable() { EditorApplication.update -= Tick; }
        private void Tick()
        {
            double now = EditorApplication.timeSinceStartup;
            if (_play) { _seconds += (float)(now-_lastTime); Sample(); Repaint(); SceneView.RepaintAll(); }
            _lastTime = now;
        }
        private void Sample()
        {
            if (CharacterGateOne.Candidates.Count != 3) return;
            var view = CharacterGateOne.Candidates[_candidate];
            CharacterMotion.Sample(view,_pose,_seconds);view.SetExpression(_blink,_jaw,_smile,_frown,_gaze);view.SetHairVisible(_hair);
        }
        private void OnGUI()
        {
            EditorGUILayout.LabelField("SilverScreen adult-v1 — visual approval required",EditorStyles.boldLabel);
            if (GUILayout.Button("Open isolated Studio review")) { CharacterGateOne.Open(); _profile=null; }
            if (CharacterGateOne.Candidates.Count != 3) { EditorGUILayout.HelpBox("Open the isolated review to inspect the three adults. Studio remains unchanged.",MessageType.Info);return; }
            EditorGUI.BeginChangeCheck();
            int next=GUILayout.Toolbar(_candidate,new[]{"A · Adrian","B · Beatrice","C · Calvin"});
            if(next!=_candidate){_candidate=next;_profile=null;}
            _pose=(CharacterPose)EditorGUILayout.EnumPopup("Motion / stress pose",_pose);
            _play=EditorGUILayout.Toggle("Play motion study",_play);
            _seconds=EditorGUILayout.Slider("Seconds",_seconds%4,0,4);
            _hair=EditorGUILayout.Toggle("Hair visible",_hair);
            _blink=EditorGUILayout.Slider("Blink",_blink,0,1);_jaw=EditorGUILayout.Slider("Jaw",_jaw,0,1);
            _smile=EditorGUILayout.Slider("Smile",_smile,0,1);_frown=EditorGUILayout.Slider("Frown",_frown,0,1);
            _gaze=EditorGUILayout.Vector2Field("Gaze (-1 to 1)",_gaze);
            if(EditorGUI.EndChangeCheck()) Sample();
            var view=CharacterGateOne.Candidates[_candidate];
            if(_profile==null)_profile=view.Appearance;
            EditorGUILayout.Space();EditorGUILayout.LabelField("Persistent identity",EditorStyles.boldLabel);
            _profile.heightMetres=EditorGUILayout.FloatField("Height metres",_profile.heightMetres);
            _profile.jawWidth=EditorGUILayout.Slider("Jaw width",_profile.jawWidth,-1,1);
            _profile.noseWidth=EditorGUILayout.Slider("Nose width",_profile.noseWidth,-1,1);
            _profile.noseBridge=EditorGUILayout.Slider("Nose bridge",_profile.noseBridge,-1,1);
            _profile.mouthWidth=EditorGUILayout.Slider("Mouth width",_profile.mouthWidth,-1,1);
            _profile.cheekWidth=EditorGUILayout.Slider("Cheek width",_profile.cheekWidth,-1,1);
            _profile.chinHeight=EditorGUILayout.Slider("Chin height",_profile.chinHeight,-1,1);
            if(GUILayout.Button("Rebuild selected appearance")) { CharacterGateOne.Rebuild(_candidate,_profile);Sample(); }
            if(GUILayout.Button("Save appearance JSON"))
            { string path=EditorUtility.SaveFilePanel("Save stable appearance",CharacterGateOne.ReviewRoot+"/Profiles",_profile.appearanceId,"json");if(path.Length>0){_profile.Validate();File.WriteAllText(path,JsonUtility.ToJson(_profile,true));} }
            if(GUILayout.Button("Load appearance JSON"))
            { string path=EditorUtility.OpenFilePanel("Load appearance",CharacterGateOne.ReviewRoot+"/Profiles","json");if(path.Length>0){_profile=JsonUtility.FromJson<CharacterAppearance>(File.ReadAllText(path));_profile.Validate();CharacterGateOne.Rebuild(_candidate,_profile);Sample();} }
            if(GUILayout.Button("Select character in Scene view")){Selection.activeGameObject=view.gameObject;SceneView.lastActiveSceneView?.Frame(new Bounds(view.Feet+Vector3.up,Vector3.one*2.2f),false);}
            if(GUILayout.Button("Capture identity review"))CharacterGateOne.CaptureIdentity();
        }
    }
}
