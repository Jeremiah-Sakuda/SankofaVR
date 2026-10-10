using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// The application's own icon, drawn by code: a small fire between hearth stones on the night ground. It replaces
    /// the engine's default icon, which the competition's no-logos rule excludes.
    /// </summary>
    public static class SankofaIcon
    {
        public const string IconPath = SankofaArt.Folder + "/Icon.png";
        const int Size = 512;

        [MenuItem("Sankofa/Set Application Icon")]
        public static Texture2D Ensure()
        {
            File.WriteAllBytes(IconPath, Draw().EncodeToPNG());
            AssetDatabase.ImportAsset(IconPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(IconPath);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            AssetDatabase.SaveAssets();
            return icon;
        }

        static Texture2D Draw()
        {
            var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var pixels = new Color[Size * Size];
            for (var y = 0; y < Size; y++)
            for (var x = 0; x < Size; x++)
            {
                // Centred coordinates, y up, -1..1.
                var p = new Vector2(x / (Size - 1f) * 2f - 1f, y / (Size - 1f) * 2f - 1f);
                pixels[y * Size + x] = Shade(p);
            }
            texture.SetPixels(pixels);
            texture.Apply(false);
            return texture;
        }

        static Color Shade(Vector2 p)
        {
            // Night above, warm ground below, lit from the fire at the centre.
            var night = Color.Lerp(new Color(0.03f, 0.035f, 0.07f), new Color(0.09f, 0.07f, 0.1f), Mathf.InverseLerp(1f, -0.2f, p.y));
            var ground = new Color(0.16f, 0.09f, 0.05f);
            var c = Color.Lerp(night, ground, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.2f, -0.35f, p.y)));
            var fireAt = new Vector2(0f, -0.28f);
            var glow = Mathf.Exp(-(p - fireAt).sqrMagnitude * 3.2f);
            c += new Color(0.95f, 0.42f, 0.12f) * glow * 0.75f;

            // The flame: a teardrop, its tip leaning a little, white at the root, orange to red at the edge.
            var q = p - new Vector2(0f, -0.32f);
            q.x -= 0.06f * Mathf.Max(0f, q.y) * q.y;
            var height = 0.78f;
            var t = Mathf.Clamp01(q.y / height);
            var halfWidth = 0.27f * Mathf.Sqrt(Mathf.Max(0f, t)) * (1f - t) * 1.6f;
            if (q.y < 0f) halfWidth = Mathf.Sqrt(Mathf.Max(0f, 0.27f * 0.27f * 0.5f - q.y * q.y * 2.2f));
            var inside = halfWidth - Mathf.Abs(q.x);
            if (inside > 0f)
            {
                var core = Mathf.Clamp01(inside / Mathf.Max(0.001f, halfWidth));
                var flame = Color.Lerp(new Color(0.85f, 0.2f, 0.05f), new Color(1f, 0.62f, 0.2f), Mathf.SmoothStep(0f, 1f, core * 1.6f));
                flame = Color.Lerp(flame, new Color(1f, 0.95f, 0.78f), Mathf.SmoothStep(0.55f, 1f, core) * (1f - t));
                c = Color.Lerp(c, flame, Mathf.SmoothStep(0f, 0.02f, inside));
            }

            // Three hearth stones in front of the flame's root, lit on their upper edges.
            foreach (var stone in new[] { new Vector3(-0.42f, -0.48f, 0.17f), new Vector3(0f, -0.56f, 0.19f), new Vector3(0.42f, -0.48f, 0.17f) })
            {
                var d = new Vector2((p.x - stone.x) / stone.z, (p.y - stone.y) / (stone.z * 0.72f));
                var r = d.magnitude;
                if (r >= 1f) continue;
                var lit = Mathf.Clamp01(d.y * 0.8f + 0.5f) * (1f - Mathf.Abs(stone.x));
                var rock = Color.Lerp(new Color(0.12f, 0.1f, 0.1f), new Color(0.78f, 0.5f, 0.32f), lit);
                c = Color.Lerp(c, rock, Mathf.SmoothStep(1f, 0.94f, r));
            }
            c.a = 1f;
            return c;
        }
    }
}
