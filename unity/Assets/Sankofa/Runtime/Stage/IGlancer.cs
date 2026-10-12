using UnityEngine;

namespace Sankofa.Runtime.Stage
{
    /// <summary>
    /// Someone of the story who can turn toward something for a moment: the visitor who looked at her or acted, or
    /// what the visitor is looking at. Presentation only; nothing in the story advances.
    /// </summary>
    public interface IGlancer
    {
        bool CanGlance { get; }
        void Glance(Vector3 at, float seconds);
    }
}
