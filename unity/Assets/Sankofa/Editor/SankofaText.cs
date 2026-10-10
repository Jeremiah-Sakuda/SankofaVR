using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Crisp text for captions and controls. TextMeshPro's essentials (its shaders and the OFL-licensed LiberationSans
    /// font) ship inside Unity's UI package; this imports them once, without the EmojiOne sprites, which would
    /// otherwise ship from a Resources folder under an attribution licence we do not need.
    /// </summary>
    public static class SankofaText
    {
        const string EssentialsPackage = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
        public const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
        const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        public const string CaptionMaterialPath = SankofaArt.Folder + "/Caption text.mat";
        const string AtkinsonFolder = "Assets/Sankofa/Art/Fonts/AtkinsonHyperlegible";

        /// <summary>Characters rendered into the atlas ahead of time, so their first appearance never stutters.</summary>
        const string Prepopulated =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "\u00A0\u00B7\u2013\u2014\u2018\u2019\u201C\u201D\u2026";

        static readonly string[] Unneeded =
        {
            "Assets/TextMesh Pro/Resources/Sprite Assets",
            "Assets/TextMesh Pro/Sprites",
        };

        [MenuItem("Sankofa/Import Text Essentials")]
        public static void EnsureEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) == null)
            {
                AssetDatabase.ImportPackage(EssentialsPackage, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }
            foreach (var path in Unneeded)
                if (AssetDatabase.IsValidFolder(path)) AssetDatabase.DeleteAsset(path);
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath);
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                // Cleared even when the reference already dangles, so no GUID points at the removed sprites.
                var sprite = so.FindProperty("m_defaultSpriteAsset");
                if (sprite != null)
                {
                    sprite.objectReferenceValue = null;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(settings);
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log(AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath) != null
                ? "[Sankofa] Text essentials ready."
                : "[Sankofa] Text essentials did not import; run Sankofa > Import Text Essentials.");
        }

        public static TMP_FontAsset Font =>
            AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath)
            ?? throw new System.InvalidOperationException("TextMeshPro essentials are missing; run SankofaProjectSetup.Configure first.");

        /// <summary>Atkinson Hyperlegible, designed for low-vision readability, for captions.</summary>
        public static TMP_FontAsset CaptionFont => Atkinson("Regular");

        /// <summary>The bold cut for the small labels on the control plaques.</summary>
        public static TMP_FontAsset LabelFont => Atkinson("Bold");

        /// <summary>
        /// A signed-distance-field font asset made from the approved OFL font file, with LiberationSans as fallback
        /// for any glyph it lacks. Created once beside the font file and reused.
        /// </summary>
        static TMP_FontAsset Atkinson(string weight)
        {
            var path = $"{AtkinsonFolder}/Atkinson Hyperlegible {weight} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            var source = AssetDatabase.LoadAssetAtPath<UnityEngine.Font>($"{AtkinsonFolder}/AtkinsonHyperlegible-{weight}.ttf")
                ?? throw new System.InvalidOperationException("Atkinson Hyperlegible " + weight + " is missing from " + AtkinsonFolder);
            var asset = TMP_FontAsset.CreateFontAsset(source, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = $"Atkinson Hyperlegible {weight} SDF";
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.TryAddCharacters(Prepopulated, out _);
            asset.fallbackFontAssetTable = new List<TMP_FontAsset> { Font };
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>The caption face with a soft dark outline, so lines stay legible over flame and sky.</summary>
        public static Material CaptionMaterial(TMP_FontAsset font)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(CaptionMaterialPath);
            if (material == null)
            {
                material = new Material(font.material) { name = "Caption text" };
                AssetDatabase.CreateAsset(material, CaptionMaterialPath);
            }
            material.shader = font.material.shader;
            material.SetTexture(ShaderUtilities.ID_MainTex, font.material.GetTexture(ShaderUtilities.ID_MainTex));
            material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.14f);
            material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 0.85f));
            material.SetFloat(ShaderUtilities.ID_FaceDilate, 0.05f);
            // After the scene and its backing, under the hands. The caption board draws it over the scene at run time
            // (CaptionBoard.OverScene): a saved material cannot keep the depth test.
            material.renderQueue = 3991;
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}
