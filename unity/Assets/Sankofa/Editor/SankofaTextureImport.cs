using UnityEditor;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Import settings for scanned materials under Art/Scanned, so they never depend on someone ticking boxes:
    /// colour maps in sRGB, "_nor_gl" maps as linear OpenGL-convention normal maps (Unity's convention), mipmapped,
    /// capped at 1K and ASTC-compressed on Quest.
    /// </summary>
    public sealed class SankofaTextureImport : AssetPostprocessor
    {
        public const string Folder = "Assets/Sankofa/Art/Scanned/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (TextureImporter)assetImporter;
            var normal = assetPath.Contains("_nor_gl");
            importer.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normal;
            importer.mipmapEnabled = true;
            importer.anisoLevel = 4;
            importer.wrapMode = UnityEngine.TextureWrapMode.Repeat;
            importer.maxTextureSize = 1024;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            {
                name = "Android",
                overridden = true,
                maxTextureSize = 1024,
                format = TextureImporterFormat.ASTC_6x6,
                compressionQuality = 50,
            });
        }
    }
}
