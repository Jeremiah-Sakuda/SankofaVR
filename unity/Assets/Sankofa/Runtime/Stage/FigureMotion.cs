using System.Collections.Generic;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>
    /// How a person or animal of the story moves: walking in along a path when revealed, going to stand or kneel
    /// where something is used, going back to its own place, and walking away when withdrawn. Presentation only: the
    /// story decides when, through reveal, move and withdraw, and the stage asks for a gesture when something the
    /// figure holds is offered, set down or shown. A figure with an animator (the skinned mannequin) walks, kneels,
    /// gives and talks through it, and what it carries follows its hand and neck bones; a figure without one (the
    /// ewe, or the procedural silhouette) is posed by the light shader, which folds the body into a kneel and swings
    /// each arm from the shoulder, and its carried things follow the same fold.
    /// </summary>
    [RequireComponent(typeof(StageObject))]
    public sealed class FigureMotion : MonoBehaviour
    {
        [Tooltip("Where the figure walks in from, through to just before its own place, in its parent's space; empty to appear in place.")]
        [SerializeField] Vector3[] entryPath = new Vector3[0];
        [SerializeField] Renderer body;
        [SerializeField] float height = 1.6f;
        [SerializeField] float speed = 0.7f;
        [Tooltip("Height of the rise and fall of each step.")]
        [SerializeField] float bob = 0.016f;
        [SerializeField] float stepLength = 0.55f;
        [Tooltip("Seconds to wait after being revealed before setting off, so a companion can follow behind.")]
        [SerializeField] float delay;
        [Tooltip("Animals have no arms to gesture with.")]
        [SerializeField] bool hasArms = true;
        [Tooltip("Points that carried things hang from (hands, neck), placed for standing; they follow the kneel.")]
        [SerializeField] Transform[] anchors = new Transform[0];
        [Tooltip("With an animator, the bones each anchor follows (same order as the anchors).")]
        [SerializeField] Transform[] anchorBones = new Transform[0];
        [Tooltip("With an animator, the giving arm's upper arm, forearm and hand, reached toward whoever is given to.")]
        [SerializeField] Transform[] reachArm = new Transform[0];
        [Tooltip("Drives a skinned body's walk, kneel, give and talk; without one the shader poses the body.")]
        [SerializeField] Animator animator;
        [Tooltip("Other parts drawn in the same light as the body (cloth, an animal's head and legs), which follow its visibility and colour.")]
        [SerializeField] Renderer[] parts = new Renderer[0];
        [Tooltip("Smoke opacity of those parts; cloth is denser than the body it covers.")]
        [SerializeField] float partSmoke = 0.88f;
        [Tooltip("Smoke opacity of the body: dense enough that a figure reads as a presence, not glass.")]
        [SerializeField] float bodySmoke = 0.84f;
        [Tooltip("How much each part shortens toward its bone while kneeling (cloth gathered on the ground).")]
        [SerializeField] float[] partKneelScale = new float[0];
        [Tooltip("The edge colour of this figure's light; one family line shares one colour across the eras.")]
        [SerializeField] Color tint = new Color(1f, 0.55f, 0.22f, 1f);
        [Tooltip("With an animator, the bone that turns the head when the figure glances at something.")]
        [SerializeField] Transform lookBone;
        [Tooltip("Degrees the head is lifted from the kneeling clip's bow.")]
        [SerializeField] float kneelLift = 28f;
        [Tooltip("Paths bend around the seat rather than passing nearer than this (metres, in the plan).")]
        [SerializeField] float personalSpace = 1.15f;

        static readonly int KneelId = Shader.PropertyToID("_Kneel");
        static readonly int HeightId = Shader.PropertyToID("_Height");
        static readonly int ArmNegId = Shader.PropertyToID("_ArmNeg");
        static readonly int ArmPosId = Shader.PropertyToID("_ArmPos");
        static readonly int ArmInId = Shader.PropertyToID("_ArmIn");
        static readonly int ProceduralId = Shader.PropertyToID("_Procedural");
        static readonly int GroundId = Shader.PropertyToID("_GroundY");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int SmokeId = Shader.PropertyToID("_Smoke");
        static readonly int CrownId = Shader.PropertyToID("_CrownY");
        static readonly int WalkingId = Animator.StringToHash("Walking");
        static readonly int KneelingId = Animator.StringToHash("Kneeling");
        static readonly int TalkingId = Animator.StringToHash("Talking");
        static readonly int GiveId = Animator.StringToHash("Give");
        static readonly int PaceId = Animator.StringToHash("Pace");

        readonly List<Vector3> route = new List<Vector3>();
        StageObject staged;
        MaterialPropertyBlock block;
        bool initialised;
        Vector3 homeLocal;
        Quaternion homeRotation;
        Vector3 basePosition;
        Quaternion? finalRotation;
        int next;
        float wait;
        float travelled;
        float kneel, kneelTarget;
        bool kneelOnArrival;
        bool presentOnArrival;
        Vector3[] anchorRest;
        Vector3[] partRest;
        StageObject[] carried;
        float armNeg, armPos, armIn;
        float reachFor, presentFor;
        float reachSide;
        Vector3 reachTarget;
        StageObject offered;
        StageObject handingOver;
        Vector3 handOverPoint;
        Vector3 passTo;
        StageObject passing;
        float passAfter;
        float reachWeight;
        Vector3 lookTarget;
        float lookFor;
        float lookWeight;

        public bool Walking => next < route.Count && wait <= 0;
        /// <summary>Metres walked so far, for a gait to keep its steps in time.</summary>
        public float Travelled => travelled;
        public float Kneel => kneel;
        /// <summary>How far each arm is raised forward (0 hanging, 1 held straight out).</summary>
        public (float Negative, float Positive) Arms => (armNeg, armPos);

        void Awake() => Initialise();

        void Initialise()
        {
            if (initialised) return;
            initialised = true;
            staged = GetComponent<StageObject>();
            block = new MaterialPropertyBlock();
            homeLocal = transform.localPosition;
            homeRotation = transform.localRotation;
            basePosition = transform.position;
            partRest = new Vector3[parts.Length];
            for (var i = 0; i < parts.Length; i++) partRest[i] = parts[i] != null ? parts[i].transform.localScale : Vector3.one;
            anchorRest = new Vector3[anchors.Length];
            for (var i = 0; i < anchors.Length; i++) anchorRest[i] = anchors[i] != null ? anchors[i].localPosition : Vector3.zero;
            var list = new List<StageObject>();
            foreach (var anchor in anchors)
                if (anchor != null) list.AddRange(anchor.GetComponentsInChildren<StageObject>(true));
            carried = list.ToArray();
        }

        Vector3 InParent(Vector3 local) => transform.parent != null ? transform.parent.TransformPoint(local) : local;
        Quaternion HomeRotation => transform.parent != null ? transform.parent.rotation * homeRotation : homeRotation;

        /// <summary>Back to its own place, standing, as the scene was built.</summary>
        public void ResetState()
        {
            Initialise();
            route.Clear();
            next = 0;
            wait = 0;
            kneel = kneelTarget = 0;
            kneelOnArrival = false;
            presentOnArrival = false;
            reachFor = presentFor = 0;
            offered = null;
            handingOver = null;
            passing = null;
            lookFor = lookWeight = 0;
            reachWeight = 0;
            armNeg = armPos = armIn = 0;
            finalRotation = null;
            transform.localPosition = homeLocal;
            transform.localRotation = homeRotation;
            basePosition = transform.position;
            Apply();
        }

        /// <summary>Revealed: appears at the start of its path and walks to its own place.</summary>
        public void Arrive()
        {
            Initialise();
            ResetState();
            if (entryPath.Length == 0) return;
            for (var i = 1; i < entryPath.Length; i++) route.Add(InParent(entryPath[i]));
            route.Add(InParent(homeLocal));
            basePosition = InParent(entryPath[0]);
            transform.position = basePosition;
            var first = route[0] - basePosition;
            first.y = 0;
            if (first.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.LookRotation(first);
            finalRotation = HomeRotation;
            wait = delay;
        }

        /// <summary>
        /// Holds out the arm on the side nearer <paramref name="toward"/> for a while: giving, handing over or setting
        /// something down.
        /// </summary>
        public void Reach(Vector3 toward, float seconds = 2.5f, StageObject waitingFor = null)
        {
            Initialise();
            reachSide = Mathf.Sign(transform.InverseTransformPoint(toward).x);
            if (reachSide == 0) reachSide = 1;
            reachFor = seconds;
            reachTarget = toward;
            offered = waitingFor;
            // Turn to whoever is being given to, unless still walking somewhere.
            if (!Walking)
            {
                var facing = toward - transform.position;
                facing.y = 0;
                if (facing.sqrMagnitude > 1e-4f) finalRotation = Quaternion.LookRotation(facing);
            }
            // The two-bone reach gives with one arm; the clip's own gesture would raise the other.
            if (animator != null && reachArm.Length != 3) animator.SetTrigger(GiveId);
        }

        /// <summary>Whether this figure can turn its head: a skinned body with a head bone.</summary>
        public bool CanGlance => lookBone != null && animator != null;

        /// <summary>
        /// Turns the head toward a point for a while: someone of the story glancing back at the visitor who looks at
        /// her, or following the visitor's look to what they are looking at. Presentation only.
        /// </summary>
        public void Glance(Vector3 at, float seconds = 2.5f)
        {
            Initialise();
            lookTarget = at;
            lookFor = seconds;
        }

        /// <summary>
        /// Hands something to the visitor in person: walks to a place at the edge of their space, faces them and holds
        /// it out in her hand toward <paramref name="point"/>. After a moment the gift leaves her hand and drifts on
        /// the firelight to <paramref name="passTo"/>, in front of the visitor and within easy reach, while she keeps
        /// her distance.
        /// </summary>
        public void HandOver(StageObject item, Vector3 place, Vector3 point, Vector3 passTo)
        {
            Initialise();
            handingOver = item;
            handOverPoint = point;
            this.passTo = passTo;
            GoTo(place, point, false);
        }

        /// <summary>Something offered from these hands still waits where it was held out, not yet taken.</summary>
        bool StillOffering => offered != null && offered.Visible && offered.CarriedBy == null &&
                              Vector3.Distance(offered.transform.position, reachTarget) < 0.2f;

        /// <summary>Brings both hands forward for a while, showing or pouring what it holds.</summary>
        public void Present(float seconds = 3f)
        {
            Initialise();
            presentFor = seconds;
        }

        /// <summary>Goes to a place, rising first if kneeling, then stands or kneels there facing what it came to.</summary>
        public void GoTo(Vector3 place, Vector3 lookAt, bool kneelThere, bool presentThere = false)
        {
            Initialise();
            route.Clear();
            next = 0;
            wait = 0;
            AddAroundSeat(basePosition, place);
            route.Add(place);
            kneelTarget = 0;
            kneelOnArrival = kneelThere;
            presentOnArrival = presentThere;
            var facing = lookAt - place;
            facing.y = 0;
            finalRotation = facing.sqrMagnitude > 1e-4f ? Quaternion.LookRotation(facing) : HomeRotation;
        }

        /// <summary>
        /// The seat is at the origin of the figure's parent. A straight way that would pass nearer to it than the
        /// personal space goes around it instead: from where the way first touches that circle to where it leaves
        /// it, along an arc a little outside it, on the side the way was passing. No leg comes nearer than the circle.
        /// </summary>
        void AddAroundSeat(Vector3 from, Vector3 to)
        {
            var seat = transform.parent != null ? transform.parent.position : Vector3.zero;
            var a = new Vector2(from.x - seat.x, from.z - seat.z);
            var b = new Vector2(to.x - seat.x, to.z - seat.z);
            var ab = b - a;
            if (ab.sqrMagnitude < 1e-4f || a.magnitude <= personalSpace || b.magnitude <= personalSpace) return;
            var t = Mathf.Clamp01(-Vector2.Dot(a, ab) / ab.sqrMagnitude);
            var nearest = a + ab * t;
            if (nearest.magnitude >= personalSpace) return;
            var passing = nearest.sqrMagnitude > 1e-4f ? Mathf.Atan2(nearest.y, nearest.x) : Mathf.Atan2(-ab.x, ab.y);
            var start = Tangent(a, passing);
            var sweep = Mathf.DeltaAngle(start * Mathf.Rad2Deg, Tangent(b, passing) * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            // The arc must pass the side the way was passing; if the short way round does not, go the long way.
            var toPassing = Mathf.DeltaAngle(start * Mathf.Rad2Deg, passing * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            if (Mathf.Sign(toPassing) != Mathf.Sign(sweep) || Mathf.Abs(toPassing) > Mathf.Abs(sweep))
                sweep -= Mathf.Sign(sweep) * Mathf.PI * 2f;
            // Points 30 degrees apart a tenth of a metre outside the circle: each chord stays outside it.
            var radius = personalSpace + 0.1f;
            var steps = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(sweep) / (30f * Mathf.Deg2Rad)));
            for (var k = 0; k <= steps; k++)
            {
                var angle = start + sweep * k / steps;
                var y = Mathf.Lerp(from.y, to.y, (float)k / steps);
                route.Add(new Vector3(seat.x + Mathf.Cos(angle) * radius, y, seat.z + Mathf.Sin(angle) * radius));
            }
        }

        /// <summary>The angle of the point where a line from <paramref name="p"/> touches the personal-space circle,
        /// on the side nearer <paramref name="toward"/>.</summary>
        float Tangent(Vector2 p, float toward)
        {
            var at = Mathf.Atan2(p.y, p.x);
            var spread = Mathf.Acos(Mathf.Clamp(personalSpace / p.magnitude, -1f, 1f));
            var left = at + spread;
            var right = at - spread;
            return Mathf.Abs(Mathf.DeltaAngle(left * Mathf.Rad2Deg, toward * Mathf.Rad2Deg)) <
                   Mathf.Abs(Mathf.DeltaAngle(right * Mathf.Rad2Deg, toward * Mathf.Rad2Deg)) ? left : right;
        }

        /// <summary>Back to its own place, standing and facing the visitor as it did.</summary>
        public void GoHome()
        {
            Initialise();
            GoTo(InParent(homeLocal), Vector3.zero, false);
            finalRotation = HomeRotation;
        }

        /// <summary>Rises and walks back the way it came, fading as it goes; without a path, fades where it stands.</summary>
        public void Leave()
        {
            Initialise();
            passing = null;
            handingOver = null;
            route.Clear();
            next = 0;
            wait = 0;
            kneelTarget = 0;
            kneelOnArrival = false;
            finalRotation = null;
            var length = 0f;
            var from = basePosition;
            for (var i = entryPath.Length - 1; i >= 0; i--)
            {
                var p = InParent(entryPath[i]);
                // Leaving, too, the way bends around the seat rather than passing in front of the visitor.
                var before = route.Count;
                AddAroundSeat(from, p);
                for (var k = before; k < route.Count; k++)
                {
                    length += Vector3.Distance(from, route[k]);
                    from = route[k];
                }
                length += Vector3.Distance(from, p);
                from = p;
                route.Add(p);
            }
            // Mostly gone by the time it reaches the edge of the scene, with whatever is still in its hands.
            var fade = route.Count == 0 ? 0.2f : 0.9f + length / speed * 0.45f;
            staged.FadeAway(fade);
            foreach (var o in carried)
                if (o != null && o.Visible && Vector3.Distance(o.transform.position, o.HomePosition) < 0.15f) o.FadeAway(fade);
        }

        void Update()
        {
            var dt = StageClock.DeltaTime;
            if (animator != null) animator.speed = StageClock.AnimatorSpeed;
            var walking = false;
            if (wait > 0) wait -= dt;
            else if (next < route.Count)
            {
                if (kneel > 0.02f) kneelTarget = 0;
                else
                {
                    walking = true;
                    var to = route[next] - basePosition;
                    var distance = to.magnitude;
                    var step = speed * dt;
                    if (distance <= step)
                    {
                        basePosition = route[next];
                        next++;
                        if (next >= route.Count)
                        {
                            kneelTarget = kneelOnArrival ? 1 : 0;
                            if (presentOnArrival) presentFor = 3.5f;
                            presentOnArrival = false;
                        }
                    }
                    else basePosition += to / distance * step;
                    travelled += Mathf.Min(step, distance);
                    to.y = 0;
                    if (to.sqrMagnitude > 1e-4f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 5f);
                }
            }
            else if (finalRotation.HasValue)
                transform.rotation = Quaternion.Slerp(transform.rotation, finalRotation.Value, dt * 3f);

            // Arrived to hand something over: hold it out, then let it go to the visitor on the firelight.
            if (handingOver != null && next >= route.Count && wait <= 0)
            {
                Reach(handOverPoint, 4f, handingOver);
                passing = handingOver;
                passAfter = 1.6f;
                handingOver = null;
            }
            if (passing != null && (passAfter -= dt) <= 0)
            {
                if (passing.InHolderHands && passing.Visible) passing.MoveTo(passTo, 2.2f);
                passing = null;
            }
            lookFor = Mathf.Max(0, lookFor - dt);
            lookWeight = Mathf.MoveTowards(lookWeight, lookFor > 0 && !walking ? 1f : 0f, dt / 0.5f);
            var reaching = reachArm.Length == 3 && offered != null && offered.InHolderHands && (reachFor > 0 || StillOffering);
            reachWeight = Mathf.MoveTowards(reachWeight, reaching ? 1f : 0f, dt / 0.6f);
            kneel = Mathf.MoveTowards(kneel, kneelTarget, dt / 0.9f);
            var lift = walking ? bob * Mathf.Abs(Mathf.Sin(travelled / stepLength * Mathf.PI)) : 0f;
            transform.position = basePosition + Vector3.up * lift;
            UpdateArms(dt, walking);
            if (animator != null)
            {
                animator.SetBool(WalkingId, walking);
                animator.SetFloat(PaceId, speed / 1.1f);
                animator.SetBool(KneelingId, kneelTarget > 0 && !walking);
                animator.SetBool(TalkingId, presentFor > 0);
            }
            else FollowAnchors();
            Apply();
        }

        /// <summary>After the animator has posed the bones this frame, carried things follow the hand and neck.</summary>
        void LateUpdate()
        {
            if (animator == null) return;
            if (reachWeight > 0.001f) ReachArm(reachTarget, Mathf.SmoothStep(0f, 1f, reachWeight));
            // Kneeling, the clip bows the head right down; lifted a little, the crown never faces the visitor like a face.
            // A glance turns the lifted head.
            if (lookBone != null && kneel > 0.01f)
                lookBone.rotation = Quaternion.AngleAxis(-kneelLift * Mathf.SmoothStep(0f, 1f, kneel), transform.right) * lookBone.rotation;
            if (lookWeight > 0.001f && lookBone != null) Look(lookTarget, Mathf.SmoothStep(0f, 1f, lookWeight));
            for (var i = 0; i < anchors.Length && i < anchorBones.Length; i++)
                if (anchors[i] != null && anchorBones[i] != null) anchors[i].position = anchorBones[i].position;
        }

        /// <summary>
        /// Turns the head bone over the animated pose toward a point, within a comfortable turn of the body's facing:
        /// up to 60 degrees aside and 25 up or down.
        /// </summary>
        void Look(Vector3 target, float weight)
        {
            var to = target - lookBone.position;
            if (to.sqrMagnitude < 1e-4f) return;
            var local = transform.InverseTransformDirection(to.normalized);
            var yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -60f, 60f);
            var pitch = Mathf.Clamp(-Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg, -25f, 25f);
            var turn = transform.rotation * Quaternion.Euler(pitch, yaw, 0) * Quaternion.Inverse(transform.rotation);
            lookBone.rotation = Quaternion.Slerp(Quaternion.identity, turn, weight) * lookBone.rotation;
        }

        /// <summary>
        /// Two-bone reach over the animated pose: the upper arm and forearm turn so the hand meets the target, or comes
        /// as near as the arm allows, the elbow keeping the bend it had.
        /// </summary>
        void ReachArm(Vector3 target, float weight)
        {
            Transform upper = reachArm[0], lower = reachArm[1], hand = reachArm[2];
            if (upper == null || lower == null || hand == null) return;
            Vector3 a = upper.position, b = lower.position, c = hand.position;
            float l1 = Vector3.Distance(a, b), l2 = Vector3.Distance(b, c);
            if (l1 < 1e-4f || l2 < 1e-4f) return;
            var toTarget = target - a;
            var direction = toTarget.sqrMagnitude > 1e-6f ? toTarget.normalized : transform.forward;
            var d = Mathf.Clamp(toTarget.magnitude, Mathf.Abs(l1 - l2) + 0.01f, (l1 + l2) * 0.995f);
            var bend = Vector3.ProjectOnPlane(b - a, direction);
            if (bend.sqrMagnitude < 1e-6f) bend = Vector3.ProjectOnPlane(Vector3.down, direction);
            bend.Normalize();
            var cos = Mathf.Clamp((l1 * l1 + d * d - l2 * l2) / (2f * l1 * d), -1f, 1f);
            var elbow = a + direction * cos * l1 + bend * Mathf.Sqrt(1f - cos * cos) * l1;
            upper.rotation = Quaternion.Slerp(upper.rotation, Quaternion.FromToRotation(b - a, elbow - a) * upper.rotation, weight);
            b = lower.position;
            c = hand.position;
            lower.rotation = Quaternion.Slerp(lower.rotation, Quaternion.FromToRotation(c - b, a + direction * d - b) * lower.rotation, weight);
        }

        void UpdateArms(float dt, bool walking)
        {
            if (!hasArms) return;
            reachFor = Mathf.Max(0, reachFor - dt);
            presentFor = Mathf.Max(0, presentFor - dt);
            float neg, pos, inward;
            if (!StillOffering && reachFor <= 0) offered = null;
            if (reachFor > 0 || StillOffering)
            {
                neg = reachSide < 0 ? 0.85f : 0.1f;
                pos = reachSide > 0 ? 0.85f : 0.1f;
                inward = 0.15f;
            }
            else if (presentFor > 0)
            {
                neg = pos = 0.55f;
                inward = 0.7f;
            }
            else if (Holding)
            {
                neg = pos = 0.3f;
                inward = 0.75f;
            }
            else if (walking)
            {
                // A loose swing, opposite arms with each step.
                var swing = 0.09f * Mathf.Sin(travelled / stepLength * Mathf.PI);
                neg = swing;
                pos = -swing;
                inward = 0;
            }
            else neg = pos = inward = 0;
            var rate = dt / 0.45f;
            armNeg = Mathf.MoveTowards(armNeg, neg, rate);
            armPos = Mathf.MoveTowards(armPos, pos, rate);
            armIn = Mathf.MoveTowards(armIn, inward, rate);
        }

        /// <summary>Something it carries is in view and still in its hands.</summary>
        bool Holding
        {
            get
            {
                foreach (var o in carried)
                    if (o != null && o.Visible && o.CarriedBy == null && Vector3.Distance(o.transform.position, o.HomePosition) < 0.08f)
                        return true;
                return false;
            }
        }

        /// <summary>The same fold as the shader's kneel, so what hangs from the hands or the neck stays there.</summary>
        void FollowAnchors()
        {
            var h = Mathf.Max(height, 0.01f);
            var hip = 0.48f * h;
            var fold = 0.6f * Mathf.SmoothStep(0f, 1f, kneel);
            for (var i = 0; i < anchors.Length; i++)
            {
                if (anchors[i] == null) continue;
                var p = anchorRest[i];
                if (p.y >= hip)
                {
                    p.z += (p.y - hip) * 0.22f * Mathf.SmoothStep(0f, 1f, kneel);
                    p.y -= hip * fold;
                }
                else p.y *= 1f - fold;
                anchors[i].localPosition = p;
            }
        }

        void Apply()
        {
            if (body == null) return;
            body.GetPropertyBlock(block);
            block.SetFloat(KneelId, Mathf.SmoothStep(0f, 1f, kneel));
            block.SetFloat(HeightId, height);
            block.SetFloat(ArmNegId, armNeg);
            block.SetFloat(ArmPosId, armPos);
            block.SetFloat(ArmInId, armIn);
            block.SetFloat(ProceduralId, animator != null ? 0f : 1f);
            block.SetFloat(GroundId, basePosition.y);
            block.SetColor(ColorId, tint);
            block.SetFloat(SmokeId, bodySmoke);
            // Where the head begins this frame, standing or kneeling: a little above the base of the neck.
            block.SetFloat(CrownId, lookBone != null ? lookBone.position.y + 0.05f * height / 1.6f : 1000f);
            body.SetPropertyBlock(block);
            if (parts.Length == 0) return;
            block.SetFloat(SmokeId, partSmoke);
            var folded = Mathf.SmoothStep(0f, 1f, kneel);
            for (var i = 0; i < parts.Length; i++)
            {
                var c = parts[i];
                if (c == null) continue;
                c.enabled = body.enabled;
                c.SetPropertyBlock(block);
                if (i < partKneelScale.Length && partRest != null && i < partRest.Length)
                {
                    var rest = partRest[i];
                    c.transform.localScale = new Vector3(rest.x, rest.y * Mathf.Lerp(1f, partKneelScale[i], folded), rest.z);
                }
            }
        }
    }
}
