using System;
using System.Linq;
using Sankofa.Runtime.Fire;
using Sankofa.Runtime.Personal;
using Sankofa.Runtime.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        public const string StoryTableScene = "Assets/Sankofa/Scenes/StoryTable.unity";
        const string TableArtFolder = "Assets/Sankofa/Art/Generated/StoryTable";
        static readonly Color TableInk = new Color(0.105f, 0.075f, 0.057f);
        static readonly Color TableCream = new Color(0.94f, 0.87f, 0.73f);

        /// <summary>A new standalone scene; existing gallery/comparison scenes and their generated art are untouched.</summary>
        [MenuItem("Sankofa/Build Personal Story Table")]
        public static void BuildStoryTable()
        {
            if (!AssetDatabase.IsValidFolder(TableArtFolder)) AssetDatabase.CreateFolder(SankofaArt.Folder, "StoryTable");
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var rig = CreateRig();
            rig.Eye.backgroundColor = new Color(0.024f, 0.028f, 0.038f);
            var anchor = new GameObject("Story table world").AddComponent<Sankofa.Runtime.App.WorldAnchor>();
            Set(anchor, "head", rig.Head);
            var world = anchor.transform;
            var table = Point(world, "Story table surface", new Vector3(0, 0.68f, 0.72f));
            var view = table.gameObject.AddComponent<StoryTableView>();
            Set(view, "tableRoot", table);

            var paper = TableMaterial("Warm paper", TableCream);
            var pageEdge = TableMaterial("Paper edges", new Color(0.62f, 0.49f, 0.32f));
            var ink = TableMaterial("Ink panels", new Color(0.105f, 0.09f, 0.08f));
            var brass = TableMaterial("Warm brass", new Color(0.49f, 0.30f, 0.12f));
            var timber = TableMaterial("Walnut deck", new Color(0.14f, 0.095f, 0.065f));
            var secondary = TableMaterial("Slate panels", new Color(0.19f, 0.23f, 0.24f));
            var shadow = TableMaterial("Page shadow", new Color(0.045f, 0.04f, 0.035f));

            // A modest virtual stand is the full-VR fallback; room placement can hide it when a real surface is used.
            var stand = Child(table, "Virtual table");
            Prim(stand, "Rounded walnut table", PrimitiveType.Cube, new Vector3(0, -0.035f, 0), new Vector3(1.25f, 0.055f, 0.72f), timber);
            Prim(stand, "Inset writing surface", PrimitiveType.Cube, new Vector3(0, -0.006f, 0), new Vector3(1.20f, 0.008f, 0.67f), ink);
            foreach (var x in new[] { -0.48f, 0.48f })
                Prim(stand, "Table support", PrimitiveType.Cube, new Vector3(x, -0.34f, 0.20f), new Vector3(0.055f, 0.63f, 0.055f), timber);
            Prim(stand, "Front brass seam", PrimitiveType.Cube, new Vector3(0, -0.005f, -0.342f), new Vector3(1.19f, 0.005f, 0.005f), brass);

            var book = Point(table, "Upright illustrated journal", new Vector3(0, 0.49f, 0.27f));
            book.localRotation = Quaternion.Euler(4, 0, 0);
            Prim(book, "Quiet folio shadow", PrimitiveType.Cube, new Vector3(0.008f, -0.006f, 0.012f), new Vector3(0.982f, 0.712f, 0.014f), shadow);
            Prim(book, "Journal cover", PrimitiveType.Cube, new Vector3(0, 0, 0.008f), new Vector3(0.976f, 0.706f, 0.013f), timber);
            for (var i = 0; i < 3; i++)
                Prim(book, "Paper leaf " + i, PrimitiveType.Cube, new Vector3(i * 0.002f, -i * 0.001f, 0.002f - i * 0.001f), new Vector3(0.95f, 0.682f, 0.001f), i == 2 ? paper : pageEdge);
            Prim(book, "Binding", PrimitiveType.Cube, new Vector3(-0.463f, 0, -0.002f), new Vector3(0.012f, 0.675f, 0.005f), brass);
            TableRule(book, new Vector3(0, 0.205f, -0.006f), 0.85f, brass);
            TableRule(book, new Vector3(0, -0.134f, -0.007f), 0.85f, pageEdge);

            Set(view, "heading", TableText(book, "Story name", "Sankofa", new Vector3(0, 0.410f, -0.005f), 0.25f, new Vector2(1.04f, 0.065f), TableCream));
            Set(view, "subheading", TableText(book, "Story setting", "A personal history, one page at a time", new Vector3(0, 0.356f, -0.005f), 0.13f, new Vector2(1.04f, 0.045f), TableCream));
            Set(view, "pageNumber", TableText(book, "Page number", "YOUR STORY TABLE", new Vector3(0, 0.306f, -0.008f), 0.10f, new Vector2(0.85f, 0.028f), TableInk));
            Set(view, "chapterTitle", TableText(book, "Chapter title", "Every story begins with a question", new Vector3(0, 0.252f, -0.008f), 0.22f, new Vector2(0.86f, 0.070f), TableInk));
            Set(view, "caption", TableText(book, "Narrative captions", "Choose a thread below, or bring a small fragment of your own.", new Vector3(0, -0.221f, -0.011f), 0.16f, new Vector2(0.86f, 0.146f), TableInk, TextAlignmentOptions.TopLeft));
            Set(view, "illustrationLabel", TableText(book, "Illustration context", "Illustrated reconstruction", new Vector3(0, -0.112f, -0.012f), 0.095f, new Vector2(0.84f, 0.029f), TableInk));
            Set(view, "capability", TableText(book, "Story availability", "Prepared story • works offline", new Vector3(0, -0.321f, -0.012f), 0.10f, new Vector2(0.84f, 0.031f), TableInk));

            var illustration = Point(book, "One flat illustrated page", new Vector3(0, 0.045f, -0.008f));
            var cast = BuildTableIllustration(illustration, "Cast and exchange", true, false);
            var scenery = BuildTableIllustration(illustration, "River landscape", false, false);
            var modern = BuildTableIllustration(illustration, "Same water today", false, true);
            var viewData = new SerializedObject(view);
            var items = viewData.FindProperty("illustrations"); items.arraySize = 4;
            var keys = new[] { "exchange", "cast-atlas", "scenery-atlas", "modern-river" };
            var roots = new[] { cast, cast, scenery, modern };
            for (var i = 0; i < keys.Length; i++)
            {
                items.GetArrayElementAtIndex(i).FindPropertyRelative("Key").stringValue = keys[i];
                items.GetArrayElementAtIndex(i).FindPropertyRelative("Root").objectReferenceValue = roots[i];
            }
            viewData.ApplyModifiedPropertiesWithoutUndo();
            var custom = Prim(illustration, "Created illustration", PrimitiveType.Quad, new Vector3(0, 0, -0.001f), new Vector3(0.85f, 0.29f, 1), TableImageMaterial("Created page image", null));
            custom.SetActive(false); Set(view, "customIllustration", custom.GetComponent<Renderer>());
            cast.SetActive(true); scenery.SetActive(false); modern.SetActive(false);

            var previous = new Object[2];
            for (var i = 0; i < 2; i++)
            {
                var leaf = Point(table, "Earlier page " + (i + 1), new Vector3(i == 0 ? -0.48f : 0.48f, 0.044f, 0.125f));
                leaf.localRotation = Quaternion.Euler(76, 0, i == 0 ? -7 : 7);
                Prim(leaf, "Earlier paper", PrimitiveType.Cube, Vector3.zero, new Vector3(0.205f, 0.20f, 0.003f), paper);
                TableRule(leaf, new Vector3(0, 0.049f, -0.003f), 0.165f, pageEdge);
                previous[i] = TableText(leaf, "Earlier page title", "", new Vector3(0, 0, -0.004f), 0.115f, new Vector2(0.174f, 0.16f), TableInk);
            }
            SetArray(view, "previousTitles", previous);
            SetArray(view, "turningPages", new Object[] { book });

            // The same compact deck carries intake, reading and recovery; the far illustration stays dominant.
            var intake = TableDeck(table, "Choose your story", new Vector3(0, 0.145f, -0.17f), new Vector2(0.85f, 0.36f), paper);
            Set(view, "intakeRoot", intake.gameObject);
            Set(view, "intakeHeading", TableText(intake, "Intake heading", "What would you like to understand?", new Vector3(0, 0.145f, -0.006f), 0.14f, new Vector2(0.78f, 0.035f), TableInk));
            Set(view, "routes", TablePanel(intake, "Interest choices", new Vector3(0, 0.092f, -0.007f), 3, new Vector2(0.237f, 0.052f), ink));
            Set(view, "regions", TablePanel(intake, "Place choice", new Vector3(-0.187f, 0.026f, -0.007f), 1, new Vector2(0.365f, 0.047f), secondary));
            Set(view, "periods", TablePanel(intake, "Period choice", new Vector3(0.187f, 0.026f, -0.007f), 1, new Vector2(0.365f, 0.047f), secondary));
            Set(view, "intakeActions", TablePanel(intake, "Start choices", new Vector3(0, -0.044f, -0.007f), 3, new Vector2(0.237f, 0.062f), ink));
            Set(view, "seedText", TableText(intake, "Your question", "Your question or family fragment (optional)", new Vector3(0, -0.105f, -0.007f), 0.105f, new Vector2(0.76f, 0.042f), TableInk));
            Set(view, "intakeSummary", TableText(intake, "Selection summary", "Trade & home / Enkare Nairobi / around the 1850s", new Vector3(0, -0.145f, -0.007f), 0.09f, new Vector2(0.76f, 0.029f), TableInk));

            var reading = TableDeck(table, "Read and wonder", new Vector3(0, 0.12f, -0.205f), new Vector2(0.85f, 0.30f), paper);
            Set(view, "readingRoot", reading.gameObject);
            TableText(reading, "Reading guidance", "Read in your own time. Point and pinch, or touch a button.", new Vector3(0, 0.115f, -0.006f), 0.10f, new Vector2(0.76f, 0.031f), TableInk);
            Set(view, "questions", TablePanel(reading, "Questions beside the story", new Vector3(0, 0.049f, -0.007f), 2, new Vector2(0.366f, 0.062f), secondary));
            Set(view, "transport", TablePanel(reading, "Turn a page", new Vector3(0, -0.026f, -0.007f), 3, new Vector2(0.237f, 0.061f), ink));

            var details = TableDeck(table, "Question and context", new Vector3(0, 0.18f, -0.13f), new Vector2(0.85f, 0.46f), paper);
            Set(view, "detailsRoot", details.gameObject);
            Set(view, "detailHeading", TableText(details, "Context heading", "A closer look", new Vector3(0, 0.177f, -0.006f), 0.18f, new Vector2(0.77f, 0.065f), TableInk));
            Set(view, "detailBody", TableText(details, "Context answer", "", new Vector3(0, -0.025f, -0.006f), 0.135f, new Vector2(0.76f, 0.30f), TableInk, TextAlignmentOptions.TopLeft));
            var progress = TableDeck(table, "Preparing and recovery", new Vector3(0, 0.15f, -0.16f), new Vector2(0.85f, 0.35f), paper);
            Set(view, "progressRoot", progress.gameObject);
            Set(view, "progressText", TableText(progress, "Preparation status", "Gathering the next pages…", new Vector3(0, 0.035f, -0.006f), 0.17f, new Vector2(0.75f, 0.21f), TableInk));
            var detailActions = TablePanel(table, "Return and recovery actions", new Vector3(0, 0.075f, -0.260f), 3, new Vector2(0.237f, 0.058f), ink);
            detailActions.transform.localRotation = Quaternion.Euler(50, 0, 0);
            Set(view, "detailActions", detailActions);

            var consent = TableDeck(table, "Online story consent", new Vector3(0, 0.17f, -0.14f), new Vector2(0.85f, 0.43f), paper);
            Set(view, "cloudConsentRoot", consent.gameObject);
            TableText(consent, "Online consent heading", "Create a story from your question", new Vector3(0, 0.155f, -0.006f), 0.18f, new Vector2(0.76f, 0.057f), TableInk);
            TableText(consent, "Online consent notice", "Your selected place, period, question and fragment will be sent to an online story service.\n\nShare only what you are comfortable sending. The prepared stories work without this.", new Vector3(0, 0.012f, -0.006f), 0.14f, new Vector2(0.75f, 0.22f), TableInk, TextAlignmentOptions.TopLeft);
            Set(view, "consentActions", TablePanel(consent, "Online consent actions", new Vector3(0, -0.157f, -0.007f), 2, new Vector2(0.355f, 0.059f), ink));

            var controls = TablePanel(table, "Always available controls", new Vector3(0, 0.017f, -0.335f), 4, new Vector2(0.173f, 0.044f), ink);
            controls.transform.localRotation = Quaternion.Euler(58, 0, 0);
            Set(view, "controls", controls);
            Set(view, "fire", BuildStoryTableFire(table));

            var pointerArt = new SankofaArtSet { Pointer = AssetDatabase.LoadAssetAtPath<Material>(SankofaArt.Folder + "/Pointer.mat") };
            BuildPointers(rig, pointerArt, view.Panels);
            table.gameObject.AddComponent<StoryTableAtmosphere>();
            var appType = Type.GetType("Sankofa.Runtime.Personal.PersonalStoryApp, Sankofa.Runtime");
            if (appType != null) new GameObject("Sankofa personal story").AddComponent(appType);
            else Debug.LogWarning("[Sankofa] Story table presentation built without the application adapter.");
            reading.gameObject.SetActive(false); details.gameObject.SetActive(false); progress.gameObject.SetActive(false); consent.gameObject.SetActive(false);
            detailActions.Hidden = true;
            EditorSceneManager.SaveScene(scene, StoryTableScene);
            AssetDatabase.SaveAssets();
            Debug.Log("[Sankofa] Story table scene built: " + StoryTableScene);
        }

        static Transform TableDeck(Transform table, string name, Vector3 at, Vector2 size, Material paper)
        {
            var deck = Point(table, name, at); deck.localRotation = Quaternion.Euler(45, 0, 0);
            Prim(deck, "Paper deck", PrimitiveType.Cube, Vector3.zero, new Vector3(size.x, size.y, 0.005f), paper);
            return deck;
        }

        static PokePanel TablePanel(Transform parent, string name, Vector3 at, int columns, Vector2 size, Material material)
        {
            var panel = Point(parent, name, at).gameObject.AddComponent<PokePanel>();
            Set(panel, "columns", columns); Set(panel, "buttonSize", size); Set(panel, "gap", 0.012f);
            Set(panel, "buttonMaterial", material); Set(panel, "labelFont", SankofaText.LabelFont);
            Set(panel, "labelColor", TableCream); Set(panel, "labelSize", 0.14f);
            Set(panel, "idle", new Color(0.13f, 0.15f, 0.15f));
            Set(panel, "hover", new Color(0.31f, 0.35f, 0.33f));
            Set(panel, "pressed", new Color(0.60f, 0.34f, 0.12f));
            Set(panel, "plaqueDepth", 0.003f); Set(panel, "plaqueBodyMaterial", material);
            return panel;
        }

        static TextMeshPro TableText(Transform parent, string name, string words, Vector3 at, float size, Vector2 bounds, Color color, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var text = Point(parent, name, at).gameObject.AddComponent<TextMeshPro>();
            text.font = SankofaText.CaptionFont; text.fontSharedMaterial = text.font.material;
            text.fontSize = size; text.text = words; text.color = color; text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal; text.rectTransform.sizeDelta = bounds;
            text.overflowMode = TextOverflowModes.Truncate; text.richText = false;
            text.lineSpacing = 3;
            return text;
        }

        static void TableRule(Transform parent, Vector3 at, float width, Material material) =>
            Prim(parent, "Fine rule", PrimitiveType.Quad, at, new Vector3(width, 0.0015f, 1), material);

        static Material TableMaterial(string name, Color color)
        {
            var path = TableArtFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("UI/Default")) { name = name }; AssetDatabase.CreateAsset(material, path); }
            material.color = color; EditorUtility.SetDirty(material); return material;
        }

        static Material TableImageMaterial(string name, Texture texture)
        {
            var path = TableArtFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Sankofa/Gallery Canvas")) { name = name }; AssetDatabase.CreateAsset(material, path); }
            material.mainTexture = texture; material.SetFloat("_Opacity", 1); EditorUtility.SetDirty(material); return material;
        }

        static Material TableAtlasMaterial(string name, string asset, Vector4 frame)
        {
            var path = TableArtFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Sankofa/Illustration")) { name = name }; AssetDatabase.CreateAsset(material, path); }
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sankofa/Art/IllustratedTheatre/" + asset);
            material.SetVector("_FrameA", frame); material.SetVector("_FrameB", frame);
            material.SetFloat("_PoseBlend", 1); material.SetFloat("_Visibility", 1); material.SetFloat("_Unfold", 1);
            EditorUtility.SetDirty(material); return material;
        }

        static GameObject BuildTableIllustration(Transform parent, string name, bool people, bool modern)
        {
            var root = Child(parent, name);
            if (modern)
            {
                Prim(root, "Printed modern river", PrimitiveType.Quad, Vector3.zero, new Vector3(0.85f, 0.29f, 1), TableImageMaterial("Modern river page", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sankofa/Art/IllustratedTheatre/modern-river-v1.png")));
                return root.gameObject;
            }
            Prim(root, "Printed dusk", PrimitiveType.Quad, Vector3.zero, new Vector3(0.85f, 0.29f, 1), TableMaterial("Printed dusk", new Color(0.29f, 0.23f, 0.27f)));
            // Atlas parts form one flat print; fractions of a millimetre only establish its graphic draw order.
            Prim(root, "Printed forest and hills", PrimitiveType.Quad, new Vector3(0, 0.030f, -0.0002f), new Vector3(0.85f, 0.23f, 1), TableAtlasMaterial("Printed landscape", "scenery-atlas.png", new Vector4(0, 0.625f, 1, 0.37f)));
            Prim(root, "Printed riverbank", PrimitiveType.Quad, new Vector3(0, -0.062f, -0.0004f), new Vector3(0.85f, 0.16f, 1), TableAtlasMaterial("Printed water", "scenery-atlas.png", new Vector4(0, 0.36f, 1, 0.26f)));
            if (people)
            {
                Prim(root, "Printed trader left", PrimitiveType.Quad, new Vector3(-0.15f, -0.015f, -0.0006f), new Vector3(0.16f, 0.24f, 1), TableAtlasMaterial("Printed Wambui", "cast-atlas.png", new Vector4(0, 1f / 3, 0.5f, 1f / 3)));
                Prim(root, "Printed trader right", PrimitiveType.Quad, new Vector3(0.15f, -0.015f, -0.0008f), new Vector3(0.16f, 0.24f, 1), TableAtlasMaterial("Printed Naserian", "cast-atlas.png", new Vector4(0.5f, 1f / 3, 0.5f, 1f / 3)));
            }
            return root.gameObject;
        }

        static HearthFire BuildStoryTableFire(Transform table)
        {
            var root = Point(table, "Quiet tabletop fire", new Vector3(-0.515f, 0.03f, -0.17f));
            root.localScale = Vector3.one * 0.25f;
            var fire = root.gameObject.AddComponent<HearthFire>();
            var flame = AssetDatabase.LoadAssetAtPath<Material>(SankofaArt.Folder + "/Flame.mat");
            var layers = new Object[3]; var cards = new Object[3];
            for (var i = 0; i < 3; i++)
            {
                var layer = Point(root, "Silent flame " + i, new Vector3((i - 1) * 0.045f, 0, i * 0.025f));
                layer.gameObject.AddComponent<Billboard>();
                layers[i] = layer;
                cards[i] = Prim(layer, "Flame", PrimitiveType.Quad, new Vector3(0, 0.5f, 0), Vector3.one, flame).GetComponent<Renderer>();
            }
            SetArray(fire, "layers", layers); SetArray(fire, "cards", cards);
            Set(fire, "lightPower", 0.55f); Set(fire, "lightAttenuation", 2f);
            Prim(root, "Bronze ember cup", PrimitiveType.Cylinder, new Vector3(0, -0.018f, 0), new Vector3(0.58f, 0.045f, 0.58f), TableMaterial("Ember bowl", new Color(0.29f, 0.16f, 0.08f)));
            return fire;
        }
    }
}
