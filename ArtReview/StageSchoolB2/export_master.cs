var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(SilverScreen.Editor.EnvironmentArt.StageSchoolA4Review.Prefab);
var parts=new System.Collections.Generic.List<SilverScreen.Editor.EnvironmentArt.StageSchoolA4Review.Part>();
var seen=new System.Collections.Generic.HashSet<UnityEngine.Mesh>();
var costs=new System.Collections.Generic.List<string>();
costs.Add("path,meshGUID,triangles,vertices");
foreach(var f in prefab.GetComponentsInChildren<UnityEngine.MeshFilter>(true)){
 var m=f.sharedMesh;var path=UnityEditor.AnimationUtility.CalculateTransformPath(f.transform,prefab.transform);
 string guid=UnityEditor.AssetDatabase.AssetPathToGUID(UnityEditor.AssetDatabase.GetAssetPath(m));
 costs.Add(path+","+guid+","+(m.triangles.Length/3)+","+m.vertexCount);
 if(!seen.Add(m))continue;
 var p=new SilverScreen.Editor.EnvironmentArt.StageSchoolA4Review.Part();p.name=guid;p.sourceGroup=path;
 var v=f.transform.position;p.pivot=new float[]{v.x,v.y,v.z};
 p.positions=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(m.vertices,x=>new float[]{x.x,x.y,x.z}));
 p.normals=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(m.normals,x=>new float[]{x.x,x.y,x.z}));
 p.uv=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.SelectMany(m.uv,x=>new float[]{x.x,x.y}));
 p.materials=System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Select(f.GetComponent<UnityEngine.Renderer>().sharedMaterials,x=>UnityEditor.AssetDatabase.GetAssetPath(x)));
 p.submeshes=new SilverScreen.Editor.EnvironmentArt.StageSchoolA4Review.Submesh[m.subMeshCount];
 for(int i=0;i<m.subMeshCount;i++)p.submeshes[i]=new SilverScreen.Editor.EnvironmentArt.StageSchoolA4Review.Submesh{indices=m.GetTriangles(i)};
 parts.Add(p);
}
System.IO.File.WriteAllText("ArtExports/StageSchoolB2/master_meshes.json",UnityEngine.JsonUtility.ToJson(new SilverScreen.Editor.EnvironmentArt.StageSchoolA4Review.Packet{parts=parts.ToArray()}));
System.IO.File.WriteAllLines("ArtReview/StageSchoolB2/master_categories.csv",costs);
result.Log("A4 unique meshes="+parts.Count+"; scene="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().path+"; dirty="+UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty+"; compilationFailed="+UnityEditor.EditorUtility.scriptCompilationFailed);
