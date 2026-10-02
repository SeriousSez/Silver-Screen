using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.Characters
{
    /// <summary>Girl-only native-buffer subset. Does not replace structural or production assets.</summary>
    public static class GirlFoundationPrototypeTools
    {
        public const string Root="Assets/SilverScreen/Art/Characters/GirlFoundationPrototype";
        public const string Evidence="TestResults/GirlFoundation/Unity";
        public const string BaldMesh=Root+"/GirlBald_RetainedData.asset";
        public const string Prefab=Root+"/SS_GirlBaldReference.prefab";
        [Serializable] public sealed class TriangleSet {public int[] indices;}
        [Serializable] public sealed class NativeData {public Vector3[] points;public Vector2[] uv;public TriangleSet[] submeshes;public string[] shapes;}
        [Serializable] public sealed class Partition {public int[] keep;public int triangles;public string nativeSha256;}

        public static SkinnedMeshRenderer Original => AssetDatabase.LoadAssetAtPath<GameObject>(ChildFoundationPrototypeTools.Model("Girl")).GetComponentInChildren<SkinnedMeshRenderer>();

        public static void ExportNative()
        {
            Directory.CreateDirectory(Evidence);
            var skin=Original;var mesh=skin.sharedMesh;
            if(mesh.vertexCount!=20207 || mesh.blendShapeCount!=29 || skin.bones.Length!=51)throw new InvalidDataException("Girl native input changed.");
            var data=new NativeData {points=mesh.vertices.Select(skin.transform.TransformPoint).ToArray(),uv=mesh.uv,
                submeshes=Enumerable.Range(0,mesh.subMeshCount).Select(s=>new TriangleSet{indices=mesh.GetTriangles(s)}).ToArray(),
                shapes=Enumerable.Range(0,mesh.blendShapeCount).Select(mesh.GetBlendShapeName).ToArray()};
            File.WriteAllText(Evidence+"/native.json",JsonUtility.ToJson(data));
        }

        [MenuItem("SilverScreen/Characters/Girl Foundation/Build bald baseline")]
        public static void Build()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Edit Mode required.");
            var partition=JsonUtility.FromJson<Partition>(File.ReadAllText(Evidence+"/partition.json"));
            using(var hash=System.Security.Cryptography.SHA256.Create())
                if(BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(Evidence+"/native.json"))).Replace("-","").ToLowerInvariant()!=partition.nativeSha256)
                    throw new InvalidDataException("Native correspondence evidence changed.");
            var source=Original.sharedMesh;var keep=partition.keep;
            if(keep.Distinct().Count()!=keep.Length || keep.Any(i=>i<0||i>=source.vertexCount))throw new InvalidDataException("Invalid Girl partition.");
            var remap=Enumerable.Repeat(-1,source.vertexCount).ToArray();for(int i=0;i<keep.Length;i++)remap[keep[i]]=i;
            var mesh=new Mesh{name="GirlBald_RetainedData",indexFormat=source.indexFormat};
            var positions=source.vertices;var normals=source.normals;var tangents=source.tangents;var colors=source.colors;
            mesh.vertices=keep.Select(i=>positions[i]).ToArray();mesh.normals=keep.Select(i=>normals[i]).ToArray();
            if(tangents.Length>0)mesh.tangents=keep.Select(i=>tangents[i]).ToArray();
            if(colors.Length>0)mesh.colors=keep.Select(i=>colors[i]).ToArray();
            for(int c=0;c<8;c++)
            {
                var values=new List<Vector4>();source.GetUVs(c,values);if(values.Count==0)continue;
                int d=source.GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute.TexCoord0+c);
                if(d==2)mesh.SetUVs(c,keep.Select(i=>(Vector2)values[i]).ToArray());
                else if(d==3)mesh.SetUVs(c,keep.Select(i=>(Vector3)values[i]).ToArray());
                else mesh.SetUVs(c,keep.Select(i=>values[i]).ToArray());
            }
            mesh.bindposes=source.bindposes;
            using(var counts=source.GetBonesPerVertex())using(var weights=source.GetAllBoneWeights())
            {
                var newCounts=new Unity.Collections.NativeArray<byte>(keep.Length,Unity.Collections.Allocator.Temp);
                try
                {
                var selected=new List<BoneWeight1>();int offset=0;
                for(int i=0;i<source.vertexCount;i++)
                {if(remap[i]>=0){newCounts[remap[i]]=counts[i];for(int j=0;j<counts[i];j++)selected.Add(weights[offset+j]);}offset+=counts[i];}
                using(var newWeights=new Unity.Collections.NativeArray<BoneWeight1>(selected.ToArray(),Unity.Collections.Allocator.Temp))mesh.SetBoneWeights(newCounts,newWeights);
                }
                finally{newCounts.Dispose();}
            }
            mesh.subMeshCount=source.subMeshCount;int triangleCount=0;
            for(int s=0;s<source.subMeshCount;s++)
            {
                var indices=source.GetTriangles(s);var retained=new List<int>();
                for(int t=0;t<indices.Length;t+=3)
                {
                    int count=Enumerable.Range(t,3).Count(i=>remap[indices[i]]>=0);
                    if(count!=0&&count!=3)throw new InvalidDataException("Girl hair partition crosses a triangle.");
                    if(count==3)retained.AddRange(Enumerable.Range(t,3).Select(i=>remap[indices[i]]));
                }
                mesh.SetTriangles(retained,s);triangleCount+=retained.Count/3;
            }
            if(triangleCount!=partition.triangles || triangleCount!=32330)throw new InvalidDataException("Girl retained surface disagrees with Blender.");
            var dv=new Vector3[source.vertexCount];var dn=new Vector3[dv.Length];var dt=new Vector3[dv.Length];
            for(int s=0;s<source.blendShapeCount;s++)for(int f=0;f<source.GetBlendShapeFrameCount(s);f++)
            {
                source.GetBlendShapeFrameVertices(s,f,dv,dn,dt);
                mesh.AddBlendShapeFrame(source.GetBlendShapeName(s),source.GetBlendShapeFrameWeight(s,f),keep.Select(i=>dv[i]).ToArray(),keep.Select(i=>dn[i]).ToArray(),keep.Select(i=>dt[i]).ToArray());
            }
            mesh.RecalculateBounds();Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var asset=AssetDatabase.LoadAssetAtPath<Mesh>(BaldMesh);
            if(asset==null){AssetDatabase.CreateAsset(mesh,BaldMesh);asset=mesh;}
            else{EditorUtility.CopySerialized(mesh,asset);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(asset);}
            var scene=EditorSceneManager.NewPreviewScene();
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ChildFoundationPrototypeTools.Prefab("Girl")));
            SceneManager.MoveGameObjectToScene(go,scene);
            try
            {
                go.name="SS_GirlBaldReference";var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();skin.sharedMesh=asset;skin.localBounds=asset.bounds;
                PrefabUtility.SaveAsPrefabAsset(go,Prefab);
                File.WriteAllText(Evidence+"/representation.txt","skinBones="+skin.bones.Length+" shapes="+asset.blendShapeCount+" renderers="+go.GetComponentsInChildren<Renderer>().Length+
                    " transforms="+go.GetComponentsInChildren<Transform>().Length+" meshMemoryBytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(asset));
            }
            finally{Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
            AssetDatabase.SaveAssetIfDirty(asset);
        }
    }
}
