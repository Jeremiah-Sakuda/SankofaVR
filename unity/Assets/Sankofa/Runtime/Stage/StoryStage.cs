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
    /// The scenes and staged objects around the stationary visitor. Performs accepted story actions, carries
    /// objects across transitions and keeps the scene lit by its one warm source: the fire at the hearth, a carried
    /// light in a story. Also the stage's inventory, so plans needing anything it lacks are refused before they play.
    /// </summary>
    public sealed class StoryStage : MonoBehaviour, IStageInventory
    {
        [SerializeField] StageScene[] scenes;
        [SerializeField] StageObject[] objects;
        [SerializeField] HearthFire fire;
        [SerializeField] Camera eye;
        [SerializeField] Transform head;
        [Tooltip("Beside the visitor: where an object rests when it is set down for them.")]
        [SerializeField] Transform restPoint;
        [Tooltip("On the hearth stone: where the keepsake waits after a visit, and a kept visit's keepsake on return.")]
        [SerializeField] Transform keepsakePoint;
        [Tooltip("Just in front of the visitor: where a received object floats when not held in a palm.")]
        [SerializeField] Transform carryPoint;
        [SerializeField] float offerForward = 0.42f;
        [SerializeField] float offerDrop = 0.3f;

        string keepsake;

        public string ActiveScene { get; private set; }
        public IReadOnlyList<StageObject> Objects => objects;
        public IReadOnlyList<StageScene> Scenes => scenes;
        public Vector3 FireTop => fire.transform.position + Vector3.up * 0.3f;

        public StageObject Find(string id)
        {
            foreach (var o in objects)
                if (o != null && o.Id == id) return o;
            return null;
        }

        public bool HasScene(string sceneId) => Array.Exists(scenes, s => s.Id == sceneId);
        public bool HasObject(string objectId, string sceneId) => Find(objectId)?.Scene == sceneId;
        public bool IsHearth(string sceneId) => Array.Find(scenes, s => s.Id == sceneId)?.IsHearth ?? false;

        public void ResetAll()
        {
            keepsake = null;
            foreach (var o in objects)
            {
                o.ResetToStart();
                if (o.TryGetComponent<FigureMotion>(out var figure)) figure.ResetState();
            }
            foreach (var s in scenes)
                if (s.IsHearth) ShowScene(s.Id);
        }

        public void ShowScene(string id)
        {
            var scene = Array.Find(scenes, s => s.Id == id);
            if (scene == null)
            {
                Debug.LogError("[Sankofa] Unknown scene " + id);
                return;
            }
            // Scenes may share geometry; activate the shown scene's root last.
            foreach (var s in scenes)
                if (s.Root != scene.Root) s.Root.SetActive(false);
            scene.Root.SetActive(true);
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
            var figure = o.GetComponent<FigureMotion>();
            switch (action.Kind)
            {
                case ActionKind.Reveal:
                case ActionKind.Illuminate:
                    // A person of the story walks in to their place rather than appearing there.
                    if (!o.Visible && figure != null) figure.Arrive();
                    if (action.Kind == ActionKind.Reveal) o.Reveal();
                    else
                    {
                        o.Illuminate();
                        // The story turns to something a person holds or owns: they show it.
                        if (o.Holder != null && Find(o.Holder.GetComponent<StageObject>().Id)?.Visible == true) o.Holder.Present();
                    }
                    break;
                case ActionKind.Move when figure == null:
                    Debug.LogError($"[Sankofa] {action}: '{o.Id}' is not a figure that can move.");
                    break;
                case ActionKind.Move when action.At == null:
                    figure.GoHome();
                    break;
                case ActionKind.Move:
                    var to = Find(action.At);
                    if (to == null)
                    {
                        Debug.LogError($"[Sankofa] No staged object for {action}; content and scene disagree.");
                        break;
                    }
                    var (place, kneel) = to.ApproachFrom(o.HomePosition);
                    // Going to another person, a figure that carries something holds it out when it arrives.
                    figure.GoTo(place, to.transform.position, kneel, to.GetComponent<FigureMotion>() != null);
                    break;
                case ActionKind.Withdraw when figure != null:
                    figure.Leave();
                    break;
                case ActionKind.Offer:
                    // The fire offers from its flames; a story offers from where the object rests.
                    if (!o.Visible) o.ShowAt(IsHearth(o.Scene) ? FireTop : o.HomePosition);
                    o.MoveTo(OfferPosition(), 1.4f);
                    // The person who gives it holds it out toward the visitor.
                    if (o.Holder != null) o.Holder.Reach(OfferPosition(), 2.5f, o);
                    break;
                case ActionKind.Place when action.At == null:
                    if (!o.Visible) o.AppearAt(o.HomePosition);
                    o.MoveTo(restPoint.position, 1.2f);
                    break;
                case ActionKind.Place:
                    var target = Find(action.At);
                    // Set down by the person who carries it, if it is still in their hands rather than the visitor's.
                    if (o.Holder != null && target != null && o.CarriedBy == null && Vector3.Distance(o.transform.position, o.HomePosition) < 0.15f)
                        o.Holder.Reach(target.PlacePosition, 2f);
                    o.Reveal();
                    o.MoveTo(target != null ? target.PlacePosition : o.HomePosition, 1.3f);
                    break;
                case ActionKind.Withdraw:
                    o.Withdraw(IsHearth(o.Scene) ? fire.transform.position + Vector3.up * 0.12f : o.HomePosition);
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
            o.Carry(hand, () =>
            {
                if (!hand.Tracked) return carryPoint.position;
                var sample = hand.Sample;
                var cradled = HandGeometry.Openness(sample) >= 1.3f && System.Numerics.Vector3.Dot(HandGeometry.PalmNormal(sample), System.Numerics.Vector3.UnitY) >= 0.3f;
                return cradled ? hand.PalmCenter + Vector3.up * 0.06f : carryPoint.position;
            });
        }

        /// <summary>
        /// One object becomes its counterpart in the next scene where it stands: in a story it settles at its own
        /// place; at the hearth it stays in the hand that carried it, or beside the visitor.
        /// </summary>
        public void Cross(string fromId, string toId, string toScene)
        {
            var from = Find(fromId);
            var to = Find(toId);
            if (from == null || to == null) return;
            var at = from.Visible ? from.transform.position : to.HomePosition;
            var hand = from.CarriedBy;
            from.Hide();
            to.ShowAt(at);
            if (!IsHearth(toScene)) to.MoveTo(to.HomePosition, 2.4f);
            else if (hand != null) Carry(to, hand);
            else to.MoveTo(restPoint.position, 1.6f);
        }

        public void ShowKeepsake(string id)
        {
            var o = Find(id);
            if (o == null) return;
            keepsake = id;
            o.Reveal();
            o.MoveTo(keepsakePoint.position, 1.6f);
        }

        /// <summary>The kept visit's keepsake waits on the hearth stone while the visitor chooses.</summary>
        public void ShowOnShelf(string id)
        {
            var o = Find(id);
            if (o == null || !IsHearth(o.Scene)) return;
            keepsake = id;
            o.AppearAt(keepsakePoint.position);
        }

        /// <summary>At the end of a visit, whatever the visitor still has apart from the keepsake goes back into the fire.</summary>
        public void Settle()
        {
            foreach (var o in objects)
                if (o.Visible && o.Id != keepsake && IsHearth(o.Scene) && o.Scene == ActiveScene)
                    o.Withdraw(fire.transform.position + Vector3.up * 0.12f, 1.6f);
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
