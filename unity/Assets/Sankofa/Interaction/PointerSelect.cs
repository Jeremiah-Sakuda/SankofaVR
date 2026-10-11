using System;

namespace Sankofa.Interaction
{
    /// <summary>
    /// Selecting by pointing, for one pointer: a pinch or trigger that begins over a button and ends over the same
    /// button presses it, as Quest's own panels do. Sweeping across buttons while held, moving off before letting go,
    /// or losing the pointer presses nothing. Separate press and release strengths keep a wavering pinch from
    /// chattering.
    /// </summary>
    public sealed class PointerSelect
    {
        readonly double press;
        readonly double release;
        string armed;
        bool needsNeutral;
        int? source;

        public PointerSelect(double press = 0.85, double release = 0.5)
        {
            if (!(press > release)) throw new ArgumentException("The press strength must exceed the release strength");
            this.press = press;
            this.release = release;
        }

        /// <summary>True while the pinch or trigger is held.</summary>
        public bool Held { get; private set; }

        /// <summary>
        /// Updates with whether the pointer is tracked, its select strength (0..1) and the button it points at, if
        /// any. Returns the button pressed on this frame, or null.
        /// </summary>
        public string Update(bool tracked, double strength, string hovered, int sourceId = 0)
        {
            if (source.HasValue && source.Value != sourceId) needsNeutral = true;
            source = sourceId;
            if (needsNeutral)
            {
                Held = false;
                armed = null;
                if (tracked && strength <= release) needsNeutral = false;
                return null;
            }
            if (!tracked)
            {
                needsNeutral = true;
                Held = false;
                armed = null;
                return null;
            }
            if (!Held)
            {
                if (strength < press) return null;
                Held = true;
                armed = hovered;
                return null;
            }
            if (strength > release)
            {
                if (hovered != armed) armed = null;
                return null;
            }
            Held = false;
            var pressed = armed != null && armed == hovered ? armed : null;
            armed = null;
            return pressed;
        }
    }
}
