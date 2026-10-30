using System;
using System.Collections.Generic;
using System.Linq;
using Sankofa.Core.Personal;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>
    /// A pop-up paper theatre on the story table. Painted scenery stands up from a turntable when a telling begins,
    /// the two painted figures take their places and poses as the narration reaches each staged beat, and choosing a
    /// side turns the stage to show the other side of the same scene. Presentation only: it reads chapter ids and
    /// narration progress, never story state, and a chapter without staging simply holds the last arrangement.
    /// </summary>
    public sealed class PopUpStage : MonoBehaviour
    {
        [Serializable] public sealed class Figure
        {
            public string Id;
            public Transform Root, Hinge, Face;
            public Renderer Paint;
            /// <summary>Atlas column of this figure, (x, width); pose rows are taken from the top of the cast atlas.</summary>
            public Vector2 Column;
        }

        [SerializeField] TextAsset stagingData;
        [SerializeField] Transform turntable;
        [Tooltip("Hinges of the painted scenery, from the back of the stage forward; each stands up from lying flat.")]
        [SerializeField] Transform[] scenery = Array.Empty<Transform>();
        [SerializeField] Figure[] figures = Array.Empty<Figure>();
        [SerializeField] Transform gourd;

        const float FoldedAngle = 88f, RiseSeconds = 0.9f, FoldSeconds = 0.35f, SpinSeconds = 0.8f, Stagger = 0.06f;
        const float StepSeconds = 0.7f, PoseSeconds = 0.4f, WordsPerSecond = 2.3f;
        static readonly int FrameA = Shader.PropertyToID("_FrameA"), FrameB = Shader.PropertyToID("_FrameB");
        static readonly int PoseBlend = Shader.PropertyToID("_PoseBlend"), Visibility = Shader.PropertyToID("_Visibility"), Highlight = Shader.PropertyToID("_Highlight");

        enum Phase { Resting, Rising, Standing, Folding, Spinning }

        sealed class Actor
        {
            public Figure Figure;
            public bool Shown, Mirror;
            public Vector3 From, To;
            public float Step = 1, Rise, Pose, PreviousPose, Blend = 1, Highlight;
        }

        TableStaging staging;
        readonly Dictionary<string, Actor> actors = new Dictionary<string, Actor>();
        Vector3[] sceneryRest = Array.Empty<Vector3>();
        Vector3[] sceneryScale = Array.Empty<Vector3>();
        Renderer[] sceneryPaint = Array.Empty<Renderer>();
        MaterialPropertyBlock block;
        Phase phase = Phase.Resting;
        float rise, phaseTime, spinFrom, spinTo, clock, length = 1;
        bool mirrored, narrationDriven, paused, reducedMotion, waiting;
        TableStaging.Chapter chapter;
        int nextBeat;
        string gourdPlace = TableStaging.GourdHidden, gourdHolder;
        Vector3 gourdFrom, gourdTo;
        float gourdMove = 1;
        string[] offered = Array.Empty<string>();

        /// <summary>0 while the scenery lies flat on the turntable, 1 once it stands.</summary>
        public float Rise => rise;
        public bool Mirrored => mirrored;
        public bool Turning => phase == Phase.Folding || phase == Phase.Spinning;
        public string ChapterId => chapter?.Id;
        /// <summary>0..1 through the current chapter's narration.</summary>
        public float Progress => Mathf.Clamp01(clock / Mathf.Max(0.01f, length));
        public string GourdPlace => gourdPlace;
        public string GourdHolder => gourdHolder;
        public TableStaging Staging => staging ?? (staging = stagingData != null ? TableStaging.Parse(stagingData.text) : null);
        public bool FigureShown(string id) => actors.TryGetValue(id, out var a) && a.Shown;
        public int FigurePose(string id) => actors.TryGetValue(id, out var a) ? Mathf.RoundToInt(a.Pose) : -1;
        public bool FigureMirrored(string id) => actors.TryGetValue(id, out var a) && a.Mirror;
        /// <summary>Where a figure stands, in stage space, once any step under way has finished.</summary>
        public Vector3 FigurePlace(string id) => actors.TryGetValue(id, out var a) ? a.To : Vector3.zero;
        /// <summary>The figure's painted body in the world, for anything that must sit at her feet or face her.</summary>
        public Transform FigureRoot(string id) => actors.TryGetValue(id, out var a) ? a.Figure.Root : null;

        void Awake()
        {
            block = new MaterialPropertyBlock();
            foreach (var figure in figures.Where(f => f != null && f.Root != null))
                actors[figure.Id] = new Actor { Figure = figure, From = figure.Root.localPosition, To = figure.Root.localPosition };
            sceneryRest = scenery.Select(s => s != null ? s.localPosition : Vector3.zero).ToArray();
            sceneryScale = scenery.Select(s => s != null ? s.localScale : Vector3.one).ToArray();
            sceneryPaint = scenery.Select(s => s != null ? s.GetComponentInChildren<Renderer>(true) : null).ToArray();
            _ = Staging;
            Apply();
        }

        /// <summary>Between tellings the painted scene lies flat on the turntable and nobody stands on it.</summary>
        public void Rest()
        {
            chapter = null; nextBeat = 0; clock = 0; narrationDriven = false; offered = Array.Empty<string>();
            foreach (var actor in actors.Values) actor.Shown = false;
            SetGourd(TableStaging.GourdHidden, null, true);
            if (mirrored) { mirrored = false; if (turntable != null) turntable.localRotation = Quaternion.identity; }
            phase = Phase.Resting; phaseTime = 0;
            if (reducedMotion) rise = 0;
            Apply();
        }

        /// <summary>
        /// Performs one chapter. A new page restarts its staging; returning to the same page from a question or source
        /// keeps the arrangement as it was. The text estimates the chapter's length until narration reports its own.
        /// </summary>
        public void Perform(string chapterId, string text, bool newPage)
        {
            var next = Staging?.For(chapterId);
            if (!newPage && chapter != null && chapter.Id == chapterId) return;
            // A page turned mid-turn settles the turntable where its current side belongs before anything else moves.
            if (Turning) SetMirror(mirrored);
            chapter = next ?? new TableStaging.Chapter { Id = chapterId, Entry = TableStaging.Stay };
            nextBeat = 0; clock = 0; narrationDriven = false;
            var words = string.IsNullOrWhiteSpace(text) ? 0 : text.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).Length;
            length = Mathf.Max(4f, words / WordsPerSecond + 1.5f);
            offered = Array.Empty<string>();
            var wantMirror = chapter.Entry == TableStaging.Turn;
            if (wantMirror != mirrored && rise > 0.01f && !reducedMotion) { BeginPhase(Phase.Folding); return; }
            if (wantMirror != mirrored) SetMirror(wantMirror);
            Arrive(snap: rise < 0.01f || reducedMotion);
            if (phase != Phase.Standing) BeginPhase(reducedMotion ? Phase.Standing : Phase.Rising);
            if (reducedMotion) rise = 1;
            Apply();
        }

        /// <summary>Narration owns the clock while it plays, so beats land on the words they belong to.</summary>
        public void SetNarration(float seconds, float clipLength)
        {
            if (clipLength <= 0) return;
            narrationDriven = true; length = clipLength; clock = Mathf.Min(seconds, clipLength);
        }

        /// <summary>Lights the figures who can be chosen; touching one belongs to the view's figure plaques.</summary>
        public void OfferFigures(IReadOnlyList<string> ids) => offered = ids?.Where(id => id != null).ToArray() ?? Array.Empty<string>();

        public void SetPaused(bool value) => paused = value;

        /// <summary>While an answer, source or notice is open over the stage, the scene lies down and its clock waits.</summary>
        public void SetWaiting(bool value)
        {
            if (waiting == value) return;
            waiting = value;
            if (!value && chapter != null && (phase == Phase.Standing || phase == Phase.Rising)) BeginPhase(reducedMotion ? Phase.Standing : Phase.Rising);
            if (reducedMotion) { rise = value || chapter == null ? 0 : 1; foreach (var a in actors.Values) a.Rise = a.Shown && !value ? 1 : 0; Apply(); }
        }
        public bool Waiting => waiting;

        public void SetReducedMotion(bool value)
        {
            reducedMotion = value;
            if (!value) return;
            if (Turning) { SetMirror(!mirrored); Arrive(true); }
            phase = chapter == null ? Phase.Resting : Phase.Standing;
            rise = chapter == null ? 0 : 1;
            foreach (var a in actors.Values) { a.Step = 1; a.Blend = 1; a.Rise = a.Shown ? 1 : 0; }
            gourdMove = 1;
            Apply();
        }

        /// <summary>Moves the staging clock, as when the visitor reads ahead without narration.</summary>
        public void Seek(float progress) { clock = Mathf.Clamp01(progress) * length; Advance(); }

        void BeginPhase(Phase value) { phase = value; phaseTime = 0; if (value == Phase.Spinning) { spinFrom = mirrored ? 180 : 0; spinTo = spinFrom + 180; } }

        void SetMirror(bool value)
        {
            mirrored = value;
            if (turntable != null) turntable.localRotation = Quaternion.Euler(0, mirrored ? 180 : 0, 0);
        }

        /// <summary>Applies the beats that open the chapter and any it has already passed.</summary>
        void Arrive(bool snap)
        {
            var first = chapter.Beats.TakeWhile(b => b.At <= 0).Count();
            for (var i = 0; i < first; i++) Play(chapter.Beats[i], snap);
            nextBeat = first;
            Advance();
        }

        void Advance()
        {
            if (chapter == null) return;
            var progress = Progress;
            while (nextBeat < chapter.Beats.Count && chapter.Beats[nextBeat].At <= progress) Play(chapter.Beats[nextBeat++], reducedMotion);
        }

        void Play(TableStaging.Beat beat, bool snap)
        {
            if (beat.Who == TableStaging.Gourd) { SetGourd(beat.Place, beat.Holder, snap); return; }
            if (!actors.TryGetValue(beat.Who, out var actor)) return;
            if (beat.Shown.HasValue) actor.Shown = beat.Shown.Value;
            if (beat.Mirror.HasValue) actor.Mirror = beat.Mirror.Value;
            if (beat.X.HasValue || beat.Z.HasValue)
            {
                var to = new Vector3(beat.X.HasValue ? (float)beat.X.Value : actor.To.x, 0, beat.Z.HasValue ? (float)beat.Z.Value : actor.To.z);
                // Someone who is not yet standing takes her place before she rises; nobody slides in lying down.
                var jump = snap || actor.Rise < 0.05f;
                actor.From = jump ? to : actor.Figure.Root.localPosition; actor.To = to; actor.Step = jump ? 1 : 0;
            }
            if (beat.Pose.HasValue && beat.Pose.Value != Mathf.RoundToInt(actor.Pose))
            {
                actor.PreviousPose = actor.Pose; actor.Pose = beat.Pose.Value; actor.Blend = snap || actor.Rise < 0.05f ? 1 : 0;
            }
            if (reducedMotion) actor.Rise = actor.Shown ? 1 : 0;
        }

        void SetGourd(string place, string holder, bool snap)
        {
            gourdPlace = place; gourdHolder = place == TableStaging.GourdWith ? holder : null;
            gourdFrom = gourd != null ? gourd.localPosition : Vector3.zero; gourdTo = GourdTarget();
            gourdMove = snap ? 1 : 0;
        }

        Vector3 GourdTarget()
        {
            Vector3 Hand(Actor a) => a.To + new Vector3((a.Mirror ? -1 : 1) * 0.045f, 0.085f, -0.012f);
            switch (gourdPlace)
            {
                case TableStaging.GourdWith when gourdHolder != null && actors.TryGetValue(gourdHolder, out var holder): return Hand(holder);
                case TableStaging.GourdBetween:
                    var standing = actors.Values.Where(a => a.Shown).ToArray();
                    var middle = standing.Length == 0 ? Vector3.zero : standing.Aggregate(Vector3.zero, (s, a) => s + a.To) / standing.Length;
                    return new Vector3(middle.x, 0.012f, middle.z - 0.03f);
                case TableStaging.GourdLifted: return new Vector3(0, 0.16f, -0.04f);
                default: return gourd != null ? gourd.localPosition : Vector3.zero;
            }
        }

        void Update()
        {
            if (paused) return;
            var dt = Time.deltaTime;
            if (waiting && !Turning)
            {
                rise = Mathf.Max(0, rise - dt / FoldSeconds);
                foreach (var a in actors.Values) a.Rise = Mathf.Min(a.Rise, rise);
                Apply();
                return;
            }
            phaseTime += dt;
            switch (phase)
            {
                case Phase.Resting: rise = Mathf.Max(0, rise - dt / FoldSeconds); break;
                case Phase.Rising:
                    rise = Mathf.Min(1, rise + dt / RiseSeconds);
                    if (rise >= 1) phase = Phase.Standing;
                    break;
                case Phase.Folding:
                    rise = Mathf.Max(0, rise - dt / FoldSeconds);
                    foreach (var a in actors.Values) a.Rise = Mathf.Min(a.Rise, rise);
                    if (rise <= 0) BeginPhase(Phase.Spinning);
                    break;
                case Phase.Spinning:
                    var t = Mathf.Clamp01(phaseTime / SpinSeconds);
                    if (turntable != null) turntable.localRotation = Quaternion.Euler(0, Mathf.Lerp(spinFrom, spinTo, t * t * (3 - 2 * t)), 0);
                    if (t >= 1) { SetMirror(!mirrored); Arrive(true); foreach (var a in actors.Values) a.Rise = 0; BeginPhase(Phase.Rising); }
                    break;
            }
            if (chapter != null && !Turning)
            {
                if (!narrationDriven) clock = Mathf.Min(length, clock + dt);
                Advance();
            }
            foreach (var a in actors.Values)
            {
                var target = a.Shown && phase != Phase.Folding && phase != Phase.Spinning ? Mathf.Min(1, rise + 0.001f) : 0;
                a.Rise = Mathf.MoveTowards(a.Rise, target, dt / (target > a.Rise ? 0.5f : FoldSeconds));
                a.Step = Mathf.Min(1, a.Step + dt / StepSeconds);
                a.Blend = Mathf.Min(1, a.Blend + dt / PoseSeconds);
                var lit = Array.IndexOf(offered, a.Figure.Id) >= 0 && !Turning;
                a.Highlight = Mathf.MoveTowards(a.Highlight, lit ? 0.6f + 0.4f * Mathf.Sin(Time.time * 3f) : 0, dt * 3f);
            }
            if (gourdMove < 1) gourdMove = Mathf.Min(1, gourdMove + dt / StepSeconds);
            gourdTo = GourdTarget();
            Apply();
        }

        void Apply()
        {
            for (var i = 0; i < scenery.Length; i++)
            {
                if (scenery[i] == null) continue;
                // Back layers rise a moment before the front ones, as the paper of a pop-up book pulls them up.
                var standing = Ease(Mathf.Clamp01(rise * (1 + Stagger * (scenery.Length - 1) / RiseSeconds) - i * Stagger / RiseSeconds));
                scenery[i].localRotation = Quaternion.Euler(Mathf.Lerp(FoldedAngle, 0, standing), 0, 0);
                var rest = sceneryRest[i];
                scenery[i].localPosition = new Vector3(mirrored ? -rest.x : rest.x, rest.y, rest.z);
                var scale = sceneryScale[i];
                scenery[i].localScale = new Vector3(mirrored ? -scale.x : scale.x, scale.y, scale.z);
                SetPaint(sceneryPaint[i], Turning ? Mathf.Clamp01(rise * 3) : 1, 0);
            }
            foreach (var a in actors.Values)
            {
                var f = a.Figure;
                var step = Ease(a.Step);
                var at = Vector3.Lerp(a.From, a.To, step);
                // A paper figure on a track lifts a little as it walks.
                at.y = Mathf.Sin(step * Mathf.PI) * 0.008f * (a.Step < 1 ? 1 : 0);
                f.Root.localPosition = at;
                if (f.Hinge != null) f.Hinge.localRotation = Quaternion.Euler(Mathf.Lerp(FoldedAngle, 0, Ease(a.Rise)), 0, 0);
                if (f.Face != null) { var s = f.Face.localScale; f.Face.localScale = new Vector3((a.Mirror ? -1 : 1) * Mathf.Abs(s.x), s.y, s.z); }
                if (f.Paint == null) continue;
                f.Paint.GetPropertyBlock(block);
                block.SetVector(FrameA, Frame(f, a.PreviousPose)); block.SetVector(FrameB, Frame(f, a.Pose));
                block.SetFloat(PoseBlend, a.Blend); block.SetFloat(Visibility, a.Rise > 0.02f ? 1 : 0); block.SetFloat(Highlight, a.Highlight);
                f.Paint.SetPropertyBlock(block);
            }
            if (gourd != null)
            {
                gourd.gameObject.SetActive(gourdPlace != TableStaging.GourdHidden && rise > 0.5f && !Turning);
                var p = Vector3.Lerp(gourdFrom, gourdTo, Ease(gourdMove));
                p.y += Mathf.Sin(Ease(gourdMove) * Mathf.PI) * 0.03f * (gourdMove < 1 ? 1 : 0);
                gourd.localPosition = p;
                gourd.localRotation = gourdPlace == TableStaging.GourdLifted && !reducedMotion ? Quaternion.Euler(0, Time.time * 25f, 0) : Quaternion.identity;
            }
        }

        void SetPaint(Renderer paint, float visibility, float highlight)
        {
            if (paint == null) return;
            paint.GetPropertyBlock(block);
            block.SetFloat(Visibility, visibility); block.SetFloat(Highlight, highlight);
            paint.SetPropertyBlock(block);
        }

        static Vector4 Frame(Figure f, float pose)
        {
            var row = Mathf.Clamp(Mathf.RoundToInt(pose), 0, TableStaging.Poses - 1);
            return new Vector4(f.Column.x, 1f - (row + 1f) / TableStaging.Poses, f.Column.y, 1f / TableStaging.Poses);
        }

        static float Ease(float t) { t = Mathf.Clamp01(t); return t * t * (3 - 2 * t); }
    }
}
