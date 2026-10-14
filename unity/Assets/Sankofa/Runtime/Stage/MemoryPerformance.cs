using System;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    [Serializable] public sealed class MemoryMoment
    {
        public string Line;
        public Transform[] Targets;
        public Vector3[] Positions;
        public Vector3[] Scales;
        public float Seconds = 3;
        public Renderer Figure;
        public Vector4 Frame;
    }

    /// <summary>Small authored actions inside an imagined memory; no gesture, outcome or story-state authority.</summary>
    public sealed class MemoryPerformance : MonoBehaviour
    {
        [SerializeField] Transform[] actors;
        [SerializeField] MemoryMoment[] moments;
        [SerializeField] StoryTransition transition;
        Vector3[] restPositions, restScales, fromPositions, fromScales;
        MemoryMoment active;
        MaterialPropertyBlock paint;
        float elapsed;
        public string CurrentLine => active?.Line;
        public float Progress => active == null ? 0 : Mathf.Clamp01(elapsed / Mathf.Max(0.05f, active.Seconds));
        void Awake()
        {
            paint=new MaterialPropertyBlock();
            restPositions = new Vector3[actors.Length]; restScales = new Vector3[actors.Length];
            for(var i=0;i<actors.Length;i++){restPositions[i]=actors[i].localPosition;restScales[i]=actors[i].localScale;}
        }
        public void ResetState()
        {
            active=null;elapsed=0;
            if(paint!=null)foreach(var moment in moments)
                if(moment.Figure!=null)
                {
                    moment.Figure.GetPropertyBlock(paint);
                    var frame=moment.Figure.sharedMaterial.GetVector("_FrameA");
                    paint.SetVector("_FrameA",frame);paint.SetVector("_FrameB",frame);moment.Figure.SetPropertyBlock(paint);
                }
            if(restPositions==null)return;
            for(var i=0;i<actors.Length;i++){actors[i].localPosition=restPositions[i];actors[i].localScale=restScales[i];}
        }
        public void Speak(string line)
        {
            foreach(var moment in moments)
                if(moment.Line==line)
                {
                    active=moment;elapsed=0;
                    if(moment.Figure!=null)
                    {
                        moment.Figure.GetPropertyBlock(paint);paint.SetVector("_FrameA",moment.Frame);paint.SetVector("_FrameB",moment.Frame);
                        moment.Figure.SetPropertyBlock(paint);
                    }
                    fromPositions=new Vector3[moment.Targets.Length];fromScales=new Vector3[moment.Targets.Length];
                    for(var i=0;i<moment.Targets.Length;i++){fromPositions[i]=moment.Targets[i].localPosition;fromScales[i]=moment.Targets[i].localScale;}
                    if(transition.ReducedMotion){elapsed=moment.Seconds;Apply(1);}
                    return;
                }
        }
        void Update()
        {
            if(active==null||StageClock.Paused)return;
            elapsed=transition.ReducedMotion?active.Seconds:elapsed+StageClock.DeltaTime;
            Apply(Mathf.SmoothStep(0,1,Progress));
        }
        void Apply(float t)
        {
            for(var i=0;i<active.Targets.Length;i++)
            {
                active.Targets[i].localPosition=Vector3.Lerp(fromPositions[i],active.Positions[i],t);
                active.Targets[i].localScale=Vector3.Lerp(fromScales[i],active.Scales[i],t);
            }
        }
    }
}
