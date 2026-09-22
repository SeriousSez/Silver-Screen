using System;
using System.Text;
using SilverScreen.Domain.ReusableAssets;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace SilverScreen.Presentation.ReusableAssets
{
    /// <summary>Composes only on content changes. No camera, scene lookup, shared material edits, or per-frame work.</summary>
    public static class DisplayArtworkComposer
    {
        public static RenderTexture Compose(DisplayArtworkTemplate template, ArtworkContext context)
        {
            if (template.Background == null || template.Font == null || template.TextShader == null)
                throw new ArgumentException("Artwork needs a background, static font, and text shader.");
            if (template.Font.atlasPopulationMode != AtlasPopulationMode.Static)
                throw new ArgumentException("Artwork requires a static font atlas to preserve shared assets.");
            var rt = new RenderTexture(template.Background.width, template.Background.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB)
            { name = "Instance artwork: " + template.name, hideFlags = HideFlags.HideAndDontSave, filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            GameObject layout = null;
            Material material = null;
            var old = RenderTexture.active;
            try
            {
                rt.Create();
                Graphics.Blit(template.Background, rt);
                layout = new GameObject("Artwork text layout", typeof(RectTransform)) { hideFlags = HideFlags.HideAndDontSave };
                layout.SetActive(false);
                var text = layout.AddComponent<TextMeshPro>();
                text.font = template.Font;
                text.fontSharedMaterial = template.Font.material;
                text.richText = false;
                text.enableAutoSizing = true;
                text.textWrappingMode = TextWrappingModes.NoWrap;
                text.overflowMode = TextOverflowModes.Truncate;
                text.alignment = TextAlignmentOptions.Center;
                text.color = Color.white;
                text.isOrthographic = true;
                text.parseCtrlCharacters = false;
                // TMP must run Awake once before ForceMeshUpdate can lay out an inactive object.
                layout.SetActive(true);
                text.renderer.forceRenderingOff = true;
                material = new Material(template.TextShader) { hideFlags = HideFlags.HideAndDontSave };
                using var commands = new CommandBuffer { name = "Compose display text fields" };
                commands.SetRenderTarget(rt);
                commands.SetViewport(new Rect(0, 0, rt.width, rt.height));
                commands.SetViewProjectionMatrices(Matrix4x4.identity, GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(0, rt.width, 0, rt.height, -1, 1), false));
                foreach (var field in template.Fields)
                {
                    text.text = SupportedText(template.Font, string.IsNullOrEmpty(field.Key) ? field.Literal : context.Get(field.Key));
                    text.fontSize = field.FontSize;
                    text.fontSizeMax = field.FontSize;
                    text.fontSizeMin = Mathf.Max(8, field.FontSize * .35f);
                    text.rectTransform.sizeDelta = field.Rect.size;
                    text.ForceMeshUpdate(true, true);
                    // Each command needs its own mesh snapshot: TMP reuses its layout mesh.
                    for (int i = 0; i < text.textInfo.materialCount; i++)
                    {
                        var meshInfo = text.textInfo.meshInfo[i];
                        if (meshInfo.vertexCount == 0) continue;
                        var mesh = UnityEngine.Object.Instantiate(meshInfo.mesh);
                        mesh.hideFlags = HideFlags.HideAndDontSave;
                        var properties = new MaterialPropertyBlock();
                        properties.SetTexture("_MainTex", meshInfo.material.GetTexture("_MainTex"));
                        // SetColor performs the project's sRGB-to-linear conversion itself.
                        properties.SetColor("_Color", field.Color);
                        commands.DrawMesh(mesh, Matrix4x4.Translate(new Vector3(field.Rect.center.x, field.Rect.center.y, 0)), material, 0, 0, properties);
                        // Execute while this immutable snapshot is alive, then reuse the command buffer.
                        Graphics.ExecuteCommandBuffer(commands);
                        commands.Clear();
                        commands.SetRenderTarget(rt);
                        commands.SetViewport(new Rect(0, 0, rt.width, rt.height));
                        commands.SetViewProjectionMatrices(Matrix4x4.identity, GL.GetGPUProjectionMatrix(Matrix4x4.Ortho(0, rt.width, 0, rt.height, -1, 1), false));
                        Dispose(mesh);
                    }
                }
                return rt;
            }
            catch { rt.Release(); Dispose(rt); throw; }
            finally { RenderTexture.active = old; if (layout != null) layout.SetActive(false); Dispose(layout); Dispose(material); }
        }
        private static string SupportedText(TMP_FontAsset font, string value)
        {
            // Avoid TMP's global dynamic fallback atlases. Localization can supply another static template font.
            var result = new StringBuilder();
            foreach (char c in value ?? string.Empty) result.Append(font.HasCharacter(c, false, false) ? c : '?');
            return result.ToString();
        }
        private static void Dispose(UnityEngine.Object obj)
        { if (obj == null) return; if (Application.isPlaying) UnityEngine.Object.Destroy(obj); else UnityEngine.Object.DestroyImmediate(obj); }
    }
}
