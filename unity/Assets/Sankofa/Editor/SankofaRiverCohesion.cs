using System.Collections.Generic;
using System.Linq;
using Sankofa.Runtime.Personal;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        public const string RiverPaintedScene="Assets/Sankofa/Scenes/RiverThresholdPainted.unity";
        public const string RiverSolidScene="Assets/Sankofa/Scenes/RiverThreshold3D.unity";
        const string CohesionArt="Assets/Sankofa/Art/Generated/RiverCohesion";
        const string CohesionSource="Assets/Sankofa/Art/RiverCohesion/";

        [MenuItem("Sankofa/Build Painted River Candidate")]
        public static void BuildRiverPaintedCandidate()
        {
            AssetDatabase.Refresh();
            BuildRiverCast(false);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sankofa] Built the painted river candidate; comparison scenes preserved.");
        }

        [MenuItem("Sankofa/Build River Cast Comparison")]
        public static void BuildRiverCastComparison()
        {
            AssetDatabase.Refresh();
            if(!AssetDatabase.IsValidFolder(CohesionArt)) AssetDatabase.CreateFolder(SankofaArt.Folder,"RiverCohesion");
            foreach(var path in System.IO.Directory.GetFiles(CohesionSource,"*.png"))
            {
                var importer=(TextureImporter)AssetImporter.GetAtPath(path.Replace('\\','/'));
                importer.maxTextureSize=2048; importer.mipmapEnabled=true; importer.wrapMode=TextureWrapMode.Clamp;
                importer.alphaIsTransparency=!path.Contains("materials");
                var android=importer.GetPlatformTextureSettings("Android"); android.overridden=true; android.maxTextureSize=2048;
                android.format=TextureImporterFormat.ASTC_6x6; importer.SetPlatformTextureSettings(android); importer.SaveAndReimport();
            }
            BuildRiverCast(false); BuildRiverCast(true);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sankofa] Built matched river character comparisons; baseline scenes preserved.");
        }

        static void BuildRiverCast(bool solid)
        {
            var scene=EditorSceneManager.OpenScene(RiverThresholdScene,OpenSceneMode.Single);
            var path=solid ? RiverSolidScene : RiverPaintedScene;
            EditorSceneManager.SaveScene(scene,path);
            var prototype=Object.FindFirstObjectByType<RiverThresholdPrototype>();
            var view=Object.FindFirstObjectByType<StoryTableView>();
            var river=GameObject.Find("River threshold world").transform.Find("Seated riverside");
            var environment=BuildCohesiveRiver(river);
            var controller=prototype.gameObject.AddComponent<RiverCastPerformance>();
            Set(controller,"prototype",prototype); Set(controller,"solid",solid); SetArray(controller,"scenery",environment.Cast<Object>().ToArray());
            var people=new Transform[2]; var faces=new Object[2];
            // The table previews use the same new artwork in both versions. Only the river cast treatment differs.
            for(var i=0;i<2;i++)
            {
                var id=i==0 ? "wambui" : "naserian";
                var material=CohesionMaterial("Portrait "+id,"Sankofa/Cohesive Figure",Color.white);
                material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(CohesionSource+id+"-rest-v1.png");
                material.SetTexture("_Offer",AssetDatabase.LoadAssetAtPath<Texture2D>(CohesionSource+id+"-offer-v1.png"));
                material.SetTexture("_Reflect",AssetDatabase.LoadAssetAtPath<Texture2D>(CohesionSource+id+"-reflect-v1.png"));
                var mini=view.Stage.GetComponentsInChildren<Renderer>(true).First(r=>r.name=="Painted figure" && r.transform.parent.parent.name.Contains(id));
                mini.sharedMaterial=material; mini.transform.localScale=new Vector3(.1333f,.2f,1);
                var old=river.Find(i==0 ? "The one who remembers" : "The younger traveller");
                if(!solid) { people[i]=old; faces[i]=old.GetComponent<Renderer>(); old.GetComponent<Renderer>().sharedMaterial=material; }
                else Object.DestroyImmediate(old.gameObject);
            }
            if(solid)
            {
                var source=EditorSceneManager.OpenScene(HearthScene,OpenSceneMode.Additive);
                for(var i=0;i<2;i++)
                {
                    var id=i==0 ? "wambui" : "naserian";
                    var original=source.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).First(t=>t.name=="figure."+id);
                    var clone=Object.Instantiate(original.gameObject); SceneManager.MoveGameObjectToScene(clone,scene);
                    clone.name="Painted solid "+id; clone.transform.SetParent(river,false);
                    clone.transform.localScale=Vector3.one; clone.SetActive(true); people[i]=clone.transform;
                    // Dependants must be removed before their required StageObject/FigureMotion components.
                    foreach(var script in clone.GetComponentsInChildren<MonoBehaviour>(true).Reverse()) Object.DestroyImmediate(script);
                    if(clone.GetComponentsInChildren<MonoBehaviour>(true).Length!=0)
                        throw new System.InvalidOperationException("Historical figure behaviour survived the comparison import: "+id);
                    foreach(var line in clone.GetComponentsInChildren<LineRenderer>(true)) Object.DestroyImmediate(line.gameObject);
                    foreach(var prop in clone.GetComponentsInChildren<MeshRenderer>(true)) Object.DestroyImmediate(prop.gameObject);
                    foreach(var collider in clone.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                    var skin=CohesionPigment("Warm painted skin","cast-materials-v1.png",new Vector4(.5f,0,.5f,.5f),new Color(1.35f,1.22f,1.08f));
                    var wrap=CohesionPigment(id+" woven wrap","cast-materials-v1.png",i==0 ? new Vector4(0,.5f,.5f,.5f) : new Vector4(0,0,.5f,.5f),Color.white);
                    var shawl=CohesionPigment(id+" shoulder cloth",i==0 ? "cast-materials-v1.png" : "../RiverThreshold/painted-surfaces-v1.png",
                        i==0 ? new Vector4(.5f,.5f,.5f,.5f) : new Vector4(0,0,.5f,.5f),Color.white);
                    var hair=CohesionMaterial("Charcoal hair","Sankofa/Cohesive Surface",new Color(.038f,.027f,.023f));
                    var eyes=CohesionMaterial("Quiet eyes","Sankofa/Cohesive Surface",Color.white); eyes.SetFloat("_VertexColor",1);
                    foreach(var r in clone.GetComponentsInChildren<Renderer>(true))
                    {
                        r.enabled=true; r.forceRenderingOff=false; r.SetPropertyBlock(null);
                        r.sharedMaterial=r.name=="Wrap" ? wrap : r.name=="Shawl" ? shawl : r.name=="Hair" ? hair : r.name=="Eyes" ? eyes : skin;
                        r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows=false;
                        if(r is SkinnedMeshRenderer skinned) skinned.updateWhenOffscreen=true;
                    }
                }
                EditorSceneManager.CloseScene(source,true);
            }
            SetArray(controller,"actors",people.Cast<Object>().ToArray());
            SetArray(prototype,"people",solid ? new Object[0] : faces);
            if (!solid)
            {
                var app = Object.FindFirstObjectByType<PersonalStoryApp>();
                SetPersonalCredits(app);
                var reading = prototype.gameObject.AddComponent<RiverReadingLayout>();
                Set(reading, "prototype", prototype);
                var choices = prototype.gameObject.AddComponent<RiverPerspectiveChoices>();
                choices.Configure(app, prototype, controller);
                var keptAnchor = Point(view.TableRoot.Find("Story page"), "River kept story anchor", new Vector3(0, -.062f, -.025f));
                var keepsake = prototype.gameObject.AddComponent<RiverKeepsakePresentation>();
                keepsake.Configure(app, prototype, view.TableRoot.Find("Kept strand"), keptAnchor,
                    AssetDatabase.LoadAssetAtPath<Material>(TableArtFolder + "/Bead unthreaded.mat"));
            }
            EditorSceneManager.SaveScene(scene,path);
        }

        static Renderer[] BuildCohesiveRiver(Transform river)
        {
            foreach(var name in new[]{"Ground below the seat","Near left reeds","Near right reeds"})
                Object.DestroyImmediate(river.Find(name).gameObject);
            var earth=CohesionPigment("Umber riverbank","river-materials-v1.png",new Vector4(0,.5f,.5f,.5f),new Color(.7f,.78f,.94f));
            earth.SetFloat("_WorldUV",1); earth.SetFloat("_Scale",.6f); earth.SetFloat("_GroundFade",1);
            earth.SetFloat("_SrcBlend",5); earth.SetFloat("_DstBlend",10); earth.SetFloat("_ZWrite",0); earth.renderQueue=2460;
            Static(river,"Sculpted painted riverbank",CohesionGround(),earth);
            var rock=CohesionPigment("Blue hour stones","river-materials-v1.png",new Vector4(.5f,.5f,.5f,.5f),new Color(.92f,.94f,.94f));
            var reeds=CohesionPigment("Olive reed strokes","river-materials-v1.png",new Vector4(.5f,0,.5f,.5f),new Color(1.1f,1.05f,.82f));
            reeds.SetFloat("_Wind",1);
            var water=CohesionMaterial("Ink blue moving water","Sankofa/Cohesive Surface",Color.white); water.SetFloat("_Water",1);
            water.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sankofa/Art/RiverThreshold/river-at-blue-hour-v1.png");
            water.SetVector("_Region",new Vector4(.65f,.015f,.30f,.28f)); water.SetFloat("_WorldUV",1); water.SetFloat("_Scale",.22f);
            var surface=Static(river,"Water in physical depth",CohesionWater(),water).GetComponent<Renderer>();
            var grasses=Static(river,"Curved reeds in physical depth",CohesionReeds(),reeds).GetComponent<Renderer>();
            var stone=CohesionStone();
            for(var i=0;i<18;i++)
            {
                var z=-1.8f+i*.38f+.17f*Mathf.Sin(i*2.6f); var x=BankEdge(z)-.02f+.20f*Mathf.Sin(i*1.5f);
                var at=new Vector3(x,CohesionHeight(x,z)+.03f,z);
                var r=Static(river,"River stone "+i,stone,rock).transform; r.localPosition=at;
                r.localScale=new Vector3(.3f+i%4*.12f,.15f+i%3*.06f,.27f+i%5*.09f); r.localRotation=Quaternion.Euler(i*17,i*83,0);
            }
            var bark=CohesionPigment("Deep indigo bark","river-materials-v1.png",new Vector4(0,0,.5f,.5f),new Color(.34f,.40f,.55f));
            // Trunks occupy the sides/rear, with clear space for hands, captions and both actors ahead.
            foreach(var p in new[]{new Vector3(-3.6f,0,2.2f),new Vector3(-4.4f,0,-1),new Vector3(4.3f,0,-2.1f),new Vector3(-2,0,-4)})
            {
                var trunk=Prim(river,"Painted tree trunk",PrimitiveType.Cylinder,p+Vector3.up*2.3f,new Vector3(.25f,2.3f,.32f),bark).transform;
                trunk.localRotation=Quaternion.Euler(0,0,p.x*1.2f);
            }
            // The shared table room otherwise kept a bright cracked-earth floor beneath the cinematic river.
            foreach(var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include,FindObjectsSortMode.None))
                if(r.name=="Painted ground") r.sharedMaterial=earth;
            return new[]{surface,grasses};
        }
        static float BankEdge(float z) => 1.65f+.25f*Mathf.Sin(z*.7f);
        static float CohesionHeight(float x,float z) => RiverCastPerformance.GroundHeight(x,z);
        static Mesh CohesionGround()
        {
            const int nx=60,nz=60; var v=new List<Vector3>(); var uv=new List<Vector2>(); var t=new List<int>();
            for(var iz=0;iz<=nz;iz++) for(var ix=0;ix<=nx;ix++)
            {
                var z=Mathf.Lerp(-10,5.1f,iz/(float)nz); var x=Mathf.Lerp(-10,BankEdge(z),ix/(float)nx);
                v.Add(new Vector3(x,CohesionHeight(x,z),z)); uv.Add(new Vector2(x,z));
                if(ix==nx||iz==nz) continue; var a=iz*(nx+1)+ix; t.AddRange(new[]{a,a+nx+1,a+1,a+1,a+nx+1,a+nx+2});
            }
            return SaveCohesionMesh("Sculpted riverbank",v,uv,t);
        }
        static Mesh CohesionWater()
        {
            var v=new List<Vector3>(); var uv=new List<Vector2>(); var t=new List<int>();
            for(var i=0;i<=60;i++)
            {
                var z=Mathf.Lerp(-10,5.2f,i/60f); v.Add(new Vector3(BankEdge(z)-.05f,-.065f,z)); v.Add(new Vector3(10,-.065f,z));
                uv.Add(new Vector2(0,i/60f)); uv.Add(new Vector2(1,i/60f));
                if(i<60) { var a=i*2; t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3}); }
            }
            return SaveCohesionMesh("Near water ribbon",v,uv,t);
        }
        static Mesh CohesionReeds()
        {
            var v=new List<Vector3>(); var uv=new List<Vector2>(); var t=new List<int>(); var random=new System.Random(411);
            for(var c=0;c<40;c++) for(var blade=0;blade<16;blade++)
            {
                var z=-2.8f+c*.185f+(float)random.NextDouble()*.3f; var x=c%3==0 ? -2.8f+(float)random.NextDouble()*.6f : BankEdge(z)-.18f;
                var origin=new Vector3(x+(float)random.NextDouble()*.18f,CohesionHeight(x,z),z+(float)random.NextDouble()*.2f);
                var height=.24f+(float)random.NextDouble()*.52f; var angle=(float)random.NextDouble()*Mathf.PI*2;
                var bend=new Vector3(Mathf.Sin(angle),0,Mathf.Cos(angle)); var side=new Vector3(bend.z,0,-bend.x);
                var start=v.Count;
                for(var k=0;k<=6;k++)
                {
                    var p=k/6f; var centre=origin+Vector3.up*(height*p)+bend*(height*.3f*p*p);
                    var width=.013f*(1-p)+.001f;
                    v.Add(centre-side*width); v.Add(centre+side*width); uv.Add(new Vector2(0,p)); uv.Add(new Vector2(1,p));
                    if(k<6) { var a=start+k*2; t.AddRange(new[]{a,a+2,a+1,a+1,a+2,a+3}); }
                }
            }
            return SaveCohesionMesh("Curved reed clusters",v,uv,t);
        }
        static Mesh CohesionStone()
        {
            var temporary=GameObject.CreatePrimitive(PrimitiveType.Sphere); var mesh=Object.Instantiate(temporary.GetComponent<MeshFilter>().sharedMesh);
            Object.DestroyImmediate(temporary); var v=mesh.vertices;
            for(var i=0;i<v.Length;i++) { var p=v[i]; v[i]*=1+.1f*Mathf.Sin(p.x*17+p.z*11)*Mathf.Cos(p.y*13); }
            var result=SaveCohesionMesh("Rounded uneven stones",v.ToList(),mesh.uv.ToList(),mesh.triangles.ToList()); Object.DestroyImmediate(mesh); return result;
        }
        static Mesh SaveCohesionMesh(string name,List<Vector3> vertices,List<Vector2> uv,List<int> triangles)
        {
            var mesh=new Mesh{name=name,vertices=vertices.ToArray(),uv=uv.ToArray(),triangles=triangles.ToArray()};
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); var path=CohesionArt+"/"+name+".asset";
            var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if(saved==null) { AssetDatabase.CreateAsset(mesh,path); return mesh; }
            EditorUtility.CopySerialized(mesh,saved); Object.DestroyImmediate(mesh); return saved;
        }
        static Material CohesionMaterial(string name,string shader,Color colour)
        {
            var path=CohesionArt+"/"+name+".mat"; var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null) { m=new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m,path); }
            m.shader=Shader.Find(shader); if(m.HasProperty("_Color")) m.color=colour; m.enableInstancing=true; EditorUtility.SetDirty(m); return m;
        }
        static Material CohesionPigment(string name,string file,Vector4 region,Color tint)
        {
            var m=CohesionMaterial(name,"Sankofa/Cohesive Surface",tint);
            var path=System.IO.Path.GetFullPath(CohesionSource+file).Replace('\\','/');
            path="Assets"+path.Substring(Application.dataPath.Length);
            m.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(path); m.SetVector("_Region",region); return m;
        }
    }
}
