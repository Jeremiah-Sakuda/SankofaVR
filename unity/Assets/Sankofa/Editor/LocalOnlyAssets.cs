using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Assets/LocalOnly holds material that may not be distributed (unlicensed candidates, personal input, raw
    /// recordings). No build may depend on it, and it may not contain a Resources folder, which Unity would ship
    /// regardless of references. StreamingAssets is Git-ignored for personal data, so a build refuses anything there.
    /// Adapted from the previous Nafasi project's guard.
    /// </summary>
    public static class LocalOnlyAssets
    {
        public const string Root = "Assets/LocalOnly/";
        public const string StreamingAssets = "Assets/StreamingAssets";

        /// <summary>Files Unity's performance test package leaves in Resources after batch test runs.</summary>
        public static bool IsTestRunLeftover(string path) =>
            path.StartsWith("Assets/Resources/") && Path.GetFileName(path).StartsWith("PerformanceTestRun") && !path.EndsWith(".meta");

        /// <summary>Deletes test-run leftovers so they cannot ship. Returns the deleted paths.</summary>
        public static IReadOnlyList<string> DeleteTestRunLeftovers()
        {
            var leftovers = AssetDatabase.GetAllAssetPaths().Where(IsTestRunLeftover).ToList();
            foreach (var path in leftovers) AssetDatabase.DeleteAsset(path);
            return leftovers;
        }

        public static IEnumerable<string> Violations(IEnumerable<string> dependencies, IEnumerable<string> folders) =>
            dependencies.Where(p => p.StartsWith(Root))
                .Concat(folders.Where(f => f.StartsWith(Root) && Path.GetFileName(f.TrimEnd('/')) == "Resources"))
                .Distinct().OrderBy(p => p);

        /// <summary>Everything a build ships regardless of scene references: Resources folders and preloaded assets.</summary>
        public static IEnumerable<string> AlwaysShipped()
        {
            var resources = AssetDatabase.GetAllAssetPaths()
                .Where(p => p.StartsWith("Assets/") && p.Split('/').Contains("Resources") && !AssetDatabase.IsValidFolder(p));
            var preloaded = UnityEditor.PlayerSettings.GetPreloadedAssets().Where(o => o != null).Select(AssetDatabase.GetAssetPath).Where(p => !string.IsNullOrEmpty(p));
            return resources.Concat(preloaded);
        }

        /// <summary>Throws before a build of <paramref name="scenes"/> that would carry local-only material.</summary>
        public static void Check(string[] scenes)
        {
            var folders = Directory.Exists(Root)
                ? Directory.GetDirectories(Root, "*", SearchOption.AllDirectories).Select(d => d.Replace('\\', '/'))
                : Enumerable.Empty<string>();
            var roots = scenes.Concat(AlwaysShipped()).Distinct().ToArray();
            var problems = Violations(AssetDatabase.GetDependencies(roots, true), folders).ToList();
            problems.AddRange(AlwaysShipped().Where(IsTestRunLeftover).Select(p => p + " (test-run leftover; SankofaBuild deletes these)"));
            if (Directory.Exists(StreamingAssets) && Directory.EnumerateFiles(StreamingAssets, "*", SearchOption.AllDirectories).Any(f => !f.EndsWith(".meta")))
                problems.Add(StreamingAssets + " (ignored local data would be copied into the build verbatim)");
            if (problems.Count > 0)
                throw new BuildFailedException("Build would include local-only or transient material: " + string.Join(", ", problems.Take(5)));
        }
    }

    /// <summary>Runs the local-only check for every player build, whichever window, menu or script starts it.</summary>
    public sealed class LocalOnlyBuildGuard : BuildPlayerProcessor
    {
        public override int callbackOrder => -1000;

        public override void PrepareForBuild(BuildPlayerContext context)
        {
            var scenes = context.BuildPlayerOptions.scenes;
            if (scenes == null || scenes.Length == 0)
                scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            LocalOnlyAssets.Check(scenes);
        }
    }
}
