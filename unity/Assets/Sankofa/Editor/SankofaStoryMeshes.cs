using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Deterministic procedural meshes for the placeholder story scenes: ground around the visitor, earthen walls,
    /// turned vessels, water, the plain's acacias, camp props and the ember-figure silhouettes. Like the hearth's meshes, they are generated
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

        // ---- water ----

        /// <summary>
        /// A ribbon of water along a path of points, flat across its width. UV.x runs across and UV.y along the
        /// path in metres, so the water shader's ripples scroll downstream at a constant speed.
        /// </summary>
        public static Mesh Ribbon(string name, IList<Vector3> path, float width, Vector3 up)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var along = 0f;
            for (var i = 0; i < path.Count; i++)
            {
                var forward = (path[Mathf.Min(i + 1, path.Count - 1)] - path[Mathf.Max(i - 1, 0)]).normalized;
                var side = Vector3.Cross(up, forward).normalized * (width * 0.5f);
                if (i > 0) along += Vector3.Distance(path[i - 1], path[i]);
                vertices.Add(path[i] - side);
                vertices.Add(path[i] + side);
                uvs.Add(new Vector2(0, along));
                uvs.Add(new Vector2(1, along));
                if (i == 0) continue;
                var a = (i - 1) * 2;
                // Faces toward "up" for a stream, toward the visitor for a falling sheet.
                triangles.AddRange(new[] { a, a + 2, a + 1, a + 1, a + 2, a + 3 });
            }
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            return Finish(mesh, name);
        }

        /// <summary>Points along a smooth curve through the given points (Catmull-Rom), about <paramref name="spacing"/> apart.</summary>
        public static List<Vector3> Smooth(IList<Vector3> points, float spacing)
        {
            var result = new List<Vector3>();
            for (var i = 0; i < points.Count - 1; i++)
            {
                var p0 = points[Mathf.Max(i - 1, 0)];
                var p1 = points[i];
                var p2 = points[i + 1];
                var p3 = points[Mathf.Min(i + 2, points.Count - 1)];
                var steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(p1, p2) / spacing));
                for (var k = 0; k < steps; k++)
                {
                    var t = (float)k / steps;
                    result.Add(0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t * t + (-p0 + 3f * p1 - 3f * p2 + p3) * t * t * t));
                }
            }
            result.Add(points[points.Count - 1]);
            return result;
        }

        // ---- the plain and the camp ----

        /// <summary>A flat-topped acacia of the plain: a short forked trunk under a wide, thin, layered crown.</summary>
        public static void Acacia(int seed, float height, out Mesh trunk, out Mesh crown)
        {
            var random = new System.Random(seed);
            float R(float a, float b) => a + (float)random.NextDouble() * (b - a);
            var trunks = new List<CombineInstance>();
            var crowns = new List<CombineInstance>();
            var fork = height * R(0.45f, 0.55f);
            trunks.Add(new CombineInstance { mesh = SankofaMeshes.Log(seed, fork, height * 0.035f, 0.3f, height * 0.03f, 8, 5), transform = Matrix4x4.Translate(new Vector3(0, fork * 0.5f, 0)) });
            var limbs = 2 + random.Next(2);
            for (var b = 0; b < limbs; b++)
            {
                var yaw = b * 360f / limbs + R(-30, 30);
                var rotation = Quaternion.Euler(0, yaw, 0) * Quaternion.Euler(R(28, 45), 0, 0);
                var length = height * R(0.45f, 0.6f);
                trunks.Add(new CombineInstance { mesh = SankofaMeshes.Log(seed * 7 + b, length, height * 0.02f, 0.5f, length * 0.05f, 6, 4), transform = Matrix4x4.TRS(new Vector3(0, fork, 0) + rotation * (Vector3.up * length * 0.5f), rotation, Vector3.one) });
                var tip = new Vector3(0, fork, 0) + rotation * (Vector3.up * length);
                for (var k = 0; k < 3; k++)
                {
                    var size = height * R(0.32f, 0.45f);
                    var at = tip + new Vector3(R(-0.4f, 0.4f) * size, R(-0.05f, 0.08f) * size, R(-0.4f, 0.4f) * size);
                    crowns.Add(new CombineInstance { mesh = SankofaMeshes.Stone(seed * 31 + b * 5 + k, 2, 0.3f, 10f), transform = Matrix4x4.TRS(at, Quaternion.Euler(0, R(0, 360), 0), new Vector3(size, size * 0.18f, size * R(0.8f, 1f))) });
                }
            }
            trunk = CombineMeshes(trunks, "Acacia trunk " + seed);
            crown = CombineMeshes(crowns, "Acacia crown " + seed);
        }

        static Mesh CombineMeshes(List<CombineInstance> parts, string name)
        {
            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.CombineMeshes(parts.ToArray(), true, true);
            mesh.SetColors(Fill(mesh.vertexCount, Color.white));
            mesh.SetUVs(1, new List<Vector2>(new Vector2[mesh.vertexCount]));
            return Finish(mesh, name);
        }

        /// <summary>A box with per-face UVs in metres, for timber, rails, a gate and a raised bed.</summary>
        public static Mesh Box(string name, Vector3 size)
        {
            var mesh = Object.Instantiate(Resources.GetBuiltinResource<Mesh>("Cube.fbx"));
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var uvs = new Vector2[vertices.Length];
            for (var i = 0; i < vertices.Length; i++)
            {
                var v = Vector3.Scale(vertices[i], size);
                vertices[i] = v;
                var n = normals[i];
                uvs[i] = Mathf.Abs(n.x) > 0.5f ? new Vector2(v.z, v.y) : Mathf.Abs(n.y) > 0.5f ? new Vector2(v.x, v.z) : new Vector2(v.x, v.y);
            }
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.SetColors(Fill(vertices.Length, Color.white));
            mesh.SetUVs(1, Fill2(vertices.Length, new Vector2(1, 1)));
            return Finish(mesh, name);
        }

        /// <summary>A gourd: a full bulb narrowing to a neck (bottle gourd), as a closed lathe profile.</summary>
        public static List<Vector2> GourdProfile(float height, float bulb, float neck)
        {
            var p = new List<Vector2> { new Vector2(0, 0) };
            const int steps = 16;
            for (var i = 0; i <= steps; i++)
            {
                var t = (float)i / steps;
                // A round bulb over the lower 60%, then a neck that narrows to the mouth.
                var r = t < 0.6f ? bulb * Mathf.Sin(Mathf.PI * Mathf.Lerp(0.12f, 0.9f, t / 0.6f))
                    : Mathf.Lerp(bulb * Mathf.Sin(Mathf.PI * 0.9f), neck, Mathf.SmoothStep(0f, 1f, (t - 0.6f) / 0.4f));
                p.Add(new Vector2(Mathf.Max(r, neck * 0.9f), t * height));
            }
            p.Add(new Vector2(0, height));
            return p;
        }

        /// <summary>A bell tent of canvas: a steep cone flaring to a short wall.</summary>
        public static List<Vector2> TentProfile(float radius, float height) => new List<Vector2>
        {
            new Vector2(radius, 0), new Vector2(radius, height * 0.18f), new Vector2(radius * 0.92f, height * 0.25f),
            new Vector2(radius * 0.5f, height * 0.6f), new Vector2(radius * 0.12f, height * 0.94f), new Vector2(0, height),
        };

        /// <summary>A charcoal stove (jiko): a waisted metal body with a bowl on top for the coals.</summary>
        public static List<Vector2> JikoProfile(float height, float radius) => new List<Vector2>
        {
            new Vector2(0, 0), new Vector2(radius * 0.8f, 0), new Vector2(radius * 0.7f, height * 0.45f), new Vector2(radius * 0.55f, height * 0.55f),
            new Vector2(radius, height * 0.95f), new Vector2(radius * 1.02f, height), new Vector2(radius * 0.9f, height), new Vector2(0, height * 0.8f),
        };

        // ---- people and animals, as shapes of light ----

        /// <summary>
        /// A woman standing in a cloth wrapped from the shoulders, as one continuous silhouette for the additive
        /// ember-figure shader: body, neck and head are a single surface so nothing overlaps and doubles in brightness,
        /// flattened front to back like a body rather than a pillar. No face, hands or ornament is modelled, so no
        /// likeness is ever drawn. The arms hang just clear of the body so their light does not pile onto it.
        /// </summary>
        public static Mesh Figure(string name, int seed, float height)
        {
            var h = height;
            var body = Lathe(name + " body", new List<Vector2>
            {
                new Vector2(0, 0), new Vector2(h * 0.105f, h * 0.008f), new Vector2(h * 0.115f, h * 0.2f), new Vector2(h * 0.1f, h * 0.45f),
                new Vector2(h * 0.105f, h * 0.58f), new Vector2(h * 0.125f, h * 0.7f), new Vector2(h * 0.135f, h * 0.775f),
                new Vector2(h * 0.105f, h * 0.81f), new Vector2(h * 0.05f, h * 0.83f), new Vector2(h * 0.034f, h * 0.85f),
                new Vector2(h * 0.036f, h * 0.87f), new Vector2(h * 0.054f, h * 0.895f), new Vector2(h * 0.06f, h * 0.935f),
                new Vector2(h * 0.052f, h * 0.975f), new Vector2(h * 0.028f, h * 0.995f), new Vector2(0, h),
            }, 24, (a, y) => Color.white, seed);
            // Front to back the body is about two thirds of its width; the head stays nearly round.
            var vertices = body.vertices;
            for (var i = 0; i < vertices.Length; i++)
            {
                var depth = Mathf.Lerp(0.66f, 0.92f, Mathf.InverseLerp(h * 0.82f, h * 0.88f, vertices[i].y));
                vertices[i].z *= depth;
            }
            body.vertices = vertices;
            body.RecalculateNormals();
            var parts = new List<CombineInstance> { new CombineInstance { mesh = body, transform = Matrix4x4.identity } };
            foreach (var side in new[] { -1f, 1f })
            {
                var arm = Lathe(name + " arm", new List<Vector2>
                {
                    new Vector2(0, 0), new Vector2(h * 0.016f, h * 0.01f), new Vector2(h * 0.019f, h * 0.12f),
                    new Vector2(h * 0.024f, h * 0.27f), new Vector2(h * 0.02f, h * 0.31f), new Vector2(0, h * 0.32f),
                }, 10, (a, y) => Color.white, seed + 3);
                // Hanging from the shoulder, hand a little forward and out from the hip.
                parts.Add(new CombineInstance { mesh = arm, transform = Matrix4x4.TRS(new Vector3(side * h * 0.163f, h * 0.46f, h * 0.02f), Quaternion.Euler(4, 0, side * 4f), Vector3.one) });
            }
            var mesh = CombineMeshes(parts, name);
            // Vertex colour tells the figure shader which arm a vertex belongs to (red: the arm on −X, green: +X), so it
            // can swing each arm from the shoulder to hold, offer or present without a skeleton.
            var bodyCount = body.vertexCount;
            var armCount = (mesh.vertexCount - bodyCount) / 2;
            var colors = new List<Color>(mesh.vertexCount);
            for (var i = 0; i < mesh.vertexCount; i++)
                colors.Add(i < bodyCount ? new Color(0, 0, 0, 1) : i < bodyCount + armCount ? new Color(1, 0, 0, 1) : new Color(0, 1, 0, 1));
            mesh.SetColors(colors);
            return mesh;
        }

        /// <summary>A ewe on its four legs, as a simple rounded silhouette.</summary>
        public static Mesh Ewe(string name, int seed)
        {
            var parts = new List<CombineInstance>();
            Mesh Round(int s) => SankofaMeshes.Stone(s, 2, 0.08f, 10f);
            parts.Add(new CombineInstance { mesh = Round(seed), transform = Matrix4x4.TRS(new Vector3(0, 0.5f, 0), Quaternion.identity, new Vector3(0.2f, 0.17f, 0.36f)) });
            parts.Add(new CombineInstance { mesh = Round(seed + 1), transform = Matrix4x4.TRS(new Vector3(0, 0.63f, 0.38f), Quaternion.Euler(30, 0, 0), new Vector3(0.07f, 0.08f, 0.13f)) });
            foreach (var (x, z) in new[] { (-0.1f, 0.22f), (0.1f, 0.22f), (-0.1f, -0.22f), (0.1f, -0.22f) })
                parts.Add(new CombineInstance { mesh = SankofaMeshes.Log(seed + 5, 0.42f, 0.022f, 0.3f, 0.005f, 6, 2), transform = Matrix4x4.TRS(new Vector3(x, 0.21f, z), Quaternion.identity, Vector3.one) });
            var mesh = CombineMeshes(parts, name);
            // No arms: black vertex colour leaves the figure shader's arm swing untouched.
            mesh.SetColors(Fill(mesh.vertexCount, new Color(0, 0, 0, 1)));
            return mesh;
        }

        static List<Vector2> Fill2(int count, Vector2 v)
        {
            var list = new List<Vector2>(count);
            for (var i = 0; i < count; i++) list.Add(v);
            return list;
        }
    }
}
