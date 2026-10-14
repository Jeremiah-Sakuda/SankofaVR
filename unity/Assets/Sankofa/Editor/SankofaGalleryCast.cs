using System;
using System.IO;
using System.Linq;
using Sankofa.Runtime.Stage;
using UnityEditor;
using UnityEngine;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        [Serializable] sealed class CastCalibration { public CastFigure[] figures; }
        [Serializable] sealed class CastFigure { public string id; public float anchorX, bodyHeight; public CastPose[] poses; }
        [Serializable] sealed class CastPose { public float footU, footV, topV, palmU, palmV; }

        static void CalibrateGalleryCast(StoryStage stage)
        {
            var data = JsonUtility.FromJson<CastCalibration>(File.ReadAllText("Assets/Sankofa/Content/gallery-figure-calibration.json"));
            foreach (var figure in data.figures)
            {
                var item = stage.Find(figure.id);
                var actor = item.GetComponent<IllustratedFigure>();
                var at = item.transform.localPosition; at.x = figure.anchorX; at.y = BoundaryGround(at);
                item.transform.localPosition = at;
                var bounds = actor.Surface.GetComponent<MeshFilter>().sharedMesh.bounds;
                var serialized = new SerializedObject(actor);
                var profiles = serialized.FindProperty("registrations"); profiles.arraySize = figure.poses.Length;
                for (var i = 0; i < figure.poses.Length; i++)
                {
                    var p = figure.poses[i];
                    if (p.topV <= p.footV || p.footU < 0 || p.footU > 1) throw new InvalidDataException("Invalid painted pose registration: " + figure.id);
                    var scale = figure.bodyHeight / (bounds.size.y * (p.topV - p.footV));
                    var offset = new Vector3(-(bounds.min.x + bounds.size.x * p.footU) * scale, -(bounds.min.y + bounds.size.y * p.footV) * scale, 0);
                    var palm = offset + new Vector3(bounds.min.x + bounds.size.x * p.palmU, bounds.min.y + bounds.size.y * p.palmV, -0.025f) * scale;
                    var profile = profiles.GetArrayElementAtIndex(i);
                    profile.FindPropertyRelative("Offset").vector3Value = offset;
                    profile.FindPropertyRelative("Hand").vector3Value = palm;
                    profile.FindPropertyRelative("Foot").vector2Value = new Vector2(p.footU, p.footV);
                    profile.FindPropertyRelative("Scale").floatValue = scale;
                    if (i == 0) { actor.Surface.transform.localPosition = offset; actor.Surface.transform.localScale = Vector3.one * scale; }
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                var cast = GalleryCopy(actor.Surface.sharedMaterial, "Registered gallery cast");
                cast.SetFloat("_Cutoff", 0.5f); cast.SetFloat("_BottomTrim", 0.024f); cast.SetFloat("_TopTrim", 0.994f);
                actor.Surface.sharedMaterial = cast;
            }
            RestGalleryVessel(stage.Find("gourd.flour"), stage.Find("figure.wambui").GetComponent<IllustratedFigure>());
            RestGalleryVessel(stage.Find("gourd.milk"), stage.Find("figure.naserian").GetComponent<IllustratedFigure>());
        }

        static void RestGalleryVessel(StageObject item, IllustratedFigure owner)
        {
            // A downturned/empty painted hand cannot support a bottle. It rests on the bank until the offering
            // pose puts it in the calibrated palm. Offer/receive then detach it through the existing stage contract.
            var root = owner.transform.parent;
            var lower = item.Surfaces.Min(r => r.bounds.min.y) - item.transform.position.y;
            item.transform.SetParent(root, true);
            var at = owner.transform.localPosition + new Vector3(owner.GetComponent<StageObject>().Id == "figure.wambui" ? 0.28f : 0.24f, 0, -0.08f);
            at.y = BoundaryGround(at) - lower;
            item.transform.localPosition = at;
        }
    }
}
