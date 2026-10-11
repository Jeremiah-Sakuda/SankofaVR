using System.Collections.Generic;
using System.Linq;
using Sankofa.Runtime.App;
using Sankofa.Runtime.Audio;
using Sankofa.Runtime.Fire;
using Sankofa.Runtime.Input;
using Sankofa.Runtime.Narration;
using Sankofa.Runtime.Stage;
using Sankofa.Runtime.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Generates the hearth scene from code so it is reproducible and reviewable:
    /// Unity -batchmode -quit -projectPath unity -buildTarget Android -executeMethod Sankofa.EditorTools.SankofaSceneBuilder.BuildScenes
    /// Change this builder and regenerate rather than editing the scene by hand. The story scenes serve the
    /// placeholder package; they are not art direction for a selected setting.
    /// </summary>
    public static partial class SankofaSceneBuilder
    {
        public const string HearthScene = "Assets/Sankofa/Scenes/Hearth.unity";
        public const string PackagePath = "Assets/Sankofa/Content/enkare-nairobi.json";
        const string CoreSdk = "Packages/com.meta.xr.sdk.core/Prefabs/";

        /// <summary>Eye position the layout is designed around; the world anchor follows the visitor's real head.</summary>
        public static readonly Vector3 SeatedEye = new Vector3(0, 1.15f, 0);
        public static readonly Vector3 FirePosition = new Vector3(0, 0.04f, 1.45f);

        struct Rig
        {
            public Transform Head;
            public Camera Eye;
            public HandSource[] Hands;
            public Transform[] Controllers;
        }

        [MenuItem("Sankofa/Build Scenes")]
        public static void BuildScenes()
        {
            // Also regenerate correctly when the menu is used twice in the same editor session.
            figureController = null;
            slenderBody = null;
            clothedBodyFinished = false;
            if (!AssetDatabase.IsValidFolder("Assets/Sankofa/Scenes")) AssetDatabase.CreateFolder("Assets/Sankofa", "Scenes");
            var art = SankofaArt.Ensure();
            SankofaIcon.Ensure();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var rig = CreateRig();
            var anchor = new GameObject("World anchor").AddComponent<WorldAnchor>();
            Set(anchor, "head", rig.Head);
            var world = anchor.transform;

            var hearth = Child(world, "Scene hearth");
            var (restTop, keepsakeTop) = BuildHearth(hearth, art);
            var early = Child(world, "Scene story.water.1850s");
            var (earlyObjects, earlySound) = BuildWater1850s(early, art);
            var railway = Child(world, "Scene story.water.1899");
            var (railwayObjects, railwaySound) = BuildWater1899(railway, art);
            var today = Child(world, "Scene story.water.today");
            var (todayObjects, todaySound) = BuildWaterToday(today, art);

            var fire = BuildFire(world, art);
            var narration = BuildNarrator(world);
            var ambience = BuildAmbience(hearth);
            var ember = BuildEmber(hearth, art);

            // The placed ember and the keepsake rest on the standing stones' actual tops.
            var restAt = restTop + Vector3.up * 0.025f;
            var keepsakeAt = keepsakeTop + Vector3.up * 0.015f;
            var rest = Point(world, "Rest point", restAt);
            var keepsake = Point(world, "Keepsake point", keepsakeAt);
            // While the visitor decides, the keepsake hangs just above the choice to keep the visit, clear of the fire.
            var keepsakeShown = Point(world, "Keepsake shown point", new Vector3(-0.16f, 1.11f, 0.45f));
            // Low and to the right of the view, so what the visitor carries without a tracked hand never hides the story.
            var carry = Point(world, "Carry point", new Vector3(0.22f, 0.74f, 0.34f));
            // What a kept visit leaves at the hearth waits on the keepsake stone.
            var beadKept = BuildBead(hearth, art, "bead.kept", "hearth", keepsakeAt + Vector3.up * 0.012f);
            var seedKept = BuildSeed(hearth, art, "seed.kept", "hearth", keepsakeAt + Vector3.up * 0.01f);

            var captions = BuildCaptions(world, art, rig.Head);
            // Pause and return wait at the lap, out of the story's way. Choices come up into view, below and to the
            // left of the fire; questions to the right, inside a comfortable view cone. All within a seated reach.
            var controls = BuildPanel(world, art, "Controls", new Vector3(-0.08f, 0.66f, 0.27f), 2);
            var choices = BuildPanel(world, art, "Choices", new Vector3(-0.28f, 0.95f, 0.42f), 2);
            var questions = BuildPanel(world, art, "Questions", new Vector3(0.33f, 0.98f, 0.42f), 1);
            var settings = BuildPanel(world, art, "Settings", new Vector3(-0.5f, 0.7f, 0.2f), 2);
            var startScreen = BuildStartScreen(world);
            BuildPointers(rig, art, new[] { controls, choices, questions, settings });
            var curtain = BuildCurtain(rig.Head, art);

            var receiver = new GameObject("Palm receiver").AddComponent<PalmReceiver>();
            SetArray(receiver, "hands", rig.Hands.Cast<Object>().ToArray());
            var ring = Prim(receiver.transform, "Palm guide", PrimitiveType.Quad, Vector3.zero, Vector3.one * 0.13f, art.PalmRing);
            ring.GetComponent<Renderer>().enabled = false;
            Set(receiver, "guide", ring.GetComponent<Renderer>());
            var toucher = new GameObject("Touch receiver").AddComponent<TouchReceiver>();
            SetArray(toucher, "hands", rig.Hands.Cast<Object>().ToArray());

            var stageObject = new GameObject("Story stage");
            var stage = stageObject.AddComponent<StoryStage>();
            ConfigureScenes(stage, hearth, early, railway, today);
            SetArray(stage, "objects", new Object[] { ember, beadKept, seedKept }.Concat(earlyObjects).Concat(railwayObjects).Concat(todayObjects).ToArray());
            Set(stage, "fire", fire);
            Set(stage, "eye", rig.Eye);
            Set(stage, "head", rig.Head);
            Set(stage, "restPoint", rest);
            Set(stage, "keepsakePoint", keepsake);
            Set(stage, "keepsakeShownPoint", keepsakeShown);
            Set(stage, "carryPoint", carry);
            var transition = stageObject.AddComponent<StoryTransition>();
            Set(transition, "curtain", curtain);
            Set(transition, "stage", stage);
            var attention = stageObject.AddComponent<AttentionSource>();
            Set(attention, "head", rig.Head);
            Set(attention, "stage", stage);

            var app = new GameObject("Sankofa").AddComponent<SankofaApp>();
            Set(app, "packageJson", AssetDatabase.LoadAssetAtPath<TextAsset>(PackagePath));
            Set(app, "credits", AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Sankofa/Content/credits.txt"));
            Set(app, "anchor", anchor);
            Set(app, "fire", fire);
            Set(app, "narration", narration);
            Set(app, "ambience", ambience);
            SetArray(app, "storyAmbience", new Object[] { earlySound, railwaySound, todaySound });
            Set(app, "captions", captions);
            Set(app, "startScreen", startScreen);
            Set(app, "controls", controls);
            Set(app, "choicesPanel", choices);
            Set(app, "questionsPanel", questions);
            Set(app, "settingsPanel", settings);
            Set(app, "stage", stage);
            Set(app, "transition", transition);
            Set(app, "receiver", receiver);
            Set(app, "toucher", toucher);
            Set(app, "attention", attention);
            SetArray(app, "hands", rig.Hands.Cast<Object>().ToArray());

            BuildEchoes();
            Set(stage, "echoMaterial", art.EmberFigure);
            Set(stage, "echoDepthMaterial", art.EmberFigureDepth);
            Set(stage, "seat", anchor.transform);

            early.gameObject.SetActive(false);
            railway.gameObject.SetActive(false);
            today.gameObject.SetActive(false);
            EditorSceneManager.SaveScene(scene, HearthScene);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(HearthScene, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Sankofa] Scene built: " + HearthScene);
        }

        // ---- rig: camera and tracked hands ----

        static Rig CreateRig()
        {
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoreSdk + "OVRCameraRig.prefab");
            var handPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoreSdk + "OVRHandPrefab.prefab");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rig.name = "OVRCameraRig";

            var managerSo = new SerializedObject(rig.GetComponent<OVRManager>());
            SetValue(managerSo, "_trackingOriginType", (int)OVRManager.TrackingOrigin.FloorLevel);
            // Meta Project Setup Tool recommendation, kept in the generated scene so regeneration preserves it.
            SetValue(managerSo, "_enableDynamicResolution", true);
            managerSo.ApplyModifiedPropertiesWithoutUndo();

            var head = Find(rig.transform, "CenterEyeAnchor");
            head.gameObject.tag = "MainCamera";
            var eye = head.GetComponent<Camera>();
            eye.clearFlags = CameraClearFlags.SolidColor;
            eye.backgroundColor = new Color(0.012f, 0.016f, 0.035f);
            // Depth is spread from 5 cm to 300 m: near enough for a hand at the face, far enough for the city's
            // skyline beyond the river. The skies draw at the far plane whatever their size.
            eye.nearClipPlane = 0.05f;
            eye.farClipPlane = 300f;

            var hands = new HandSource[2];
            var sources = new GameObject("Hand sources");
            for (var i = 0; i < 2; i++)
            {
                var left = i == 0;
                var anchor = Find(rig.transform, left ? "LeftHandAnchor" : "RightHandAnchor");
                var hand = (GameObject)PrefabUtility.InstantiatePrefab(handPrefab, anchor);
                hand.name = left ? "Left hand" : "Right hand";
                var ovrHand = hand.GetComponent<OVRHand>();
                var skeleton = hand.GetComponent<OVRSkeleton>();
                Set(ovrHand, "HandType", left ? 0 : 1);
                // OpenXR hand skeleton and mesh (XRHandLeft = 4, XRHandRight = 5).
                Set(skeleton, "_skeletonType", left ? 4 : 5);
                var mesh = hand.GetComponent<OVRMesh>();
                if (mesh != null) Set(mesh, "_meshType", left ? 4 : 5);

                var source = new GameObject(left ? "Left hand source" : "Right hand source").AddComponent<HandSource>();
                source.transform.SetParent(sources.transform, false);
                Set(source, "left", left);
                Set(source, "hand", ovrHand);
                Set(source, "skeleton", skeleton);
                hands[i] = source;
            }
            var controllers = new[] { Find(rig.transform, "LeftControllerAnchor"), Find(rig.transform, "RightControllerAnchor") };
            return new Rig { Head = head, Eye = eye, Hands = hands, Controllers = controllers };
        }

        // ---- the hearth at night ----

        const int GroundSeed = 21;
        /// <summary>Toward the moon from the hearth: ahead and to the left, about 33 degrees up.</summary>
        static readonly Vector3 HearthMoon = new Vector3(-0.45f, 0.55f, 0.7f);
        const string MeshFolder = SankofaArt.Folder + "/Meshes";

        /// <summary>
        /// The hearth: generated ground with an ash pit, weathered stones, a laid and leaning log fire on glowing coals,
        /// seats waiting for company, a treeline, grass, and the night sky. Static pieces are combined per material
        /// (a handful of draw calls on Quest) and their occlusion and soft fire shadows are baked into vertices.
        /// </summary>
        static (Vector3 RestTop, Vector3 KeepsakeTop) BuildHearth(Transform root, SankofaArtSet art)
        {
            var random = new System.Random(11);
            float R(float a, float b) => Range(random, a, b);
            float Ground(Vector3 at) => SankofaMeshes.GroundHeight(at, FirePosition, GroundSeed);

            var terrain = SankofaMeshes.Terrain(FirePosition, GroundSeed);
            var cover = terrain.uv;
            var vertices = terrain.vertices;
            for (var i = 0; i < cover.Length; i++)
            {
                var distance = new Vector2(vertices[i].x - FirePosition.x, vertices[i].z - FirePosition.z).magnitude;
                cover[i].y = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(2.3f, 4.5f, distance)) * (0.72f + SankofaMeshes.Fbm(vertices[i] * 0.4f, 109, 2) * 0.2f);
            }
            terrain.uv = cover;
            var ground = Static(root, "Ground", terrain, art.Ground);

            var stones = new List<CombineInstance>();
            for (var i = 0; i < 12; i++)
            {
                var angle = i / 12f * Mathf.PI * 2f + R(-0.1f, 0.1f);
                var at = FirePosition + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * R(0.44f, 0.5f);
                var size = new Vector3(R(0.1f, 0.14f), R(0.07f, 0.1f), R(0.085f, 0.12f));
                at.y = Ground(at) + size.y * 0.3f;
                var ring = Part(SankofaMeshes.Stone(100 + i), at, Quaternion.Euler(R(-8, 8), -angle * Mathf.Rad2Deg + R(-20, 20), R(-8, 8)), size);
                var shade = new Color(R(0.85f, 1.1f), R(0.85f, 1.05f), R(0.82f, 1.05f));
                stones.Add(Tinted(ring, (p, n) =>
                {
                    var toFire = FirePosition + Vector3.up * 0.2f - p;
                    var soot = Mathf.Clamp01(1f - toFire.magnitude / 0.6f) * Mathf.Clamp01(Vector3.Dot(n, toFire.normalized) + 0.2f);
                    return shade * (1f - 0.8f * soot);
                }));
            }
            // Standing stones bring the placed ember and the keepsake up into view, about 35 degrees below eye level.
            var tops = new List<Vector3>();
            foreach (var (x, seed) in new[] { (0.34f, 201), (-0.34f, 202) })
            {
                var at = new Vector3(x, 0, 0.46f);
                at.y = Ground(at) + 0.29f;
                var part = Part(SankofaMeshes.Stone(seed, 3, 0.26f, 0.75f), at, Quaternion.Euler(R(-4, 4), R(0, 360), R(-4, 4)), new Vector3(0.12f, 0.38f, 0.07f));
                var shade = new Color(R(0.8f, 0.95f), R(0.8f, 0.92f), R(0.82f, 0.95f));
                stones.Add(Tinted(part, (p, n) => shade));
                // The surface height at the stone's centre, where things are set down, not its highest edge.
                var placed = part.mesh.vertices.Select(v => part.transform.MultiplyPoint3x4(v)).ToList();
                var centre = placed.Where(v => new Vector2(v.x - at.x, v.z - at.z).magnitude < 0.035f).ToList();
                var top = (centre.Count > 0 ? centre : placed).Max(v => v.y);
                tops.Add(new Vector3(at.x, top, at.z));
            }
            for (var i = 0; i < 120; i++)
            {
                var angle = R(0, Mathf.PI * 2f);
                var at = FirePosition + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * R(0.6f, 3f);
                var size = R(0.008f, 0.03f);
                at.y = Ground(at) + size * 0.2f;
                stones.Add(Part(SankofaMeshes.Stone(800 + i, 1, 0.3f, 0.3f), at, Quaternion.Euler(R(0, 360), R(0, 360), R(0, 360)), new Vector3(size * 1.3f, size * 0.7f, size)));
            }
            var stoneFilter = Static(root, "Stones", Combine(stones, "Hearth stones"), art.Rock);

            // Three logs laid across the coals and three leaning in over them; their inner ends glow.
            var logs = new List<CombineInstance>();
            for (var i = 0; i < 3; i++)
            {
                var m = Matrix4x4.TRS(FirePosition + new Vector3(0, 0.05f + i * 0.036f, 0), Quaternion.Euler(0, i * 60f + 15f, 0) * Quaternion.Euler(90, 0, 0), Vector3.one);
                logs.Add(new CombineInstance { mesh = Glowing(SankofaMeshes.Log(300 + i, R(0.5f, 0.58f), R(0.042f, 0.055f)), m, 1.3f), transform = m });
            }
            for (var i = 0; i < 3; i++)
            {
                var angle = (50f + i * 40f) * Mathf.Deg2Rad;
                var outward = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                var bottom = FirePosition + outward * 0.25f + Vector3.up * 0.02f;
                var top = FirePosition + outward * 0.035f + Vector3.up * 0.33f;
                var along = top - bottom;
                var m = Matrix4x4.TRS((bottom + top) * 0.5f, Quaternion.FromToRotation(Vector3.up, along.normalized), Vector3.one);
                logs.Add(new CombineInstance { mesh = Glowing(SankofaMeshes.Log(310 + i, along.magnitude + 0.06f, R(0.03f, 0.042f), 0.25f), m, 1.1f), transform = m });
            }
            var logFilter = Static(root, "Burning logs", Combine(logs, "Burning logs"), art.BurningWood);

            var coals = new List<CombineInstance>();
            for (var i = 0; i < 26; i++)
            {
                var angle = R(0, Mathf.PI * 2f);
                var radius = Mathf.Sqrt(R(0, 1)) * 0.22f;
                var at = FirePosition + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                var size = R(0.018f, 0.04f);
                at.y = Ground(at) + size * 0.3f;
                var m = Matrix4x4.TRS(at, Quaternion.Euler(R(0, 360), R(0, 360), R(0, 360)), new Vector3(size * 1.3f, size * 0.7f, size));
                coals.Add(new CombineInstance { mesh = Glowing(SankofaMeshes.Stone(400 + i, 1, 0.3f, 0.3f), m, 1.6f), transform = m });
            }
            var coalFilter = Static(root, "Coals", Combine(coals, "Coals"), art.Embers);

            // Seats around the fire, waiting for company.
            var seats = new List<CombineInstance>();
            foreach (var (offset, yaw, seed) in new[] { (new Vector3(-1.25f, 0, 0.45f), 62f, 501), (new Vector3(1.28f, 0, 0.4f), -58f, 502) })
            {
                var at = FirePosition + offset;
                at.y = Ground(at) + 0.11f;
                seats.Add(Part(SankofaMeshes.Log(seed, 1.15f, 0.13f, 0.06f, 0.02f, 18, 12), at, Quaternion.Euler(90, yaw, 0), Vector3.one));
            }
            for (var i = 0; i < 28; i++)
            {
                var angle = R(0, Mathf.PI * 2f);
                var at = FirePosition + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * R(0.65f, 2.6f);
                var radius = R(0.006f, 0.012f);
                at.y = Ground(at) + radius;
                seats.Add(Part(SankofaMeshes.Log(900 + i, R(0.12f, 0.32f), radius, 0.3f, 0.01f, 6, 4), at, Quaternion.Euler(90, R(0, 360), 0), Vector3.one));
            }
            var seatFilter = Static(root, "Seats", Combine(seats, "Seats and twigs"), art.SeatWood);

            // A treeline frames the clearing, left open ahead so the horizon and stars show above the fire.
            var trunks = new List<CombineInstance>();
            var canopies = new List<CombineInstance>();
            for (var i = 0; i < 18; i++)
            {
                var degrees = i * 20f + R(-7, 7);
                if (Mathf.Abs(Mathf.DeltaAngle(degrees, 90f)) < 18f) continue;
                var at = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad)) * R(9f, 15f);
                at.y = Ground(at) - 0.08f;
                SankofaMeshes.Tree(600 + i, R(3.6f, 6.2f), out var trunk, out var canopy);
                var m = Matrix4x4.TRS(at, Quaternion.Euler(0, R(0, 360), 0), Vector3.one);
                trunks.Add(new CombineInstance { mesh = trunk, transform = m });
                canopies.Add(new CombineInstance { mesh = canopy, transform = m });
            }
            var trunkFilter = Static(root, "Tree trunks", Combine(trunks, "Tree trunks"), art.TreeBark);
            var canopyFilter = Static(root, "Tree canopies", Combine(canopies, "Tree canopies"), art.TreeCanopy);
            var grass = Static(root, "Grass", SankofaMeshes.Grass(FirePosition, Vector3.zero, 31, 650, 2.3f, 10f, Ground), art.Grass);

            Prim(root, "Night sky", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 140f, art.Sky);
            var stars = Static(root, "Stars", SankofaMeshes.Stars(1800, 7, 62f), art.Stars);

            var details = BuildUnderstory(root, art, 710, Ground,
                p => new Vector2(p.x - FirePosition.x, p.z - FirePosition.z).magnitude > 2.8f &&
                    !(Mathf.Abs(p.x) < 1.6f && p.z > 0f), 112, 11f, false);
            SankofaBake.Bake(
                new[] { ground, stoneFilter, logFilter, coalFilter, seatFilter, trunkFilter, canopyFilter }.Concat(details).ToArray(),
                new[] { ground, stoneFilter, logFilter, seatFilter, trunkFilter, canopyFilter }.Concat(details).ToArray(),
                root.TransformPoint(FirePosition), root.TransformDirection(HearthMoon),
                new HashSet<MeshFilter>(new[] { ground, stoneFilter, seatFilter, trunkFilter, canopyFilter }.Concat(details)));
            SaveMeshes(details.ToArray());
            SaveMeshes(new[] { ground, stoneFilter, logFilter, coalFilter, seatFilter, trunkFilter, canopyFilter, grass, stars });
            // The keepsake waits on the right stone, clear of the controls on the left.
            return (tops[1], tops[0]);
        }

        static CombineInstance Part(Mesh mesh, Vector3 position, Quaternion rotation, Vector3 scale) =>
            new CombineInstance { mesh = mesh, transform = Matrix4x4.TRS(position, rotation, scale) };

        /// <summary>Multiplies a part's vertex tint by a function of each vertex's placed position and normal.</summary>
        static CombineInstance Tinted(CombineInstance part, System.Func<Vector3, Vector3, Color> tint)
        {
            var mesh = part.mesh;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var colors = mesh.colors;
            for (var i = 0; i < vertices.Length; i++)
            {
                var c = tint(part.transform.MultiplyPoint3x4(vertices[i]), part.transform.MultiplyVector(normals[i]).normalized);
                colors[i] = new Color(colors[i].r * c.r, colors[i].g * c.g, colors[i].b * c.b, colors[i].a);
            }
            mesh.colors = colors;
            return part;
        }

        static Mesh Combine(List<CombineInstance> parts, string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Sets where burning wood may glow (UV2.y): strongest at the centre of the fire, low down.</summary>
        static Mesh Glowing(Mesh mesh, Matrix4x4 placement, float strength)
        {
            var vertices = mesh.vertices;
            var bake = new List<Vector2>();
            mesh.GetUVs(1, bake);
            for (var i = 0; i < vertices.Length; i++)
            {
                var at = placement.MultiplyPoint3x4(vertices[i]);
                var across = new Vector2(at.x - FirePosition.x, at.z - FirePosition.z).magnitude;
                var up = at.y - FirePosition.y;
                var g = (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.27f, across))) * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.42f, up)));
                bake[i] = new Vector2(bake[i].x, Mathf.Clamp01(0.06f + g * strength));
            }
            mesh.SetUVs(1, bake);
            return mesh;
        }

        static MeshFilter Static(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var filter = go.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return filter;
        }

        /// <summary>Generated meshes become assets so the scene references them instead of embedding them.</summary>
        static void SaveMeshes(IEnumerable<MeshFilter> filters)
        {
            if (!AssetDatabase.IsValidFolder(MeshFolder)) AssetDatabase.CreateFolder(SankofaArt.Folder, "Meshes");
            foreach (var filter in filters)
            {
                var path = $"{MeshFolder}/{filter.sharedMesh.name}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(filter.sharedMesh, path);
            }
        }

        // ---- fire, narrator and story objects ----

        static HearthFire BuildFire(Transform world, SankofaArtSet art)
        {
            var root = Child(world, "Hearth fire");
            root.localPosition = FirePosition;
            var fire = root.gameObject.AddComponent<HearthFire>();
            var offsets = new[]
            {
                Vector3.zero, new Vector3(0.07f, 0, 0.03f), new Vector3(-0.065f, 0, 0.05f), new Vector3(0.035f, 0, -0.065f), new Vector3(-0.045f, 0, -0.045f),
            };
            var layers = new Object[offsets.Length];
            var cards = new Object[offsets.Length];
            for (var i = 0; i < offsets.Length; i++)
            {
                var layer = Child(root, "Flame layer " + i);
                layer.localPosition = offsets[i];
                layer.gameObject.AddComponent<Billboard>();
                var card = Prim(layer, "Card", PrimitiveType.Quad, new Vector3(0, 0.5f, 0), Vector3.one, art.Flame);
                layers[i] = layer;
                cards[i] = card.GetComponent<Renderer>();
            }
            SetArray(fire, "layers", layers);
            SetArray(fire, "cards", cards);
            var scales = new SerializedObject(fire);
            var list = scales.FindProperty("layerScale");
            var sizes = new[] { new Vector2(1f, 1f), new Vector2(0.55f, 0.7f), new Vector2(0.5f, 0.62f), new Vector2(0.6f, 0.76f), new Vector2(0.45f, 0.55f) };
            list.arraySize = sizes.Length;
            for (var i = 0; i < sizes.Length; i++) list.GetArrayElementAtIndex(i).vector2Value = sizes[i];
            scales.ApplyModifiedPropertiesWithoutUndo();
            Set(fire, "sparks", BuildSparks(root, art));
            Set(fire, "smoke", BuildSmoke(root, art));
            // A soft halo stands in for bloom, which is too costly on Quest.
            var halo = Prim(root, "Halo", PrimitiveType.Quad, new Vector3(0, 0.38f, 0), Vector3.one * 1.8f, art.Halo);
            Set(halo.AddComponent<Billboard>(), "upright", false);
            Set(fire, "halo", halo.GetComponent<Renderer>());

            var sound = Child(root, "Crackle");
            sound.localPosition = new Vector3(0, 0.15f, 0);
            var crackle = sound.gameObject.AddComponent<FireCrackle>();
            var fireSource = Source(sound.gameObject, 0.9f, 1f);
            fireSource.clip = Recording("hearth-fire");
            Set(crackle, "sound", fireSource);
            Set(crackle, "volume", 0.5f);
            Set(fire, "crackle", crackle);
            return fire;
        }

        static ParticleSystem BuildSparks(Transform parent, SankofaArtSet art)
        {
            var go = Child(parent, "Sparks");
            go.localPosition = new Vector3(0, 0.08f, 0);
            var ps = go.gameObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.4f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.006f, 0.014f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.62f, 0.26f), new Color(1f, 0.86f, 0.55f));
            main.gravityModifier = -0.04f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 200;
            var emission = ps.emission;
            emission.rateOverTime = 10f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.12f;
            shape.rotation = new Vector3(-90f, 0, 0);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.25f;
            noise.frequency = 1.2f;
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(new Color(1f, 0.4f, 0.15f), 1) },
                new[] { new GradientAlphaKey(1, 0), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0, 1) });
            colour.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = art.Sparks;
            // Stretched along their motion, sparks read as streaks rather than dots.
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.03f;
            renderer.lengthScale = 1.2f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        static ParticleSystem BuildSmoke(Transform parent, SankofaArtSet art)
        {
            var go = Child(parent, "Smoke");
            go.localPosition = new Vector3(0, 0.45f, 0);
            var ps = go.gameObject.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(5f, 8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.12f, 0.24f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.28f, 0.48f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            // Colour only seeds each puff's wisp pattern; the shader lights the smoke itself.
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.1f, 0.4f, 1f, 1f), new Color(0.9f, 0.8f, 1f, 1f));
            main.gravityModifier = -0.008f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 14;
            main.prewarm = true;
            var emission = ps.emission;
            emission.rateOverTime = 1.2f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 8f;
            shape.radius = 0.08f;
            shape.rotation = new Vector3(-90f, 0, 0);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.12f;
            noise.frequency = 0.4f;
            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0, 0.6f, 1, 2.6f));
            var spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
            var colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) },
                new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, 0.2f), new GradientAlphaKey(0.7f, 0.6f), new GradientAlphaKey(0, 1) });
            colour.color = fade;
            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = art.Smoke;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        /// <summary>Crickets around the clearing, a distant chorus ahead and wind off to one side, all at the hearth.</summary>
        static NightAmbience BuildAmbience(Transform hearth)
        {
            var root = Child(hearth, "Night ambience");
            var crickets = new[] { new Vector3(-5.5f, 0.1f, 3.5f), new Vector3(6f, 0.1f, 5.5f), new Vector3(-4f, 0.1f, -5f), new Vector3(4.5f, 0.1f, -4f) }
                .Select((p, i) =>
                {
                    var c = Child(root, "Cricket " + i);
                    c.localPosition = p;
                    var source = Source(c.gameObject, 1f, 2f);
                    source.maxDistance = 30f;
                    source.clip = Recording("crickets");
                    return (Object)source;
                }).ToArray();
            var far = Child(root, "Chorus");
            far.localPosition = new Vector3(0, 1f, 12f);
            var wind = Child(root, "Wind");
            wind.localPosition = new Vector3(-9f, 3f, 2f);
            var ambience = root.gameObject.AddComponent<NightAmbience>();
            SetArray(ambience, "crickets", crickets);
            var chorus = Source(far.gameObject, 0.35f, 6f);
            chorus.clip = Recording("maasai-nature");
            Set(ambience, "chorus", chorus);
            var windSource = Source(wind.gameObject, 0.5f, 6f);
            windSource.clip = Recording("wind-grass");
            Set(ambience, "wind", windSource);
            return ambience;
        }

        static NarrationPlayer BuildNarrator(Transform world)
        {
            // The voice comes from the fire, and stays where the fire was while a story surrounds the visitor.
            var go = Child(world, "Narrator voice");
            go.localPosition = FirePosition + new Vector3(0, 0.7f, 0);
            var narration = go.gameObject.AddComponent<NarrationPlayer>();
            var voice = Source(go.gameObject, 0.85f, 1.6f);
            voice.loop = false;
            voice.priority = 0;
            Set(narration, "voice", voice);
            return narration;
        }

        static StageObject BuildEmber(Transform hearth, SankofaArtSet art)
        {
            var root = Child(hearth, "Ember");
            root.localPosition = FirePosition + new Vector3(0, 0.3f, 0);
            // A small coal whose cracks glow, rather than a flat emissive ball.
            var coal = SankofaMeshes.Stone(777, 2, 0.32f, 0.4f);
            coal.name = "Held ember";
            coal.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), coal.vertexCount).ToList());
            var core = Static(root, "Core", coal, art.HeldEmber);
            core.transform.localScale = new Vector3(0.032f, 0.024f, 0.028f);
            SaveMeshes(new[] { core });
            var glow = GlowCard(root, art, 0.26f);
            var o = root.gameObject.AddComponent<StageObject>();
            Set(o, "objectId", "ember");
            Set(o, "sceneId", "hearth");
            Set(o, "visibleAtStart", false);
            SetArray(o, "surfaces", new Object[] { core.GetComponent<Renderer>() });
            Set(o, "glow", glow);
            Set(o, "glowIntensity", 1.5f);
            return o;
        }

        static Renderer GlowCard(Transform parent, SankofaArtSet art, float size)
        {
            var card = Prim(parent, "Glow", PrimitiveType.Quad, Vector3.zero, Vector3.one * size, art.Glow);
            var billboard = card.AddComponent<Billboard>();
            Set(billboard, "upright", false);
            return card.GetComponent<Renderer>();
        }

        // ---- captions, controls and transition ----

        static CaptionBoard BuildCaptions(Transform world, SankofaArtSet art, Transform head)
        {
            var go = Child(world, "Captions");
            go.localPosition = new Vector3(0, 1.5f, 1.55f);
            go.localRotation = FacingVisitor(go.localPosition);
            var board = go.gameObject.AddComponent<CaptionBoard>();
            var font = SankofaText.CaptionFont;
            Set(board, "font", font);
            Set(board, "textMaterial", SankofaText.CaptionMaterial(font));
            Set(board, "backingMaterial", art.CaptionBacking);
            // Captions stay readable when the visitor looks at the lap, a figure or the far bank.
            var follow = go.gameObject.AddComponent<LazyFollow>();
            Set(follow, "head", head);
            Set(follow, "pivot", SeatedEye);
            return board;
        }

        /// <summary>
        /// The title card over the fire before a visit, above where captions will appear: the name, the place, and
        /// how to choose by touch, pinch or trigger. Sizes keep the smallest line above one degree from the seat.
        /// </summary>
        static StartScreen BuildStartScreen(Transform world)
        {
            var root = Child(world, "Start screen");
            root.localPosition = new Vector3(0, 1.62f, 1.8f);
            root.localRotation = FacingVisitor(root.localPosition);
            var font = SankofaText.CaptionFont;
            TextMeshPro Line(string name, string text, TMP_FontAsset face, float size, float y, float width, Color color, float spacing)
            {
                var go = Child(root, name);
                go.localPosition = new Vector3(0, y, 0);
                var t = go.gameObject.AddComponent<TextMeshPro>();
                t.font = face;
                t.fontSharedMaterial = SankofaText.CaptionMaterial(face);
                t.text = text;
                t.fontSize = size;
                t.characterSpacing = spacing;
                t.color = color;
                t.alignment = TextAlignmentOptions.Center;
                t.textWrappingMode = TextWrappingModes.Normal;
                t.rectTransform.sizeDelta = new Vector2(width, size * 0.3f);
                return t;
            }
            var screen = root.gameObject.AddComponent<StartScreen>();
            Set(screen, "title", Line("Title", "Sankofa", SankofaText.LabelFont, 1.15f, 0.16f, 1.4f, new Color(1f, 0.8f, 0.52f), 12f));
            Set(screen, "subtitle", Line("Place", "Enkare Nairobi \u00b7 the cool water", font, 0.42f, 0.02f, 1.4f, new Color(0.96f, 0.86f, 0.72f), 2f));
            Set(screen, "hint", Line("How to choose", "Sit comfortably. To choose, touch a button with a fingertip, or point and pinch. With controllers, point and pull the trigger.",
                font, 0.36f, -0.14f, 1.25f, new Color(0.9f, 0.84f, 0.76f), 0f));
            return screen;
        }

        /// <summary>A pointer per side, for choosing by pinch or trigger from the seat; the right one also takes the mouse in the editor.</summary>
        static void BuildPointers(Rig rig, SankofaArtSet art, PokePanel[] panels)
        {
            var sources = new GameObject("Pointers");
            for (var i = 0; i < 2; i++)
            {
                var left = i == 0;
                var go = new GameObject(left ? "Left pointer" : "Right pointer");
                go.transform.SetParent(sources.transform, false);
                var pointer = go.AddComponent<PointerSource>();
                var line = go.AddComponent<LineRenderer>();
                line.useWorldSpace = true;
                line.positionCount = 2;
                line.widthMultiplier = 0.003f;
                line.numCapVertices = 2;
                line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                line.receiveShadows = false;
                line.sharedMaterial = art.Pointer;
                line.enabled = false;
                var dot = Prim(go.transform, "Dot", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.01f, art.Pointer);
                dot.SetActive(false);
                Set(pointer, "left", left);
                Set(pointer, "hand", rig.Hands[i]);
                Set(pointer, "controller", rig.Controllers[i]);
                SetArray(pointer, "panels", panels.Cast<Object>().ToArray());
                Set(pointer, "ray", line);
                Set(pointer, "dot", dot.transform);
                Set(pointer, "eye", rig.Eye);
                Set(pointer, "mouseInEditor", !left);
            }
        }

        static PokePanel BuildPanel(Transform world, SankofaArtSet art, string name, Vector3 position, int columns)
        {
            var go = Child(world, name + " panel");
            go.localPosition = position;
            go.localRotation = FacingVisitor(position);
            var panel = go.gameObject.AddComponent<PokePanel>();
            Set(panel, "columns", columns);
            Set(panel, "buttonMaterial", art.Plaque);
            Set(panel, "labelFont", SankofaText.LabelFont);
            Set(panel, "labelColor", new Color(1f, 0.9f, 0.74f));
            Set(panel, "glow", 0.35f);
            Set(panel, "idle", new Color(0.3f, 0.2f, 0.13f, 1f));
            Set(panel, "hover", new Color(0.55f, 0.36f, 0.2f, 1f));
            Set(panel, "pressed", new Color(1f, 0.6f, 0.28f, 1f));
            return panel;
        }

        /// <summary>Faces (local −Z) toward the seated eye, as button quads and text are seen from −Z.</summary>
        static Quaternion FacingVisitor(Vector3 position) => Quaternion.LookRotation(position - SeatedEye, Vector3.up);

        static Renderer BuildCurtain(Transform head, SankofaArtSet art)
        {
            var sphere = Prim(head, "Transition curtain", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.6f, art.Curtain);
            var renderer = sphere.GetComponent<Renderer>();
            renderer.enabled = false;
            return renderer;
        }

        static void ConfigureScenes(StoryStage stage, Transform hearth, Transform early, Transform railway, Transform today)
        {
            var so = new SerializedObject(stage);
            var list = so.FindProperty("scenes");
            list.arraySize = 4;
            var night = list.GetArrayElementAtIndex(0);
            Scene(night, "hearth", true, hearth, new Color(0.012f, 0.016f, 0.035f),
                new Color(0.02f, 0.026f, 0.048f), new Color(0.014f, 0.011f, 0.009f), 9f, 45f);
            night.FindPropertyRelative("TowardMoon").vector3Value = HearthMoon;
            night.FindPropertyRelative("Moon").colorValue = new Color(0.05f, 0.062f, 0.095f);
            night.FindPropertyRelative("MistColor").colorValue = new Color(0.045f, 0.055f, 0.08f);
            night.FindPropertyRelative("Mist").vector4Value = new Vector4(0.65f, 0.15f, 1.3f, 6f);
            night.FindPropertyRelative("Grade").vector4Value = new Vector4(1.3f, 0.92f, 1f, 1f);
            ConfigureWater1850s(list.GetArrayElementAtIndex(1), early);
            ConfigureWater1899(list.GetArrayElementAtIndex(2), railway);
            ConfigureWaterToday(list.GetArrayElementAtIndex(3), today);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Scene(SerializedProperty e, string id, bool isHearth, Transform root, Color sky, Color ambientSky, Color ambientGround, float fogStart, float fogEnd)
        {
            e.FindPropertyRelative("Id").stringValue = id;
            e.FindPropertyRelative("IsHearth").boolValue = isHearth;
            e.FindPropertyRelative("Root").objectReferenceValue = root.gameObject;
            e.FindPropertyRelative("Sky").colorValue = sky;
            e.FindPropertyRelative("AmbientSky").colorValue = ambientSky;
            e.FindPropertyRelative("AmbientGround").colorValue = ambientGround;
            e.FindPropertyRelative("Fog").colorValue = sky;
            e.FindPropertyRelative("FogStart").floatValue = fogStart;
            e.FindPropertyRelative("FogEnd").floatValue = fogEnd;
        }

        // ---- helpers ----

        public const string RecordedFolder = "Assets/Sankofa/Art/Recorded/";

        /// <summary>A prepared field recording (see Art/Recorded/sources.json and scripts/prepare_recordings.py).</summary>
        static AudioClip Recording(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(RecordedFolder + name + ".wav");
            if (clip == null) throw new System.IO.FileNotFoundException("Missing recording; run scripts/prepare_recordings.py", RecordedFolder + name + ".wav");
            return clip;
        }

        static AudioSource Source(GameObject go, float spatial, float minDistance)
        {
            var source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = spatial;
            source.rolloffMode = AudioRolloffMode.Logarithmic;
            source.minDistance = minDistance;
            source.maxDistance = 25f;
            source.dopplerLevel = 0;
            return source;
        }

        static Transform Child(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static Transform Point(Transform parent, string name, Vector3 position)
        {
            var t = Child(parent, name);
            t.localPosition = position;
            return t;
        }

        static GameObject Prim(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, Quaternion? rotation = null)
        {
            var go = GameObject.CreatePrimitive(type);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localRotation = rotation ?? Quaternion.identity;
            go.transform.localScale = scale;
            var renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

        static Transform Find(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).First(t => t.name == name);

        static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            SetValue(so, field, value);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetValue(SerializedObject so, string field, object value)
        {
            var p = so.FindProperty(field) ?? throw new System.ArgumentException($"{so.targetObject.GetType().Name} has no serialized field {field}");
            switch (value)
            {
                case null: p.objectReferenceValue = null; break;
                case Object o: p.objectReferenceValue = o; break;
                case bool b: p.boolValue = b; break;
                case int i: p.intValue = i; break;
                case float f: p.floatValue = f; break;
                case string s: p.stringValue = s; break;
                case Color c: p.colorValue = c; break;
                case Vector2 v: p.vector2Value = v; break;
                case Vector3 v: p.vector3Value = v; break;
                default: throw new System.ArgumentException("Unsupported serialized value " + value.GetType().Name);
            }
        }

        static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field) ?? throw new System.ArgumentException($"{target.GetType().Name} has no serialized field {field}");
            p.arraySize = values.Length;
            for (var i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
