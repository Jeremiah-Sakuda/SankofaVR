using System.Collections.Generic;
using System.Linq;
using Sankofa.EditorTools;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Stage;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Sankofa.Tests
{
    /// <summary>
    /// Noticing works as a visitor would do it: facing the middle of what they see, a person's body rather than the
    /// ground at her feet, counts as facing it, by the same rule the attention tracker applies.
    /// </summary>
    public class AttentionSceneTests
    {
        static IEnumerable<string> Scenes => new[] { SankofaSceneBuilder.HearthScene, SankofaSceneBuilder.TheatreScene };

        [TestCaseSource(nameof(Scenes))]
        public void FacingTheMiddleOfWhatIsSeenNoticesIt(string path)
        {
            EditorSceneManager.OpenScene(path);
            var stage = Object.FindFirstObjectByType<StoryStage>();
            foreach (var scene in stage.Scenes) scene.Root.SetActive(true);
            var eye = Object.FindFirstObjectByType<WorldAnchor>().transform.TransformPoint(SankofaSceneBuilder.SeatedEye);
            var problems = new List<string>();
            foreach (var o in Object.FindObjectsByType<StageObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (o.AttentionRadius <= 0) continue;
                var seen = o.Surfaces.Where(r => r != null && r.enabled).Select(r => r.bounds).ToList();
                if (seen.Count == 0) continue;
                var bounds = seen[0];
                foreach (var b in seen.Skip(1)) bounds.Encapsulate(b);
                var toSeen = bounds.center - eye;
                var toCandidate = o.AttentionCentre - eye;
                var allowed = Mathf.Atan2(o.AttentionRadius, toCandidate.magnitude) * Mathf.Rad2Deg + 7f;
                var angle = Vector3.Angle(toSeen, toCandidate);
                TestContext.WriteLine($"{o.Id}: {angle:0.0} degrees from what is seen, {allowed:0.0} allowed");
                if (angle > allowed) problems.Add($"{o.Id} ({angle:0.0} > {allowed:0.0})");
            }
            Assert.That(problems, Is.Empty, "facing the middle of these does not notice them");
        }
    }
}
