using UnityEditor;
using UnityEngine;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        // The room belongs to the hearth presentation, so the entire surround withdraws on story entry.
        // No colliders, dynamic lights or navigation: the painting and the seated ledge remain the only destinations.
        static void BuildGalleryArchitecture(Transform root)
        {
            var plaster=GallerySurface("Limestone plaster",new Color(0.46f,0.39f,0.29f),0,0.8f);
            var dark=GallerySurface("Shadow reveals",new Color(0.038f,0.045f,0.043f));
            var timber=GallerySurface("Smoked walnut",new Color(0.21f,0.13f,0.073f),1,0.20f);
            var ceiling=GallerySurface("Ceiling mineral",new Color(0.16f,0.165f,0.15f));
            var stone=GallerySurface("Honed stone",new Color(0.19f,0.18f,0.155f),2);
            var bronze=GallerySurface("Brushed bronze",new Color(0.40f,0.26f,0.12f),3);
            var glow=GallerySurface("Warm cove",new Color(0.62f,0.39f,0.18f),4);
            Prim(root,"Stone floor",PrimitiveType.Cube,new Vector3(0,-0.07f,-0.1f),new Vector3(9.2f,0.12f,7.5f),stone);
            Prim(root,"Exhibit wall",PrimitiveType.Cube,new Vector3(0,1.85f,3.4f),new Vector3(9.2f,3.8f,0.18f),dark);
            Prim(root,"Plaster exhibit recess",PrimitiveType.Cube,new Vector3(0,1.83f,3.34f),new Vector3(7.9f,3.52f,0.12f),plaster);
            Prim(root,"Ceiling",PrimitiveType.Cube,new Vector3(0,3.78f,-0.1f),new Vector3(9.2f,0.16f,7.5f),ceiling);
            Prim(root,"Rear wall",PrimitiveType.Cube,new Vector3(0,1.85f,-3.8f),new Vector3(9.2f,3.8f,0.16f),plaster);
            foreach(var x in new[]{-4.5f,4.5f})
            {
                Prim(root,"Side wall",PrimitiveType.Cube,new Vector3(x,1.85f,-0.1f),new Vector3(0.16f,3.8f,7.5f),dark);
                Prim(root,"Timber side inset",PrimitiveType.Cube,new Vector3(x*0.987f,1.67f,-0.2f),new Vector3(0.06f,3.1f,6.7f),timber);
                Prim(root,"Side cove",PrimitiveType.Cube,new Vector3(x*0.973f,3.36f,-0.15f),new Vector3(0.018f,0.025f,6.8f),glow);
                Prim(root,"Stone skirting",PrimitiveType.Cube,new Vector3(x*0.98f,0.1f,-0.15f),new Vector3(0.10f,0.18f,7.0f),stone);
                // Wide timber bays and fine bronze joints give depth without a dense slat/draw-call field.
                for(var z=-3f;z<=3;z+=1.2f)
                    Prim(root,"Timber bay joint",PrimitiveType.Cube,new Vector3(x*0.975f,1.67f,z),new Vector3(0.025f,3.1f,0.016f),bronze);
                Prim(root,"Exhibit timber pier",PrimitiveType.Cube,new Vector3(x*0.937f,1.8f,3.20f),new Vector3(0.40f,3.6f,0.20f),timber);
                Prim(root,"Exhibit vertical light",PrimitiveType.Cube,new Vector3(x*0.887f,1.83f,3.265f),new Vector3(0.016f,3.38f,0.022f),glow);
            }
            Prim(root,"Exhibit cove reveal",PrimitiveType.Cube,new Vector3(0,3.56f,3.15f),new Vector3(8.05f,0.10f,0.22f),dark);
            Prim(root,"Exhibit cove light",PrimitiveType.Cube,new Vector3(0,3.51f,3.18f),new Vector3(7.9f,0.018f,0.028f),glow);
            Prim(root,"Exhibit floor reveal",PrimitiveType.Cube,new Vector3(0,0.07f,3.24f),new Vector3(8.0f,0.06f,0.07f),dark);
            for(var z=-3.1f;z<3.1f;z+=1.05f)
                Prim(root,"Ceiling timber rib",PrimitiveType.Cube,new Vector3(0,3.65f,z),new Vector3(8.85f,0.11f,0.12f),timber);
            // Quiet rear seating gives scale when looking back, without suggesting another destination or exhibit.
            Prim(root,"Rear bench seat",PrimitiveType.Cube,new Vector3(0,0.44f,-3.12f),new Vector3(2.2f,0.08f,0.45f),timber);
            foreach(var x in new[]{-0.82f,0.82f})
                Prim(root,"Rear bench stone foot",PrimitiveType.Cube,new Vector3(x,0.20f,-3.12f),new Vector3(0.14f,0.4f,0.34f),stone);
            Prim(root,"Rear wall reveal",PrimitiveType.Cube,new Vector3(0,0.10f,-3.68f),new Vector3(9,0.16f,0.05f),dark);
            Prim(root,"Rear cove",PrimitiveType.Cube,new Vector3(0,3.36f,-3.69f),new Vector3(8.8f,0.022f,0.02f),glow);
            // A low, open-backed walnut ledge makes the controls belong to furniture without blocking a reach.
            Prim(root,"Exhibit ledge",PrimitiveType.Cube,new Vector3(0,0.66f,0.55f),new Vector3(1.22f,0.035f,0.28f),timber);
            Prim(root,"Ledge bronze underside",PrimitiveType.Cube,new Vector3(0,0.637f,0.55f),new Vector3(1.18f,0.009f,0.25f),bronze);
            foreach(var x in new[]{-0.50f,0.50f})
                Prim(root,"Ledge support",PrimitiveType.Cube,new Vector3(x,0.31f,0.63f),new Vector3(0.035f,0.62f,0.14f),timber);
        }

        static Material GallerySurface(string name,Color colour,float finish=0,float wash=0)
        {
            var path=GalleryFolder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Sankofa/Gallery Surface"));AssetDatabase.CreateAsset(material,path);}
            material.shader=Shader.Find("Sankofa/Gallery Surface");material.SetColor("_Color",colour);
            material.SetFloat("_Finish",finish);material.SetFloat("_Wash",wash);
            material.SetTexture("_Noise",AssetDatabase.LoadAssetAtPath<Texture2D>(SankofaArt.NoisePath));
            EditorUtility.SetDirty(material);return material;
        }
    }
}
