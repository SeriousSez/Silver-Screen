using SilverScreen.Presentation.ReusableAssets;
using UnityEditor;

namespace SilverScreen.Editor.ReusableAssets
{
    [CustomEditor(typeof(DisplayInsertSlot)), CanEditMultipleObjects]
    public sealed class DisplayInsertSlotEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();DrawDefaultInspector();
            if(!EditorGUI.EndChangeCheck())return;
            foreach(var item in targets)
            {
                var slot=(DisplayInsertSlot)item;slot.Refresh();
                PrefabUtility.RecordPrefabInstancePropertyModifications(slot);
            }
        }
    }
}
