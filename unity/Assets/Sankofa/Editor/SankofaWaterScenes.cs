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
            // Tall enough that the spring leaves it between lap and chest height, where a seated hand meets it.
            var springRock = new Vector3(0.52f, BoundaryGround(new Vector3(0.52f, 0, 0.3f)) + 0.3f, 0.3f);
            rocks.Add(Part(SankofaMeshes.Stone(1701, 3, 0.16f, 0.25f), springRock, Quaternion.Euler(0, 25, 0), new Vector3(0.3f, 0.42f, 0.26f)));
            // Far enough out that the women kneeling across it stay a body's length from the visitor.
            var tradeRock = new Vector3(0.2f, BoundaryGround(new Vector3(0.2f, 0, 0.72f)) + 0.24f, 0.72f);
            rocks.Add(Part(SankofaMeshes.Stone(1702, 3, 0.1f, 0.25f), tradeRock, Quaternion.Euler(0, -15, 0), new Vector3(0.22f, 0.26f, 0.2f)));
            foreach (var (angle, seed) in new[] { (0f, 1703), (120f, 1704), (240f, 1705) })
            {
                var at = TradersFireAt + Quaternion.Euler(0, angle, 0) * Vector3.forward * 0.17f;
                at.y = BoundaryGround(at) + 0.04f;
                rocks.Add(Part(SankofaMeshes.Stone(seed, 2, 0.2f, 0.3f), at, Quaternion.Euler(0, angle, 0), new Vector3(0.09f, 0.07f, 0.08f)));
            }
            var rockFilter = Static(root, "Rocks", Combine(rocks, "1850s rocks"), art.Rock);

            // The spring leaves the rock at its shoulder, where the visitor's hand meets it. A rounded fall, wider than it is deep, so it reads as a stream of water from any side rather than a sheet.
            // It runs down the outside of the rock's flank toward the seat, clear of the stone, and soaks into the
            // ground at its foot.
            var c = springRock;
            var spillFall = SankofaStoryMeshes.Tube("Spring falling", SankofaStoryMeshes.Smooth(new List<Vector3>
            {
                new Vector3(0.35f, c.y + 0.36f, 0.29f), new Vector3(0.24f, c.y + 0.22f, 0.29f), new Vector3(0.19f, c.y + 0.05f, 0.3f),
                new Vector3(0.19f, c.y - 0.15f, 0.31f), new Vector3(0.2f, BoundaryGround(new Vector3(0.2f, 0, 0.32f)) + 0.01f, 0.32f),
            }, 0.03f), 0.06f, 0.05f, 0.8f);

            // A few logs of firewood beside the traders' fire.
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
            SaveMeshes(new[] { ground, water, rockFilter, trunks, canopies, stones, woodFilter, statics[3], statics[4] });

            // The spring comes out at the rock's lip and falls; the falling water is the thing the visitor touches.
            var spillRoot = Child(root, "Water spill");
            var fall = Static(spillRoot, "Body", spillFall, art.Spill);
            SaveMeshes(new[] { fall });
            var spill = Staged(spillRoot.gameObject, "water.spill", "story.water.1850s", true, new[] { fall.GetComponent<Renderer>() });
            Set(spill, "touchPoint", Marker(spillRoot, "Touch point", new Vector3(0.3f, c.y + 0.29f, 0.29f)));
            Set(spill, "touchRadius", 0.1f);
            Set(spill, "attentionRadius", 0.15f);

            var fire = BuildTradersFire(root, art);
            var hide = BuildHide(root, art, tradeRock + Vector3.up * 0.25f);
            // Wambui waits back from the traders' fire, so her way to the hide does not pass through it.
            var groundAt = BoundaryGround(new Vector3(-0.75f, 0, 1.6f));
            var wambui = BuildFigure(root, art, "figure.wambui", "story.water.1850s", new Vector3(-0.75f, groundAt, 1.6f), 1.6f, FromTheForest, KikuyuLine);
            var naserian = BuildFigure(root, art, "figure.naserian", "story.water.1850s", new Vector3(0.85f, BoundaryGround(new Vector3(0.85f, 0, 1.3f)), 1.3f), 1.66f, FromThePlain, MaasaiLine);
            // The ewe comes to stand between the two women and behind them while they trade, dimmer than they are.
            var ewe = BuildEwe(root, art, new Vector3(0.45f, BoundaryGround(new Vector3(0.45f, 0, 2.5f)), 2.5f), EweFromThePlain);
            // For the trade the visitor and the two women sit around the hide: Wambui across it, Naserian to the right.
            Approaches(hide, true, new Vector2(0.05f, 1.35f), new Vector2(0.95f, 1.15f));
            var flour = BuildGourd(root, art, "gourd.flour", "story.water.1850s", new Vector3(-0.75f, groundAt + 0.98f, 1.48f), 0.23f, 0.095f, 0.03f, 1f, false);
            var milk = BuildGourd(root, art, "gourd.milk", "story.water.1850s", new Vector3(0.66f, BoundaryGround(new Vector3(0.66f, 0, 1.2f)), 1.2f), 0.34f, 0.075f, 0.026f, 0.55f, false);
            var bead = BuildBead(root, art, "bead", "story.water.1850s", new Vector3(0.84f, 1.36f, 1.25f));
            // Gourds hang from the hand by their necks rather than standing on it.
            Carries(wambui, flour, "Hands", new Vector3(0, -0.2f, 0));
            Carries(naserian, bead, "Hands", new Vector3(0, -0.01f, 0.03f));
            // Where each holds her gift out to the visitor: in front, a step beyond their space, clear of the fire and
            // rocks. The gift drifts the rest of the way to them on the firelight.
            HandsOver(flour, root, new Vector2(-0.25f, 1.45f));
            HandsOver(bead, root, new Vector2(0.75f, 1.25f));
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
                Prim(layer, "Card", PrimitiveType.Quad, new Vector3(0, 0.12f, 0), new Vector3(0.1f, 0.24f, 1f) * (i == 0 ? 1f : 0.7f), art.Flame);
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
            // Draped: the edge falls away over the rock's sides, four to seven centimetres (the mesh is flattened to
            // 0.014 of its height below), unevenly.
            var vertices = mesh.vertices;
            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                var r = new Vector2(v.x, v.z).magnitude;
                var angle = Mathf.Atan2(v.z, v.x);
                var droop = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1.05f, r)) * (4f + 1.2f * Mathf.Sin(angle * 3f));
                // An animal's outline, not a disc: legs and neck stretch out, the flanks curve in.
                var outline = 1.05f * (1f + 0.08f * Mathf.Cos(angle * 4f + 0.4f) + 0.06f * Mathf.Sin(angle * 7f) + SankofaMeshes.Fbm(new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 2f, 1741, 2) * 0.12f);
                vertices[i] = new Vector3(v.x * outline, v.y - droop, v.z * outline);
            }
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), mesh.vertexCount).ToList());
            var body = Static(hideRoot, "Body", mesh, art.Hide);
            body.transform.localScale = new Vector3(0.21f, 0.014f, 0.18f);
            body.transform.localRotation = Quaternion.Euler(0, 20, 0);
            SaveMeshes(new[] { body });
            var o = Staged(hideRoot.gameObject, "hide", "story.water.1850s", true, new[] { body.GetComponent<Renderer>() });
            // Touched at its near edge, within reach, while the women kneel at its far side.
            Set(o, "touchPoint", Marker(hideRoot, "Touch point", new Vector3(0, 0.02f, -0.16f)));
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
            Set(motion, "lookBone", neckBone);
            Set(motion, "headBone", Bone("Head"));
            // An unhurried walk with a shorter stride, so the legs stay inside the wrap.
            Set(motion, "speed", 0.55f);
            Transform Bone(string name) => figureRoot.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
            SetArray(motion, "reachArm", new Object[] { Bone("upperarm_r"), Bone("lowerarm_r"), hand });
            Set(motion, "tint", tint);
            SetArray(motion, "parts", cloth.Cast<Object>().ToArray());
            // Kneeling, the wrap gathers a little on the ground, still covering the knees, and the shawl falls shorter.
            var kneelScale = new SerializedObject(motion);
            var scales = kneelScale.FindProperty("partKneelScale");
            scales.arraySize = 2;
            scales.GetArrayElementAtIndex(0).floatValue = 0.9f;
            scales.GetArrayElementAtIndex(1).floatValue = 0.8f;
            kneelScale.ApplyModifiedPropertiesWithoutUndo();
            return o;
        }

        static StageObject BuildEwe(Transform root, SankofaArtSet art, Vector3 at, Vector2[] path)
        {
            var eweRoot = Child(root, "figure.ewe");
            eweRoot.localPosition = at;
            eweRoot.localRotation = Quaternion.Euler(0, -105, 0);
            var (bodyMesh, headMesh, legMesh) = SankofaStoryMeshes.EweParts("Figure ewe", 1810);
            var body = Static(eweRoot, "Body", bodyMesh, art.EmberFigure);
            DrawAsFigure(body.GetComponent<Renderer>(), art);
            var neck = Child(eweRoot, "Neck");
            neck.localPosition = new Vector3(0, 0.58f, 0.26f);
            var head = Static(neck, "Head", headMesh, art.EmberFigure);
            DrawAsFigure(head.GetComponent<Renderer>(), art);
            var legs = new[] { (-0.1f, 0.22f), (0.1f, 0.22f), (-0.1f, -0.22f), (0.1f, -0.22f) }.Select((p, i) =>
            {
                var hip = Child(eweRoot, "Leg " + i);
                hip.localPosition = new Vector3(p.Item1, 0.42f, p.Item2);
                DrawAsFigure(Static(hip, "Leg", legMesh, art.EmberFigure).GetComponent<Renderer>(), art);
                return hip;
            }).ToArray();
            SaveMeshes(new[] { body, head });
            SaveMeshes(new[] { legs[0].GetComponentInChildren<MeshFilter>() });
            var o = StageLight(eweRoot, "figure.ewe", "story.water.1850s", body.GetComponent<Renderer>(), 0.35f);
            // The ewe follows a little behind Naserian, with a quicker, smaller step.
            var motion = Walks(eweRoot, body.GetComponent<Renderer>(), 0.7f, path, 1.2f, 0.02f, false);
            SetArray(motion, "parts", new Object[] { head.GetComponent<Renderer>() }.Concat(legs.Select(l => (Object)l.GetComponentInChildren<Renderer>())).ToArray());
            Set(motion, "partSmoke", 0.6f);
            Set(motion, "bodySmoke", 0.6f);
            Set(motion, "tint", new Color(MaasaiLine.r * 0.7f, MaasaiLine.g * 0.7f, MaasaiLine.b * 0.7f, 1f));
            var gait = eweRoot.gameObject.AddComponent<QuadrupedGait>();
            Set(gait, "motion", motion);
            SetArray(gait, "legs", legs.Cast<Object>().ToArray());
            Set(gait, "head", neck);
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

        /// <summary>Where the holder steps to give something to the visitor in person, at ground level in the scene's plan.</summary>
        static void HandsOver(StageObject item, Transform root, Vector2 place) =>
            Set(item, "handOver", Marker(root, item.Id + " hand-over", new Vector3(place.x, BoundaryGround(new Vector3(place.x, 0, place.y)), place.y)));

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
        static readonly Vector2[] EweFromThePlain = { new Vector2(6.9f, 2.8f), new Vector2(4.6f, 2.7f), new Vector2(2.4f, 2.6f), new Vector2(0.45f, 2.5f) };

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
            // A rounded glass bead, pierced, slipped from the string it was worn on. Its colour waits for family review.
            var profile = new List<Vector2> { new Vector2(0.0032f, -0.0072f) };
            for (var k = 0; k <= 8; k++)
            {
                var angle = Mathf.Lerp(-0.5f, 0.5f, k / 8f) * Mathf.PI;
                profile.Add(new Vector2(0.0032f + 0.0068f * Mathf.Cos(angle), 0.0072f * Mathf.Sin(angle)));
            }
            profile.Add(new Vector2(0.0032f, 0.0072f));
            profile.Add(new Vector2(0.0032f, -0.0072f));
            // Glass of one colour with a pale band toward each end and a lighter equator, as wound trade beads often have.
            var glass = new Color(0.2f, 0.4f, 0.8f);
            var band = new Color(0.92f, 0.86f, 0.7f);
            var mesh = SankofaStoryMeshes.Lathe("Bead " + id, profile, 16, (a, y) =>
            {
                var t = Mathf.Abs(y) / 0.0072f;
                var c = Color.Lerp(glass * 1.25f, glass, Mathf.SmoothStep(0f, 1f, t * 2f));
                return Mathf.Abs(t - 0.62f) < 0.05f ? band : new Color(c.r, c.g, c.b, 1f);
            }, 1750);
            mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), mesh.vertexCount).ToList());
            var body = Static(beadRoot, "Body", mesh, art.Bead);
            // The hole toward the visitor, so it reads as a bead wherever it is, not as a capsule seen from the side.
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            SaveMeshes(new[] { body });
            var o = Staged(beadRoot.gameObject, id, scene, false, new[] { body.GetComponent<Renderer>() });
            // A little of the fire's light lingers on what it gives, so a small thing reads at arm's length; what
            // waits on the hearth stone from a kept visit glows a little more, so it is found again.
            var kept = id.EndsWith(".kept");
            // A large trade bead, so it reads in the hand and on the stone: about 3.4 cm across.
            beadRoot.localScale = Vector3.one * 1.8f;
            if (kept) body.transform.localScale *= 1.25f;
            Set(o, "glow", GlowCard(beadRoot, art, kept ? 0.12f : 0.09f));
            Set(o, "glowIntensity", 1.0f);
            Set(o, "attentionRadius", 0.05f);
            return o;
        }

        static StageObject BuildSeed(Transform root, SankofaArtSet art, string id, string scene, Vector3 at)
        {
            var seedRoot = Child(root, id);
            seedRoot.localPosition = at;
            // A pinch of millet folded into a leaf, gathered at the top and tied with a twist of fibre.
            var mesh = SankofaStoryMeshes.Lathe("Seed bundle " + id, new List<Vector2>
            {
                new Vector2(0f, 0f), new Vector2(0.011f, 0.002f), new Vector2(0.017f, 0.01f), new Vector2(0.016f, 0.02f),
                new Vector2(0.009f, 0.028f), new Vector2(0.004f, 0.032f), new Vector2(0.007f, 0.04f), new Vector2(0f, 0.043f),
            }, 10, (a, y) => new Color(1f, 1f - 0.25f * Mathf.Abs(Mathf.Sin(a * 2.5f)), 1f, 1f), 1760);
            mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), mesh.vertexCount).ToList());
            var body = Static(seedRoot, "Body", mesh, art.Leaf);
            var tieMesh = SankofaStoryMeshes.Lathe("Seed tie " + id, new List<Vector2>
            {
                new Vector2(0.0045f, 0.029f), new Vector2(0.0062f, 0.03f), new Vector2(0.0062f, 0.0335f), new Vector2(0.0045f, 0.0345f),
            }, 10, (a, y) => Color.white, 1761);
            tieMesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), tieMesh.vertexCount).ToList());
            var tie = Static(seedRoot, "Tie", tieMesh, art.SeatWood);
            SaveMeshes(new[] { body, tie });
            var o = Staged(seedRoot.gameObject, id, scene, false, new[] { body.GetComponent<Renderer>(), tie.GetComponent<Renderer>() });
            seedRoot.localScale = Vector3.one * 1.45f;
            Set(o, "glow", GlowCard(seedRoot, art, 0.12f));
            Set(o, "glowIntensity", 0.95f);
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
            // Each tent: sewn panels, a hem muddied by the rains, a dark door facing the stream, guy ropes to pegs
            // and the pole's cap.
            var canvas = new List<CombineInstance>();
            var rigging = new List<CombineInstance>();
            foreach (var at in new[] { new Vector3(-2.6f, 0, 5f), new Vector3(0.9f, 0, 6.2f), new Vector3(4.2f, 0, 5.2f), new Vector3(6.6f, 0, 8.4f), new Vector3(-0.8f, 0, 9.5f) })
            {
                var p = at;
                p.y = BoundaryGround(at) - 0.02f;
                var radius = R(1.5f, 1.9f);
                var height = R(2.6f, 3f);
                var shade = R(0.85f, 1f);
                var tent = SankofaStoryMeshes.Lathe("Tent", SankofaStoryMeshes.TentProfile(radius, height), 56, (a, y) =>
                {
                    var seam = Mathf.Abs(Mathf.Sin(a * 7f)) > 0.985f ? 0.72f : 1f;
                    var hem = Mathf.Lerp(0.62f, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.45f, y)));
                    // The door, an opening in the canvas itself on the side facing the stream (+Z), dark inside.
                    var fromDoor = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 90f));
                    if (fromDoor < 10f && y < 1.05f) return new Color(0.14f, 0.12f, 0.1f);
                    return new Color(shade, shade, shade) * seam * hem;
                }, (int)(at.x * 10));
                // The door faces the stream and the visitor across it.
                var yaw = Mathf.Atan2(-p.x, -p.z) * Mathf.Rad2Deg + R(-25f, 25f);
                var turn = Quaternion.Euler(0, yaw, 0);
                canvas.Add(Part(tent, p, turn, Vector3.one));
                for (var k = 0; k < 8; k++)
                {
                    var around = Quaternion.Euler(0, yaw + 22.5f + k * 45f, 0);
                    var from = p + around * new Vector3(0, height * 0.25f, radius * 0.92f);
                    var peg = p + around * new Vector3(0, 0, radius * 1.8f);
                    peg.y = BoundaryGround(peg);
                    var span = peg - from;
                    rigging.Add(Part(SankofaStoryMeshes.Box("Guy rope", new Vector3(0.006f, 0.006f, span.magnitude)), from + span * 0.5f, Quaternion.LookRotation(span), Vector3.one));
                    rigging.Add(Part(SankofaStoryMeshes.Box("Peg", new Vector3(0.03f, 0.16f, 0.03f)), peg + Vector3.up * 0.06f, Quaternion.Euler(0, 0, 12f), Vector3.one));
                }
                rigging.Add(Part(SankofaStoryMeshes.Box("Pole cap", new Vector3(0.05f, 0.22f, 0.05f)), p + Vector3.up * (height + 0.08f), Quaternion.identity, Vector3.one));
            }
            var tentFilter = Static(root, "Tents", Combine(canvas, "1899 tents"), art.Canvas);
            // Hemp rope and pale pegs: camp gear, nothing that reads as a weapon.
            var riggingFilter = Static(root, "Tent rigging", Combine(rigging.Select(r => Tinted(r, (q, n) => new Color(0.62f, 0.55f, 0.42f))).ToList(), "1899 tent rigging"), art.Canvas);

            var timber = new List<CombineInstance>();
            for (var i = 0; i < 9; i++)
            {
                var at = new Vector3(2.4f + R(-1.2f, 1.2f), 0, 3.8f + R(-0.6f, 0.6f));
                at.y = BoundaryGround(at) + 0.2f;
                timber.Add(Part(SankofaStoryMeshes.Box("Crate", new Vector3(R(0.4f, 0.7f), 0.4f, R(0.4f, 0.6f))), at, Quaternion.Euler(0, R(-20, 20), 0), Vector3.one));
            }
            // The rail crib within reach on the right: sleepers laid crosswise to lap height.
            var crib = new Vector3(0.45f, BoundaryGround(new Vector3(0.45f, 0, 0.7f)), 0.7f);
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
            SaveMeshes(new[] { ground, water, trunks, canopies, stones, statics[3], statics[4], tentFilter, riggingFilter, timberFilter, stumpFilter });

            var rails = BuildRails(root, art, crib + Vector3.up * 0.48f);
            var lantern = BuildLantern(root, art, "lantern.1899", "story.water.1899", CampLanternAt);
            // Njeri stands clear of the camp lantern, well to its left from the seat; Naisula on the right, as their
            // grandmothers did.
            var njeriAt = new Vector3(-1.2f, BoundaryGround(new Vector3(-1.2f, 0, 1.4f)), 1.4f);
            var naisulaAt = new Vector3(0.98f, BoundaryGround(new Vector3(0.98f, 0, 1.5f)), 1.5f);
            var njeri = BuildFigure(root, art, "figure.njeri", "story.water.1899", njeriAt, 1.58f, FromTheForest, KikuyuLine);
            var naisula = BuildFigure(root, art, "figure.naisula", "story.water.1899", naisulaAt, 1.64f, FromThePlain, MaasaiLine);
            // Njeri comes to stand at Naisula's side to pour what flour she has into her gourd.
            Approaches(naisula, false, new Vector2(0.05f, 1.62f));
            var empty = BuildGourd(root, art, "gourd.empty", "story.water.1899", njeriAt + new Vector3(0.18f, 0.12f, -0.08f), 0.24f, 0.1f, 0.03f, 0.9f, false);
            var old = BuildGourd(root, art, "gourd.old", "story.water.1899", naisulaAt + new Vector3(-0.2f, 0.14f, -0.08f), 0.32f, 0.07f, 0.025f, 0.45f, false);
            var seed = BuildSeed(root, art, "seed", "story.water.1899", njeriAt + new Vector3(0.12f, 0.85f, -0.1f));
            Carries(njeri, empty, "Hands", new Vector3(0, -0.21f, 0));
            Carries(njeri, seed, "Hands", new Vector3(0, 0.01f, 0.03f));
            HandsOver(seed, root, new Vector2(-0.15f, 1.45f));
            Carries(naisula, old, "Hands", new Vector3(0, -0.28f, 0));

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
            // The rails run away from the visitor, so their sections face the seat and the near end is the one touched.
            railRoot.localRotation = Quaternion.Euler(0, -90f, 0);
            // Four new flat-bottomed rails side by side, their sections showing at the ends toward the visitor, and a
            // bundle of fishplates with their bolts across the far end, waiting for the joints.
            Color Weathered(Vector3 p, Vector3 n) => Weather(p, n, false);
            Color Weather(Vector3 p, Vector3 n, bool head)
            {
                var w = top + p;
                var rain = SankofaMeshes.Fbm(w * 7f, 1899, 3) + 0.15f * Mathf.Sin(w.x * 23f);
                var scale = new Color(0.2f, 0.2f, 0.23f);
                var rust = new Color(0.72f, 0.3f, 0.12f);
                // Mostly dark mill scale, rust blooming in patches where the rain has stood.
                var c = Color.Lerp(scale, rust, 0.85f * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.02f, 0.3f, rain)));
                // The rolled running surface is smoother and catches the light; the sawn ends have rusted first.
                if (head && n.y > 0.8f && p.y > 0.09f) c = Color.Lerp(c, new Color(0.46f, 0.46f, 0.48f), 0.5f);
                if (Mathf.Abs(n.x) > 0.9f) c = Color.Lerp(c, new Color(0.5f, 0.27f, 0.15f), 0.7f);
                return c;
            }
            var steel = new List<CombineInstance>();
            var section = SankofaStoryMeshes.RailSection();
            for (var i = 0; i < 4; i++)
            {
                // Spaced on the crib with gaps between, so each reads as a rail rather than one ribbed slab.
                var z = (i - 1.5f) * 0.19f;
                var at = new Vector3(i % 2 == 0 ? 0.03f : -0.04f, 0f, z);
                steel.Add(Tinted(Part(SankofaStoryMeshes.Extrude("Rail " + i, section, 1.4f, 24), at, Quaternion.identity, Vector3.one), (p, n) => Weather(p, n, true)));
            }
            // Fishplates stacked crosswise at one end, with the bolts that will join the rails laid on top.
            for (var k = 0; k < 3; k++)
            {
                var plate = new Vector3(0.5f + k * 0.012f, 0.1f + 0.007f + k * 0.014f, 0.01f * k);
                steel.Add(Tinted(Part(SankofaStoryMeshes.Box("Fishplate", new Vector3(0.075f, 0.014f, 0.66f)), plate, Quaternion.Euler(0, 4f * k - 4f, 0), Vector3.one), Weathered));
            }
            for (var b = 0; b < 4; b++)
            {
                var along = -0.17f + b * 0.11f;
                var bolt = SankofaStoryMeshes.Lathe("Bolt", new List<Vector2>
                {
                    new Vector2(0, 0), new Vector2(0.011f, 0), new Vector2(0.011f, 0.012f), new Vector2(0.006f, 0.012f),
                    new Vector2(0.006f, 0.07f), new Vector2(0f, 0.07f),
                }, 6, (a, y) => Color.white, 40 + b);
                bolt.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), bolt.vertexCount).ToList());
                steel.Add(Tinted(Part(bolt, new Vector3(0.5f, 0.142f + 0.011f, along), Quaternion.Euler(0, 0, b % 2 == 0 ? 90f : -90f) * Quaternion.Euler(0, 30f * b, 0), Vector3.one), Weathered));
            }
            var body = Static(railRoot, "Body", Combine(steel, "1899 rails"), art.RailRust);
            SaveMeshes(new[] { body });
            var o = Staged(railRoot.gameObject, "rails", "story.water.1899", true, new[] { body.GetComponent<Renderer>() });
            Set(o, "touchPoint", Marker(railRoot, "Touch point", new Vector3(-0.62f, 0.1f, -0.095f)));
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
                // A reclaimed bank: dark, grassed earth near the water, paler trodden ground further off.
                var near = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(14f, 3f, p.magnitude));
                var c = Color.Lerp(new Color(0.55f, 0.52f, 0.47f), new Color(0.36f, 0.4f, 0.28f), near) * (1f + SankofaMeshes.Fbm(p * 0.8f, 95, 3) * 0.35f);
                var bank = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(1.2f, 2.4f, CourseDistance(p)));
                return Color.Lerp(c, new Color(0.38f, 0.36f, 0.33f), bank);
            }, p => 0f, 5f, 160f), art.Ground);
            var riverPoints = SankofaStoryMeshes.Smooth(StreamCourse, 0.4f).Select(p => new Vector3(p.x, RiverGround(p) + 0.35f, p.z)).ToList();
            // Wide enough that its edges run under the banks.
            var river = Static(root, "River", SankofaStoryMeshes.Ribbon("Today river", riverPoints, 2.9f, Vector3.up), art.River);

            // The city beyond the river's green strip: low blocks nearer with balconies toward the river, towers
            // only far off, and the KICC tower on the skyline. Each block carries its own tone, window pattern and
            // occupancy in its vertex colour (see City.shader): r tone, g style (glass above 0.8), b how many
            // windows are lit, a its base height.
            var blocks = new List<CombineInstance>();
            var towerTops = new List<Vector3>();
            void Building(Mesh mesh, Vector3 at, Quaternion turn, float baseY, float tone, float style, float occupancy)
            {
                // Alpha 1 marks plain parts with no windows: balconies, tanks, fins and the hall roof.
                var c = new Color(tone, style, occupancy, baseY > 900f ? 1f : Mathf.InverseLerp(-8f, 32f, baseY) * 0.95f);
                mesh.SetColors(Enumerable.Repeat(c, mesh.vertexCount).ToList());
                blocks.Add(Part(mesh, at, turn, Vector3.one));
            }
            for (var i = 0; i < 200 && blocks.Count < 170; i++)
            {
                var degrees = R(-15f, 195f);
                var distance = R(55f, 240f);
                var at = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad)) * distance;
                if (at.z < 38f) continue;
                var tower = distance > 130f && random.NextDouble() < 0.25;
                var height = tower ? R(40f, 80f) * (random.NextDouble() < 0.15 ? 1.8f : 1f) : R(9f, 22f) + distance * R(0f, 0.08f);
                // Storeys are 3.4 m, as the shader draws them.
                height = Mathf.Max(2, Mathf.Round(height / 3.4f)) * 3.4f;
                var size = new Vector3(R(10f, 26f), height, R(10f, 20f));
                var baseY = RiverGround(at) - 0.5f;
                at.y = baseY + height * 0.5f;
                var turn = Quaternion.Euler(0, R(-25, 25), 0);
                var tone = R(0f, 1f);
                var style = tower && random.NextDouble() < 0.6 ? R(0.85f, 1f) : R(0f, 0.75f);
                Building(SankofaStoryMeshes.Box("Block", size), at, turn, baseY, tone, style, R(0.3f, 1f));
                if (tower) towerTops.Add(at + Vector3.up * (height * 0.5f + 0.6f));
                // Balconies on the nearer residential blocks, a slab per storey on the side facing the river.
                if (!tower && distance < 120f && random.NextDouble() < 0.6)
                {
                    var face = turn * new Vector3(0, 0, -size.z * 0.5f - 0.55f);
                    for (var floor = 1; floor < height / 3.4f; floor++)
                        Building(SankofaStoryMeshes.Box("Balcony", new Vector3(size.x * 0.82f, 0.18f, 1.1f)), new Vector3(at.x, baseY + floor * 3.4f + 0.09f, at.z) + face, turn, 999f, tone * 0.8f, 0f, 0f);
                }
                // Rooflines: an upper storey set back, a water tank or a stair head, so the skyline is not all flat.
                var roof = at + Vector3.up * height * 0.5f;
                var pick = random.NextDouble();
                if (pick < 0.35)
                {
                    var upper = new Vector3(size.x * R(0.45f, 0.7f), 3.4f, size.z * R(0.5f, 0.75f));
                    Building(SankofaStoryMeshes.Box("Upper storey", upper), roof + turn * new Vector3(R(-2f, 2f), upper.y * 0.5f, R(-2f, 2f)), turn, baseY, tone, style, R(0.3f, 1f));
                }
                else if (pick < 0.75)
                {
                    var tank = new Vector3(R(1.6f, 2.6f), R(1.4f, 2.2f), R(1.6f, 2.6f));
                    Building(SankofaStoryMeshes.Box("Water tank", tank), roof + turn * new Vector3(size.x * R(-0.3f, 0.3f), tank.y * 0.5f + 0.6f, size.z * R(-0.3f, 0.3f)), turn, 999f, tone, 0f, 0f);
                    Building(SankofaStoryMeshes.Box("Stair head", new Vector3(2.4f, 2.6f, 2.4f)), roof + turn * new Vector3(size.x * R(-0.35f, 0.35f), 1.3f, size.z * R(-0.35f, 0.35f)), turn, 999f, tone, 0f, 0f);
                }
            }
            // The Kenyatta International Convention Centre: the ribbed cylindrical tower with its crown, beside the
            // cone of its plenary hall, the shape most people know Nairobi's skyline by. Its distance is compressed.
            var kicc = new Vector3(Mathf.Cos(70f * Mathf.Deg2Rad), 0, Mathf.Sin(70f * Mathf.Deg2Rad)) * 235f;
            var kiccBase = RiverGround(kicc) - 1f;
            var kiccTower = SankofaStoryMeshes.Lathe("KICC tower", new List<Vector2>
            {
                new Vector2(0, 0), new Vector2(10f, 0), new Vector2(10f, 92f), new Vector2(11.5f, 95f), new Vector2(12.5f, 99f),
                new Vector2(11f, 101f), new Vector2(6f, 103f), new Vector2(0, 103.5f),
            }, 28, (a, y) => Color.white, 1974);
            Building(kiccTower, kicc + Vector3.up * kiccBase, Quaternion.identity, kiccBase, 0.55f, 0.5f, 0.7f);
            for (var f = 0; f < 28; f++)
            {
                var angle = f * Mathf.PI * 2f / 28f;
                var fin = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                Building(SankofaStoryMeshes.Box("KICC fin", new Vector3(0.5f, 90f, 1.4f)), kicc + fin * 10.4f + Vector3.up * (kiccBase + 46f), Quaternion.LookRotation(fin), 999f, 0.7f, 0f, 0f);
            }
            var hall = SankofaStoryMeshes.Lathe("KICC hall", new List<Vector2>
            {
                new Vector2(0, 0), new Vector2(24f, 0), new Vector2(24f, 9f), new Vector2(18f, 16f), new Vector2(4f, 26f), new Vector2(0, 27f),
            }, 24, (a, y) => Color.white, 1967);
            Building(hall, kicc + new Vector3(-30f, kiccBase, -12f), Quaternion.identity, 999f, 0.35f, 0f, 0f);
            towerTops.Add(kicc + Vector3.up * (kiccBase + 104.5f));
            var cityFilter = Static(root, "City", Combine(blocks, "Today city"), art.City);
            // Red aviation lights on the towers.
            for (var t = 0; t < towerTops.Count; t++)
            {
                var light = Prim(root, "Aviation light " + t, PrimitiveType.Quad, towerTops[t], Vector3.one * 2.2f, art.TailLight);
                Set(light.AddComponent<Billboard>(), "upright", false);
            }

            // Trees along both banks, dark against the city's glow, and the road between the river and the blocks.
            var trunks = new List<CombineInstance>();
            var crowns = new List<CombineInstance>();
            for (var i = 0; i < 60 && trunks.Count < 26; i++)
            {
                var x = R(-60f, 60f);
                var z = R(9f, 30f);
                var at = new Vector3(x, 0, z + Mathf.Abs(x) * 0.18f);
                if (Mathf.Abs(at.z - 24f - Mathf.Abs(x) * 0.18f) < 3f) continue;
                // Keep the view across the river open straight ahead; trees frame it at the sides, clear of the lamps.
                if (Mathf.Abs(Mathf.Atan2(at.x, at.z)) < 0.45f) continue;
                if (Enumerable.Range(0, 5).Any(k => Vector2.Distance(new Vector2(at.x, at.z), new Vector2(-20f + k * 10f, 6.5f + Mathf.Abs(-20f + k * 10f) * 0.18f)) < 5f)) continue;
                at.y = RiverGround(at) - 0.05f;
                Mesh trunk, crown;
                if (random.NextDouble() < 0.7) SankofaStoryMeshes.Acacia(570 + i, R(6f, 10f), out trunk, out crown);
                else SankofaMeshes.Tree(570 + i, R(7f, 12f), out trunk, out crown);
                var m = Matrix4x4.TRS(at, Quaternion.Euler(0, R(0, 360), 0), Vector3.one);
                trunks.Add(new CombineInstance { mesh = trunk, transform = m });
                crowns.Add(new CombineInstance { mesh = crown, transform = m });
            }
            for (var i = 0; i < 30 && trunks.Count < 38; i++)
            {
                var degrees = R(200f, 340f);
                var at = new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0, Mathf.Sin(degrees * Mathf.Deg2Rad)) * R(7f, 22f);
                at.y = RiverGround(at) - 0.05f;
                SankofaMeshes.Tree(600 + i, R(6f, 11f), out var trunk, out var crown);
                var m = Matrix4x4.TRS(at, Quaternion.Euler(0, R(0, 360), 0), Vector3.one);
                trunks.Add(new CombineInstance { mesh = trunk, transform = m });
                crowns.Add(new CombineInstance { mesh = crown, transform = m });
            }
            var trunkFilter = Static(root, "Tree trunks", Combine(trunks, "Today trunks"), art.TreeBark);
            var crownFilter = Static(root, "Tree canopies", Combine(crowns, "Today canopies"), art.TreeCanopy);
            BuildTraffic(root, art);

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
                // In front of the lamp head, so the head never hides part of its own glow.
                glow.transform.parent.localPosition = pole + new Vector3(0, 4.9f, -0.62f);
                Set(glow.transform.parent.gameObject.AddComponent<Billboard>(), "upright", false);
            }
            var weedsMesh = SankofaMeshes.Grass(Vector3.zero, Vector3.zero, 97, 700, 0.9f, 9f, RiverGround, p => CourseDistance(p) > 1.5f);
            weedsMesh.name = "Today weeds";
            var weeds = Static(root, "Weeds", weedsMesh, art.Grass);
            Prim(root, "Sky", PrimitiveType.Sphere, Vector3.zero, Vector3.one * 420f, art.SkyCity);
            // Only the brightest stars show through the city's glow.
            var cityStars = SankofaMeshes.Stars(160, 977, 190f);
            cityStars.name = "Today stars";
            SaveMeshes(new[] { Static(root, "Stars", cityStars, art.Stars) });

            Bake(root, new[] { ground, gardenFilter }, JikoAt + Vector3.up * 0.2f);
            SaveMeshes(new[] { ground, river, cityFilter, gardenFilter, weeds, trunkFilter, crownFilter });

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

        /// <summary>
        /// The road across the river, 24 m out between the trees and the blocks: headlights coming one way, tail
        /// lights going the other, at an unhurried city pace.
        /// </summary>
        static void BuildTraffic(Transform root, SankofaArtSet art)
        {
            var road = new List<Vector3>();
            for (var x = -110f; x <= 110f; x += 10f)
            {
                var p = new Vector3(x, 0, 24f + Mathf.Abs(x) * 0.18f);
                p.y = RiverGround(p) + 0.7f;
                road.Add(p);
            }
            var traffic = Child(root, "Traffic").gameObject.AddComponent<Traffic>();
            var random = new System.Random(2026);
            var lights = new List<Transform>();
            for (var i = 0; i < 14; i++)
            {
                var toward = i % 2 == 0;
                var card = Prim(traffic.transform, (toward ? "Headlights " : "Tail lights ") + i, PrimitiveType.Quad, Vector3.zero, new Vector3(1.1f, 0.6f, 1f), toward ? art.Headlight : art.TailLight);
                Set(card.AddComponent<Billboard>(), "upright", true);
                lights.Add(card.transform);
            }
            var so = new SerializedObject(traffic);
            var points = so.FindProperty("road");
            points.arraySize = road.Count;
            for (var i = 0; i < road.Count; i++) points.GetArrayElementAtIndex(i).vector3Value = road[i];
            var list = so.FindProperty("lights");
            list.arraySize = lights.Count;
            for (var i = 0; i < lights.Count; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = lights[i];
            so.FindProperty("seed").intValue = random.Next(1000);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static StageObject BuildJiko(Transform root, SankofaArtSet art)
        {
            var jikoRoot = Child(root, "Jiko");
            jikoRoot.localPosition = JikoAt;
            // A ceramic jiko as sold along any Nairobi road: galvanised cladding pinched at the waist, a clay liner
            // holding the coals, three pot rests, an ash door at the foot, and two cobs of maize roasting over it.
            Mesh Turned(string name, List<Vector2> profile, int seed, System.Func<float, float, Color> tint)
            {
                var mesh = SankofaStoryMeshes.Lathe(name, profile, 32, tint, seed);
                mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, 1), mesh.vertexCount).ToList());
                return mesh;
            }
            // Soot climbs from the lip; a seam and a few dents darken the sheet.
            var cladding = Turned("Jiko cladding", SankofaStoryMeshes.JikoCladding(), 7, (a, y) =>
            {
                var soot = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.25f, y)) * 0.65f;
                var seam = Mathf.Abs(Mathf.DeltaAngle(a * Mathf.Rad2Deg, 200f)) < 4f ? 0.25f : 0f;
                var v = 1f - soot - seam + SankofaMeshes.Fbm(new Vector3(Mathf.Cos(a) * 3f, y * 20f, Mathf.Sin(a) * 3f), 71, 2) * 0.12f;
                return new Color(v, v, v * 1.02f, 1f);
            });
            var liner = Turned("Jiko liner", SankofaStoryMeshes.JikoLiner(), 8, (a, y) =>
            {
                // The clay is blackened inside where the coals sit; the rim keeps its fired colour.
                var burnt = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.255f, 0.215f, y));
                var v = Mathf.Lerp(1f, 0.25f, burnt);
                return new Color(v, v, v, 1f);
            });
            var bodyFilter = Static(jikoRoot, "Cladding", cladding, art.Cladding);
            var linerFilter = Static(jikoRoot, "Liner", liner, art.Liner);

            // Pot rests, door, handles and the grill, in plain sheet metal.
            var toVisitor = new Vector3(-JikoAt.x, 0, -JikoAt.z).normalized;
            var front = Quaternion.LookRotation(toVisitor);
            var fittings = new List<CombineInstance>();
            for (var k = 0; k < 3; k++)
            {
                var radial = Quaternion.Euler(0, 60f + k * 120f, 0) * toVisitor;
                fittings.Add(Part(SankofaStoryMeshes.Box("Pot rest", new Vector3(0.007f, 0.036f, 0.032f)), radial * 0.108f + Vector3.up * 0.282f,
                    Quaternion.LookRotation(radial) * Quaternion.Euler(-12f, 0, 0), Vector3.one));
            }
            // The ash door, swung open on its hinge so the glow of the chamber shows.
            // The chamber wall leans in by about fourteen degrees; the opening follows it.
            var doorAt = toVisitor * 0.1015f + Vector3.up * 0.058f;
            var wall = front * Quaternion.Euler(-14f, 0, 0);
            var hinge = doorAt + front * new Vector3(0.036f, 0, 0.002f);
            var swung = front * Quaternion.Euler(0, 70f, 0);
            fittings.Add(Part(SankofaStoryMeshes.Box("Ash door", new Vector3(0.07f, 0.046f, 0.004f)), hinge + swung * new Vector3(-0.035f, 0, 0), swung, Vector3.one));
            foreach (var side in new[] { -1f, 1f })
            {
                var radial = Quaternion.Euler(0, 90f * side, 0) * toVisitor;
                var handle = Quaternion.LookRotation(radial);
                fittings.Add(Part(SankofaStoryMeshes.Box("Handle", new Vector3(0.05f, 0.006f, 0.006f)), radial * 0.122f + Vector3.up * 0.175f, handle, Vector3.one));
                foreach (var end in new[] { -1f, 1f })
                    fittings.Add(Part(SankofaStoryMeshes.Box("Handle arm", new Vector3(0.006f, 0.006f, 0.03f)), radial * 0.106f + handle * new Vector3(end * 0.022f, 0, 0) + Vector3.up * 0.175f, handle, Vector3.one));
            }
            // A wire grill across the back half of the rests, so the coals still show in front.
            for (var g = 0; g < 5; g++)
                fittings.Add(Part(SankofaStoryMeshes.Box("Grill wire", new Vector3(0.24f, 0.004f, 0.004f)), front * new Vector3(0, 0.302f - 0.282f, -0.01f - g * 0.022f) + Vector3.up * 0.282f, front, Vector3.one));
            foreach (var x in new[] { -0.08f, 0.08f })
                fittings.Add(Part(SankofaStoryMeshes.Box("Grill rail", new Vector3(0.004f, 0.004f, 0.11f)), front * new Vector3(x, 0.02f, -0.055f) + Vector3.up * 0.282f, front, Vector3.one));
            var fittingFilter = Static(jikoRoot, "Fittings", Combine(fittings, "Jiko fittings"), art.Metal);

            // Two cobs on the grill, turned now and then by whoever keeps the stall; one is further on than the other.
            var cobs = new List<CombineInstance>();
            for (var c = 0; c < 2; c++)
            {
                var done = c == 0 ? 0.55f : 0.25f;
                var cob = Turned("Maize cob", new List<Vector2>
                {
                    new Vector2(0, 0), new Vector2(0.012f, 0.004f), new Vector2(0.019f, 0.02f), new Vector2(0.021f, 0.06f),
                    new Vector2(0.02f, 0.1f), new Vector2(0.015f, 0.13f), new Vector2(0.006f, 0.142f), new Vector2(0, 0.145f),
                }, 50 + c, (a, y) =>
                {
                    var scorch = SankofaMeshes.Fbm(new Vector3(Mathf.Cos(a) * 4f, y * 40f, Mathf.Sin(a) * 4f), 60 + c, 3);
                    var v = Mathf.Lerp(1f, 0.12f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f - done, 0.35f - done, scorch)));
                    // Rows of kernels: a dark seam between rows around the cob and between kernels along it.
                    var rows = Mathf.Pow(Mathf.Abs(Mathf.Sin(a * 7f)), 0.35f);
                    var kernels = Mathf.Pow(Mathf.Abs(Mathf.Sin(y * 230f + Mathf.Floor(a * 14f / Mathf.PI) * 1.3f)), 0.3f);
                    v *= Mathf.Lerp(0.45f, 1f, rows * kernels);
                    return new Color(v, v * 0.95f, v * 0.9f, 1f);
                });
                // Side by side across the grill, a hand's width apart, so each reads as a cob rather than a crossed shape.
                cobs.Add(Part(cob, front * new Vector3(-0.07f + c * 0.01f, 0.041f, -0.015f - c * 0.055f) + Vector3.up * 0.282f,
                    front * Quaternion.Euler(0, 0, -90f) * Quaternion.Euler(0, 6f * c, 0), Vector3.one));
            }
            var cobFilter = Static(jikoRoot, "Maize", Combine(cobs, "Jiko maize"), art.Maize);

            // The coals, heaped in the liner's bowl, and the dim glow of the ash chamber through the open door.
            var coals = new List<CombineInstance>();
            var random = new System.Random(1960);
            for (var i = 0; i < 14; i++)
            {
                var mesh = SankofaMeshes.Stone(1961 + i, 1, 0.3f, 0.3f);
                mesh.SetUVs(1, Enumerable.Repeat(new Vector2(1, Range(random, 0.9f, 1.4f)), mesh.vertexCount).ToList());
                var size = Range(random, 0.02f, 0.03f);
                var spread = Range(random, 0f, 0.06f);
                var angle = Range(random, 0f, Mathf.PI * 2f);
                coals.Add(Part(mesh, new Vector3(Mathf.Cos(angle) * spread, 0.235f + (0.06f - spread) * 0.35f, Mathf.Sin(angle) * spread),
                    Quaternion.Euler(Range(random, 0, 360), Range(random, 0, 360), 0), new Vector3(size * 1.3f, size * 0.7f, size)));
            }
            var ash = SankofaStoryMeshes.Box("Ash chamber", new Vector3(0.06f, 0.04f, 0.004f));
            ash.SetUVs(1, Enumerable.Repeat(new Vector2(1, 0.35f), ash.vertexCount).ToList());
            coals.Add(Part(ash, doorAt, wall, Vector3.one));
            var coalFilter = Static(jikoRoot, "Coals", Combine(coals, "Jiko coals"), art.Embers);
            SaveMeshes(new[] { bodyFilter, linerFilter, fittingFilter, cobFilter, coalFilter });
            var o = Staged(jikoRoot.gameObject, "jiko", "story.water.today", false, new[]
            {
                coalFilter.GetComponent<Renderer>(), bodyFilter.GetComponent<Renderer>(), linerFilter.GetComponent<Renderer>(),
                fittingFilter.GetComponent<Renderer>(), cobFilter.GetComponent<Renderer>(),
            });
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
            gateRoot.localPosition = new Vector3(0.6f, 0, 0.22f);
            // Turned to run away from the visitor's side, so it is seen along its length rather than as a wall.
            gateRoot.localRotation = Quaternion.Euler(0, -75, 0);
            // A low garden gate of thin, faded slats with a diagonal brace, open to the light: it stands at the
            // visitor's side, so it is no taller than a seated shoulder.
            var parts = new List<CombineInstance>
            {
                Part(SankofaStoryMeshes.Box("Gate post", new Vector3(0.06f, 1.0f, 0.06f)), new Vector3(-0.32f, 0.5f, 0), Quaternion.identity, Vector3.one),
                Part(SankofaStoryMeshes.Box("Gate post", new Vector3(0.06f, 1.0f, 0.06f)), new Vector3(0.32f, 0.5f, 0), Quaternion.identity, Vector3.one),
                Part(SankofaStoryMeshes.Box("Gate rail", new Vector3(0.58f, 0.045f, 0.025f)), new Vector3(0, 0.8f, 0), Quaternion.identity, Vector3.one),
                Part(SankofaStoryMeshes.Box("Gate rail", new Vector3(0.58f, 0.045f, 0.025f)), new Vector3(0, 0.3f, 0), Quaternion.identity, Vector3.one),
                Part(SankofaStoryMeshes.Box("Gate brace", new Vector3(0.025f, 0.66f, 0.02f)), new Vector3(0, 0.55f, 0.02f), Quaternion.Euler(0, 0, -52f), Vector3.one),
            };
            for (var i = 0; i < 7; i++)
                parts.Add(Part(SankofaStoryMeshes.Box("Gate slat", new Vector3(0.035f, 0.56f, 0.015f)), new Vector3(-0.24f + i * 0.08f, 0.55f, -0.012f), Quaternion.Euler(0, 0, (i % 3 - 1) * 1.2f), Vector3.one));
            var body = Static(gateRoot, "Body", Combine(parts, "Garden gate"), art.Paint);
            // The garden's fence runs on from the gate, away from the visitor: rough posts and two rails.
            var fence = new List<CombineInstance>();
            var random = new System.Random(1063);
            for (var k = 1; k <= 5; k++)
            {
                var x = 0.32f + k * 0.9f;
                var lean = Quaternion.Euler(Range(random, -3f, 3f), 0, Range(random, -4f, 4f));
                fence.Add(Part(SankofaStoryMeshes.Box("Fence post", new Vector3(0.06f, Range(random, 0.95f, 1.1f), 0.06f)), new Vector3(x, 0.5f, Range(random, -0.03f, 0.03f)), lean, Vector3.one));
            }
            foreach (var y in new[] { 0.4f, 0.85f })
                fence.Add(Part(SankofaStoryMeshes.Box("Fence rail", new Vector3(4.5f, 0.04f, 0.025f)), new Vector3(0.32f + 2.25f, y, 0.035f), Quaternion.Euler(0, 0, Range(random, -0.8f, 0.8f)), Vector3.one));
            var fenceFilter = Static(gateRoot, "Fence", Combine(fence, "Garden fence"), art.SeatWood);
            SaveMeshes(new[] { body, fenceFilter });
            var o = Staged(gateRoot.gameObject, "gate", "story.water.today", true, new[] { body.GetComponent<Renderer>() });
            Set(o, "touchPoint", Marker(gateRoot, "Touch point", new Vector3(-0.12f, 0.8f, 0.02f)));
            Set(o, "touchRadius", 0.12f);
            Set(o, "attentionRadius", 0.35f);
            Set(o, "placePoint", Marker(gateRoot, "Place point", new Vector3(-0.12f, 0.84f, -0.03f)));
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
