using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Stage;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        public const string TheatreScene = "Assets/Sankofa/Scenes/IllustratedTheatre.unity";
        const string TheatreLayout = "Assets/Sankofa/Content/illustrated-theatre-layout.json";
        const string TheatreFolder = "Assets/Sankofa/Art/Generated/Theatre";

        [Serializable] sealed class TheatreLayoutData
        {
            public string scene, castAtlas, sceneryAtlas;
            public FigureLayout[] figures;
            public LayerLayout[] layers;
            public ResponseLayout[] responses;
        }
        [Serializable] sealed class FigureLayout
        {
            public string id;
            public int column;
            public float x, z, width, height, handX, handY, offerX, offerY;
            public string[] items;
        }
        [Serializable] sealed class LayerLayout
        {
            public string name;
            public float u, v, w, h, x, y, z, width, height;
        }
        [Serializable] sealed class ResponseLayout
        {
            public string objectId;
            public string[] figures;
            public int acceptedPose, passivePose, layer;
        }

        [MenuItem("Sankofa/Build Illustrated Theatre MVP")]
        public static void BuildIllustratedTheatre()
        {
            if (!File.Exists(HearthScene)) BuildScenes();
            else EditorSceneManager.OpenScene(HearthScene, OpenSceneMode.Single);
            var layout = JsonUtility.FromJson<TheatreLayoutData>(File.ReadAllText(TheatreLayout));
            var stage = Object.FindFirstObjectByType<StoryStage>();
            var root = stage.Scenes.Single(s => s.Id == layout.scene).Root.transform;
            if (!AssetDatabase.IsValidFolder(TheatreFolder)) AssetDatabase.CreateFolder(SankofaArt.Folder, "Theatre");
            var cast = ImportAtlas(layout.castAtlas);
            var scenery = ImportAtlas(layout.sceneryAtlas);
            var castMaterial = IllustrationMaterial("Cast", cast, new Vector4(0, 2f / 3, 0.5f, 1f / 3));
            var figures = new Dictionary<string, IllustratedFigure>();
            foreach (var f in layout.figures)
            {
                var item = stage.Find(f.id);
                // Keep the stable story object and its carrying anchors, replace only its presentation.
                foreach (var child in item.transform.Cast<Transform>().ToArray())
                    if (child.name != "Hands" && child.name != "Neck") Object.DestroyImmediate(child.gameObject);
                // Remove dependants first: Unity refuses to remove their required FigureMotion otherwise.
                foreach (var component in item.GetComponents<MonoBehaviour>())
                    if (component is FigurePresence || component is FigureExchange || component is FigureGrounding)
                        Object.DestroyImmediate(component);
                Object.DestroyImmediate(item.GetComponent<FigureMotion>());
                item.transform.localPosition = new Vector3(f.x, BoundaryGround(new Vector3(f.x, 0, f.z)), f.z);
                item.transform.localRotation = Quaternion.identity;
                var body = IllustrationCard(item.transform, f.id + " painted", f.width, f.height, castMaterial);
                body.transform.localPosition = new Vector3(-0.18f, -0.07f, 0);
                var shade = ContactShade(item.transform, f.id, 0.5f, 0.26f);
                SetArray(item, "surfaces", new Object[] { body, shade });
                Set(item, "echoMesh", body.GetComponent<MeshFilter>().sharedMesh);
                Set(item, "scaleWithVisibility", false);
                Set(item, "holder", null);
                var actor = item.gameObject.AddComponent<IllustratedFigure>();
                Set(actor, "surface", body);
                Set(actor, "transition", stage.GetComponent<StoryTransition>());
                var hand = item.transform.Find("Hands");
                Set(actor, "giftOrigin", hand);
                Set(actor, "waitingHand", new Vector3(f.handX, f.handY, -0.03f));
                Set(actor, "offeringHand", new Vector3(f.offerX, f.offerY, -0.03f));
                hand.localPosition = new Vector3(f.handX, f.handY, -0.03f);
                var so = new SerializedObject(actor);
                var frames = so.FindProperty("frames"); frames.arraySize = 3;
                for (var row = 0; row < 3; row++) frames.GetArrayElementAtIndex(row).vector4Value = new Vector4(f.column * 0.5f, (2 - row) / 3f, 0.5f, 1f / 3);
                so.ApplyModifiedPropertiesWithoutUndo();
                foreach (var id in f.items)
                {
                    var prop = stage.Find(id);
                    prop.transform.SetParent(hand, false);
                    prop.transform.localPosition = new Vector3(0, -0.12f, -0.04f);
                    Set(prop, "holder", null);
                    Set(prop, "illustratedHolder", actor);
                    Set(prop, "handOver", null);
                }
                figures.Add(f.id, actor);
            }

            // Let painted scenery supply the first scene's composition; keep tangible ground, water, fire and props.
            foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                var name = renderer.name.ToLowerInvariant();
                if (name.Contains("canop") || name.Contains("trunk") || name.Contains("shrubs") ||
                    name.Contains("sedges") || name.Contains("leaf litter") || name.Contains("grass") ||
                    name.Contains("fallen limbs") || name.Contains("edge outcrops") ||
                    // One river: the painted water is the stream, so the modelled ribbon and its pale stones leave.
                    name == "stream" || name == "bank stones")
                    renderer.enabled = false;
            }
            // Keep the sheep tangible and readable when the story names it. The existing geometry gets the same
            // opaque pigment treatment as the gifts, instead of translucent firelight through the river stones.
            var ewe = stage.Find("figure.ewe");
            var features = SheepFace(ewe.transform.Find("Neck"));
            var sheepSurfaces = ewe.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var surface in sheepSurfaces)
            {
                var fleece = surface.name == "Body";
                var material = PaintedPropMaterial(fleece ? "sheep fleece" : "sheep face and legs", null, 0.25f);
                material.SetColor("_Color", fleece ? new Color(0.78f, 0.69f, 0.5f) : new Color(0.32f, 0.23f, 0.17f));
                material.SetFloat("_VertexTint", 0); // The legacy meshes encode shader masks in black vertex colours.
                surface.sharedMaterials = new[] { material };
                surface.enabled = true;
            }
            var facePigment = PaintedPropMaterial("sheep facial details", null, 0.12f);
            facePigment.SetColor("_Color", Color.white);
            features.sharedMaterial = facePigment;
            SetArray(ewe, "surfaces", sheepSurfaces.Cast<Object>().ToArray());
            Set(ewe, "glow", null);
            Set(ewe, "attentionRadius", 0.24f);
            Set(ewe.GetComponent<FigureMotion>(), "useMaterialPigment", true);
            var echo = new Mesh { name = "Painted sheep memory" };
            echo.CombineMeshes(sheepSurfaces.Select(r => new CombineInstance
            {
                mesh = r.GetComponent<MeshFilter>().sharedMesh,
                transform = ewe.transform.worldToLocalMatrix * r.transform.localToWorldMatrix
            }).ToArray());
            var echoPath = TheatreFolder + "/Painted sheep memory.asset";
            AssetDatabase.DeleteAsset(echoPath); AssetDatabase.CreateAsset(echo, echoPath);
            Set(ewe, "echoMesh", echo);
            // The ground the women stand on meets the painting's values: violet-brown earth, not a dark stage floor.
            var ground = root.Find("Ground").GetComponent<Renderer>();
            var groundPath = TheatreFolder + "/Painted ground.mat";
            AssetDatabase.DeleteAsset(groundPath);
            var groundMaterial = new Material(ground.sharedMaterial) { name = "Painted ground" };
            groundMaterial.SetColor("_Tint", new Color(1.9f, 1.6f, 1.8f, 1f));
            AssetDatabase.CreateAsset(groundMaterial, groundPath);
            ground.sharedMaterial = groundMaterial;
            // Things handled in the story are drawn in the paintings' language: banded firelight and an ink edge.
            foreach (var (id, ink) in new[] { ("gourd.flour", 0.44f), ("gourd.milk", 0.44f), ("hide", 0.3f), ("bead", 0.24f) })
                foreach (var surface in stage.Find(id).Surfaces)
                    surface.sharedMaterial = PaintedPropMaterial(id, surface.sharedMaterial, ink);
            foreach (var id in new[] { "gourd.flour", "gourd.milk" })
            {
                var pigment = stage.Find(id).Surfaces.First().sharedMaterial;
                pigment.SetColor("_Color", new Color(0.62f, 0.38f, 0.22f));
                pigment.SetFloat("_InkWidth", 0.3f);
            }
            var layers = new List<Renderer>();
            foreach (var l in layout.layers)
            {
                var mat = IllustrationMaterial(l.name, scenery, new Vector4(l.u, l.v, l.w, l.h));
                var layer = IllustrationCard(root, l.name, l.width, l.height, mat);
                layer.transform.localPosition = new Vector3(l.x, l.y, l.z);
                layers.Add(layer);
            }
            var theatre = root.gameObject.AddComponent<IllustratedTheatre>();
            SetArray(theatre, "layers", layers.Cast<Object>().ToArray());
            Set(theatre, "transition", stage.GetComponent<StoryTransition>());
            var data = new SerializedObject(theatre);
            var responses = data.FindProperty("responses"); responses.arraySize = layout.responses.Length;
            for (var i = 0; i < layout.responses.Length; i++)
            {
                var value = layout.responses[i]; var response = responses.GetArrayElementAtIndex(i);
                response.FindPropertyRelative("Object").stringValue = value.objectId;
                response.FindPropertyRelative("AcceptedPose").intValue = value.acceptedPose;
                response.FindPropertyRelative("PassivePose").intValue = value.passivePose;
                response.FindPropertyRelative("Layer").intValue = value.layer;
                var castRefs = response.FindPropertyRelative("Figures"); castRefs.arraySize = value.figures.Length;
                for (var j = 0; j < value.figures.Length; j++) castRefs.GetArrayElementAtIndex(j).objectReferenceValue = figures[value.figures[j]];
            }
            data.ApplyModifiedPropertiesWithoutUndo();
            var app = Object.FindFirstObjectByType<SankofaApp>();
            Set(app, "packageJson", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Sankofa/Content/illustrated-theatre-mvp.json"));
            Set(app, "storeNamespace", "illustrated-theatre-mvp");
            Set(stage, "offerDrop", 0.2f);
            root.parent.Find("Carry point").localPosition = new Vector3(0.14f, 1.02f, 0.58f);
            Object.FindFirstObjectByType<Sankofa.Runtime.UI.CaptionBoard>().transform.localPosition += Vector3.up * 0.12f;
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), TheatreScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(TheatreScene, true), new EditorBuildSettingsScene(HearthScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Sankofa] Illustrated theatre MVP built: " + TheatreScene);
        }

        static Texture2D ImportAtlas(string path)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = 2048;
            importer.SetPlatformTextureSettings(new TextureImporterPlatformSettings
            { name = "Android", overridden = true, maxTextureSize = 2048, format = TextureImporterFormat.ASTC_6x6, compressionQuality = 75 });
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material IllustrationMaterial(string name, Texture2D atlas, Vector4 frame)
        {
            var path = TheatreFolder + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Sankofa/Illustration")); AssetDatabase.CreateAsset(material, path); }
            material.SetTexture("_MainTex", atlas);
            material.SetVector("_FrameA", frame); material.SetVector("_FrameB", frame);
            material.SetFloat("_PoseBlend", 1);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Material PaintedPropMaterial(string id, Material source, float ink)
        {
            var path = TheatreFolder + "/Painted " + id + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Sankofa/Painted Prop")); AssetDatabase.CreateAsset(material, path); }
            material.SetColor("_Color", source != null && source.HasProperty("_Color") ? source.color : Color.white);
            material.SetTexture("_Noise", AssetDatabase.LoadAssetAtPath<Texture2D>(SankofaArt.NoisePath));
            material.SetFloat("_InkWidth", ink);
            material.SetFloat("_VertexTint", 1);
            EditorUtility.SetDirty(material);
            return material;
        }

        static Renderer ContactShade(Transform person, string id, float width, float depth)
        {
            var path = TheatreFolder + "/Contact shade.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find("Sankofa/Contact Shade"));
                AssetDatabase.CreateAsset(material, path);
            }
            var shade = IllustrationCard(person, id + " contact", width, depth, material);
            shade.transform.localRotation = Quaternion.Euler(90, 0, 0);
            shade.transform.localPosition = new Vector3(-0.18f, 0.018f, -depth / 2);
            shade.shadowCastingMode = ShadowCastingMode.Off;
            return shade;
        }

        static Renderer SheepFace(Transform neck)
        {
            var pieces = new List<CombineInstance>();
            void Mark(Vector3 at, Vector3 scale, Color pigment)
            {
                var piece = SankofaMeshes.Stone(2900 + pieces.Count, 2, 0.015f, 10f);
                piece.colors = Enumerable.Repeat(pigment, piece.vertexCount).ToArray();
                pieces.Add(new CombineInstance { mesh = piece, transform = Matrix4x4.TRS(at, Quaternion.identity, scale) });
            }
            var ink = new Color(0.075f, 0.045f, 0.035f);
            foreach (var side in new[] { -1f, 1f })
            {
                Mark(new Vector3(side * 0.057f, 0.095f, 0.16f), new Vector3(0.012f, 0.016f, 0.016f), ink);
                Mark(new Vector3(side * 0.066f, 0.1f, 0.156f), new Vector3(0.004f, 0.005f, 0.005f), new Color(0.86f, 0.78f, 0.61f));
            }
            Mark(new Vector3(0, 0.06f, 0.272f), new Vector3(0.041f, 0.02f, 0.016f), ink);
            var mesh = new Mesh { name = "Painted sheep face" };
            mesh.CombineMeshes(pieces.ToArray());
            foreach (var piece in pieces) Object.DestroyImmediate(piece.mesh);
            var path = TheatreFolder + "/Painted sheep face.asset";
            AssetDatabase.DeleteAsset(path); AssetDatabase.CreateAsset(mesh, path);
            var face = Static(neck, "Painted face marks", mesh, null).GetComponent<MeshRenderer>();
            return face;
        }

        static Renderer IllustrationCard(Transform parent, string name, float width, float height, Material material)
        {
            var mesh = new Mesh { name = name };
            mesh.vertices = new[] { new Vector3(-width / 2, 0, 0), new Vector3(width / 2, 0, 0), new Vector3(-width / 2, height, 0), new Vector3(width / 2, height, 0) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.up, Vector2.one };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 }; mesh.RecalculateNormals(); mesh.RecalculateBounds();
            var path = TheatreFolder + "/" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<Mesh>(path) != null) AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            return Static(parent, name, mesh, material).GetComponent<Renderer>();
        }
    }
}
