using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Story;
using Sankofa.EditorTools;
using Sankofa.Flow;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.Stage;
using Sankofa.Runtime.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sankofa.Tests
{
    /// <summary>The generated scene is fully wired and agrees with the bundled story package it plays.</summary>
    public class HearthSceneTests
    {
        StoryPackage package;

        [OneTimeSetUp]
        public void OpenScene()
        {
            EditorSceneManager.OpenScene(SankofaSceneBuilder.HearthScene, OpenSceneMode.Single);
            package = StoryReader.ReadPackage(AssetDatabase.LoadAssetAtPath<TextAsset>(SankofaSceneBuilder.PackagePath).text);
        }

        static T Single<T>() where T : Object
        {
            var found = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(found.Length, Is.EqualTo(1), typeof(T).Name);
            return found[0];
        }

        static IEnumerable<string> MissingReferences(Object target)
        {
            var so = new SerializedObject(target);
            var p = so.GetIterator();
            while (p.NextVisible(true))
                if (p.propertyType == SerializedPropertyType.ObjectReference && p.objectReferenceValue == null && p.name != "m_Script")
                    yield return $"{target.GetType().Name}.{p.propertyPath}";
        }

        [Test]
        public void EverySankofaComponentIsWired()
        {
            var components = new List<Object>
            {
                Single<SankofaApp>(), Single<StoryStage>(), Single<StoryTransition>(), Single<PalmReceiver>(), Single<TouchReceiver>(),
                Single<AttentionSource>(), Single<WorldAnchor>(),
            };
            components.AddRange(Object.FindObjectsByType<HandSource>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            components.AddRange(Object.FindObjectsByType<Runtime.Fire.HearthFire>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            components.AddRange(Object.FindObjectsByType<Runtime.Narration.NarrationPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            components.Add(Single<CaptionBoard>());
            components.Add(Single<StartScreen>());


            var pointers = Object.FindObjectsByType<PointerSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(pointers.Length, Is.EqualTo(2), "a pointer for each side");
            components.AddRange(pointers);
            components.Add(Single<Runtime.Audio.NightAmbience>());
            Assert.That(components.SelectMany(MissingReferences), Is.Empty);
            // People have an animated skinned body and a head bone to glance with; the ewe is posed by the figure
            // shader, so has neither.
            foreach (var figure in Object.FindObjectsByType<FigureMotion>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var isPerson = figure.GetComponent<StageObject>().Id != "figure.ewe";
                Assert.That(MissingReferences(figure).Where(m => isPerson || !(m.EndsWith(".animator") || m.EndsWith(".lookBone"))), Is.Empty, figure.name);
                if (!isPerson) continue;
                Assert.That(figure.GetComponentInChildren<SkinnedMeshRenderer>(), Is.Not.Null, figure.name + " has a skinned body");
                // A person's head stands at a person's height, not a giant's or a child's.
                var height = new SerializedObject(figure).FindProperty("height").floatValue;
                var head = figure.GetComponentsInChildren<Transform>(true).First(t => t.name == "Head");
                Assert.That(head.position.y - figure.transform.position.y, Is.InRange(height * 0.8f, height * 0.97f), figure.name + " head height");
            }
            // The ewe walks on four legs and lowers her head to graze.
            var gait = Object.FindObjectsByType<QuadrupedGait>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(gait.Length, Is.EqualTo(1), "the ewe's gait");
            Assert.That(MissingReferences(gait[0]), Is.Empty);
            Assert.That(new SerializedObject(gait[0]).FindProperty("legs").arraySize, Is.EqualTo(4));
            // Cars on the far road today: lights and a road to drive.
            var traffic = Single<Traffic>();
            Assert.That(MissingReferences(traffic), Is.Empty);
            Assert.That(new SerializedObject(traffic).FindProperty("road").arraySize, Is.GreaterThan(1));
            Assert.That(new SerializedObject(traffic).FindProperty("lights").arraySize, Is.GreaterThan(0));
            // An occasional sound without a subject (hammering along the line) plays whenever its scene is shown.
            foreach (var sound in Object.FindObjectsByType<Runtime.Audio.ObjectSound>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                Assert.That(MissingReferences(sound).Where(m => !m.EndsWith(".subject")), Is.Empty);
            // Panels may use their font's own material.
            var panels = Object.FindObjectsByType<PokePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(panels.Length, Is.EqualTo(4), "controls, choices, questions and settings");
            foreach (var panel in panels)
                Assert.That(MissingReferences(panel).Where(m => !m.EndsWith(".labelMaterial")), Is.Empty);
            Assert.That(Object.FindObjectsByType<HandSource>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(2));

            // A glow, touch point and place point are optional for staged objects; their surfaces are not.
            var optional = new[] { ".glow", ".touchPoint", ".placePoint", ".holder", ".handOver" };
            foreach (var o in Object.FindObjectsByType<StageObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Assert.That(MissingReferences(o).Where(m => !optional.Any(m.EndsWith)), Is.Empty, o.Id);
                Assert.That(new SerializedObject(o).FindProperty("surfaces").arraySize, Is.GreaterThan(0), o.Id);
            }
        }

        [Test]
        public void TheStageHoldsExactlyWhatTheBundledPackageCanUse()
        {
            var staged = Object.FindObjectsByType<StageObject>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(staged.Select(o => o.Id).OrderBy(id => id), Is.EqualTo(package.Objects.Select(o => o.Id).OrderBy(id => id)),
                "every object the story can act on is staged, and nothing else");
            foreach (var o in package.Objects)
                Assert.That(staged.Single(s => s.Id == o.Id).Scene, Is.EqualTo(o.Scene), o.Id);

            var stage = Single<StoryStage>();
            Assert.That(stage.Scenes.Select(s => s.Id).OrderBy(id => id), Is.EqualTo(package.Scenes.Select(s => s.Id).OrderBy(id => id)));
            foreach (var s in package.Scenes)
                Assert.That(stage.Scenes.Single(x => x.Id == s.Id).IsHearth, Is.EqualTo(s.Kind == SceneKind.Hearth), s.Id);
            var playable = PackageValidator.Accept(package, out var report, stage);
            Assert.That(playable, Is.Not.Null, "every route plays on this stage: " + report);

            // A touch invitation needs somewhere to touch; a placement needs somewhere to set things down.
            foreach (var o in package.Objects.Where(o => o.Capabilities.Contains(ActionKind.Touch)))
                Assert.That(new SerializedObject(staged.Single(s => s.Id == o.Id)).FindProperty("touchPoint").objectReferenceValue, Is.Not.Null, o.Id);
            var targets = package.Beats.SelectMany(b => b.Lines.Concat(b.Invitation?.Accepted ?? new Line[0]).Concat(b.Invitation?.Default ?? new Line[0]))
                .SelectMany(l => l.Actions).Where(a => a.Kind == ActionKind.Place && a.At != null).Select(a => a.At).Distinct();
            foreach (var id in targets)
                Assert.That(new SerializedObject(staged.Single(s => s.Id == id)).FindProperty("placePoint").objectReferenceValue, Is.Not.Null, id);
        }

        static void WithinReach(Vector3 local, string name)
        {
            var horizontal = new Vector2(local.x, local.z).magnitude;
            Assert.That(horizontal, Is.LessThanOrEqualTo(0.61f), name + " within about two feet of the seat");
            Assert.That(local.y, Is.InRange(0.5f, 1.1f), name + " between lap and chest height");
        }

        [Test]
        public void ControlsRestingPlacesAndTouchTargetsAreWithinSeatedReach()
        {
            var anchor = Single<WorldAnchor>().transform;
            var places = new[] { "Controls panel", "Choices panel", "Questions panel", "Settings panel", "Rest point", "Keepsake point", "Carry point" };
            foreach (var name in places)
                WithinReach(anchor.GetComponentsInChildren<Transform>(true).Single(t => t.name == name).localPosition, name);
            // Three rows of choices (the returning visitor's menu) still sit within reach.
            var controls = anchor.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Controls panel").localPosition;
            var choices = anchor.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Choices panel").localPosition;
            WithinReach(choices + Vector3.down * 2 * (0.05f + 0.012f), "the third row of choices");

            foreach (var o in Object.FindObjectsByType<StageObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var touch = (Transform)new SerializedObject(o).FindProperty("touchPoint").objectReferenceValue;
                if (touch == null) continue;
                var local = anchor.InverseTransformPoint(touch.position);
                WithinReach(local, o.Id + " touch point");
                // Reaching for a story object never passes through the controls, which are on the visitor's left.
                Assert.That(local.x, Is.GreaterThan(-0.05f), o.Id + " is reached on the right");
                Assert.That(Vector3.Distance(local, controls), Is.GreaterThan(0.25f), o.Id + " is clear of the controls");
                Assert.That(Vector3.Distance(local, choices), Is.GreaterThan(0.25f), o.Id + " is clear of the choices");
            }
            var so = new SerializedObject(Single<StoryStage>());
            Assert.That(so.FindProperty("offerForward").floatValue, Is.InRange(0.3f, 0.55f), "objects are offered within reach");
        }

        [Test]
        public void FiguresWalkInFromBeyondTheSceneAndKeepTheirDistance()
        {
            var anchor = Single<WorldAnchor>().transform;
            var controls = anchor.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Controls panel").localPosition;
            var figures = Object.FindObjectsByType<FigureMotion>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(figures.Select(f => f.GetComponent<StageObject>().Id), Is.SupersetOf(new[] { "figure.wambui", "figure.naserian", "figure.ewe", "figure.njeri", "figure.naisula" }));
            foreach (var f in figures)
            {
                var path = new SerializedObject(f).FindProperty("entryPath");
                Assert.That(path.arraySize, Is.GreaterThanOrEqualTo(2), f.name);
                var start = path.GetArrayElementAtIndex(0).vector3Value;
                Assert.That(new Vector2(start.x, start.z).magnitude, Is.GreaterThan(3f), f.name + " comes from beyond the clearing");
            }
            // Where people go to stand or kneel stays out of the visitor's lap and clear of the controls.
            foreach (var o in Object.FindObjectsByType<StageObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var points = new SerializedObject(o).FindProperty("approachPoints");
                for (var i = 0; i < points.arraySize; i++)
                {
                    var local = anchor.InverseTransformPoint(((Transform)points.GetArrayElementAtIndex(i).objectReferenceValue).position);
                    // Personal space: a person standing or kneeling stays over a metre from the seated visitor.
                    Assert.That(new Vector2(local.x, local.z).magnitude, Is.GreaterThan(1.1f), o.Id + " approach " + i + " keeps a body's distance from the seat");
                    Assert.That(Vector2.Distance(new Vector2(local.x, local.z), new Vector2(controls.x, controls.z)), Is.GreaterThan(0.5f), o.Id + " approach " + i + " is clear of the controls");
                }
            }
            // Hand-over places keep the same personal space and stay clear of the controls.
            foreach (var o in Object.FindObjectsByType<StageObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var place = (Transform)new SerializedObject(o).FindProperty("handOver").objectReferenceValue;
                if (place == null) continue;
                var local = anchor.InverseTransformPoint(place.position);
                Assert.That(new Vector2(local.x, local.z).magnitude, Is.GreaterThan(1.1f), o.Id + " is handed over from a body's distance");
                Assert.That(Vector2.Distance(new Vector2(local.x, local.z), new Vector2(controls.x, controls.z)), Is.GreaterThan(0.5f), o.Id + " hand-over is clear of the controls");
                Assert.That(o.Holder, Is.Not.Null, o.Id + " has someone to hand it over");
            }
            // Every figure the package moves has somewhere to go.
            foreach (var action in package.Beats.SelectMany(b => b.Lines.Concat(b.Invitation?.Accepted ?? new List<Line>()).Concat(b.Invitation?.Default ?? new List<Line>()))
                         .SelectMany(l => l.Actions).Where(a => a.Kind == ActionKind.Move))
            {
                Assert.That(figures.Any(f => f.GetComponent<StageObject>().Id == action.Object), action.ToString());
                if (action.At == null) continue;
                var target = Object.FindObjectsByType<StageObject>(FindObjectsInactive.Include, FindObjectsSortMode.None).Single(o => o.Id == action.At);
                Assert.That(new SerializedObject(target).FindProperty("approachPoints").arraySize, Is.GreaterThan(0), action + ": the target has a place to stand or kneel");
            }
        }

        static IEnumerable<AudioClip> Clips(Runtime.Audio.ObjectSound sound)
        {
            var list = new SerializedObject(sound).FindProperty("clips");
            for (var i = 0; i < list.arraySize; i++) yield return list.GetArrayElementAtIndex(i).objectReferenceValue as AudioClip;
        }

        [Test]
        public void EverySoundIsAListedFieldRecordingUnderAnOpenLicence()
        {
            var listed = System.IO.File.ReadAllText(SankofaSceneBuilder.RecordedFolder + "sources.json");
            var clips = Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                // The narrator loads its lines at runtime; an occasional sound's source plays that sound's own clips.
                .Where(s => s.GetComponent<Runtime.Narration.NarrationPlayer>() == null && s.GetComponent<Runtime.Audio.ObjectSound>() == null)
                .Select(s => s.clip)
                .Concat(Object.FindObjectsByType<Runtime.Audio.ObjectSound>(FindObjectsInactive.Include, FindObjectsSortMode.None).SelectMany(Clips))
                .ToList();
            Assert.That(clips, Is.Not.Empty);
            foreach (var clip in clips)
            {
                Assert.That(clip, Is.Not.Null, "a source with nothing to play would be silent, or filled in by code");
                var path = AssetDatabase.GetAssetPath(clip);
                Assert.That(path, Does.StartWith(SankofaSceneBuilder.RecordedFolder), clip.name + " is a prepared field recording");
                Assert.That(listed, Does.Contain("\"file\": \"" + System.IO.Path.GetFileName(path) + "\""), clip.name + " is listed with its source");
            }
            foreach (var licence in System.Text.RegularExpressions.Regex.Matches(listed, "\"licence\": \"([^\"]+)\"").Cast<System.Text.RegularExpressions.Match>())
                Assert.That(new[] { "CC0 1.0", "CC BY 3.0", "CC BY 4.0" }, Has.Member(licence.Groups[1].Value));
            // Recordings that ask for credit are credited in the app.
            var credits = System.IO.File.ReadAllText("Assets/Sankofa/Content/credits.txt");
            foreach (var author in new[] { "matucha", "SterckxS", "tim.kahn", "celldroid", "visualasylum" })
                Assert.That(credits, Does.Contain(author));
            Assert.That(new SerializedObject(Single<SankofaApp>()).FindProperty("credits").objectReferenceValue, Is.Not.Null, "the settings can show the credits");
        }

        [Test]
        public void HandsDrawOverTheTransitionCurtain()
        {
            var curtain = (Renderer)new SerializedObject(Single<StoryTransition>()).FindProperty("curtain").objectReferenceValue;
            Assert.That(curtain.sharedMaterial.renderQueue, Is.LessThan(HandSource.HandRenderQueue));
            Assert.That(curtain.enabled, Is.False, "the curtain is hidden until a transition");
        }

        [Test]
        public void TheEngineFreeCoreRunsUnderUnity()
        {
            var playable = PackageValidator.Accept(package, out var report, Single<StoryStage>());
            Assert.That(playable, Is.Not.Null, report.ToString());
            var flow = new VisitFlow(playable);
            flow.Start(skipWelcome: true);
            Assert.That(flow.Choose("era.1850s"), Is.True);
            var cues = flow.DrainCues();
            Assert.That(cues.OfType<Cue.Speak>().Last().Line.Id, Is.EqualTo("e1.gather"));
        }

        // Imported sound must keep its true length: a clip resampled at import but played or mixed at the wrong rate
        // sounds sped up. Compared with each source file's own header (WAV) or with the decoded length (narration).
        [Test]
        public void RecordingsAndNarrationKeepTheirDuration()
        {
            foreach (var path in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Sankofa/Art/Recorded" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                var bytes = System.IO.File.ReadAllBytes(path);
                var rate = System.BitConverter.ToInt32(bytes, 24);
                var blockAlign = System.BitConverter.ToInt16(bytes, 32);
                var dataStart = 12;
                while (System.Text.Encoding.ASCII.GetString(bytes, dataStart, 4) != "data") dataStart += 8 + System.BitConverter.ToInt32(bytes, dataStart + 4);
                var seconds = System.BitConverter.ToInt32(bytes, dataStart + 4) / (float)(blockAlign * rate);
                TestContext.WriteLine($"{path}: source {seconds:F2} s at {rate} Hz; clip {clip.length:F2} s, {clip.samples} samples at {clip.frequency} Hz");
                Assert.That(clip.length, Is.EqualTo(seconds).Within(0.05f), path);
                Assert.That(clip.samples / (float)clip.frequency, Is.EqualTo(clip.length).Within(0.05f), path + " samples at its frequency");
            }
            foreach (var path in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Sankofa/Resources/Narration" }).Select(AssetDatabase.GUIDToAssetPath))
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                Assert.That(clip.samples / (float)clip.frequency, Is.EqualTo(clip.length).Within(0.05f), path);
                Assert.That(clip.length, Is.InRange(0.5f, 60f), path);
            }
        }

        [Test]
        public void TheBuildShowsNoEngineSplashLogoOrIcon()
        {
            Assert.That(PlayerSettings.SplashScreen.show, Is.False, "no splash screen");
            Assert.That(PlayerSettings.SplashScreen.showUnityLogo, Is.False, "no engine logo");
            // The launcher shows our own icon, not the engine's default.
            var icons = PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, IconKind.Any);
            Assert.That(icons, Is.Not.Empty, "an application icon is set");
            Assert.That(AssetDatabase.GetAssetPath(icons[0]), Is.EqualTo(SankofaIcon.IconPath), "the project's own icon");
        }

        [Test]
        public void QuestRenderingSettingsHold()
        {
            var android = UnityEngine.XR.OpenXR.OpenXRSettings.GetSettingsForBuildTargetGroup(BuildTargetGroup.Android);
            Assert.That(android, Is.Not.Null, "OpenXR settings for Android");
            Assert.That(android.renderMode, Is.EqualTo(UnityEngine.XR.OpenXR.OpenXRSettings.RenderMode.SinglePassInstanced), "multiview");
            foreach (var feature in android.GetFeatures())
                if (SankofaProjectSetup.DisabledFeatures.Contains(feature.GetType().Name))
                    Assert.That(feature.enabled, Is.False, feature.GetType().Name + " stays off: the built-in pipeline renders no motion vectors");
            Assert.That(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android), Is.EqualTo(new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan }));
        }

        // Our own geometry budget for one scene on standalone Quest, until a device profile replaces it: the triangles
        // and renderers (a proxy for draw calls) of each scene's root, which is all that shows besides the panels.
        const int TriangleBudget = 300000;
        const int RendererBudget = 175;

        [Test]
        public void EachSceneStaysWithinItsGeometryBudget()
        {
            foreach (var scene in Single<StoryStage>().Scenes)
            {
                var renderers = scene.Root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToList();
                long triangles = 0;
                foreach (var r in renderers)
                {
                    var mesh = r is SkinnedMeshRenderer skinned ? skinned.sharedMesh : r.TryGetComponent<MeshFilter>(out var filter) ? filter.sharedMesh : null;
                    if (mesh == null) continue;
                    // Each material is a pass over the mesh: a figure's depth and light both count.
                    long meshTriangles = 0;
                    for (var sub = 0; sub < mesh.subMeshCount; sub++) meshTriangles += mesh.GetIndexCount(sub) / 3;
                    triangles += meshTriangles * Mathf.Max(1, r.sharedMaterials.Length - mesh.subMeshCount + 1);
                }
                TestContext.WriteLine($"{scene.Id}: {triangles} triangles, {renderers.Count} renderers");
                Assert.That(triangles, Is.LessThanOrEqualTo(TriangleBudget), scene.Id + " triangles");
                Assert.That(renderers.Count, Is.LessThanOrEqualTo(RendererBudget), scene.Id + " renderers");
            }
        }
    }

    public class BuildGuardTests
    {
        [Test]
        public void CredentialsAreRemovedAfterInjectionAndRestoredEvenWhenVerificationFails()
        {
            const string path = "Assets/SankofaPrivacyTest.asset";
            var type = System.AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Meta.XR.ImmersiveDebugger.DevAgent.RuntimeSettings"))
                .First(t => t != null);
            var settings = ScriptableObject.CreateInstance(type);
            var apk = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.Guid.NewGuid().ToString("N") + ".apk");
            try
            {
                AssetDatabase.CreateAsset(settings, path);
                var so = new SerializedObject(settings);
                so.FindProperty("accessToken").stringValue = "original-test-credential";
                so.FindProperty("enabled").boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
                SankofaBuildPrivacy.CaptureEditorSettings();
                // Models Meta's later build callback overwriting the editor value.
                so.FindProperty("accessToken").stringValue = "injected-test-credential";
                so.ApplyModifiedPropertiesWithoutUndo();
                new SankofaBuildPrivacy().OnPreprocessBuild(null);
                so.Update();
                Assert.That(so.FindProperty("accessToken").stringValue, Is.Empty);
                Assert.That(so.FindProperty("enabled").boolValue, Is.False);
                using (var zip = System.IO.Compression.ZipFile.Open(apk, System.IO.Compression.ZipArchiveMode.Create))
                using (var writer = new System.IO.StreamWriter(zip.CreateEntry("assets/settings").Open()))
                    writer.Write("injected-test-credential");
                Assert.Throws<UnityEditor.Build.BuildFailedException>(() => SankofaBuildPrivacy.VerifyApk(apk));
                SankofaBuildPrivacy.RestoreEditorSettings();
                so.Update();
                Assert.That(so.FindProperty("accessToken").stringValue, Is.EqualTo("original-test-credential"));
                Assert.That(so.FindProperty("enabled").boolValue, Is.True);
            }
            finally
            {
                SankofaBuildPrivacy.RestoreEditorSettings();
                AssetDatabase.DeleteAsset(path);
                if (System.IO.File.Exists(apk)) System.IO.File.Delete(apk);
            }
        }

        [Test]
        public void TheHearthSceneAndAlwaysShippedAssetsAvoidLocalOnlyMaterial()
        {
            Assert.That(AssetDatabase.GetDependencies(new[] { SankofaSceneBuilder.HearthScene }, true).Where(p => p.StartsWith(LocalOnlyAssets.Root)), Is.Empty);
            var shipped = LocalOnlyAssets.AlwaysShipped().ToArray();
            Assert.That(shipped, Does.Contain("Assets/Resources/OculusRuntimeSettings.asset"), "Resources assets are covered");
            LocalOnlyAssets.DeleteTestRunLeftovers();
            Assert.DoesNotThrow(() => LocalOnlyAssets.Check(new[] { SankofaSceneBuilder.HearthScene }));
        }

        [Test]
        public void TestRunLeftoversInResourcesAreRecognised()
        {
            Assert.That(LocalOnlyAssets.IsTestRunLeftover("Assets/Resources/PerformanceTestRunInfo.json"), Is.True);
            Assert.That(LocalOnlyAssets.IsTestRunLeftover("Assets/Resources/PerformanceTestRunSettings.json"), Is.True);
            Assert.That(LocalOnlyAssets.IsTestRunLeftover("Assets/Resources/OculusRuntimeSettings.asset"), Is.False);
            Assert.That(LocalOnlyAssets.IsTestRunLeftover("Assets/Sankofa/PerformanceTestRunInfo.json"), Is.False, "outside Resources nothing ships");
        }

        [Test]
        public void EveryPlayerBuildRunsTheGuard()
        {
            Assert.That(TypeCache.GetTypesDerivedFrom<UnityEditor.Build.BuildPlayerProcessor>(), Does.Contain(typeof(LocalOnlyBuildGuard)));
        }

        [Test]
        public void DependenciesAndResourcesFoldersInsideTheLocalFolderAreRejected()
        {
            var problems = LocalOnlyAssets.Violations(
                new[] { "Assets/Sankofa/Art/Generated/Noise.asset", LocalOnlyAssets.Root + "Candidates/scan.fbx" },
                new[] { "Assets/Resources", LocalOnlyAssets.Root + "Recordings", LocalOnlyAssets.Root + "Voice/Resources" }).ToList();
            Assert.That(problems, Is.EqualTo(new[] { LocalOnlyAssets.Root + "Candidates/scan.fbx", LocalOnlyAssets.Root + "Voice/Resources" }));
        }
    }
}
