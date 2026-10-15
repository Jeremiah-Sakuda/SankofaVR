using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sankofa.Runtime.Stage
{
    /// <summary>The stationary pavilion releases into the painting. Presentation only; no story/input authority.</summary>
    public sealed class GalleryAtmosphere : MonoBehaviour
    {
        Renderer[] surfaces;
        Material[][] originals, fades;
        readonly List<Material> ownedMaterials=new List<Material>();
        TMP_Text[] labels;
        Color[] colours;
        MaterialPropertyBlock block;
        bool blending;
        public float Presence { get; private set; }=1;
        public void SetPresence(float value)
        {
            if(surfaces==null)
            {
                surfaces=GetComponentsInChildren<Renderer>(true).Where(r=>r.sharedMaterial!=null&&r.sharedMaterial.shader.name=="Sankofa/Gallery Surface").ToArray();
                originals=surfaces.Select(r=>r.sharedMaterials).ToArray();
                labels=GetComponentsInChildren<TMP_Text>(true);colours=labels.Select(t=>t.color).ToArray();
                block=new MaterialPropertyBlock();
            }
            Presence=Mathf.Clamp01(value);
            var fade=Presence<0.9999f;
            if(fade&&fades==null)
            {
                // Allocate once per shared material, only on the first transition. Nothing is saved to the scene.
                var copies=new Dictionary<Material,Material>();
                Material Copy(Material original)
                {
                    if(copies.TryGetValue(original,out var existing))return existing;
                    var material=new Material(original){name=original.name+" (gallery release)",hideFlags=HideFlags.DontSave};
                    material.SetInt("_SrcBlend",(int)BlendMode.SrcAlpha);material.SetInt("_DstBlend",(int)BlendMode.OneMinusSrcAlpha);
                    material.SetInt("_ZWrite",0);material.renderQueue=(int)RenderQueue.Transparent;
                    copies.Add(original,material);ownedMaterials.Add(material);return material;
                }
                fades=originals.Select(materials=>materials.Select(Copy).ToArray()).ToArray();
            }
            if(fade!=blending)
            {
                for(var i=0;i<surfaces.Length;i++)surfaces[i].sharedMaterials=fade?fades[i]:originals[i];
                blending=fade;
            }
            foreach(var surface in surfaces)
            {
                surface.GetPropertyBlock(block);block.SetFloat("_Visibility",Presence);surface.SetPropertyBlock(block);
            }
            for(var i=0;i<labels.Length;i++)
            {
                var colour=colours[i];colour.a*=Presence;labels[i].color=colour;
            }
        }
        void OnDestroy()
        {
            foreach(var material in ownedMaterials)if(material!=null)Destroy(material);
        }
    }
}
