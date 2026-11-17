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
        public const string OutputName = "Sankofa-StoryTable.apk";

        [MenuItem("Sankofa/Build APK")]
        public static void BuildMain()
        {
            Build(SankofaSceneBuilder.StoryTableScene, OutputName);
        }

        [MenuItem("Sankofa/Comparison/Build Hearth APK")]
        public static void BuildHearth() => Build(SankofaSceneBuilder.HearthScene, "Sankofa-Hearth-Comparison.apk");

        [MenuItem("Sankofa/Build Illustrated Theatre APK")]
        public static void BuildIllustratedTheatre()
        {
            Build(SankofaSceneBuilder.TheatreScene, "Sankofa-IllustratedTheatre.apk");
        }

        [MenuItem("Sankofa/Build Living Gallery APK")]
        public static void BuildLivingGallery() => Build(SankofaSceneBuilder.GalleryScene, "Sankofa-LivingGallery.apk");

        [MenuItem("Sankofa/Build Personal Story Table APK")]
        public static void BuildPersonalStoryTable() => Build(SankofaSceneBuilder.StoryTableScene, OutputName);

        /// <summary>Explicit illustrated-river review artifact. The default and signed release still target StoryTable.</summary>
        [MenuItem("Sankofa/Build Painted River Review APK")]
        public static void BuildRiverReview() => Build(SankofaSceneBuilder.RiverPaintedScene, "Sankofa-RiverPainted-Review.apk");

        public const string ReleaseOutputName = "Sankofa-StoryTable-release.apk";

        /// <summary>
        /// The signed upload build for the Competition release channel. Signing and the version code come only from
        /// the environment, so no keystore path, password or alias is ever written into the project:
        /// SANKOFA_KEYSTORE (path), SANKOFA_KEYSTORE_PASS, SANKOFA_KEY_ALIAS, SANKOFA_KEY_PASS (defaults to the
        /// keystore password) and SANKOFA_VERSION_CODE (an integer above every code already uploaded).
        /// Refuses to build from a working tree with uncommitted changes.
        /// </summary>
        [MenuItem("Sankofa/Build Release APK (signed)")]
        public static void BuildRelease()
        {
            string Need(string name)
            {
                var value = System.Environment.GetEnvironmentVariable(name);
                if (string.IsNullOrEmpty(value)) throw new BuildFailedException($"[Sankofa] Release build needs {name}.");
                return value;
            }
            var keystore = Need("SANKOFA_KEYSTORE");
            if (!File.Exists(keystore)) throw new BuildFailedException("[Sankofa] SANKOFA_KEYSTORE does not point to a file.");
            var storePass = Need("SANKOFA_KEYSTORE_PASS");
            var alias = Need("SANKOFA_KEY_ALIAS");
            var keyPass = System.Environment.GetEnvironmentVariable("SANKOFA_KEY_PASS");
            if (string.IsNullOrEmpty(keyPass)) keyPass = storePass;
            if (!int.TryParse(Need("SANKOFA_VERSION_CODE"), out var versionCode) || versionCode < 1)
                throw new BuildFailedException("[Sankofa] SANKOFA_VERSION_CODE must be a positive integer.");
            var repository = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            var commit = Commit(repository);
            if (commit == null || commit.EndsWith("-dirty"))
                throw new BuildFailedException("[Sankofa] Release builds come from a clean, committed working tree.");

            var previousCode = PlayerSettings.Android.bundleVersionCode;
            var previousCustom = PlayerSettings.Android.useCustomKeystore;
            var previousName = PlayerSettings.Android.keystoreName;
            var previousAlias = PlayerSettings.Android.keyaliasName;
            try
            {
                PlayerSettings.Android.bundleVersionCode = versionCode;
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystore;
                PlayerSettings.Android.keystorePass = storePass;
                PlayerSettings.Android.keyaliasName = alias;
                PlayerSettings.Android.keyaliasPass = keyPass;
                Debug.Log($"[Sankofa] Release build: version code {versionCode}, source {commit}.");
                Build(SankofaSceneBuilder.StoryTableScene, ReleaseOutputName);
            }
            finally
            {
                // Leave the project exactly as committed: debug signing, the original code and no secrets.
                PlayerSettings.Android.bundleVersionCode = previousCode;
                PlayerSettings.Android.useCustomKeystore = previousCustom;
                PlayerSettings.Android.keystoreName = previousName;
                PlayerSettings.Android.keyaliasName = previousAlias;
                PlayerSettings.Android.keystorePass = "";
                PlayerSettings.Android.keyaliasPass = "";
                AssetDatabase.SaveAssets();
            }
        }

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
            // A previous interrupted build may have left its metadata on disk. Never append a second identity.
            PlayerSettings.bundleVersion = commit != null ? $"{version.Split('+')[0]}+{commit}" : version;
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
                AssetDatabase.SaveAssets();
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
