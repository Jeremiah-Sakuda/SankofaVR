using System.Linq;
using NUnit.Framework;
using Sankofa.EditorTools;
using Sankofa.Runtime.Personal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sankofa.Tests
{
    public sealed class RiverThresholdSceneTests
    {
        [Test]
        public void NearbyVesselsHaveDepthAndOpenInteriorsRatherThanFlatPaintings()
        {
            EditorSceneManager.OpenScene(SankofaSceneBuilder.RiverThresholdScene);
            var table=Object.FindFirstObjectByType<StoryTableView>().TableRoot;
            foreach(var name in new[]{"Water pot","Flour gourd"})
            {
                var item=table.Find("Pop-up stage/Heirloom "+name).GetComponentInChildren<MeshFilter>();
                Assert.That(item.sharedMesh.bounds.size.z,Is.GreaterThan(.06f),name+" has physical depth");
                var collider=item.gameObject.AddComponent<MeshCollider>(); collider.sharedMesh=item.sharedMesh;
                Physics.SyncTransforms();
                var ray=new Ray(item.transform.TransformPoint(new Vector3(0,.14f,0)),-item.transform.up);
                Assert.That(collider.Raycast(ray,out var hit,1),Is.True);
                Assert.That(item.transform.InverseTransformPoint(hit.point).y,Is.LessThan(.04f),
                    name+" has an open mouth; a ray down the centre reaches the inside bottom");
                Object.DestroyImmediate(collider);
            }
        }

        [Test]
        public void TheDisclosureRecordingFinishesBeforeTheTitleReplacesIt()
        {
            var line = Sankofa.Core.Personal.PersonalStoryVoice.Welcome[2];
            var clip = Resources.Load<AudioClip>("Narration/personal-table/" + Sankofa.Core.Personal.PersonalStoryVoice.ClipName(line));
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.length, Is.LessThan(RiverThresholdPrototype.TitleAt - RiverThresholdPrototype.DisclosureAt),
                "The integrity disclosure must finish rather than being cut off by the next cinematic beat.");
        }

        [Test]
        public void SavedPrototypeCannotPackageTheUnverifiedLocalMusic()
        {
            EditorSceneManager.OpenScene(SankofaSceneBuilder.RiverThresholdScene);
            var dependencies = AssetDatabase.GetDependencies(SankofaSceneBuilder.RiverThresholdScene, true);
            Assert.That(dependencies.Where(p => p.StartsWith("Assets/LocalOnly/")), Is.Empty);
            var prototype = Object.FindFirstObjectByType<RiverThresholdPrototype>();
            Assert.That(prototype, Is.Not.Null);
            Assert.That(new SerializedObject(prototype).FindProperty("clearedMusic").objectReferenceValue, Is.Null);
            // The baseline still has no cinematic gate; this is a separate comparison scene.
            EditorSceneManager.OpenScene(SankofaSceneBuilder.StoryTableScene);
            Assert.That(Object.FindFirstObjectByType<RiverThresholdPrototype>(), Is.Null);
        }
    }
}
