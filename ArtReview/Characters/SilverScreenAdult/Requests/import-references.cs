string folder = "Assets/Editor/SilverScreenAdultSource/References";
AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
result.Log("PROJECT=" + Application.dataPath + " SCENE=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " DIRTY=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty);
foreach (var path in System.IO.Directory.GetFiles(folder, "*.png"))
{
    string normalized = path.Replace('\\', '/');
    AssetDatabase.ImportAsset(normalized, ImportAssetOptions.ForceSynchronousImport);
    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(normalized);
    result.Log(normalized + " ID=" + EntityId.ToULong(texture.GetEntityId()));
}
