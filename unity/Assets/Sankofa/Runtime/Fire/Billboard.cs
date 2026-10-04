using UnityEngine;

namespace Sankofa.Runtime.Fire
{
    /// <summary>Turns a card to face the viewer. Upright cards (flames) turn only about the vertical axis.</summary>
    public sealed class Billboard : MonoBehaviour
    {
        [SerializeField] bool upright = true;

        void LateUpdate()
        {
            var camera = Camera.main;
            if (camera == null) return;
            var away = transform.position - camera.transform.position;
            if (upright) away.y = 0;
            if (away.sqrMagnitude < 1e-6f) return;
            transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }
    }
}
