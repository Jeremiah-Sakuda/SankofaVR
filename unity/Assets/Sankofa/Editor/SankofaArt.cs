using UnityEditor;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>Materials and textures used by the generated scene. Regenerate rather than edit by hand.</summary>
    public sealed class SankofaArtSet
    {
        public Texture2D Noise, Detail;
        public Material Flame, Glow, Sparks, Curtain, CaptionBacking;
        public Material Workbench, Brass, LanternCore;
        public Material Water, Spill, River, EmberFigure, City, Gourd, Hide, Canvas, Steel, Soil, Leaf, Bead, Metal, Paint;
        public Material Ground, Rock, BurningWood, Embers, HeldEmber, SeatWood, TreeBark, TreeCanopy, Grass, Sky, Stars, Smoke, Halo, Plaque;
    }

    public static class SankofaArt
    {
        public const string Folder = "Assets/Sankofa/Art/Generated";
        public const string NoisePath = Folder + "/Noise.asset";
        public const string DetailPath = Folder + "/Detail.asset";
        public const string ScannedFolder = "Assets/Sankofa/Art/Scanned";

        /// <summary>A scanned map approved and recorded in Art/Scanned/sources.json.</summary>
        static Texture2D Scan(string id, string map)
        {
            var path = $"{ScannedFolder}/{id}/{id}_{map}_1k.jpg";
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path) ?? throw new System.InvalidOperationException("Missing scanned texture " + path);
        }

        public static SankofaArtSet Ensure()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Sankofa/Art", "Generated");
            var noise = EnsureNoise();
            var detail = EnsureDetail();
            var art = new SankofaArtSet
            {
                Noise = noise,
                Detail = detail,
                Flame = Make("Flame", "Sankofa/Flame", m => m.SetTexture("_Noise", noise)),
                Glow = Make("Glow", "Sankofa/Glow", m =>
                {
                    m.SetColor("_Color", new Color(1f, 0.55f, 0.22f));
                    m.SetFloat("_Falloff", 2.4f);
                }),
                Sparks = Make("Sparks", "Sankofa/Glow", m =>
                {
                    m.SetColor("_Color", new Color(1f, 0.62f, 0.3f));
                    m.SetFloat("_Intensity", 1.6f);
                    m.SetFloat("_Falloff", 1.6f);
                }),
                Curtain = Make("Curtain", "Sankofa/Curtain", m => m.SetTexture("_Noise", noise)),
                CaptionBacking = Make("Caption backing", "Sprites/Default", m => m.color = new Color(0.025f, 0.02f, 0.018f, 0.55f)),

                // Story scenes at the water.
                Workbench = Make("Workbench wood", "Sankofa/Fire Lit", m =>
                {
                    m.SetColor("_Color", new Color(0.4f, 0.28f, 0.18f));
                    m.SetTexture("_Detail", detail);
                    m.SetVector("_DetailScale", new Vector4(0.5f, 3f, 0, 0));
                    m.SetFloat("_DetailStrength", 0.35f);
                    m.SetFloat("_DetailAlbedo", 0.45f);
                    m.SetFloat("_Wrap", 0.3f);
                    m.EnableKeyword("_DETAIL_UV");
                }),
                Brass = Lit("Lantern frame", new Color(0.36f, 0.27f, 0.14f)),
                Water = Make("Stream water", "Sankofa/Water", m =>
                {
                    m.SetTexture("_Noise", noise);
                    m.SetColor("_Deep", new Color(0.015f, 0.03f, 0.028f));
                    m.SetColor("_Shallow", new Color(0.06f, 0.08f, 0.065f));
                    m.SetColor("_Sky", new Color(0.1f, 0.11f, 0.17f));
                    m.SetFloat("_Flow", 0.35f);
                    m.SetFloat("_Scale", 1.6f);
                }),
                Spill = Make("Spill water", "Sankofa/Water", m =>
                {
                    m.SetTexture("_Noise", noise);
                    m.SetColor("_Deep", new Color(0.05f, 0.07f, 0.07f));
                    m.SetColor("_Shallow", new Color(0.16f, 0.2f, 0.2f));
                    m.SetColor("_Sky", new Color(0.2f, 0.22f, 0.28f));
                    m.SetFloat("_Flow", 1.6f);
                    m.SetFloat("_Scale", 4f);
                    m.SetFloat("_Foam", 0.55f);
                }),
                River = Make("River water", "Sankofa/Water", m =>
                {
                    m.SetTexture("_Noise", noise);
                    m.SetColor("_Deep", new Color(0.02f, 0.025f, 0.02f));
                    m.SetColor("_Shallow", new Color(0.05f, 0.055f, 0.04f));
                    m.SetColor("_Sky", new Color(0.18f, 0.12f, 0.08f));
                    m.SetFloat("_Flow", 0.25f);
                    m.SetFloat("_Scale", 0.7f);
                    // A slow, broad river: gentle ripples, so the glints stay few.
                    m.SetFloat("_Strength", 0.16f);
                }),
                EmberFigure = Make("Ember figure", "Sankofa/Ember Figure", m => m.SetTexture("_Noise", noise)),
                City = Make("City", "Sankofa/City", m => { m.SetFloat("_Lit", 0.22f); m.SetFloat("_Glow", 0.9f); }),
                Gourd = Baked("Gourd", new Color(0.72f, 0.5f, 0.27f), detail, "_DETAIL_UV", new Vector2(3f, 1f), 0.1f, 0.3f),
                Hide = Baked("Hide", new Color(0.45f, 0.3f, 0.2f), detail, "_DETAIL_TRIPLANAR", new Vector2(6f, 6f), 0.5f, 0.5f),
                Canvas = Baked("Canvas", new Color(0.78f, 0.72f, 0.6f), detail, "_DETAIL_UV", new Vector2(2f, 2f), 0.15f, 0.2f),
                Steel = Make("Rail steel", "Sankofa/Fire Lit", m =>
                {
                    m.SetColor("_Color", new Color(0.22f, 0.19f, 0.17f));
                    m.SetTexture("_Detail", detail);
                    m.SetVector("_DetailScale", new Vector4(2f, 2f, 0, 0));
                    m.SetFloat("_DetailStrength", 0.4f);
                    m.SetFloat("_DetailAlbedo", 0.5f);
                    m.SetFloat("_Rim", 1f);
                    m.SetColor("_HighlightColor", new Color(0.55f, 0.7f, 0.9f));
                    m.EnableKeyword("_DETAIL_UV");
                }),
                Soil = Baked("Garden soil", new Color(0.2f, 0.14f, 0.1f), detail, "_DETAIL_TRIPLANAR", new Vector2(5f, 5f), 0.7f, 0.6f),
                Leaf = Baked("Leaf", new Color(0.24f, 0.32f, 0.12f), detail, "_DETAIL_TRIPLANAR", new Vector2(8f, 8f), 0.2f, 0.3f),
                Bead = Make("Glass bead", "Sankofa/Fire Lit", m =>
                {
                    // Colour is a placeholder until family review.
                    m.SetColor("_Color", new Color(0.12f, 0.25f, 0.55f));
                    m.SetColor("_Emission", new Color(0.02f, 0.04f, 0.09f));
                    m.SetFloat("_Rim", 1.6f);
                    m.SetFloat("_Wrap", 0.5f);
                }),
                Metal = Lit("Jiko metal", new Color(0.3f, 0.27f, 0.24f)),
                Paint = Baked("Gate paint", new Color(0.12f, 0.32f, 0.3f), detail, "_DETAIL_UV", new Vector2(2f, 2f), 0.2f, 0.25f),
                LanternCore = Lit("Lantern flame", new Color(0.3f, 0.2f, 0.1f), new Color(1f, 0.72f, 0.4f)),

                // Hearth materials: baked occlusion and fire shadows, generated surface detail.
                // Scanned materials (Poly Haven, CC0): see Art/Scanned/sources.json and the asset register.
                Ground = Make("Hearth ground", "Sankofa/Ground", m =>
                {
                    m.SetColor("_Tint", new Color(0.9f, 0.86f, 0.82f));
                    m.SetTexture("_Albedo", Scan("dry_ground_01", "diff"));
                    m.SetTexture("_Normal", Scan("dry_ground_01", "nor_gl"));
                    m.SetFloat("_Scale", 0.55f);
                    m.SetTexture("_Albedo2", Scan("burned_ground_01", "diff"));
                    m.SetTexture("_Normal2", Scan("burned_ground_01", "nor_gl"));
                    m.SetFloat("_Scale2", 0.8f);
                    m.SetFloat("_NormalStrength", 1f);
                    m.SetTexture("_Macro", detail);
                }),
                Rock = Scanned(Baked("Hearth rock", Color.white, detail, "_DETAIL_TRIPLANAR", new Vector2(0.35f, 0.35f), 0f, 0.3f),
                    "rock_boulder_dry", new Vector2(1.6f, 1.6f), 1f),
                SeatWood = Scanned(Baked("Seat wood", new Color(0.95f, 0.88f, 0.8f), detail, "_DETAIL_UV", new Vector2(0.5f, 0.3f), 0f, 0.3f),
                    "bark_brown_02", new Vector2(1f, 1f), 1.1f),
                TreeBark = Scanned(Baked("Tree bark", new Color(0.32f, 0.3f, 0.28f), detail, "_DETAIL_UV", new Vector2(0.5f, 0.3f), 0f, 0.2f),
                    "bark_brown_02", new Vector2(1f, 0.6f), 1f),
                TreeCanopy = Baked("Tree canopy", new Color(0.05f, 0.065f, 0.05f), detail, "_DETAIL_TRIPLANAR", new Vector2(0.8f, 0.8f), 0.6f, 0.3f),
                Grass = Make("Grass", "Sankofa/Fire Lit", m =>
                {
                    m.SetColor("_Color", Color.white);
                    m.SetFloat("_UseBake", 1);
                    m.SetFloat("_Cull", 0);
                    m.SetFloat("_Wrap", 0.6f);
                    m.SetFloat("_DetailAlbedo", 0);
                    m.SetFloat("_Wind", 0.05f);
                    m.SetFloat("_Rim", 1.2f);
                }),
                // Control plaques: dark wood with grain; PokePanel tints and lights them.
                Plaque = Make("Plaque", "Sankofa/Fire Lit", m =>
                {
                    m.SetColor("_Color", Color.white);
                    m.SetTexture("_Detail", detail);
                    m.SetVector("_DetailScale", new Vector4(1.6f, 0.5f, 0, 0));
                    m.SetFloat("_DetailStrength", 0.5f);
                    m.SetFloat("_DetailAlbedo", 0.5f);
                    m.SetFloat("_Wrap", 1f);
                    m.SetFloat("_Rim", 0f);
                    m.EnableKeyword("_DETAIL_UV");
                }),
                BurningWood = Make("Burning wood", "Sankofa/Charcoal", m =>
                {
                    m.SetTexture("_Detail", detail);
                    m.SetVector("_DetailScale", new Vector4(2.5f, 3.5f, 0, 0));
                    m.SetFloat("_CrackThreshold", 0.56f);
                    // The charred bark keeps its scanned relief.
                    m.SetTexture("_ScanNormal", Scan("bark_brown_02", "nor_gl"));
                    m.SetVector("_ScanScale", new Vector4(1f, 1.2f, 0, 0));
                    m.SetFloat("_ScanNormalStrength", 0.8f);
                    m.SetFloat("_DetailStrength", 0.4f);
                }),
                Embers = Make("Embers", "Sankofa/Charcoal", m =>
                {
                    m.SetTexture("_Detail", detail);
                    m.SetVector("_DetailScale", new Vector4(3f, 3f, 0, 0));
                    m.SetFloat("_CrackThreshold", 0.45f);
                    m.SetFloat("_Glow", 1.5f);
                }),
                Sky = Make("Night sky", "Sankofa/Sky", m => m.SetTexture("_Noise", noise)),
                Stars = Make("Stars", "Sankofa/Stars", null),
                Smoke = Make("Smoke", "Sankofa/Smoke", m =>
                {
                    m.SetTexture("_Noise", noise);
                    m.SetFloat("_Density", 0.14f);
                }),
                HeldEmber = Make("Held ember", "Sankofa/Charcoal", m =>
                {
                    m.SetTexture("_Detail", detail);
                    m.SetVector("_DetailScale", new Vector4(2.2f, 2.2f, 0, 0));
                    m.SetColor("_EmberColor", new Color(1f, 0.42f, 0.1f));
                    m.SetFloat("_CrackThreshold", 0.35f);
                    m.SetFloat("_Glow", 2.4f);
                }),
                Halo = Make("Fire halo", "Sankofa/Glow", m =>
                {
                    m.SetColor("_Color", new Color(1f, 0.48f, 0.18f));
                    m.SetFloat("_Falloff", 2.8f);
                }),
            };
            AssetDatabase.SaveAssets();
            return art;
        }

        static Material Baked(string name, Color albedo, Texture2D detail, string mode, Vector2 scale, float strength, float variation) =>
            Make(name, "Sankofa/Fire Lit", m =>
            {
                m.SetColor("_Color", albedo);
                m.SetFloat("_UseBake", 1);
                m.SetTexture("_Detail", detail);
                m.SetVector("_DetailScale", new Vector4(scale.x, scale.y, 0, 0));
                m.SetFloat("_DetailStrength", strength);
                m.SetFloat("_DetailAlbedo", variation);
                foreach (var keyword in new[] { "_DETAIL_PLANAR", "_DETAIL_TRIPLANAR", "_DETAIL_UV" })
                    if (keyword == mode) m.EnableKeyword(keyword);
                    else m.DisableKeyword(keyword);
            });

        static Material Scanned(Material m, string id, Vector2 scale, float normalStrength)
        {
            m.SetTexture("_ScanAlbedo", Scan(id, "diff"));
            m.SetTexture("_ScanNormal", Scan(id, "nor_gl"));
            m.SetVector("_ScanScale", new Vector4(scale.x, scale.y, 0, 0));
            m.SetFloat("_ScanNormalStrength", normalStrength);
            m.EnableKeyword("_SCANNED");
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Lit(string name, Color albedo, Color emission = default) =>
            Make(name, "Sankofa/Fire Lit", m =>
            {
                m.SetColor("_Color", albedo);
                m.SetColor("_Emission", emission);
            });

        /// <summary>Loads or creates the material and reapplies its settings, keeping the asset (and its GUID) stable.</summary>
        static Material Make(string name, string shaderName, System.Action<Material> configure)
        {
            var shader = Shader.Find(shaderName);
            if (shader == null) throw new System.InvalidOperationException("Shader not found: " + shaderName);
            var path = $"{Folder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            material.shader = shader;
            // Regenerated materials start from a clean keyword state; their configuration enables what they need.
            material.shaderKeywords = System.Array.Empty<string>();
            material.enableInstancing = true;
            configure?.Invoke(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Tileable value noise, a different scale per channel, for flames and the transition curtain.</summary>
        static Texture2D EnsureNoise()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(NoisePath);
            if (existing != null) return existing;
            const int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
            {
                name = "Sankofa noise",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
            };
            var pixels = new Color32[size * size];
            var random = new System.Random(1931);
            var channels = new[] { Fractal(size, 4, random), Fractal(size, 8, random), Fractal(size, 16, random), Fractal(size, 2, random) };
            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(Byte(channels[0][i]), Byte(channels[1][i]), Byte(channels[2][i]), Byte(channels[3][i]));
            texture.SetPixels32(pixels);
            texture.Apply(true);
            AssetDatabase.CreateAsset(texture, NoisePath);
            return texture;
        }

        /// <summary>
        /// Tileable surface detail: a fractal height field in B and its slopes as a normal offset in R and G, for
        /// grain in earth, stone and bark without photographic textures.
        /// </summary>
        static Texture2D EnsureDetail()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Texture2D>(DetailPath);
            if (existing != null) return existing;
            const int size = 256;
            var random = new System.Random(4113);
            var coarse = Fractal(size, 8, random);
            var fine = Fractal(size, 32, random);
            var height = new float[size * size];
            for (var i = 0; i < height.Length; i++)
            {
                // Ridged coarse layer reads as cracks and grain; the fine layer adds grit.
                var ridge = 1f - Mathf.Abs(coarse[i] * 2f - 1f);
                height[i] = Mathf.Clamp01(ridge * 0.65f + fine[i] * 0.35f);
            }
            float H(int x, int y) => height[((y + size) % size) * size + (x + size) % size];
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
            for (var x = 0; x < size; x++)
            {
                var dx = (H(x + 1, y) - H(x - 1, y)) * 6f;
                var dy = (H(x, y + 1) - H(x, y - 1)) * 6f;
                pixels[y * size + x] = new Color32(Byte(0.5f - dx * 0.5f), Byte(0.5f - dy * 0.5f), Byte(H(x, y)), 255);
            }
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, true, true)
            {
                name = "Sankofa detail",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 2,
            };
            texture.SetPixels32(pixels);
            texture.Apply(true);
            AssetDatabase.CreateAsset(texture, DetailPath);
            return texture;
        }

        static byte Byte(float v) => (byte)Mathf.Clamp(Mathf.RoundToInt(v * 255f), 0, 255);

        /// <summary>Three octaves of smoothly interpolated lattice noise that wraps at the texture edge.</summary>
        static float[] Fractal(int size, int baseCells, System.Random random)
        {
            var result = new float[size * size];
            var amplitude = 1f;
            var total = 0f;
            for (var octave = 0; octave < 3; octave++)
            {
                var cells = baseCells << octave;
                var lattice = new float[cells * cells];
                for (var i = 0; i < lattice.Length; i++) lattice[i] = (float)random.NextDouble();
                for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var fx = (float)x / size * cells;
                    var fy = (float)y / size * cells;
                    int x0 = (int)fx, y0 = (int)fy;
                    float tx = Smooth(fx - x0), ty = Smooth(fy - y0);
                    int x1 = (x0 + 1) % cells, y1 = (y0 + 1) % cells;
                    var top = Mathf.Lerp(lattice[y0 * cells + x0], lattice[y0 * cells + x1], tx);
                    var bottom = Mathf.Lerp(lattice[y1 * cells + x0], lattice[y1 * cells + x1], tx);
                    result[y * size + x] += Mathf.Lerp(top, bottom, ty) * amplitude;
                }
                total += amplitude;
                amplitude *= 0.5f;
            }
            for (var i = 0; i < result.Length; i++) result[i] /= total;
            return result;
        }

        static float Smooth(float t) => t * t * (3f - 2f * t);
    }
}
