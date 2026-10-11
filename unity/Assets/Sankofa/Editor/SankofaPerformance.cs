using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        static AnimationClip KneelingRest(AnimationClip kneel)
        {
            // Hold the source's supported kneeling pose instead of repeating a repair gesture.
            // Preserve the torso and arm relationship so fitted clothing and resting hands stay coherent.
            var pose = new AnimationClip { name = "Kneeling rest", frameRate = 30, wrapMode = WrapMode.Loop };
            foreach (var binding in AnimationUtility.GetCurveBindings(kneel))
            {
                if (binding.type != typeof(Transform)) continue;
                var source = AnimationUtility.GetEditorCurve(kneel, binding);
                var value = source.Evaluate(kneel.length * 0.35f);
                AnimationUtility.SetEditorCurve(pose, binding, AnimationCurve.Constant(0, 4, value));
            }
            pose.EnsureQuaternionContinuity();
            var settings = AnimationUtility.GetAnimationClipSettings(pose);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(pose, settings);
            const string path = SankofaArt.Folder + "/Kneeling rest.anim";
            var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existing == null) { AssetDatabase.CreateAsset(pose, path); return pose; }
            EditorUtility.CopySerialized(pose, existing);
            Object.DestroyImmediate(pose);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        static float MeasureWalkSpeed(Animator actor, Transform[] feet)
        {
            var walk = AssetDatabase.LoadAllAssetsAtPath(SankofaFigureImport.ModelPath).OfType<AnimationClip>().Single(c => c.name == "Walk_Loop");
            var clone = Object.Instantiate(actor.gameObject);
            clone.hideFlags = HideFlags.HideAndDontSave;
            clone.GetComponent<Animator>().enabled = false;
            var bones = feet.Select(f => clone.GetComponentsInChildren<Transform>().Single(t => t.name == f.name)).ToArray();
            const int samples = 120;
            var positions = new Vector3[2, samples];
            var minimum = new[] { float.MaxValue, float.MaxValue };
            for (var i = 0; i < samples; i++)
            {
                walk.SampleAnimation(clone, walk.length * i / samples);
                for (var f = 0; f < 2; f++)
                {
                    positions[f, i] = bones[f].position;
                    minimum[f] = Mathf.Min(minimum[f], positions[f, i].y);
                }
            }
            var sum = 0f; var count = 0;
            for (var f = 0; f < 2; f++)
                for (var i = 1; i < samples; i++)
                    if (positions[f, i].y < minimum[f] + 0.035f && positions[f, i - 1].y < minimum[f] + 0.035f)
                    {
                        var step = positions[f, i] - positions[f, i - 1]; step.y = 0;
                        var speed = step.magnitude * samples / walk.length;
                        if (speed > 0.03f) { sum += speed; count++; }
                    }
            Object.DestroyImmediate(clone);
            if (count == 0) throw new System.InvalidOperationException("Walk calibration found no low-foot stance samples.");
            var measured = sum / count;
            if (measured < 0.15f || measured > 3f) throw new System.InvalidOperationException("Unexpected walk stance speed " + measured);
            Debug.Log("[Sankofa] Measured walk stance speed " + measured.ToString("F3") + " m/s from " + count + " samples.");
            return measured;
        }
    }
}
