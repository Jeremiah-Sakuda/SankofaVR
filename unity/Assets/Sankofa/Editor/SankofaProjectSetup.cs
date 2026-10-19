using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEditor.XR.OpenXR.Features;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;
using UnityEngine.XR.OpenXR;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Configures the project for a seated, hands-first, full-VR Quest build. Reproducible from the command line:
    /// Unity -batchmode -quit -projectPath unity -buildTarget Android -executeMethod Sankofa.EditorTools.SankofaProjectSetup.Configure
    /// Meta's Project Setup Tool fixes run last so its current requirements take precedence.
    /// Adapted from the previous Nafasi project's setup tool (see docs/workspace-provenance.md).
    /// </summary>
    public static class SankofaProjectSetup
    {
        /// <summary>Provisional. A store release channel fixes the identifier permanently; confirm it before the first upload.</summary>
        public const string ApplicationId = "com.sankofa.quest";
        public const string ProductName = "Sankofa";
        public const string Version = "0.1.0";

        static readonly string[] AndroidOpenXRFeatures =
        {
            "com.unity.openxr.feature.metaquest",
            "com.meta.openxr.feature.metaxr",
            "com.unity.openxr.feature.input.handinteraction",
            "com.unity.openxr.feature.input.oculustouch",
        };

        static readonly string[] StandaloneOpenXRFeatures =
        {
            "com.meta.openxr.feature.metaxr",
            "com.unity.openxr.feature.input.handinteraction",
            "com.unity.openxr.feature.input.oculustouch",
        };

        /// <summary>
        /// Hands reach Sankofa through Meta's OVRPlugin (Meta XR feature). Unity's Microsoft Hand Interaction profile
        /// shares an id with XR Hands' feature but is not a Quest profile, so it stays off.
        /// </summary>
        // SpaceWarp needs motion vectors, which the built-in pipeline does not render; enabled, it would cost without helping.
        static readonly string[] DisabledFeatureTypes = { "MicrosoftHandInteraction", "SpaceWarpFeature", "MetaXRSpaceWarp" };

        [MenuItem("Sankofa/Configure Project for Quest")]
        public static void Configure()
        {
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);
            ConfigurePlayer();
            ConfigureAudio();
            ConfigureXR();
            ConfigureMetaProjectConfig();
            ConfigureQuality();
            RunMetaProjectSetupFixes();
            // After Meta's fixes, which turn SpaceWarp back on.
            DisableFeatures();
            EnsureStoryServicePermission();
            SankofaText.EnsureEssentials();
            AssetDatabase.SaveAssets();
            Debug.Log("[Sankofa] Project configured for Quest (Android, OpenXR, hands first, optional room fitting).");
        }

        static void ConfigurePlayer()
        {
            PlayerSettings.companyName = ProductName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.bundleVersion = Version;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            // No engine logo or splash: the competition rules bar corporate logos, and the hearth is the opening.
            PlayerSettings.SplashScreen.show = false;
            // Frame timings for the "[Sankofa] perf" log, so a headset run measures itself.
            PlayerSettings.enableFrameTimingStats = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.Android.bundleVersionCode = Math.Max(1, PlayerSettings.Android.bundleVersionCode);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel32;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.Vulkan });
            PlayerSettings.colorSpace = ColorSpace.Linear;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.Android.forceInternetPermission = false;
            EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ASTC;

            // Input System only: "Both" is unsupported on Android. Hands come through OVRPlugin.
            var player = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset").FirstOrDefault();
            if (player != null)
            {
                var so = new SerializedObject(player);
                var input = so.FindProperty("activeInputHandler");
                if (input != null)
                {
                    input.intValue = 1;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
        }

        /// <summary>48 kHz with a moderate DSP buffer: narration and ambience need stability more than tap latency.</summary>
        static void ConfigureAudio()
        {
            var manager = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/AudioManager.asset").FirstOrDefault();
            if (manager == null) return;
            var so = new SerializedObject(manager);
            Set(so, "m_RequestedDSPBufferSize", 512);
            Set(so, "m_DSPBufferSize", 512);
            Set(so, "m_SampleRate", 48000);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Set(SerializedObject so, string property, int value)
        {
            var p = so.FindProperty(property);
            if (p != null) p.intValue = value;
            else Debug.LogWarning($"[Sankofa] AudioManager has no {property}");
        }

        static void ConfigureXR()
        {
            if (!EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.settingsKey, out XRGeneralSettingsPerBuildTarget perTarget) || perTarget == null)
            {
                perTarget = ScriptableObject.CreateInstance<XRGeneralSettingsPerBuildTarget>();
                if (!AssetDatabase.IsValidFolder("Assets/XR")) AssetDatabase.CreateFolder("Assets", "XR");
                AssetDatabase.CreateAsset(perTarget, "Assets/XR/XRGeneralSettingsPerBuildTarget.asset");
                EditorBuildSettings.AddConfigObject(XRGeneralSettings.settingsKey, perTarget, true);
            }

            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                if (!perTarget.HasSettingsForBuildTarget(group)) perTarget.CreateDefaultSettingsForBuildTarget(group);
                if (!perTarget.HasManagerSettingsForBuildTarget(group)) perTarget.CreateDefaultManagerSettingsForBuildTarget(group);
                var settings = perTarget.SettingsForBuildTarget(group);
                settings.InitManagerOnStart = true;
                XRPackageMetadataStore.AssignLoader(settings.Manager, "UnityEngine.XR.OpenXR.OpenXRLoader", group);
                EditorUtility.SetDirty(settings);
            }
            EditorUtility.SetDirty(perTarget);

            EnableFeatures(BuildTargetGroup.Android, AndroidOpenXRFeatures);
            // Play mode uses the desktop settings; the Meta XR Simulator is an OpenXR runtime there.
            EnableFeatures(BuildTargetGroup.Standalone, StandaloneOpenXRFeatures);


            var android = OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            if (android != null)
            {
                // Single-pass instanced (multiview on Quest). Every Sankofa shader carries the stereo macros it needs.
                android.renderMode = OpenXRSettings.RenderMode.SinglePassInstanced;
                // Story pacing is not tap-latency bound: favour a stable frame over late input polling.
                android.latencyOptimization = OpenXRSettings.LatencyOptimization.PrioritizeRendering;
                EditorUtility.SetDirty(android);
            }
        }

        static void DisableFeatures()
        {
            foreach (var group in new[] { BuildTargetGroup.Android, BuildTargetGroup.Standalone })
            {
                var settings = OpenXRSettings.GetSettingsForBuildTargetGroup(group);
                if (settings == null) continue;
                foreach (var feature in settings.GetFeatures())
                    if (Array.IndexOf(DisabledFeatureTypes, feature.GetType().Name) >= 0 && feature.enabled)
                    {
                        feature.enabled = false;
                        EditorUtility.SetDirty(feature);
                    }
            }
        }

        /// <summary>Feature types that stay off on every platform.</summary>
        public static System.Collections.Generic.IReadOnlyList<string> DisabledFeatures => DisabledFeatureTypes;

        static void EnableFeatures(BuildTargetGroup group, string[] ids)
        {
            FeatureHelpers.RefreshFeatures(group);
            foreach (var id in ids)
            {
                var feature = FeatureHelpers.GetFeatureWithIdForBuildTarget(group, id);
                if (feature == null)
                {
                    Debug.LogWarning($"[Sankofa] {group} OpenXR feature not found: {id}");
                    continue;
                }
                feature.enabled = true;
                EditorUtility.SetDirty(feature);
            }
        }

        /// <summary>
        /// Android's default quality level renders with 4x MSAA, so thin flames, grass and type stay smooth in the
        /// headset rather than depending on a runtime override.
        /// </summary>
        static void ConfigureQuality()
        {
            var asset = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/QualitySettings.asset")[0];
            var so = new SerializedObject(asset);
            var levels = so.FindProperty("m_QualitySettings");
            var perPlatform = so.FindProperty("m_PerPlatformDefaultQuality");
            var android = -1;
            for (var i = 0; i < perPlatform.arraySize; i++)
            {
                var entry = perPlatform.GetArrayElementAtIndex(i);
                if (entry.FindPropertyRelative("first").stringValue == "Android") android = entry.FindPropertyRelative("second").intValue;
            }
            if (android < 0 || android >= levels.arraySize)
            {
                Debug.LogWarning("[Sankofa] No Android default quality level found; MSAA not set.");
                return;
            }
            levels.GetArrayElementAtIndex(android).FindPropertyRelative("antiAliasing").intValue = 4;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void ConfigureMetaProjectConfig()
        {
            var config = AssetDatabase.LoadAssetAtPath<OVRProjectConfig>("Assets/Oculus/OculusProjectConfig.asset") ?? OVRProjectConfig.CachedProjectConfig;
            if (config == null)
            {
                Debug.LogWarning("[Sankofa] OVRProjectConfig not available yet; rerun Configure after import.");
                return;
            }
            // Hands first, and every step works with them alone; Touch controllers also work for those who pick them up.
            config.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersAndHands;
            // Pokes, pinches and the open palm are the whole interface: track hands at the higher rate.
            config.handTrackingFrequency = OVRProjectConfig.HandTrackingFrequency.HIGH;
            // Manual stand remains complete; room use is optional and permission follows an explicit action.
            config.insightPassthroughSupport = OVRProjectConfig.FeatureSupport.Supported;
            config.sceneSupport = OVRProjectConfig.FeatureSupport.Supported;
            OVRProjectConfig.CommitProjectConfig(config);
        }

        const string ManifestPath = "Assets/Plugins/Android/AndroidManifest.xml";

        /// <summary>
        /// Unity adds INTERNET for the optional story service. Remove the old offline-only override on upgrade.
        /// </summary>
        static void EnsureStoryServicePermission()
        {
            if (!System.IO.File.Exists(ManifestPath))
            {
                Debug.LogWarning("[Sankofa] No project Android manifest yet; rerun Configure after Meta's setup creates it.");
                return;
            }
            var text = System.IO.File.ReadAllText(ManifestPath);
            var updated = PermitStoryNetworking(text);
            if (updated == text) return;
            System.IO.File.WriteAllText(ManifestPath, updated);
            AssetDatabase.ImportAsset(ManifestPath);
        }

        public static string PermitStoryNetworking(string manifest)
        {
            var document = new System.Xml.XmlDocument { PreserveWhitespace = true, XmlResolver = null };
            document.LoadXml(manifest);
            var removals = document.GetElementsByTagName("uses-permission").Cast<System.Xml.XmlElement>()
                .Where(node => node.GetAttribute("name", "http://schemas.android.com/apk/res/android") == "android.permission.INTERNET"
                    && node.GetAttribute("node", "http://schemas.android.com/tools") == "remove").ToArray();
            if (removals.Length == 0) return manifest;
            foreach (var node in removals) node.ParentNode.RemoveChild(node);
            return document.OuterXml;
        }

        [MenuItem("Sankofa/Configure Story Table Capabilities")]
        public static void ConfigureStoryTableCapabilities()
        {
            ConfigureMetaProjectConfig();
            EnsureStoryServicePermission();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Applies Meta's Project Setup Tool fixes (blocking), via its internal API.</summary>
        static void RunMetaProjectSetupFixes()
        {
            var setup = typeof(OVRProjectConfig).Assembly.GetType("OVRProjectSetup");
            var fix = setup?.GetMethod("FixTasks", BindingFlags.Static | BindingFlags.NonPublic);
            var logEnum = setup?.GetNestedType("LogMessages", BindingFlags.Public | BindingFlags.NonPublic);
            if (fix == null || logEnum == null)
            {
                Debug.LogWarning("[Sankofa] Meta Project Setup Tool API not found; run it from Edit > Project Settings > Meta XR.");
                return;
            }
            try
            {
                var args = new object[] { BuildTargetGroup.Android, null, Enum.Parse(logEnum, "Changed"), true, null, false };
                fix.Invoke(null, args);
            }
            catch (TargetInvocationException e)
            {
                Debug.LogWarning("[Sankofa] Meta Project Setup Tool fixes failed: " + e.InnerException?.Message);
            }
        }
    }
}
