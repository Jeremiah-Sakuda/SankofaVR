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
        /// <summary>A plain cloth lathe at a bone, upright with the figure, flattened front to back like a body.</summary>
        static Renderer Cloth(Transform figureRoot, SankofaArtSet art, Transform bone, string name, List<Vector2> profile)
        {
            // The lathe builds its surface from the bottom up, so its faces point outward.
            profile = profile.OrderBy(p => p.y).ToList();
            var mesh = SankofaStoryMeshes.Lathe(figureRoot.name + " " + name, profile, 28, (a, y) => new Color(0, 0, 0, 1), name.Length);
            var filter = Static(figureRoot, name, mesh, art.EmberFigure);
            SaveMeshes(new[] { filter });
            var t = filter.transform;
            t.position = bone.position;
            t.rotation = figureRoot.rotation;
            t.localScale = new Vector3(1f, 1f, 0.78f);
            t.SetParent(bone, true);
            var r = filter.GetComponent<Renderer>();
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

        static AnimatorController figureController;

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
            Transform Bone(string name) => model.GetComponentsInChildren<Transform>(true).First(t => t.name == name);
            // Scale to the figure's standing height, measured from the skeleton as imported: the head joint sits at
            // about nine tenths of a standing person's height. (The mesh's own bounds are in the file's units.)
            var headHeight = Bone("Head").position.y - model.transform.position.y;
            var scale = height * 0.89f / Mathf.Max(0.01f, headHeight);
            // A little slighter than the mannequin's athletic build.
            model.transform.localScale = new Vector3(scale * 0.9f, scale, scale * 0.9f);
            renderer.sharedMaterials = Enumerable.Repeat(art.EmberFigure, renderer.sharedMaterials.Length).ToArray();
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
                new Vector2(h * 0.165f, -h * 0.3f), new Vector2(h * 0.175f, -h * 0.4f),
            });
            var shawl = Cloth(figureRoot, art, Bone("spine_03"), "Shawl", new List<Vector2>
            {
                new Vector2(h * 0.06f, h * 0.12f), new Vector2(h * 0.15f, h * 0.075f), new Vector2(h * 0.19f, h * 0.02f),
                new Vector2(h * 0.17f, -h * 0.1f), new Vector2(h * 0.145f, -h * 0.22f), new Vector2(h * 0.13f, -h * 0.3f),
            });
            return (renderer, animator, Bone("hand_r"), Bone("neck_01"), new[] { wrap, shawl });
        }
    }
}
