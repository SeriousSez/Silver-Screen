var root=AssetDatabase.LoadAssetAtPath<GameObject>(SilverScreen.Editor.EnvironmentArt.StageSchoolA4Review.Prefab);
var filters=root.GetComponentsInChildren<MeshFilter>(true);
foreach(var bench in filters){
 string name=AnimationUtility.CalculateTransformPath(bench.transform,root.transform);if(!name.StartsWith("Exterior/ApplicantBench"))continue;
 var vertices=bench.sharedMesh.vertices;var bounds=new Bounds(bench.transform.TransformPoint(vertices[0]),Vector3.zero);foreach(var v in vertices)bounds.Encapsulate(bench.transform.TransformPoint(v));
 float nearest=float.PositiveInfinity;string wallName="";int count=0;
 foreach(var wall in filters){
  string path=AnimationUtility.CalculateTransformPath(wall.transform,root.transform);if(!path.StartsWith("Shell/Front"))continue;
  var verts=wall.sharedMesh.vertices;for(int i=0;i<verts.Length;i++)verts[i]=wall.transform.TransformPoint(verts[i]);var ix=wall.sharedMesh.triangles;
  for(int i=0;i<ix.Length;i+=3){var a=verts[ix[i]];var b=verts[ix[i+1]];var c=verts[ix[i+2]];var mn=Vector3.Min(a,Vector3.Min(b,c));var mx=Vector3.Max(a,Vector3.Max(b,c));
   if(mx.x<bounds.min.x||mn.x>bounds.max.x||mx.y<bounds.min.y||mn.y>bounds.max.y)continue;count++;
   if(mn.z<nearest){nearest=mn.z;wallName=path;}
  }
 }
 result.Log(name+" facade="+wallName+" frontZ="+nearest.ToString("F4")+" rearZ="+bounds.max.z.ToString("F4")+" separation="+(nearest-bounds.max.z).ToString("F4")+" triangles="+count);
 if(count==0 || nearest-bounds.max.z<.10f)throw new System.InvalidOperationException("Bench requires at least 100 mm clearance from the facade: "+name);
}
