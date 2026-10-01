using System.Linq;
using SilverScreen.Domain.Buildings;
using SilverScreen.Presentation.Buildings;
using UnityEditor;
using UnityEngine;

namespace SilverScreen.Editor
{
    public sealed class ConstructionDressingWindow : EditorWindow
    {
        private ConstructionSiteView _site;
        private ConstructionPhase _phase;
        private Vector2 _scroll;
        [MenuItem("SilverScreen/Construction Dressing")]
        public static void Open()=>GetWindow<ConstructionDressingWindow>("Construction Dressing");
        private void OnInspectorUpdate()=>Repaint();
        private void OnGUI()
        {
            EditorGUILayout.HelpBox("Play a New Studio and place a site. These controls change presentation only: work, collision, capabilities and worker activity still follow the real construction state. Pause the strategic clock to inspect at leisure.",MessageType.Info);
            if(!EditorApplication.isPlaying)return;
            foreach(var site in Object.FindObjectsByType<ConstructionSiteView>())
                if(GUILayout.Button(site.Building.Definition.DisplayName+" / "+site.Building.Id.Substring(0,6)))
                {
                    if(_site!=null)_site.PreviewPhase(null);
                    _site=site;_phase=site.Building.Phase;
                }
            if(_site==null)return;
            EditorGUILayout.LabelField("Authoritative",_site.Building.State+" / "+_site.Building.Phase);
            EditorGUILayout.LabelField("Presented",_site.PresentedPhase+(_site.IsPreviewing?" (debug override)":""));
            using(new EditorGUI.DisabledScope(_site.Building.IsOperational))
            {
                _phase=(ConstructionPhase)EditorGUILayout.EnumPopup("Inspect phase",_phase);
                if(GUILayout.Button("Preview selected phase"))_site.PreviewPhase(_phase);
                if(GUILayout.Button("Follow real construction"))_site.PreviewPhase(null);
            }
            if(GUILayout.Button("Select / frame site")){Selection.activeGameObject=_site.gameObject;SceneView.lastActiveSceneView?.FrameSelected();}
            EditorGUILayout.LabelField("Modules",(_site.Plan?.Modules.Count??0).ToString());
            EditorGUILayout.LabelField("Visible final-model renderers",_site.VisibleBuildingRenderers.ToString());
            _scroll=EditorGUILayout.BeginScrollView(_scroll);
            if(_site.Plan!=null)foreach(var group in _site.Plan.Modules.GroupBy(m=>m.Type))EditorGUILayout.LabelField(group.Key.ToString(),group.Count().ToString());
            EditorGUILayout.LabelField("Real worker activity points");
            foreach(var point in _site.ActivityPoints)EditorGUILayout.LabelField(point.Kind+" / "+point.Slot,point.LocalPosition.ToString());
            EditorGUILayout.EndScrollView();
        }
        private void OnDisable(){if(_site!=null)_site.PreviewPhase(null);}
    }
}
