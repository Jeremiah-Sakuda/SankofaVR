using System.Collections.Generic;
using System.Linq;
using Sankofa.Runtime.Audio;
using Sankofa.Runtime.Fire;
using Sankofa.Runtime.Stage;
using UnityEditor;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// The Enkare Nairobi scenes around the stationary visitor: the cool-water stream where the forest meets the plain
    /// before the railway, the same water as a railway depot in 1899, and the river in the city today. Layout follows
    /// docs/content/enkare-nairobi/scenes.md: every touch within a seated reach on the visitor's right, away from the
    /// controls on the left, and nothing touched on the ground. People and animals are shapes of light.
    /// </summary>
    public static partial class SankofaSceneBuilder
    {
        // ---- shared ground: the stream between the forest and the plain ----

        /// <summary>The stream comes down from the forest on the left, passes in front of the visitor and runs out across the plain.</summary>
        static readonly Vector3[] StreamCourse =
        {
            new Vector3(-30f, 0, 11.5f), new Vector3(-14f, 0, 5.8f), new Vector3(-6f, 0, 3.3f), new Vector3(-2.2f, 0, 2.55f), new Vector3(0f, 0, 2.4f),
            new Vector3(2.2f, 0, 2.55f), new Vector3(5.5f, 0, 3.6f), new Vector3(11f, 0, 6.2f), new Vector3(20f, 0, 11.5f), new Vector3(40f, 0, 24f),
        };

        static float CourseDistance(Vector3 p, float stretch = 1f)
        {
            var best = float.MaxValue;
            var q = new Vector2(p.x, p.z);
            for (var i = 0; i < StreamCourse.Length - 1; i++)
            {
                var a = new Vector2(StreamCourse[i].x, StreamCourse[i].z);
                var b = new Vector2(StreamCourse[i + 1].x, StreamCourse[i + 1].z);
                var t = Mathf.Clamp01(Vector2.Dot(q - a, b - a) / (b - a).sqrMagnitude);
                best = Mathf.Min(best, Vector2.Distance(q, a + (b - a) * t));
            }
            return best / stretch;
        }

        /// <summary>The forest side (left) rises and the plain (right) falls away; the stream cuts a narrow channel.</summary>
        static float BoundaryGround(Vector3 p)
        {
            var r = new Vector2(p.x, p.z).magnitude;
            var near = SankofaMeshes.Fbm(p * 0.9f, 81, 3) * 0.035f;
            var slope = Mathf.Clamp(-p.x * 0.07f, -0.8f, 2.5f) * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.5f, 6f, r));
            var rolling = SankofaMeshes.Fbm(p * 0.12f, 83, 4) * 1.1f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(4f, 14f, r));
            // Low, rolling hills far off; taller ones would rise as hard-edged shapes against the sky beyond the fog.
            var hills = (SankofaMeshes.Fbm(p * 0.03f, 87, 3) + 0.45f) * 5f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(26f, 70f, r));
            var channel = -StreamDepth * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.6f, CourseDistance(p))));
            return near + slope + rolling + hills + channel;
        }

        /// <summary>
        /// The channel's depth, and the water's height above its bed: low enough that the ribbon's edges run under the
        /// banks, so no edge of the water is ever seen standing above the ground.
        /// </summary>
        const float StreamDepth = 0.22f, WaterAboveBed = 0.1f, StreamWidth = 0.9f;

        static List<Vector3> WaterLine(float depth)
        {
            var points = SankofaStoryMeshes.Smooth(StreamCourse, 0.35f);
            for (var i = 0; i < points.Count; i++)
            {
                var p = points[i];
                p.y = BoundaryGround(p) + depth;
                points[i] = p;
            }
            return points;
        }

        static Color BoundaryTint(Vector3 p, float trampled)
        {
            var r = new Vector2(p.x, p.z).magnitude;
            var forest = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1f, -6f, p.x));
            var dry = new Color(1.0f, 0.9f, 0.72f);
            var green = new Color(0.62f, 0.7f, 0.52f);
            var c = Color.Lerp(dry, green, forest) * (1f + SankofaMeshes.Fbm(p * 0.6f, 89, 3) * 0.3f);
            var bank = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.5f, 1.4f, CourseDistance(p)));
            c = Color.Lerp(c, new Color(0.45f, 0.4f, 0.34f), bank * 0.8f);
            c = Color.Lerp(c, new Color(1.1f, 1.0f, 0.88f), trampled * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(9f, 2.5f, new Vector2(p.x - 1f, p.z - 5.5f).magnitude)));
            return Color.Lerp(c, new Color(0.5f, 0.5f, 0.46f), Mathf.InverseLerp(20f, 50f, r));
        }

        /// <summary>The ground, the stream and the forest and plain around it, shared by both eras at the water.</summary>
        static (MeshFilter Ground, MeshFilter Water, List<MeshFilter> Static) BuildBoundary(Transform root, SankofaArtSet art, string era, int seed, bool cut, float trampled, Material sky, int starCount)
        {
            var random = new System.Random(seed);
            float R(float a, float b) => Range(random, a, b);
            var ground = Static(root, "Ground", SankofaStoryMeshes.Ground(era + " ground", BoundaryGround, p => BoundaryTint(p, trampled), p => 0f, 5f, 90f), art.Ground);
            var water = Static(root, "Stream", SankofaStoryMeshes.Ribbon(era + " stream", WaterLine(WaterAboveBed), StreamWidth, Vector3.up), art.Water);

            // Forest up the slope on the left and behind; acacias scattered over the plain on the right.
            var trunks = new List<CombineInstance>();
            var canopies = new List<CombineInstance>();
            for (var i = 0; i < 60 && trunks.Count < (cut ? 22 : 34); i++)
            {
                var degrees = R(95f, 265f);
                var at = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad)) * R(cut ? 9f : 6.5f, 24f);
                if (CourseDistance(at) < 1.8f) continue;
                at.y = BoundaryGround(at) - 0.1f;
                SankofaMeshes.Tree(seed * 10 + i, R(5f, 9f), out var trunk, out var canopy);
                var m = Matrix4x4.TRS(at, Quaternion.Euler(0, R(0, 360), 0), Vector3.one);
                trunks.Add(new CombineInstance { mesh = trunk, transform = m });
                canopies.Add(new CombineInstance { mesh = canopy, transform = m });
            }
            for (var i = 0; i < 30 && trunks.Count < (cut ? 30 : 44); i++)
            {
                var degrees = R(-75f, 70f);
                var at = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad)) * R(9f, 40f);
                if (CourseDistance(at) < 2.5f) continue;
                at.y = BoundaryGround(at) - 0.05f;
                SankofaStoryMeshes.Acacia(seed * 20 + i, R(4f, 7f), out var trunk, out var crown);
                var m = Matrix4x4.TRS(at, Quaternion.Euler(0, R(0, 360), 0), Vector3.one);
                trunks.Add(new CombineInstance { mesh = trunk, transform = m });
                canopies.Add(new CombineInstance { mesh = crown, transform = m });
            }
            var trunkFilter = Static(root, "Tree trunks", Combine(trunks, era + " trunks"), art.TreeBark);
            var canopyFilter = Static(root, "Tree canopies", Combine(canopies, era + " canopies"), art.TreeCanopy);

            // Stones along the banks.
            var stones = new List<CombineInstance>();
            for (var i = 0; i < 70; i++)
            {
                var along = StreamCourse[2 + random.Next(StreamCourse.Length - 5)];
                var at = along + new Vector3(R(-2.5f, 2.5f), 0, R(-1.2f, 1.2f));
                if (new Vector2(at.x, at.z).magnitude < 1.1f) continue;
                var size = R(0.04f, 0.22f);
                at.y = BoundaryGround(at) + size * 0.2f;
                stones.Add(Part(SankofaMeshes.Stone(seed * 30 + i, 2, 0.25f, 0.3f), at, Quaternion.Euler(R(-10, 10), R(0, 360), R(-10, 10)), new Vector3(size * 1.3f, size * 0.7f, size)));
            }
            var stoneFilter = Static(root, "Bank stones", Combine(stones, era + " bank stones"), art.Rock);

            var grassMesh = SankofaMeshes.Grass(Vector3.zero, Vector3.zero, seed + 5, cut ? 650 : 1100, 1.0f, 14f, BoundaryGround,
                p => CourseDistance(p) > 0.5f && (cut ? new Vector2(p.x - 1f, p.z - 5.5f).magnitude > 6f : true));
            grassMesh.name = era + " grass";
            var grass = Static(root, "Grass", grassMesh, art.Grass);

            Prim(root, "Sky", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 170f, sky);
            var starMesh = SankofaMeshes.Stars(Mathf.Max(1, starCount), seed + 7, 75f);
            starMesh.name = era + " stars";
            var stars = Static(root, "Stars", starMesh, art.Stars);
            // An overcast sky has no stars to show.
            if (starCount <= 0) stars.gameObject.SetActive(false);
            return (ground, water, new List<MeshFilter> { trunkFilter, canopyFilter, stoneFilter, grass, stars });
        }

        // ---- before the railway ----

        static readonly Vector3 TradersFireAt = new Vector3(-0.5f, 0, 1.05f);

        static (StageObject[] Objects, StoryAmbience Sound) BuildWater1850s(Transform root, SankofaArtSet art)
        {
            var (ground, water, statics) = BuildBoundary(root, art, "1850s", 111, false, 0f, art.SkyDusk, 450);
            var trunks = statics[0];
            var canopies = statics[1];
            var stones = statics[2];

            // The spring over the rock on the right, and the trading rock in front, both brought up within reach.
            var rocks = new List<CombineInstance>();
            var springRock = new Vector3(0.52f, BoundaryGround(new Vector3(0.52f, 0, 0.3f)) + 0.24f, 0.3f);
            rocks.Add(Part(SankofaMeshes.Stone(1701, 3, 0.16f, 0.25f), springRock, Quaternion.Euler(0, 25, 0), new Vector3(0.3f, 0.34f, 0.26f)));
            var tradeRock = new Vector3(0.18f, BoundaryGround(new Vector3(0.18f, 0, 0.5f)) + 0.24f, 0.5f);
            rocks.Add(Part(SankofaMeshes.Stone(1702, 3, 0.1f, 0.25f), tradeRock, Quaternion.Euler(0, -15, 0), new Vector3(0.22f, 0.26f, 0.2f)));
            foreach (var (angle, seed) in new[] { (0f, 1703), (120f, 1704), (240f, 1705) })
            {
                var at = TradersFireAt + Quaternion.Euler(0, angle, 0) * Vector3.forward * 0.17f;
                at.y = BoundaryGround(at) + 0.04f;
                rocks.Add(Part(SankofaMeshes.Stone(seed, 2, 0.2f, 0.3f), at, Quaternion.Euler(0, angle, 0), new Vector3(0.09f, 0.07f, 0.08f)));
            }
            var rockFilter = Static(root, "Rocks", Combine(rocks, "1850s rocks"), art.Rock);

            var springTop = springRock.y + 0.3f;
            var spillTop = SankofaStoryMeshes.Ribbon("Spring over the rock", new List<Vector3>
            {
                new Vector3(0.66f, springTop - 0.03f, 0.38f), new Vector3(0.52f, springTop + 0.005f, 0.33f), new Vector3(0.36f, springTop - 0.01f, 0.3f),
            }, 0.07f, Vector3.up);
            var spillFall = SankofaStoryMeshes.Ribbon("Spring falling", new List<Vector3>
            {
                new Vector3(0.34f, springTop - 0.015f, 0.3f), new Vector3(0.3f, springTop - 0.2f, 0.31f), new Vector3(0.27f, 0.25f, 0.33f), new Vector3(0.26f, ground.sharedMesh != null ? BoundaryGround(new Vector3(0.26f, 0, 0.34f)) + 0.02f : 0.02f, 0.34f),
            }, 0.07f, new Vector3(-1f, 0, -0.35f).normalized);
            var rivulet = SankofaStoryMeshes.Ribbon("Spring to the stream", WaterPath(new Vector3(0.26f, 0, 0.36f), new Vector3(0.5f, 0, 2.15f), 0.02f), 0.08f, Vector3.up);

            // Long grass and reeds hide the rivulet's banks; a few logs of firewood beside the traders' fire.
            var wood = new List<CombineInstance>();
            for (var i = 0; i < 4; i++)
            {
                var at = TradersFireAt + new Vector3(0.28f + i * 0.04f, 0, -0.05f + i * 0.06f);
                at.y = BoundaryGround(at) + 0.03f;
                wood.Add(Part(SankofaMeshes.Log(1720 + i, 0.4f, 0.025f, 0.1f, 0.01f, 8, 4), at, Quaternion.Euler(90, 70 + i * 13, 0), Vector3.one));
            }
            var woodFilter = Static(root, "Firewood", Combine(wood, "1850s firewood"), art.SeatWood);

            var light = TradersFireAt + new Vector3(0, BoundaryGround(TradersFireAt) + 0.25f, 0);
            Bake(root, new[] { ground, rockFilter, trunks, canopies, stones, woodFilter }, light);
            var springTopFilter = Static(root, "Spring", spillTop, art.Spill);
            var rivuletFilter = Static(root, "Rivulet", rivulet, art.Water);
            SaveMeshes(new[] { ground, water, rockFilter, trunks, canopies, stones, woodFilter, springTopFilter, rivuletFilter, statics[3], statics[4] });

            // The falling water is the thing the visitor touches.
            var spillRoot = Child(root, "Water spill");
            var fall = Static(spillRoot, "Body", spillFall, art.Spill);
            SaveMeshes(new[] { fall });
            var spill = Staged(spillRoot.gameObject, "water.spill", "story.water.1850s", true, new[] { fall.GetComponent<Renderer>(), springTopFilter.GetComponent<Renderer>() });
            Set(spill, "touchPoint", Marker(spillRoot, "Touch point", new Vector3(0.33f, springTop - 0.03f, 0.29f)));
            Set(spill, "touchRadius", 0.1f);
            Set(spill, "attentionRadius", 0.15f);

            var fire = BuildTradersFire(root, art);
            var hide = BuildHide(root, art, tradeRock + Vector3.up * 0.25f);
            // Wambui waits back from the traders' fire, so her way to the hide does not pass through it.
            var groundAt = BoundaryGround(new Vector3(-0.75f, 0, 1.6f));
            var wambui = BuildFigure(root, art, "figure.wambui", "story.water.1850s", new Vector3(-0.75f, groundAt, 1.6f), 1.6f, FromTheForest, KikuyuLine);
            var naserian = BuildFigure(root, art, "figure.naserian", "story.water.1850s", new Vector3(0.85f, BoundaryGround(new Vector3(0.85f, 0, 1.3f)), 1.3f), 1.66f, FromThePlain, MaasaiLine);
            // The ewe waits on the near bank, seen between the two women while they trade.
            var ewe = BuildEwe(root, art, new Vector3(1.3f, BoundaryGround(new Vector3(1.3f, 0, 1.9f)), 1.9f), EweFromThePlain);
            // For the trade the visitor and the two women sit around the hide: Wambui across it, Naserian to the right.
            Approaches(hide, true, new Vector2(0.0f, 1.12f), new Vector2(0.86f, 0.92f));
            var flour = BuildGourd(root, art, "gourd.flour", "story.water.1850s", new Vector3(-0.75f, groundAt + 0.98f, 1.48f), 0.23f, 0.095f, 0.03f, 1f, false);
            var milk = BuildGourd(root, art, "gourd.milk", "story.water.1850s", new Vector3(0.66f, BoundaryGround(new Vector3(0.66f, 0, 1.2f)), 1.2f), 0.34f, 0.075f, 0.026f, 0.55f, false);
            var bead = BuildBead(root, art, "bead", "story.water.1850s", new Vector3(0.84f, 1.36f, 1.25f));
            Carries(wambui, flour, "Hands", Vector3.zero);
            Carries(naserian, bead, "Neck", Vector3.zero);
            Owns(naserian, milk);

            // The ewe bleats now and then once she has come to the water.
            var bleat = ewe.gameObject.AddComponent<ObjectSound>();
            Set(bleat, "subject", ewe);
            var bleatSource = Source(ewe.gameObject, 0.9f, 1.5f);
            bleatSource.loop = false;
            Set(bleat, "source", bleatSource);
            SetArray(bleat, "clips", new Object[] { Recording("bleat-1"), Recording("bleat-2"), Recording("bleat-3"), Recording("bleat-4") });
            Set(bleat, "volume", 0.35f);
            var sound = BuildStoryAmbience(root, new (string, Vector3, float, bool)[]
            {
                ("brook", new Vector3(0.1f, 0f, 2.4f), 0.22f, false),
                ("brook", new Vector3(0.3f, 0.5f, 0.32f), 0.06f, false),
                ("crickets", new Vector3(-5f, 0.2f, 4f), 0.1f, false),
                ("crickets", new Vector3(6f, 0.2f, -3f), 0.08f, false),
                ("wind-grass", new Vector3(8f, 2f, 3f), 0.12f, false),
                ("maasai-nature", new Vector3(-5f, 4f, 6f), 0.12f, true),
                ("bells", new Vector3(9f, 1f, 12f), 0.1f, true),
            }, bleat);
            return (new[] { fire, spill, wambui, flour, naserian, ewe, milk, hide, bead }, sound);
        }

        static List<Vector3> WaterPath(Vector3 from, Vector3 to, float depth)
        {
            var points = new List<Vector3>();
            for (var i = 0; i <= 10; i++)
            {
                var p = Vector3.Lerp(from, to, i / 10f) + Vector3.right * Mathf.Sin(i * 0.9f) * 0.03f;
                p.y = BoundaryGround(p) + depth;
                points.Add(p);
            }
            return points;
        }

        static void Bake(Transform root, IList<MeshFilter> filters, Vector3 light)
        {
            var moon = SceneMoon(root.name);
            SankofaBake.Bake(filters, filters, root.TransformPoint(light) - Vector3.up * 0.36f, root.TransformDirection(moon), new HashSet<MeshFilter>(filters));
        }

        static Vector3 SceneMoon(string sceneRoot) =>
            sceneRoot.EndsWith("1850s") ? WaterMoon1850s : sceneRoot.EndsWith("1899") ? WaterMoon1899 : WaterMoonToday;

        /// <summary>The traders' fire: coals and two small flames between the three stones, the ember's continuation.</summary>
        static StageObject BuildTradersFire(Transform root, SankofaArtSet art)
        {
            var fireRoot = Child(root, "Traders' fire");
            fireRoot.localPosition = TradersFireAt + Vector3.up * (BoundaryGround(TradersFireAt) + 0.02f);
            var coals = new List<CombineInstance>();
            var random = new System.Random(1730);
            for (var i = 0; i < 9; i++)
            {
                var at = new Vector3(Range(random, -0.07f, 0.07f), 0.01f, Range(random, -0.07f, 0.07f));
                var size = Range(random, 0.02f, 0.035f);
                var mesh = SankofaMeshes.Stone(1731 + i, 1, 0.3f, 0.3f);
                mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, Range(random, 0.8f, 1.3f)), mesh.vertexCount).ToList());
                coals.Add(Part(mesh, at, Quaternion.Euler(Range(random, 0, 360), Range(random, 0, 360), 0), new Vector3(size * 1.3f, size * 0.7f, size)));
            }
            var coalFilter = Static(fireRoot, "Coals", Combine(coals, "1850s fire coals"), art.Embers);
            SaveMeshes(new[] { coalFilter });
            // A small cooking fire's crackle, quieter than the hearth's.
            var crackleAt = Child(fireRoot, "Crackle");
            crackleAt.localPosition = new Vector3(0, 0.08f, 0);
            var crackle = crackleAt.gameObject.AddComponent<FireCrackle>();
            var crackleSource = Source(crackleAt.gameObject, 0.9f, 0.6f);
            crackleSource.clip = Recording("small-fire");
            Set(crackle, "sound", crackleSource);
            Set(crackle, "volume", 0.35f);
            for (var i = 0; i < 2; i++)
            {
                var layer = Child(fireRoot, "Flame " + i);
                layer.localPosition = new Vector3(i == 0 ? 0 : 0.03f, 0, i == 0 ? 0 : -0.02f);
                layer.gameObject.AddComponent<Billboard>();
                Prim(layer, "Card", PrimitiveType.Quad, new Vector3(0, 0.13f, 0), new Vector3(0.16f, 0.26f, 1f) * (i == 0 ? 1f : 0.7f), art.Flame);
            }
            var o = Staged(fireRoot.gameObject, "fire.1850s", "story.water.1850s", false, new[] { coalFilter.GetComponent<Renderer>() });
            Set(o, "glow", GlowCard(fireRoot, art, 0.9f));
            Set(o, "glowIntensity", 0.9f);
            Set(o, "emitsLight", true);
            Set(o, "lightColor", new Color(1f, 0.6f, 0.3f));
            Set(o, "lightIntensity", 1.7f);
            return o;
        }

        static StageObject BuildHide(Transform root, SankofaArtSet art, Vector3 top)
        {
            var hideRoot = Child(root, "Trading hide");
            hideRoot.localPosition = top;
            var mesh = SankofaMeshes.Stone(1740, 3, 0.12f, 0.2f);
            mesh.name = "Trading hide";
            // Draped: the edge falls away over the rock's sides, a few centimetres, unevenly.
            var vertices = mesh.vertices;
            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                var r = new Vector2(v.x, v.z).magnitude;
                var droop = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.05f, r)) * (1.6f + 0.6f * Mathf.Sin(Mathf.Atan2(v.z, v.x) * 3f));
                vertices[i] = new Vector3(v.x * 1.05f, v.y - droop, v.z * 1.05f);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), mesh.vertexCount).ToList());
            var body = Static(hideRoot, "Body", mesh, art.Hide);
            body.transform.localScale = new Vector3(0.21f, 0.022f, 0.18f);
            body.transform.localRotation = Quaternion.Euler(0, 20, 0);
            SaveMeshes(new[] { body });
            var o = Staged(hideRoot.gameObject, "hide", "story.water.1850s", true, new[] { body.GetComponent<Renderer>() });
            Set(o, "touchPoint", Marker(hideRoot, "Touch point", new Vector3(0, 0.03f, -0.02f)));
            Set(o, "touchRadius", 0.11f);
            Set(o, "attentionRadius", 0.16f);
            Set(o, "placePoint", Marker(hideRoot, "Place point", new Vector3(0.03f, 0.03f, 0.02f)));
            return o;
        }

        // One ember colour per family line, so the granddaughters carry their grandmothers' light across the eras.
        static readonly Color KikuyuLine = new Color(1f, 0.64f, 0.26f, 1f);
        static readonly Color MaasaiLine = new Color(1f, 0.42f, 0.2f, 1f);

        /// <summary>
        /// A person of the story: the faceless skinned mannequin, animated (walk, idle, kneel, give, talk) and drawn
        /// only as a shape of firelight in her family's colour. What she carries follows her right hand or her neck.
        /// </summary>
        static StageObject BuildFigure(Transform root, SankofaArtSet art, string id, string scene, Vector3 at, float height, Vector2[] path, Color tint)
        {
            var figureRoot = Child(root, id);
            figureRoot.localPosition = at;
            figureRoot.localRotation = Quaternion.LookRotation(new Vector3(-at.x, 0, -at.z), Vector3.up);
            var (body, animator, hand, neckBone, cloth) = SkinnedBody(figureRoot, art, height);
            var o = StageLight(figureRoot, id, scene, body, 0.5f);
            var hands = Marker(figureRoot, "Hands", new Vector3(0, 0.53f * height, 0.2f));
            var neck = Marker(figureRoot, "Neck", new Vector3(0, 0.845f * height, 0.08f));
            var motion = Walks(figureRoot, body, height, path, 0f, 0f, true, hands, neck);
            Set(motion, "animator", animator);
            SetArray(motion, "anchorBones", new Object[] { hand, neckBone });
            Set(motion, "tint", tint);
            SetArray(motion, "cloth", cloth.Cast<Object>().ToArray());
            return o;
        }

        static StageObject BuildEwe(Transform root, SankofaArtSet art, Vector3 at, Vector2[] path)
        {
            var eweRoot = Child(root, "figure.ewe");
            eweRoot.localPosition = at;
            eweRoot.localRotation = Quaternion.Euler(0, -105, 0);
            var body = Static(eweRoot, "Body", SankofaStoryMeshes.Ewe("Figure ewe", 1810), art.EmberFigure);
            SaveMeshes(new[] { body });
            var o = StageLight(eweRoot, "figure.ewe", "story.water.1850s", body.GetComponent<Renderer>(), 0.35f);
            // The ewe follows a little behind Naserian, with a quicker, smaller step.
            Walks(eweRoot, body.GetComponent<Renderer>(), 0.7f, path, 1.2f, 0.01f, false);
            return o;
        }

        /// <summary>
        /// The figure walks in along a path over the ground (points given on the plan, heights from the terrain) when
        /// it is revealed, and back along it when it leaves.
        /// </summary>
        static FigureMotion Walks(Transform figure, Renderer body, float height, Vector2[] path, float delay, float bob, bool arms, params Transform[] anchors)
        {
            var motion = figure.gameObject.AddComponent<FigureMotion>();
            var ground = figure.parent.name.EndsWith("today") ? (System.Func<Vector3, float>)RiverGround : BoundaryGround;
            var points = path.Select(p => new Vector3(p.x, ground(new Vector3(p.x, 0, p.y)), p.y)).ToArray();
            Set(motion, "body", body);
            Set(motion, "height", height);
            Set(motion, "delay", delay);
            Set(motion, "bob", bob);
            Set(motion, "hasArms", arms);
            SetArray(motion, "anchors", anchors.Cast<Object>().ToArray());
            var so = new SerializedObject(motion);
            var list = so.FindProperty("entryPath");
            list.arraySize = points.Length;
            for (var i = 0; i < points.Length; i++) list.GetArrayElementAtIndex(i).vector3Value = points[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return motion;
        }

        /// <summary>
        /// Something a person of the story carries: it hangs from one of her anchors, walks with her, and is offered,
        /// shown or set down from her hands with a gesture.
        /// </summary>
        static void Carries(StageObject person, StageObject item, string anchor, Vector3 offset)
        {
            item.transform.SetParent(person.transform.Find(anchor), false);
            item.transform.localPosition = offset;
            Owns(person, item);
        }

        /// <summary>Something that belongs to a person of the story without being in her hands: she shows it when the story turns to it.</summary>
        static void Owns(StageObject person, StageObject item) => Set(item, "holder", person.GetComponent<FigureMotion>());

        /// <summary>Places where a figure stands or kneels to use an object, at ground level, in the scene's plan.</summary>
        static void Approaches(StageObject o, bool kneel, params Vector2[] places)
        {
            var points = places.Select((p, i) => Marker(o.transform, "Approach " + i, Vector3.zero)).ToArray();
            for (var i = 0; i < places.Length; i++)
            {
                var flat = new Vector3(places[i].x, 0, places[i].y);
                points[i].localPosition = Vector3.zero;
                points[i].position = o.transform.parent.TransformPoint(new Vector3(flat.x, BoundaryGround(flat), flat.z));
            }
            SetArray(o, "approachPoints", points.Cast<Object>().ToArray());
            Set(o, "approachKneels", kneel);
        }

        // Paths on the plan (x, z): the Kikuyu women come down from the forest on the left along the near bank, the
        // Maasai women in from the plain on the right; each ends just short of her own place.
        static readonly Vector2[] FromTheForest = { new Vector2(-6.2f, 1.75f), new Vector2(-4f, 1.7f), new Vector2(-2.2f, 1.62f), new Vector2(-1.4f, 1.58f) };
        static readonly Vector2[] FromThePlain = { new Vector2(6.4f, 1.9f), new Vector2(4.2f, 1.6f), new Vector2(2.4f, 1.4f), new Vector2(1.4f, 1.32f) };
        static readonly Vector2[] EweFromThePlain = { new Vector2(6.9f, 2.2f), new Vector2(4.6f, 2.0f), new Vector2(2.9f, 1.95f), new Vector2(1.9f, 1.92f) };

        /// <summary>A shape of light fades in where it stands instead of growing; its brightness is its visibility.</summary>
        static StageObject StageLight(Transform root, string id, string scene, Renderer body, float attention)
        {
            var o = Staged(root.gameObject, id, scene, false, new[] { body });
            Set(o, "scaleWithVisibility", false);
            Set(o, "glow", body);
            Set(o, "glowIntensity", 1f);
            Set(o, "attentionRadius", attention);
            return o;
        }

        static StageObject BuildGourd(Transform root, SankofaArtSet art, string id, string scene, Vector3 at, float height, float bulb, float neck, float tone, bool visible)
        {
            var gourdRoot = Child(root, id);
            gourdRoot.localPosition = at;
            var mesh = SankofaStoryMeshes.Lathe("Gourd " + id, SankofaStoryMeshes.GourdProfile(height, bulb, neck), 24, (a, y) => new Color(tone, tone * 0.95f, tone * 0.9f), id.Length);
            mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), mesh.vertexCount).ToList());
            var body = Static(gourdRoot, "Body", mesh, art.Gourd);
            body.transform.localPosition = new Vector3(0, -height * 0.4f, 0);
            SaveMeshes(new[] { body });
            var o = Staged(gourdRoot.gameObject, id, scene, visible, new[] { body.GetComponent<Renderer>() });
            Set(o, "attentionRadius", 0.12f);
            return o;
        }

        static StageObject BuildBead(Transform root, SankofaArtSet art, string id, string scene, Vector3 at)
        {
            var beadRoot = Child(root, id);
            beadRoot.localPosition = at;
            var mesh = SankofaMeshes.Stone(1750, 2, 0.02f, 10f);
            mesh.name = "Bead " + id;
            mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), mesh.vertexCount).ToList());
            var body = Static(beadRoot, "Body", mesh, art.Bead);
            body.transform.localScale = new Vector3(0.013f, 0.011f, 0.013f);
            SaveMeshes(new[] { body });
            var o = Staged(beadRoot.gameObject, id, scene, false, new[] { body.GetComponent<Renderer>() });
            // A little of the fire's light lingers on what it gives, so a small thing reads at arm's length; what
            // waits on the hearth stone from a kept visit glows a little more, so it is found again.
            var kept = id.EndsWith(".kept");
            if (kept) body.transform.localScale *= 1.4f;
            Set(o, "glow", GlowCard(beadRoot, art, kept ? 0.16f : 0.09f));
            Set(o, "glowIntensity", kept ? 0.85f : 0.4f);
            Set(o, "attentionRadius", 0.05f);
            return o;
        }

        static StageObject BuildSeed(Transform root, SankofaArtSet art, string id, string scene, Vector3 at)
        {
            var seedRoot = Child(root, id);
            seedRoot.localPosition = at;
            var mesh = SankofaMeshes.Stone(1760, 2, 0.28f, 0.4f);
            mesh.name = "Seed bundle " + id;
            mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), mesh.vertexCount).ToList());
            var body = Static(seedRoot, "Body", mesh, art.Leaf);
            body.transform.localScale = new Vector3(0.03f, 0.018f, 0.024f);
            SaveMeshes(new[] { body });
            var o = Staged(seedRoot.gameObject, id, scene, false, new[] { body.GetComponent<Renderer>() });
            Set(o, "glow", GlowCard(seedRoot, art, 0.1f));
            Set(o, "glowIntensity", 0.35f);
            Set(o, "attentionRadius", 0.05f);
            return o;
        }

        static void ConfigureWater1850s(SerializedProperty e, Transform root)
        {
            // Dusk: violet sky light, warm afterglow over the plain, the moon rising over the forest.
            Scene(e, "story.water.1850s", false, root, new Color(0.11f, 0.07f, 0.1f),
                new Color(0.1f, 0.085f, 0.13f), new Color(0.04f, 0.03f, 0.025f), 9f, 60f);
            e.FindPropertyRelative("TowardMoon").vector3Value = WaterMoon1850s;
            e.FindPropertyRelative("Moon").colorValue = new Color(0.08f, 0.075f, 0.11f);
            e.FindPropertyRelative("MistColor").colorValue = new Color(0.13f, 0.085f, 0.11f);
            e.FindPropertyRelative("Mist").vector4Value = new Vector4(0.5f, 0.0f, 1.2f, 7f);
            e.FindPropertyRelative("Grade").vector4Value = new Vector4(1.25f, 0.95f, 0.7f, 1.0f);
        }

        // ---- when the railway came ----

        static readonly Vector3 CampLanternAt = new Vector3(-0.42f, 0.98f, 0.85f);

        static (StageObject[] Objects, StoryAmbience Sound) BuildWater1899(Transform root, SankofaArtSet art)
        {
            var (ground, water, statics) = BuildBoundary(root, art, "1899", 113, true, 1f, art.SkyMorning, 0);
            var trunks = statics[0];
            var canopies = statics[1];
            var stones = statics[2];
            var random = new System.Random(41);
            float R(float a, float b) => Range(random, a, b);

            // The camp across the stream: bell tents, stores, a cart; the new line on its embankment beyond.
            var canvas = new List<CombineInstance>();
            foreach (var at in new[] { new Vector3(-2.6f, 0, 5f), new Vector3(0.9f, 0, 6.2f), new Vector3(4.2f, 0, 5.2f), new Vector3(6.6f, 0, 8.4f), new Vector3(-0.8f, 0, 9.5f) })
            {
                var p = at;
                p.y = BoundaryGround(at) - 0.02f;
                var tent = SankofaStoryMeshes.Lathe("Tent", SankofaStoryMeshes.TentProfile(R(1.5f, 1.9f), R(2.6f, 3f)), 28, (a, y) => new Color(1, 1, 1) * R(0.85f, 1f), (int)(at.x * 10));
                canvas.Add(Part(tent, p, Quaternion.Euler(0, R(0, 360), 0), Vector3.one));
            }
            var tentFilter = Static(root, "Tents", Combine(canvas, "1899 tents"), art.Canvas);

            var timber = new List<CombineInstance>();
            for (var i = 0; i < 9; i++)
            {
                var at = new Vector3(2.4f + R(-1.2f, 1.2f), 0, 3.8f + R(-0.6f, 0.6f));
                at.y = BoundaryGround(at) + 0.2f;
                timber.Add(Part(SankofaStoryMeshes.Box("Crate", new Vector3(R(0.4f, 0.7f), 0.4f, R(0.4f, 0.6f))), at, Quaternion.Euler(0, R(-20, 20), 0), Vector3.one));
            }
            // The rail crib within reach on the right: sleepers laid crosswise to lap height.
            var crib = new Vector3(0.38f, BoundaryGround(new Vector3(0.38f, 0, 0.5f)), 0.5f);
            for (var layer = 0; layer < 4; layer++)
                for (var k = 0; k < 2; k++)
                {
                    var along = layer % 2 == 0;
                    var offset = (k == 0 ? -0.2f : 0.2f);
                    var at = crib + new Vector3(along ? 0 : offset, 0.06f + layer * 0.12f, along ? offset : 0);
                    timber.Add(Part(SankofaStoryMeshes.Box("Sleeper", new Vector3(along ? 0.9f : 0.2f, 0.12f, along ? 0.2f : 0.9f)), at, Quaternion.identity, Vector3.one));
                }
            // The embankment running across the plain beyond the camp.
            timber.Add(Part(SankofaStoryMeshes.Box("Embankment", new Vector3(90f, 1.2f, 4f)), new Vector3(0, BoundaryGround(new Vector3(0, 0, 14f)) - 0.1f, 14f), Quaternion.Euler(0, -8, 0), Vector3.one));
            var timberFilter = Static(root, "Camp timber", Combine(timber, "1899 timber"), art.Workbench);
            Prim(root, "Lantern pole", PrimitiveType.Cylinder, new Vector3(CampLanternAt.x, 0.55f, CampLanternAt.z + 0.05f), new Vector3(0.05f, 0.55f, 0.05f), art.Workbench);

            // Tree stumps where the forest edge was cut for the camp.
            var stumps = new List<CombineInstance>();
            for (var i = 0; i < 14; i++)
            {
                var degrees = R(100f, 200f);
                var at = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad)) * R(3f, 8f);
                if (CourseDistance(at) < 1.2f) continue;
                at.y = BoundaryGround(at) + 0.12f;
                stumps.Add(Part(SankofaMeshes.Log(1900 + i, R(0.2f, 0.4f), R(0.1f, 0.18f), 0.05f, 0.01f, 12, 3), at, Quaternion.identity, Vector3.one));
            }
            var stumpFilter = Static(root, "Stumps", Combine(stumps, "1899 stumps"), art.SeatWood);

            Bake(root, new[] { ground, trunks, canopies, stones, tentFilter, timberFilter, stumpFilter }, CampLanternAt);
            SaveMeshes(new[] { ground, water, trunks, canopies, stones, statics[3], statics[4], tentFilter, timberFilter, stumpFilter });

            var rails = BuildRails(root, art, crib + Vector3.up * 0.48f);
            var lantern = BuildLantern(root, art, "lantern.1899", "story.water.1899", CampLanternAt);
            // Njeri stands clear of the camp lantern; Naisula on the right, as their grandmothers did.
            var njeriAt = new Vector3(-0.95f, BoundaryGround(new Vector3(-0.95f, 0, 1.5f)), 1.5f);
            var naisulaAt = new Vector3(0.8f, BoundaryGround(new Vector3(0.8f, 0, 1.32f)), 1.32f);
            var njeri = BuildFigure(root, art, "figure.njeri", "story.water.1899", njeriAt, 1.58f, FromTheForest, KikuyuLine);
            var naisula = BuildFigure(root, art, "figure.naisula", "story.water.1899", naisulaAt, 1.64f, FromThePlain, MaasaiLine);
            // Njeri comes to stand at Naisula's side to pour what flour she has into her gourd.
            Approaches(naisula, false, new Vector2(0.17f, 1.45f));
            var empty = BuildGourd(root, art, "gourd.empty", "story.water.1899", njeriAt + new Vector3(0.18f, 0.12f, -0.08f), 0.24f, 0.1f, 0.03f, 0.9f, false);
            var old = BuildGourd(root, art, "gourd.old", "story.water.1899", naisulaAt + new Vector3(-0.2f, 0.14f, -0.08f), 0.32f, 0.07f, 0.025f, 0.45f, false);
            var seed = BuildSeed(root, art, "seed", "story.water.1899", njeriAt + new Vector3(0.12f, 0.85f, -0.1f));
            Carries(njeri, empty, "Hands", Vector3.zero);
            Carries(njeri, seed, "Hands", new Vector3(0.09f, 0.02f, 0.06f));
            Carries(naisula, old, "Hands", Vector3.zero);

            // Hammering somewhere along the line, now near, now further off. No recorded voices: the workers'
            // languages are to be checked before anyone is heard speaking. No cattle bells in a year without cattle.
            var hammers = new[] { (new Vector3(5f, 1f, 12f), 0.22f, 2f), (new Vector3(-9f, 1f, 13f), 0.12f, 6f) }.Select((h, i) =>
            {
                var at = Child(root, "Hammering " + i);
                at.localPosition = h.Item1;
                var hammer = at.gameObject.AddComponent<ObjectSound>();
                var hammerSource = Source(at.gameObject, 0.6f, 4f);
                hammerSource.loop = false;
                Set(hammer, "source", hammerSource);
                SetArray(hammer, "clips", new Object[] { Recording("spikes") });
                Set(hammer, "volume", h.Item2);
                Set(hammer, "interval", new Vector2(9f, 18f));
                Set(hammer, "firstAfter", h.Item3);
                return hammer;
            }).ToArray();
            var sound = BuildStoryAmbience(root, new (string, Vector3, float, bool)[]
            {
                ("brook", new Vector3(0.1f, 0f, 2.4f), 0.14f, false),
                ("wind-grass", new Vector3(-6f, 2f, 2f), 0.15f, false),
                ("tent", new Vector3(1f, 1.5f, 6.2f), 0.1f, false),
            }, hammers);
            return (new[] { lantern, rails, njeri, empty, naisula, old, seed }, sound);
        }

        static StageObject BuildRails(Transform root, SankofaArtSet art, Vector3 top)
        {
            var railRoot = Child(root, "Rails");
            railRoot.localPosition = top;
            var steel = new List<CombineInstance>();
            for (var i = 0; i < 4; i++)
            {
                var z = (i - 1.5f) * 0.11f;
                steel.Add(Part(SankofaStoryMeshes.Box("Rail foot", new Vector3(1.4f, 0.012f, 0.09f)), new Vector3(0, 0.006f, z), Quaternion.identity, Vector3.one));
                steel.Add(Part(SankofaStoryMeshes.Box("Rail web", new Vector3(1.4f, 0.06f, 0.016f)), new Vector3(0, 0.042f, z), Quaternion.identity, Vector3.one));
                steel.Add(Part(SankofaStoryMeshes.Box("Rail head", new Vector3(1.4f, 0.022f, 0.05f)), new Vector3(0, 0.08f, z), Quaternion.identity, Vector3.one));
            }
            var body = Static(railRoot, "Body", Combine(steel, "1899 rails"), art.Steel);
            SaveMeshes(new[] { body });
            var o = Staged(railRoot.gameObject, "rails", "story.water.1899", true, new[] { body.GetComponent<Renderer>() });
            Set(o, "touchPoint", Marker(railRoot, "Touch point", new Vector3(-0.08f, 0.1f, -0.1f)));
            Set(o, "touchRadius", 0.11f);
            Set(o, "attentionRadius", 0.25f);
            return o;
        }

        static void ConfigureWater1899(SerializedProperty e, Transform root)
        {
            // A grey dawn under cloud: soft light from above, little colour, the lantern still burning.
            Scene(e, "story.water.1899", false, root, new Color(0.22f, 0.22f, 0.24f),
                new Color(0.16f, 0.165f, 0.18f), new Color(0.06f, 0.055f, 0.05f), 10f, 70f);
            e.FindPropertyRelative("TowardMoon").vector3Value = WaterMoon1899;
            e.FindPropertyRelative("Moon").colorValue = new Color(0.1f, 0.1f, 0.105f);
            e.FindPropertyRelative("MistColor").colorValue = new Color(0.22f, 0.22f, 0.235f);
            e.FindPropertyRelative("Mist").vector4Value = new Vector4(0.45f, 0.1f, 1.0f, 8f);
            e.FindPropertyRelative("Grade").vector4Value = new Vector4(1.05f, 0.7f, 0.5f, 0.3f);
        }

        // ---- the same water, today ----

        static readonly Vector3 JikoAt = new Vector3(-0.45f, 0.46f, 0.85f);

        static float RiverGround(Vector3 p)
        {
            var r = new Vector2(p.x, p.z).magnitude;
            var near = SankofaMeshes.Fbm(p * 0.9f, 91, 3) * 0.02f;
            var channel = -0.6f * (1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.8f, 1.7f, CourseDistance(p))));
            var far = (SankofaMeshes.Fbm(p * 0.03f, 93, 3) + 0.4f) * 6f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(60f, 140f, r));
            return near + channel + far;
        }

        static (StageObject[] Objects, StoryAmbience Sound) BuildWaterToday(Transform root, SankofaArtSet art)
        {
            var random = new System.Random(57);
            float R(float a, float b) => Range(random, a, b);
            var ground = Static(root, "Ground", SankofaStoryMeshes.Ground("Today ground", RiverGround, p =>
            {
                var c = new Color(0.62f, 0.56f, 0.5f) * (1f + SankofaMeshes.Fbm(p * 0.8f, 95, 3) * 0.35f);
                var bank = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.2f, 2.4f, CourseDistance(p)));
                return Color.Lerp(c, new Color(0.38f, 0.36f, 0.33f), bank);
            }, p => 0f, 5f, 160f), art.Ground);
            var riverPoints = SankofaStoryMeshes.Smooth(StreamCourse, 0.4f).Select(p => new Vector3(p.x, RiverGround(p) + 0.35f, p.z)).ToList();
            // Wide enough that its edges run under the banks.
            var river = Static(root, "River", SankofaStoryMeshes.Ribbon("Today river", riverPoints, 2.9f, Vector3.up), art.River);

            // The city beyond the river's green strip: low blocks nearer, towers only far off, lit windows, and
            // streetlights along the far bank.
            var blocks = new List<CombineInstance>();
            for (var i = 0; i < 160 && blocks.Count < 110; i++)
            {
                var degrees = R(-15f, 195f);
                var distance = R(50f, 230f);
                var at = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad)) * distance;
                if (at.z < 30f) continue;
                var tower = distance > 130f && random.NextDouble() < 0.25;
                var height = tower ? R(40f, 80f) : R(9f, 22f) + distance * R(0f, 0.08f);
                var size = new Vector3(R(10f, 26f), height, R(10f, 20f));
                at.y = RiverGround(at) + height * 0.5f - 0.5f;
                var turn = Quaternion.Euler(0, R(-25, 25), 0);
                blocks.Add(Part(SankofaStoryMeshes.Box("Block", size), at, turn, Vector3.one));
                // Rooflines: an upper storey set back, a water tank or a stair head, so the skyline is not all flat boxes.
                var roof = at + Vector3.up * height * 0.5f;
                var pick = random.NextDouble();
                if (pick < 0.35)
                {
                    var upper = new Vector3(size.x * R(0.45f, 0.7f), R(3f, 7f), size.z * R(0.5f, 0.75f));
                    blocks.Add(Part(SankofaStoryMeshes.Box("Upper storey", upper), roof + turn * new Vector3(R(-2f, 2f), upper.y * 0.5f, R(-2f, 2f)), turn, Vector3.one));
                }
                else if (pick < 0.75)
                {
                    var tank = new Vector3(R(1.6f, 2.6f), R(1.4f, 2.2f), R(1.6f, 2.6f));
                    blocks.Add(Part(SankofaStoryMeshes.Box("Water tank", tank), roof + turn * new Vector3(size.x * R(-0.3f, 0.3f), tank.y * 0.5f + 0.6f, size.z * R(-0.3f, 0.3f)), turn, Vector3.one));
                    blocks.Add(Part(SankofaStoryMeshes.Box("Stair head", new Vector3(2.4f, 2.6f, 2.4f)), roof + turn * new Vector3(size.x * R(-0.35f, 0.35f), 1.3f, size.z * R(-0.35f, 0.35f)), turn, Vector3.one));
                }
            }
            var cityFilter = Static(root, "City", Combine(blocks, "Today city"), art.City);

            var garden = new List<CombineInstance>();
            // The raised bed within reach, and the gate of the community garden behind it on the right.
            garden.Add(Part(SankofaStoryMeshes.Box("Raised bed", new Vector3(0.75f, 0.5f, 0.42f)), new Vector3(0.44f, 0.25f, 0.52f), Quaternion.Euler(0, 8, 0), Vector3.one));
            garden.Add(Part(SankofaStoryMeshes.Box("Stall table", new Vector3(0.6f, 0.04f, 0.4f)), new Vector3(JikoAt.x, JikoAt.y - 0.02f, JikoAt.z), Quaternion.identity, Vector3.one));
            foreach (var (x, z) in new[] { (-0.25f, -0.15f), (0.25f, -0.15f), (-0.25f, 0.15f), (0.25f, 0.15f) })
                garden.Add(Part(SankofaStoryMeshes.Box("Stall leg", new Vector3(0.04f, JikoAt.y - 0.04f, 0.04f)), new Vector3(JikoAt.x + x, (JikoAt.y - 0.04f) * 0.5f, JikoAt.z + z), Quaternion.identity, Vector3.one));
            var gardenFilter = Static(root, "Garden and stall", Combine(garden, "Today garden"), art.Workbench);
            // Streetlights along the path on the far bank, a street's spacing apart.
            for (var i = 0; i < 5; i++)
            {
                var x = -20f + i * 10f;
                var z = 6.5f + Mathf.Abs(x) * 0.18f;
                var pole = new Vector3(x, RiverGround(new Vector3(x, 0, z)), z);
                Prim(root, "Streetlight " + i, PrimitiveType.Cylinder, pole + Vector3.up * 2.5f, new Vector3(0.09f, 2.5f, 0.09f), art.Steel);
                Prim(root, "Streetlight head " + i, PrimitiveType.Cube, pole + new Vector3(0, 5f, -0.25f), new Vector3(0.18f, 0.08f, 0.55f), art.Steel);
                var glow = GlowCard(Child(root, "Streetlight glow " + i), art, 0.6f);
                glow.transform.parent.localPosition = pole + new Vector3(0, 4.92f, -0.4f);
                Set(glow.transform.parent.gameObject.AddComponent<Billboard>(), "upright", false);
            }
            var weedsMesh = SankofaMeshes.Grass(Vector3.zero, Vector3.zero, 97, 260, 0.9f, 6f, RiverGround, p => CourseDistance(p) > 1.5f);
            weedsMesh.name = "Today weeds";
            var weeds = Static(root, "Weeds", weedsMesh, art.Grass);
            Prim(root, "Sky", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 420f, art.SkyCity);
            // Only the brightest stars show through the city's glow.
            var cityStars = SankofaMeshes.Stars(160, 977, 190f);
            cityStars.name = "Today stars";
            SaveMeshes(new[] { Static(root, "Stars", cityStars, art.Stars) });

            Bake(root, new[] { ground, gardenFilter }, JikoAt + Vector3.up * 0.2f);
            SaveMeshes(new[] { ground, river, cityFilter, gardenFilter, weeds });

            var jiko = BuildJiko(root, art);
            var gate = BuildGate(root, art);
            var soil = BuildSoil(root, art);
            var sprout = BuildSprout(root, art);
            var bead = BuildBead(root, art, "bead.today", "story.water.today", new Vector3(0.42f, 1.0f, 0.4f));
            var seed = BuildSeed(root, art, "seed.today", "story.water.today", new Vector3(0.4f, 0.75f, 0.45f));
            var sound = BuildStoryAmbience(root, new (string, Vector3, float, bool)[]
            {
                ("brook", new Vector3(0.2f, 0f, 2.4f), 0.14f, false),
                ("city-far", new Vector3(0f, 6f, 40f), 0.25f, true),
                ("crickets", new Vector3(-3f, 0.2f, -2f), 0.05f, false),
                // A matatu stage in Nairobi, across the river: engines, touts and passengers.
                ("matatu", new Vector3(-6f, 1.5f, 14f), 0.18f, true),
            });
            return (new[] { jiko, gate, soil, sprout, bead, seed }, sound);
        }

        static StageObject BuildJiko(Transform root, SankofaArtSet art)
        {
            var jikoRoot = Child(root, "Jiko");
            jikoRoot.localPosition = JikoAt;
            var bodyMesh = SankofaStoryMeshes.Lathe("Jiko body", SankofaStoryMeshes.JikoProfile(0.26f, 0.13f), 24, (a, y) => Color.white, 7);
            bodyMesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), bodyMesh.vertexCount).ToList());
            var body = Static(jikoRoot, "Body", bodyMesh, art.Metal);
            var coals = new List<CombineInstance>();
            var random = new System.Random(1960);
            for (var i = 0; i < 7; i++)
            {
                var mesh = SankofaMeshes.Stone(1961 + i, 1, 0.3f, 0.3f);
                mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, Range(random, 0.9f, 1.4f)), mesh.vertexCount).ToList());
                var size = Range(random, 0.02f, 0.03f);
                coals.Add(Part(mesh, new Vector3(Range(random, -0.06f, 0.06f), 0.255f, Range(random, -0.06f, 0.06f)), Quaternion.Euler(Range(random, 0, 360), 0, 0), new Vector3(size * 1.3f, size * 0.7f, size)));
            }
            var coalFilter = Static(jikoRoot, "Coals", Combine(coals, "Jiko coals"), art.Embers);
            SaveMeshes(new[] { body, coalFilter });
            var o = Staged(jikoRoot.gameObject, "jiko", "story.water.today", false, new[] { coalFilter.GetComponent<Renderer>(), body.GetComponent<Renderer>() });
            var glow = GlowCard(jikoRoot, art, 0.35f);
            glow.transform.localPosition = new Vector3(0, 0.3f, 0);
            Set(o, "glow", glow);
            Set(o, "glowIntensity", 0.7f);
            Set(o, "emitsLight", true);
            Set(o, "lightColor", new Color(1f, 0.55f, 0.28f));
            Set(o, "lightIntensity", 1.5f);
            return o;
        }

        static StageObject BuildGate(Transform root, SankofaArtSet art)
        {
            var gateRoot = Child(root, "Garden gate");
            gateRoot.localPosition = new Vector3(0.5f, 0, 0.14f);
            gateRoot.localRotation = Quaternion.Euler(0, -70, 0);
            var parts = new List<CombineInstance>
            {
                Part(SankofaStoryMeshes.Box("Gate post", new Vector3(0.07f, 1.2f, 0.07f)), new Vector3(-0.32f, 0.6f, 0), Quaternion.identity, Vector3.one),
                Part(SankofaStoryMeshes.Box("Gate post", new Vector3(0.07f, 1.2f, 0.07f)), new Vector3(0.32f, 0.6f, 0), Quaternion.identity, Vector3.one),
                Part(SankofaStoryMeshes.Box("Gate rail", new Vector3(0.6f, 0.06f, 0.03f)), new Vector3(0, 0.95f, 0), Quaternion.identity, Vector3.one),
                Part(SankofaStoryMeshes.Box("Gate rail", new Vector3(0.6f, 0.06f, 0.03f)), new Vector3(0, 0.35f, 0), Quaternion.identity, Vector3.one),
            };
            for (var i = 0; i < 5; i++)
                parts.Add(Part(SankofaStoryMeshes.Box("Gate slat", new Vector3(0.06f, 0.66f, 0.02f)), new Vector3(-0.22f + i * 0.11f, 0.65f, 0), Quaternion.identity, Vector3.one));
            var body = Static(gateRoot, "Body", Combine(parts, "Garden gate"), art.Paint);
            SaveMeshes(new[] { body });
            var o = Staged(gateRoot.gameObject, "gate", "story.water.today", true, new[] { body.GetComponent<Renderer>() });
            Set(o, "touchPoint", Marker(gateRoot, "Touch point", new Vector3(-0.12f, 0.88f, 0.02f)));
            Set(o, "touchRadius", 0.12f);
            Set(o, "attentionRadius", 0.35f);
            Set(o, "placePoint", Marker(gateRoot, "Place point", new Vector3(-0.12f, 0.92f, -0.03f)));
            return o;
        }

        static StageObject BuildSoil(Transform root, SankofaArtSet art)
        {
            var soilRoot = Child(root, "Garden soil");
            soilRoot.localPosition = new Vector3(0.44f, 0.5f, 0.52f);
            var mesh = SankofaMeshes.Stone(1970, 3, 0.25f, 0.2f);
            mesh.name = "Garden soil";
            mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), mesh.vertexCount).ToList());
            var body = Static(soilRoot, "Body", mesh, art.Soil);
            body.transform.localScale = new Vector3(0.36f, 0.03f, 0.2f);
            body.transform.localRotation = Quaternion.Euler(0, 8, 0);
            SaveMeshes(new[] { body });
            var o = Staged(soilRoot.gameObject, "soil", "story.water.today", true, new[] { body.GetComponent<Renderer>() });
            Set(o, "touchPoint", Marker(soilRoot, "Touch point", new Vector3(-0.1f, 0.04f, -0.06f)));
            Set(o, "touchRadius", 0.12f);
            Set(o, "attentionRadius", 0.25f);
            Set(o, "placePoint", Marker(soilRoot, "Place point", new Vector3(-0.08f, 0.035f, -0.04f)));
            return o;
        }

        static StageObject BuildSprout(Transform root, SankofaArtSet art)
        {
            var sproutRoot = Child(root, "Sprout");
            sproutRoot.localPosition = new Vector3(0.36f, 0.54f, 0.48f);
            var parts = new List<CombineInstance>();
            for (var i = 0; i < 3; i++)
            {
                var leaf = SankofaMeshes.Stone(1980 + i, 2, 0.05f, 10f);
                parts.Add(Part(leaf, new Vector3(0, 0.03f + i * 0.006f, 0), Quaternion.Euler(-35, i * 120, 0) * Quaternion.Euler(0, 0, 0), new Vector3(0.008f, 0.004f, 0.03f)));
            }
            var mesh = Combine(parts, "Sprout");
            mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), mesh.vertexCount).ToList());
            var body = Static(sproutRoot, "Body", mesh, art.Leaf);
            SaveMeshes(new[] { body });
            return Staged(sproutRoot.gameObject, "sprout", "story.water.today", false, new[] { body.GetComponent<Renderer>() });
        }

        static void ConfigureWaterToday(SerializedProperty e, Transform root)
        {
            // A city night: sodium haze low all round, the light from below rather than from the moon.
            Scene(e, "story.water.today", false, root, new Color(0.13f, 0.075f, 0.04f),
                new Color(0.09f, 0.065f, 0.05f), new Color(0.035f, 0.025f, 0.02f), 18f, 200f);
            e.FindPropertyRelative("TowardMoon").vector3Value = WaterMoonToday;
            e.FindPropertyRelative("Moon").colorValue = new Color(0.05f, 0.04f, 0.03f);
            e.FindPropertyRelative("MistColor").colorValue = new Color(0.12f, 0.075f, 0.045f);
            e.FindPropertyRelative("Mist").vector4Value = new Vector4(0.45f, 0.3f, 0.6f, 12f);
            e.FindPropertyRelative("Grade").vector4Value = new Vector4(1.3f, 0.9f, 0.7f, 1.0f);
        }

        static readonly Vector3 WaterMoon1850s = new Vector3(-0.5f, 0.4f, 0.6f);
        static readonly Vector3 WaterMoon1899 = new Vector3(-0.2f, 0.85f, 0.45f);
        static readonly Vector3 WaterMoonToday = new Vector3(-0.4f, 0.6f, 0.7f);

        // ---- shared pieces ----

        static StoryAmbience BuildStoryAmbience(Transform root, (string Recording, Vector3 At, float Volume, bool Far)[] layers, params ObjectSound[] voices)
        {
            var ambienceRoot = Child(root, "Ambience");
            var ambience = ambienceRoot.gameObject.AddComponent<StoryAmbience>();
            var so = new SerializedObject(ambience);
            var voiceList = so.FindProperty("voices");
            voiceList.arraySize = voices.Length;
            for (var i = 0; i < voices.Length; i++) voiceList.GetArrayElementAtIndex(i).objectReferenceValue = voices[i];
            var list = so.FindProperty("layers");
            list.arraySize = layers.Length;
            for (var i = 0; i < layers.Length; i++)
            {
                var place = Child(ambienceRoot, layers[i].Recording + " " + i);
                place.localPosition = layers[i].At;
                var source = Source(place.gameObject, layers[i].Far ? 0.5f : 0.85f, layers[i].Recording == "brook" ? 1f : 4f);
                source.clip = Recording(layers[i].Recording);
                var e = list.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("Source").objectReferenceValue = source;
                e.FindPropertyRelative("Volume").floatValue = layers[i].Volume;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            return ambience;
        }

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
            var root = Child(parent, "Lantern " + id);
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
    }
}
