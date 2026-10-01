if(UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)throw new System.InvalidOperationException("Edit Mode required");
const string root="Assets/SilverScreen/Environment/StageSchoolB";
if(!UnityEditor.AssetDatabase.IsValidFolder(root+"/Archive"))UnityEditor.AssetDatabase.CreateFolder(root,"Archive");
string before=root+"/Resources/StageSchool_Runtime.prefab",after=root+"/Archive/StageSchool_Runtime.prefab";
string guid=UnityEditor.AssetDatabase.AssetPathToGUID(before);
if(!string.IsNullOrEmpty(guid)){
 var error=UnityEditor.AssetDatabase.MoveAsset(before,after);if(!string.IsNullOrEmpty(error))throw new System.InvalidOperationException(error);
 if(UnityEditor.AssetDatabase.AssetPathToGUID(after)!=guid)throw new System.InvalidOperationException("Historical prefab GUID changed");
}
result.Log("Historical large B prefab outside Resources; preserved GUID="+UnityEditor.AssetDatabase.AssetPathToGUID(after));
