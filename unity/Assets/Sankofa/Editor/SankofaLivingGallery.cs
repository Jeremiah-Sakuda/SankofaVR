using System.IO;
using System.Linq;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.Stage;
using Sankofa.Runtime.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        public const string GalleryScene = "Assets/Sankofa/Scenes/LivingGallery.unity";
        public const string GalleryPackage = "Assets/Sankofa/Content/living-gallery.json";
        const string GalleryFolder = "Assets/Sankofa/Art/Generated/Gallery";
        [MenuItem("Sankofa/Build Living Gallery")]
        public static void BuildLivingGallery()
        {
            BuildIllustratedTheatre();
            if (!AssetDatabase.IsValidFolder(GalleryFolder)) AssetDatabase.CreateFolder(SankofaArt.Folder, "Gallery");
            var stage = Object.FindFirstObjectByType<StoryStage>();
            var world = Object.FindFirstObjectByType<WorldAnchor>().transform;
            var painting = CaptureComposition(stage, "story.water.1850s", "What We Carry");
            BuildModernIllustration(stage);
            var home = stage.Scenes.Single(s => s.IsHearth);
            var surroundings = Child(home.Root.transform, "Gallery architecture");
            // Hide the inherited outdoor environment; the gallery is the outer journey, not another stop before it.
            foreach (var r in home.Root.GetComponentsInChildren<Renderer>(true)) r.enabled = false;
            var plaster = GalleryPigment("Warm plaster", new Color(0.16f, 0.175f, 0.19f));
            var floor = GalleryPigment("Slate floor", new Color(0.065f, 0.073f, 0.085f));
            var wood = GalleryPigment("Walnut frame", new Color(0.16f, 0.105f, 0.065f));
            var brass = GalleryPigment("Brass edge", new Color(0.66f, 0.42f, 0.2f));
            Prim(surroundings, "Floor", PrimitiveType.Cube, new Vector3(0,-0.06f,1.8f), new Vector3(10,0.1f,10), floor);
            Prim(surroundings, "Exhibit wall", PrimitiveType.Cube, new Vector3(0,1.8f,3.3f), new Vector3(9,4,0.12f), plaster);
            foreach (var x in new[] {-4.5f,4.5f})
                Prim(surroundings, "Side wall", PrimitiveType.Cube, new Vector3(x,1.8f,0), new Vector3(0.12f,4,7), plaster);
            Prim(surroundings, "Exhibit ledge", PrimitiveType.Cube, new Vector3(0,0.66f,0.55f), new Vector3(1.22f,0.035f,0.28f), wood);
            var work = Child(world, "What We Carry framed");
            work.localPosition = new Vector3(0,1.53f,3.19f);
            var imageMaterial = new Material(Shader.Find("Sankofa/Gallery Canvas")) { name = "What We Carry canonical" };
            var matPath=GalleryFolder+"/Canonical painting.mat";
            AssetDatabase.DeleteAsset(matPath); AssetDatabase.CreateAsset(imageMaterial,matPath);
            imageMaterial.mainTexture = painting;
            var canvas=Prim(work,"Canonical canvas",PrimitiveType.Quad,Vector3.zero,new Vector3(3.55f,1.997f,1),imageMaterial).GetComponent<Renderer>();
            var edges = new System.Collections.Generic.List<Renderer>();
            foreach (var trim in new[] { (0.08f,wood), (0.024f,brass) })
            {
                var w=3.55f+trim.Item1*2; var h=1.997f+trim.Item1*2;
                foreach(var sign in new[]{-1f,1f})
                {
                    edges.Add(Prim(work,"Frame horizontal",PrimitiveType.Cube,new Vector3(0,sign*h/2,-0.014f),new Vector3(w,trim.Item1,0.045f),trim.Item2).GetComponent<Renderer>());
                    edges.Add(Prim(work,"Frame vertical",PrimitiveType.Cube,new Vector3(sign*w/2,0,-0.014f),new Vector3(trim.Item1,h,0.045f),trim.Item2).GetComponent<Renderer>());
                }
            }
            GalleryText(surroundings,"Gallery title","SANKOFA",new Vector3(0,3.07f,3.16f),0.85f,3.0f);
            GalleryText(surroundings,"Gallery purpose","Stories time nearly left behind",new Vector3(0,2.88f,3.16f),0.42f,3.8f);
            GalleryText(surroundings,"Exhibit title","What We Carry",new Vector3(0,2.68f,3.16f),0.68f,3.8f);
            GalleryText(surroundings,"Exhibit placard","Enkare Nairobi · before the railway\nAn imagined meeting · draft for cultural review",new Vector3(0,0.28f,3.16f),0.42f,4f);
            var gallery=world.gameObject.AddComponent<LivingGallery>();
            Set(gallery,"painting",work); Set(gallery,"canvas",canvas); Set(gallery,"stage",stage);
            SetArray(gallery,"frame",edges.Cast<Object>().ToArray());
            Set(stage.GetComponent<StoryTransition>(),"gallery",gallery);
            BuildVignettes(stage);
            var unused=stage.Scenes.Single(x=>x.Id=="story.water.1899");
            SetArray(stage,"objects",stage.Objects.Where(x=>x.Scene!="story.water.1899").Cast<Object>().ToArray());
            var stageData=new SerializedObject(stage);var scenes=stageData.FindProperty("scenes");
            for(var i=scenes.arraySize-1;i>=0;i--)
                if(scenes.GetArrayElementAtIndex(i).FindPropertyRelative("Id").stringValue==unused.Id)scenes.DeleteArrayElementAtIndex(i);
            stageData.ApplyModifiedPropertiesWithoutUndo();
            Object.DestroyImmediate(unused.Root);
            var milk=stage.Find("gourd.milk");
            var naserian=stage.Find("figure.naserian").GetComponent<IllustratedFigure>();
            milk.transform.SetParent(naserian.transform.Find("Hands"), false);
            milk.transform.localPosition=new Vector3(0.12f,-0.12f,-0.04f);
            Set(milk,"holder",null);Set(milk,"illustratedHolder",naserian);Set(milk,"handOver",null);
            var app=Object.FindFirstObjectByType<SankofaApp>();
            var objectQuestions=world.gameObject.AddComponent<ObjectQuestions>();
            var questionChips=Enumerable.Range(0,3).Select(i=>BuildPanel(world,SankofaArt.Ensure(),"Object question "+i,Vector3.zero,1)).ToArray();
            foreach(var chip in questionChips) { Set(chip,"buttonSize",new Vector2(0.28f,0.067f));Set(chip,"labelSize",0.3f); }
            Set(objectQuestions,"stage",stage);Set(objectQuestions,"head",GameObject.Find("CenterEyeAnchor").transform);
            SetArray(objectQuestions,"panels",questionChips.Cast<Object>().ToArray());Set(app,"objectQuestions",objectQuestions);
            foreach(var pointer in Object.FindObjectsByType<PointerSource>(FindObjectsSortMode.None))
            {
                var data=new SerializedObject(pointer);var panels=data.FindProperty("panels");
                var original=Enumerable.Range(0,panels.arraySize).Select(i=>panels.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
                SetArray(pointer,"panels",original.Concat(questionChips).ToArray());
            }
            var deposit=world.gameObject.AddComponent<GalleryKeep>();Set(deposit,"app",app);Set(deposit,"stage",stage);
            SetArray(deposit,"hands",Object.FindObjectsByType<HandSource>(FindObjectsSortMode.None).Cast<Object>().ToArray());
            var recess=Marker(world,"Keep recess",new Vector3(0.3f,0.85f,0.42f));Set(deposit,"recess",recess);
            Prim(surroundings,"Keep station",PrimitiveType.Cylinder,new Vector3(0.3f,0.75f,0.42f),new Vector3(0.14f,0.075f,0.14f),wood);
            Prim(surroundings,"Keep recess rim",PrimitiveType.Cylinder,new Vector3(0.3f,0.826f,0.42f),new Vector3(0.17f,0.008f,0.17f),brass);
            GalleryText(surroundings,"Keep recess label","KEEP THIS VISIT",new Vector3(0.3f,0.94f,0.42f),0.13f,0.28f);
            SetArray(app,"storyAmbience",Object.FindObjectsByType<Sankofa.Runtime.Audio.StoryAmbience>(FindObjectsInactive.Include,FindObjectsSortMode.None).Cast<Object>().ToArray());
            Set(app,"gallery",gallery); Set(app,"packageJson",AssetDatabase.LoadAssetAtPath<TextAsset>(GalleryPackage));
            // Keep the existing shelf: legacy kept plans retain their original routes, content and audio.
            Set(app,"storeNamespace","illustrated-theatre-mvp");
            var card=Object.FindFirstObjectByType<StartScreen>();
            foreach(var text in card.GetComponentsInChildren<TextMeshPro>()) text.enabled=false;
            world.Find("Keepsake point").localPosition=new Vector3(0.3f,0.84f,0.42f);
            world.Find("Rest point").localPosition=new Vector3(0.28f,0.77f,0.5f);
            world.Find("Keepsake shown point").localPosition=new Vector3(0.19f,0.94f,0.48f);
            var choices=world.Find("Choices panel");
            choices.localPosition=new Vector3(0,0.90f,0.56f);
            choices.localRotation=FacingVisitor(choices.localPosition);
            Set(choices.GetComponent<PokePanel>(),"buttonSize",new Vector2(0.24f,0.075f));
            Set(choices.GetComponent<PokePanel>(),"labelSize",0.32f);
            Set(choices.GetComponent<PokePanel>(),"gap",0.018f);
            home.Sky=new Color(0.08f,0.075f,0.065f); home.FogStart=30; home.FogEnd=60;
            home.AmbientSky=new Color(0.33f,0.29f,0.25f); home.AmbientGround=new Color(0.13f,0.12f,0.1f);
            home.Grade=new Vector4(1.25f,0.85f,0.1f,0.1f);
            var captions=Object.FindFirstObjectByType<CaptionBoard>();
            captions.transform.localPosition=new Vector3(0,0.84f,1.5f);
            captions.transform.localRotation=FacingVisitor(captions.transform.localPosition);
            Set(captions,"width",1.08f);
            Set(captions.GetComponent<LazyFollow>(),"verticalSlack",24f);
            Set(app,"fireComparison",false);
            // Put the living flame at the painted fire, inside the work; it remains the visible narrator.
            var fire=Object.FindFirstObjectByType<Sankofa.Runtime.Fire.HearthFire>(FindObjectsInactive.Include);
            fire.transform.localPosition=new Vector3(0.03f,0.73f,3.1f); fire.transform.localScale=Vector3.one*0.36f;
            stage.ShowScene(home.Id);
            // CaptureComposition only changes temporary render properties; clear those before saving the scene.
            foreach(var o in stage.Objects) foreach(var r in o.Surfaces) r.SetPropertyBlock(null);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(),GalleryScene);
            EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(GalleryScene,true),new EditorBuildSettingsScene(TheatreScene,true),new EditorBuildSettingsScene(HearthScene,true)};
            AssetDatabase.SaveAssets();
            Debug.Log("[Sankofa] Living gallery built: "+GalleryScene);
        }
        static void BuildModernIllustration(StoryStage stage)
        {
            // The provenance-recorded river illustration supplies distance; the near garden is tangible.
            var image=ImportAtlas("Assets/Sankofa/Art/IllustratedTheatre/modern-river-v1.png");
            var root=stage.Scenes.Single(s=>s.Id=="story.water.today").Root.transform;
            var near=stage.Objects.Where(o=>o.Scene=="story.water.today").SelectMany(o=>o.Surfaces).ToArray();
            var nearFloor=GalleryPigment("Modern bank",new Color(0.43f,0.27f,0.16f));nearFloor.SetFloat("_GroundRadius",1.6f);
            root.Find("Ground").GetComponent<Renderer>().sharedMaterial=nearFloor;
            var gate=stage.Find("gate");gate.transform.localPosition=new Vector3(0.15f,0,0.5f);gate.transform.localRotation=Quaternion.Euler(0,-25,0);
            gate.transform.Find("Touch point").localPosition=new Vector3(-0.12f,0.9f,0.02f);
            foreach(var r in root.GetComponentsInChildren<Renderer>(true))
                if(!near.Contains(r)&&r.name!="Ground")r.enabled=false;
            foreach(var traffic in root.GetComponentsInChildren<Traffic>(true))traffic.enabled=false;
            var path=GalleryFolder+"/Modern river illustration.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Sankofa/Gallery Canvas"));AssetDatabase.CreateAsset(material,path);}
            material.mainTexture=image;EditorUtility.SetDirty(material);
            var card=IllustrationCard(root,"Illustrated modern river",28f,15.75f,material);
            card.transform.localPosition=new Vector3(0,-6.725f,7.5f);
            foreach(var id in new[]{"gate","soil","bead.today"})
                foreach(var r in stage.Find(id).Surfaces)
                    r.sharedMaterial=GalleryPigment("Modern "+id,id=="gate"?new Color(0.22f,0.36f,0.25f):id=="soil"?new Color(0.30f,0.20f,0.13f):new Color(0.65f,0.34f,0.20f));
        }

        static void BuildVignettes(StoryStage stage)
        {
            var root=stage.Scenes.Single(s=>s.Id=="story.water.1850s").Root.transform;
            var list=stage.Objects.Cast<Object>().ToList();
            foreach(var memory in new[]{"portions","waiting"})
            {
                var go=Child(root,"Remembered "+memory);go.localPosition=new Vector3(0,1.44f,1.85f);
                go.localRotation=FacingVisitor(go.localPosition);
                var surfaces=new System.Collections.Generic.List<Renderer>();
                surfaces.Add(Prim(go,"Memory backing",PrimitiveType.Cube,Vector3.zero,new Vector3(0.82f,0.56f,0.022f),MemoryPaper()).GetComponent<Renderer>());
                if(memory=="portions")
                {
                    foreach(var x in new[]{-0.20f,0.20f})
                    {
                        surfaces.Add(Prim(go,"Flour bowl",PrimitiveType.Cylinder,new Vector3(x,-0.17f,-0.14f),new Vector3(0.2f,0.012f,0.16f),GalleryPigment("Flour",new Color(0.73f,0.63f,0.43f)),Quaternion.Euler(32,0,0)).GetComponent<Renderer>());
                        var source=stage.Find("gourd.flour").Surfaces.First();
                        var vessel=Static(go,"Morning vessel",source.GetComponent<MeshFilter>().sharedMesh,source.sharedMaterial).GetComponent<Renderer>();
                        vessel.transform.localPosition=new Vector3(x,-0.18f,-0.06f);vessel.transform.localScale=Vector3.one*0.70f;surfaces.Add(vessel);
                    }
                }
                else
                {
                    var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sankofa/Art/IllustratedTheatre/cast-atlas.png");
                    for(var i=0;i<2;i++)
                    {
                        var card=IllustrationCard(go,"Remembered woman "+i,0.39f,0.46f,IllustrationMaterial("Memory figure "+i,atlas,new Vector4(i*0.5f,2f/3,0.5f,1f/3)));
                        card.transform.localPosition=new Vector3((i-0.5f)*0.37f,-0.24f,-0.04f);surfaces.Add(card);
                    }
                }
                var item=go.gameObject.AddComponent<StageObject>();
                Set(item,"objectId","memory."+memory);Set(item,"sceneId","story.water.1850s");
                Set(item,"scaleWithVisibility",false);SetArray(item,"surfaces",surfaces.Cast<Object>().ToArray());
                list.Add(item);
            }
            SetArray(stage,"objects",list.ToArray());
        }

        static Material MemoryPaper()
        {
            var path=GalleryFolder+"/Memory paper.mat";
            var material=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(material==null){material=new Material(Shader.Find("Unlit/Color"));AssetDatabase.CreateAsset(material,path);}
            material.color=new Color(0.11f,0.125f,0.14f);EditorUtility.SetDirty(material);return material;
        }

        static Material GalleryPigment(string name, Color color)
        {
            var path=GalleryFolder+"/"+name+".mat";
            var m=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(m==null){m=new Material(Shader.Find("Sankofa/Painted Prop"));AssetDatabase.CreateAsset(m,path);}
            m.SetColor("_Color",color); m.SetFloat("_VertexTint",0); m.SetFloat("_InkWidth",0.07f); m.SetFloat("_Visibility",1);
            m.SetTexture("_Noise",AssetDatabase.LoadAssetAtPath<Texture2D>(SankofaArt.NoisePath));
            EditorUtility.SetDirty(m);return m;
        }
        static void GalleryText(Transform root,string name,string words,Vector3 at,float size,float width)
        {
            var go=Child(root,name);go.localPosition=at;
            var text=go.gameObject.AddComponent<TextMeshPro>();text.font=SankofaText.CaptionFont;
            text.text=words;text.fontSize=size;text.color=new Color(0.92f,0.82f,0.64f);
            text.alignment=TextAlignmentOptions.Center;text.rectTransform.sizeDelta=new Vector2(width,0.3f);
        }
        static Texture2D CaptureComposition(StoryStage stage,string id,string name,bool includeObjects=true)
        {
            stage.ShowScene(id);
            var scene=stage.Scenes.Single(s=>s.Id==id).Root;
            var all=scene.GetComponentsInChildren<Transform>(true);
            var oldLayers=all.Select(t=>t.gameObject.layer).ToArray();
            foreach(var t in all)t.gameObject.layer=29;
            foreach(var o in stage.Objects.Where(o=>o.Scene==id))
                foreach(var r in o.Surfaces)
                {
                    r.enabled=includeObjects;
                    if(!includeObjects)continue;
                    var b=new MaterialPropertyBlock();
                    b.SetFloat("_Visibility",1);b.SetFloat("_Unfold",1);b.SetFloat("_PoseBlend",1);
                    if(o.TryGetComponent<IllustratedFigure>(out var f))
                    {var uv = new Vector4(o.Id == "figure.naserian" ? 0.5f : 0, 2f/3, 0.5f, 1f/3); b.SetVector("_FrameA",uv); b.SetVector("_FrameB",uv);}
                    r.SetPropertyBlock(b);
                }
            var cameraObject=new GameObject("Canonical composition camera");var camera=cameraObject.AddComponent<Camera>();
            camera.transform.position=SeatedEye;camera.fieldOfView=65;camera.aspect=16f/9;
            camera.cullingMask=1<<29;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0.05f,0.055f,0.1f);
            var target=new RenderTexture(1920,1080,24){antiAliasing=4};camera.targetTexture=target;
            var previous=RenderTexture.active;camera.Render();RenderTexture.active=target;
            var image=new Texture2D(1920,1080,TextureFormat.RGB24,false);
            image.ReadPixels(new Rect(0,0,1920,1080),0,0);image.Apply();
            var path=GalleryFolder+"/"+name+".png";File.WriteAllBytes(path,image.EncodeToPNG());
            RenderTexture.active=previous;target.Release();Object.DestroyImmediate(target);Object.DestroyImmediate(image);Object.DestroyImmediate(cameraObject);
            for(var i=0;i<all.Length;i++)all[i].gameObject.layer=oldLayers[i];
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);importer.maxTextureSize=2048;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings{name="Android",overridden=true,maxTextureSize=2048,format=TextureImporterFormat.ASTC_6x6});importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }
    }
}
