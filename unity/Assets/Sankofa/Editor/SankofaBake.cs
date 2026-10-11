using System.Collections.Generic;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Bakes soft fire shadows and ambient occlusion into vertices: the vertex colour's alpha holds occlusion and
    /// UV2.x the fraction of the flames each vertex can see. For surfaces in <c>moonlit</c>, UV2.y holds the
    /// moon's visibility (elsewhere UV2.y is left alone; burning wood keeps its glow mask there). The fire and the
    /// moon never move relative to the hearth, so their shadows are traced once in the editor; at runtime the
    /// shaders multiply them into the light at no extra cost.
    /// </summary>
    public static class SankofaBake
    {
        const int AoRays = 18;
        const float AoDistance = 0.6f;

        public static void Bake(IList<MeshFilter> receivers, IList<MeshFilter> occluders, Vector3 fire, Vector3 towardMoon,
            ICollection<MeshFilter> moonlit, float fireReach = 14f)
        {
            var colliders = new List<MeshCollider>();
            foreach (var f in occluders)
            {
                // Leaf cards are mostly air. Their full rectangles must not bake solid canopy shadows.
                if (f.GetComponent<Renderer>().sharedMaterial.IsKeywordEnabled("_FOLIAGE")) continue;
                var c = f.gameObject.AddComponent<MeshCollider>();
                c.sharedMesh = f.sharedMesh;
                colliders.Add(c);
            }
            Physics.SyncTransforms();
            try
            {
                var flames = FlameSamples(fire);
                var moonRays = MoonRays(towardMoon.normalized);
                foreach (var f in receivers) BakeMesh(f, flames, fire, fireReach, moonlit.Contains(f) ? moonRays : null);
            }
            finally
            {
                foreach (var c in colliders) Object.DestroyImmediate(c);
            }
        }

        /// <summary>Points spread through the flame volume, so shadows soften with distance like a real fire's.</summary>
        static Vector3[] FlameSamples(Vector3 fire)
        {
            const int count = 12;
            var samples = new Vector3[count];
            for (var k = 0; k < count; k++)
            {
                var angle = k * 2.39996f;
                var radius = 0.13f * Mathf.Sqrt((k + 0.5f) / count);
                var height = 0.16f + 0.4f * Mathf.Repeat(k * 0.618034f, 1f);
                samples[k] = fire + new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
            }
            return samples;
        }

        /// <summary>A few directions within the moon's soft disc, for slightly blurred moon shadows.</summary>
        static Vector3[] MoonRays(Vector3 toward)
        {
            Basis(toward, out var t, out var b);
            const float spread = 0.03f;
            return new[] { toward, (toward + t * spread).normalized, (toward - t * spread).normalized, (toward + b * spread).normalized, (toward - b * spread).normalized };
        }

        static void BakeMesh(MeshFilter filter, Vector3[] flames, Vector3 fire, float fireReach, Vector3[] moonRays)
        {
            var mesh = filter.sharedMesh;
            var vertices = mesh.vertices;
            var normals = mesh.normals;
            var colors = mesh.colors;
            var bake = new List<Vector2>();
            mesh.GetUVs(1, bake);
            if (colors.Length != vertices.Length)
            {
                colors = new Color[vertices.Length];
                for (var i = 0; i < colors.Length; i++) colors[i] = Color.white;
            }
            while (bake.Count < vertices.Length) bake.Add(Vector2.zero);

            var toWorld = filter.transform.localToWorldMatrix;
            for (var i = 0; i < vertices.Length; i++)
            {
                var p = toWorld.MultiplyPoint3x4(vertices[i]);
                var n = toWorld.MultiplyVector(normals[i]).normalized;
                var origin = p + n * 0.012f;

                var visible = 0;
                if ((p - fire).sqrMagnitude > fireReach * fireReach) visible = flames.Length;
                else
                    foreach (var flame in flames)
                    {
                        var to = flame - origin;
                        var distance = to.magnitude;
                        if (Vector3.Dot(to, n) <= 0) continue;
                        if (!Physics.Raycast(origin, to / distance, distance - 0.02f)) visible++;
                    }

                var open = 0;
                Basis(n, out var tangent, out var bitangent);
                for (var k = 0; k < AoRays; k++)
                {
                    // Cosine-weighted Fibonacci hemisphere.
                    var u = (k + 0.5f) / AoRays;
                    var r = Mathf.Sqrt(u);
                    var phi = k * 2.39996f;
                    var dir = tangent * (r * Mathf.Cos(phi)) + bitangent * (r * Mathf.Sin(phi)) + n * Mathf.Sqrt(1f - u);
                    if (!Physics.Raycast(origin, dir, AoDistance)) open++;
                }

                var occlusion = 0.3f + 0.7f * open / AoRays;
                colors[i].a = occlusion;
                var moon = bake[i].y;
                if (moonRays != null)
                {
                    var lit = 0;
                    foreach (var dir in moonRays)
                        if (Vector3.Dot(dir, n) > 0 && !Physics.Raycast(origin, dir, 40f)) lit++;
                    moon = (float)lit / moonRays.Length;
                }
                bake[i] = new Vector2((float)visible / flames.Length, moon);
            }
            mesh.colors = colors;
            mesh.SetUVs(1, bake);
        }

        static void Basis(Vector3 n, out Vector3 tangent, out Vector3 bitangent)
        {
            tangent = Vector3.Cross(n, Mathf.Abs(n.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            bitangent = Vector3.Cross(n, tangent);
        }
    }
}
