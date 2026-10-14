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
                var dark=SankofaStoryMeshes.Lathe("Gourd inner shadow",new[]{new Vector2(0,height*0.88f),new Vector2(neck*0.69f,height*0.88f)},24,(a,y)=>new Color(0.14f,0.09f,0.05f),1802);
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
                var pigment=item.Surfaces.First().sharedMaterial;
                pigment.SetColor("_Color",new Color(0.83f,0.66f,0.45f));pigment.SetFloat("_InkWidth",0.17f);pigment.SetFloat("_VertexTint",1);
                EditorUtility.SetDirty(pigment);Set(item,"echoMesh",mesh);
            }
            // A hand-made gate reads through uneven weathered pigment while keeping its thin, open silhouette.
            var gate=stage.Find("gate");var gateBody=gate.Surfaces.First().GetComponent<MeshFilter>();
            var worn=Object.Instantiate(gateBody.sharedMesh);worn.name="Gallery weathered gate";
            worn.colors=worn.vertices.Select(v=>
            {
                var wear=0.72f+0.25f*Mathf.Sin(v.x*37+v.y*23+v.z*11);
                return Color.Lerp(new Color(0.63f,0.48f,0.32f),new Color(0.55f,0.72f,0.55f),wear);
            }).ToArray();
            SaveGalleryMesh(worn,gateBody);Set(gate,"echoMesh",worn);
        }
        static void SaveGalleryMesh(Mesh mesh,MeshFilter target)
        {
            var path=GalleryFolder+"/"+mesh.name+".asset";
            AssetDatabase.DeleteAsset(path);AssetDatabase.CreateAsset(mesh,path);target.sharedMesh=mesh;
        }
    }
}
