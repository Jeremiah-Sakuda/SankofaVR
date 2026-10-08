using UnityEditor;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Import settings for the griot's narration under Resources/Narration: mono, compressed in memory as Vorbis so
    /// fifteen minutes of voice stays small in the build, and loaded only when a line is spoken. Field recordings
    /// under Art/Recorded are decompressed when the scene loads, so the fire's cracks can be found in them and they
    /// loop without decoding cost; distant beds are kept at 24 kHz to halve that memory.
    /// </summary>
    public sealed class SankofaAudioImport : AssetPostprocessor
    {
        public const string Folder = "Assets/Sankofa/Resources/Narration/";

        public const string RecordedFolder = "Assets/Sankofa/Art/Recorded/";

        void OnPreprocessAudio()
        {
            if (assetPath.StartsWith(RecordedFolder))
            {
                ImportRecording();
                return;
            }
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

        void ImportRecording()
        {
            var importer = (AudioImporter)assetImporter;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            var name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            var detailed = name == "hearth-fire" || name == "small-fire" || name == "brook" || name == "spikes" || name.StartsWith("bleat");
            var settings = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.DecompressOnLoad,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.5f,
                sampleRateSetting = detailed ? AudioSampleRateSetting.PreserveSampleRate : AudioSampleRateSetting.OverrideSampleRate,
                sampleRateOverride = 24000,
                preloadAudioData = true,
            };
            importer.defaultSampleSettings = settings;
            importer.SetOverrideSampleSettings("Android", settings);
        }
    }
}
