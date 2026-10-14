using System.IO;
using System.Linq;
using NUnit.Framework;
using Sankofa.Core.Story;
using Sankofa.Runtime.Stage;
using UnityEditor.SceneManagement;
using UnityEngine;
namespace Sankofa.Tests
{
    public class LivingGallerySceneTests
    {
        [Test] public void BothPerspectivesAndTheirContinuationsHaveACompleteStage()
        {
            EditorSceneManager.OpenScene("Assets/Sankofa/Scenes/LivingGallery.unity");
            var stage=Object.FindFirstObjectByType<StoryStage>();
            var p=StoryReader.ReadPackage(File.ReadAllText("Assets/Sankofa/Content/living-gallery.json"));
            Assert.That(PackageValidator.Accept(p,out var report,stage),Is.Not.Null,report.ToString());
            Assert.That(p.Routes.Count(r=>!r.IsContinuation),Is.EqualTo(2));
            Assert.That(stage.Find("memory.portions"),Is.Not.Null);
            Assert.That(Vector3.Distance(stage.Find("gate").TouchPosition,new Vector3(0,1.15f,0)),Is.LessThan(0.65f),"gate interaction stays within seated reach");
            Assert.That(stage.Find("memory.waiting"),Is.Not.Null);
            Assert.That(stage.Find("memory.waiting").transform.Find("Remembered shore"),Is.Not.Null);
            Assert.That(stage.Find("memory.portions").transform.Find("Memory backing").GetComponent<Renderer>().sharedMaterial.HasProperty("_Visibility"),Is.True,"memory backing participates in story reveals");
            Assert.That(stage.Find("gourd.milk").IllustratedHolder,Is.Not.Null);
            Assert.That(stage.HasScene("story.water.1899"),Is.False,"unplayed rigs stay in the comparison scene");
            Assert.That(Object.FindObjectsByType<FigurePresence>(FindObjectsInactive.Include,FindObjectsSortMode.None),Is.Empty);
            var modern=stage.Scenes.Single(s=>s.Id=="story.water.today").Root;
            Assert.That(modern.transform.Find("Illustrated modern river"),Is.Not.Null);
        }
        [Test] public void GalleryUsesAStaticCanonicalImageAndSeatedControls()
        {
            EditorSceneManager.OpenScene("Assets/Sankofa/Scenes/LivingGallery.unity");
            Assert.That(Object.FindFirstObjectByType<LivingGallery>(),Is.Not.Null);
            var image=GameObject.Find("Canonical canvas").GetComponent<Renderer>().sharedMaterial.mainTexture;
            Assert.That(image,Is.Not.Null);Assert.That(image.width,Is.GreaterThanOrEqualTo(1024));
            Assert.That(Object.FindObjectsByType<Camera>(FindObjectsSortMode.None).Any(c=>c.targetTexture!=null),Is.False,"preview does not render a second live scene");
            Assert.That(Vector3.Distance(GameObject.Find("Choices panel").transform.position,new Vector3(0,1.15f,0)),Is.LessThan(0.65f));
        }
    }
}
