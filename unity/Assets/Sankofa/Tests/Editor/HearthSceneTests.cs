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
            components.AddRange(Object.FindObjectsByType<FigureMotion>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            components.AddRange(Object.FindObjectsByType<Runtime.Audio.ObjectSound>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            var pointers = Object.FindObjectsByType<PointerSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(pointers.Length, Is.EqualTo(2), "a pointer for each side");
            components.AddRange(pointers);
            components.Add(Single<Runtime.Audio.NightAmbience>());
            Assert.That(components.SelectMany(MissingReferences), Is.Empty);
            // Panels may use their font's own material.
            var panels = Object.FindObjectsByType<PokePanel>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.That(panels.Length, Is.EqualTo(3), "controls, questions and settings");
            foreach (var panel in panels)
                Assert.That(MissingReferences(panel).Where(m => !m.EndsWith(".labelMaterial")), Is.Empty);
            Assert.That(Object.FindObjectsByType<HandSource>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length, Is.EqualTo(2));

            // A glow, touch point and place point are optional for staged objects; their surfaces are not.
            var optional = new[] { ".glow", ".touchPoint", ".placePoint" };
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
            var places = new[] { "Controls panel", "Questions panel", "Settings panel", "Rest point", "Keepsake point", "Carry point" };
            foreach (var name in places)
                WithinReach(anchor.GetComponentsInChildren<Transform>(true).Single(t => t.name == name).localPosition, name);
            // Three rows of controls (the returning visitor's choices) still sit above the lap.
            var controls = anchor.GetComponentsInChildren<Transform>(true).Single(t => t.name == "Controls panel").localPosition;
            WithinReach(controls + Vector3.down * 2 * (0.05f + 0.012f), "the third row of controls");

            foreach (var o in Object.FindObjectsByType<StageObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var touch = (Transform)new SerializedObject(o).FindProperty("touchPoint").objectReferenceValue;
                if (touch == null) continue;
                var local = anchor.InverseTransformPoint(touch.position);
                WithinReach(local, o.Id + " touch point");
                // Reaching for a story object never passes through the controls, which are on the visitor's left.
                Assert.That(local.x, Is.GreaterThan(-0.05f), o.Id + " is reached on the right");
                Assert.That(Vector3.Distance(local, controls), Is.GreaterThan(0.25f), o.Id + " is clear of the controls");
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
                    Assert.That(new Vector2(local.x, local.z).magnitude, Is.GreaterThan(0.75f), o.Id + " approach " + i + " keeps a body's distance from the seat");
                    Assert.That(Vector2.Distance(new Vector2(local.x, local.z), new Vector2(controls.x, controls.z)), Is.GreaterThan(0.5f), o.Id + " approach " + i + " is clear of the controls");
                }
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
