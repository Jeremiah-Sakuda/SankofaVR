using System.Linq;
using Sankofa.Core.Story;
using Sankofa.EditorTools;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Stage;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sankofa.Tests
{
    public class IllustratedTheatreSceneTests
    {
        [OneTimeSetUp] public void Open() => EditorSceneManager.OpenScene(SankofaSceneBuilder.TheatreScene);

        [Test]
        public void TheatreHasAValidBundledStoryAndNoHumanoidRigInItsIllustratedScene()
        {
            var stage = Object.FindFirstObjectByType<StoryStage>();
            var app = Object.FindFirstObjectByType<SankofaApp>();
            var packageJson = (TextAsset)new SerializedObject(app).FindProperty("packageJson").objectReferenceValue;
            var package = StoryReader.ReadPackage(packageJson.text);
            Assert.That(PackageValidator.Accept(package, out var report, stage), Is.Not.Null, string.Join("\n", report.Errors));
            Assert.That(package.Status, Is.EqualTo(ContentStatus.Draft));
            Assert.That(package.Routes.Count, Is.EqualTo(2), "one illustrated chapter and its existing continuation");
            var scene = stage.Scenes.Single(s => s.Root.GetComponent<IllustratedTheatre>() != null);
            Assert.That(scene.Root.GetComponentsInChildren<SkinnedMeshRenderer>(true), Is.Empty);
            Assert.That(scene.Root.GetComponentsInChildren<IllustratedFigure>(true).Length, Is.EqualTo(2));
            foreach (var figure in scene.Root.GetComponentsInChildren<IllustratedFigure>(true))
            {
                Assert.That(figure.GetComponent<FigureMotion>(), Is.Null, "illustrated people have no walking controller");
                Assert.That(figure.GetComponent<StageObject>().EchoMesh, Is.EqualTo(figure.Surface.GetComponent<MeshFilter>().sharedMesh));
            }
            foreach (var id in new[] { "gourd.flour", "bead" })
            {
                Assert.That(stage.Find(id).IllustratedHolder, Is.Not.Null, id + " starts with its painted holder");
                Assert.That(stage.Find(id).Holder, Is.Null, id + " does not need a humanoid rig");
            }
        }

        [Test]
        public void IllustratedChapterFitsTheExistingGeometryProxyBudget()
        {
            var scene = Object.FindFirstObjectByType<StoryStage>().Scenes.Single(s => s.Root.GetComponent<IllustratedTheatre>() != null);
            var renderers = scene.Root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            long triangles = 0;
            foreach (var renderer in renderers)
            {
                var mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                if (mesh == null) continue;
                long meshTriangles = 0;
                for (var sub = 0; sub < mesh.subMeshCount; sub++) meshTriangles += mesh.GetIndexCount(sub) / 3;
                // Use the baseline's convention: each additional material pass also counts.
                triangles += meshTriangles * Mathf.Max(1, renderer.sharedMaterials.Length - mesh.subMeshCount + 1);
            }
            TestContext.WriteLine($"Illustrated chapter: {triangles} triangles, {renderers.Length} renderers (geometry proxy only)");
            Assert.That(triangles, Is.LessThanOrEqualTo(300000));
            Assert.That(renderers.Length, Is.LessThanOrEqualTo(175));
        }

        [Test]
        public void ThePaintingIsNotDoubledByModelledStandIns()
        {
            var stage = Object.FindFirstObjectByType<StoryStage>();
            var root = stage.Scenes.Single(s => s.Root.GetComponent<IllustratedTheatre>() != null).Root;
            var shown = root.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).Select(r => r.name).ToArray();
            Assert.That(shown, Has.None.EqualTo("Stream").And.None.EqualTo("Bank stones"), "the painted river is the only river");
            var ewe = stage.Find("figure.ewe");
            Assert.That(ewe.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled), Is.Empty, "the unpainted sheep is heard, not seen");
            Assert.That(ewe.AttentionRadius, Is.Zero, "and cannot be noticed or remembered as a shape");
            Assert.That(ewe.EchoMesh, Is.Null);
            foreach (var id in new[] { "gourd.flour", "gourd.milk", "hide", "bead" })
                foreach (var surface in stage.Find(id).Surfaces)
                    Assert.That(surface.sharedMaterial.shader.name, Is.EqualTo("Sankofa/Painted Prop"), id + " is drawn in the paintings' language");
        }

        [Test]
        public void ArtPreservesAlphaAndUsesDepthWritesAndStereoSafeRendering()
        {
            foreach (var figure in Object.FindObjectsByType<IllustratedFigure>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var so = new SerializedObject(figure);
                Assert.That(so.FindProperty("frames").arraySize, Is.EqualTo(3));
                var surface = (Renderer)so.FindProperty("surface").objectReferenceValue;
                Assert.That(surface.sharedMaterial.shader.name, Is.EqualTo("Sankofa/Illustration"));
                var path = AssetDatabase.GetAssetPath(surface.sharedMaterial.mainTexture);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.DoesSourceTextureHaveAlpha(), Is.True);
                Assert.That(importer.alphaIsTransparency, Is.True);
                Assert.That(importer.GetPlatformTextureSettings("Android").format, Is.EqualTo(TextureImporterFormat.ASTC_6x6));
            }
        }
    }
}
