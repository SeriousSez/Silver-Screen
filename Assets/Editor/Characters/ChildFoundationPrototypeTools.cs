using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.Characters
{
    /// <summary>Isolated structural imports; no child facial authoring or production replacement.</summary>
    public static class ChildFoundationPrototypeTools
    {
        public const string Root = "Assets/SilverScreen/Art/Characters/ChildFoundationPrototype";
        public const string Evidence = "TestResults/ChildFoundation";
        public static string Model(string family) => Root + "/" + family + "/" + family + ".fbx";
        public static string Prefab(string family) => Root + "/" + family + "/SS_" + family + "Structural.prefab";
        public static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);
        [Serializable] public sealed class Shape
        {
            public string name, status;
            public float defaultWeight;
            public float[] frames, maxDisplacementMetres;
            public bool storedFinite, sampledFinite, resetExact;
        }
        [Serializable] public sealed class Inspection
        {
            public string family, sourceSha256;
            public bool avatarValid, avatarHuman;
            public float globalScale, fileScale;
            public Vector3 boundsMin, boundsMax, modelScale;
            public int vertices, triangles, skinBones, transforms, submeshes, minimumWeights, maximumWeights;
            public string[] hierarchy, humanMapping;
            public Shape[] shapes;
        }

        public static void ImportAndInspect(string family)
        {
            if (family != "Boy" && family != "Girl") throw new ArgumentException("Unknown family.");
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode required.");
            string relative = $"Stylized_{family}_Body_Base/Stylized_{family}_Body_Base_Shape_Keys/Shape_Keys/Stylized{family}BodyBaseMesh3DModelFacialShapeKeys(deform_bones_only).fbx";
            string source = "C:/Users/Sez/Documents/Assets/rigged-stylized-human-body-base-mesh-rigged-3d-models/" + relative;
            string expected = family == "Boy" ? "eeb40153409f883104f4dbf29da23bdf7e415546d57fc4893d4a7af317d3c4df" : "37986d08d3fc659b48172f2c0026d594a3b570fd6d98a4ba15233221f90b268f";
            using var hash = SHA256.Create();
            string actual = BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(source))).Replace("-", "").ToLowerInvariant();
            if (actual != expected) throw new InvalidDataException("Source differs from reviewed inventory.");
            Directory.CreateDirectory(Path.GetDirectoryName(Model(family)));
            Directory.CreateDirectory(Evidence + "/" + family + "/Unity");
            if (!File.Exists(Model(family))) File.Copy(source, Model(family));
            else if (!File.ReadAllBytes(Model(family)).SequenceEqual(File.ReadAllBytes(source))) throw new InvalidDataException("Local import differs.");
            AssetDatabase.ImportAsset(Model(family), ImportAssetOptions.ForceSynchronousImport);
            var importer = (ModelImporter)AssetImporter.GetAtPath(Model(family));
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.globalScale = 1; importer.useFileScale = true;
            importer.importAnimation = false; importer.importBlendShapes = true;
            importer.importNormals = ModelImporterNormals.Import; importer.isReadable = true;
            importer.optimizeGameObjects = false; importer.importCameras = false; importer.importLights = false;
            importer.skinWeights = ModelImporterSkinWeights.Custom; importer.maxBonesPerVertex = 10; importer.minBoneWeight = .00001f;
            importer.SaveAndReimport();
            var preview = EditorSceneManager.NewPreviewScene();
            var go = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Model(family)));
            SceneManager.MoveGameObjectToScene(go, preview);
            try
            {
                var animator = go.GetComponent<Animator>(); animator.enabled = false;
                var skin = go.GetComponentInChildren<SkinnedMeshRenderer>(); var mesh = skin.sharedMesh;
                var world = mesh.vertices.Select(skin.transform.TransformPoint).ToArray();
                var result = new Inspection { family = family, sourceSha256 = actual,
                    avatarValid = animator.avatar != null && animator.avatar.isValid,
                    avatarHuman = animator.avatar != null && animator.avatar.isHuman,
                    globalScale = importer.globalScale, fileScale = importer.fileScale, modelScale = go.transform.localScale,
                    boundsMin = new Vector3(world.Min(v=>v.x),world.Min(v=>v.y),world.Min(v=>v.z)),
                    boundsMax = new Vector3(world.Max(v=>v.x),world.Max(v=>v.y),world.Max(v=>v.z)),
                    vertices = mesh.vertexCount, triangles = mesh.triangles.Length/3, submeshes = mesh.subMeshCount,
                    skinBones = skin.bones.Length, transforms = go.GetComponentsInChildren<Transform>().Length,
                    hierarchy = go.GetComponentsInChildren<Transform>().Select(t=>AnimationUtility.CalculateTransformPath(t,go.transform)+" | position="+t.position.ToString("F6")+" scale="+t.localScale.ToString("F6")).ToArray() };
                using (var counts = mesh.GetBonesPerVertex()) { result.minimumWeights=counts.Min(v=>(int)v); result.maximumWeights=counts.Max(v=>(int)v); }
                result.humanMapping = result.avatarValid && result.avatarHuman ? Enumerable.Range(0,(int)HumanBodyBones.LastBone).Select(i=> {
                    var b=animator.GetBoneTransform((HumanBodyBones)i);return ((HumanBodyBones)i)+"="+(b==null?"MISSING":AnimationUtility.CalculateTransformPath(b,go.transform)); }).ToArray() : Array.Empty<string>();
                var baked = new Mesh();
                try
                {
                    skin.BakeMesh(baked,true); var neutral=baked.vertices;
                    var dv=new Vector3[mesh.vertexCount];var dn=new Vector3[dv.Length];var dt=new Vector3[dv.Length];
                    var shapes=new List<Shape>();
                    for(int s=0;s<mesh.blendShapeCount;s++)
                    {
                        var row=new Shape {name=mesh.GetBlendShapeName(s), defaultWeight=skin.GetBlendShapeWeight(s),
                            storedFinite=true,sampledFinite=true,frames=new float[mesh.GetBlendShapeFrameCount(s)],maxDisplacementMetres=new float[5]};
                        for(int f=0;f<row.frames.Length;f++) {row.frames[f]=mesh.GetBlendShapeFrameWeight(s,f);mesh.GetBlendShapeFrameVertices(s,f,dv,dn,dt);row.storedFinite &= dv.All(Finite)&&dn.All(Finite)&&dt.All(Finite);}
                        for(int w=0;w<5;w++)
                        {
                            skin.SetBlendShapeWeight(s,w*25);skin.BakeMesh(baked,true);var points=baked.vertices;
                            bool finite=points.All(Finite)&&baked.normals.All(Finite);row.sampledFinite &= finite;
                            row.maxDisplacementMetres[w]=finite?points.Select((p,i)=>skin.transform.TransformVector(p-neutral[i]).magnitude).Max():-1;
                        }
                        skin.SetBlendShapeWeight(s,row.defaultWeight);skin.BakeMesh(baked,true);row.resetExact=neutral.SequenceEqual(baked.vertices);
                        row.status=row.storedFinite&&row.sampledFinite&&row.resetExact&&row.frames.All(w=>w>0)&&row.maxDisplacementMetres.Skip(1).All(d=>d>1e-7f)?"SUPPORTED":"BROKEN";
                        shapes.Add(row);
                    }
                    result.shapes=shapes.ToArray();
                }
                finally {Object.DestroyImmediate(baked);}
                File.WriteAllText(Evidence+"/"+family+"/Unity/inspection.json",JsonUtility.ToJson(result,true));
                // Explicit structural gate: preserve the inspected import even when a candidate fails.
                if(!result.avatarValid || !result.avatarHuman || result.shapes.Length!=29 || result.shapes.Any(s=>s.status!="SUPPORTED")) return;
                animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(HumanBasePrototypeTools.Root+"/Animation/PrototypeLocomotion.controller");
                animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.enabled=true;
                var material=AssetDatabase.LoadAssetAtPath<Material>(HumanBasePrototypeTools.Root+"/Materials/SuppliedNeutralURP.mat");
                skin.sharedMaterials=Enumerable.Repeat(material,mesh.subMeshCount).ToArray();skin.updateWhenOffscreen=true;
                go.name="SS_"+family+"Structural";
                // Preserve imported scale; translate the measured foot minimum only.
                go.transform.position=Vector3.up*-result.boundsMin.y;
                PrefabUtility.SaveAsPrefabAsset(go,Prefab(family));
            }
            finally {Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(preview);}
        }
    }
}
