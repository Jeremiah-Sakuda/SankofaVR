using System.Collections.Generic;
using System.Linq;
using Sankofa.Runtime.Stage;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        // Fictional prop detailing for the painting's language, not a reconstruction of a specific artefact.
        // Replaces meshes in the candidate only; the comparison scene and original generated meshes remain intact.
        static void PolishGalleryObjects(StoryStage stage)
        {
            foreach(var id in new[]{"gourd.flour","gourd.milk"})
            {
                var item=stage.Find(id);var body=item.Surfaces.First().GetComponent<MeshFilter>();
                var height=id=="gourd.flour"?0.23f:0.34f;var bulb=id=="gourd.flour"?0.095f:0.075f;
                var neck=id=="gourd.flour"?0.03f:0.026f;
                var profile=new List<Vector2>
                {
                    new Vector2(0,0),new Vector2(bulb*0.35f,0.008f),new Vector2(bulb*0.82f,height*0.12f),
                    new Vector2(bulb,height*0.3f),new Vector2(bulb*0.96f,height*0.48f),new Vector2(bulb*0.65f,height*0.65f),
                    new Vector2(neck,height*0.79f),new Vector2(neck,height*0.95f),
                    new Vector2(neck*1.13f,height*0.95f),new Vector2(neck*1.13f,height),
                    new Vector2(neck*0.72f,height),new Vector2(neck*0.72f,height*0.86f),new Vector2(0,height*0.86f)
                };
                var vessel=SankofaStoryMeshes.Lathe("Gallery "+id,profile,32,(a,y)=>
                {
                    var tone=0.92f+0.07f*Mathf.Sin(a*7+y*80);
                    return new Color(tone,tone*0.9f,tone*0.72f);
                },1801);
                var dark=SankofaStoryMeshes.Lathe("Gourd inner shadow",new[]{new Vector2(neck*0.69f,height*0.88f),new Vector2(0,height*0.88f)},24,(a,y)=>new Color(0.14f,0.09f,0.05f),1802);
                var strapPoints=Enumerable.Range(0,17).Select(i=>
                {
                    var a=i*Mathf.PI/8;
                    return new Vector3(Mathf.Sin(a)*neck*1.05f,height*0.87f+Mathf.Cos(a)*0.015f,Mathf.Cos(a)*neck*1.05f);
                }).ToArray();
                var strap=SankofaStoryMeshes.Tube("Gourd cord",strapPoints,0.0035f,0.0035f);
                strap.colors=Enumerable.Repeat(new Color(0.25f,0.16f,0.09f),strap.vertexCount).ToArray();
                var mesh=new Mesh{name="Gallery detailed "+id};
                mesh.CombineMeshes(new[]{vessel,dark,strap}.Select(m=>new CombineInstance{mesh=m,transform=Matrix4x4.identity}).ToArray());
                SaveGalleryMesh(mesh,body);
                foreach(var m in new[]{vessel,dark,strap})Object.DestroyImmediate(m);
                var pigment=GalleryCopy(item.Surfaces.First().sharedMaterial,"Gallery "+id+" pigment");
                item.Surfaces.First().sharedMaterial=pigment;
                pigment.SetColor("_Color",new Color(0.58f,0.37f,0.20f));pigment.SetFloat("_InkWidth",0.17f);pigment.SetFloat("_VertexTint",1);
                EditorUtility.SetDirty(pigment);Set(item,"echoMesh",mesh);
            }
            RefineGalleryBeads(stage);
            RefineGalleryGate(stage);
        }

        static Mesh Join(string name,List<CombineInstance> parts)
        {
            var mesh=new Mesh{name=name};mesh.CombineMeshes(parts.ToArray());
            foreach(var part in parts)Object.DestroyImmediate(part.mesh);
            return mesh;
        }
        static Material GalleryCopy(Material original,string name)
        {
            var path=GalleryFolder+"/"+name+".mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(original){name=name};AssetDatabase.CreateAsset(material,path);}
            else material.CopyPropertiesFromMaterial(original);
            material.SetFloat("_VertexTint",1);EditorUtility.SetDirty(material);return material;
        }
        static void Rounded(List<CombineInstance> parts,Vector3 at,Vector3 scale,Color pigment,int seed,int subdivision=2)
        {
            var mesh=SankofaMeshes.Stone(seed,subdivision,0.02f,10f);
            mesh.colors=Enumerable.Repeat(pigment,mesh.vertexCount).ToArray();
            parts.Add(new CombineInstance{mesh=mesh,transform=Matrix4x4.TRS(at,Quaternion.identity,scale)});
        }
        static void RefineGallerySheep(StoryStage stage)
        {
            var ewe=stage.Find("figure.ewe");var body=ewe.transform.Find("Body").GetComponent<MeshFilter>();
            ewe.transform.localPosition=new Vector3(0.10f,0,2.85f);
            ewe.transform.localRotation=Quaternion.Euler(0,-105,0);
            var motion=new SerializedObject(ewe.GetComponent<FigureMotion>());
            motion.FindProperty("entryPath").arraySize=0;motion.FindProperty("delay").floatValue=0;motion.ApplyModifiedPropertiesWithoutUndo();
            var parts=new List<CombineInstance>();var centre=new Vector3(0,0.52f,0);var radius=new Vector3(0.18f,0.18f,0.34f);
            Rounded(parts,centre,radius,new Color(0.94f,0.89f,0.78f),6300,3);
            // Shallow, irregular fleece clumps follow one coherent body, instead of a stack of smooth blobs.
            for(var i=0;i<64;i++)
            {
                var y=1-2*(i+0.5f)/64;var a=i*2.399963f;var r=Mathf.Sqrt(1-y*y);
                var normal=new Vector3(Mathf.Cos(a)*r,y,Mathf.Sin(a)*r);if(y< -0.55f)continue;
                var tone=0.86f+0.10f*Mathf.Sin(i*1.7f);var size=0.027f+0.008f*Mathf.Sin(i*2.3f);
                Rounded(parts,centre+Vector3.Scale(radius,normal)*0.94f,Vector3.one*size,new Color(tone,tone*0.96f,tone*0.87f),6400+i,1);
            }
            Rounded(parts,new Vector3(0,0.49f,-0.34f),new Vector3(0.035f,0.07f,0.035f),new Color(0.8f,0.75f,0.64f),6510);
            SaveGalleryMesh(Join("Gallery sculpted ewe fleece",parts),body);
            body.GetComponent<Renderer>().sharedMaterial=GalleryCopy(body.GetComponent<Renderer>().sharedMaterial,"Gallery ewe fleece pigment");
            body.GetComponent<Renderer>().sharedMaterial.SetColor("_Color",new Color(0.58f,0.54f,0.47f));
            var facePigment=GalleryCopy(ewe.transform.Find("Neck/Head").GetComponent<Renderer>().sharedMaterial,"Gallery ewe face pigment");
            var head=ewe.transform.Find("Neck/Head").GetComponent<MeshFilter>();
            var headMesh=Object.Instantiate(head.sharedMesh);headMesh.name="Gallery ewe head";
            headMesh.colors=Enumerable.Repeat(new Color(0.92f,0.82f,0.70f),headMesh.vertexCount).ToArray();SaveGalleryMesh(headMesh,head);
            head.GetComponent<Renderer>().sharedMaterial=facePigment;
            foreach(var leg in ewe.transform.Cast<Transform>().Where(t=>t.name.StartsWith("Leg ")))
            {
                var filter=leg.GetComponentInChildren<MeshFilter>();parts=new List<CombineInstance>();
                var shaft=Object.Instantiate(filter.sharedMesh);shaft.colors=Enumerable.Repeat(new Color(0.95f,0.87f,0.75f),shaft.vertexCount).ToArray();
                shaft.vertices=shaft.vertices.Select(v=>new Vector3(v.x*1.35f,v.y,v.z*1.35f)).ToArray();shaft.RecalculateNormals();shaft.RecalculateBounds();
                parts.Add(new CombineInstance{mesh=shaft,transform=Matrix4x4.identity});
                foreach(var side in new[]{-1f,1f})Rounded(parts,new Vector3(side*0.008f,-0.407f,0.013f),new Vector3(0.015f,0.024f,0.032f),new Color(0.28f,0.22f,0.17f),6520+(int)side);
                SaveGalleryMesh(Join("Gallery ewe hoof "+leg.name,parts),filter);
                filter.GetComponent<Renderer>().sharedMaterial=facePigment;
            }
            var echo=new Mesh{name="Gallery sculpted ewe memory"};
            echo.CombineMeshes(ewe.Surfaces.Select(r=>new CombineInstance{mesh=r.GetComponent<MeshFilter>().sharedMesh,transform=ewe.transform.worldToLocalMatrix*r.transform.localToWorldMatrix}).ToArray());
            var path=GalleryFolder+"/"+echo.name+".asset";AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(echo,path);Set(ewe,"echoMesh",echo);
        }
        static void RefineGalleryBeads(StoryStage stage)
        {
            var pigment=GalleryPigment("Bead blue pigment",new Color(0.78f,0.88f,1));pigment.SetFloat("_VertexTint",1);pigment.SetFloat("_InkWidth",0.15f);
            foreach(var id in new[]{"bead","bead.kept","bead.today"})
            {
                var bead=stage.Find(id);if(bead==null)continue;var body=bead.Surfaces.First().GetComponent<MeshFilter>();
                var profile=new List<Vector2>{new Vector2(0.0032f,-0.0072f)};
                for(var k=0;k<=12;k++){var a=Mathf.Lerp(-0.5f,0.5f,k/12f)*Mathf.PI;profile.Add(new Vector2(0.0032f+0.0068f*Mathf.Cos(a),0.0072f*Mathf.Sin(a)));}
                profile.Add(new Vector2(0.0032f,0.0072f));profile.Add(new Vector2(0.0032f,-0.0072f));
                var mesh=SankofaStoryMeshes.Lathe("Gallery pierced "+id,profile,32,(a,y)=>
                {
                    var band=Mathf.Abs(Mathf.Abs(y)/0.0072f-0.62f)<0.13f;
                    return band?new Color(0.93f,0.86f,0.7f):new Color(0.18f,0.36f,0.74f)*(0.93f+0.07f*Mathf.Cos(a*5));
                },6600);
                SaveGalleryMesh(mesh,body);body.GetComponent<Renderer>().sharedMaterial=pigment;Set(bead,"echoMesh",mesh);
                // The old glow card spans the pierced opening. Body highlight and the receive ring still supply feedback.
                var glow=new SerializedObject(bead).FindProperty("glow").objectReferenceValue as Renderer;
                Set(bead,"glow",null);if(glow!=null)Object.DestroyImmediate(glow.gameObject);
            }
        }
        static Mesh Board(string name,Vector3 size,int seed)
        {
            var ring=new[]{new Vector2(-0.5f,-0.36f),new Vector2(-0.36f,-0.5f),new Vector2(0.36f,-0.5f),new Vector2(0.5f,-0.36f),new Vector2(0.5f,0.36f),new Vector2(0.36f,0.5f),new Vector2(-0.36f,0.5f),new Vector2(-0.5f,0.36f)};
            var vertices=new List<Vector3>();var triangles=new List<int>();var colors=new List<Color>();
            for(var level=0;level<3;level++)for(var i=0;i<8;i++)
            {
                var grain=Mathf.Sin(seed*0.7f+i*1.3f+level*2.1f);var y=(level/2f-0.5f)*size.y+(level==2?grain*0.005f:0);
                vertices.Add(new Vector3(ring[i].x*size.x*(1+grain*0.035f),y,ring[i].y*size.z));
                colors.Add(Color.Lerp(new Color(0.63f,0.46f,0.27f),new Color(0.42f,0.59f,0.39f),Mathf.Clamp01(0.45f+grain*0.36f)));
                if(level>0){var a=(level-1)*8+i;var b=(level-1)*8+(i+1)%8;triangles.AddRange(new[]{a,b,b+8,a,b+8,a+8});}
            }
            for(var i=1;i<7;i++){triangles.AddRange(new[]{0,i+1,i,16,16+i,17+i});}
            var mesh=new Mesh{name=name};mesh.SetVertices(vertices);mesh.SetTriangles(triangles,0);mesh.SetColors(colors);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        static void RefineGalleryGate(StoryStage stage)
        {
            var gate=stage.Find("gate");var body=gate.Surfaces.First().GetComponent<MeshFilter>();var parts=new List<CombineInstance>();
            void Add(Vector3 size,Vector3 at,float roll,int seed)
            {parts.Add(new CombineInstance{mesh=Board("Garden wood",size,seed),transform=Matrix4x4.TRS(at,Quaternion.Euler(0,0,roll),Vector3.one)});}
            Add(new Vector3(0.064f,1.01f,0.064f),new Vector3(-0.32f,0.5f,0),-0.8f,6700);
            Add(new Vector3(0.061f,1.04f,0.059f),new Vector3(0.32f,0.5f,0),0.7f,6701);
            foreach(var y in new[]{0.3f,0.8f})Add(new Vector3(0.045f,0.58f,0.025f),new Vector3(0,y,0),90,6702+(int)(y*10));
            Add(new Vector3(0.026f,0.66f,0.023f),new Vector3(0,0.55f,0.026f),-52,6710);
            for(var i=0;i<7;i++)Add(new Vector3(0.037f,0.55f+0.012f*Mathf.Sin(i*2),0.019f),new Vector3(-0.24f+i*0.08f,0.55f,-0.014f),(i%3-1)*1.1f,6720+i);
            // Small inset fasteners catch light; included in the one existing renderer.
            foreach(var x in new[]{-0.24f,0.24f})foreach(var y in new[]{0.3f,0.8f})Rounded(parts,new Vector3(x,y,-0.032f),new Vector3(0.004f,0.004f,0.002f),new Color(0.16f,0.15f,0.12f),6740,1);
            SaveGalleryMesh(Join("Gallery bevelled garden gate",parts),body);Set(gate,"echoMesh",body.sharedMesh);
        }

        static void SaveGalleryMesh(Mesh mesh,MeshFilter target)
        {
            var path=GalleryFolder+"/"+mesh.name+".asset";
            AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);target.sharedMesh=mesh;
        }
    }
}
