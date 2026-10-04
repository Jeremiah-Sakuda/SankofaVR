using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Android build for Quest, written to the ignored artifacts/ folder at the repository root:
    /// Unity -batchmode -quit -projectPath unity -buildTarget Android -executeMethod Sankofa.EditorTools.SankofaBuild.BuildMain
    /// Builds only the explicitly listed scene; the local competition brief and references live outside Assets.
    /// </summary>
    public static class SankofaBuild
    {
        public const string OutputName = "Sankofa.apk";

        [MenuItem("Sankofa/Build APK")]
        public static void BuildMain()
        {
            var repository = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var output = Path.Combine(repository, "artifacts", OutputName);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var scenes = new[] { SankofaSceneBuilder.HearthScene };

            foreach (var deleted in LocalOnlyAssets.DeleteTestRunLeftovers()) Debug.Log("[Sankofa] Deleted test-run leftover before building: " + deleted);
            LocalOnlyAssets.Check(scenes);
            try
            {
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = scenes,
                    locationPathName = output,
                    target = BuildTarget.Android,
                    targetGroup = BuildTargetGroup.Android,
                    options = BuildOptions.None,
                });
                var summary = report.summary;
                var bytes = File.Exists(output) ? new FileInfo(output).Length : 0;
                Debug.Log($"[Sankofa] Build {summary.result}: {output} ({bytes / (1024.0 * 1024.0):0.0} MB APK, {summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalTime})");
                if (summary.result != BuildResult.Succeeded) throw new BuildFailedException("Sankofa build failed; see the build report.");
            }
            finally
            {
                SankofaBuildPrivacy.RestoreEditorSettings();
            }
        }
    }
}
