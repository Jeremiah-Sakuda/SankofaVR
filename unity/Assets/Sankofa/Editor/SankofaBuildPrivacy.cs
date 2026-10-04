using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>Runs after Meta's token injection. Build settings must never carry editor credentials.</summary>
    public sealed class SankofaBuildPrivacy : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        const string SettingsType = "Meta.XR.ImmersiveDebugger.DevAgent.RuntimeSettings";
        static readonly Dictionary<UnityEngine.Object, string> originals = new Dictionary<UnityEngine.Object, string>();
        static readonly HashSet<string> forbidden = new HashSet<string>();
        public int callbackOrder => int.MaxValue;

        internal static IEnumerable<UnityEngine.Object> Settings() => AssetDatabase.FindAssets("t:ScriptableObject", new[] { "Assets" })
            .Select(AssetDatabase.GUIDToAssetPath).Select(AssetDatabase.LoadMainAssetAtPath)
            .Where(a => a != null && a.GetType().FullName == SettingsType);

        public static void CaptureEditorSettings()
        {
            RestoreEditorSettings();
            foreach (var asset in Settings()) originals[asset] = EditorJsonUtility.ToJson(asset);
            forbidden.Clear();
            EditorApplication.update -= RestoreAfterFailedBuild;
            EditorApplication.update += RestoreAfterFailedBuild;
        }

        static void RestoreAfterFailedBuild()
        {
            if (!BuildPipeline.isBuildingPlayer) RestoreEditorSettings();
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            foreach (var asset in Settings())
            {
                var so = new SerializedObject(asset);
                foreach (var name in new[] { "accessToken", "witClientAccessToken", "serverAddress" })
                {
                    var field = so.FindProperty(name);
                    if (field == null) throw new BuildFailedException("DevAgent settings changed; review the build sanitizer.");
                    if (field.stringValue.Length > 0) forbidden.Add(field.stringValue);
                    field.stringValue = "";
                }
                var enabled = so.FindProperty("enabled");
                if (enabled == null) throw new BuildFailedException("DevAgent enable flag is missing.");
                enabled.boolValue = false;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(asset);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Sankofa] Removed development credentials and disabled DevAgent for this build.");
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            try { VerifyApk(report.summary.outputPath); }
            finally { RestoreEditorSettings(); }
        }

        public static void VerifyApk(string path)
        {
            if (!path.EndsWith(".apk", StringComparison.OrdinalIgnoreCase)) return;
            using var archive = ZipFile.OpenRead(path);
            foreach (var entry in archive.Entries)
            {
                using var stream = entry.Open();
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                var bytes = memory.ToArray();
                foreach (var value in forbidden)
                    if (Contains(bytes, Encoding.UTF8.GetBytes(value)) || Contains(bytes, Encoding.Unicode.GetBytes(value)))
                        throw new BuildFailedException("Build contains local development settings. Do not distribute this APK.");
            }
            Debug.Log("[Sankofa] APK verified: local development credentials are absent.");
        }

        static bool Contains(byte[] bytes, byte[] value)
        {
            for (var i = 0; i <= bytes.Length - value.Length; i++)
            {
                if (bytes[i] != value[0]) continue;
                var j = 1;
                while (j < value.Length && bytes[i + j] == value[j]) j++;
                if (j == value.Length) return true;
            }
            return false;
        }

        public static void RestoreEditorSettings()
        {
            EditorApplication.update -= RestoreAfterFailedBuild;
            foreach (var pair in originals)
                if (pair.Key != null)
                {
                    EditorJsonUtility.FromJsonOverwrite(pair.Value, pair.Key);
                    EditorUtility.SetDirty(pair.Key);
                }
            if (originals.Count > 0) AssetDatabase.SaveAssets();
            originals.Clear();
        }
    }

    // Take the snapshot before SDK callbacks mutate the asset, also for builds launched in Unity's UI.
    public sealed class SankofaBuildPrivacySnapshot : IPreprocessBuildWithReport
    {
        public int callbackOrder => int.MinValue;
        public void OnPreprocessBuild(BuildReport report) => SankofaBuildPrivacy.CaptureEditorSettings();
    }
}
