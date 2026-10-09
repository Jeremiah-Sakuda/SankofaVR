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
                // Metres both ways, so ripples are round rather than stretched across a wide river.
                uvs.Add(new Vector2(0, along));
                uvs.Add(new Vector2(width, along));
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

        /// <summary>
        /// The sheet-metal cladding of a ceramic charcoal stove (jiko), 0.26 m tall: a flared base and ash chamber,
        /// a pinched waist, the bowl flaring to a rolled lip, and the inside of the lip turning down to meet the liner.
        /// </summary>
        public static List<Vector2> JikoCladding() => new List<Vector2>
        {
            new Vector2(0, 0.002f), new Vector2(0.104f, 0f), new Vector2(0.114f, 0.012f), new Vector2(0.108f, 0.02f),
            new Vector2(0.088f, 0.1f), new Vector2(0.084f, 0.118f), new Vector2(0.09f, 0.135f), new Vector2(0.122f, 0.215f),
            new Vector2(0.134f, 0.243f), new Vector2(0.139f, 0.25f), new Vector2(0.135f, 0.256f), new Vector2(0.126f, 0.252f),
            new Vector2(0.122f, 0.23f),
        };

        /// <summary>The fired-clay liner that holds the coals: its thick rim stands proud of the metal, its bowl narrows to the grate.</summary>
        public static List<Vector2> JikoLiner() => new List<Vector2>
        {
            new Vector2(0.123f, 0.226f), new Vector2(0.125f, 0.262f), new Vector2(0.118f, 0.27f), new Vector2(0.104f, 0.268f),
            new Vector2(0.094f, 0.25f), new Vector2(0.074f, 0.215f), new Vector2(0.05f, 0.2f), new Vector2(0f, 0.197f),
        };

        /// <summary>
        /// A flat-bottomed rail section of the 1890s, about 0.1 m tall, in the (z, y) plane: foot, web, head and a
        /// slightly crowned running surface. Counter-clockwise, for <see cref="Extrude"/>.
        /// </summary>
        public static List<Vector2> RailSection() => new List<Vector2>
        {
            new Vector2(-0.045f, 0f), new Vector2(0.045f, 0f), new Vector2(0.045f, 0.006f), new Vector2(0.013f, 0.019f),
            new Vector2(0.0075f, 0.03f), new Vector2(0.0075f, 0.064f), new Vector2(0.013f, 0.07f), new Vector2(0.025f, 0.074f),
            new Vector2(0.025f, 0.091f), new Vector2(0.019f, 0.098f), new Vector2(0f, 0.1f), new Vector2(-0.019f, 0.098f),
            new Vector2(-0.025f, 0.091f), new Vector2(-0.025f, 0.074f), new Vector2(-0.013f, 0.07f), new Vector2(-0.0075f, 0.064f),
            new Vector2(-0.0075f, 0.03f), new Vector2(-0.013f, 0.019f), new Vector2(-0.045f, 0.006f),
        };

        /// <summary>
        /// A section (z, y), counter-clockwise, extruded along X over the given length and centred on the origin, with
        /// flat sides and capped ends. UVs run in metres along the length and around the section.
        /// </summary>
        public static Mesh Extrude(string name, IList<Vector2> section, float length)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var half = length * 0.5f;
            void Face(int a, int b, int c, Vector3 outward)
            {
                // Unity's front face is the one whose normal is Cross(b - a, c - a).
                var n = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
                if (Vector3.Dot(n, outward) >= 0) triangles.AddRange(new[] { a, b, c });
                else triangles.AddRange(new[] { a, c, b });
            }
            var around = 0f;
            for (var k = 0; k < section.Count; k++)
            {
                var p = section[k];
                var q = section[(k + 1) % section.Count];
                var edge = q - p;
                var outward = new Vector3(0, -edge.x, edge.y).normalized;
                var i = vertices.Count;
                vertices.Add(new Vector3(-half, p.y, p.x));
                vertices.Add(new Vector3(-half, q.y, q.x));
                vertices.Add(new Vector3(half, q.y, q.x));
                vertices.Add(new Vector3(half, p.y, p.x));
                var next = around + edge.magnitude;
                uvs.Add(new Vector2(-half, around));
                uvs.Add(new Vector2(-half, next));
                uvs.Add(new Vector2(half, next));
                uvs.Add(new Vector2(half, around));
                around = next;
                Face(i, i + 1, i + 2, outward);
                Face(i, i + 2, i + 3, outward);
            }
            var cap = EarClip(section);
            foreach (var side in new[] { -1f, 1f })
            {
                var start = vertices.Count;
                foreach (var p in section)
                {
                    vertices.Add(new Vector3(side * half, p.y, p.x));
                    uvs.Add(new Vector2(p.x, p.y));
                }
                for (var t = 0; t < cap.Count; t += 3)
                    Face(start + cap[t], start + cap[t + 1], start + cap[t + 2], new Vector3(side, 0, 0));
            }
            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.SetColors(Fill(vertices.Count, Color.white));
            mesh.SetUVs(1, Fill2(vertices.Count, new Vector2(1, 1)));
            return Finish(mesh, name);
        }

        /// <summary>Triangulates a simple counter-clockwise polygon by clipping ears; returns index triples.</summary>
        static List<int> EarClip(IList<Vector2> polygon)
        {
            var remaining = new List<int>();
            for (var i = 0; i < polygon.Count; i++) remaining.Add(i);
            var result = new List<int>();
            float Cross(Vector2 a, Vector2 b, Vector2 c) => (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
            var guard = 0;
            while (remaining.Count > 3 && guard++ < 10000)
            {
                for (var i = 0; i < remaining.Count; i++)
                {
                    int ia = remaining[(i + remaining.Count - 1) % remaining.Count], ib = remaining[i], ic = remaining[(i + 1) % remaining.Count];
                    Vector2 a = polygon[ia], b = polygon[ib], c = polygon[ic];
                    if (Cross(a, b, c) <= 1e-9f) continue;
                    var inside = false;
                    foreach (var j in remaining)
                    {
                        if (j == ia || j == ib || j == ic) continue;
                        var p = polygon[j];
                        if (Cross(a, b, p) >= 0 && Cross(b, c, p) >= 0 && Cross(c, a, p) >= 0) { inside = true; break; }
                    }
                    if (inside) continue;
                    result.AddRange(new[] { ia, ib, ic });
                    remaining.RemoveAt(i);
                    break;
                }
            }
            if (remaining.Count == 3) result.AddRange(remaining);
            return result;
        }

        // ---- people and animals, as shapes of light ----

        /// <summary>
        /// A woman standing in a cloth wrapped from the shoulders, as one continuous silhouette for the additive
        /// ember-figure shader: body, neck and head are a single surface so nothing overlaps and doubles in brightness,
        /// flattened front to back like a body rather than a pillar, the wrap flaring a little at the hem. No face,
        /// hands or ornament is modelled, so no likeness is ever drawn; clothing waits for family review.
        /// </summary>
        public static Mesh Figure(string name, int seed, float height)
        {
            var h = height;
            var body = Lathe(name + " body", new List<Vector2>
            {
                new Vector2(0, 0), new Vector2(h * 0.128f, h * 0.006f), new Vector2(h * 0.118f, h * 0.1f), new Vector2(h * 0.11f, h * 0.25f), new Vector2(h * 0.1f, h * 0.45f),
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
                // Hanging from the shoulder, which it meets, hand a little forward and clear of the hip. The figure
                // shader draws only the nearest surface, so where arm and body meet they do not double in light.
                parts.Add(new CombineInstance { mesh = arm, transform = Matrix4x4.TRS(new Vector3(side * h * 0.15f, h * 0.46f, h * 0.02f), Quaternion.Euler(4, 0, side * 4f), Vector3.one) });
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

        /// <summary>
        /// A ewe in parts, so she can walk and graze: the body with its fleece and tail; the neck and head with
        /// drooping ears, built around the point where the neck meets the body; and one leg, built hanging down from
        /// its top. Black vertex colour leaves the figure shader's arm swing untouched.
        /// </summary>
        public static (Mesh Body, Mesh Head, Mesh Leg) EweParts(string name, int seed)
        {
            Mesh Round(int s) => SankofaMeshes.Stone(s, 2, 0.08f, 10f);
            Mesh Dark(List<CombineInstance> parts, string part)
            {
                var m = CombineMeshes(parts, name + " " + part);
                m.SetColors(Fill(m.vertexCount, new Color(0, 0, 0, 1)));
                return m;
            }
            var body = new List<CombineInstance>
            {
                new CombineInstance { mesh = Round(seed), transform = Matrix4x4.TRS(new Vector3(0, 0.5f, 0), Quaternion.identity, new Vector3(0.2f, 0.17f, 0.36f)) },
                new CombineInstance { mesh = Round(seed + 11), transform = Matrix4x4.TRS(new Vector3(0, 0.56f, -0.12f), Quaternion.identity, new Vector3(0.19f, 0.15f, 0.22f)) },
                new CombineInstance { mesh = Round(seed + 12), transform = Matrix4x4.TRS(new Vector3(0, 0.56f, 0.14f), Quaternion.identity, new Vector3(0.18f, 0.14f, 0.2f)) },
                new CombineInstance { mesh = Round(seed + 15), transform = Matrix4x4.TRS(new Vector3(0, 0.5f, -0.37f), Quaternion.Euler(20, 0, 0), new Vector3(0.035f, 0.06f, 0.035f)) },
            };
            // Head parts relative to the neck's root at (0, 0.58, 0.26).
            var neckRoot = new Vector3(0, 0.58f, 0.26f);
            var head = new List<CombineInstance>
            {
                new CombineInstance { mesh = Round(seed + 13), transform = Matrix4x4.TRS(new Vector3(0, 0.6f, 0.3f) - neckRoot, Quaternion.Euler(-35, 0, 0), new Vector3(0.065f, 0.1f, 0.065f)) },
                new CombineInstance { mesh = Round(seed + 1), transform = Matrix4x4.TRS(new Vector3(0, 0.66f, 0.42f) - neckRoot, Quaternion.Euler(35, 0, 0), new Vector3(0.06f, 0.07f, 0.12f)) },
            };
            foreach (var side in new[] { -1f, 1f })
                head.Add(new CombineInstance { mesh = Round(seed + 14), transform = Matrix4x4.TRS(new Vector3(side * 0.065f, 0.69f, 0.37f) - neckRoot, Quaternion.Euler(0, 0, side * -35f), new Vector3(0.045f, 0.012f, 0.025f)) });
            var leg = new List<CombineInstance>
            {
                new CombineInstance { mesh = SankofaMeshes.Log(seed + 5, 0.42f, 0.022f, 0.3f, 0.005f, 6, 2), transform = Matrix4x4.TRS(new Vector3(0, -0.21f, 0), Quaternion.identity, Vector3.one) },
            };
            return (Dark(body, "body"), Dark(head, "head"), Dark(leg, "leg"));
        }

        static List<Vector2> Fill2(int count, Vector2 v)
        {
            var list = new List<Vector2>(count);
            for (var i = 0; i < count; i++) list.Add(v);
            return list;
        }
    }
}
