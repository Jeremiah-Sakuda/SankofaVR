using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
        [Tooltip("The seat the world was placed around; its forward is where the visitor faces when sitting straight.")]
        [SerializeField] Transform seat;
        [Tooltip("The light people and things of the story are drawn in; echoes of what the visitor lingered on use it too.")]
        [SerializeField] Material echoMaterial;
        [Tooltip("The depth of that light, drawn first, so an echo reads as one silhouette rather than its overlapping shells.")]
        [SerializeField] Material echoDepthMaterial;
        [Tooltip("Beside the visitor: where an object rests when it is set down for them.")]
        [SerializeField] Transform restPoint;
        [Tooltip("On the hearth stone: where the keepsake waits after a visit, and a kept visit's keepsake on return.")]
        [SerializeField] Transform keepsakePoint;
        [Tooltip("Where the keepsake waits in the firelight while the visitor decides whether to keep the visit, beside the choice.")]
        [SerializeField] Transform keepsakeShownPoint;
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
                if (o.TryGetComponent<IllustratedFigure>(out var illustration)) illustration.ResetState();
            }
            foreach (var s in scenes)
                if (s.Root.TryGetComponent<IllustratedTheatre>(out var theatre)) theatre.ResetState();
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
            // Echoes belong to the place they were shown in.
            ClearEchoes();
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

        /// <summary>
        /// Where a gift handed over in person comes to rest for the visitor: in front of the seat and within reach, a
        /// little to whichever side is clear of the people of the story and low, so it shows against the ground and
        /// reads as having come to the visitor rather than to anyone else.
        /// </summary>
        public Vector3 GiftPoint(Vector3 giverAt)
        {
            var offer = OfferPosition();
            var right = seat != null ? seat.right : head.right;
            right.y = 0;
            right.Normalize();
            // Of the two sides, the one farther from every person of the story now in the scene, the giver included.
            Vector3 Side(float s) => offer + right * s * 0.12f + Vector3.down * 0.06f;
            float Clearance(Vector3 p)
            {
                var nearest = Vector2.Distance(new Vector2(p.x, p.z), new Vector2(giverAt.x, giverAt.z));
                foreach (var o in objects)
                    if (o.Visible && o.Scene == ActiveScene && o.GetComponent<FigureMotion>() != null)
                        nearest = Mathf.Min(nearest, Vector2.Distance(new Vector2(p.x, p.z), new Vector2(o.transform.position.x, o.transform.position.z)));
                return nearest;
            }
            return Clearance(Side(-1f)) >= Clearance(Side(1f)) ? Side(-1f) : Side(1f);
        }

        /// <summary>
        /// Where something is offered to the visitor: in front of the seat at lap height, within the reach the scene is
        /// built to, wherever the head happens to be turned at that moment.
        /// </summary>
        public Vector3 OfferPosition()
        {
            var forward = seat != null ? seat.forward : head.forward;
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
            if (o.TryGetComponent<IllustratedFigure>(out var illustration))
            {
                illustration.Perform(action, o);
                return;
            }
            if (action.Kind == ActionKind.Offer && o.IllustratedHolder != null)
            {
                o.IllustratedHolder.Offer(o, this);
                return;
            }
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
                    var companion = to.GetComponent<FigureMotion>();
                    figure.GoTo(place, to.transform.position, kneel, companion != null, companion);
                    break;
                case ActionKind.Withdraw when figure != null:
                    figure.Leave();
                    break;
                case ActionKind.Offer:
                    // The fire offers from its flames; a story offers from where the object rests.
                    if (!o.Visible) o.ShowAt(IsHearth(o.Scene) ? FireTop : o.HomePosition);
                    // Handed over in person: the giver steps to the edge of the visitor's space and holds it out in
                    // her hand, a little nearer her than the usual offer, so the hand and the thing stay together.
                    if (o.InHolderHands && o.HandOverPlace.HasValue)
                    {
                        var offer = OfferPosition();
                        var standAt = o.HandOverPlace.Value;
                        var toward = new Vector3(standAt.x, offer.y, standAt.z);
                        o.Holder.HandOver(o, standAt, Vector3.MoveTowards(offer, toward, 0.14f), GiftPoint(standAt));
                        break;
                    }
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

        /// <summary>Both live and replayed outcomes select the same illustration. Looking never commits a choice.</summary>
        public void ResolveIllustration(StageObject item, bool accepted)
        {
            var scene = Array.Find(scenes, s => s.Id == ActiveScene);
            if (scene != null && scene.Root.TryGetComponent<IllustratedTheatre>(out var theatre)) theatre.Resolve(item, accepted);
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
            o.Illuminate();
            o.MoveTo(keepsakeShownPoint != null ? keepsakeShownPoint.position : keepsakePoint.position, 1.6f);
        }

        /// <summary>
        /// The story answers the visitor's look: someone looked at glances back at them, and when they look at a
        /// thing, the nearest person of the story follows their look to it. Presentation only; nothing advances.
        /// </summary>
        public void AnswerLook(string id)
        {
            var o = Find(id);
            if (o == null) return;
            if (o.TryGetComponent<FigureMotion>(out var looked) && looked.CanGlance)
            {
                looked.Glance(head.position, 3f);
                return;
            }
            FigureMotion nearest = null;
            var best = float.MaxValue;
            foreach (var other in Objects)
            {
                if (!other.Visible || other.Scene != ActiveScene || !other.TryGetComponent<FigureMotion>(out var f) || !f.CanGlance) continue;
                var d = Vector3.Distance(other.transform.position, o.transform.position);
                if (d < best) { best = d; nearest = f; }
            }
            if (nearest != null) nearest.Glance(o.transform.position, 2.5f);
        }

        readonly List<GameObject> echoes = new List<GameObject>();
        // Echoes waiting out their delay belong to one showing; a clear or a new showing ends them before they rise.
        int echoShowing;

        /// <summary>
        /// Shows back what the visitor lingered on: a small shape of light for each, rising out of the fire at home,
        /// or out of the scene's own light as a continuation opens, then staying there, dim and turning slowly, until
        /// the scene changes or a new visit begins. Presentation only.
        /// </summary>
        public void Echo(IReadOnlyList<string> ids)
        {
            var from = FireTop;
            if (!IsHearth(ActiveScene))
                foreach (var o in objects)
                    if (o.Scene == ActiveScene && o.Visible && o.EmitsLight) { from = o.transform.position + Vector3.up * 0.15f; break; }
            // To the seat's right of the light, in a row, clear of the choices on the left and of the flame itself.
            ShowEchoes(ids, from + SeatRight * 0.28f, 0.35f, 0.26f, 0.2f, true);
        }

        /// <summary>A kept visit's echoes wait small beside its keepsake on the hearth stone while the visitor chooses.</summary>
        public void ShelveEchoes(IReadOnlyList<string> ids) =>
            ShowEchoes(ids, keepsakePoint.position + Vector3.up * 0.02f, 0.12f, 0.1f, 0.03f, false);

        public void ClearEchoes()
        {
            echoShowing++;
            foreach (var e in echoes) if (e != null) Destroy(e);
            echoes.Clear();
        }

        /// <summary>Echoes shown now, risen or still rising.</summary>
        public int EchoCount => echoes.Count(e => e != null);

        /// <summary>The seat's right, level: rows and sides are laid out from it, not from where the head is turned.</summary>
        Vector3 SeatRight
        {
            get
            {
                var right = seat != null ? seat.right : head.right;
                right.y = 0;
                return right.sqrMagnitude > 1e-4f ? right.normalized : Vector3.right;
            }
        }

        void ShowEchoes(IReadOnlyList<string> ids, Vector3 from, float largest, float spacing, float rise, bool toOneSide)
        {
            if (echoMaterial == null || ids == null) return;
            ClearEchoes();
            var showing = echoShowing;
            var shown = ids.Select(Find).Where(o => o != null && o.EchoMesh != null).Take(3).ToList();
            for (var i = 0; i < shown.Count; i++)
            {
                var aside = toOneSide ? i * spacing : (i - (shown.Count - 1) / 2f) * spacing;
                StartCoroutine(RiseEcho(shown[i], transform.InverseTransformPoint(from), aside, i * 0.9f, largest, rise, showing));
            }
        }

        IEnumerator RiseEcho(StageObject o, Vector3 from, float aside, float delay, float largest, float rise, int showing)
        {
            for (var t = 0f; t < delay; t += StageClock.DeltaTime) yield return null;
            if (showing != echoShowing) yield break;
            var go = new GameObject("Echo of " + o.Id);
            go.transform.SetParent(transform, false);
            echoes.Add(go);
            go.AddComponent<MeshFilter>().sharedMesh = o.EchoMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var illustration = o.GetComponent<IllustratedFigure>();
            renderer.sharedMaterials = illustration != null ? illustration.Surface.sharedMaterials :
                echoDepthMaterial != null ? new[] { echoMaterial, echoDepthMaterial } : new[] { echoMaterial };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Sizes kept relative, a woman taller than a ewe and a ewe than a gourd, within a hand's height and a forearm's.
            var size = o.EchoMesh.bounds.size;
            var extent = Mathf.Max(size.y, size.x * 0.8f, size.z * 0.8f);
            var height = Mathf.Clamp(extent * 0.25f, 0.08f, largest);
            var scale = height / Mathf.Max(0.01f, extent);
            go.transform.localScale = Vector3.one * scale;
            var start = from + transform.InverseTransformVector(SeatRight) * aside;
            var block = new MaterialPropertyBlock();
            const float rising = 3.5f;
            // Rises clear of the light it came from, below the captions, then stays, breathing a little.
            for (var t = 0f; go != null; t += StageClock.DeltaTime)
            {
                var x = Mathf.Clamp01(t / rising);
                var local = start + Vector3.up * (0.04f + rise * Mathf.SmoothStep(0f, 1f, x) + 0.01f * Mathf.Sin(t * 1.3f + aside * 9f));
                go.transform.localPosition = local - Vector3.up * o.EchoMesh.bounds.min.y * scale;
                var at = transform.TransformPoint(local);
                var toward = Vector3.ProjectOnPlane(head.position - at, Vector3.up);
                if (toward.sqrMagnitude < 1e-4f) toward = -Vector3.forward;
                go.transform.rotation = Quaternion.LookRotation(illustration != null ? -toward : toward) * Quaternion.Euler(0, 20f * Mathf.Sin(t * 0.4f + aside * 5f), 0);
                var glow = Mathf.Lerp(1.4f, 0.8f, Mathf.Clamp01((t - rising) / 2f)) * Mathf.Clamp01(t / 0.6f);
                block.SetFloat("_Intensity", glow);
                block.SetFloat("_Procedural", 0f);
                block.SetFloat("_Memory", 1f);
                block.SetFloat("_Height", height);
                // The ground well below, so no part of an echo frays into smoke like a figure's feet.
                block.SetFloat("_GroundY", at.y - 0.15f);
                // No smoke: an echo adds light and never darkens the flame behind it.
                block.SetFloat("_Smoke", 0f);
                block.SetColor("_Color", new Color(1f, 0.6f, 0.26f, 1f));
                if (illustration != null) illustration.PaintMemory(renderer, block, Mathf.Clamp01(t / 0.6f) * 0.8f);
                else renderer.SetPropertyBlock(block);
                yield return null;
            }
        }

        /// <summary>
        /// The people of earlier scenes, life-size and faint, where they stood: shown for a few seconds as a
        /// continuation opens on the same ground, so one place across time is seen as well as told. Presentation only.
        /// </summary>
        public void RecallPeople(IEnumerable<string> sceneIds)
        {
            if (echoMaterial == null) return;
            var scenesShown = new HashSet<string>(sceneIds);
            var showing = echoShowing;
            var i = 0;
            foreach (var o in objects)
                if (scenesShown.Contains(o.Scene) && o.EchoMesh != null &&
                    (o.GetComponent<IllustratedFigure>() != null || o.TryGetComponent<FigureMotion>(out var f) && f.CanGlance))
                    StartCoroutine(Recall(o, i++ * 0.6f, showing));
        }

        IEnumerator Recall(StageObject o, float delay, int showing)
        {
            for (var t = 0f; t < delay; t += StageClock.DeltaTime) yield return null;
            if (showing != echoShowing) yield break;
            var go = new GameObject("Recalled " + o.Id);
            go.transform.SetParent(transform, false);
            echoes.Add(go);
            go.AddComponent<MeshFilter>().sharedMesh = o.EchoMesh;
            var renderer = go.AddComponent<MeshRenderer>();
            var illustration = o.GetComponent<IllustratedFigure>();
            renderer.sharedMaterials = illustration != null ? illustration.Surface.sharedMaterials :
                echoDepthMaterial != null ? new[] { echoMaterial, echoDepthMaterial } : new[] { echoMaterial };
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            // Facing the visitor, as they stood to listen and to give.
            var toward = Vector3.ProjectOnPlane(head.position - o.HomePosition, Vector3.up);
            go.transform.SetPositionAndRotation(o.HomePosition, toward.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(illustration != null ? -toward : toward) : Quaternion.identity);
            var block = new MaterialPropertyBlock();
            const float seconds = 10f;
            for (var t = 0f; t < seconds && go != null; t += StageClock.DeltaTime)
            {
                var fade = Mathf.Min(Mathf.Clamp01(t / 2.5f), Mathf.Clamp01((seconds - t) / 3f));
                block.SetFloat("_Intensity", 0.55f * fade);
                block.SetFloat("_Procedural", 0f);
                block.SetFloat("_Memory", 1f);
                block.SetFloat("_Height", 1.6f);
                block.SetFloat("_GroundY", o.HomePosition.y);
                block.SetFloat("_Smoke", 0.35f * fade);
                block.SetColor("_Color", new Color(1f, 0.6f, 0.26f, 1f));
                if (illustration != null) illustration.PaintMemory(renderer, block, fade * 0.55f);
                else renderer.SetPropertyBlock(block);
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        /// <summary>The visitor did what the story asked: the nearest person of it, if any, turns to look at them.</summary>
        public void AcknowledgeVisitor()
        {
            FigureMotion nearest = null;
            var best = float.MaxValue;
            foreach (var other in Objects)
            {
                if (!other.Visible || other.Scene != ActiveScene || !other.TryGetComponent<FigureMotion>(out var f) || !f.CanGlance) continue;
                var d = Vector3.Distance(other.transform.position, head.position);
                if (d < best) { best = d; nearest = f; }
            }
            if (nearest != null) nearest.Glance(head.position, 3f);
        }

        /// <summary>A kept visit's keepsake goes down to rest on the hearth stone, where it waits for the next visit.</summary>
        public void RestKeepsake()
        {
            var o = keepsake != null ? Find(keepsake) : null;
            if (o == null || !o.Visible) return;
            o.MoveTo(keepsakePoint.position, 1.4f);
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
