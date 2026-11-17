using System.Linq;
using NUnit.Framework;
using Sankofa.EditorTools;
using Sankofa.Runtime.Personal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sankofa.Tests
{
    public sealed class RiverCastSceneTests
    {
        [TestCase(false)] [TestCase(true)]
        public void ComparisonHasSharedSceneryAndTheDeclaredCharacterGeometry(bool solid)
        {
            var path=solid ? SankofaSceneBuilder.RiverSolidScene : SankofaSceneBuilder.RiverPaintedScene;
            EditorSceneManager.OpenScene(path); var cast=Object.FindFirstObjectByType<RiverCastPerformance>();
            Assert.That(cast,Is.Not.Null); Assert.That(cast.Solid,Is.EqualTo(solid)); Assert.That(cast.Actors.Length,Is.EqualTo(2));
            foreach(var actor in cast.Actors)
            {
                if(solid)
                {
                    var skin=actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                    Assert.That(skin.Length,Is.GreaterThanOrEqualTo(4)); Assert.That(skin.All(r=>r.bones.Length>0),Is.True);
                    Assert.That(skin.All(r=>r.sharedMaterial.shader.name=="Sankofa/Cohesive Surface"),Is.True);
                    Assert.That(actor.GetComponentsInChildren<MonoBehaviour>(true),Is.Empty,"old Hearth behavior must not advance this app's cast");
                }
                else
                {
                    var m=actor.GetComponent<Renderer>().sharedMaterial; Assert.That(m.shader.name,Is.EqualTo("Sankofa/Cohesive Figure"));
                    Assert.That(new[]{"_MainTex","_Offer","_Reflect"}.All(p=>m.GetTexture(p)!=null),Is.True);
                }
            }
            var meshes=Object.FindObjectsByType<MeshFilter>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            Assert.That(meshes.Single(m=>m.name=="Sculpted painted riverbank").sharedMesh.vertexCount,Is.GreaterThan(1000));
            Assert.That(meshes.Single(m=>m.name=="Curved reeds in physical depth").sharedMesh.bounds.size.z,Is.GreaterThan(5));
            Assert.That(AssetDatabase.GetDependencies(path,true).Any(p=>p.StartsWith("Assets/LocalOnly/")),Is.False);
        }
        [Test]
        public void PaintedRiverPackagesCompleteCreditsAndBothFontNotices()
        {
            EditorSceneManager.OpenScene(SankofaSceneBuilder.RiverPaintedScene);
            var app = new SerializedObject(Object.FindFirstObjectByType<PersonalStoryApp>());
            var credit = app.FindProperty("creditsText").objectReferenceValue as TextAsset;
            Assert.That(credit, Is.Not.Null);
            Assert.That(credit.text, Does.Contain("freesound.org/s/541572").And.Contain("creativecommons.org/licenses/by/3.0"));
            var notices = app.FindProperty("fontNotices"); Assert.That(notices.arraySize, Is.EqualTo(2));
            for (var i = 0; i < notices.arraySize; i++)
            {
                var notice = notices.GetArrayElementAtIndex(i).objectReferenceValue as TextAsset;
                Assert.That(notice, Is.Not.Null);
                Assert.That(notice.text.ToUpperInvariant(), Does.Contain("SIL OPEN FONT LICENSE"));
                Assert.That(AssetDatabase.GetDependencies(SankofaSceneBuilder.RiverPaintedScene, true),
                    Does.Contain(AssetDatabase.GetAssetPath(notice)), "the notice is an actual build dependency");
            }
        }

        [Test]
        public void BaselineHasNoComparisonAdapter()
        {
            foreach(var path in new[]{SankofaSceneBuilder.RiverThresholdScene,SankofaSceneBuilder.StoryTableScene,SankofaSceneBuilder.HearthScene})
            {
                EditorSceneManager.OpenScene(path); Assert.That(Object.FindFirstObjectByType<RiverCastPerformance>(),Is.Null,path);
            }
        }
    }
}
