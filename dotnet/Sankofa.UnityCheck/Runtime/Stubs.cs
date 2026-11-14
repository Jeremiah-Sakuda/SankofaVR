// Minimal stand-ins for packages that are not on NuGet, written only so the runtime scripts can be type-checked.
// They carry signatures, never behaviour. Members are added as the unchanged, Unity-verified code asks for them.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace TMPro
{
    public enum TextAlignmentOptions { TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight, Midline, MidlineLeft, MidlineRight, TopJustified, Justified }
    public enum TextOverflowModes { Overflow, Ellipsis, Masking, Truncate, ScrollRect, Page, Linked }
    public enum TextWrappingModes { NoWrap, Normal, PreserveWhitespace, PreserveWhitespaceNoWrap }
    public enum FontStyles { Normal = 0, Bold = 1, Italic = 2, Underline = 4 }
    public class TMP_FontAsset : ScriptableObject { public Material material; }
    public struct TMP_LineInfo { public int firstCharacterIndex, lastCharacterIndex, characterCount; public float lineHeight; }
    public class TMP_TextInfo { public int lineCount, characterCount, pageCount; public TMP_LineInfo[] lineInfo; }
    public abstract class TMP_Text : MonoBehaviour
    {
        public string text { get; set; }
        public float fontSize { get; set; }
        public Color color { get; set; }
        public float alpha { get; set; }
        public TextAlignmentOptions alignment { get; set; }
        public TMP_FontAsset font { get; set; }
        public Material fontSharedMaterial { get; set; }
        public Material fontMaterial { get; set; }
        public bool enableWordWrapping { get; set; }
        public TextWrappingModes textWrappingMode { get; set; }
        public bool richText { get; set; }
        public bool enableAutoSizing { get; set; }
        public float fontSizeMin { get; set; }
        public float fontSizeMax { get; set; }
        public float lineSpacing { get; set; }
        public float characterSpacing { get; set; }
        public FontStyles fontStyle { get; set; }
        public TextOverflowModes overflowMode { get; set; }
        public int maxVisibleCharacters { get; set; }
        public int maxVisibleLines { get; set; }
        public int pageToDisplay { get; set; }
        public Vector4 margin { get; set; }
        public float preferredHeight => 0;
        public float preferredWidth => 0;
        public Bounds bounds => default;
        public Bounds textBounds => default;
        public TMP_TextInfo textInfo => null;
        public RectTransform rectTransform => null;
        public void ForceMeshUpdate(bool ignoreActiveState = false, bool forceTextReparsing = false) { }
        public Vector2 GetPreferredValues(string text, float width, float height) => default;
        public Vector2 GetPreferredValues(float width, float height) => default;
        public Vector2 GetPreferredValues(string text) => default;
        public Vector2 GetPreferredValues() => default;
        public bool isTextOverflowing => false;
        public bool isTextTruncated => false;
        public TMP_TextInfo GetTextInfo(string text) => null;
        public void SetText(string text) { }
    }
    public class TextMeshPro : TMP_Text
    {
        public int sortingOrder { get; set; }
        public Renderer renderer => null;
    }
}

public class OVRBone { public OVRSkeleton.BoneId Id { get; } public Transform Transform { get; } }
public class OVRSkeleton : MonoBehaviour
{
    public enum BoneId { XRHand_Wrist, XRHand_IndexProximal, XRHand_MiddleProximal, XRHand_LittleProximal, XRHand_IndexTip, XRHand_MiddleTip, XRHand_RingTip, XRHand_ThumbTip }
    public bool IsDataValid => false;
    public bool IsInitialized => false;
    public IList<OVRBone> Bones => null;
}
public class OVRHand : MonoBehaviour
{
    public enum HandFinger { Thumb, Index, Middle, Ring, Pinky }
    public bool IsTracked => false;
    public bool IsDataValid => false;
    public bool IsDataHighConfidence => false;
    public bool IsPointerPoseValid => false;
    public Transform PointerPose => null;
    public float GetFingerPinchStrength(HandFinger finger) => 0;
    public bool GetFingerIsPinching(HandFinger finger) => false;
}
public class OVRPassthroughLayer : MonoBehaviour { public float textureOpacity { get; set; } public bool hidden { get; set; } }
public static class OVRPermissionsRequester { public const string ScenePermission = "com.oculus.permission.USE_SCENE"; }
public class OVRManager : MonoBehaviour
{
    public enum FoveatedRenderingLevel { Off, Low, Medium, High, HighTop }
    public static event Action HMDMounted, HMDUnmounted, InputFocusLost, InputFocusAcquired, VrFocusLost, VrFocusAcquired;
    public static OVRManager instance => null;
    public static OVRDisplay display => null;
    public static FoveatedRenderingLevel foveatedRenderingLevel { get; set; }
    public static bool useDynamicFoveatedRendering { get; set; }
    public static bool IsInsightPassthroughSupported() => false;
    public bool isInsightPassthroughEnabled { get; set; }
}

namespace Meta.XR.MRUtilityKit
{
    public class MRUKAnchor : MonoBehaviour
    {
        [Flags] public enum SceneLabels { FLOOR = 1, CEILING = 2, WALL_FACE = 4, TABLE = 8, COUCH = 16, OTHER = 32 }
        public bool HasAnyLabel(SceneLabels labels) => false;
        public Bounds? VolumeBounds => null;
        public Rect? PlaneRect => null;
    }
    public class MRUKRoom : MonoBehaviour { public List<MRUKAnchor> Anchors => null; }
    public class MRUK : MonoBehaviour
    {
        public enum LoadDeviceResult { Success, NoScenePermission, NoRoomsFound, Failure }
        [Serializable] public class MRUKSettings { public bool LoadSceneOnStartup; }
        public static MRUK Instance => null;
        public MRUKSettings SceneSettings;
        public Task<LoadDeviceResult> LoadSceneFromDevice(bool requestSceneCaptureIfNoDataFound = true, bool removeMissingRooms = true) => null;
        public MRUKRoom GetCurrentRoom() => null;
    }
}

public class OVRDisplay { public event Action RecenteredPose; public float displayFrequency { get; set; } public float[] displayFrequenciesAvailable => null; }
public static class OVRInput
{
    public enum Controller { None, LTouch, RTouch, Touch, Hands, LHand, RHand }
    public enum Axis1D { None, PrimaryIndexTrigger, PrimaryHandTrigger, SecondaryIndexTrigger, SecondaryHandTrigger }
    public enum Button { None, One, Two, Three, Four, Start, PrimaryIndexTrigger, PrimaryHandTrigger }
    public static bool IsControllerConnected(Controller controller) => false;
    public static bool GetControllerPositionTracked(Controller controller) => false;
    public static float Get(Axis1D axis, Controller controller = Controller.None) => 0;
    public static bool Get(Button button, Controller controller = Controller.None) => false;
    public static bool GetDown(Button button, Controller controller = Controller.None) => false;
}
