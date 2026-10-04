using System;
using System.Collections.Generic;
using Sankofa.Core.Story;
using Sankofa.Interaction;
using Sankofa.Runtime.Fire;
using Sankofa.Runtime.Input;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    [Serializable]
    public sealed class StageScene
    {
        public string Id;
        public bool IsHearth;
        public GameObject Root;
        public Color Sky = new Color(0.01f, 0.012f, 0.025f);
        public Color AmbientSky = new Color(0.05f, 0.06f, 0.1f);
        public Color AmbientGround = new Color(0.025f, 0.02f, 0.02f);
        public Color Fog = new Color(0.01f, 0.012f, 0.025f);
        public float FogStart = 6f;
        public float FogEnd = 22f;
        public Vector3 TowardMoon = new Vector3(-0.45f, 0.55f, 0.7f);
        public Color Moon = Color.black;
        public Color MistColor = Color.black;
        [Tooltip("Amount, base height, height falloff, start distance.")]
        public Vector4 Mist = Vector4.zero;
        [Tooltip("Exposure, saturation, shadow coolness, highlight warmth.")]
        public Vector4 Grade = new Vector4(1f, 1f, 0f, 0f);
    }

    /// <summary>
    /// The scenes and staged objects around the stationary visitor. Performs accepted story actions and keeps
    /// the scene lit by its one warm source: the fire at the hearth, a carried light in a story.
    /// </summary>
    public sealed class StoryStage : MonoBehaviour
    {
        [SerializeField] StageScene[] scenes;
        [SerializeField] StageObject[] objects;
        [SerializeField] HearthFire fire;
        [SerializeField] Camera eye;
        [SerializeField] Transform head;
        [Tooltip("Beside the visitor: where an object rests when the default path places it.")]
        [SerializeField] Transform restPoint;
        [Tooltip("On the hearth stone: where the keepsake waits at the end of a visit.")]
        [SerializeField] Transform keepsakePoint;
        [Tooltip("Just in front of the visitor: where a received object floats when not held in a palm.")]
        [SerializeField] Transform carryPoint;
        [SerializeField] float offerForward = 0.42f;
        [SerializeField] float offerDrop = 0.3f;

        public string ActiveScene { get; private set; }
        public IReadOnlyList<StageObject> Objects => objects;
        public IReadOnlyList<StageScene> Scenes => scenes;

        public StageObject Find(string id)
        {
            foreach (var o in objects)
                if (o != null && o.Id == id) return o;
            return null;
        }

        public bool HasScene(string id) => Array.Exists(scenes, s => s.Id == id);

        public void ResetAll()
        {
            foreach (var o in objects) o.ResetToStart();
            foreach (var s in scenes)
                if (s.IsHearth) ShowScene(s.Id);
        }

        public void ShowScene(string id)
        {
            foreach (var s in scenes) s.Root.SetActive(s.Id == id);
            var scene = Array.Find(scenes, s => s.Id == id);
            if (scene == null)
            {
                Debug.LogError("[Sankofa] Unknown scene " + id);
                return;
            }
            ActiveScene = id;
            if (eye != null) eye.backgroundColor = scene.Sky;
            SceneLighting.SetAmbient(scene.AmbientSky, scene.AmbientGround);
            SceneLighting.SetFog(scene.Fog, scene.FogStart, scene.FogEnd);
            SceneLighting.SetMoon(scene.TowardMoon, scene.Moon);
            SceneLighting.SetMist(scene.MistColor, scene.Mist.x, scene.Mist.y, scene.Mist.z, scene.Mist.w);
            SceneLighting.SetGrade(scene.Grade.x, scene.Grade.y, scene.Grade.z, scene.Grade.w);
            if (fire != null)
            {
                fire.gameObject.SetActive(scene.IsHearth);
                fire.LightsScene = scene.IsHearth;
            }
        }

        /// <summary>In front of and below the visitor's eyes, within a comfortable seated reach.</summary>
        public Vector3 OfferPosition()
        {
            var forward = head.forward;
            forward.y = 0;
            if (forward.sqrMagnitude < 1e-4f) forward = transform.forward;
            return head.position + forward.normalized * offerForward + Vector3.down * offerDrop;
        }

        public void Perform(StoryAction action)
        {
            var o = Find(action.Object);
            if (o == null)
            {
                Debug.LogError($"[Sankofa] No staged object for {action}; content and scene disagree.");
                return;
            }
            switch (action.Kind)
            {
                case ActionKind.Reveal:
                    o.Reveal();
                    break;
                case ActionKind.Illuminate:
                    o.Illuminate();
                    break;
                case ActionKind.Offer:
                    o.ShowAt(fire.transform.position + Vector3.up * 0.3f);
                    o.MoveTo(OfferPosition(), 1.4f);
                    break;
                case ActionKind.Place:
                    o.Reveal();
                    o.MoveTo(restPoint.position, 1.2f);
                    break;
            }
        }

        /// <summary>
        /// A received object stays in the receiving palm while it is open and facing up, and otherwise floats just in
        /// front of the visitor. Accepted with the select control, it goes straight to that place.
        /// </summary>
        public void Carry(StageObject o, HandSource hand)
        {
            if (hand == null)
            {
                o.MoveTo(carryPoint.position, 0.8f);
                return;
            }
            o.Follow(() =>
            {
                if (!hand.Tracked) return carryPoint.position;
                var sample = hand.Sample;
                var cradled = HandGeometry.Openness(sample) >= 1.3f && System.Numerics.Vector3.Dot(HandGeometry.PalmNormal(sample), System.Numerics.Vector3.UnitY) >= 0.3f;
                return cradled ? hand.PalmCenter + Vector3.up * 0.06f : carryPoint.position;
            });
        }

        public void ShowKeepsake(string id)
        {
            var o = Find(id);
            if (o == null) return;
            o.Reveal();
            o.MoveTo(keepsakePoint.position, 1.6f);
        }

        void LateUpdate()
        {
            var scene = Array.Find(scenes, s => s.Id == ActiveScene);
            if (scene == null) return;
            // The world turns to face the seated visitor (and again on recentre); the moon turns with it,
            // so its light agrees with the moon shadows baked into the scene.
            SceneLighting.SetMoon(scene.Root.transform.rotation * scene.TowardMoon, scene.Moon);
            if (scene.IsHearth) return;
            foreach (var o in objects)
            {
                if (o.Scene != ActiveScene || !o.EmitsLight || !o.Visible) continue;
                var flicker = 1f + (Mathf.PerlinNoise(Time.time * 4.7f, 0.9f) - 0.5f) * 0.16f;
                SceneLighting.SetLight(o.transform.position + Vector3.up * 0.05f, o.LightColor, o.LightIntensity * flicker, 1.1f);
                return;
            }
            SceneLighting.SetLight(Vector3.zero, Color.black, 0, 1);
        }
    }
}
