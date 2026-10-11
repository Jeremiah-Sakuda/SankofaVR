using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Import rules for Quaternius's CC0 base characters and Universal Animation Library (see the bundled
    /// licences in Art/Figures). The base character supplies anatomy; the library supplies motion. Their materials
    /// are not imported (the figures are drawn by the ember shader), the looping clips loop, and its bones stay
    /// exposed so carried things can follow the hands.
    /// </summary>
    public sealed class SankofaFigureImport : AssetPostprocessor
    {
        public const string ModelPath = "Assets/Sankofa/Art/Figures/UAL1_Standard.fbx";

        // Bumped whenever these rules change, so Unity reimports the model.
        public const string CharacterPath = "Assets/Sankofa/Art/Figures/Superhero_Female_FullBody.fbx";
        public const string HairPath = "Assets/Sankofa/Art/Figures/Hair_BuzzedFemale.fbx";
        public override uint GetVersion() => 4;

        void OnPreprocessModel()
        {
            if (assetPath != ModelPath && assetPath != CharacterPath && assetPath != HairPath) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.optimizeGameObjects = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
            importer.isReadable = true; // Offline mesh derivation and echo baking require CPU geometry.
            importer.importAnimation = true;
        }

        void OnPreprocessAnimation()
        {
            if (assetPath != ModelPath) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                // The file names its takes "Armature|Walk_Loop"; keep the plain names.
                clip.name = clip.name.Substring(clip.name.LastIndexOf('|') + 1);
                clip.loopTime = clip.name.EndsWith("_Loop") || clip.name == "Fixing_Kneeling";
            }
            importer.clipAnimations = clips;
        }
    }

    public static partial class SankofaSceneBuilder
    {
        public static void InspectFigureSources()
        {
            foreach (var path in new[] { SankofaFigureImport.ModelPath, SankofaFigureImport.CharacterPath, SankofaFigureImport.HairPath })
            {
                var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var skin = model.GetComponentInChildren<SkinnedMeshRenderer>();
                Debug.Log("[Sankofa] Source " + path + " bones: " + string.Join(",", skin.bones.Select(t => t.name)));
                Debug.Log("[Sankofa] Source hierarchy " + string.Join(",", model.GetComponentsInChildren<Transform>().Take(12).Select(t => AnimationUtility.CalculateTransformPath(t, model.transform))));
                Debug.Log("[Sankofa] Skin " + skin.name + " bounds " + skin.sharedMesh.bounds + " transforms " + skin.transform.localToWorldMatrix);
            }
            var clip = AssetDatabase.LoadAllAssetsAtPath(SankofaFigureImport.ModelPath).OfType<AnimationClip>().First(c => c.name == "Idle_Loop");
            Debug.Log("[Sankofa] Animation paths " + string.Join(",", AnimationUtility.GetCurveBindings(clip).Take(12).Select(c => c.path)));
        }

        /// <summary>
        /// Draws a figure part in ember light, with its depth written first (see EmberFigureDepth.shader) so that
        /// all a figure's parts together show only their nearest surface. The part must be a single submesh.
        /// </summary>
        public static void DrawAsFigure(Renderer renderer, SankofaArtSet art) =>
            renderer.sharedMaterials = new[] { art.EmberFigure, art.EmberFigureDepth };

        /// <summary>Folded cloth fitted in the figure's space and skinned to the same skeleton as the body.
        /// Plain construction is a theatrical draft, not a claim about historical dress.</summary>
        static Renderer Cloth(Transform figureRoot, SankofaArtSet art, SkinnedMeshRenderer body, string name,
            float height, List<Vector2> profile)
        {
            if (name == "Shawl") return FittedCloth(figureRoot, body, art, height);
            const int sides = 64;
            profile = profile.OrderBy(p => p.y).ToList();
            var dense = new List<Vector2>();
            for (var k = 0; k < profile.Count - 1; k++)
                for (var j = 0; j < 5; j++) dense.Add(Vector2.Lerp(profile[k], profile[k + 1], j / 5f));
            dense.Add(profile[profile.Count - 1]);
            profile = dense;
            var vertices = new List<Vector3>();
            var uv = new List<Vector2>();
            var weights = new List<BoneWeight>();
            var triangles = new List<int>();
            var skinMatrices = SkinMatrices(body);
            int Bone(string n) => System.Array.FindIndex(body.bones, b => b.name == n);
            var pelvis = Bone("pelvis");
            var spine = Bone("spine_03");
            var waist = Bone("spine_01");
            var thighL = Bone("thigh_l");
            var thighR = Bone("thigh_r");
            for (var row = 0; row < profile.Count; row++)
            {
                var p = profile[row];
                var y = p.y * height;
                for (var col = 0; col <= sides; col++)
                {
                    var angle = col * Mathf.PI * 2f / sides;
                    // Long folds taper into the waist; the hem is gently irregular rather than a perfect lathe.
                    var fold = Mathf.Sin(angle * 12f + 0.3f) * 0.012f * height;
                    fold *= name == "Wrap" ? Mathf.InverseLerp(0.6f, 0.12f, p.y) : 0.4f;
                    var r = p.x * height + fold;
                    var local = new Vector3(Mathf.Cos(angle) * r, y,
                        Mathf.Sin(angle) * r * (name == "Wrap" ? 0.7f : 0.63f) + 0.012f);
                    uv.Add(new Vector2((float)col / sides, p.y * 4f));
                    if (name == "Wrap")
                    {
                        var follow = 0.8f * Mathf.InverseLerp(0.58f, 0.2f, p.y);
                        var left = 0.5f - Mathf.Cos(angle) * 0.5f;
                        var torso = Mathf.InverseLerp(0.55f, 0.66f, p.y);
                        weights.Add(new BoneWeight { boneIndex0 = pelvis, weight0 = 1 - follow - torso, boneIndex1 = thighL, weight1 = follow * left, boneIndex2 = thighR, weight2 = follow * (1 - left), boneIndex3 = waist, weight3 = torso });
                    }
                    else
                    {
                        var upper = Mathf.InverseLerp(0.54f, 0.74f, p.y);
                        weights.Add(new BoneWeight { boneIndex0 = spine, weight0 = upper, boneIndex1 = pelvis, weight1 = 1 - upper });
                    }
                    vertices.Add(SkinMatrix(skinMatrices, weights[weights.Count - 1]).inverse.MultiplyPoint3x4(figureRoot.TransformPoint(local)));
                    if (row == 0 || col == sides) continue;
                    int k = row * (sides + 1) + col;
                    triangles.AddRange(new[] { k, k + 1, k - sides - 1, k + 1, k - sides, k - sides - 1 });
                }
            }
            var mesh = new Mesh { name = "Figure " + name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.boneWeights = weights.ToArray();
            mesh.bindposes = body.sharedMesh.bindposes;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var path = "Assets/Sankofa/Art/Generated/Figure " + name + ".asset";
            // All figures share construction in mesh space; scale is inherited from their model.
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing != null) mesh = existing;
            else AssetDatabase.CreateAsset(mesh, path);
            var go = new GameObject(name);
            go.transform.SetParent(body.transform, false);
            var cloth = go.AddComponent<SkinnedMeshRenderer>();
            cloth.sharedMesh = mesh;
            cloth.bones = body.bones;
            cloth.rootBone = body.rootBone;
            cloth.localBounds = body.localBounds;
            DrawAsFigure(cloth, art);
            cloth.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cloth.receiveShadows = false;
            return cloth;
        }

        // A complete garment, authored in the posed figure's space and converted to bind space. It
        // uses interpolated clip edges and the artist's weights at the neck and shoulders. Covered body faces are
        // removed after garment derivation, so animated skin cannot poke through. Plain construction still needs cultural review.
        static Renderer FittedCloth(Transform figureRoot, SkinnedMeshRenderer body, SankofaArtSet art, float height)
        {
            const string path = "Assets/Sankofa/Art/Generated/Figure Shawl.asset";
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                var vertices = new List<Vector3>();
                var uv = new List<Vector2>();
                var weights = new List<BoneWeight>();
                var triangles = new List<int>();
                var skinMatrices = SkinMatrices(body);
                int Bone(string name) => System.Array.FindIndex(body.bones, b => b.name == name);
                void Vertex(Vector3 point, Vector2 tex, BoneWeight weight)
                {
                    vertices.Add(SkinMatrix(skinMatrices, weight).inverse.MultiplyPoint3x4(figureRoot.TransformPoint(point)));
                    uv.Add(tex); weights.Add(weight);
                }
                var sourceVertices = body.sharedMesh.vertices;
                var sourceNormals = body.sharedMesh.normals;
                var sourceWeights = body.sharedMesh.boneWeights;
                var sourceTriangles = body.sharedMesh.triangles;
                var sourceUV = body.sharedMesh.uv;
                var names = body.bones.Select(b => b.name).ToArray();
                ClothVertex Surface(int index, bool torso)
                {
                    var skin = SkinMatrix(skinMatrices, sourceWeights[index]);
                    var normal = skin.inverse.transpose.MultiplyVector(sourceNormals[index]).normalized;
                    var point = figureRoot.InverseTransformPoint(skin.MultiplyPoint3x4(sourceVertices[index]) + normal * 0.014f);
                    // Cotton hangs below the chest rather than outlining the base character's abdominal muscles.
                    if (torso && point.y < height * 0.73f && point.z > 0)
                        point.z = Mathf.Max(point.z, height * 0.082f * (1 - Mathf.Pow(point.x / (height * 0.17f), 2)));
                    return new ClothVertex { Point = point, UV = sourceUV[index], Weight = sourceWeights[index] };
                }
                void Polygon(List<ClothVertex> polygon)
                {
                    for (var j = 1; j + 1 < polygon.Count; j++)
                    {
                        var first = vertices.Count;
                        foreach (var v in new[] { polygon[0], polygon[j], polygon[j + 1] }) Vertex(v.Point, v.UV, v.Weight);
                        triangles.AddRange(new[] { first, first + 1, first + 2 });
                    }
                }
                bool Torso(int index)
                {
                    var n = names[DominantBone(sourceWeights[index])];
                    return n.StartsWith("spine") || n.StartsWith("clavicle") || n.StartsWith("neck") || n == "Head" || n == "pelvis";
                }
                // Preserve the artist's shoulder/neck weights. An independent loft did not follow the neck
                // when the kneeling clip bowed, and exposed a hole even with a perfectly closed waist.
                for (var i = 0; i < sourceTriangles.Length; i += 3)
                {
                    var ids = new[] { sourceTriangles[i], sourceTriangles[i + 1], sourceTriangles[i + 2] };
                    if (!ids.Any(Torso)) continue;
                    var polygon = ids.Select(k => Surface(k, true)).ToList();
                    polygon = ClipCloth(polygon, v => v.Point.y - height * 0.60f);
                    polygon = ClipCloth(polygon, v => height * 0.865f - v.Point.y);
                    Polygon(polygon);
                }
                // Clean cuffs on sleeves with the same artist-authored shoulder and arm weights.
                foreach (var side in new[] { "l", "r" })
                {
                    var upperIndex = Bone("upperarm_" + side);
                    var upper = figureRoot.InverseTransformPoint(body.bones[upperIndex].position);
                    var lower = figureRoot.InverseTransformPoint(body.bones[Bone("lowerarm_" + side)].position);
                    var length = Vector3.Distance(upper, lower);
                    var axis = (lower - upper).normalized;
                    for (var i = 0; i < sourceTriangles.Length; i += 3)
                    {
                        var ids = new[] { sourceTriangles[i], sourceTriangles[i + 1], sourceTriangles[i + 2] };
                        if (!ids.Any(k => DominantBone(sourceWeights[k]) == upperIndex)) continue;
                        var polygon = ids.Select(k => Surface(k, false)).ToList();
                        polygon = ClipCloth(polygon, v => length * 0.65f - Vector3.Dot(v.Point - upper, axis));
                        Polygon(polygon);
                    }
                }
                mesh = new Mesh { name = "Figure Shawl" };
                mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
                mesh.boneWeights = weights.ToArray(); mesh.bindposes = body.sharedMesh.bindposes;
                mesh.normals = WeldedNormals(mesh); mesh.RecalculateBounds();
                AssetDatabase.CreateAsset(mesh, path);
            }
            var go = new GameObject("Shawl");
            go.transform.SetParent(body.transform, false);
            var cloth = go.AddComponent<SkinnedMeshRenderer>();
            cloth.sharedMesh = mesh; cloth.bones = body.bones; cloth.rootBone = body.rootBone;
            cloth.localBounds = body.localBounds;
            DrawAsFigure(cloth, art);
            cloth.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            cloth.receiveShadows = false;
            return cloth;
        }

        static Matrix4x4[] SkinMatrices(SkinnedMeshRenderer body) => body.bones
            .Select((bone, i) => bone.localToWorldMatrix * body.sharedMesh.bindposes[i]).ToArray();

        // Mesh vertices are in bind space, not necessarily the imported skeleton's current pose. Author cloth
        // in the posed figure's space and invert the weighted skin matrix to put each new vertex back in bind space.
        static Matrix4x4 SkinMatrix(Matrix4x4[] matrices, BoneWeight w)
        {
            var result = new Matrix4x4();
            void Add(int bone, float weight)
            {
                if (weight == 0) return;
                for (var row = 0; row < 4; row++)
                    for (var column = 0; column < 4; column++) result[row, column] += matrices[bone][row, column] * weight;
            }
            Add(w.boneIndex0, w.weight0); Add(w.boneIndex1, w.weight1);
            Add(w.boneIndex2, w.weight2); Add(w.boneIndex3, w.weight3);
            return result;
        }

        struct ClothVertex { public Vector3 Point; public Vector2 UV; public BoneWeight Weight; }

        static List<ClothVertex> ClipCloth(List<ClothVertex> input, System.Func<ClothVertex, float> distance)
        {
            var result = new List<ClothVertex>();
            if (input.Count == 0) return result;
            var previous = input[input.Count - 1]; var previousDistance = distance(previous);
            foreach (var current in input)
            {
                var currentDistance = distance(current);
                if ((previousDistance >= 0) != (currentDistance >= 0))
                {
                    var t = previousDistance / (previousDistance - currentDistance);
                    result.Add(new ClothVertex { Point = Vector3.Lerp(previous.Point, current.Point, t),
                        UV = Vector2.Lerp(previous.UV, current.UV, t), Weight = BlendWeights(previous.Weight, current.Weight, t) });
                }
                if (currentDistance >= 0) result.Add(current);
                previous = current; previousDistance = currentDistance;
            }
            return result;
        }

        static BoneWeight BlendWeights(BoneWeight a, BoneWeight b, float t)
        {
            var weights = new Dictionary<int, float>();
            void Add(int bone, float weight) => weights[bone] = (weights.TryGetValue(bone, out var value) ? value : 0) + weight;
            Add(a.boneIndex0, a.weight0 * (1 - t)); Add(a.boneIndex1, a.weight1 * (1 - t));
            Add(a.boneIndex2, a.weight2 * (1 - t)); Add(a.boneIndex3, a.weight3 * (1 - t));
            Add(b.boneIndex0, b.weight0 * t); Add(b.boneIndex1, b.weight1 * t);
            Add(b.boneIndex2, b.weight2 * t); Add(b.boneIndex3, b.weight3 * t);
            var best = weights.OrderByDescending(kv => kv.Value).Take(4).ToArray();
            var total = best.Sum(kv => kv.Value); var result = new BoneWeight();
            if (best.Length > 0) { result.boneIndex0 = best[0].Key; result.weight0 = best[0].Value / total; }
            if (best.Length > 1) { result.boneIndex1 = best[1].Key; result.weight1 = best[1].Value / total; }
            if (best.Length > 2) { result.boneIndex2 = best[2].Key; result.weight2 = best[2].Value / total; }
            if (best.Length > 3) { result.boneIndex3 = best[3].Key; result.weight3 = best[3].Value / total; }
            return result;
        }

        const string FigureControllerPath = "Assets/Sankofa/Art/Generated/Figure.controller";
        const string FigureMaskPath = "Assets/Sankofa/Art/Generated/Figure upper body.mask";

        /// <summary>The figure animator: idle, walk and kneel for the body; give and talk for the upper body.</summary>
        static AnimatorController FigureController(GameObject model)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(SankofaFigureImport.ModelPath).OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
            AnimationClip Clip(string name) => clips.TryGetValue(name, out var c) ? c : throw new KeyNotFoundException("Mannequin clip " + name);

            AssetDatabase.DeleteAsset(FigureControllerPath);
            var controller = AnimatorController.CreateAnimatorControllerAtPath(FigureControllerPath);
            controller.AddParameter("Walking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Kneeling", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Talking", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Give", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Pace", AnimatorControllerParameterType.Float);

            var body = controller.layers[0].stateMachine;
            var idle = body.AddState("Idle");
            idle.motion = Clip("Idle_Loop");
            var walk = body.AddState("Walk");
            walk.motion = Clip("Walk_Loop");
            walk.speedParameterActive = true;
            walk.speedParameter = "Pace";
            var kneel = body.AddState("Kneel");
            kneel.motion = KneelingRest(Clip("Fixing_Kneeling"));
            body.defaultState = idle;
            void Link(AnimatorState from, AnimatorState to, string parameter, bool value, float seconds)
            {
                var t = from.AddTransition(to);
                t.hasExitTime = false;
                t.duration = seconds;
                t.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parameter);
            }
            Link(idle, walk, "Walking", true, 0.25f);
            Link(walk, idle, "Walking", false, 0.3f);
            Link(idle, kneel, "Kneeling", true, 0.7f);
            Link(walk, kneel, "Kneeling", true, 0.7f);
            Link(kneel, idle, "Kneeling", false, 0.7f);

            // The upper body gives and talks over whatever the legs are doing, kneeling included.
            AssetDatabase.DeleteAsset(FigureMaskPath);
            var mask = new AvatarMask();
            var paths = model.GetComponentsInChildren<Transform>(true).Select(t => AnimationUtility.CalculateTransformPath(t, model.transform)).ToArray();
            mask.transformCount = paths.Length;
            string[] upper = { "spine", "clavicle", "upperarm", "lowerarm", "hand", "thumb", "index", "middle", "ring", "pinky", "neck", "Head" };
            for (var i = 0; i < paths.Length; i++)
            {
                mask.SetTransformPath(i, paths[i]);
                var leaf = paths[i].Split('/').Last();
                mask.SetTransformActive(i, upper.Any(u => leaf.StartsWith(u)));
            }
            AssetDatabase.CreateAsset(mask, FigureMaskPath);
            controller.AddLayer(new AnimatorControllerLayer
            {
                name = "Upper body",
                defaultWeight = 1f,
                avatarMask = mask,
                stateMachine = new AnimatorStateMachine { name = "Upper body" },
            });
            var layers = controller.layers;
            var upperMachine = layers[1].stateMachine;
            AssetDatabase.AddObjectToAsset(upperMachine, controller);
            var rest = upperMachine.AddState("Rest");
            upperMachine.defaultState = rest;
            var give = upperMachine.AddState("Give");
            give.motion = Clip("Interact");
            var talk = upperMachine.AddState("Talk");
            talk.motion = Clip("Idle_Talking_Loop");
            var toGive = upperMachine.AddAnyStateTransition(give);
            toGive.hasExitTime = false;
            toGive.duration = 0.25f;
            toGive.canTransitionToSelf = false;
            toGive.AddCondition(AnimatorConditionMode.If, 0, "Give");
            var back = give.AddTransition(rest);
            back.hasExitTime = true;
            back.exitTime = 0.95f;
            back.duration = 0.35f;
            Link(rest, talk, "Talking", true, 0.4f);
            Link(talk, rest, "Talking", false, 0.5f);
            controller.layers = layers;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>Smooth normals that ignore UV seams: vertices at the same place share one normal.</summary>
        static Vector3[] WeldedNormals(Mesh mesh)
        {
            var vertices = mesh.vertices;
            var sums = new Dictionary<Vector3Int, Vector3>();
            Vector3Int Key(Vector3 v) => Vector3Int.RoundToInt(v * 10000f);
            for (var sub = 0; sub < mesh.subMeshCount; sub++)
            {
                var triangles = mesh.GetTriangles(sub);
                for (var t = 0; t < triangles.Length; t += 3)
                {
                    Vector3 a = vertices[triangles[t]], b = vertices[triangles[t + 1]], c = vertices[triangles[t + 2]];
                    var face = Vector3.Cross(b - a, c - a);
                    foreach (var k in new[] { triangles[t], triangles[t + 1], triangles[t + 2] })
                    {
                        var key = Key(vertices[k]);
                        sums[key] = (sums.TryGetValue(key, out var sum) ? sum : Vector3.zero) + face;
                    }
                }
            }
            return vertices.Select(v => sums.TryGetValue(Key(v), out var n) && n.sqrMagnitude > 0 ? n.normalized : Vector3.up).ToArray();
        }

        static AnimatorController figureController;
        static Mesh slenderBody;
        static bool clothedBodyFinished;
        const string SlenderBodyPath = "Assets/Sankofa/Art/Generated/Figure body.asset";
        const string FigureEyesPath = "Assets/Sankofa/Art/Generated/Figure eyes.asset";

        static void BlinkEyes(Transform figure, SkinnedMeshRenderer eyes)
        {
            var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(FigureEyesPath);
            if (mesh == null)
            {
                var source = eyes.sharedMesh;
                mesh = new Mesh { name = "Figure eyes" };
                mesh.vertices = source.vertices; mesh.normals = source.normals; mesh.uv = source.uv;
                mesh.triangles = source.triangles; mesh.boneWeights = source.boneWeights; mesh.bindposes = source.bindposes;
                var matrices = SkinMatrices(eyes);
                var vertices = source.vertices;
                var weights = source.boneWeights;
                var points = vertices.Select((v, i) => figure.InverseTransformPoint(SkinMatrix(matrices, weights[i]).MultiplyPoint3x4(v))).ToArray();
                var centreX = points.Average(p => p.x);
                var leftY = points.Where(p => p.x < centreX).Average(p => p.y);
                var rightY = points.Where(p => p.x >= centreX).Average(p => p.y);
                var colours = new Color[vertices.Length];
                var groups = new[] { points.Where(p => p.x < centreX).ToArray(), points.Where(p => p.x >= centreX).ToArray() };
                var delta = new Vector3[vertices.Length];
                for (var i = 0; i < vertices.Length; i++)
                {
                    var p = points[i];
                    var centreY = p.x < centreX ? leftY : rightY;
                    var group = groups[p.x < centreX ? 0 : 1];
                    var x = group.Average(v => v.x); var z = group.Average(v => v.z);
                    var radius = (group.Max(v => v.x) - group.Min(v => v.x)) * 0.24f;
                    var r = new Vector2(p.x - x, p.y - centreY).magnitude;
                    var pupil = p.z > z ? 1 - Mathf.SmoothStep(radius * 0.7f, radius * 1.2f, r) : 0;
                    colours[i] = Color.Lerp(new Color(0.34f, 0.30f, 0.24f), new Color(0.02f, 0.015f, 0.01f), pupil);
                    p.y = Mathf.Lerp(centreY, p.y, 0.08f);
                    delta[i] = SkinMatrix(matrices, weights[i]).inverse.MultiplyPoint3x4(figure.TransformPoint(p)) - vertices[i];
                }
                mesh.AddBlendShapeFrame("Blink", 100, delta, new Vector3[delta.Length], new Vector3[delta.Length]);
                mesh.colors = colours;
                mesh.RecalculateBounds(); AssetDatabase.CreateAsset(mesh, FigureEyesPath);
            }
            eyes.sharedMesh = mesh;
        }

        static int DominantBone(BoneWeight w)
        {
            int bone = w.boneIndex0;
            float weight = w.weight0;
            if (w.weight1 > weight) { bone = w.boneIndex1; weight = w.weight1; }
            if (w.weight2 > weight) { bone = w.boneIndex2; weight = w.weight2; }
            if (w.weight3 > weight) bone = w.boneIndex3;
            return bone;
        }

        /// <summary>
        /// The CC0 base character with a narrower torso, preserving the artist's head, joints and fingers.
        /// Skinning is unchanged, so every library clip still fits.
        /// </summary>
        static Mesh SlenderBody(SkinnedMeshRenderer renderer)
        {
            var original = renderer.sharedMesh;
            var mesh = new Mesh { name = "Figure body", indexFormat = original.indexFormat };
            // Retain the artist's anatomical head, joints and individual fingers. The previous radial projection
            // collapsed hands into mittens and pulled shoulders into balls; proportions belong to the mesh.
            var vertices = original.vertices;
            var names = renderer.bones.Select(b => b.name).ToArray();
            var weights = original.boneWeights;
            for (var i = 0; i < vertices.Length; i++)
            {
                var w = weights[i];
                var n = names[DominantBone(w)];
                if (n.StartsWith("spine") || n.StartsWith("clavicle")) vertices[i].x *= 0.9f;
            }
            mesh.vertices = vertices;
            mesh.uv = original.uv;
            mesh.colors = original.colors;
            // One submesh, so the figure's depth material covers all of it.
            var sourceTriangles = Enumerable.Range(0, original.subMeshCount).SelectMany(original.GetTriangles).ToArray();
            // Keep all source surfaces until the garments are derived; trimming legs here also removed
            // hip triangles needed to close the shirt's tucked hem.
            mesh.subMeshCount = 1;
            mesh.SetTriangles(sourceTriangles, 0);
            mesh.boneWeights = weights;
            mesh.bindposes = renderer.sharedMesh.bindposes;
            mesh.normals = WeldedNormals(mesh);
            mesh.RecalculateBounds();
            AssetDatabase.DeleteAsset(SlenderBodyPath);
            AssetDatabase.CreateAsset(mesh, SlenderBodyPath);
            return mesh;
        }

        /// <summary>
        /// A skinned figure body under a figure root: the base character, scaled to the figure's height, drawn only by the
        /// ember shader, animated by the figure controller. Returns its renderer, animator and hand and neck bones.
        /// </summary>
        static (Renderer Body, Animator Animator, Transform Hand, Transform Neck, Renderer[] Cloth) SkinnedBody(Transform figureRoot, SankofaArtSet art, float height)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(SankofaFigureImport.CharacterPath);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, figureRoot);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Body";
            model.transform.localPosition = Vector3.zero;
            // The character faces the file's −Z; our figures face +Z toward whoever they turn to.
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            if (figureController == null) figureController = FigureController(model);
            var renderer = model.GetComponentsInChildren<SkinnedMeshRenderer>().OrderByDescending(r => r.sharedMesh.vertexCount).First();
            if (slenderBody == null)
            {
                AssetDatabase.DeleteAsset("Assets/Sankofa/Art/Generated/Figure Wrap.asset");
                AssetDatabase.DeleteAsset("Assets/Sankofa/Art/Generated/Figure Shawl.asset");
                AssetDatabase.DeleteAsset(FigureEyesPath);
                slenderBody = SlenderBody(renderer);
            }
            renderer.sharedMesh = slenderBody;
            Transform Bone(string name) => model.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
            // Scale to the figure's standing height, measured from the skeleton as imported: the head joint sits at
            // about nine tenths of a standing person's height. (The mesh's own bounds are in the file's units.)
            var headHeight = Bone("Head").position.y - model.transform.position.y;
            var scale = height * 0.89f / Mathf.Max(0.01f, headHeight);
            // Slighter than the source's athletic build, narrowest across the shoulders.
            model.transform.localScale = new Vector3(scale * 0.84f, scale, scale * 0.88f);
            DrawAsFigure(renderer, art);
            renderer.updateWhenOffscreen = false;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            if (!model.TryGetComponent<Animator>(out var animator)) animator = model.AddComponent<Animator>();
            animator.runtimeAnimatorController = figureController;
            animator.applyRootMotion = false;
            // Always posed: carried things follow the hand bones even when nothing is looking at them.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            // A wrapped cloth from the waist to below the knee and a cloth over the shoulders, both plain: what the
            // women wear is for family review. Each moves with its bone and is drawn in the same light.
            var h = height;
            var wrap = Cloth(figureRoot, art, renderer, "Wrap", h, new List<Vector2>
            {
                new Vector2(0.17f, 0.12f), new Vector2(0.18f, 0.26f), new Vector2(0.164f, 0.4f),
                new Vector2(0.15f, 0.49f), new Vector2(0.126f, 0.58f), new Vector2(0.106f, 0.66f),
            });
            var shawl = Cloth(figureRoot, art, renderer, "Shawl", h, new List<Vector2>
            {
                new Vector2(0.125f, 0.53f), new Vector2(0.14f, 0.64f), new Vector2(0.16f, 0.74f),
                new Vector2(0.168f, 0.8f), new Vector2(0.12f, 0.84f), new Vector2(0.047f, 0.868f),
            });
            // The garment is the visible torso. Removing covered skin avoids animated chest/shoulder
            // intersections rather than drawing two competing surfaces. Keep the complete source while
            // deriving weighted sleeves, and keep exposed neck, head, arms, hands and feet afterwards.
            if (!clothedBodyFinished)
            {
                clothedBodyFinished = true;
                var names = renderer.bones.Select(b => b.name).ToArray();
                var weights = slenderBody.boneWeights;
                var source = slenderBody.triangles;
                bool Covered(int i) { var n = names[DominantBone(weights[i])]; return n.StartsWith("spine") || n.StartsWith("clavicle") || n.StartsWith("thigh") || n.StartsWith("calf"); }
                var kept = new List<int>();
                for (var i = 0; i < source.Length; i += 3)
                    if (!Covered(source[i]) && !Covered(source[i + 1]) && !Covered(source[i + 2]))
                        kept.AddRange(new[] { source[i], source[i + 1], source[i + 2] });
                slenderBody.SetTriangles(kept, 0);
                EditorUtility.SetDirty(slenderBody);
            }
            var detail = model.GetComponentsInChildren<SkinnedMeshRenderer>().Where(r => r != renderer && r != wrap && r != shawl).Cast<Renderer>().ToList();
            foreach (var r in detail)
            {
                if (r.name == "Eyes") BlinkEyes(figureRoot, (SkinnedMeshRenderer)r);
                DrawAsFigure(r, art);
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
            var hairSource = AssetDatabase.LoadAssetAtPath<GameObject>(SankofaFigureImport.HairPath).GetComponentInChildren<SkinnedMeshRenderer>();
            var hair = new GameObject("Hair").AddComponent<SkinnedMeshRenderer>();
            hair.transform.SetParent(renderer.transform, false);
            hair.sharedMesh = hairSource.sharedMesh;
            hair.bones = hairSource.bones.Select(b => Bone(b.name)).ToArray();
            hair.rootBone = renderer.rootBone;
            hair.localBounds = renderer.localBounds;
            DrawAsFigure(hair, art);
            hair.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hair.receiveShadows = false;
            detail.Add(hair);
            return (renderer, animator, Bone("hand_r"), Bone("neck_01"), new[] { wrap, shawl }.Concat(detail).ToArray());
        }
    }
}
