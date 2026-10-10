using System.Collections.Generic;
using System.Linq;
using Sankofa.Runtime.Stage;
using UnityEditor;
using UnityEngine;

namespace Sankofa.EditorTools
{
    public static partial class SankofaSceneBuilder
    {
        /// <summary>
        /// Gives every object the visitor can notice its echo: its whole shape as one mesh in its own space, which the
        /// fire shows back in its light when the visitor lingered on it. People are taken in their idle pose; what
        /// they carry, and glow cards, are left out.
        /// </summary>
        static void BuildEchoes()
        {
            var idle = AssetDatabase.LoadAllAssetsAtPath(SankofaFigureImport.ModelPath).OfType<AnimationClip>().FirstOrDefault(c => c.name == "Idle_Loop");
            foreach (var o in Object.FindObjectsByType<StageObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (o.AttentionRadius <= 0) continue;
                var mesh = EchoShape(o, idle);
                if (mesh == null) continue;
                mesh.name = "Echo " + o.Id;
                var path = $"{MeshFolder}/{mesh.name}.asset";
                AssetDatabase.DeleteAsset(path);
                AssetDatabase.CreateAsset(mesh, path);
                Set(o, "echoMesh", mesh);
            }
        }

        static Mesh EchoShape(StageObject o, AnimationClip idle)
        {
            var toLocal = o.transform.worldToLocalMatrix;
            var parts = new List<CombineInstance>();
            bool Own(Component c) => c.GetComponentInParent<StageObject>(true) == o;
            foreach (var skin in o.GetComponentsInChildren<SkinnedMeshRenderer>(true).Where(Own))
            {
                var animator = skin.GetComponentInParent<Animator>(true);
                if (idle != null && animator != null) idle.SampleAnimation(animator.gameObject, 0.5f);
                var baked = new Mesh();
                // Baked without the renderer's scale, so it is placed by position and rotation alone.
                skin.BakeMesh(baked, false);
                parts.Add(new CombineInstance { mesh = baked, transform = toLocal * Matrix4x4.TRS(skin.transform.position, skin.transform.rotation, Vector3.one) });
            }
            foreach (var filter in o.GetComponentsInChildren<MeshFilter>(true).Where(Own))
            {
                var renderer = filter.GetComponent<Renderer>();
                if (filter.sharedMesh == null || renderer == null || filter.name == "Glow") continue;
                var shader = renderer.sharedMaterial != null ? renderer.sharedMaterial.shader.name : "";
                if (shader.Contains("Glow") || shader.Contains("Flame") || shader.Contains("Smoke")) continue;
                parts.Add(new CombineInstance { mesh = filter.sharedMesh, transform = toLocal * filter.transform.localToWorldMatrix });
            }
            if (parts.Count == 0) return null;
            // One submesh each, so every part is drawn in the echo's one material.
            var singles = parts.Select(p =>
            {
                var m = Object.Instantiate(p.mesh);
                var all = Enumerable.Range(0, m.subMeshCount).SelectMany(m.GetTriangles).ToArray();
                m.subMeshCount = 1;
                m.SetTriangles(all, 0);
                return new CombineInstance { mesh = m, transform = p.transform };
            }).ToArray();
            var mesh = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            mesh.CombineMeshes(singles, true, true);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
