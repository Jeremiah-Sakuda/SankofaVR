using System;
using System.Collections.Generic;
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
            // Nothing in this scene casts a realtime shadow, the visitor's rendered hands included.
            foreach (var renderer in rig.Head.root.GetComponentsInChildren<Renderer>(true))
            { renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; renderer.receiveShadows = false; }
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

            // The story is performed on a pop-up paper stage raised on a low drum; the words and actions sit on one open
            // page in front of it, the way a pop-up book keeps its text below the scene.
            var stage = BuildPopUpStage(table, ink, brass, out var heirlooms, out var plaques);
            Set(view, "stage", stage);
            SetArray(view, "heirlooms", heirlooms);
            SetArray(view, "figureChoices", plaques);

            // One open page in front of the stage holds the words and every action, low enough to keep the stage in view.
            var page = Point(table, "Story page", new Vector3(0, 0.072f, -0.222f));
            page.localRotation = Quaternion.Euler(66, 0, 0);
            Prim(page, "Page shadow", PrimitiveType.Cube, new Vector3(0.004f, -0.004f, 0.006f), new Vector3(0.668f, 0.302f, 0.004f), shadow);
            Prim(page, "Page paper", PrimitiveType.Cube, Vector3.zero, new Vector3(0.66f, 0.30f, 0.005f), paper);
            TableRule(page, new Vector3(0, 0.080f, -0.003f), 0.60f, brass);
            Set(view, "heading", TableText(page, "Story name", "Sankofa", new Vector3(-0.15f, 0.133f, -0.004f), 0.12f, new Vector2(0.30f, 0.02f), TableInk, TextAlignmentOptions.Left));
            Set(view, "subheading", TableText(page, "Story setting", "For the stories you never got to hear", new Vector3(0.11f, 0.133f, -0.004f), 0.08f, new Vector2(0.38f, 0.02f), TableInk, TextAlignmentOptions.Right));
            var pageNumber = TableText(page, "Page number", "YOUR STORY TABLE", new Vector3(0, 0.114f, -0.004f), 0.07f, new Vector2(0.60f, 0.014f), TableInk);
            Set(view, "pageNumber", pageNumber); Set(view, "intakeSummary", pageNumber);
            Set(view, "chapterTitle", TableText(page, "Chapter title", "Every story begins with a question", new Vector3(0, 0.095f, -0.004f), 0.12f, new Vector2(0.60f, 0.02f), TableInk));
            Set(view, "caption", TableText(page, "Narrative captions", "Choose what you would carry back.", new Vector3(0, 0.038f, -0.004f), 0.15f, new Vector2(0.62f, 0.076f), TableInk, TextAlignmentOptions.TopLeft));
            var seedPreview = TableText(page, "Your question preview", "", new Vector3(0, 0.038f, -0.005f), 0.14f, new Vector2(0.62f, 0.076f), TableInk, TextAlignmentOptions.TopLeft);
            Set(view, "seedText", seedPreview); seedPreview.gameObject.SetActive(false);
            Set(view, "illustrationLabel", TableText(page, "Illustration context", "Illustrated reconstruction", new Vector3(-0.155f, -0.008f, -0.004f), 0.07f, new Vector2(0.30f, 0.013f), TableInk, TextAlignmentOptions.Left));
            Set(view, "capability", TableText(page, "Story availability", "Prepared story • works offline", new Vector3(0.155f, -0.008f, -0.004f), 0.07f, new Vector2(0.30f, 0.013f), TableInk, TextAlignmentOptions.Right));
            SetArray(view, "previousTitles", new Object[0]);
            SetArray(view, "turningPages", new Object[] { page });

            // Intake and reading actions share the lower half of the page; only one set is shown at a time.
            var intake = Point(page, "Choose your story", Vector3.zero);
            Set(view, "intakeRoot", intake.gameObject);
            // Centred between the two page labels and above the buttons (whose top edge is at -0.026), never behind them.
            Set(view, "intakeHeading", TableText(intake, "Intake heading", "", new Vector3(0, -0.0155f, -0.004f), 0.07f, new Vector2(0.28f, 0.011f), TableInk));
            Set(view, "keptActions", TablePanel(intake, "Kept story choices", new Vector3(0, -0.094f, -0.007f), 2, new Vector2(0.29f, 0.04f), ink));
            Set(view, "intakeActions", TablePanel(intake, "Start choices", new Vector3(0, -0.047f, -0.007f), 3, new Vector2(0.19f, 0.042f), ink));
            var reading = Point(page, "Read and wonder", Vector3.zero);
            Set(view, "readingRoot", reading.gameObject);
            Set(view, "transport", TablePanel(reading, "Turn a page", new Vector3(0, -0.047f, -0.007f), 4, new Vector2(0.143f, 0.042f), ink));
            Set(view, "questions", TablePanel(reading, "Questions beside the story", new Vector3(0, -0.094f, -0.007f), 2, new Vector2(0.29f, 0.04f), secondary));

            // Answers, preparation and consent open over the stage, which waits behind them.
            var details = TableDeck(table, "Question and context", new Vector3(0, 0.23f, 0.02f), new Vector2(0.85f, 0.39f), paper);
            details.Find("Paper deck").localPosition = new Vector3(0, 0.015f, 0);
            Set(view, "detailsRoot", details.gameObject);
            Set(view, "detailHeading", TableText(details, "Context heading", "A closer look", new Vector3(0, 0.177f, -0.006f), 0.18f, new Vector2(0.77f, 0.065f), TableInk));
            Set(view, "detailBody", TableText(details, "Context answer", "", new Vector3(0, 0, -0.006f), 0.135f, new Vector2(0.76f, 0.25f), TableInk, TextAlignmentOptions.TopLeft));
            var progress = TableDeck(table, "Preparing and recovery", new Vector3(0, 0.19f, 0), new Vector2(0.85f, 0.31f), paper);
            progress.Find("Paper deck").localPosition = new Vector3(0, 0.02f, 0);
            Set(view, "progressRoot", progress.gameObject);
            Set(view, "progressText", TableText(progress, "Preparation status", "Gathering the next pages…", new Vector3(0, 0.035f, -0.006f), 0.17f, new Vector2(0.75f, 0.21f), TableInk));
            var detailActions = TablePanel(table, "Return and recovery actions", new Vector3(0, 0.118f, -0.102f), 3, new Vector2(0.237f, 0.058f), ink);
            detailActions.transform.localRotation = Quaternion.Euler(45, 0, 0);
            Set(view, "detailActions", detailActions);

            var consent = TableDeck(table, "Online story consent", new Vector3(0, 0.23f, 0.02f), new Vector2(0.85f, 0.39f), paper);
            consent.Find("Paper deck").localPosition = new Vector3(0, 0.015f, 0);
            Set(view, "cloudConsentRoot", consent.gameObject);
            Set(view, "consentHeading", TableText(consent, "Online consent heading", "Create a story from your question", new Vector3(0, 0.155f, -0.006f), 0.18f, new Vector2(0.76f, 0.057f), TableInk));
            Set(view, "consentBody", TableText(consent, "Online consent notice", "Your selected place, period, question and fragment will be sent to an online story service.\n\nShare only what you are comfortable sending. The prepared stories work without this.", new Vector3(0, 0.012f, -0.006f), 0.14f, new Vector2(0.75f, 0.22f), TableInk, TextAlignmentOptions.TopLeft));
            Set(view, "consentActions", TablePanel(consent, "Online consent actions", new Vector3(0, -0.157f, -0.007f), 2, new Vector2(0.355f, 0.059f), ink));

            var controls = TablePanel(page, "Always available controls", new Vector3(0, -0.133f, -0.007f), 3, new Vector2(0.15f, 0.03f), ink);
            Set(controls, "gap", 0.008f); Set(controls, "labelSize", 0.11f);
            Set(view, "controls", controls);
            Set(view, "fire", BuildStoryTableFire(table));

            var pointerArt = new SankofaArtSet { Pointer = AssetDatabase.LoadAssetAtPath<Material>(SankofaArt.Folder + "/Pointer.mat") };
            BuildPointers(rig, pointerArt, view.Panels);
            table.gameObject.AddComponent<StoryTableAtmosphere>();
            var appType = Type.GetType("Sankofa.Runtime.Personal.PersonalStoryApp, Sankofa.Runtime");
            if (appType != null)
            {
                var app = new GameObject("Sankofa personal story").AddComponent(appType);
                // The app owns the fire's sound and plays it on the headset only; the tabletop view stays silent.
                Set(app, "fireRecording", Recording("small-fire"));
                Set(app, "riverRecording", Recording("brook"));
                Set(app, "nightRecording", Recording("crickets"));
            }
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
            if (material == null) { material = new Material(Shader.Find("Sankofa/Gallery Surface")) { name = name }; AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find("Sankofa/Gallery Surface"); material.color = color;
            material.SetTexture("_Noise", AssetDatabase.LoadAssetAtPath<Texture2D>(SankofaArt.NoisePath));
            material.SetFloat("_Visibility", 1); material.SetFloat("_SrcBlend", 1); material.SetFloat("_DstBlend", 0); material.SetFloat("_ZWrite", 1);
            EditorUtility.SetDirty(material); return material;
        }

        static Material TableImageMaterial(string name, Texture texture)
        {
            var path = TableArtFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Sankofa/Gallery Canvas")) { name = name }; AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find("Sankofa/Gallery Canvas");
            material.mainTexture = texture; material.SetFloat("_Opacity", 1); EditorUtility.SetDirty(material); return material;
        }

        /// <summary>A whole painted texture through the illustrations' own depth-written shader: never blended, so a large
        /// stage surface adds no transparent overdraw on a mobile GPU.</summary>
        static Material TablePaintMaterial(string name, Texture2D texture)
        {
            var path = TableArtFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Sankofa/Illustration")) { name = name }; AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find("Sankofa/Illustration"); material.mainTexture = texture;
            var whole = new Vector4(0, 0, 1, 1);
            material.SetVector("_FrameA", whole); material.SetVector("_FrameB", whole);
            material.SetFloat("_PoseBlend", 1); material.SetFloat("_Visibility", 1); material.SetFloat("_Unfold", 1);
            EditorUtility.SetDirty(material); return material;
        }

        static Material TableAtlasMaterial(string name, string asset, Vector4 frame)
        {
            var path = TableArtFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Sankofa/Illustration")) { name = name }; AssetDatabase.CreateAsset(material, path); }
            material.shader = Shader.Find("Sankofa/Illustration");
            material.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sankofa/Art/IllustratedTheatre/" + asset);
            material.SetVector("_FrameA", frame); material.SetVector("_FrameB", frame);
            material.SetFloat("_PoseBlend", 1); material.SetFloat("_Visibility", 1); material.SetFloat("_Unfold", 1);
            EditorUtility.SetDirty(material); return material;
        }

        /// <summary>
        /// A paper theatre on a turntable. Scenery hinges stand from flat, back to front, using the painted scenery atlas;
        /// the two painted figures stand on their own hinges; a small gourd travels between them. In front, three
        /// heirlooms wait on slanted cards: touching one chooses the thread a telling follows.
        /// </summary>
        static PopUpStage BuildPopUpStage(Transform table, Material ink, Material brass, out Object[] heirlooms, out Object[] plaques)
        {
            Prim(table, "Stage drum", PrimitiveType.Cylinder, new Vector3(0.02f, 0.04f, 0.11f), new Vector3(0.6f, 0.04f, 0.6f), TableMaterial("Walnut deck", new Color(0.14f, 0.095f, 0.065f)));
            var root = Point(table, "Pop-up stage", new Vector3(0.02f, 0.08f, 0.11f));
            var stage = root.gameObject.AddComponent<PopUpStage>();
            Set(stage, "stagingData", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Sankofa/Content/personal-table-staging.json"));
            var turntable = Child(root, "Turntable");
            Prim(turntable, "Brass rim", PrimitiveType.Cylinder, new Vector3(0, 0.004f, 0), new Vector3(0.585f, 0.004f, 0.585f), brass);
            Prim(turntable, "Painted ground", PrimitiveType.Cylinder, new Vector3(0, 0.007f, 0), new Vector3(0.56f, 0.006f, 0.56f),
                TablePaintMaterial("Stage earth", AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Sankofa/Art/Scanned/dry_ground_01/dry_ground_01_diff_1k.jpg")));
            Prim(turntable, "Painted water", PrimitiveType.Quad, new Vector3(0, 0.0135f, 0.075f), new Vector3(0.50f, 0.11f, 1), TableAtlasMaterial("Stage water", "scenery-atlas.png", new Vector4(0.2f, 0.40f, 0.6f, 0.12f)), Quaternion.Euler(90, 0, 0));
            Set(stage, "turntable", turntable);

            var sky = TablePaintMaterial("Stage dusk", StageSky());
            var hinges = new[]
            {
                StageLayer(root, "Dusk backdrop", new Vector3(0, 0.013f, 0.235f), new Vector2(0.60f, 0.34f), sky, true),
                StageLayer(root, "Painted hills", new Vector3(0, 0.013f, 0.205f), new Vector2(0.58f, 0.215f), TableAtlasMaterial("Stage hills", "scenery-atlas.png", new Vector4(0, 0.625f, 1, 0.37f)), false),
                StageLayer(root, "Painted riverbank", new Vector3(0, 0.013f, 0.115f), new Vector2(0.54f, 0.14f), TableAtlasMaterial("Stage riverbank", "scenery-atlas.png", new Vector4(0, 0.36f, 1, 0.26f)), false),
                StageLayer(root, "Left reeds", new Vector3(-0.205f, 0.013f, -0.075f), new Vector2(0.15f, 0.138f), TableAtlasMaterial("Stage reeds left", "scenery-atlas.png", new Vector4(0, 0.02f, 0.36f, 0.33f)), false),
                StageLayer(root, "Right reeds", new Vector3(0.205f, 0.013f, -0.075f), new Vector2(0.15f, 0.138f), TableAtlasMaterial("Stage reeds right", "scenery-atlas.png", new Vector4(0.64f, 0.02f, 0.36f, 0.33f)), false),
            };
            SetArray(stage, "scenery", hinges);

            var cast = new[] { ("wambui", "Printed Wambui", new Vector2(0, 0.5f), -0.12f), ("naserian", "Printed Naserian", new Vector2(0.5f, 0.5f), 0.12f) };
            var stageData = new SerializedObject(stage);
            var figures = stageData.FindProperty("figures"); figures.arraySize = cast.Length;
            plaques = new Object[cast.Length];
            for (var i = 0; i < cast.Length; i++)
            {
                var (id, materialName, column, x) = cast[i];
                var figure = Point(root, "Paper figure " + id, new Vector3(x, 0.013f, 0));
                var hinge = Child(figure, "Fold");
                var face = Prim(hinge, "Painted figure", PrimitiveType.Quad, new Vector3(0, 0.1f, 0), new Vector3(0.2f, 0.2f, 1),
                    TableAtlasMaterial(materialName, "cast-atlas.png", new Vector4(column.x, 2f / 3, column.y, 1f / 3)));
                var item = figures.GetArrayElementAtIndex(i);
                item.FindPropertyRelative("Id").stringValue = id;
                item.FindPropertyRelative("Root").objectReferenceValue = figure;
                item.FindPropertyRelative("Hinge").objectReferenceValue = hinge;
                item.FindPropertyRelative("Face").objectReferenceValue = face.transform;
                item.FindPropertyRelative("Paint").objectReferenceValue = face.GetComponent<Renderer>();
                item.FindPropertyRelative("Column").vector2Value = column;
                // Her plaque hangs just above her head and walks with her; it never turns with her painted mirror image.
                var plaque = TablePanel(figure, "Follow " + id, new Vector3(0, 0.245f, -0.01f), 1, new Vector2(0.15f, 0.042f), ink);
                plaque.transform.localRotation = Quaternion.Euler(18, 0, 0);
                Set(plaque, "labelSize", 0.11f);
                plaques[i] = plaque;
            }
            stageData.ApplyModifiedPropertiesWithoutUndo();

            var gourd = Point(root, "Stage gourd", new Vector3(0, 0.013f, 0));
            TableProp(gourd, "Stage gourd body", GourdWithLip(0.042f, 0.016f, 0.005f), new Color(0.60f, 0.38f, 0.19f), 811);
            Set(stage, "gourd", gourd);
            var invite = TablePanel(root, "Pass the gourd", new Vector3(0, 0.1f, 0), 1, new Vector2(0.13f, 0.034f), ink);
            invite.transform.localRotation = Quaternion.Euler(18, 0, 0);
            Set(invite, "labelSize", 0.1f);
            Set(table.GetComponent<StoryTableView>(), "gourdInvite", invite);

            // The heirlooms' cards follow the interest panel's own layout: three columns, centred.
            var cards = TablePanel(root, "Interest choices", new Vector3(0, 0.03f, -0.03f), 3, new Vector2(0.125f, 0.055f), TableMaterial("Woven mats", new Color(0.36f, 0.25f, 0.15f)));
            cards.transform.localRotation = Quaternion.Euler(55, 0, 0);
            Set(cards, "gap", 0.03f); Set(cards, "labelSize", 0.13f);
            var spacing = 0.125f + 0.03f;
            var objects = new[]
            {
                ("Travelling bundle", new List<Vector2> { new Vector2(0, 0), new Vector2(0.026f, 0.004f), new Vector2(0.040f, 0.018f), new Vector2(0.041f, 0.036f), new Vector2(0.03f, 0.052f), new Vector2(0.012f, 0.06f), new Vector2(0.014f, 0.068f), new Vector2(0.008f, 0.074f), new Vector2(0, 0.074f) }, new Color(0.15f, 0.19f, 0.42f)),
                ("Flour gourd", GourdWithLip(0.10f, 0.038f, 0.012f), new Color(0.60f, 0.38f, 0.19f)),
                ("Water pot", new List<Vector2> { new Vector2(0, 0), new Vector2(0.024f, 0.002f), new Vector2(0.043f, 0.024f), new Vector2(0.045f, 0.044f), new Vector2(0.032f, 0.064f), new Vector2(0.021f, 0.071f), new Vector2(0.027f, 0.08f), new Vector2(0.023f, 0.083f), new Vector2(0, 0.083f) }, new Color(0.50f, 0.24f, 0.13f)),
            };
            heirlooms = new Object[objects.Length];
            for (var i = 0; i < objects.Length; i++)
            {
                var (name, profile, tone) = objects[i];
                var holder = Point(root, "Heirloom " + name, new Vector3((i - 1) * spacing, 0.013f, 0.045f));
                TableProp(holder, name, profile, tone, 820 + i);
                heirlooms[i] = holder;
            }
            Set(table.GetComponent<StoryTableView>(), "routes", cards);
            return stage;
        }

        /// <summary>A dusk sky for the stage's back cloth: indigo overhead, warm at the horizon, with a few stars.</summary>
        static Texture2D StageSky()
        {
            var path = TableArtFolder + "/Stage sky.asset";
            // Texels match the back cloth's 0.60 x 0.34 m proportions, so stars stay round.
            var sky = new Texture2D(352, 200, TextureFormat.RGBA32, false) { name = "Stage sky", wrapMode = TextureWrapMode.Clamp };
            var top = new Color(0.05f, 0.05f, 0.13f); var middle = new Color(0.25f, 0.15f, 0.26f); var horizon = new Color(0.74f, 0.40f, 0.22f);
            var random = new System.Random(1850);
            for (var y = 0; y < sky.height; y++)
            {
                var t = y / (sky.height - 1f);
                var colour = t < 0.35f ? Color.Lerp(horizon, middle, Mathf.SmoothStep(0, 1, t / 0.35f)) : Color.Lerp(middle, top, Mathf.SmoothStep(0, 1, (t - 0.35f) / 0.65f));
                for (var x = 0; x < sky.width; x++) sky.SetPixel(x, y, colour);
            }
            for (var i = 0; i < 90; i++)
            {
                var x = random.Next(sky.width); var y = random.Next(sky.height / 2, sky.height);
                var glint = 0.45f + (float)random.NextDouble() * 0.5f;
                sky.SetPixel(x, y, Color.Lerp(sky.GetPixel(x, y), new Color(1, 0.95f, 0.85f), glint));
            }
            sky.Apply();
            AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(sky, path);
            return sky;
        }

        /// <summary>A bottle gourd with a turned lip, as the gallery's detailed vessels have.</summary>
        static List<Vector2> GourdWithLip(float height, float bulb, float neck) => new List<Vector2>
        {
            new Vector2(0, 0), new Vector2(bulb * 0.35f, 0.008f * height / 0.1f), new Vector2(bulb * 0.82f, height * 0.12f),
            new Vector2(bulb, height * 0.3f), new Vector2(bulb * 0.96f, height * 0.48f), new Vector2(bulb * 0.65f, height * 0.65f),
            new Vector2(neck, height * 0.79f), new Vector2(neck, height * 0.95f), new Vector2(neck * 1.15f, height * 0.95f),
            new Vector2(neck * 1.15f, height), new Vector2(0, height),
        };

        /// <summary>A scenery hinge lying on the stage floor; its painted face stands above it when the hinge turns up.</summary>
        static Transform StageLayer(Transform stage, string name, Vector3 at, Vector2 size, Material material, bool opaque)
        {
            var hinge = Point(stage, name, at);
            Prim(hinge, name + " face", PrimitiveType.Quad, new Vector3(0, size.y / 2, 0), new Vector3(size.x, size.y, 1), material);
            if (opaque) Prim(hinge, name + " back", PrimitiveType.Quad, new Vector3(0, size.y / 2, 0.001f), new Vector3(size.x, size.y, 1), material, Quaternion.Euler(0, 180, 0));
            return hinge;
        }

        static void TableProp(Transform parent, string name, List<Vector2> profile, Color tone, int seed)
        {
            var mesh = SankofaStoryMeshes.Lathe("Table " + name, profile, 24, (a, y) => new Color(tone.r, tone.g, tone.b) * (0.9f + 0.1f * Mathf.Sin(a * 5 + y * 90)), seed);
            var path = TableArtFolder + "/" + mesh.name + ".asset";
            AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
            // Each prop carries its own pigment; vertex tint only adds the hand-turned variation.
            var materialPath = TableArtFolder + "/Table " + name + " pigment.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(Shader.Find("Sankofa/Painted Prop")) { name = "Table " + name + " pigment" }; AssetDatabase.CreateAsset(material, materialPath); }
            material.SetColor("_Color", tone); material.SetFloat("_VertexTint", 0); material.SetFloat("_InkWidth", 0.24f);
            material.SetTexture("_Noise", AssetDatabase.LoadAssetAtPath<Texture2D>(SankofaArt.NoisePath));
            EditorUtility.SetDirty(material);
            Static(parent, name, mesh, material);
        }

        /// <summary>The griot's fire burns in a clay bowl at the front left of the table, beside the telling and within reach.</summary>
        static HearthFire BuildStoryTableFire(Transform table)
        {
            var hearth = Point(table, "Griot fire", new Vector3(-0.45f, 0, -0.03f));
            var clay = TableMaterial("Ember bowl", new Color(0.29f, 0.16f, 0.08f));
            Prim(hearth, "Clay hearth", PrimitiveType.Cylinder, new Vector3(0, 0.012f, 0), new Vector3(0.16f, 0.012f, 0.16f), clay);
            for (var i = 0; i < 5; i++)
            {
                var a = i * Mathf.PI * 2 / 5 + 0.3f;
                Prim(hearth, "Hearth stone", PrimitiveType.Sphere, new Vector3(Mathf.Cos(a) * 0.072f, 0.026f, Mathf.Sin(a) * 0.072f), new Vector3(0.042f, 0.03f, 0.036f), TableMaterial("Hearth stones", new Color(0.24f, 0.22f, 0.20f)));
            }
            var root = Point(hearth, "Quiet tabletop fire", new Vector3(0, 0.028f, 0));
            root.localScale = Vector3.one * 0.34f;
            var fire = root.gameObject.AddComponent<HearthFire>();
            var flame = AssetDatabase.LoadAssetAtPath<Material>(SankofaArt.Folder + "/Flame.mat");
            var layers = new Object[3]; var flames = new Object[3];
            for (var i = 0; i < 3; i++)
            {
                var layer = Point(root, "Silent flame " + i, new Vector3((i - 1) * 0.045f, 0, i * 0.025f));
                layer.gameObject.AddComponent<Billboard>();
                layers[i] = layer;
                flames[i] = Prim(layer, "Flame", PrimitiveType.Quad, new Vector3(0, 0.5f, 0), Vector3.one, flame).GetComponent<Renderer>();
            }
            SetArray(fire, "layers", layers); SetArray(fire, "cards", flames);
            Set(fire, "lightPower", 0.7f); Set(fire, "lightAttenuation", 2f);
            return fire;
        }
    }
}
