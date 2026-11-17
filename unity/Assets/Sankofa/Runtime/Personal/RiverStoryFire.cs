using Sankofa.Runtime.Fire;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>Presentation only: the established narrator supplies state, and the prototype supplies paused time.</summary>
    [DefaultExecutionOrder(700)]
    public sealed class RiverStoryFire : MonoBehaviour
    {
        [SerializeField] HearthFire narrator;
        [SerializeField] Renderer[] tongues;
        RiverThresholdPrototype prototype;
        MaterialPropertyBlock block;
        public Renderer[] Tongues => tongues;
        void Awake() { prototype=FindFirstObjectByType<RiverThresholdPrototype>(); block=new MaterialPropertyBlock(); }
        void LateUpdate()
        {
            if(prototype==null) return;
            for(var i=0;i<tongues.Length;i++)
            {
                tongues[i].GetPropertyBlock(block);
                block.SetFloat("_Clock",prototype.PaintClock);
                block.SetFloat("_Motion",prototype.Sequence.ReducedMotion ? 0 : 1);
                block.SetFloat("_Presence",Mathf.Clamp(.72f+narrator.Current.Intensity*.24f,.75f,1.1f));
                block.SetFloat("_Seed",i*1.79f);
                tongues[i].SetPropertyBlock(block);
            }
        }
    }
}
