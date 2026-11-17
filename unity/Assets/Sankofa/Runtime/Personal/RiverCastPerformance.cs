using System;
using System.Linq;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>Two visual treatments of the same accepted stage cues. Never chooses a chapter or advances the story.</summary>
    [DefaultExecutionOrder(750)]
    public sealed class RiverCastPerformance : MonoBehaviour
    {
        [SerializeField] RiverThresholdPrototype prototype;
        [SerializeField] Transform[] actors = Array.Empty<Transform>();
        [SerializeField] Renderer[] scenery = Array.Empty<Renderer>();
        [SerializeField] bool solid;
        Animator[] animators;
        SkinnedMeshRenderer[][] skins;
        Transform[] bodies, rightFeet, leftFeet;
        Transform[] heads, rightArms, rightForearms, leftArms;
        SkinnedMeshRenderer[] eyes;
        Transform[] poseBones;
        Vector3[] heldPositions;
        Quaternion[] heldRotations;
        bool hasPose;
        MaterialPropertyBlock block;
        sealed class PaintedActor
        {
            public Renderer Paint;
            public readonly MaterialPropertyBlock Properties = new MaterialPropertyBlock();
            public bool Placed, Shown, Mirrored;
            public Vector3 Place;
            public int PreviousPose, Pose;
            public float PoseAt, Blend = 1, Focus;
        }
        PaintedActor[] portraits = Array.Empty<PaintedActor>();
        object paintedStory;
        const float PortraitChangeSeconds = .4f;
        public bool Solid => solid;
        public Transform[] Actors => actors;
        public float AppliedClock { get; private set; }
        public bool MotionAllowed { get; private set; }
        /// <summary>The generated riverbank surface; portrait feet follow its height rather than hovering above it.</summary>
        public static float GroundHeight(float x, float z) => -.055f + .022f * Mathf.Sin(x * 1.7f) * Mathf.Sin(z * 1.4f);

        void Awake()
        {
            block=new MaterialPropertyBlock();
            animators=actors.Select(a=>a.GetComponentInChildren<Animator>(true)).ToArray();
            skins=actors.Select(a=>a.GetComponentsInChildren<SkinnedMeshRenderer>(true)).ToArray();
            Transform[] Bones(string name) => actors.Select(a=>a.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name==name)).ToArray();
            heads=Bones("Head"); rightArms=Bones("upperarm_r"); rightForearms=Bones("lowerarm_r"); leftArms=Bones("upperarm_l");
            bodies=Bones("Body"); rightFeet=Bones("foot_r"); leftFeet=Bones("foot_l");
            eyes=actors.Select(a=>a.GetComponentsInChildren<SkinnedMeshRenderer>(true).FirstOrDefault(r=>r.name=="Eyes")).ToArray();
            foreach(var a in animators) if(a!=null) { a.speed=0; a.applyRootMotion=false; a.cullingMode=AnimatorCullingMode.AlwaysAnimate; }
            poseBones=actors.SelectMany(a=>a.GetComponentsInChildren<Transform>(true)).ToArray();
            heldPositions=new Vector3[poseBones.Length]; heldRotations=new Quaternion[poseBones.Length];
            if (!solid) portraits = actors.Select(a => new PaintedActor { Paint = a.GetComponent<Renderer>() }).ToArray();
        }
        void LateUpdate()
        {
            if(prototype.App?.View?.Stage==null) return;
            if(prototype.App.Session.Paused)
            {
                // Unity can reapply its zero-speed base pose before LateUpdate; retain the fully composed gesture.
                if(hasPose) for(var b=0;b<poseBones.Length;b++) { poseBones[b].localPosition=heldPositions[b]; poseBones[b].localRotation=heldRotations[b]; }
                for(var i=0;i<actors.Length;i++) foreach(var skin in skins[i]) skin.enabled=actors[i].gameObject.activeSelf;
                // The prototype copies the miniature's paint every LateUpdate, including its folded visibility.
                // Hold the river's composed properties as well as its transforms while story time is paused.
                foreach (var portrait in portraits) ApplyPortraitPaint(portrait);
                return;
            }
            var stage=prototype.App.View.Stage;
            var previousClock = AppliedClock;
            AppliedClock=prototype.PaintClock; MotionAllowed=!prototype.Sequence.ReducedMotion;
            var elapsed = Mathf.Max(0, AppliedClock - previousClock);
            if (!solid && !ReferenceEquals(paintedStory, prototype.App.Session.Story))
            {
                paintedStory = prototype.App.Session.Story;
                foreach (var portrait in portraits) { portrait.Placed = false; portrait.Shown = false; }
            }
            foreach(var r in scenery)
            {
                r.GetPropertyBlock(block); block.SetFloat("_Clock",AppliedClock); block.SetFloat("_Motion",MotionAllowed ? 1 : 0); r.SetPropertyBlock(block);
            }
            for(var i=0;i<actors.Length;i++)
            {
                var id=i==0 ? "wambui" : "naserian";
                if (!solid) { PaintPortrait(i, id, stage, elapsed); continue; }
                var shown=prototype.RiverVisible && stage.FigureShown(id);
                if(actors[i].gameObject.activeSelf!=shown) actors[i].gameObject.SetActive(shown);
                if(!shown) continue;
                var source=stage.FigureRoot(id);
                var at=source!=null ? source.localPosition : stage.FigurePlace(id);
                var x=at.x*4.5f-.45f; var z=2.55f+at.z*3;
                actors[i].localPosition=new Vector3(x,-.025f,z);
                actors[i].localRotation=Quaternion.Euler(0,180+(i==0 ? -12 : 12),0);
                var a=animators[i];
                if(a==null) continue;
                // Re-sample the base before additive gestures: no accumulating bone rotation and no independent time.
                bodies[i].localPosition=Vector3.zero;
                a.Play("Idle",0,MotionAllowed ? AppliedClock*.24f+i*.37f : .2f); a.Update(0);
                // Imported clip offsets must not move the actor away from the stage's accepted position.
                var footCentre=(rightFeet[i].position+leftFeet[i].position)*.5f;
                bodies[i].position+=actors[i].position+actors[i].up*.07f-footCentre;
                // This adapter owns cast visibility; imported animation is presentation only.
                foreach(var skin in skins[i]) skin.enabled=true;
                var pose=stage.FigurePose(id);
                if(pose==1)
                {
                    rightArms[i].localRotation*=Quaternion.Euler(-18,0,-32);
                    rightForearms[i].localRotation*=Quaternion.Euler(0,0,-45);
                    leftArms[i].localRotation*=Quaternion.Euler(0,0,12);
                }
                else if(pose==2) heads[i].localRotation*=Quaternion.Euler(8,i==0 ? 14 : -14,0);
                if(eyes[i]!=null && eyes[i].sharedMesh.blendShapeCount>0)
                {
                    var phase=Mathf.Repeat(AppliedClock+i*1.37f,4.8f);
                    eyes[i].SetBlendShapeWeight(0,MotionAllowed ? Mathf.Clamp01(1-Mathf.Abs(phase-.12f)/.12f)*100 : 0);
                }
            }
            for(var b=0;b<poseBones.Length;b++) { heldPositions[b]=poseBones[b].localPosition; heldRotations[b]=poseBones[b].localRotation; }
            hasPose=true;
        }

        void PaintPortrait(int index, string id, PopUpStage stage, float elapsed)
        {
            var portrait = portraits[index];
            // Authored absence still matters (for example the documented naming page). A miniature folding
            // is only its presentation transition, however, and must not erase a person beside the river.
            portrait.Shown = prototype.RiverVisible && stage.FigureShown(id);
            if (actors[index].gameObject.activeSelf != portrait.Shown) actors[index].gameObject.SetActive(portrait.Shown);
            if (!portrait.Shown) return;
            var pose = Mathf.Clamp(stage.FigurePose(id), 0, 2);
            var followed = prototype.App.Session.Followed;
            string figure = null;
            if (followed != null && stage.Staging != null) stage.Staging.ChoiceFigures.TryGetValue(followed.Id, out figure);
            var focus = figure == null ? .35f : figure == id ? 1 : 0;
            if (!portrait.Placed)
            {
                // Keep the first accepted arrangement for this telling. The miniature's later mirrored tracks
                // describe turning a paper theatre, not people walking through one another in a full-size room.
                var at = stage.FigurePlace(id);
                var x = at.x * 4.5f - .45f; var z = 2.55f + at.z * 3;
                // All six approved portraits place the opaque foot within 1.4% of the texture's lower edge.
                portrait.Place = new Vector3(x, .805f + GroundHeight(x, z), z);
                portrait.Mirrored = stage.FigureMirrored(id);
                portrait.PreviousPose = portrait.Pose = pose; portrait.Blend = 1;
                portrait.PoseAt = AppliedClock - PortraitChangeSeconds; portrait.Focus = focus;
                portrait.Placed = true;
            }
            if (portrait.Pose != pose)
            {
                portrait.PreviousPose = portrait.Blend < .5f ? portrait.PreviousPose : portrait.Pose;
                portrait.Pose = pose; portrait.PoseAt = AppliedClock;
            }
            portrait.Blend = MotionAllowed ? Mathf.SmoothStep(0, 1, (AppliedClock - portrait.PoseAt) / PortraitChangeSeconds) : 1;
            portrait.Focus = MotionAllowed ? Mathf.MoveTowards(portrait.Focus, focus, elapsed / PortraitChangeSeconds) : focus;
            actors[index].localPosition = portrait.Place;
            actors[index].localScale = new Vector3(portrait.Mirrored ? -1.107f : 1.107f, 1.66f, 1);
            actors[index].localRotation = Quaternion.Euler(0, index == 0 ? -5 : 6, 0);
            ApplyPortraitPaint(portrait);
        }

        static Vector4 PortraitFrame(int pose) => new Vector4(0, 1f - (pose + 1f) / 3f, .5f, 1f / 3f);
        static void ApplyPortraitPaint(PaintedActor portrait)
        {
            if (portrait.Paint == null || !portrait.Placed) return;
            var paint = portrait.Properties;
            paint.SetVector("_FrameA", PortraitFrame(portrait.PreviousPose));
            paint.SetVector("_FrameB", PortraitFrame(portrait.Pose));
            paint.SetFloat("_PoseBlend", portrait.Blend); paint.SetFloat("_Highlight", portrait.Focus);
            paint.SetFloat("_Visibility", portrait.Shown ? 1 : 0); paint.SetFloat("_Unfold", 1);
            paint.SetFloat("_RiverDissolve", 1);
            portrait.Paint.SetPropertyBlock(paint);
        }
    }
}
