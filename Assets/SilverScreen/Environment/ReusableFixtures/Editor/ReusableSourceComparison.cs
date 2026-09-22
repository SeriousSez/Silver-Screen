using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace SilverScreen.Editor.ReusableAssets
{
    /// <summary>Independent check against the approved Unity FBX, not the new mesh packet.</summary>
    public static class ReusableSourceComparison
    {
        [Serializable] public class Result
        {
            public string asset;
            public int canonicalVertices, unmatchedVertices, canonicalTriangles, referenceTriangles;
            public float maximumPositionErrorMetres, maximumUvError, maximumNormalDifference;
        }
        [Serializable] public class Report { public Result[] assets; }
        private sealed class Source
        {
            public Mesh mesh; public Vector3[] vertices,normals; public Vector2[] uv; public Material[] materials;
            public Dictionary<Vector3Int,List<int>> buckets=new Dictionary<Vector3Int,List<int>>();
        }
        private static Vector3Int Cell(Vector3 p)=>new Vector3Int(Mathf.FloorToInt(p.x*1000),Mathf.FloorToInt(p.y*1000),Mathf.FloorToInt(p.z*1000));
        private static Matrix4x4 Basis(ReusableFixtureBuilder.Record r)
        {
            var m=Matrix4x4.identity;
            for(int i=0;i<3;i++)for(int j=0;j<3;j++)m[i,j]=r.basisRows[3*i+j];
            return m;
        }
        private static Dictionary<string,Source> ReadSources()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SilverScreen/Environment/Stage1CleanCandidate/Stage1_CleanCandidate_A.prefab");
            var result=new Dictionary<string,Source>();
            foreach(var f in prefab.GetComponentsInChildren<MeshFilter>(true).Where(f=>new[]{"Export_PermanentServices","Export_PermanentLighting","Export_PersonnelDoors","Export_FixedProtection","Export_AcousticTreatment"}.Contains(f.name)))
            {
                var s=new Source{mesh=f.sharedMesh,vertices=f.sharedMesh.vertices,normals=f.sharedMesh.normals,uv=f.sharedMesh.uv,materials=f.GetComponent<MeshRenderer>().sharedMaterials};
                for(int i=0;i<s.vertices.Length;i++) {var key=Cell(s.vertices[i]);if(!s.buckets.TryGetValue(key,out var list))s.buckets[key]=list=new List<int>();list.Add(i);}
                result.Add(f.name.Replace("Export_",""),s);
            }
            return result;
        }
        private static GameObject Reference(ReusableFixtureBuilder.Record r,GameObject canonical,Dictionary<string,Source> sources,Scene scene,out Result result)
        {
            result=new Result{asset=r.name};var root=new GameObject(r.name+"_ApprovedUnityReference");SceneManager.MoveGameObjectToScene(root,scene);
            var basis=Basis(r);var inverse=basis.inverse;var origin=ReusableFixtureBuilder.V(r.origin);
            foreach(var part in r.parts)
            {
                var source=sources[part.members.Select(m=>m.semanticGroup).Distinct().Single()];
                var native=canonical.transform.Find("Geometry/"+part.name).GetComponent<MeshFilter>().sharedMesh;
                var nv=native.vertices;var nn=native.normals;var nu=native.uv;var accepted=new bool[source.vertices.Length];
                for(int i=0;i<nv.Length;i++)
                {
                    result.canonicalVertices++;
                    // FBX mesh-local X is reflected relative to the saved Blender source.
                    var p=inverse.MultiplyVector(nv[i])+origin;p.x=-p.x;var cell=Cell(p);bool found=false;float bestNormal=float.PositiveInfinity;
                    for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)for(int dz=-1;dz<=1;dz++)
                    {
                        if(!source.buckets.TryGetValue(cell+new Vector3Int(dx,dy,dz),out var candidates))continue;
                        foreach(int j in candidates)
                        {
                            float pe=Vector3.Distance(p,source.vertices[j]),ue=Vector2.Distance(nu[i],source.uv[j]);
                            if(pe>0.00002f||ue>0.00002f)continue;
                            found=true;accepted[j]=true;var normal=source.normals[j];normal.x=-normal.x;normal=basis.MultiplyVector(normal);
                            bestNormal=Mathf.Min(bestNormal,Vector3.Distance(nn[i],normal));
                            result.maximumPositionErrorMetres=Mathf.Max(result.maximumPositionErrorMetres,pe);result.maximumUvError=Mathf.Max(result.maximumUvError,ue);
                        }
                    }
                    if(!found)result.unmatchedVertices++;else result.maximumNormalDifference=Mathf.Max(result.maximumNormalDifference,bestNormal);
                }
                result.canonicalTriangles+=native.triangles.Length/3;
                var positions=new List<Vector3>();var normals=new List<Vector3>();var uvs=new List<Vector2>();var mapping=new Dictionary<int,int>();
                int Vertex(int j)
                {
                    if(mapping.TryGetValue(j,out int existing))return existing;
                    var p=source.vertices[j];p.x=-p.x;p=basis.MultiplyVector(p-origin);
                    var n=source.normals[j];n.x=-n.x;n=basis.MultiplyVector(n);
                    int next=positions.Count;mapping[j]=next;positions.Add(p);normals.Add(n);uvs.Add(source.uv[j]);return next;
                }
                var submeshes=new List<int[]>();
                for(int sub=0;sub<source.mesh.subMeshCount;sub++)
                {
                    var triangles=source.mesh.GetTriangles(sub);var selected=new List<int>();
                    for(int i=0;i<triangles.Length;i+=3)
                    {
                        if(!accepted[triangles[i]]||!accepted[triangles[i+1]]||!accepted[triangles[i+2]])continue;
                        selected.Add(Vertex(triangles[i]));selected.Add(Vertex(triangles[i+1]));selected.Add(Vertex(triangles[i+2]));result.referenceTriangles++;
                    }
                    submeshes.Add(selected.ToArray());
                }
                var mesh=new Mesh{name="Temporary source reference "+part.name};mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uvs);mesh.subMeshCount=submeshes.Count;
                for(int i=0;i<submeshes.Count;i++)mesh.SetTriangles(submeshes[i],i,false);mesh.RecalculateBounds();mesh.RecalculateTangents();
                var obj=new GameObject(part.name);obj.transform.SetParent(root.transform,false);obj.AddComponent<MeshFilter>().sharedMesh=mesh;obj.AddComponent<MeshRenderer>().sharedMaterials=source.materials;
            }
            return root;
        }
        [MenuItem("SilverScreen/Art/Reusable Fixtures/4 Compare against approved Unity source")]
        public static void Run()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Compare in Edit Mode.");
            var original=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewPreviewScene();var results=new List<Result>();var temporaryMeshes=new List<Mesh>();
            Directory.CreateDirectory(ReusableFixtureBuilder.ReviewRoot);
            try
            {
                // Preview-scene culling separates references from the live studio without changing it.
                var sources=ReadSources();
                var sunObject=new GameObject("Comparison daylight");SceneManager.MoveGameObjectToScene(sunObject,scene);
                var sun=sunObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=2.4f;sun.transform.rotation=Quaternion.Euler(40,-145,0);sun.cullingMask=1<<30;
                var cameraObject=new GameObject("Comparison camera");SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.scene=scene;camera.cullingMask=1<<30;camera.nearClipPlane=.005f;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.21f,.24f);
                int index=10;
                foreach(var r in ReusableFixtureBuilder.ReadManifest().assets)
                {
                    var canonical=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ReusableFixtureBuilder.PrefabPath(r.name)),scene);
                    var reference=Reference(r,canonical,sources,scene,out var result);results.Add(result);
                    temporaryMeshes.AddRange(reference.GetComponentsInChildren<MeshFilter>().Select(f=>f.sharedMesh));
                    foreach(var root in new[]{canonical,reference})foreach(var t in root.GetComponentsInChildren<Transform>(true))t.gameObject.layer=30;
                    var bounds=canonical.GetComponent<SilverScreen.Presentation.ReusableAssets.ReusableFixture>().Definition.VisibleBounds;
                    var target=bounds.center;float extent=Mathf.Max(bounds.size.x,bounds.size.y,bounds.size.z);
                    var eye=target+new Vector3(extent*.55f,extent*.28f,extent*1.85f);
                    reference.SetActive(false);
                    ReusableFixtureBuilder.Capture(camera,(index++)+"_"+r.name+"_canonical",eye,target,40,1000,1000);
                    canonical.SetActive(false);reference.SetActive(true);
                    ReusableFixtureBuilder.Capture(camera,(index++)+"_"+r.name+"_source",eye,target,40,1000,1000);
                    Object.DestroyImmediate(canonical);Object.DestroyImmediate(reference);
                }
                File.WriteAllText(ReusableFixtureBuilder.ReviewRoot+"/source-comparison.json",JsonUtility.ToJson(new Report{assets=results.ToArray()},true));
                if(results.Any(r=>r.unmatchedVertices>0||r.canonicalTriangles!=r.referenceTriangles||r.maximumNormalDifference>.001f))
                    throw new InvalidOperationException("Source comparison differs; inspect source-comparison.json.");
            }
            finally
            {
                foreach(var mesh in temporaryMeshes)if(mesh!=null)Object.DestroyImmediate(mesh);
                EditorSceneManager.ClosePreviewScene(scene);if(original.IsValid()&&original.isLoaded)SceneManager.SetActiveScene(original);
            }
            Debug.Log("Reusable M1: all 19 fixtures match approved Unity source positions, UVs, normals and triangle counts.");
        }
    }
}
