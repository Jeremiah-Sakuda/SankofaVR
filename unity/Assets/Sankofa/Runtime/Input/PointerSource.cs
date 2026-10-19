using Sankofa.Interaction;
using Sankofa.Runtime.UI;
using UnityEngine;

namespace Sankofa.Runtime.Input
{
    /// <summary>
    /// Pointing at the panels from where the visitor sits, for one side: a tracked hand's system aim pose with an
    /// index pinch, or else a Touch controller with its trigger, or in the editor with no headset active, the mouse.
    /// A fingertip still presses any button; this adds a way to choose without reaching, which seated visitors with
    /// limited reach and the Meta XR Simulator both need. A select lands on release over the same button.
    /// </summary>
    // After the hands validate their sample (100), before the panels test fingertips and dispatch presses (200).
    [DefaultExecutionOrder(150)]
    public sealed class PointerSource : MonoBehaviour
    {
        [SerializeField] bool left;
        [SerializeField] HandSource hand;
        [Tooltip("The controller anchor on this side; it points along its forward axis.")]
        [SerializeField] Transform controller;
        [SerializeField] PokePanel[] panels;
        [SerializeField] LineRenderer ray;
        [SerializeField] Transform dot;
        [SerializeField] float reach = 4f;
        [Tooltip("With a controller, a short faint ray shows where it points even when no button is under it.")]
        [SerializeField] float idleRay = 0.35f;
        [Tooltip("In the editor with no headset active, the mouse points from this camera.")]
        [SerializeField] Camera eye;
        [SerializeField] bool mouseInEditor;

        enum Source { None, Hand, Controller, Mouse }

        // A pinch moves the aim: forgive a moment off the button it began on.
        readonly PointerSelect select = new PointerSelect(slipGrace: 0.15);
        Source source;

        public bool Tracked => source != Source.None;
        /// <summary>The button this pointer rests on, if any.</summary>
        public string Hovered { get; private set; }

        /// <summary>New modal panels share the same checked pointer path as the permanent controls.</summary>
        public void ConfigurePanels(PokePanel[] targets) => panels = targets ?? new PokePanel[0];

        void Update()
        {
            source = Read(out var pointer, out var strength);
            ProcessPointer(pointer, strength, source != Source.None, (int)source, Time.deltaTime);
        }

        /// <summary>Routes a validated sample through the real panel hit and release adapter.</summary>
        public void ProcessPointer(Ray pointer, float strength, bool tracked, int sourceId, float deltaSeconds)
        {
            PokePanel target = null;
            string id = null;
            var distance = float.MaxValue;
            if (tracked)
                foreach (var panel in panels)
                    if (panel.TryHit(pointer, reach, out var d, out var hit) && d < distance)
                    {
                        distance = d;
                        id = hit;
                        target = panel;
                    }
            Hovered = id;
            if (target != null) target.PointAt(id);
            // Keyed by panel as well as button, so a pinch begun on one panel cannot land on another's button.
            var key = target == null ? null : target.GetInstanceID() + "/" + target.Revision + "/" + id;
            var selected = select.Update(tracked, strength, key, sourceId, deltaSeconds);
            // Slip grace may return the original button while this frame hits nothing. Resolve that key,
            // never this frame's target; removed, hidden or inactive panels cannot receive a stale press.
            if (selected != null)
                foreach (var panel in panels)
                    if (panel != null && panel.isActiveAndEnabled && !panel.Hidden)
                        foreach (var button in panel.Ids)
                            if (selected == panel.GetInstanceID() + "/" + panel.Revision + "/" + button) panel.PressFromPointer(button);
            Draw(pointer, target != null ? distance : 0f);
        }

        Source Read(out Ray pointer, out float strength)
        {
            pointer = default;
            strength = 0;
            var h = hand != null ? hand.Hand : null;
            if (h != null && hand.Holding)
            {
                // Last-known poses are for rendering, not for selecting moving panels.
                return Source.None;
            }
            if (h != null && hand.Tracked && h.IsPointerPoseValid)
            {
                // This hand's finger already at a panel is poking it; its ray yields so one gesture is not read twice.
                // The other hand's pointer is unaffected.
                var tip = hand.Fingertip;
                if (tip.HasValue)
                    foreach (var panel in panels)
                        if (panel.Near(tip.Value)) return Source.None;
                var pose = h.PointerPose;
                pointer = new Ray(pose.position, pose.forward);
                strength = h.GetFingerPinchStrength(OVRHand.HandFinger.Index);
                return Source.Hand;
            }
            var c = left ? OVRInput.Controller.LTouch : OVRInput.Controller.RTouch;
            if (controller != null && OVRInput.IsControllerConnected(c) && OVRInput.GetControllerPositionTracked(c))
            {
                pointer = new Ray(controller.position, controller.forward);
                strength = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, c);
                return Source.Controller;
            }
#if UNITY_EDITOR
            var mouse = UnityEngine.InputSystem.Mouse.current;
            if (mouseInEditor && eye != null && mouse != null && !UnityEngine.XR.XRSettings.isDeviceActive)
            {
                pointer = eye.ScreenPointToRay(mouse.position.ReadValue());
                strength = mouse.leftButton.isPressed ? 1f : 0f;
                return Source.Mouse;
            }
#endif
            return Source.None;
        }

        void Draw(Ray pointer, float hitDistance)
        {
            var length = hitDistance > 0 ? hitDistance : source == Source.Controller ? idleRay : 0f;
            var show = length > 0 && source != Source.Mouse;
            if (ray != null)
            {
                ray.enabled = show;
                if (show)
                {
                    ray.SetPosition(0, pointer.origin + pointer.direction * 0.03f);
                    ray.SetPosition(1, pointer.GetPoint(length - 0.003f));
                    var strong = hitDistance > 0 ? 1f : 0.35f;
                    ray.startColor = new Color(1f, 0.78f, 0.5f, 0f);
                    ray.endColor = new Color(1f, 0.78f, 0.5f, (select.Held ? 0.95f : 0.7f) * strong);
                }
            }
            if (dot != null)
            {
                var onButton = hitDistance > 0;
                dot.gameObject.SetActive(onButton);
                if (onButton)
                {
                    dot.position = pointer.GetPoint(hitDistance - 0.004f);
                    dot.localScale = Vector3.one * (select.Held ? 0.007f : 0.01f);
                }
            }
        }
    }
}
