using System.Collections.Generic;
using System.Linq;
using Sankofa.Runtime.Stage;
using UnityEditor;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// The placeholder package's story scenes around the stationary visitor: a clay worker's yard at late dusk and a
    /// road at night. Imagined places for exercising the story system, not art direction for a selected setting.
    /// Interactive objects stay within a seated reach on the visitor's right, away from the controls on the left.
    /// </summary>
    public static partial class SankofaSceneBuilder
    {
        // ---- the yard ----

        static readonly Vector3 YardMoon = new Vector3(0.35f, 0.5f, 0.8f);
        static readonly Vector3 Kiln = new Vector3(-2.1f, 0, 2.5f);
        const float BenchTop = 0.52f;
        static readonly Vector3 BowlAt = new Vector3(0.06f, BenchTop, 0.62f);
        static readonly Vector3 YardLanternAt = new Vector3(0.4f, BenchTop + 0.065f, 0.72f);

        static Vector3 KilnMouth => Kiln + new Vector3(-Kiln.x, 0, -Kiln.z).normalized * 0.71f + Vector3.up * 0.2f;

        static float YardGround(Vector3 p)
        {
            var r = new Vector2(p.x, p.z).magnitude;
            var yard = SankofaMeshes.Fbm(p * 1.5f, 41, 3) * 0.012f;
            var outside = SankofaMeshes.Fbm(p * 0.16f, 43, 4) * 0.9f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4.5f, 12f, r));
            var hills = (SankofaMeshes.Fbm(p * 0.035f, 47, 3) + 0.42f) * 9f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(22f, 60f, r));
            return yard + outside + hills;
        }

        static StageObject[] BuildYard(Transform root, SankofaArtSet art)
        {
            var random = new System.Random(23);
            float R(float a, float b) => Range(random, a, b);
            var mouth = KilnMouth;

            Color Tint(Vector3 p)
            {
                var r = new Vector2(p.x, p.z).magnitude;
                var swept = new Color(0.86f, 0.76f, 0.66f) * (1f + SankofaMeshes.Fbm(p * 1.1f, 45, 3) * 0.25f);
                var wild = new Color(0.78f, 0.78f, 0.68f) * (1f + SankofaMeshes.Fbm(p * 0.3f, 46, 2) * 0.3f);
                var c = Color.Lerp(swept, wild, Mathf.InverseLerp(3.6f, 5f, r));
                return Color.Lerp(c, new Color(0.5f, 0.5f, 0.46f), Mathf.InverseLerp(18f, 45f, r));
            }
            float Burn(Vector3 p)
            {
                var ragged = SankofaMeshes.Fbm(p * 4f, 49, 2) * 0.2f;
                return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f + ragged, 1.15f + ragged, Vector3.Distance(new Vector3(p.x, 0, p.z), new Vector3(mouth.x, 0, mouth.z))));
            }
            var ground = Static(root, "Ground", SankofaStoryMeshes.Ground("Yard ground", YardGround, Tint, Burn), art.Ground);

            // A closed yard with one doorway ahead and to the right, a little taller than a seated visitor's eyes.
            var wallPath = new[]
            {
                new Vector3(0.55f, 0, 3.7f), new Vector3(-1.8f, 0, 3.85f), new Vector3(-3.3f, 0, 3.2f), new Vector3(-3.55f, 0, 0.5f),
                new Vector3(-3.3f, 0, -2.3f), new Vector3(-1f, 0, -2.85f), new Vector3(1.6f, 0, -2.75f), new Vector3(3.3f, 0, -2.2f),
                new Vector3(3.55f, 0, 0.8f), new Vector3(3.3f, 0, 3.2f), new Vector3(1.75f, 0, 3.7f),
            };
            var walls = Static(root, "Walls", SankofaStoryMeshes.Wall("Yard walls", wallPath, 1.95f, 0.38f, 61, YardGround), art.EarthWall);

            // A beehive kiln in the far corner, still glowing at its mouth, with firewood stacked beside it.
            var kilnParts = new List<CombineInstance>
            {
                Part(SankofaMeshes.Stone(1201, 3, 0.035f, 0.1f), Kiln + Vector3.up * 0.2f, Quaternion.Euler(0, 30, 0), new Vector3(0.74f, 0.7f, 0.74f)),
            };
            var kiln = Static(root, "Kiln", Combine(kilnParts, "Kiln"), art.EarthWall);
            // The firing mouth: a charred arch in the dome with coals still glowing inside it.
            var facing = Quaternion.LookRotation(new Vector3(-Kiln.x, 0, -Kiln.z), Vector3.up);
            var opening = SankofaMeshes.Stone(1209, 2, 0.08f, 0.6f);
            opening.name = "Kiln mouth";
            opening.SetUVs(1, Enumerable.Repeat(new Vector2(1, 0.06f), opening.vertexCount).ToList());
            var mouthFilter = Static(root, "Kiln mouth", opening, art.BurningWood);
            mouthFilter.transform.localPosition = mouth;
            mouthFilter.transform.localRotation = facing;
            mouthFilter.transform.localScale = new Vector3(0.24f, 0.2f, 0.07f);
            var coals = new List<CombineInstance>();
            for (var i = 0; i < 9; i++)
            {
                var at = mouth + facing * new Vector3(R(-0.13f, 0.13f), R(-0.13f, -0.04f), R(0.01f, 0.05f));
                var size = R(0.025f, 0.05f);
                var mesh = SankofaMeshes.Stone(1210 + i, 1, 0.3f, 0.3f);
                mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, R(0.8f, 1.3f)), mesh.vertexCount).ToList());
                coals.Add(Part(mesh, at, Quaternion.Euler(R(0, 360), R(0, 360), R(0, 360)), new Vector3(size * 1.3f, size * 0.8f, size)));
            }
            var kilnCoals = Static(root, "Kiln coals", Combine(coals, "Kiln coals"), art.Embers);
            var wood = new List<CombineInstance>();
            for (var i = 0; i < 7; i++)
            {
                var layer = i < 4 ? 0 : 1;
                var at = Kiln + new Vector3(1.05f + (i % 4) * 0.13f - layer * 0.06f, 0.07f + layer * 0.11f, -0.5f + R(-0.03f, 0.03f));
                wood.Add(Part(SankofaMeshes.Log(1220 + i, R(0.6f, 0.75f), R(0.045f, 0.06f), 0.08f, 0.02f, 10, 6), at, Quaternion.Euler(90, R(-8, 8), 0), Vector3.one));
            }
            var firewood = Static(root, "Firewood", Combine(wood, "Yard firewood"), art.SeatWood);

            // Pots drying along the walls and by the kiln, and two on the bench.
            var pots = new List<CombineInstance>();
            var spots = new[]
            {
                (new Vector3(-1.2f, 0, 3.3f), 0.42f), (new Vector3(-0.85f, 0, 3.4f), 0.3f), (new Vector3(-2.9f, 0, 1.4f), 0.5f),
                (new Vector3(-3.0f, 0, 0.7f), 0.36f), (new Vector3(-2.75f, 0, 2.6f), 0.28f), (new Vector3(2.9f, 0, 1.9f), 0.46f),
                (new Vector3(3.0f, 0, 1.2f), 0.33f), (new Vector3(2.6f, 0, 2.7f), 0.38f), (new Vector3(-1.5f, 0, 2.1f), 0.22f),
                (new Vector3(0.48f, BenchTop, 0.86f), 0.14f), (new Vector3(-0.2f, BenchTop, 0.86f), 0.11f),
            };
            for (var i = 0; i < spots.Length; i++)
            {
                var (at, height) = spots[i];
                var profile = SankofaStoryMeshes.JarProfile(height, height * R(0.32f, 0.45f), height * R(0.14f, 0.22f), height * R(0.18f, 0.26f));
                var tone = R(0.75f, 1.05f);
                var mesh = SankofaStoryMeshes.Lathe("Pot " + i, profile, 28, (a, y) => new Color(tone, tone * R(0.9f, 1f), tone * R(0.85f, 1f)), 300 + i);
                if (at.y == 0) at.y = YardGround(at);
                pots.Add(Part(mesh, at, Quaternion.Euler(0, R(0, 360), 0), Vector3.one));
            }
            var potFilter = Static(root, "Pots", Combine(pots, "Yard pots"), art.FiredClay);

            // The bench the bowl stands on, and the frame of the doorway.
            Prim(root, "Bench top", PrimitiveType.Cube, new Vector3(0.16f, BenchTop - 0.02f, 0.74f), new Vector3(0.86f, 0.04f, 0.44f), art.Workbench);
            foreach (var (x, z) in new[] { (-0.22f, 0.56f), (0.54f, 0.56f), (-0.22f, 0.92f), (0.54f, 0.92f) })
                Prim(root, "Bench leg", PrimitiveType.Cube, new Vector3(x, (BenchTop - 0.04f) / 2f, z), new Vector3(0.05f, BenchTop - 0.04f, 0.05f), art.Workbench);
            Prim(root, "Door post west", PrimitiveType.Cube, new Vector3(0.62f, 1.05f, 3.72f), new Vector3(0.14f, 2.1f, 0.42f), art.Workbench);
            Prim(root, "Door post east", PrimitiveType.Cube, new Vector3(1.68f, 1.05f, 3.72f), new Vector3(0.14f, 2.1f, 0.42f), art.Workbench);
            Prim(root, "Lintel", PrimitiveType.Cube, new Vector3(1.15f, 2.15f, 3.72f), new Vector3(1.3f, 0.14f, 0.46f), art.Workbench);

            // Trees beyond the walls, their crowns above the wall line, and grass outside.
            var trunks = new List<CombineInstance>();
            var canopies = new List<CombineInstance>();
            for (var i = 0; i < 12; i++)
            {
                var degrees = i * 30f + R(-10, 10);
                var at = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad)) * R(10f, 17f);
                at.y = YardGround(at) - 0.08f;
                SankofaMeshes.Tree(640 + i, R(4f, 6.2f), out var trunk, out var canopy);
                var m = Matrix4x4.TRS(at, Quaternion.Euler(0, R(0, 360), 0), Vector3.one);
                trunks.Add(new CombineInstance { mesh = trunk, transform = m });
                canopies.Add(new CombineInstance { mesh = canopy, transform = m });
            }
            var trunkFilter = Static(root, "Tree trunks", Combine(trunks, "Yard tree trunks"), art.TreeBark);
            var canopyFilter = Static(root, "Tree canopies", Combine(canopies, "Yard tree canopies"), art.TreeCanopy);
            var grassMesh = SankofaMeshes.Grass(Vector3.zero, Vector3.zero, 71, 420, 4.6f, 11f, YardGround);
            grassMesh.name = "Yard grass";
            var grass = Static(root, "Grass", grassMesh, art.Grass);

            Prim(root, "Sky", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 140f, art.Sky);
            var starMesh = SankofaMeshes.Stars(1300, 9, 62f);
            starMesh.name = "Yard stars";
            var stars = Static(root, "Stars", starMesh, art.Stars);

            // The lantern on the bench lights the yard; its shadows and the moon's are baked like the hearth's.
            var light = root.TransformPoint(YardLanternAt + Vector3.up * 0.05f);
            SankofaBake.Bake(
                new[] { ground, walls, kiln, potFilter, firewood, trunkFilter, canopyFilter },
                new[] { ground, walls, kiln, potFilter, firewood, trunkFilter, canopyFilter },
                light - Vector3.up * 0.36f, root.TransformDirection(YardMoon),
                new HashSet<MeshFilter> { ground, walls, kiln, potFilter, firewood, trunkFilter, canopyFilter });
            SaveMeshes(new[] { ground, walls, kiln, mouthFilter, kilnCoals, potFilter, firewood, trunkFilter, canopyFilter, grass, stars });

            var lantern = BuildLantern(root, art, "lantern", "story.yard", YardLanternAt);
            var bowl = BuildBowl(root, art, BowlAt, out var chipCentre);
            var shard = BuildShard(root, art, "shard", "story.yard", new Vector3(-0.15f, BenchTop + 0.025f, 0.66f));
            return new[] { lantern, bowl, shard };
        }

        static void ConfigureYard(SerializedProperty e, Transform root)
        {
            Scene(e, "story.yard", false, root, new Color(0.035f, 0.03f, 0.06f),
                new Color(0.06f, 0.055f, 0.085f), new Color(0.03f, 0.022f, 0.016f), 7f, 40f);
            e.FindPropertyRelative("TowardMoon").vector3Value = YardMoon;
            e.FindPropertyRelative("Moon").colorValue = new Color(0.06f, 0.068f, 0.095f);
            e.FindPropertyRelative("MistColor").colorValue = new Color(0.05f, 0.05f, 0.08f);
            e.FindPropertyRelative("Mist").vector4Value = new Vector4(0.4f, 0.1f, 1.5f, 5f);
            e.FindPropertyRelative("Grade").vector4Value = new Vector4(1.3f, 0.95f, 0.95f, 0.9f);
        }

        // ---- the road ----

        static readonly Vector3 RoadMoon = new Vector3(-0.3f, 0.65f, 0.7f);
        static readonly Vector3 WaymarkerAt = new Vector3(0.16f, 0, 0.6f);
        static readonly Vector3 CairnAt = new Vector3(0.42f, 0, 0.38f);
        static readonly Vector3 RoadLanternAt = new Vector3(-0.24f, 0.29f, 0.82f);
        static readonly Vector3[] RoadPath =
        {
            new Vector3(-30f, 0, -6f), new Vector3(-12f, 0, -1.5f), new Vector3(-5f, 0, 1f), new Vector3(-1.5f, 0, 1.75f),
            new Vector3(1.5f, 0, 2.05f), new Vector3(4f, 0, 3.3f), new Vector3(6.5f, 0, 7f), new Vector3(8f, 0, 14f),
            new Vector3(7.5f, 0, 24f), new Vector3(4f, 0, 40f), new Vector3(0f, 0, 70f),
        };

        static float RoadDistance(Vector3 p)
        {
            var best = float.MaxValue;
            var q = new Vector2(p.x, p.z);
            for (var i = 0; i < RoadPath.Length - 1; i++)
            {
                var a = new Vector2(RoadPath[i].x, RoadPath[i].z);
                var b = new Vector2(RoadPath[i + 1].x, RoadPath[i + 1].z);
                var t = Mathf.Clamp01(Vector2.Dot(q - a, b - a) / (b - a).sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(q, a + (b - a) * t));
            }
            return best;
        }

        static float RoadWeight(Vector3 p) => 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.65f, 1.15f, RoadDistance(p) + SankofaMeshes.Fbm(p * 2.2f, 55, 2) * 0.25f));

        static float RoadGround(Vector3 p)
        {
            var r = new Vector2(p.x, p.z).magnitude;
            var near = SankofaMeshes.Fbm(p * 0.7f, 51, 3) * 0.06f;
            var rolling = SankofaMeshes.Fbm(p * 0.12f, 53, 4) * 1.4f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(3f, 14f, r));
            var hills = (SankofaMeshes.Fbm(p * 0.03f, 57, 3) + 0.45f) * 11f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(24f, 65f, r));
            var natural = near + rolling + hills;
            // The road is worn a little lower and smoother than the verges.
            var road = rolling + hills + near * 0.3f - 0.03f;
            return Mathf.Lerp(natural, road, RoadWeight(p));
        }

        static StageObject[] BuildRoad(Transform root, SankofaArtSet art)
        {
            var random = new System.Random(29);
            float R(float a, float b) => Range(random, a, b);

            Color Tint(Vector3 p)
            {
                var r = new Vector2(p.x, p.z).magnitude;
                var d = RoadDistance(p);
                var ruts = 1f - 0.24f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.04f, 0.13f, Mathf.Abs(d - 0.34f))));
                var road = new Color(1.35f, 1.2f, 1.02f) * ruts * (1f + SankofaMeshes.Fbm(p * 1.6f, 58, 2) * 0.2f);
                var verge = new Color(0.52f, 0.56f, 0.44f) * (1f + SankofaMeshes.Fbm(p * 0.5f, 59, 3) * 0.35f);
                var c = Color.Lerp(verge, road, RoadWeight(p));
                return Color.Lerp(c, new Color(0.45f, 0.47f, 0.44f), Mathf.InverseLerp(20f, 55f, r));
            }
            var ground = Static(root, "Ground", SankofaStoryMeshes.Ground("Road ground", RoadGround, Tint, p => 0f, 5f, 90f), art.Ground);

            // Stones along the verges, and the flat one the lantern stands on.
            var rocks = new List<CombineInstance>();
            var lanternRock = new Vector3(RoadLanternAt.x, 0, RoadLanternAt.z);
            lanternRock.y = RoadGround(lanternRock) + 0.12f;
            rocks.Add(Part(SankofaMeshes.Stone(1401, 3, 0.16f, 0.2f), lanternRock, Quaternion.Euler(0, 20, 0), new Vector3(0.24f, 0.15f, 0.2f)));
            for (var i = 0; i < 40; i++)
            {
                var along = RoadPath[1 + random.Next(RoadPath.Length - 3)];
                var at = along + new Vector3(R(-3f, 3f), 0, R(-3f, 3f));
                if (RoadDistance(at) < 0.9f || new Vector2(at.x, at.z).magnitude < 1.2f) continue;
                var size = R(0.05f, 0.22f);
                at.y = RoadGround(at) + size * 0.25f;
                rocks.Add(Part(SankofaMeshes.Stone(1410 + i, 2, 0.25f, 0.3f), at, Quaternion.Euler(R(-10, 10), R(0, 360), R(-10, 10)), new Vector3(size * 1.3f, size * 0.7f, size)));
            }
            var rockFilter = Static(root, "Stones", Combine(rocks, "Road stones"), art.Rock);

            var trunks = new List<CombineInstance>();
            var canopies = new List<CombineInstance>();
            for (var i = 0; i < 40 && trunks.Count < 16; i++)
            {
                var degrees = R(0, 360);
                var at = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad)) * R(8f, 24f);
                // Keep the view ahead open, where the road leads off toward the hills under the moon.
                if (RoadDistance(at) < 2.5f || Mathf.Abs(Mathf.DeltaAngle(degrees, 80f)) < 45f) continue;
                at.y = RoadGround(at) - 0.08f;
                SankofaMeshes.Tree(700 + i, R(3.8f, 6.8f), out var trunk, out var canopy);
                var m = Matrix4x4.TRS(at, Quaternion.Euler(0, R(0, 360), 0), Vector3.one);
                trunks.Add(new CombineInstance { mesh = trunk, transform = m });
                canopies.Add(new CombineInstance { mesh = canopy, transform = m });
            }
            var trunkFilter = Static(root, "Tree trunks", Combine(trunks, "Road tree trunks"), art.TreeBark);
            var canopyFilter = Static(root, "Tree canopies", Combine(canopies, "Road tree canopies"), art.TreeCanopy);
            var grassMesh = SankofaMeshes.Grass(Vector3.zero, Vector3.zero, 73, 950, 0.9f, 13f, RoadGround,
                p => RoadDistance(p) > 0.95f && Vector3.Distance(new Vector3(p.x, 0, p.z), WaymarkerAt) > 0.3f && Vector3.Distance(new Vector3(p.x, 0, p.z), CairnAt) > 0.35f);
            grassMesh.name = "Road grass";
            var grass = Static(root, "Grass", grassMesh, art.Grass);

            Prim(root, "Sky", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 160f, art.Sky);
            var starMesh = SankofaMeshes.Stars(2200, 13, 70f);
            starMesh.name = "Road stars";
            var stars = Static(root, "Stars", starMesh, art.Stars);

            var waymarker = BuildWaymarker(root, art, out var waymarkerFilter);
            var cairn = BuildCairn(root, art, out var cairnFilter);

            var light = root.TransformPoint(RoadLanternAt + Vector3.up * 0.05f);
            SankofaBake.Bake(
                new[] { ground, rockFilter, trunkFilter, canopyFilter, waymarkerFilter, cairnFilter },
                new[] { ground, rockFilter, trunkFilter, canopyFilter, waymarkerFilter, cairnFilter },
                light - Vector3.up * 0.36f, root.TransformDirection(RoadMoon),
                new HashSet<MeshFilter> { ground, rockFilter, trunkFilter, canopyFilter, waymarkerFilter, cairnFilter });
            SaveMeshes(new[] { ground, rockFilter, trunkFilter, canopyFilter, grass, stars, waymarkerFilter, cairnFilter });

            var lantern = BuildLantern(root, art, "road.lantern", "story.road", RoadLanternAt);
            var waystone = BuildWaystone(root, art, "waystone", "story.road",
                new Vector3(WaymarkerAt.x - 0.12f, RoadGround(WaymarkerAt) + 0.025f, WaymarkerAt.z - 0.1f));
            return new[] { lantern, waymarker, waystone, cairn };
        }

        static void ConfigureRoad(SerializedProperty e, Transform root)
        {
            Scene(e, "story.road", false, root, new Color(0.012f, 0.018f, 0.04f),
                new Color(0.028f, 0.034f, 0.06f), new Color(0.012f, 0.011f, 0.01f), 12f, 70f);
            e.FindPropertyRelative("TowardMoon").vector3Value = RoadMoon;
            e.FindPropertyRelative("Moon").colorValue = new Color(0.085f, 0.1f, 0.15f);
            e.FindPropertyRelative("MistColor").colorValue = new Color(0.04f, 0.05f, 0.075f);
            e.FindPropertyRelative("Mist").vector4Value = new Vector4(0.8f, 0.1f, 1.0f, 8f);
            e.FindPropertyRelative("Grade").vector4Value = new Vector4(1.35f, 0.9f, 1.05f, 0.85f);
        }

        // ---- story objects ----

        static StageObject Staged(GameObject go, string id, string scene, bool visible, IEnumerable<Renderer> surfaces)
        {
            var o = go.AddComponent<StageObject>();
            Set(o, "objectId", id);
            Set(o, "sceneId", scene);
            Set(o, "visibleAtStart", visible);
            SetArray(o, "surfaces", surfaces.Cast<Object>().ToArray());
            return o;
        }

        static Transform Marker(Transform parent, string name, Vector3 local)
        {
            var t = Child(parent, name);
            t.localPosition = local;
            return t;
        }

        static StageObject BuildLantern(Transform parent, SankofaArtSet art, string id, string scene, Vector3 position)
        {
            var root = Child(parent, id == "lantern" ? "Lantern" : "Lantern " + id);
            root.localPosition = position;
            var parts = new[]
            {
                Prim(root, "Base", PrimitiveType.Cylinder, new Vector3(0, -0.06f, 0), new Vector3(0.11f, 0.012f, 0.11f), art.Brass),
                Prim(root, "Flame", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 0.06f, art.LanternCore),
                Prim(root, "Cap", PrimitiveType.Cylinder, new Vector3(0, 0.065f, 0), new Vector3(0.1f, 0.01f, 0.1f), art.Brass),
                Prim(root, "Handle", PrimitiveType.Cylinder, new Vector3(0, 0.085f, 0), new Vector3(0.02f, 0.012f, 0.02f), art.Brass),
            }.Select(p => p.GetComponent<Renderer>()).ToList();
            for (var i = 0; i < 4; i++)
            {
                var angle = (45 + i * 90) * Mathf.Deg2Rad;
                parts.Add(Prim(root, "Rib " + i, PrimitiveType.Cylinder, new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 0.048f,
                    new Vector3(0.008f, 0.06f, 0.008f), art.Brass).GetComponent<Renderer>());
            }
            var o = Staged(root.gameObject, id, scene, false, parts);
            Set(o, "glow", GlowCard(root, art, 0.5f));
            Set(o, "glowIntensity", 1.1f);
            Set(o, "emitsLight", true);
            Set(o, "lightColor", new Color(1f, 0.72f, 0.44f));
            Set(o, "lightIntensity", 1.4f);
            return o;
        }

        /// <summary>The chipped bowl: touchable at the front of its rim, and the piece that broke away fits its chip.</summary>
        static StageObject BuildBowl(Transform parent, SankofaArtSet art, Vector3 position, out Vector3 chipCentre)
        {
            var root = Child(parent, "Bowl");
            root.localPosition = position;
            var body = Static(root, "Body", SankofaStoryMeshes.ChippedBowl("Chipped bowl", 72), art.FiredClay);
            SaveMeshes(new[] { body });
            SankofaStoryMeshes.Shard("Bowl piece", out chipCentre);
            var o = Staged(root.gameObject, "bowl", "story.yard", true, new[] { body.GetComponent<Renderer>() });
            Set(o, "touchPoint", Marker(root, "Touch point", new Vector3(0, SankofaStoryMeshes.BowlHeight, -0.105f)));
            Set(o, "touchRadius", 0.09f);
            Set(o, "attentionRadius", 0.13f);
            Set(o, "placePoint", Marker(root, "Place point", chipCentre));
            return o;
        }

        static StageObject BuildShard(Transform parent, SankofaArtSet art, string id, string scene, Vector3 position)
        {
            var root = Child(parent, id == "shard" ? "Bowl piece" : "Kept bowl piece");
            root.localPosition = position;
            var mesh = SankofaStoryMeshes.Shard(id == "shard" ? "Bowl piece" : "Kept bowl piece", out _);
            var body = Static(root, "Body", mesh, art.FiredClay);
            SaveMeshes(new[] { body });
            var o = Staged(root.gameObject, id, scene, false, new[] { body.GetComponent<Renderer>() });
            Set(o, "attentionRadius", 0.06f);
            // A little of the fire's light lingers on what it gives, so a small piece reads at arm's length.
            Set(o, "glow", GlowCard(root, art, 0.13f));
            Set(o, "glowIntensity", 0.35f);
            return o;
        }

        static StageObject BuildWaystone(Transform parent, SankofaArtSet art, string id, string scene, Vector3 position)
        {
            var root = Child(parent, id == "waystone" ? "Waystone" : "Kept waystone");
            root.localPosition = position;
            var mesh = SankofaMeshes.Stone(id == "waystone" ? 1501 : 1502, 2, 0.12f, 0.45f);
            mesh.name = id == "waystone" ? "Waystone" : "Kept waystone";
            // Carried about, so it is lit fully rather than with baked shadows.
            mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), mesh.vertexCount).ToList());
            var body = Static(root, "Body", mesh, art.Rock);
            body.transform.localScale = new Vector3(0.034f, 0.022f, 0.028f);
            body.transform.localRotation = Quaternion.Euler(0, 35, 0);
            SaveMeshes(new[] { body });
            var o = Staged(root.gameObject, id, scene, false, new[] { body.GetComponent<Renderer>() });
            Set(o, "attentionRadius", 0.05f);
            Set(o, "glow", GlowCard(root, art, 0.12f));
            Set(o, "glowIntensity", 0.35f);
            return o;
        }

        /// <summary>A standing stone beside the road, touchable on its upper face toward the visitor.</summary>
        static StageObject BuildWaymarker(Transform parent, SankofaArtSet art, out MeshFilter filter)
        {
            var root = Child(parent, "Waymarker");
            var at = WaymarkerAt;
            at.y = RoadGround(at);
            root.localPosition = at;
            var mesh = SankofaMeshes.Stone(1301, 3, 0.12f, 0.3f);
            mesh.name = "Waymarker";
            filter = Static(root, "Body", mesh, art.Rock);
            filter.transform.localPosition = new Vector3(0, 0.34f, 0);
            filter.transform.localRotation = Quaternion.Euler(4, 25, -3);
            filter.transform.localScale = new Vector3(0.13f, 0.42f, 0.1f);
            var o = Staged(root.gameObject, "waymarker", "story.road", true, new[] { filter.GetComponent<Renderer>() });
            Set(o, "touchPoint", Marker(root, "Touch point", new Vector3(-0.01f, 0.6f, -0.09f)));
            Set(o, "touchRadius", 0.1f);
            Set(o, "attentionRadius", 0.3f);
            return o;
        }

        /// <summary>A pile of travellers' stones, with room on top for one more.</summary>
        static StageObject BuildCairn(Transform parent, SankofaArtSet art, out MeshFilter filter)
        {
            var root = Child(parent, "Cairn");
            var at = CairnAt;
            at.y = RoadGround(at);
            root.localPosition = at;
            var random = new System.Random(31);
            float R(float a, float b) => Range(random, a, b);
            // Built up on a boulder, so its top is within a seated reach.
            var stones = new List<CombineInstance>
            {
                Part(SankofaMeshes.Stone(1599, 3, 0.18f, 0.3f), new Vector3(0, 0.1f, 0), Quaternion.Euler(0, 40, 0), new Vector3(0.24f, 0.17f, 0.22f)),
            };
            var y = 0.21f;
            var layers = new[] { 4, 3, 3, 2, 1 };
            for (var l = 0; l < layers.Length; l++)
            {
                var size = 0.12f - l * 0.018f;
                for (var k = 0; k < layers[l]; k++)
                {
                    var angle = k * Mathf.PI * 2f / layers[l] + l;
                    var spread = layers[l] == 1 ? 0f : size * 0.9f;
                    var p = new Vector3(Mathf.Cos(angle) * spread, y + size * 0.45f, Mathf.Sin(angle) * spread);
                    stones.Add(Part(SankofaMeshes.Stone(1600 + l * 10 + k, 2, 0.2f, 0.35f), p, Quaternion.Euler(R(-12, 12), R(0, 360), R(-12, 12)), new Vector3(size * 1.2f, size * 0.62f, size)));
                }
                y += size * 0.85f;
            }
            filter = Static(root, "Body", Combine(stones, "Cairn"), art.Rock);
            var o = Staged(root.gameObject, "cairn", "story.road", true, new[] { filter.GetComponent<Renderer>() });
            Set(o, "touchPoint", Marker(root, "Touch point", new Vector3(-0.02f, y - 0.02f, -0.02f)));
            Set(o, "touchRadius", 0.1f);
            Set(o, "attentionRadius", 0.2f);
            Set(o, "placePoint", Marker(root, "Place point", new Vector3(0, y + 0.012f, 0)));
            return o;
        }
    }
}
