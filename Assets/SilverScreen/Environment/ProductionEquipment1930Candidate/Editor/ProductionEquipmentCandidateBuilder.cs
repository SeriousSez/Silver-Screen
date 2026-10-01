using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace SilverScreen.Editor.EnvironmentArt
{
    /// <summary>Independent review-only equipment; no dependency on a building or gameplay kit.</summary>
    public static class ProductionEquipmentCandidateBuilder
    {
        public const string Root="Assets/SilverScreen/Environment/ProductionEquipment1930Candidate";
        const string Kit="Assets/SilverScreen/Environment/PeriodEnvironment1930";
        [Serializable] class Submesh { public int[] indices; }
        [Serializable] class Part { public string name; public float[] positions,normals,uv; public string[] materials; public Submesh[] submeshes; }
        [Serializable] class Equipment { public string name; public Part[] parts; }
        [Serializable] class Packet { public Equipment[] assets; }
        static void Folder(string path)
        {
            var p="Assets";
            foreach(var s in path.Split('/').Skip(1)){if(!AssetDatabase.IsValidFolder(p+"/"+s))AssetDatabase.CreateFolder(p,s);p+="/"+s;}
        }
        [MenuItem("SilverScreen/Art/Production Equipment Candidates/Import")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            Folder(Root+"/Meshes");Folder(Root+"/Prefabs");
            Packet data;
            using(var file=File.OpenRead("ArtExports/ProductionEquipment1930Candidate/equipment_meshes.json.gz"))
            using(var zip=new GZipStream(file,CompressionMode.Decompress))
            using(var reader=new StreamReader(zip))data=JsonUtility.FromJson<Packet>(reader.ReadToEnd());
            var scene=EditorSceneManager.NewPreviewScene();
            try
            {
                foreach(var asset in data.assets)
                {
                    var root=new GameObject(asset.name);SceneManager.MoveGameObjectToScene(root,scene);
                    foreach(var p in asset.parts)
                    {
                        var go=new GameObject(p.name);SceneManager.MoveGameObjectToScene(go,scene);go.transform.SetParent(root.transform,false);
                        var path=Root+"/Meshes/"+asset.name+"_"+p.name+".asset";
                        var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(path);
                        if(mesh==null){mesh=new Mesh();AssetDatabase.CreateAsset(mesh,path);}
                        mesh.Clear();mesh.name=asset.name+"_"+p.name;mesh.indexFormat=IndexFormat.UInt32;
                        mesh.vertices=Enumerable.Range(0,p.positions.Length/3).Select(i=>new Vector3(p.positions[3*i],p.positions[3*i+1],p.positions[3*i+2])).ToArray();
                        mesh.normals=Enumerable.Range(0,p.normals.Length/3).Select(i=>new Vector3(p.normals[3*i],p.normals[3*i+1],p.normals[3*i+2])).ToArray();
                        mesh.uv=Enumerable.Range(0,p.uv.Length/2).Select(i=>new Vector2(p.uv[2*i],p.uv[2*i+1])).ToArray();
                        mesh.subMeshCount=p.submeshes.Length;
                        for(int i=0;i<p.submeshes.Length;i++)mesh.SetTriangles(p.submeshes[i].indices,i,false);
                        mesh.RecalculateBounds();mesh.RecalculateTangents();EditorUtility.SetDirty(mesh);
                        go.AddComponent<MeshFilter>().sharedMesh=mesh;
                        go.AddComponent<MeshRenderer>().sharedMaterials=p.materials.Select(n=>AssetDatabase.LoadAssetAtPath<Material>(Kit+"/Materials/"+n+".mat")??throw new FileNotFoundException("Equipment material: "+n)).ToArray();
                    }
                    PrefabUtility.SaveAsPrefabAsset(root,Root+"/Prefabs/"+asset.name+".prefab");
                    UnityEngine.Object.DestroyImmediate(root);
                }
                AssetDatabase.SaveAssets();
            }
            finally{EditorSceneManager.ClosePreviewScene(scene);}
        }
    }
}
