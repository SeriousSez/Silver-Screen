using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SilverScreen.Editor
{
    /// <summary>Reproducible companion to the Stage Blender generators; assets only, no scene setup.</summary>
    public static class StageAssetImport
    {
        [Serializable] private sealed class Glyph
        {
            public string character = string.Empty;
            public float[] vertices = Array.Empty<float>();
            public int[] triangles = Array.Empty<int>();
        }
        [Serializable] private sealed class GlyphLibrary { public Glyph[] glyphs = Array.Empty<Glyph>(); }
        private const string Root = "Assets/SilverScreen/Environment/ReferenceKit/";

        public static void ImportGeneratedData()
        {
            AssetDatabase.Refresh();
            var library = JsonUtility.FromJson<GlyphLibrary>(File.ReadAllText("ArtSource/ReferenceKit1930/stage_glyphs.json"));
            if (library?.glyphs == null) throw new InvalidDataException("Stage glyph source is missing or invalid.");
            if (!AssetDatabase.IsValidFolder(Root + "StageGlyphs"))
                AssetDatabase.CreateFolder(Root.TrimEnd('/'), "StageGlyphs");
            foreach (var glyph in library.glyphs)
            {
                string path = Root + "StageGlyphs/Glyph_" + glyph.character + ".asset";
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                bool create = mesh == null;
                if (create) mesh = new Mesh();
                else { Undo.RecordObject(mesh, "Regenerate Stage glyph"); mesh.Clear(); }
                mesh.name = "Glyph_" + glyph.character;
                var vertices = new Vector3[glyph.vertices.Length / 3];
                for (int i = 0; i < vertices.Length; i++)
                    vertices[i] = new Vector3(glyph.vertices[i * 3], glyph.vertices[i * 3 + 1], glyph.vertices[i * 3 + 2]);
                mesh.vertices = vertices;
                mesh.triangles = glyph.triangles;
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                if (create) AssetDatabase.CreateAsset(mesh, path);
                else EditorUtility.SetDirty(mesh);
            }
            string[] families = { "Timber", "Concrete", "Floor" };
            string[] materials = { "STG_DarkDoors", "STG_Foundation", "STG_ConcreteFloor" };
            for (int i = 0; i < families.Length; i++)
            {
                string stem = Root + "StageTextures/STG_" + families[i];
                var importer = AssetImporter.GetAtPath(stem + "Normal.png") as TextureImporter;
                if (importer == null) throw new FileNotFoundException(stem + "Normal.png");
                importer.textureType = TextureImporterType.NormalMap;
                importer.sRGBTexture = false;
                importer.wrapMode = TextureWrapMode.Repeat;
                importer.anisoLevel = 4;
                importer.SaveAndReimport();
                var surfaceImporter = (TextureImporter)AssetImporter.GetAtPath(stem + "Surface.png");
                surfaceImporter.sRGBTexture = false;
                surfaceImporter.alphaSource = TextureImporterAlphaSource.FromInput;
                surfaceImporter.SaveAndReimport();
                var material = AssetDatabase.LoadAssetAtPath<Material>(Root + "StageMaterials/" + materials[i] + ".mat");
                Undo.RecordObject(material, "Import Stage material detail");
                material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(stem + "Color.png"));
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(stem + "Normal.png"));
                material.SetFloat("_BumpScale", i == 0 ? 0.28f : 0.16f);
                material.SetTexture("_MetallicGlossMap", AssetDatabase.LoadAssetAtPath<Texture2D>(stem + "Surface.png"));
                material.SetFloat("_Smoothness", 1f);
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
                material.EnableKeyword("_NORMALMAP");
                EditorUtility.SetDirty(material);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
