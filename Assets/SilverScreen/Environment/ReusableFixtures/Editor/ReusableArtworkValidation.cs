using System;
using System.IO;
using SilverScreen.Domain.ReusableAssets;
using SilverScreen.Presentation.ReusableAssets;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace SilverScreen.Editor.ReusableAssets
{
    public static class ReusableArtworkValidation
    {
        private const string Root = ReusableFixtureBuilder.Root;
        public static void BuildTemplates()
        {
            if (!AssetDatabase.IsValidFolder(Root + "/Artwork")) AssetDatabase.CreateFolder(Root, "Artwork");
            Build("OpenHouse", "StudioPoster", new[] {
                Field(ArtworkContext.StudioName, new Rect(45,735,510,105), 42, "#f0e6ca"),
                Field(ArtworkContext.CurrentYear, new Rect(65,40,470,65), 30, "#e7d5a8") });
            Build("StudioNotice", "StudioNotice", new[] {
                Field(ArtworkContext.ProductionTitle, new Rect(75,173,850,70), 38, "#274850"),
                Field(ArtworkContext.StudioName, new Rect(75,67,850,65), 32, "#aa7544") });
        }
        private static ArtworkTextField Field(string key, Rect rect, float size, string color)
        {
            ColorUtility.TryParseHtmlString(color, out var c);
            return new ArtworkTextField { Key = key, Rect = rect, FontSize = size, Color = c };
        }
        private static void Build(string name, string background, ArtworkTextField[] fields)
        {
            string path = Root + "/Artwork/" + name + ".asset";
            var template = AssetDatabase.LoadAssetAtPath<DisplayArtworkTemplate>(path);
            if (template == null) { template = ScriptableObject.CreateInstance<DisplayArtworkTemplate>(); AssetDatabase.CreateAsset(template, path); }
            template.Background = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + background + ".png");
            template.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            template.TextShader = Shader.Find("SilverScreen/Display Artwork Text");
            template.Fields = fields;
            EditorUtility.SetDirty(template); AssetDatabase.SaveAssetIfDirty(template);
        }
        public static ArtworkContext Context(string studio, string year, string title)
        {
            var context = new ArtworkContext();
            context.Set(ArtworkContext.StudioName, studio); context.Set(ArtworkContext.CurrentYear, year); context.Set(ArtworkContext.ProductionTitle, title);
            return context;
        }
        public static DisplayArtworkPresenter Attach(DisplayInsertSlot slot, string template, InsertRole role, ArtworkContext context)
        {
            var presenter = slot.gameObject.AddComponent<DisplayArtworkPresenter>();
            presenter.Configure(slot, AssetDatabase.LoadAssetAtPath<DisplayArtworkTemplate>(Root + "/Artwork/" + template + ".asset"), role, ArtworkFit.FitWithMat,
                new[] { Preview(ArtworkContext.StudioName, context), Preview(ArtworkContext.CurrentYear, context), Preview(ArtworkContext.ProductionTitle, context) });
            presenter.Initialize(context);
            return presenter;
        }
        private static ArtworkFieldValue Preview(string key, ArtworkContext context) => new ArtworkFieldValue { Key = key, Value = context.Get(key) };
        public static void SaveTexture(Texture source, string name)
        {
            var old = RenderTexture.active; var texture = new Texture2D(source.width, source.height, TextureFormat.RGB24, false, false);
            try
            {
                RenderTexture.active = (RenderTexture)source; texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0); texture.Apply();
                File.WriteAllBytes(ReusableFixtureBuilder.ReviewRoot + "/" + name + ".png", texture.EncodeToPNG());
            }
            finally { RenderTexture.active = old; UnityEngine.Object.DestroyImmediate(texture); }
        }
    }
}
