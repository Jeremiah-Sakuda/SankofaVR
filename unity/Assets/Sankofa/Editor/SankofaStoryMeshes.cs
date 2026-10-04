using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Deterministic procedural meshes for the placeholder story scenes: ground around the visitor, earthen walls,
    /// turned pots and the chipped bowl with the piece that fits it. Like the hearth's meshes, they are generated
    /// and saved as assets; the same seed always gives the same shape.
    /// </summary>
    public static class SankofaStoryMeshes
    {
        static Mesh Finish(Mesh mesh, string name)
        {
            mesh.name = name;
            mesh.RecalculateNormals();
            if (mesh.uv != null && mesh.uv.Length == mesh.vertexCount) mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return mesh;
        }

        static List<Color> Fill(int count, Color c)
        {
            var list = new List<Color>(count);
            for (var i = 0; i < count; i++) list.Add(c);
            return list;
        }

        // ---- ground ----

        /// <summary>
        /// A disc of ground centred on the visitor, fine nearby and coarse far away. Height, tint and the burn
        /// weight the ground shader blends by (UV.x) come from the scene; baking adds occlusion and visibility.
        /// </summary>
        public static Mesh Ground(string name, Func<Vector3, float> height, Func<Vector3, Color> tint, Func<Vector3, float> burn, float near = 4f, float far = 80f)
        {
            const int segments = 144;
            var radii = new List<float>();
            for (var r = 0.06f; r < near; r += 0.06f) radii.Add(r);
            for (var r = near; r < far; r *= 1.1f) radii.Add(r);

            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var weights = new List<Vector2>();
            var triangles = new List<int>();

            void Add(float x, float z)
            {
                var at = new Vector3(x, 0, z);
                at.y = height(at);
                vertices.Add(at);
                var c = tint(at);
                c.a = 1;
                colors.Add(c);
                weights.Add(new Vector2(Mathf.Clamp01(burn(at)), 0));
            }

            Add(0, 0);
            foreach (var r in radii)
                for (var s = 0; s < segments; s++)
                {
                    var angle = s * Mathf.PI * 2f / segments;
                    Add(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
                }
            for (var s = 0; s < segments; s++)
                triangles.AddRange(new[] { 0, 1 + (s + 1) % segments, 1 + s });
            for (var ring = 0; ring < radii.Count - 1; ring++)
            {
                var inner = 1 + ring * segments;
                var outer = inner + segments;
                for (var s = 0; s < segments; s++)
                {
                    var n = (s + 1) % segments;
                    triangles.AddRange(new[] { inner + s, inner + n, outer + s, inner + n, outer + n, outer + s });
                }
            }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, weights);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(1, new List<Vector2>(new Vector2[vertices.Count]));
            return Finish(mesh, name);
        }

        // ---- earthen walls ----

        /// <summary>
        /// A hand-built earthen wall along a path of ground points: thick, slightly leaning, with a rounded,
        /// uneven top. Faces carry no UVs; the wall's material projects its texture from world space.
        /// </summary>
        public static Mesh Wall(string name, IList<Vector3> path, float height, float thickness, int seed, Func<Vector3, float> ground)
        {
            var points = new List<Vector3>();
            for (var i = 0; i < path.Count - 1; i++)
            {
                var steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(path[i], path[i + 1]) / 0.2f));
                for (var k = 0; k < steps; k++) points.Add(Vector3.Lerp(path[i], path[i + 1], (float)k / steps));
            }
            points.Add(path[path.Count - 1]);

            // Cross-section around the wall's top, from the outer foot over the crown to the inner foot.
            var across = new[] { -1f, -1f, -0.7f, 0f, 0.7f, 1f, 1f };
            var up = new[] { -0.15f, 0.82f, 0.96f, 1.02f, 0.96f, 0.82f, -0.15f };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            for (var i = 0; i < points.Count; i++)
            {
                var forward = (points[Mathf.Min(i + 1, points.Count - 1)] - points[Mathf.Max(i - 1, 0)]);
                forward.y = 0;
                forward.Normalize();
                var side = new Vector3(forward.z, 0, -forward.x);
                var p = points[i];
                var baseY = ground(p);
                var h = height * (1f + SankofaMeshes.Fbm(p * 1.3f, seed, 3) * 0.22f);
                var t = thickness * (1f + SankofaMeshes.Fbm(p * 2.1f, seed + 5, 2) * 0.3f);
                for (var k = 0; k < across.Length; k++)
                {
                    // Earth walls batter inward as they rise.
                    var rise = Mathf.Clamp01(up[k]);
                    var width = t * 0.5f * across[k] * (1f - 0.18f * rise);
                    var bulge = SankofaMeshes.Fbm(p * 3.7f + Vector3.up * up[k] * 2f, seed + 11, 2) * 0.03f;
                    vertices.Add(p + side * (width + bulge * Mathf.Sign(across[k])) + Vector3.up * (baseY + up[k] * h));
                }
            }
            var n = across.Length;
            for (var i = 0; i < points.Count - 1; i++)
                for (var k = 0; k < n - 1; k++)
                {
                    var a = i * n + k;
                    var b = a + n;
                    // (along × across) points out of the wall on both faces and up over the crown.
                    triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            foreach (var (row, flip) in new[] { (0, false), (points.Count - 1, true) })
            {
                var start = row * n;
                for (var k = 1; k < n - 2; k++)
                    triangles.AddRange(flip ? new[] { start, start + k + 1, start + k } : new[] { start, start + k, start + k + 1 });
            }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(Fill(vertices.Count, Color.white));
            mesh.SetUVs(1, new List<Vector2>(new Vector2[vertices.Count]));
            return Finish(mesh, name);
        }

        // ---- turned clay ----

        /// <summary>A wheel-turned or coiled vessel: a closed profile (radius, height) revolved about the vertical.</summary>
        public static Mesh Lathe(string name, IList<Vector2> profile, int segments, Func<float, float, Color> tint, int seed)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var wobbleSeed = new Vector3(seed * 0.37f, 0, seed * 0.91f);
            for (var k = 0; k < profile.Count; k++)
                for (var s = 0; s <= segments; s++)
                {
                    var angle = s * Mathf.PI * 2f / segments;
                    var p = profile[k];
                    // Hand-made: the wall is never perfectly round.
                    var r = p.x * (1f + SankofaMeshes.Fbm(new Vector3(Mathf.Cos(angle), p.y * 6f, Mathf.Sin(angle)) + wobbleSeed, seed, 2) * 0.04f);
                    vertices.Add(new Vector3(Mathf.Cos(angle) * r, p.y, Mathf.Sin(angle) * r));
                    uvs.Add(new Vector2((float)s / segments, (float)k / (profile.Count - 1)));
                    colors.Add(tint(angle, p.y));
                }
            for (var k = 0; k < profile.Count - 1; k++)
                for (var s = 0; s < segments; s++)
                {
                    var a = k * (segments + 1) + s;
                    var b = a + segments + 1;
                    triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(1, new List<Vector2>(new Vector2[vertices.Count]));
            return Finish(mesh, name);
        }

        /// <summary>A simple jar profile: foot, swelling body, shoulder, neck and lip, with its inside.</summary>
        public static List<Vector2> JarProfile(float height, float belly, float neck, float lip)
        {
            var p = new List<Vector2> { new Vector2(0, 0) };
            const int steps = 14;
            for (var i = 0; i <= steps; i++)
            {
                var t = (float)i / steps;
                var y = t * height;
                var swell = Mathf.Sin(Mathf.Clamp01(t / 0.82f) * Mathf.PI);
                var r = Mathf.Lerp(belly * 0.45f, neck, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.55f, 0.9f, t))) + belly * 0.55f * swell * (1f - Mathf.InverseLerp(0.62f, 0.85f, t));
                if (t > 0.9f) r = Mathf.Lerp(neck, lip, Mathf.InverseLerp(0.9f, 1f, t));
                p.Add(new Vector2(r, y));
            }
            p.Add(new Vector2(lip * 0.86f, height * 0.995f));
            p.Add(new Vector2(neck * 0.8f, height * 0.9f));
            p.Add(new Vector2(0, height * 0.88f));
            return p;
        }

        // ---- the chipped bowl and its piece ----

        public const float BowlHeight = 0.088f;
        const float WallThickness = 0.0065f;
        const float FloorHeight = 0.012f;
        /// <summary>The missing piece spans these angles around the bowl (radians, in the bowl's own frame).</summary>
        public const float ChipFrom = -2.32f, ChipTo = -1.66f;

        static float OuterRadius(float y) => 0.047f + 0.064f * Mathf.Sin(Mathf.PI * 0.5f * Mathf.Pow(Mathf.Clamp01(y / BowlHeight), 0.72f));
        static float InnerRadius(float y) => Mathf.Max(0.004f, OuterRadius(y) - WallThickness);

        /// <summary>The broken edge's height at an angle: a ragged line below the rim inside the chip.</summary>
        static float BreakHeight(float angle)
        {
            var t = Mathf.InverseLerp(ChipFrom, ChipTo, angle);
            var depth = Mathf.Sin(t * Mathf.PI);
            return BowlHeight - 0.026f * Mathf.Pow(depth, 0.5f) + SankofaMeshes.Fbm(new Vector3(angle * 9f, 0, 0), 77, 2) * 0.006f * depth;
        }

        /// <summary>
        /// The painted line: a band of dark pigment just under the rim, not quite even, as a brush going around
        /// by hand leaves it. Darker at the foot; a little lighter at the lip.
        /// </summary>
        static Color BowlTint(float angle, float y)
        {
            var clay = new Color(0.94f, 0.86f, 0.78f);
            var centre = 0.068f + 0.0026f * Mathf.Sin(angle * 3f + 0.7f) + SankofaMeshes.Fbm(new Vector3(angle * 4f, 1, 0), 31, 2) * 0.004f;
            var half = 0.0042f + SankofaMeshes.Fbm(new Vector3(angle * 7f, 3, 0), 33, 2) * 0.0016f;
            var band = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(half * 0.7f, half * 1.25f, Mathf.Abs(y - centre)));
            var pigment = new Color(0.2f, 0.11f, 0.07f);
            var c = Color.Lerp(clay, pigment, band * 0.92f);
            c *= Mathf.Lerp(0.78f, 1f, Mathf.InverseLerp(0f, 0.02f, y));
            c.a = 1;
            return c;
        }

        /// <summary>One closed cross-section of the bowl wall at an angle, from the foot (or a break) over the rim.</summary>
        static List<Vector2> Section(float from, bool closed)
        {
            const int steps = 16;
            var outer = new List<Vector2>();
            for (var i = 0; i <= steps; i++)
            {
                var y = Mathf.Lerp(from, BowlHeight, (float)i / steps);
                outer.Add(new Vector2(OuterRadius(y), y));
            }
            var inner = new List<Vector2>();
            var innerFrom = Mathf.Max(from, FloorHeight);
            for (var i = steps; i >= 0; i--)
            {
                var y = Mathf.Lerp(innerFrom, BowlHeight + 0.002f, (float)i / steps);
                inner.Add(new Vector2(InnerRadius(y), y));
            }
            var section = new List<Vector2>();
            if (closed) section.Add(new Vector2(0, 0));
            section.AddRange(outer);
            section.AddRange(inner);
            if (closed) section.Add(new Vector2(0, FloorHeight));
            return section;
        }

        /// <summary>
        /// The bowl, turned around its vertical axis, with a piece of its rim broken away between
        /// <see cref="ChipFrom"/> and <see cref="ChipTo"/>. Every cross-section has the same number of points, so a
        /// chipped section simply folds its upper points down onto the broken edge.
        /// </summary>
        public static Mesh ChippedBowl(string name, int segments)
        {
            var full = Section(0, true);
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            for (var s = 0; s <= segments; s++)
            {
                var angle = -Mathf.PI + s * Mathf.PI * 2f / segments;
                var chipped = angle > ChipFrom && angle < ChipTo;
                var cut = chipped ? BreakHeight(angle) : float.MaxValue;
                for (var k = 0; k < full.Count; k++)
                {
                    var p = full[k];
                    if (p.y > cut)
                    {
                        // Fold the wall above the break onto the broken edge, from the outside face to the inside.
                        var outerSide = Mathf.Abs(p.x - OuterRadius(p.y)) < WallThickness * 0.5f;
                        p = new Vector2(outerSide ? OuterRadius(cut) : InnerRadius(cut), cut);
                    }
                    vertices.Add(new Vector3(Mathf.Cos(angle) * p.x, p.y, Mathf.Sin(angle) * p.x));
                    uvs.Add(new Vector2((float)s / segments, (float)k / (full.Count - 1)));
                    colors.Add(BowlTint(angle, p.y));
                }
            }
            var n = full.Count;
            for (var s = 0; s < segments; s++)
                for (var k = 0; k < n - 1; k++)
                {
                    var a = s * n + k;
                    var b = a + n;
                    triangles.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                }
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(1, Fill2(vertices.Count, new Vector2(1, 1)));
            return Finish(mesh, name);
        }

        /// <summary>
        /// The piece that broke away, in the bowl's own frame minus <paramref name="centre"/>, so that set at the
        /// bowl's position plus the centre it fills the chip exactly. Its sides are closed so it reads as a solid shard.
        /// </summary>
        public static Mesh Shard(string name, out Vector3 centre)
        {
            const int arcs = 12;
            var sections = new List<List<Vector3>>();
            var tints = new List<List<Color>>();
            var sum = Vector3.zero;
            var count = 0;
            for (var s = 0; s <= arcs; s++)
            {
                // A hair inside the chip, so the mended bowl shows its seam.
                var angle = Mathf.Lerp(ChipFrom + 0.012f, ChipTo - 0.012f, (float)s / arcs);
                var cut = BreakHeight(angle) + 0.0008f;
                var section = Section(cut, false);
                var ring = new List<Vector3>();
                var tint = new List<Color>();
                foreach (var p in section)
                {
                    var v = new Vector3(Mathf.Cos(angle) * p.x, p.y, Mathf.Sin(angle) * p.x);
                    ring.Add(v);
                    tint.Add(BowlTint(angle, p.y));
                    sum += v;
                    count++;
                }
                sections.Add(ring);
                tints.Add(tint);
            }
            centre = sum / count;
            var vertices = new List<Vector3>();
            var colors = new List<Color>();
            var triangles = new List<int>();
            var n = sections[0].Count;
            foreach (var ring in sections) foreach (var v in ring) vertices.Add(v - centre);
            foreach (var t in tints) colors.AddRange(t);
            // The wall itself, and the broken bottom edge closing the section (last point back to the first).
            for (var s = 0; s < arcs; s++)
            {
                for (var k = 0; k < n; k++)
                {
                    var a = s * n + k;
                    var a1 = s * n + (k + 1) % n;
                    var b = a + n;
                    var b1 = a1 + n;
                    triangles.AddRange(new[] { a, a1, b, a1, b1, b });
                }
            }
            // The two broken ends.
            foreach (var (s, flip) in new[] { (0, true), (arcs, false) })
            {
                var start = s * n;
                var centreIndex = vertices.Count;
                var c = Vector3.zero;
                for (var k = 0; k < n; k++) c += vertices[start + k];
                vertices.Add(c / n);
                colors.Add(new Color(0.72f, 0.5f, 0.38f));
                for (var k = 0; k < n; k++)
                {
                    var k1 = (k + 1) % n;
                    triangles.AddRange(flip ? new[] { centreIndex, start + k1, start + k } : new[] { centreIndex, start + k, start + k1 });
                }
            }
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(1, Fill2(vertices.Count, new Vector2(1, 1)));
            return Finish(mesh, name);
        }

        static List<Vector2> Fill2(int count, Vector2 v)
        {
            var list = new List<Vector2>(count);
            for (var i = 0; i < count; i++) list.Add(v);
            return list;
        }
    }
}
