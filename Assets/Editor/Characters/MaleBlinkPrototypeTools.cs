using System;
using System.IO;
using System.Linq;
using SilverScreen.Presentation.Characters;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace SilverScreen.Editor.Characters
{
    /// <summary>Isolated MaleCornerRelax10 addition; never reimports or rewrites the bald baseline.</summary>
    public static class MaleBlinkPrototypeTools
    {
        public const string Root = "Assets/SilverScreen/Art/Characters/MaleFoundationPrototype";
        public const string Prefab = Root + "/SS_MaleBlinkPrototype.prefab";
        public const string MeshPath = Root + "/AdultMaleBald_BlinkBoth.asset";
        public const string Review = "TestResults/MaleFoundation/UnityBlink";
        public const string BaselineMesh = MaleFoundationPrototypeTools.Root + "/AdultMaleBald_RetainedData.asset";

        [Serializable] public sealed class NativeReference
        {
            public Vector3[] vertices, world, normals, worldNormals;
            public Vector4[] tangents;
            public Vector2[] uv;
            public int[] triangles;
            public float[] matrix;
            public string[] shapes;
        }

        public sealed class Frame
        {
            public float Weight;
            public Vector3[] Positions, Normals, Tangents, ReferenceNormals;
        }

        public sealed class Addition
        {
            public int[] SourceVertices;
            public Vector3[] ReferenceNeutral, ReferenceClosed;
            public Frame[] Frames;
        }

        public static Matrix4x4 ModelMatrix => AssetDatabase.LoadAssetAtPath<GameObject>(
            MaleFoundationPrototypeTools.Model).GetComponentInChildren<SkinnedMeshRenderer>().transform.localToWorldMatrix;

        [MenuItem("SilverScreen/Characters/Male BlinkBoth/1 Export native reference")]
        public static void ExportNativeReference()
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(BaselineMesh);
            if (mesh == null) throw new FileNotFoundException("The existing retained-data bald baseline is required.");
            var mat = ModelMatrix;
            var data = new NativeReference
            {
                vertices = mesh.vertices, world = mesh.vertices.Select(mat.MultiplyPoint3x4).ToArray(), normals = mesh.normals,
                worldNormals = mesh.normals.Select(v => mat.inverse.transpose.MultiplyVector(v).normalized).ToArray(),
                tangents = mesh.tangents, uv = mesh.uv, triangles = mesh.triangles,
                matrix = Enumerable.Range(0, 16).Select(i => mat[i]).ToArray(),
                shapes = Enumerable.Range(0, mesh.blendShapeCount).Select(mesh.GetBlendShapeName).ToArray()
            };
            Directory.CreateDirectory(Review);
            File.WriteAllText(Review + "/native-baseline.json", JsonUtility.ToJson(data));
        }

        public static Addition ReadAddition()
        {
            using var stream = new BinaryReader(File.OpenRead(Review + "/blink-addition.bin"));
            if (new string(stream.ReadChars(8)) != "SSBLINK1") throw new InvalidDataException("Unexpected packet version.");
            int count = stream.ReadInt32(), frames = stream.ReadInt32();
            if (count != 17738 || frames != 20) throw new InvalidDataException("Unexpected selected packet dimensions.");
            var data = new Addition { SourceVertices = new int[count], Frames = new Frame[frames] };
            for (int i = 0; i < count; i++) data.SourceVertices[i] = stream.ReadInt32();
            data.ReferenceNeutral = ReadVectors(stream, count); data.ReferenceClosed = ReadVectors(stream, count);
            for (int f = 0; f < frames; f++) data.Frames[f] = new Frame
            {
                Weight = stream.ReadSingle(), Positions = ReadVectors(stream, count), Normals = ReadVectors(stream, count),
                Tangents = ReadVectors(stream, count), ReferenceNormals = ReadVectors(stream, count)
            };
            if (stream.BaseStream.Position != stream.BaseStream.Length) throw new InvalidDataException("Trailing packet data.");
            return data;
        }

        private static Vector3[] ReadVectors(BinaryReader stream, int count)
        {
            var values = new Vector3[count];
            for (int i = 0; i < count; i++)
            {
                values[i] = new Vector3(stream.ReadSingle(), stream.ReadSingle(), stream.ReadSingle());
                if (!Finite(values[i])) throw new InvalidDataException("Non-finite packet data.");
            }
            return values;
        }

        public static bool Finite(Vector3 v) => float.IsFinite(v.x) && float.IsFinite(v.y) && float.IsFinite(v.z);

        [MenuItem("SilverScreen/Characters/Male BlinkBoth/2 Build isolated selected candidate")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Build in Edit Mode.");
            var source = AssetDatabase.LoadAssetAtPath<Mesh>(BaselineMesh);
            var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MaleFoundationPrototypeTools.Prefab);
            if (source == null || sourcePrefab == null || source.blendShapeCount != 29)
                throw new InvalidOperationException("Expected the existing native bald baseline with 29 shapes.");
            var data = ReadAddition();
            var mat = ModelMatrix; var vertices = source.vertices;
            float neutralError = vertices.Select((v, i) => Vector3.Distance(mat.MultiplyPoint3x4(v), data.ReferenceNeutral[i])).Max();
            if (neutralError > .00005f) throw new InvalidOperationException("Packet does not match this native bald mesh.");
            Directory.CreateDirectory(Root); AssetDatabase.Refresh();
            // Instantiate preserves all native vertex buffers, bindposes, UV channels and 29 frames,
            // including their original frame semantics. Do not recalculate the baseline.
            var mesh = Object.Instantiate(source); mesh.name = "AdultMaleBald_MaleCornerRelax10_BlinkBoth";
            foreach (var frame in data.Frames)
                mesh.AddBlendShapeFrame("BlinkBoth", frame.Weight, frame.Positions, frame.Normals, frame.Tangents);
            var asset = AssetDatabase.LoadAssetAtPath<Mesh>(MeshPath);
            if (asset == null) { AssetDatabase.CreateAsset(mesh, MeshPath); asset = mesh; }
            else { EditorUtility.CopySerialized(mesh, asset); Object.DestroyImmediate(mesh); EditorUtility.SetDirty(asset); }
            AssetDatabase.SaveAssetIfDirty(asset);
            var root = Object.Instantiate(sourcePrefab);
            try
            {
                root.name = "SS_MaleBlinkPrototype";
                var visual = root.GetComponent<HumanBasePrototypeVisual>();
                visual.Face.sharedMesh = asset;
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
                File.WriteAllLines(Review + "/construction.txt", new[] {
                    "Basis: native retained bald mesh clone; no FBX reimport or baseline recalculation.",
                    "Selected: MaleCornerRelax10; one added BlinkBoth, 20 normal-sampling frames, linear position delta.",
                    "Avatar="+visual.Animator.avatar.name+" human="+visual.Animator.avatar.isHuman+" valid="+visual.Animator.avatar.isValid,
                    "Bones="+visual.Face.bones.Length+" shapes="+asset.blendShapeCount+" skinRenderers="+root.GetComponentsInChildren<SkinnedMeshRenderer>().Length,
                    "Vertices="+asset.vertexCount+" triangles="+asset.triangles.Length/3+" submeshes="+asset.subMeshCount,
                    "Shared native mesh memory bytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(asset),
                    "Baseline memory bytes="+UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(source),
                    "Baseline reference mismatch metres="+neutralError.ToString("R")
                });
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
