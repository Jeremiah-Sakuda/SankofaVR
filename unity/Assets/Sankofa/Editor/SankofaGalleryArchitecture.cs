using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        // A contemporary pavilion, not a reconstruction: real apertures, layered depth and a quiet seated focus.
        static void BuildGalleryArchitecture(Transform root)
        {
            var plaster=GallerySurface("Limestone plaster",new Color(0.43f,0.39f,0.31f),0,0.7f);
            var dark=GallerySurface("Shadow reveals",new Color(0.038f,0.045f,0.043f));
            var timber=GallerySurface("Smoked walnut",new Color(0.26f,0.15f,0.078f),1,0.2f);
            var ceiling=GallerySurface("Ceiling mineral",new Color(0.13f,0.16f,0.17f));
            var stone=GallerySurface("Honed stone",new Color(0.23f,0.235f,0.22f),2);
            var bronze=GallerySurface("Brushed bronze",new Color(0.40f,0.26f,0.12f),3);
            var glow=GallerySurface("Warm cove",new Color(0.52f,0.34f,0.17f),4);
            Prim(root,"Stone floor",PrimitiveType.Cube,new Vector3(0,-0.07f,-0.1f),new Vector3(9.2f,0.12f,8.2f),stone);
            // Freestanding exhibit wall: the garden is visible around it, rather than ending at a box corner.
            Prim(root,"Exhibit wall",PrimitiveType.Cube,new Vector3(0,1.78f,3.42f),new Vector3(6.85f,3.58f,0.24f),dark);
            Prim(root,"Plaster exhibit recess",PrimitiveType.Cube,new Vector3(0,1.78f,3.33f),new Vector3(6.65f,3.48f,0.12f),plaster);
            Prim(root,"Exhibit cove reveal",PrimitiveType.Cube,new Vector3(0,3.52f,3.13f),new Vector3(6.85f,0.10f,0.23f),dark);
            Prim(root,"Exhibit cove light",PrimitiveType.Cube,new Vector3(0,3.47f,3.17f),new Vector3(6.65f,0.018f,0.028f),glow);
            Prim(root,"Exhibit floor reveal",PrimitiveType.Cube,new Vector3(0,0.07f,3.24f),new Vector3(6.75f,0.06f,0.07f),dark);

            var ribs=new List<CombineInstance>();
            for(var z=-3.7f;z<=3.7f;z+=1.48f)
                ribs.Add(Part(GalleryArch(4.30f,2.25f,1.75f,0.15f,0.15f),new Vector3(0,0,z),Quaternion.identity,Vector3.one));
            var ribMesh=Static(root,"Vault timber ribs",Combine(ribs,"Gallery pavilion vault ribs"),timber);
            // Two curved roof leaves leave a narrow real opening to the sky along the spine.
            var roofMesh=Static(root,"Vault roof leaves",GalleryVault(),ceiling);
            var arcades=new List<CombineInstance>();
            foreach(var sign in new[]{-1f,1f})
            {
                var x=sign*4.45f;
                for(var z=-2.45f;z<=2.45f;z+=2.45f)
                    arcades.Add(Part(GalleryArch(1.05f,1.45f,0.65f,0.16f,0.30f),new Vector3(x,0,z),Quaternion.Euler(0,90,0),Vector3.one));
                Prim(root,"Arcade low wall",PrimitiveType.Cube,new Vector3(x,0.27f,0),new Vector3(0.32f,0.54f,7.6f),plaster);
                Prim(root,"Arcade sill",PrimitiveType.Cube,new Vector3(x,0.56f,0),new Vector3(0.44f,0.065f,7.6f),bronze);
                Prim(root,"Arcade warm edge",PrimitiveType.Cube,new Vector3(x-sign*0.18f,0.59f,0),new Vector3(0.018f,0.018f,7.5f),glow);
                // Curved ribs meet a continuous timber spring beam; no floating architectural pieces.
                Prim(root,"Vault spring beam",PrimitiveType.Cube,new Vector3(x,2.28f,0),new Vector3(0.20f,0.18f,7.9f),timber);
            }
            var arcadeMesh=Static(root,"Garden arcades",Combine(arcades,"Gallery garden arcades"),plaster);
            var rear=new List<CombineInstance>{Part(GalleryArch(3.3f,1.85f,1.5f,0.22f,0.32f),new Vector3(0,0,-3.86f),Quaternion.identity,Vector3.one)};
            var rearMesh=Static(root,"Rear garden arch",Combine(rear,"Gallery rear garden arch"),plaster);
            Prim(root,"Rear bench seat",PrimitiveType.Cube,new Vector3(0,0.44f,-3.12f),new Vector3(2.2f,0.08f,0.45f),timber);
            foreach(var x in new[]{-0.82f,0.82f})
                Prim(root,"Rear bench stone foot",PrimitiveType.Cube,new Vector3(x,0.20f,-3.12f),new Vector3(0.14f,0.4f,0.34f),stone);
            Prim(root,"Exhibit ledge",PrimitiveType.Cube,new Vector3(0,0.66f,0.55f),new Vector3(1.22f,0.035f,0.28f),timber);
            Prim(root,"Ledge bronze underside",PrimitiveType.Cube,new Vector3(0,0.637f,0.55f),new Vector3(1.18f,0.009f,0.25f),bronze);
            foreach(var x in new[]{-0.50f,0.50f})
                Prim(root,"Ledge support",PrimitiveType.Cube,new Vector3(x,0.31f,0.63f),new Vector3(0.035f,0.62f,0.14f),timber);
            SaveMeshes(new[]{ribMesh,roofMesh,arcadeMesh,rearMesh});
            BuildGalleryGarden(root);
        }

        static void BuildGalleryGarden(Transform root)
        {
            var art=SankofaArt.Ensure();
            var earth=GallerySurface("Courtyard earth",new Color(0.045f,0.065f,0.055f));
            Prim(root,"Garden ground",PrimitiveType.Cube,new Vector3(0,-0.20f,0),new Vector3(44,0.14f,44),earth);
            var skyPath=GalleryFolder+"/Pavilion dusk.mat";
            var sky=AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if(sky==null){sky=new Material(art.Sky);AssetDatabase.CreateAsset(sky,skyPath);}
            sky.SetColor("_Zenith",new Color(0.015f,0.032f,0.075f));
            sky.SetColor("_Horizon",new Color(0.13f,0.19f,0.24f));
            sky.SetColor("_GlowColor",new Color(0.12f,0.055f,0.025f));
            sky.SetVector("_GlowDir",new Vector4(-1,0,-1,0));sky.SetFloat("_ShowMoon",0);
            EditorUtility.SetDirty(sky);
            Prim(root,"Pavilion dusk sky",PrimitiveType.Sphere,Vector3.zero,Vector3.one*160,sky);
            var trunks=new List<CombineInstance>();var canopies=new List<CombineInstance>();
            for(var i=0;i<8;i++)
            {
                var sign=i<4?-1f:1f;var z=-7f+(i%4)*4.7f;
                var at=new Vector3(sign*(7.3f+(i%2)*1.8f),-0.15f,z);
                SankofaMeshes.Tree(8300+i,4.8f+(i%3)*0.6f,out var trunk,out var canopy);
                trunks.Add(Part(trunk,at,Quaternion.Euler(0,i*61,0),Vector3.one));
                canopies.Add(Part(canopy,at,Quaternion.Euler(0,i*61,0),Vector3.one));
            }
            var bark=GallerySurface("Courtyard trunks",new Color(0.08f,0.105f,0.10f),1);
            var leavesPath=GalleryFolder+"/Courtyard leaves.mat";
            var leaves=AssetDatabase.LoadAssetAtPath<Material>(leavesPath);
            if(leaves==null){leaves=new Material(art.TreeCanopy);AssetDatabase.CreateAsset(leaves,leavesPath);}
            leaves.SetColor("_Color",new Color(0.14f,0.25f,0.23f));leaves.SetFloat("_UseBake",0);
            leaves.SetColor("_Emission",new Color(0.012f,0.022f,0.035f));EditorUtility.SetDirty(leaves);
            var woodMesh=Static(root,"Courtyard tree trunks",Combine(trunks,"Gallery courtyard trunks"),bark);
            var leafMesh=Static(root,"Courtyard tree leaves",Combine(canopies,"Gallery courtyard leaves"),leaves);
            var shrubs=new List<CombineInstance>();
            for(var i=0;i<48;i++)
            {
                var sign=i<24?-1f:1f;var z=-4+(i%24)*0.35f;
                shrubs.Add(Part(SankofaMeshes.FoliageCluster(8500+i),new Vector3(sign*(5.65f+(i%3)*0.24f),0.34f,z),Quaternion.Euler(0,i*37,0),new Vector3(0.55f,0.60f,0.55f)));
            }
            var shrubMesh=Static(root,"Courtyard planting",Combine(shrubs,"Gallery courtyard planting"),leaves);
            var horizon=Static(root,"Courtyard horizon",GalleryHorizon(),GallerySurface("Courtyard distant hills",new Color(0.055f,0.095f,0.12f)));
            var stars=Static(root,"Courtyard stars",SankofaMeshes.Stars(420,8310,65),art.Stars);
            stars.sharedMesh.name="Gallery courtyard stars";
            SaveMeshes(new[]{woodMesh,leafMesh,shrubMesh,horizon,stars});
        }

        static Mesh GalleryHorizon()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            for(var i=0;i<=96;i++)
            {
                var angle=i*Mathf.PI*2/96;var direction=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle));
                var height=1.55f+0.55f*Mathf.Sin(angle*4)+0.35f*Mathf.Cos(angle*7);
                vertices.Add(direction*26+Vector3.down*0.2f);vertices.Add(direction*26+Vector3.up*height);
                uv.Add(new Vector2(i/96f,0));uv.Add(new Vector2(i/96f,1));
                if(i<96){var n=i*2;triangles.AddRange(new[]{n,n+1,n+3,n,n+3,n+2});}
            }
            var mesh=new Mesh{name="Gallery distant garden horizon"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        // A genuinely open, extruded arch. Keeping the opening geometric gives stereo/head-lean parallax.
        static Mesh GalleryArch(float half,float spring,float rise,float thickness,float depth)
        {
            var inner=new List<Vector2>{new Vector2(-half,0)};
            var outer=new List<Vector2>{new Vector2(-half-thickness,0)};
            for(var i=0;i<=32;i++)
            {
                var angle=Mathf.PI-i*Mathf.PI/32;
                inner.Add(new Vector2(Mathf.Cos(angle)*half,spring+Mathf.Sin(angle)*rise));
                outer.Add(new Vector2(Mathf.Cos(angle)*(half+thickness),spring+Mathf.Sin(angle)*(rise+thickness)));
            }
            inner.Add(new Vector2(half,0));outer.Add(new Vector2(half+thickness,0));
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                var n=vertices.Count;vertices.AddRange(new[]{a,b,c,d});
                uv.AddRange(new[]{Vector2.zero,Vector2.up,Vector2.one,Vector2.right});
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3});
            }
            Vector3 P(Vector2 v,float z)=>new Vector3(v.x,v.y,z);
            for(var i=0;i<inner.Count-1;i++)
            {
                var a=P(inner[i],-depth/2);var b=P(inner[i+1],-depth/2);
                var c=P(outer[i+1],-depth/2);var d=P(outer[i],-depth/2);
                var e=P(inner[i],depth/2);var f=P(inner[i+1],depth/2);
                var g=P(outer[i+1],depth/2);var h=P(outer[i],depth/2);
                Quad(d,c,b,a);Quad(e,f,g,h);Quad(a,b,f,e);Quad(h,g,c,d);
            }
            var mesh=new Mesh{name="Gallery arch construction"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);
            mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }

        static Mesh GalleryVault()
        {
            var vertices=new List<Vector3>();var triangles=new List<int>();var uv=new List<Vector2>();
            foreach(var side in new[]{-1f,1f})
            for(var i=0;i<32;i++)
            {
                var a=0.16f+i*(Mathf.PI/2-0.16f)/32;var b=0.16f+(i+1)*(Mathf.PI/2-0.16f)/32;
                Vector3 P(float t,float z)=>new Vector3(side*Mathf.Sin(t)*4.45f,2.25f+Mathf.Cos(t)*1.9f,z);
                var n=vertices.Count;vertices.AddRange(new[]{P(a,-4),P(a,4),P(b,4),P(b,-4)});
                uv.AddRange(new[]{new Vector2(a,0),new Vector2(a,1),new Vector2(b,1),new Vector2(b,0)});
                // Both faces are explicit; no transparent skylight or double-sided material pass.
                triangles.AddRange(new[]{n,n+1,n+2,n,n+2,n+3,n+2,n+1,n,n+3,n+2,n});
            }
            var mesh=new Mesh{name="Gallery curved roof leaves"};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetUVs(0,uv);
            // Coplanar reverse faces cancel recalculated normals; give the underside a stable inward normal.
            var normals=new List<Vector3>();foreach(var v in vertices)normals.Add(new Vector3(-v.x/4.45f,-(v.y-2.25f)/1.9f,0).normalized);
            mesh.SetNormals(normals);mesh.RecalculateBounds();return mesh;
        }

        static Material GallerySurface(string name,Color colour,float finish=0,float wash=0)
        {
            var path=GalleryFolder+"/"+name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Sankofa/Gallery Surface"));AssetDatabase.CreateAsset(material,path);}
            material.shader=Shader.Find("Sankofa/Gallery Surface");material.SetColor("_Color",colour);
            material.SetFloat("_Finish",finish);material.SetFloat("_Wash",wash);material.SetFloat("_Visibility",1);
            material.SetTexture("_Noise",AssetDatabase.LoadAssetAtPath<Texture2D>(SankofaArt.NoisePath));
            EditorUtility.SetDirty(material);return material;
        }
    }
}
