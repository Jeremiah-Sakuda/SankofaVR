using System.Linq;
using TMPro;
using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>The stationary pavilion releases into the painting. Presentation only; no story/input authority.</summary>
    public sealed class GalleryAtmosphere : MonoBehaviour
    {
        Renderer[] surfaces;
        TMP_Text[] labels;
        Color[] colours;
        MaterialPropertyBlock block;
        public float Presence { get; private set; }=1;
        public void SetPresence(float value)
        {
            if(surfaces==null)
            {
                surfaces=GetComponentsInChildren<Renderer>(true).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.HasProperty("_Visibility")&&r.GetComponent<TMP_Text>()==null).ToArray();
                labels=GetComponentsInChildren<TMP_Text>(true);colours=labels.Select(t=>t.color).ToArray();
                block=new MaterialPropertyBlock();
            }
            Presence=Mathf.Clamp01(value);
            foreach(var surface in surfaces)
            {
                surface.GetPropertyBlock(block);block.SetFloat("_Visibility",Presence);surface.SetPropertyBlock(block);
            }
            for(var i=0;i<labels.Length;i++)
            {
                var colour=colours[i];colour.a*=Presence;labels[i].color=colour;
            }
        }
    }
}
