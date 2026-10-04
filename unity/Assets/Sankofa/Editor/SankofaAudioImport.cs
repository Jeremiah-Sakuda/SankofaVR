using UnityEditor;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Import settings for the griot's narration under Resources/Narration: mono, compressed in memory as Vorbis so
    /// fifteen minutes of voice stays small in the build, and loaded only when a line is spoken.
    /// </summary>
    public sealed class SankofaAudioImport : AssetPostprocessor
    {
        public const string Folder = "Assets/Sankofa/Resources/Narration/";

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(Folder)) return;
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            var settings = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.CompressedInMemory,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.6f,
                sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                preloadAudioData = true,
            };
            importer.defaultSampleSettings = settings;
            importer.SetOverrideSampleSettings("Android", settings);
        }
    }
}
