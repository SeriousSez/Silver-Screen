using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SilverScreen.Presentation.Characters;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.Characters
{
    /// <summary>Imports the generated bald comparison without replacing any validated prototype.</summary>
    public static class FemaleBaldPrototypeTools
    {
        public const string Root = "Assets/SilverScreen/Art/Characters/FemaleBaldPrototype";
        public const string Model = Root + "/Models/AdultFemaleBald.fbx";
        public const string Prefab = Root + "/SS_FemaleBaldPrototype.prefab";
        public const string Review = "TestResults/FemaleBaldBase";

        [MenuItem("SilverScreen/Characters/Female Bald Base/Build isolated generated comparison")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
            if (!File.Exists(Model)) throw new FileNotFoundException("Run export_female_bald.py first.", Model);
            AssetDatabase.Refresh();
            var source = (ModelImporter)AssetImporter.GetAtPath(HumanBasePrototypeTools.Root + "/Models/AdultFemale.fbx");
            var importer = (ModelImporter)AssetImporter.GetAtPath(Model);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.globalScale = source.globalScale;
            importer.useFileScale = source.useFileScale;
            importer.importAnimation = false;
            importer.importBlendShapes = true;
            importer.importNormals = source.importNormals;
            importer.importTangents = source.importTangents;
            importer.optimizeGameObjects = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importVisibility = false;
            importer.skinWeights = source.skinWeights;
            importer.maxBonesPerVertex = source.maxBonesPerVertex;
            importer.minBoneWeight = source.minBoneWeight;
            var human = importer.humanDescription;
            human.human = source.humanDescription.human;
            importer.humanDescription = human;
            importer.SaveAndReimport();
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var avatar = asset.GetComponent<Animator>().avatar;
            var assetSkin = asset.GetComponentInChildren<SkinnedMeshRenderer>();
            if (avatar == null || !avatar.isHuman || !avatar.isValid || assetSkin.bones.Length != 51 || assetSkin.sharedMesh.blendShapeCount != 29)
                throw new InvalidOperationException("Generated bald import failed body/shape validation.");
            var originalAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HumanBasePrototypeTools.Root + "/Models/AdultFemale.fbx");
            var originalSkin = originalAsset.GetComponentInChildren<SkinnedMeshRenderer>();
            var retainedMesh = BuildRetainedMesh(originalSkin, assetSkin);
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(HumanBasePrototypeTools.PrefabPath));
            try
            {
                go.name = "SS_FemaleBaldPrototype";
                var presentation = go.GetComponent<HumanBasePrototypeVisual>();
                var skin = presentation.Animator.GetComponentInChildren<SkinnedMeshRenderer>();
                // Keep the validated Avatar, exact skeleton, body scale, material and bindings.
                // Only the derived mesh changes; removed hair must not rescale the anatomy.
                skin.sharedMesh = retainedMesh;
                skin.localBounds = retainedMesh.bounds;
                presentation.Animator.gameObject.name = "AdultFemaleBald";
                PrefabUtility.SaveAsPrefabAsset(go, Prefab);
            }
            finally { Object.DestroyImmediate(go); }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory(Review);
            File.WriteAllLines(Review + "/unity-import.txt", new[] {
                "Humanoid valid=" + avatar.isValid + " human=" + avatar.isHuman,
                "Skin bones=" + assetSkin.bones.Length + " renderers=" + asset.GetComponentsInChildren<SkinnedMeshRenderer>().Length,
                "Vertices=" + assetSkin.sharedMesh.vertexCount + " triangles=" + assetSkin.sharedMesh.triangles.Length / 3,
                "BlendShapes=" + assetSkin.sharedMesh.blendShapeCount,
                "Runtime retained vertices="+retainedMesh.vertexCount+" triangles="+retainedMesh.triangles.Length/3,
                "Runtime shared mesh memory bytes=" + UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(retainedMesh)
            }.Concat(importer.humanDescription.human.Select(h => h.humanName + "=" + h.boneName)));
        }

        private static (int,int,int) Cell(Vector3 p) =>
            (Mathf.RoundToInt(p.x*100000),Mathf.RoundToInt(p.y*100000),Mathf.RoundToInt(p.z*100000));

        private static Mesh BuildRetainedMesh(SkinnedMeshRenderer original, SkinnedMeshRenderer exported)
        {
            // Blender owns the explicit island removal. Match its retained surface to the
            // original import, then subset the original native data to avoid FBX recalculating
            // existing shape normals or changing shape-frame semantics.
            var source=original.sharedMesh;var target=exported.sharedMesh;
            var a=source.vertices;var b=target.vertices;var au=source.uv;var bu=target.uv;
            var au2=source.uv2;var bu2=target.uv2;
            var points=b.Select(exported.transform.TransformPoint).ToArray();
            var cells=new Dictionary<(int,int,int),List<int>>();
            for(int i=0;i<points.Length;i++)
            {
                var key=Cell(points[i]);
                if(!cells.TryGetValue(key,out var list))cells[key]=list=new List<int>();
                list.Add(i);
            }
            var keep=new List<int>();var remap=Enumerable.Repeat(-1,a.Length).ToArray();
            for(int i=0;i<a.Length;i++)
            {
                var point=original.transform.TransformPoint(a[i]);var c=Cell(point);bool found=false;
                for(int x=-2;x<=2&&!found;x++)for(int y=-2;y<=2&&!found;y++)for(int z=-2;z<=2&&!found;z++)
                    if(cells.TryGetValue((c.Item1+x,c.Item2+y,c.Item3+z),out var list))foreach(int j in list)
                        if(Vector3.Distance(point,points[j])<.00003f && Vector2.Distance(au[i],bu[j])<.000002f && Vector2.Distance(au2[i],bu2[j])<.000002f)
                        {found=true;break;}
                if(found){remap[i]=keep.Count;keep.Add(i);}
            }
            if(keep.Count==0 || keep.Count==a.Length)throw new InvalidOperationException("No verified hair removal partition.");
            var mesh=new Mesh { name="AdultFemaleBald_RetainedData",indexFormat=source.indexFormat };
            mesh.vertices=keep.Select(i=>a[i]).ToArray();
            var normals=source.normals;mesh.normals=keep.Select(i=>normals[i]).ToArray();
            var tangents=source.tangents;if(tangents.Length>0)mesh.tangents=keep.Select(i=>tangents[i]).ToArray();
            var colours=source.colors;if(colours.Length>0)mesh.colors=keep.Select(i=>colours[i]).ToArray();
            for(int channel=0;channel<8;channel++)
            {
                var values=new List<Vector4>();source.GetUVs(channel,values);if(values.Count==0)continue;
                var dimension=source.GetVertexAttributeDimension(UnityEngine.Rendering.VertexAttribute.TexCoord0+channel);
                if(dimension==2)mesh.SetUVs(channel,keep.Select(i=>(Vector2)values[i]).ToArray());
                else if(dimension==3)mesh.SetUVs(channel,keep.Select(i=>(Vector3)values[i]).ToArray());
                else mesh.SetUVs(channel,keep.Select(i=>values[i]).ToArray());
            }
            mesh.bindposes=source.bindposes;
            var counts=source.GetBonesPerVertex();var weights=source.GetAllBoneWeights();
            var newCounts=new Unity.Collections.NativeArray<byte>(keep.Count,Unity.Collections.Allocator.Temp);
            var selectedWeights=new List<BoneWeight1>();int offset=0;
            try
            {
                for(int i=0;i<a.Length;i++)
                {
                    if(remap[i]>=0){newCounts[remap[i]]=counts[i];for(int j=0;j<counts[i];j++)selectedWeights.Add(weights[offset+j]);}
                    offset+=counts[i];
                }
                var newWeights=new Unity.Collections.NativeArray<BoneWeight1>(selectedWeights.ToArray(),Unity.Collections.Allocator.Temp);
                try{mesh.SetBoneWeights(newCounts,newWeights);}finally{newWeights.Dispose();}
            }
            finally{counts.Dispose();weights.Dispose();newCounts.Dispose();}
            mesh.subMeshCount=source.subMeshCount;int triangleCount=0;
            for(int sub=0;sub<source.subMeshCount;sub++)
            {
                var indices=source.GetTriangles(sub);var kept=new List<int>();
                for(int t=0;t<indices.Length;t+=3)
                {
                    int count=(remap[indices[t]]>=0?1:0)+(remap[indices[t+1]]>=0?1:0)+(remap[indices[t+2]]>=0?1:0);
                    if(count!=0&&count!=3)throw new InvalidOperationException("Removal crosses a triangle; stop.");
                    if(count==3){kept.Add(remap[indices[t]]);kept.Add(remap[indices[t+1]]);kept.Add(remap[indices[t+2]]);}
                }
                triangleCount+=kept.Count/3;mesh.SetTriangles(kept,sub);
            }
            if(triangleCount!=32512)throw new InvalidOperationException("Retained triangle set disagrees with Blender.");
            var dv=new Vector3[a.Length];var dn=new Vector3[a.Length];var dt=new Vector3[a.Length];
            var frameLines=new List<string>();
            for(int shape=0;shape<source.blendShapeCount;shape++)for(int frame=0;frame<source.GetBlendShapeFrameCount(shape);frame++)
            {
                source.GetBlendShapeFrameVertices(shape,frame,dv,dn,dt);
                float weight=source.GetBlendShapeFrameWeight(shape,frame);
                mesh.AddBlendShapeFrame(source.GetBlendShapeName(shape),weight,keep.Select(i=>dv[i]).ToArray(),keep.Select(i=>dn[i]).ToArray(),keep.Select(i=>dt[i]).ToArray());
                frameLines.Add(source.GetBlendShapeName(shape)+" frame="+frame+" weight="+weight);
            }
            mesh.RecalculateBounds();
            string path=Root+"/AdultFemaleBald_RetainedData.asset";
            var existing=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(existing==null){AssetDatabase.CreateAsset(mesh,path);existing=mesh;}
            else{EditorUtility.CopySerialized(mesh,existing);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(existing);}
            Directory.CreateDirectory(Review);
            File.WriteAllLines(Review+"/unity-original-indices.txt",keep.Select(i=>i.ToString()));
            File.WriteAllLines(Review+"/preserved-shape-frames.txt",frameLines);
            return existing;
        }
    }
}
