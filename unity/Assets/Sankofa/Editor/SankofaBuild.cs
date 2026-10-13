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
            Build(SankofaSceneBuilder.HearthScene, OutputName);
        }

        [MenuItem("Sankofa/Build Illustrated Theatre APK")]
        public static void BuildIllustratedTheatre()
        {
            Build(SankofaSceneBuilder.TheatreScene, "Sankofa-IllustratedTheatre.apk");
        }

        [MenuItem("Sankofa/Build Living Gallery APK")]
        public static void BuildLivingGallery() => Build(SankofaSceneBuilder.GalleryScene, "Sankofa-LivingGallery.apk");

        static void Build(string scene, string outputName)
        {
            var repository = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var output = Path.Combine(repository, "artifacts", outputName);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            var scenes = new[] { scene };

            foreach (var deleted in LocalOnlyAssets.DeleteTestRunLeftovers()) Debug.Log("[Sankofa] Deleted test-run leftover before building: " + deleted);
            LocalOnlyAssets.Check(scenes);
            // The build carries the commit it was made from, logged at start, so an APK is traceable to its source.
            var version = PlayerSettings.bundleVersion;
            var commit = Commit(repository);
            PlayerSettings.bundleVersion = commit != null ? $"{version}+{commit}" : version;
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
                PlayerSettings.bundleVersion = version;
            }
        }

        /// <summary>The short hash of the checked-out commit, with "-dirty" if the working tree has changes; null without git.</summary>
        static string Commit(string repository)
        {
            string Git(string arguments)
            {
                try
                {
                    var info = new System.Diagnostics.ProcessStartInfo("git", arguments)
                    {
                        WorkingDirectory = repository,
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                    using var process = System.Diagnostics.Process.Start(info);
                    var output = process.StandardOutput.ReadToEnd().Trim();
                    process.WaitForExit();
                    return process.ExitCode == 0 ? output : null;
                }
                catch (System.Exception)
                {
                    return null;
                }
            }
            var hash = Git("rev-parse --short HEAD");
            if (string.IsNullOrEmpty(hash)) return null;
            return string.IsNullOrEmpty(Git("status --porcelain")) ? hash : hash + "-dirty";
        }
    }
}
