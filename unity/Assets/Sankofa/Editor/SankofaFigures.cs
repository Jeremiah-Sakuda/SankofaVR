using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Sankofa.EditorTools
{
    /// <summary>
    /// Import rules for the story figures' mannequin: Quaternius's Universal Animation Library (CC0; see
    /// Art/Figures/UAL-License.txt), a faceless humanoid with walk, idle, kneel, reach and talk clips. Its materials
    /// are not imported (the figures are drawn by the ember shader), the looping clips loop, and its bones stay
    /// exposed so carried things can follow the hands.
    /// </summary>
    public sealed class SankofaFigureImport : AssetPostprocessor
    {
        public const string ModelPath = "Assets/Sankofa/Art/Figures/UAL1_Standard.fbx";

        // Bumped whenever these rules change, so Unity reimports the model.
        public override uint GetVersion() => 2;

        void OnPreprocessModel()
        {
            if (assetPath != ModelPath) return;
            var importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Generic;
            importer.materialImportMode = ModelImporterMaterialImportMode.None;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.optimizeGameObjects = false;
            importer.meshCompression = ModelImporterMeshCompression.Medium;
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
        /// <summary>
        /// Draws a figure part in ember light, with its depth written first (see EmberFigureDepth.shader) so that
        /// all a figure's parts together show only their nearest surface. The part must be a single submesh.
        /// </summary>
        public static void DrawAsFigure(Renderer renderer, SankofaArtSet art) =>
            renderer.sharedMaterials = new[] { art.EmberFigure, art.EmberFigureDepth };

        /// <summary>A plain cloth lathe at a bone, upright with the figure, flattened front to back like a body.</summary>
        static Renderer Cloth(Transform figureRoot, SankofaArtSet art, Transform bone, string name, List<Vector2> profile)
        {
            // The lathe builds its surface from the bottom up, so its faces point outward.
            profile = profile.OrderBy(p => p.y).ToList();
            // The hem and the top edge fold inward, so no open rim catches the light as a bright line.
            var bottom = profile[0];
            var top = profile[profile.Count - 1];
            profile.Insert(0, new Vector2(bottom.x * 0.82f, bottom.y + 0.012f));
            profile.Add(new Vector2(top.x * 0.82f, top.y - 0.012f));
            var mesh = SankofaStoryMeshes.Lathe(figureRoot.name + " " + name, profile, 28, (a, y) => new Color(0, 0, 0, 1), name.Length);
            var filter = Static(figureRoot, name, mesh, art.EmberFigure);
            SaveMeshes(new[] { filter });
            var t = filter.transform;
            t.position = bone.position;
            t.rotation = figureRoot.rotation;
            t.localScale = new Vector3(1f, 1f, 0.85f);
            t.SetParent(bone, true);
            var r = filter.GetComponent<Renderer>();
            DrawAsFigure(r, art);
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return r;
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
            kneel.motion = Clip("Fixing_Kneeling");
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
        const string SlenderBodyPath = "Assets/Sankofa/Art/Generated/Figure body.asset";

        /// <summary>
        /// The mannequin reshaped for our figures, in its bind pose: arms drawn in toward their bones, a narrower,
        /// flatter chest and slighter shoulders, and the head smoothed to a plain ovoid, so the ember rim never
        /// picks out a brow or a nose. Skinning is unchanged, so every clip still fits.
        /// </summary>
        static Mesh SlenderBody(SkinnedMeshRenderer renderer)
        {
            var source = renderer.sharedMesh;
            var mesh = Object.Instantiate(source);
            mesh.name = "Figure body";
            var names = renderer.bones.Select(b => b.name).ToArray();
            var origins = mesh.bindposes.Select(m => m.inverse.MultiplyPoint3x4(Vector3.zero)).ToArray();
            int Index(string name) => System.Array.IndexOf(names, name);
            // Each limb bone runs to the joint of the next.
            var limbs = new Dictionary<int, (Vector3 From, Vector3 To, float Keep)>();
            // Arms become smooth tapered sleeves of light, start and end radius along each bone, so no ball joint,
            // wrist ring or knuckle catches the rim. Fingers fold into the hand, which reads as a closed mitten.
            var tubes = new Dictionary<int, (Vector3 From, Vector3 To, float R0, float R1)>();
            var fingers = new Dictionary<int, int>();
            foreach (var side in new[] { "_l", "_r" })
            {
                void Limb(string bone, string next, float keep)
                {
                    int a = Index(bone), b = Index(next);
                    if (a >= 0 && b >= 0) limbs[a] = (origins[a], origins[b], keep);
                }
                void Tube(string bone, string next, float r0, float r1)
                {
                    int a = Index(bone), b = Index(next);
                    if (a >= 0 && b >= 0) tubes[a] = (origins[a], origins[b], r0, r1);
                }
                // Radii as fractions of the forearm's length, so they follow the mannequin's own scale.
                // Full at the shoulder, so the arm grows out of the body rather than hanging from a ball joint.
                Tube("upperarm" + side, "lowerarm" + side, 0.27f, 0.19f);
                Tube("lowerarm" + side, "hand" + side, 0.19f, 0.13f);
                Limb("clavicle" + side, "upperarm" + side, 0.8f);
                Limb("thigh" + side, "calf" + side, 0.88f);
                // The mannequin's hands are modelled as fists; drawn in, they read as a relaxed hand rather than a punch.
                int lower = Index("lowerarm" + side), hand = Index("hand" + side);
                if (lower >= 0 && hand >= 0)
                {
                    var forearm = origins[hand] - origins[lower];
                    tubes[hand] = (origins[hand], origins[hand] + forearm * 0.42f, 0.12f, 0.07f);
                    foreach (var finger in new[] { "thumb", "index", "middle", "ring", "pinky" })
                        foreach (var joint in new[] { "_01", "_02", "_03", "_04_leaf" })
                        {
                            var f = Index(finger + joint + side);
                            if (f >= 0) fingers[f] = hand;
                        }
                }
            }
            // Tube radii in mesh units: a fraction of the forearm's length.
            var forearmLength = 1f;
            {
                int lower = Index("lowerarm_r"), hand = Index("hand_r");
                if (lower >= 0 && hand >= 0) forearmLength = (origins[hand] - origins[lower]).magnitude;
            }
            int chest = Index("spine_03"), upperChest = Index("spine_02"), head = Index("Head"), neck = Index("neck_01");
            var vertices = mesh.vertices;
            var weights = mesh.boneWeights;
            // The head's ovoid: centred on the vertices that are all head, as wide and deep as they are.
            var headPoints = Enumerable.Range(0, vertices.Length).Where(i => weights[i].boneIndex0 == head && weights[i].weight0 > 0.95f).Select(i => vertices[i]).ToList();
            if (headPoints.Count == 0) throw new System.InvalidOperationException("The mannequin has no vertices bound only to its Head bone.");
            var headMin = headPoints.Aggregate(Vector3.Min);
            var headMax = headPoints.Aggregate(Vector3.Max);
            var headCentre = (headMin + headMax) * 0.5f;
            var headRadii = (headMax - headMin) * 0.5f;
            var spineAxis = chest >= 0 ? origins[chest] : Vector3.zero;
            for (var i = 0; i < vertices.Length; i++)
            {
                var v = vertices[i];
                var w = weights[i];
                var moved = Vector3.zero;
                void Pull(int bone, float weight)
                {
                    if (weight <= 0f) return;
                    if (fingers.TryGetValue(bone, out var palm)) bone = palm;
                    if (tubes.TryGetValue(bone, out var tube))
                    {
                        var axis = tube.To - tube.From;
                        var t = Mathf.Clamp01(Vector3.Dot(v - tube.From, axis) / axis.sqrMagnitude);
                        var onAxis = tube.From + axis * t;
                        var radial = v - onAxis;
                        var radius = Mathf.Lerp(tube.R0, tube.R1, t) * forearmLength;
                        var target = radial.sqrMagnitude > 1e-10f ? onAxis + radial.normalized * radius : onAxis;
                        moved += weight * (target - v);
                    }
                    else if (limbs.TryGetValue(bone, out var limb))
                    {
                        var axis = limb.To - limb.From;
                        var t = Mathf.Clamp01(Vector3.Dot(v - limb.From, axis) / axis.sqrMagnitude);
                        var onAxis = limb.From + axis * t;
                        moved += weight * (onAxis + (v - onAxis) * limb.Keep - v);
                    }
                    else if (bone == chest || bone == upperChest)
                    {
                        // Narrower across and shallower front to back about the spine.
                        var target = new Vector3(spineAxis.x + (v.x - spineAxis.x) * 0.86f, v.y, spineAxis.z + (v.z - spineAxis.z) * 0.84f);
                        moved += weight * (target - v);
                    }
                    else if (bone == neck)
                    {
                        // The neck drawn in a little; its tendons are smoothed by the welded normals.
                        var axis = new Vector3(origins[neck].x, v.y, origins[neck].z);
                        moved += weight * (axis + (v - axis) * 0.88f - v);
                    }
                    else if (bone == head)
                    {
                        var d = v - headCentre;
                        var unit = new Vector3(d.x / headRadii.x, d.y / headRadii.y, d.z / headRadii.z);
                        // A little fuller than the mannequin's, so the head does not read small on its neck.
                        var onOvoid = headCentre + Vector3.Scale(unit.normalized, headRadii * 1.15f);
                        moved += weight * (onOvoid - v);
                    }
                }
                Pull(w.boneIndex0, w.weight0);
                Pull(w.boneIndex1, w.weight1);
                Pull(w.boneIndex2, w.weight2);
                Pull(w.boneIndex3, w.weight3);
                vertices[i] = v + moved;
            }
            mesh.vertices = vertices;
            // The fingers are now part of the hand: bind them to it, so a clip's finger poses cannot open the mitten.
            for (var i = 0; i < weights.Length; i++)
            {
                var w = weights[i];
                if (fingers.TryGetValue(w.boneIndex0, out var h0)) w.boneIndex0 = h0;
                if (fingers.TryGetValue(w.boneIndex1, out var h1)) w.boneIndex1 = h1;
                if (fingers.TryGetValue(w.boneIndex2, out var h2)) w.boneIndex2 = h2;
                if (fingers.TryGetValue(w.boneIndex3, out var h3)) w.boneIndex3 = h3;
                weights[i] = w;
            }
            mesh.boneWeights = weights;
            // One submesh, so the figure's depth material covers all of it.
            var all = Enumerable.Range(0, mesh.subMeshCount).SelectMany(mesh.GetTriangles).ToArray();
            mesh.subMeshCount = 1;
            mesh.SetTriangles(all, 0);
            mesh.normals = WeldedNormals(mesh);
            mesh.RecalculateBounds();
            AssetDatabase.DeleteAsset(SlenderBodyPath);
            AssetDatabase.CreateAsset(mesh, SlenderBodyPath);
            return mesh;
        }

        /// <summary>
        /// A skinned figure body under a figure root: the mannequin, scaled to the figure's height, drawn only by the
        /// ember shader, animated by the figure controller. Returns its renderer, animator and hand and neck bones.
        /// </summary>
        static (Renderer Body, Animator Animator, Transform Hand, Transform Neck, Renderer[] Cloth) SkinnedBody(Transform figureRoot, SankofaArtSet art, float height)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(SankofaFigureImport.ModelPath);
            var model = (GameObject)PrefabUtility.InstantiatePrefab(asset, figureRoot);
            PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            model.name = "Body";
            model.transform.localPosition = Vector3.zero;
            // The mannequin faces the file's −Z; our figures face +Z toward whoever they turn to.
            model.transform.localRotation = Quaternion.Euler(0, 180, 0);
            if (figureController == null) figureController = FigureController(model);
            var renderer = model.GetComponentInChildren<SkinnedMeshRenderer>();
            if (slenderBody == null) slenderBody = SlenderBody(renderer);
            renderer.sharedMesh = slenderBody;
            Transform Bone(string name) => model.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
            // Scale to the figure's standing height, measured from the skeleton as imported: the head joint sits at
            // about nine tenths of a standing person's height. (The mesh's own bounds are in the file's units.)
            var headHeight = Bone("Head").position.y - model.transform.position.y;
            var scale = height * 0.89f / Mathf.Max(0.01f, headHeight);
            // Slighter than the mannequin's athletic build, narrowest across the shoulders.
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
            var wrap = Cloth(figureRoot, art, Bone("pelvis"), "Wrap", new List<Vector2>
            {
                new Vector2(h * 0.105f, h * 0.075f), new Vector2(h * 0.125f, h * 0.0f), new Vector2(h * 0.15f, -h * 0.14f),
                // Flaring to the hem, so a stepping leg stays inside it.
                new Vector2(h * 0.172f, -h * 0.3f), new Vector2(h * 0.182f, -h * 0.4f),
            });
            var shawl = Cloth(figureRoot, art, Bone("spine_03"), "Shawl", new List<Vector2>
            {
                // Falls from the neck over sloping shoulders and narrows to the waist, rather than squaring them.
                // Close about the neck, so no open collar catches the rim when the head bows.
                new Vector2(h * 0.034f, h * 0.135f), new Vector2(h * 0.07f, h * 0.1f), new Vector2(h * 0.085f, h * 0.075f), new Vector2(h * 0.11f, h * 0.0f),
                // The hem tucks in under the wrap, so no lip of it catches the light across the hip.
                new Vector2(h * 0.115f, -h * 0.12f), new Vector2(h * 0.112f, -h * 0.27f), new Vector2(h * 0.094f, -h * 0.33f),
            });
            return (renderer, animator, Bone("hand_r"), Bone("neck_01"), new[] { wrap, shawl });
        }
    }
}
