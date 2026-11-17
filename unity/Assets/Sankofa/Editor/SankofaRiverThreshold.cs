using System.Linq;
using Sankofa.Runtime.Personal;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        public const string RiverThresholdScene = "Assets/Sankofa/Scenes/RiverThresholdPrototype.unity";
        const string RiverArt = "Assets/Sankofa/Art/Generated/RiverThreshold";

        [MenuItem("Sankofa/Build River Threshold Prototype")]
        public static void BuildRiverThresholdPrototype()
        {
            // Save a new scene before modifying anything. No comparison scene or existing generated material is rebuilt.
            var scene = EditorSceneManager.OpenScene(StoryTableScene, OpenSceneMode.Single);
            EditorSceneManager.SaveScene(scene, RiverThresholdScene);
            if (!AssetDatabase.IsValidFolder(RiverArt)) AssetDatabase.CreateFolder(SankofaArt.Folder, "RiverThreshold");
            var world = new GameObject("River threshold world").transform;
            var prototype = new GameObject("Sankofa river threshold prototype").AddComponent<RiverThresholdPrototype>();
            Set(prototype, "world", world);
            var view = Object.FindFirstObjectByType<StoryTableView>();
            BuildRiverStoryObjects(view);
            var ink = RiverMaterial("Quiet ink", "Sankofa/Gallery Surface", new Color(.035f, .047f, .055f));
            ink.SetTexture("_Noise", AssetDatabase.LoadAssetAtPath<Texture2D>(SankofaArt.NoisePath));
            // Retain the real captions and hand controls, with a quieter page beneath the human-scale scene.
            var page = view.TableRoot.Find("Story page");
            page.Find("Page paper").GetComponent<Renderer>().sharedMaterial = ink;
            page.Find("Page shadow").GetComponent<Renderer>().sharedMaterial = ink;
            foreach (var text in page.GetComponentsInChildren<TMP_Text>(true)) text.color = TableCream;

            var opening = Point(world, "Cinematic opening", Vector3.zero);
            Set(prototype, "opening", opening);
            var art = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sankofa/Art/RiverThreshold/hands-and-water-pot-v1.png");
            var import = (TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(art));
            import.maxTextureSize = 2048; import.mipmapEnabled = true; import.wrapMode = TextureWrapMode.Clamp; import.SaveAndReimport();
            var painted = RiverMaterial("Hands and water pot", "Sankofa/Cinematic Paint", Color.white);
            painted.mainTexture = art; painted.SetFloat("_Edge", .09f); painted.renderQueue = 3000;
            painted.SetFloat("_Animation",3);
            var image = Prim(opening, "Opening painting", PrimitiveType.Quad, new Vector3(0, 1.42f, 3.2f), new Vector3(6.4f, 3.6f, 1), painted).GetComponent<Renderer>();
            Set(prototype, "painting", image);
            var fireArt = RiverMaterial("Fire and threads", "Sankofa/Cinematic Paint", Color.white);
            fireArt.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sankofa/Art/RiverThreshold/fire-and-threads-v1.png");
            fireArt.SetFloat("_Edge",.09f); fireArt.renderQueue=2999;
            fireArt.SetFloat("_Animation",1);
            Set(prototype,"firePainting",Prim(opening,"A fire keeping stories",PrimitiveType.Quad,new Vector3(0,1.42f,3.22f),new Vector3(6.4f,3.6f,1),fireArt).GetComponent<Renderer>());
            var ember=RiverCopy("Painted ember strokes",fireArt);
            ember.SetFloat("_Animation",0); ember.SetFloat("_EmberCutout",1); ember.SetFloat("_Edge",.12f);
            ember.SetVector("_UVRect",new Vector4(.49f,.72f,.22f,.14f)); ember.renderQueue=3002; ember.enableInstancing=true;
            var sparks=new Object[12];
            for(var i=0;i<sparks.Length;i++) sparks[i]=Prim(opening,"Painted ember "+i,PrimitiveType.Quad,Vector3.zero,new Vector3(.12f+i%3*.035f,.045f,1),ember).transform;
            SetArray(prototype,"embers",sparks);
            var font = RiverCopy("Cinematic type", SankofaText.CaptionFont.material); font.renderQueue = 3220;
            var title = TableText(opening, "Opening title", "THE GRIOT", new Vector3(-1.28f, 2.06f, 3.05f), 1.05f, new Vector2(2.5f, .3f), new Color(.87f,.61f,.30f), TextAlignmentOptions.Left);
            var words = TableText(opening, "Opening words", "Come.\nSit with me.", new Vector3(-1.28f, 1.53f, 3.04f), 1.6f, new Vector2(2.5f, .86f), TableCream, TextAlignmentOptions.Left);
            var context = TableText(opening, "Opening context", "", new Vector3(-1.28f, .93f, 3.03f), .62f, new Vector2(2.5f, .38f), new Color(.75f,.74f,.69f), TextAlignmentOptions.Left);
            foreach (var text in new[] { title, words, context }) { text.fontSharedMaterial = font; text.overflowMode = TextOverflowModes.Overflow; }
            Set(prototype, "title", title); Set(prototype, "words", words); Set(prototype, "context", context);
            var fire = Object.Instantiate(view.TableRoot.Find("Griot fire").gameObject, opening).transform;
            fire.name = "Opening griot fire"; fire.localPosition = new Vector3(-.38f, .56f, 1.30f); fire.localScale = Vector3.one * .65f;
            Set(prototype, "openingFire", fire.GetComponentInChildren<Sankofa.Runtime.Fire.HearthFire>());
            fire.gameObject.SetActive(false); // The opening's painted flame gives way to the real table fire at the choice.
            Set(prototype,"fireRecording",Recording("small-fire")); Set(prototype,"riverRecording",Recording("brook"));
            Set(prototype,"potTouch",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sankofa/Art/Recorded/RiverThreshold/pot-touch.wav"));
            Set(prototype,"waterStir",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sankofa/Art/Recorded/RiverThreshold/water-stir.wav"));
            Set(prototype,"paperTurn",AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sankofa/Resources/Sounds/stage-rise.wav"));

            var river = Point(world, "Seated riverside", Vector3.zero); Set(prototype, "riverside", river);
            var sky = RiverCopy("River dusk sky", AssetDatabase.LoadAssetAtPath<Material>(SankofaArt.Folder + "/Sky.mat"));
            if (sky == null) sky = RiverMaterial("River dusk sky", "Sankofa/Sky", Color.white);
            sky.SetTexture("_Noise", AssetDatabase.LoadAssetAtPath<Texture2D>(SankofaArt.NoisePath));
            sky.SetColor("_Zenith", new Color(.006f,.016f,.045f)); sky.SetColor("_Horizon", new Color(.055f,.07f,.12f));
            sky.SetColor("_GlowColor", new Color(.20f,.10f,.065f)); sky.SetVector("_GlowDir", new Vector4(-.3f,0,1,0));
            sky.SetFloat("_ShowMoon", 0);
            Prim(river, "Dusk around the seat", PrimitiveType.Sphere, new Vector3(0, 1.15f, 0), Vector3.one * 70, sky);
            var earth = RiverMaterial("River earth", "Sankofa/Cinematic Paint", new Color(.055f,.039f,.030f));
            earth.mainTexture=Texture2D.whiteTexture; earth.SetFloat("_WorldTextureScale",0); earth.SetFloat("_ZWrite",1); earth.renderQueue=2000;
            Prim(river, "Ground below the seat", PrimitiveType.Cylinder, new Vector3(0,-.06f,-.3f), new Vector3(2.4f,.045f,2.4f), earth);
            var riverWater = RiverMaterial("Painted river surround","Sankofa/Cinematic Paint",Color.white);
            riverWater.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sankofa/Art/RiverThreshold/river-at-blue-hour-v1.png");
            riverWater.SetFloat("_Animation",2); riverWater.SetFloat("_ZWrite",1); riverWater.SetFloat("_Edge",.025f); riverWater.renderQueue=2000;
            Set(prototype, "water", Static(river, "River beside the seat", CurvedRiverPainting(), riverWater).GetComponent<Renderer>());
            // Painted layers at different real depths create stereo parallax without moving the viewer.
            RiverCard(river, "Near left reeds", new Vector3(-2.1f,.15f,1.3f), new Vector2(1.7f,.56f), "Stage reeds left", 40);
            RiverCard(river, "Near right reeds", new Vector3(2.25f,.15f,1.1f), new Vector2(1.7f,.56f), "Stage reeds right", -40);
            // Side and rear scenery surrounds a seated visitor; no essential action requires turning around.
            foreach (var side in new[] { -1, 1 })
            {
                RiverCard(river, "Side trees " + side, new Vector3(side*8,2.0f,1), new Vector2(19,7), "Stage hills", side * -70);
                RiverCard(river, "Rear trees " + side, new Vector3(side*5,2.0f,-8), new Vector2(19,7), "Stage hills", side * -155);
            }
            var people = new Object[2];
            for (var i = 0; i < 2; i++)
            {
                var person = Prim(river, i == 0 ? "The one who remembers" : "The younger traveller", PrimitiveType.Quad,
                    new Vector3(i == 0 ? -.9f : .18f, .83f, i == 0 ? 2.42f : 2.78f), new Vector3(1.66f,1.66f,1),
                    AssetDatabase.LoadAssetAtPath<Material>(TableArtFolder + (i == 0 ? "/Printed Wambui.mat" : "/Printed Naserian.mat")));
                people[i] = person.GetComponent<Renderer>();
            }
            SetArray(prototype, "people", people);
            river.gameObject.SetActive(false);

            var hero = Object.Instantiate(view.TableRoot.Find("Pop-up stage/Heirloom Water pot").gameObject, world).transform;
            hero.name = "The chosen water pot"; hero.localPosition = new Vector3(0,.88f,1.25f); hero.localScale = Vector3.one * 3;
            Set(prototype, "heroPot", hero); hero.gameObject.SetActive(false);
            var cover = RiverMaterial("Ink dissolve", "Sankofa/Cinematic Paint", new Color(.018f,.024f,.035f));
            cover.SetFloat("_ZTest", 8); cover.SetFloat("_Opacity", 0); cover.renderQueue = 3100;
            Set(prototype, "veil", Prim(world, "Ink dissolve around the seat", PrimitiveType.Sphere, new Vector3(0,1.15f,0), Vector3.one * 15, cover).GetComponent<Renderer>());
            var transitionText = TableText(world, "Threshold caption", "", new Vector3(0,1.43f,2.0f), .7f, new Vector2(2.7f,.45f), TableCream);
            transitionText.fontSharedMaterial = font; Set(prototype, "thresholdCaption", transitionText);
            var controlsInk = RiverCopy("Cinematic control faces", ink); controlsInk.renderQueue = 3290;
            var panel = TablePanel(world, "Cinematic hand controls", new Vector3(0,.82f,.70f), 3, new Vector2(.18f,.046f), controlsInk);
            panel.transform.localRotation = Quaternion.Euler(26,0,0);
            var controlFont=RiverCopy("Cinematic control labels",SankofaText.LabelFont.material); controlFont.renderQueue=3310;
            Set(panel, "labelMaterial", controlFont); Set(panel, "labelSize", .11f); Set(prototype, "controls", panel);
            foreach (var r in world.GetComponentsInChildren<Renderer>(true)) { r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false; }
            EditorSceneManager.SaveScene(scene, RiverThresholdScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sankofa] Separate river-threshold prototype built: " + RiverThresholdScene);
        }

        static Material RiverMaterial(string name, string shader, Color color)
        {
            var path = RiverArt + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find(shader); if(material.HasProperty("_Color")) material.color = color; EditorUtility.SetDirty(material); return material;
        }
        static Material RiverCopy(string name, Material source)
        {
            if (source == null) return null;
            var path = RiverArt + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(source); AssetDatabase.CreateAsset(material, path); }
            else material.CopyPropertiesFromMaterial(source);
            EditorUtility.SetDirty(material); return material;
        }
        static void RiverCard(Transform parent, string name, Vector3 at, Vector2 size, string material, float yaw = 0)
        {
            var paint=AssetDatabase.LoadAssetAtPath<Material>(TableArtFolder + "/" + material + ".mat");
            if(material.Contains("reeds"))
            {
                paint=RiverCopy("Whole painted reeds",paint);
                paint.SetVector("_FrameA",new Vector4(0,0,1,.34f)); paint.SetVector("_FrameB",new Vector4(0,0,1,.34f));
            }
            Prim(parent, name, PrimitiveType.Quad, at, new Vector3(size.x,size.y,1), paint, Quaternion.Euler(0,yaw,0));
        }
        static Mesh CurvedRiverPainting()
        {
            const int steps = 80;
            var vertices = new Vector3[(steps+1)*2]; var uv = new Vector2[vertices.Length]; var indices = new int[steps*6];
            for (var i=0; i<=steps; i++)
            {
                var u=i/(float)steps; var angle=Mathf.Lerp(-75,75,u)*Mathf.Deg2Rad;
                var x=Mathf.Sin(angle)*5.5f; var z=Mathf.Cos(angle)*5.5f;
                vertices[i*2] = new Vector3(x,-2.6f,z); vertices[i*2+1] = new Vector3(x,5.5f,z);
                uv[i*2] = new Vector2(u,0); uv[i*2+1] = new Vector2(u,1);
                if (i==steps) continue;
                var a=i*2; var k=i*6;
                indices[k]=a; indices[k+1]=a+2; indices[k+2]=a+1;
                indices[k+3]=a+2; indices[k+4]=a+3; indices[k+5]=a+1;
            }
            var mesh = new Mesh { name="Curved painted river", vertices=vertices, uv=uv, triangles=indices };
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            var path = RiverArt + "/Curved river painting.asset";
            var saved = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (saved == null) { AssetDatabase.CreateAsset(mesh,path); return mesh; }
            EditorUtility.CopySerialized(mesh,saved); Object.DestroyImmediate(mesh); return saved;
        }
    }
}
