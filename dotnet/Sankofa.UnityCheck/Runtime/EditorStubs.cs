// Editor-only names the runtime scripts touch under UNITY_EDITOR.
#if UNITY_EDITOR
namespace UnityEditor { public static class EditorApplication { public static bool isPlaying { get; set; } public static bool isPaused { get; set; } } }
#endif
namespace UnityEngine.InputSystem
{
    public class Vector2Control { public UnityEngine.Vector2 ReadValue() => default; }
    public class ButtonControl { public bool isPressed => false; public bool wasPressedThisFrame => false; }
    public class Mouse { public static Mouse current => null; public Vector2Control position => null; public ButtonControl leftButton => null; public ButtonControl rightButton => null; }
    public class Keyboard { public static Keyboard current => null; }
}
