using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SilverScreen.Presentation.Characters;
using SilverScreen.Presentation.Employees;
using SilverScreen.Presentation.Interaction;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.Characters
{
    /// <summary>Male-only isolated evidence. Never changes purchased assets or production spawns.</summary>
    public static class MaleFoundationPrototypeTools
    {
        public const string Root = "Assets/SilverScreen/Art/Characters/MaleFoundationPrototype";
        public const string Model = Root + "/Models/AdultMale.fbx";
        public const string Prefab = Root + "/SS_MaleBaldPrototype.prefab";
        public const string Review = "TestResults/MaleFoundation/Unity";
        public const string BaldMesh = Root + "/AdultMaleBald_RetainedData.asset";
        private const string Source = "C:/Users/Sez/Documents/Assets/rigged-stylized-human-body-base-mesh-rigged-3d-models/stylized-male-body-base/Stylized_Male_Body_Base_Shape_Keys/Shape Keys/StylizedMaleBodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx";

        [MenuItem("SilverScreen/Characters/Male Foundation/Build bald baseline")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Build in Edit Mode.");
            Directory.CreateDirectory(Root+"/Models"); Directory.CreateDirectory(Review);
            if (!File.Exists(Model)) File.Copy(Source,Model);
            else if (!File.ReadAllBytes(Model).SequenceEqual(File.ReadAllBytes(Source))) throw new InvalidOperationException("Male import differs from source.");
            AssetDatabase.Refresh();
            Configure(Model);
            var original=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var animator=original.GetComponent<Animator>();
            if(animator.avatar==null || !animator.avatar.isValid || !animator.avatar.isHuman) throw new InvalidOperationException("Male Humanoid invalid; stop.");
            var correspondence=Root+"/Models/MaleBaldCorrespondence.fbx";
            var ci=(ModelImporter)AssetImporter.GetAtPath(correspondence);
            ci.isReadable=true;ci.importAnimation=false;ci.importBlendShapes=true;ci.SaveAndReimport();
            var exported=AssetDatabase.LoadAssetAtPath<GameObject>(correspondence).GetComponentInChildren<SkinnedMeshRenderer>();
            var sourceSkin=original.GetComponentInChildren<SkinnedMeshRenderer>();
            var retained=BuildRetainedMesh(sourceSkin,exported);
            var go=new GameObject("SS_MaleBaldPrototype");go.SetActive(false);
            try
            {
                var nav=go.AddComponent<NavMeshAgent>();nav.radius=.35f;nav.height=2;nav.baseOffset=1;
                nav.speed=1.35f;nav.acceleration=8;nav.angularSpeed=360;nav.stoppingDistance=.12f;
                var capsule=go.AddComponent<CapsuleCollider>();capsule.height=2;capsule.radius=.35f;
                var employee=go.AddComponent<EmployeeAgent>();employee.SetGenericFilmingPresentationEnabled(false);
                var pivot=new GameObject("CharacterVisualRoot").transform;pivot.SetParent(go.transform,false);pivot.localPosition=Vector3.down;
                var visual=Object.Instantiate(original,pivot);visual.name="AdultMaleBald";
                // Keep Male's independently measured native stature; align native foot minimum.
                visual.transform.localPosition=Vector3.up*.0032076272f;
                var a=visual.GetComponent<Animator>();
                a.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HumanBasePrototypeTools.Root+"/Animation/PrototypeLocomotion.controller");
                a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;a.updateMode=AnimatorUpdateMode.UnscaledTime;
                var skin=visual.GetComponentInChildren<SkinnedMeshRenderer>();skin.sharedMesh=retained;skin.localBounds=retained.bounds;
                var material=AssetDatabase.LoadAssetAtPath<Material>(HumanBasePrototypeTools.Root+"/Materials/SuppliedNeutralURP.mat");
                skin.sharedMaterials=Enumerable.Repeat(material,retained.subMeshCount).ToArray();skin.updateWhenOffscreen=true;
                go.AddComponent<HumanBasePrototypeVisual>().Configure(a,pivot,skin);go.AddComponent<HeldPersonPresentation>();
                var indicator=GameObject.CreatePrimitive(PrimitiveType.Cylinder);indicator.name="SelectionIndicator";indicator.transform.SetParent(go.transform,false);
                indicator.transform.localPosition=new Vector3(0,-.98f,0);indicator.transform.localScale=new Vector3(.8f,.01f,.8f);
                Object.DestroyImmediate(indicator.GetComponent<Collider>());indicator.GetComponent<Renderer>().sharedMaterial=material;
                employee.SetSelectionIndicator(indicator);indicator.SetActive(false);go.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(go,Prefab);
            }
            finally {Object.DestroyImmediate(go);}
            AssetDatabase.SaveAssets();
            Probe();
        }

        private static void Configure(string path)
        {
            var importer=(ModelImporter)AssetImporter.GetAtPath(path);
            importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;
            importer.globalScale=1;importer.useFileScale=true;importer.importAnimation=false;importer.importBlendShapes=true;
            importer.importNormals=ModelImporterNormals.Import;importer.isReadable=true;importer.optimizeGameObjects=false;
            importer.importCameras=false;importer.importLights=false;importer.importVisibility=false;
            importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=10;importer.minBoneWeight=.00001f;
            // Unity auto-maps this Male hierarchy; Probe records each resulting actual transform.
            importer.SaveAndReimport();
        }

        public static bool Finite(Vector3 v) => float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
        public static void Probe()
        {
            Directory.CreateDirectory(Review);
            var go=Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
            try
            {
                go.GetComponent<NavMeshAgent>().enabled=false;
                var a=go.GetComponentInChildren<Animator>();a.enabled=false;
                var skin=go.GetComponentInChildren<SkinnedMeshRenderer>();var mesh=skin.sharedMesh;
                var lines=new List<string> {"Avatar valid="+a.avatar.isValid+" human="+a.avatar.isHuman,
                    "Transforms total="+go.GetComponentsInChildren<Transform>(true).Length+" skinBones="+skin.bones.Length,
                    "Vertices="+mesh.vertexCount+" triangles="+mesh.triangles.Length/3+" shapes="+mesh.blendShapeCount,
                    "Normals="+mesh.normals.Length+" tangents="+mesh.tangents.Length+" submeshes="+mesh.subMeshCount};
                for(int h=0;h<(int)HumanBodyBones.LastBone;h++) {var b=a.GetBoneTransform((HumanBodyBones)h);lines.Add("HUMAN "+(HumanBodyBones)h+"="+(b==null?"MISSING":b.name));}
                foreach(var b in skin.bones)lines.Add("SKIN "+b.name+" parent="+(b.parent==null?"":b.parent.name));
                var baked=new Mesh();skin.BakeMesh(baked,true);var neutral=baked.vertices;
                var dv=new Vector3[mesh.vertexCount];var dn=new Vector3[mesh.vertexCount];var dt=new Vector3[mesh.vertexCount];
                for(int s=0;s<mesh.blendShapeCount;s++)
                {
                    var frames=new List<string>();bool frameFinite=true;
                    for(int f=0;f<mesh.GetBlendShapeFrameCount(s);f++) {mesh.GetBlendShapeFrameVertices(s,f,dv,dn,dt);frameFinite&=dv.All(Finite)&&dn.All(Finite)&&dt.All(Finite);frames.Add(mesh.GetBlendShapeFrameWeight(s,f).ToString("R",System.Globalization.CultureInfo.InvariantCulture));}
                    var rows=new List<string>();bool allFinite=true;
                    foreach(float weight in new[]{0f,25f,50f,75f,100f})
                    {
                        skin.SetBlendShapeWeight(s,weight);skin.BakeMesh(baked,true);var v=baked.vertices;bool finite=v.All(Finite)&&baked.normals.All(Finite);allFinite&=finite;
                        rows.Add(weight+":"+(finite?"finite max="+v.Select((p,i)=>Vector3.Distance(p,neutral[i])).Max().ToString("R"):"NONFINITE"));
                    }
                    skin.SetBlendShapeWeight(s,0);skin.BakeMesh(baked,true);
                    if(!baked.vertices.SequenceEqual(neutral)) throw new InvalidOperationException("Shape neutral reset drift: "+s);
                    lines.Add("SHAPE "+s+" "+mesh.GetBlendShapeName(s)+" frames="+string.Join(",",frames)+" default=0 "+(allFinite&&frameFinite?"SUPPORTED":"BROKEN")+" "+string.Join(";",rows));
                }
                Object.DestroyImmediate(baked);
                File.WriteAllLines(Review+"/native-inspection.txt",lines);
            }
            finally {Object.DestroyImmediate(go);}
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
            var mesh=new Mesh { name="AdultMaleBald_RetainedData",indexFormat=source.indexFormat };
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
            if(triangleCount!=30574)throw new InvalidOperationException("Retained triangle set disagrees with Blender.");
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
            string path=Root+"/AdultMaleBald_RetainedData.asset";
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
