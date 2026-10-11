using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        // Presentation only: ecological clusters and signs of use, never selectable story objects.
        // Coordinates are authored in the stage frame. Keep the seat and all performer routes open.
        static List<MeshFilter> BuildUnderstory(Transform root, SankofaArtSet art, int seed,
            Func<Vector3, float> ground, Func<Vector3, bool> allow, int count, float outer, bool riverbank)
        {
            var random = new System.Random(seed);
            float R(float a, float b) => Range(random, a, b);
            var leaves = new List<CombineInstance>();
            var wood = new List<CombineInstance>();
            var rocks = new List<CombineInstance>();
            var litter = new List<CombineInstance>();
            var reedVertices = new List<Vector3>();
            var reedColors = new List<Color>();
            var reedUV = new List<Vector2>();
            var reedTriangles = new List<int>();
            for (var i = 0; i < count; i++)
            {
                Vector3 p = default;
                var found = false;
                for (var attempt = 0; attempt < 100; attempt++)
                {
                    var a = R(0, Mathf.PI * 2);
                    var distance = R(2.8f, outer); // More detail nearby, sparser toward the horizon.
                    p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * distance;
                    if (!allow(p) || SankofaMeshes.Fbm(p * 0.45f, seed, 2) < -0.08f) continue;
                    found = true;
                    break;
                }
                if (!found) continue;
                p.y = ground(p);
                var scale = R(0.3f, 0.7f);
                // A low woody shrub: individual sprigs around a centre, not a floating solid crown.
                for (var k = 0; k < 4; k++)
                {
                    var yaw = R(0, 360);
                    var tip = p + Quaternion.Euler(0, yaw, 0) * new Vector3(0, scale * R(0.45f, 0.9f), scale * 0.3f);
                    leaves.Add(Tinted(Part(SankofaMeshes.FoliageCluster(seed + i * 4 + k), tip,
                        Quaternion.Euler(R(-30, 30), yaw, R(-20, 20)), Vector3.one * scale),
                        (q, n) => new Color(1.1f, 1.25f, 0.85f)));
                }
                wood.Add(Part(SankofaMeshes.Log(seed + i, scale * 0.7f, scale * 0.025f, 0.3f, 0.02f, 5, 2),
                    p + Vector3.up * scale * 0.3f, Quaternion.Euler(0, R(0, 360), R(-15, 15)), Vector3.one));
                for (var k = 0; k < 4; k++)
                {
                    var at = p + new Vector3(R(-0.7f, 0.7f), 0, R(-0.7f, 0.7f));
                    at.y = ground(at) + 0.012f;
                    var leaf = new Mesh { name = "Fallen leaf" };
                    var size = R(0.03f, 0.08f);
                    leaf.vertices = new[] { new Vector3(-size * 0.35f, 0, 0), new Vector3(0, 0.01f, size), new Vector3(size * 0.35f, 0, 0), new Vector3(0, 0, -size) };
                    leaf.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                    leaf.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
                    var tone = Color.Lerp(new Color(0.26f, 0.18f, 0.08f), new Color(0.48f, 0.34f, 0.12f), R(0, 1));
                    leaf.colors = new[] { tone, tone, tone, tone };
                    leaf.uv2 = new[] { Vector2.one, Vector2.one, Vector2.one, Vector2.one };
                    leaf.RecalculateNormals();
                    litter.Add(Part(leaf, at, Quaternion.Euler(0, R(0, 360), 0), Vector3.one));
                }
            }
            // A few rock outcrops and fallen limbs break the empty plane at the clearing's edge.
            for (var i = 0; i < 18; i++)
            {
                var a = R(0, Mathf.PI * 2);
                var p = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * R(3.3f, outer);
                if (!allow(p)) continue;
                p.y = ground(p);
                var size = R(0.25f, 0.75f);
                rocks.Add(Part(SankofaMeshes.Stone(seed + 400 + i, 2, 0.22f, 0.35f), p + Vector3.up * size * 0.04f,
                    Quaternion.Euler(0, R(0, 360), 0), new Vector3(size * 1.4f, size * 0.65f, size)));
                if (i % 3 == 0)
                    wood.Add(Part(SankofaMeshes.Log(seed + 500 + i, R(1.2f, 2.1f), R(0.06f, 0.11f), 0.3f, 0.07f, 8, 5),
                        p + Vector3.up * 0.08f, Quaternion.Euler(90, R(0, 360), R(-6, 6)), Vector3.one));
            }
            if (riverbank)
            {
                // Patchy sedge-like blades follow the actual watercourse; leave the central exchange visible.
                for (var i = 0; i < 100; i++)
                {
                    var span = random.Next(1, StreamCourse.Length - 2);
                    var a = StreamCourse[span];
                    var b = StreamCourse[span + 1];
                    var direction = (b - a).normalized;
                    var side = new Vector3(-direction.z, 0, direction.x);
                    var p = Vector3.Lerp(a, b, R(0, 1)) + side * R(0.65f, 1.05f) * (i % 2 == 0 ? 1 : -1);
                    if (Mathf.Abs(p.x) < 1.9f || !allow(p)) continue;
                    p.y = ground(p);
                    for (var k = 0; k < 10; k++)
                    {
                        var yaw = R(0, Mathf.PI * 2);
                        var outward = new Vector3(Mathf.Cos(yaw), 0, Mathf.Sin(yaw));
                        var width = Vector3.Cross(Vector3.up, outward) * R(0.012f, 0.024f);
                        var h = R(0.3f, 0.75f);
                        var start = reedVertices.Count;
                        for (var s = 0; s < 4; s++)
                        {
                            var f = s / 4f;
                            var point = p + Vector3.up * h * f + outward * h * f * f * 0.4f;
                            var w = width * (1 - f * 0.8f);
                            reedVertices.Add(point - w); reedVertices.Add(point + w);
                            var tint = Color.Lerp(new Color(0.12f, 0.19f, 0.08f), new Color(0.4f, 0.48f, 0.22f), f);
                            reedColors.Add(tint); reedColors.Add(tint);
                            reedUV.Add(new Vector2(0, f)); reedUV.Add(new Vector2(1, f));
                            if (s > 0) reedTriangles.AddRange(new[] { start + s * 2 - 2, start + s * 2, start + s * 2 - 1, start + s * 2 - 1, start + s * 2, start + s * 2 + 1 });
                        }
                        reedVertices.Add(p + Vector3.up * h + outward * h * 0.4f);
                        reedColors.Add(new Color(0.48f, 0.52f, 0.27f)); reedUV.Add(new Vector2(0.5f, 1));
                        reedTriangles.AddRange(new[] { start + 6, start + 8, start + 7 });
                    }
                }
            }
            var filters = new List<MeshFilter>
            {
                Static(root, "Understory shrubs", Combine(leaves, root.name + " shrubs"), art.TreeCanopy),
                Static(root, "Fallen limbs", Combine(wood, root.name + " fallen limbs"), art.TreeBark),
                Static(root, "Edge outcrops", Combine(rocks, root.name + " outcrops"), art.Rock),
                Static(root, "Leaf litter", Combine(litter, root.name + " leaf litter"), art.Grass),
            };
            if (reedVertices.Count > 0)
            {
                var mesh = new Mesh { name = root.name + " bank sedges", indexFormat = IndexFormat.UInt32 };
                mesh.SetVertices(reedVertices); mesh.SetColors(reedColors); mesh.SetUVs(0, reedUV);
                var bake = new List<Vector2>();
                for (var i = 0; i < reedVertices.Count; i++) bake.Add(Vector2.one);
                mesh.SetUVs(1, bake); mesh.SetTriangles(reedTriangles, 0); mesh.RecalculateNormals();
                filters.Add(Static(root, "Bank sedges", mesh, art.Grass));
            }
            return filters;
        }

        static List<MeshFilter> BuildCampStores(Transform root, SankofaArtSet art)
        {
            var wood = new List<CombineInstance>();
            var canvas = new List<CombineInstance>();
            var metal = new List<CombineInstance>();
            // Storage off to the left: irregular stacked crates, slatted faces and a rough shelter.
            // No props on the women's routes or on the central view across the stream.
            var site = new Vector3(-5.1f, 0, 4.5f);
            site.y = BoundaryGround(site);
            for (var i = 0; i < 6; i++)
            {
                var p = site + new Vector3((i % 3) * 0.65f, (i / 3) * 0.48f + 0.24f, 0);
                wood.Add(Part(SankofaStoryMeshes.Box("Store crate", new Vector3(0.6f, 0.46f, 0.55f)), p, Quaternion.identity, Vector3.one));
                for (var k = 0; k < 5; k++)
                    wood.Add(Tinted(Part(SankofaStoryMeshes.Box("Crate face slat", new Vector3(0.55f, 0.068f, 0.035f)),
                        p + new Vector3(0, -0.18f + k * 0.09f, -0.29f), Quaternion.identity, Vector3.one), (q, n) => new Color(1.25f, 1.12f, 0.87f)));
            }
            foreach (var x in new[] { -0.5f, 2f })
                wood.Add(Part(SankofaMeshes.Log(4510, 1.65f, 0.035f, 0.1f, 0, 6, 2), site + new Vector3(x, 0.8f, 0.6f), Quaternion.identity, Vector3.one));
            canvas.Add(Part(SankofaStoryMeshes.Box("Store awning", new Vector3(2.8f, 0.025f, 1.8f)), site + new Vector3(0.75f, 1.6f, 0.65f), Quaternion.Euler(8, 0, -4), Vector3.one));
            for (var i = 0; i < 4; i++)
            {
                var p = site + new Vector3(2.2f + i * 0.3f, 0.28f, -0.2f + i % 2 * 0.5f);
                canvas.Add(Part(SankofaMeshes.Stone(4520 + i, 2, 0.09f, 0.15f), p, Quaternion.Euler(0, i * 30, 0), new Vector3(0.23f, 0.36f, 0.21f)));
            }
            for (var i = 0; i < 2; i++)
            {
                var p = new Vector3(4.7f + i * 0.6f, 0, 3.7f);
                p.y = BoundaryGround(p);
                var barrel = SankofaStoryMeshes.Lathe("Store barrel", new List<Vector2> { new Vector2(0, 0), new Vector2(0.21f, 0), new Vector2(0.26f, 0.3f), new Vector2(0.21f, 0.62f), new Vector2(0, 0.62f) }, 16, (a, y) => Color.white, 4530 + i);
                wood.Add(Part(barrel, p, Quaternion.identity, Vector3.one));
                foreach (var y in new[] { 0.06f, 0.31f, 0.56f })
                {
                    var r = y == 0.31f ? 0.265f : 0.23f;
                    var hoop = SankofaStoryMeshes.Lathe("Barrel hoop", new List<Vector2> { new Vector2(r, y - 0.014f), new Vector2(r, y + 0.014f) }, 16, (a, h) => Color.white, 4538);
                    metal.Add(Part(hoop, p, Quaternion.identity, Vector3.one));
                }
            }
            return new List<MeshFilter> { Static(root, "Camp store wood", Combine(wood, "Camp store wood"), art.Workbench),
                Static(root, "Camp store canvas", Combine(canvas, "Camp store canvas"), art.Canvas),
                Static(root, "Camp store hoops", Combine(metal, "Camp store hoops"), art.Steel) };
        }

        static List<MeshFilter> BuildCommunityBeds(Transform root, SankofaArtSet art)
        {
            var wood = new List<CombineInstance>();
            var soil = new List<CombineInstance>();
            var plants = new List<CombineInstance>();
            foreach (var x in new[] { -6f, -3.2f, 3.5f, 6.5f })
            {
                var p = new Vector3(x, 0, 5.3f + Mathf.Abs(x) * 0.18f);
                p.y = RiverGround(p);
                soil.Add(Part(SankofaStoryMeshes.Box("Garden soil", new Vector3(1.8f, 0.16f, 1.1f)), p + Vector3.up * 0.08f, Quaternion.identity, Vector3.one));
                foreach (var z in new[] { -0.59f, 0.59f })
                    wood.Add(Part(SankofaStoryMeshes.Box("Bed rim", new Vector3(2f, 0.22f, 0.08f)), p + new Vector3(0, 0.11f, z), Quaternion.identity, Vector3.one));
                foreach (var end in new[] { -0.96f, 0.96f })
                    wood.Add(Part(SankofaStoryMeshes.Box("Bed end", new Vector3(0.08f, 0.22f, 1.1f)), p + new Vector3(end, 0.11f, 0), Quaternion.identity, Vector3.one));
                for (var k = 0; k < 12; k++)
                    plants.Add(Part(SankofaMeshes.FoliageCluster(4600 + k), p + new Vector3(-0.7f + k % 4 * 0.46f, 0.33f, -0.33f + k / 4 * 0.33f), Quaternion.Euler(0, k * 73, 0), Vector3.one * 0.21f));
            }
            return new List<MeshFilter> { Static(root, "Community bed rims", Combine(wood, "Community bed rims"), art.Workbench),
                Static(root, "Community bed soil", Combine(soil, "Community bed soil"), art.Soil),
                Static(root, "Community planting", Combine(plants, "Community planting"), art.TreeCanopy) };
        }
    }
}
