using System.Collections.Generic;
using Sankofa.Runtime.Fire;
using Sankofa.Runtime.Personal;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        // These replacements are made only in the copied prototype scene. Original comparison assets stay intact.
        static void BuildRiverStoryObjects(StoryTableView view)
        {
            const string folder="Assets/Sankofa/Art/RiverThreshold/";
            foreach(var filename in new[]{"painted-surfaces-v1.png","painted-fire-study-v1.png"})
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(folder+filename);
                importer.maxTextureSize=2048; importer.mipmapEnabled=true; importer.wrapMode=TextureWrapMode.Clamp;
                importer.alphaIsTransparency=filename.Contains("fire");
                var android=importer.GetPlatformTextureSettings("Android"); android.overridden=true;
                android.maxTextureSize=2048; android.format=TextureImporterFormat.ASTC_6x6;
                importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
            }
            var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"painted-surfaces-v1.png");
            var clay=ObjectPigment("Painted terracotta",atlas,new Vector4(0,.5f,.5f,.5f));
            var gourd=ObjectPigment("Painted dried gourd",atlas,new Vector4(.5f,.5f,.5f,.5f));
            var cloth=ObjectPigment("Painted woven cloth",atlas,new Vector4(0,0,.5f,.5f));
            var wood=ObjectPigment("Painted charred wood",atlas,new Vector4(.5f,0,.5f,.5f));
            var stage=view.TableRoot.Find("Pop-up stage");
            var pot=TurnedObject("Rounded hollow water pot",new[]{
                new Vector2(0,0),new Vector2(.022f,0),new Vector2(.026f,.004f),new Vector2(.036f,.010f),
                new Vector2(.044f,.025f),new Vector2(.046f,.041f),new Vector2(.040f,.057f),new Vector2(.029f,.069f),
                new Vector2(.024f,.075f),new Vector2(.028f,.078f),new Vector2(.028f,.082f),new Vector2(.024f,.084f),
                new Vector2(.021f,.081f),new Vector2(.021f,.075f),new Vector2(.026f,.067f),new Vector2(.034f,.053f),
                new Vector2(.038f,.035f),new Vector2(.031f,.015f),new Vector2(.019f,.008f),new Vector2(0,.008f)},.084f,813);
            var vessel=TurnedObject("Rounded hollow flour gourd",new[]{
                new Vector2(0,0),new Vector2(.018f,.001f),new Vector2(.030f,.009f),new Vector2(.038f,.026f),
                new Vector2(.037f,.043f),new Vector2(.030f,.057f),new Vector2(.019f,.072f),new Vector2(.012f,.084f),
                new Vector2(.012f,.095f),new Vector2(.014f,.098f),new Vector2(.013f,.101f),new Vector2(.010f,.099f),
                new Vector2(.009f,.090f),new Vector2(.014f,.076f),new Vector2(.025f,.060f),new Vector2(.032f,.041f),
                new Vector2(.030f,.023f),new Vector2(.018f,.010f),new Vector2(0,.009f)},.101f,811);
            ReplaceObject(stage.Find("Heirloom Water pot"),pot,clay);
            ReplaceObject(stage.Find("Heirloom Flour gourd"),vessel,gourd);
            var small=stage.Find("Stage gourd"); ReplaceObject(small,vessel,gourd);
            small.GetChild(0).localScale=Vector3.one*.42f;
            var bundle=TurnedObject("Folded cloth bundle",new[]{new Vector2(0,0),new Vector2(.024f,.002f),
                new Vector2(.038f,.012f),new Vector2(.043f,.028f),new Vector2(.038f,.045f),new Vector2(.027f,.057f),
                new Vector2(.012f,.065f),new Vector2(.008f,.069f),new Vector2(0,.072f)},.072f,820,true);
            var parcel=stage.Find("Heirloom Travelling bundle"); ReplaceObject(parcel,bundle,cloth);
            // Small folded ends above the gathered body give the parcel a real knot from either side.
            for(var i=0;i<2;i++)
            {
                var tie=Static(parcel,"Folded cloth knot "+i,bundle,cloth).transform;
                tie.localPosition=new Vector3((i==0 ? -1 : 1)*.008f,.063f,0);
                tie.localRotation=Quaternion.Euler(0,0,i==0 ? -48 : 48); tie.localScale=new Vector3(.28f,.36f,.25f);
            }

            var hearth=view.TableRoot.Find("Griot fire");
            foreach(var r in hearth.GetComponentsInChildren<Renderer>(true)) r.enabled=false;
            var logs=Point(hearth,"Painted three-dimensional hearth",Vector3.zero);
            var log=TurnedObject("Irregular charred log",new[]{new Vector2(0,0),new Vector2(.017f,0),
                new Vector2(.020f,.012f),new Vector2(.017f,.075f),new Vector2(.020f,.13f),
                new Vector2(.015f,.20f),new Vector2(.013f,.21f),new Vector2(0,.21f)},.21f,830);
            for(var i=0;i<4;i++)
            {
                var item=Static(logs,"Charred log "+i,log,wood).transform;
                var angle=(i*57+12)*Mathf.Deg2Rad;
                item.localPosition=new Vector3(-Mathf.Sin(angle)*.09f,.020f+(i%2)*.014f,-Mathf.Cos(angle)*.09f);
                item.localRotation=Quaternion.Euler(86,i*57+12,0);
                item.localScale=new Vector3(1, i%2==0 ? 1 : .85f,1);
            }
            var coals=RiverCopy("Painted embers",wood); coals.SetFloat("_Ember",.65f);
            for(var i=0;i<9;i++)
            {
                var a=i*2.39996f;
                Prim(logs,"Warm coal "+i,PrimitiveType.Sphere,new Vector3(Mathf.Sin(a)*.045f,.012f,Mathf.Cos(a)*.045f),
                    new Vector3(.025f,.012f,.020f),coals,Quaternion.Euler(i*13,i*37,8));
            }
            var flame=RiverMaterial("Painted flame volume","Sankofa/Painted Flame",Color.white);
            flame.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"painted-fire-study-v1.png");
            var ribbons=FlameRibbon(); var tongues=new Object[5];
            for(var i=0;i<tongues.Length;i++)
            {
                var tongue=Static(logs,"Curved painted flame "+i,ribbons,flame).transform;
                var a=i*2.39996f;
                tongue.localPosition=new Vector3(Mathf.Sin(a)*.025f,.025f,Mathf.Cos(a)*.025f);
                tongue.localRotation=Quaternion.Euler(0,i*72+8,0);
                tongue.localScale=new Vector3(.8f+(i%2)*.16f,.72f+(i%3)*.14f,1);
                tongues[i]=tongue.GetComponent<Renderer>();
            }
            var presence=logs.gameObject.AddComponent<RiverStoryFire>();
            Set(presence,"narrator",hearth.GetComponentInChildren<HearthFire>()); SetArray(presence,"tongues",tongues);
        }

        static Material ObjectPigment(string name,Texture texture,Vector4 region)
        {
            var material=RiverMaterial(name,"Sankofa/Painted Object",Color.white);
            material.mainTexture=texture; material.SetVector("_Region",region); material.enableInstancing=true; return material;
        }
        static void ReplaceObject(Transform root,Mesh mesh,Material material)
        {
            for(var i=root.childCount-1;i>=0;i--) Object.DestroyImmediate(root.GetChild(i).gameObject);
            Static(root,"Painted solid vessel",mesh,material);
        }
        static Mesh TurnedObject(string name,Vector2[] knots,float height,int seed,bool folded=false)
        {
            var profile=new List<Vector2>();
            for(var k=0;k<knots.Length-1;k++)
            for(var step=0;step<4;step++)
            {
                var t=step/4f; var a=knots[Mathf.Max(0,k-1)]; var b=knots[k]; var c=knots[k+1]; var d=knots[Mathf.Min(knots.Length-1,k+2)];
                var p=.5f*((2*b)+(-a+c)*t+(2*a-5*b+4*c-d)*t*t+(-a+3*b-3*c+d)*t*t*t);
                p.x=Mathf.Max(0,p.x); p.y=Mathf.Max(0,p.y); profile.Add(p);
            }
            profile.Add(knots[knots.Length-1]);
            const int sides=48;
            var mesh=SankofaStoryMeshes.Lathe(name,profile,sides,(a,y)=>Color.white,seed);
            var vertices=mesh.vertices; var uv=mesh.uv;
            for(var i=0;i<vertices.Length;i++)
            {
                uv[i].y=vertices[i].y/height;
                if(!folded) continue;
                var a=Mathf.Atan2(vertices[i].z,vertices[i].x);
                var gather=Mathf.SmoothStep(0,1,vertices[i].y/height);
                var pleat=1+(.035f+.12f*gather)*Mathf.Sin(a*9+vertices[i].y*19);
                vertices[i].x*=pleat; vertices[i].z*=pleat;
            }
            mesh.vertices=vertices; mesh.uv=uv; mesh.RecalculateNormals();
            var normals=mesh.normals; var colours=new Color[vertices.Length];
            for(var ring=0;ring<profile.Count;ring++)
            {
                var first=ring*(sides+1); var last=first+sides;
                normals[first]=normals[last]=(normals[first]+normals[last]).normalized;
                for(var side=0;side<=sides;side++)
                {
                    var i=first+side; var inward=Vector3.Dot(normals[i],new Vector3(vertices[i].x,0,vertices[i].z))<-.001f;
                    var shade=inward ? Mathf.Lerp(.30f,.70f,Mathf.Pow(Mathf.Clamp01(vertices[i].y/height),3)) : 1;
                    colours[i]=new Color(shade,shade,shade,1);
                }
            }
            mesh.normals=normals; mesh.colors=colours; mesh.RecalculateTangents(); mesh.RecalculateBounds();
            return SaveRiverMesh(mesh,name);
        }
        static Mesh FlameRibbon()
        {
            const int across=8, up=18;
            var vertices=new List<Vector3>(); var uv=new List<Vector2>(); var triangles=new List<int>();
            for(var y=0;y<=up;y++) for(var x=0;x<=across;x++)
            {
                var u=x/(float)across; var v=y/(float)up;
                vertices.Add(new Vector3((u-.5f)*.19f,v*.29f,Mathf.Sin(u*Mathf.PI)*.035f+Mathf.Sin(v*5)*v*.023f));
                uv.Add(new Vector2(u,v));
                if(x==across||y==up) continue;
                var a=y*(across+1)+x; var b=a+across+1;
                triangles.AddRange(new[]{a,b,a+1,a+1,b,b+1});
            }
            var mesh=new Mesh(); mesh.SetVertices(vertices); mesh.SetUVs(0,uv); mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); return SaveRiverMesh(mesh,"Curved flame ribbon");
        }
        static Mesh SaveRiverMesh(Mesh mesh,string name)
        {
            mesh.name=name; var path=RiverArt+"/"+name+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null) { AssetDatabase.CreateAsset(mesh,path); return mesh; }
            EditorUtility.CopySerialized(mesh,saved); Object.DestroyImmediate(mesh); return saved;
        }
    }
}
