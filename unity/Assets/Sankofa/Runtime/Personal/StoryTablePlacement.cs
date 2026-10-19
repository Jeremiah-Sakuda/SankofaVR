using System;
using System.Linq;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace Sankofa.Runtime.Personal
{
    /// <summary>Scene data stays on the headset. A table changes the deck's height and usable size.</summary>
    public sealed class StoryTablePlacement : MonoBehaviour
    {
        StoryTableView view;
        Transform head;
        MRUK roomKit;
        OVRPassthroughLayer passthrough;
        bool loading, fitted, enabledRoom;
        int placementVersion;
        float heightAdjustment;
        bool usedFallbackHead;
        public bool UsesSurface => fitted;
        public bool PassthroughActive => enabledRoom;
        public bool Loading => loading;
        public string Status { get; private set; } = "Adjustable story stand";
        public event Action<string> StatusChanged;

        public void Configure(StoryTableView tableView, Transform eye)
        {
            view = tableView; head = eye;
            Recenter();
#if UNITY_ANDROID && !UNITY_EDITOR
            StartCoroutine(EnableRoomWhenReady());
#endif
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        System.Collections.IEnumerator EnableRoomWhenReady()
        {
            for (var i = 0; i < 50 && !UnityEngine.XR.XRSettings.isDeviceActive; i++) yield return new WaitForSecondsRealtime(.1f);
            var manager = FindFirstObjectByType<OVRManager>();
            if (manager != null && OVRManager.IsInsightPassthroughSupported())
            {
                manager.isInsightPassthroughEnabled = true;
                passthrough = manager.gameObject.AddComponent<OVRPassthroughLayer>();
                passthrough.overlayType = OVROverlay.OverlayType.Underlay;
                passthrough.textureOpacity = 1;
                enabledRoom = true;
                var camera = head != null ? head.GetComponent<Camera>() : Camera.main;
                if (camera != null) { camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.clear; }
                SetStatus("Your room • story stand ready");
            }
        }
#endif

        public void Recenter()
        {
            if (view == null || head == null) return;
            placementVersion++; fitted = false; loading = false;
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .5f) forward = Vector3.forward;
            usedFallbackHead = head.position.y < .45f;
            var eyePosition = usedFallbackHead ? new Vector3(head.position.x, 1.15f, head.position.z) : head.position;
            var center = eyePosition + forward * .72f;
            center.y = Mathf.Clamp(eyePosition.y - .48f + heightAdjustment, .32f, 1.25f);
            view.FitToSurface(center, Quaternion.LookRotation(forward, Vector3.up), 1.12f, .68f);
            SetVirtualStand(true);
            SetStatus(enabledRoom ? "Your room • adjustable story stand" : "Adjustable story stand");
        }

        public void AdjustHeight(float amount)
        {
            heightAdjustment = Mathf.Clamp(heightAdjustment + amount, -.24f, .24f);
            Recenter();
        }

        /// <summary>Called only after the visitor chooses Fit my table. No room scan is sent to a server.</summary>
        public async void FitRoomTable()
        {
            if (loading || view == null) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            var version = ++placementVersion;
            loading = true; SetStatus("Finding a comfortable table…");
            try
            {
                if (!await RequestScenePermission())
                {
                    if (this != null && version == placementVersion) { Recenter(); SetStatus("Room access was not granted • the story stand is ready"); }
                    return;
                }
                if (this == null || version != placementVersion) return;
                roomKit = MRUK.Instance;
                if (roomKit == null)
                {
                    var go = new GameObject("Local room placement");
                    go.SetActive(false);
                    roomKit = go.AddComponent<MRUK>();
                    roomKit.SceneSettings = new MRUK.MRUKSettings { LoadSceneOnStartup = false };
                    go.SetActive(true);
                }
                var load = roomKit.LoadSceneFromDevice(requestSceneCaptureIfNoDataFound: false);
                if (await System.Threading.Tasks.Task.WhenAny(load, System.Threading.Tasks.Task.Delay(8000)) != load)
                {
                    if (this != null && version == placementVersion) { Recenter(); SetStatus("Room placement took too long • the story stand is ready"); }
                    return;
                }
                var result = await load;
                if (this == null || version != placementVersion) return;
                if (result != MRUK.LoadDeviceResult.Success)
                {
                    Recenter(); SetStatus(result == MRUK.LoadDeviceResult.NoScenePermission
                        ? "Room access was not granted • the story stand is ready"
                        : "No room data is available • the story stand is ready");
                    return;
                }
                var room = roomKit.GetCurrentRoom();
                var candidates = room == null ? null : room.Anchors.Where(a => a != null && a.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE)).ToArray();
                MRUKAnchor best = null; Vector3 at = default; float width = 0, depth = 0, distance = float.MaxValue;
                if (candidates != null)
                    foreach (var table in candidates)
                    {
                        var b = table.VolumeBounds;
                        var p = table.PlaneRect;
                        if (!b.HasValue && !p.HasValue) continue;
                        var center = b.HasValue ? table.transform.TransformPoint(new Vector3(b.Value.center.x, b.Value.center.y, b.Value.max.z)) : table.transform.TransformPoint(new Vector3(p.Value.center.x, p.Value.center.y, 0));
                        // Scene table planes face up; use world bounds to tolerate rotated furniture.
                        var normal = table.transform.forward;
                        if (Vector3.Dot(normal, Vector3.up) < .99f) continue;
                        var w = b.HasValue ? b.Value.size.x : p.Value.width;
                        var d = b.HasValue ? b.Value.size.y : p.Value.height;
                        if (w < .90f || d < .55f || center.y < head.position.y - .85f || center.y > head.position.y - .12f) continue;
                        var horizontal = Vector3.ProjectOnPlane(center - head.position, Vector3.up);
                        if (horizontal.magnitude > 1.25f || Vector3.Dot(horizontal.normalized, Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized) < .25f) continue;
                        if (horizontal.sqrMagnitude >= distance) continue;
                        best = table; at = center + Vector3.up * .012f; width = w; depth = d; distance = horizontal.sqrMagnitude;
                    }
                if (best == null) { Recenter(); SetStatus("No nearby table found • the story stand is ready"); return; }
                var rotation = RotationAlongSurface(best.transform.rotation, at - head.position);
                view.FitToSurface(at, rotation, Mathf.Min(width - .06f, 1.25f), Mathf.Min(depth - .06f, .8f));
                fitted = true; SetVirtualStand(false); SetStatus("Fitted to your table • Recenter restores the stand");
            }
            catch (Exception) { if (this != null && version == placementVersion) { Recenter(); SetStatus("Room placement unavailable • the story stand is ready"); } }
            finally { if (this != null) loading = false; }
#else
            Recenter(); SetStatus("Room fitting needs a Quest • story stand ready");
            await System.Threading.Tasks.Task.CompletedTask;
#endif
        }

        /// <summary>Retains the measured tabletop axes; facing the visitor may flip them, never introduce overhang through a new yaw.</summary>
        public static Quaternion RotationAlongSurface(Quaternion surfaceRotation, Vector3 approach)
        {
            var forward = Vector3.ProjectOnPlane(surfaceRotation * Vector3.up, Vector3.up).normalized;
            if (Vector3.Dot(forward, Vector3.ProjectOnPlane(approach, Vector3.up)) < 0) forward = -forward;
            return Quaternion.LookRotation(forward, Vector3.up);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        static async System.Threading.Tasks.Task<bool> RequestScenePermission()
        {
            var permission = OVRPermissionsRequester.ScenePermission;
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(permission)) return true;
            var answer = new System.Threading.Tasks.TaskCompletionSource<bool>();
            var callbacks = new UnityEngine.Android.PermissionCallbacks();
            callbacks.PermissionGranted += id => answer.TrySetResult(id == permission);
            callbacks.PermissionDenied += id => answer.TrySetResult(false);
            UnityEngine.Android.Permission.RequestUserPermission(permission, callbacks);
            // A silent/no callback permission request cannot strand the table controls.
            var completed = await System.Threading.Tasks.Task.WhenAny(answer.Task, System.Threading.Tasks.Task.Delay(15000));
            return completed == answer.Task && await answer.Task;
        }
#endif

        void SetStatus(string message) { Status = message; StatusChanged?.Invoke(message); }
        void SetVirtualStand(bool value)
        {
            var stand = view != null ? view.TableRoot.Find("Virtual table") : null;
            if (stand != null) stand.gameObject.SetActive(value);
        }
        void Update()
        {
            if (usedFallbackHead && head != null && head.position.y >= .45f && !fitted && !loading) Recenter();
        }
        void OnDestroy() { placementVersion++; if (passthrough != null) Destroy(passthrough); }
    }
}
