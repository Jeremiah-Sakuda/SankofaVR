using System;
using System.Linq;
using Meta.XR.MRUtilityKit;
using Sankofa.Interaction;
using Sankofa.Runtime.Input;
using UnityEngine;
using Num = System.Numerics;

namespace Sankofa.Runtime.Personal
{
    /// <summary>
    /// Where the story stands. It begins on an adjustable virtual stand in a dark room of its own. When the visitor
    /// asks to use their own table, the room is shown and the story is set on that table: found in the headset's room
    /// scan when there is one, and otherwise wherever the visitor rests a hand. Scene data and hand positions stay on
    /// the headset.
    /// </summary>
    public sealed class StoryTablePlacement : MonoBehaviour
    {
        /// <summary>The visitor's room is shown a little dimmed, as at dusk, so the fire still reads as the light.</summary>
        public const float RoomOpacity = 0.7f;
        /// <summary>A tracked palm lying flat rests about this far above the surface it lies on.</summary>
        public const float PalmAboveSurface = 0.015f;
        /// <summary>How long the visitor has to rest a hand before the stand returns.</summary>
        public const float HandWaitSeconds = 25f;
        /// <summary>The front of the story sits this far beyond the resting hand, and never nearer or farther than these.</summary>
        const float BeyondHand = 0.30f, NearestCentre = 0.62f, FarthestCentre = 0.92f;

        StoryTableView view;
        Transform head;
        HandSource[] hands = Array.Empty<HandSource>();
        RestGesture[] rests = Array.Empty<RestGesture>();
        MRUK roomKit;
        OVRPassthroughLayer passthrough;
        bool loading, fitted, enabledRoom, awaitingHand, eyeRemembered;
        int placementVersion;
        float heightAdjustment, handWaited;
        bool usedFallbackHead;
        CameraClearFlags eyeClear;
        Color eyeBackground;

        public bool UsesSurface => fitted;
        public bool PassthroughActive => enabledRoom;
        public bool Loading => loading;
        /// <summary>The visitor has been asked to rest a hand on their table.</summary>
        public bool AwaitingHand => awaitingHand;
        /// <summary>0 to 1 through the resting hand's hold, for the page to show.</summary>
        public float HandProgress => awaitingHand && rests.Length > 0 ? rests.Max(r => r.Progress) : 0;
        public string Status { get; private set; } = "Adjustable story stand";
        public event Action<string> StatusChanged;
        /// <summary>Deliberate recentering, including recovery of an initial untracked head; height changes are separate.</summary>
        public event Action Recentered;
        /// <summary>Raised before requesting room data or asking for a hand, so immersive presentation can withdraw.</summary>
        public event Action RoomPlacementRequested;
        /// <summary>Raised when the visitor is asked to rest a hand on their table.</summary>
        public event Action HandAsked;
        /// <summary>Raised when that request ends, whether the story was set down, the stand returned or time ran out.</summary>
        public event Action HandSettled;

        /// <summary>
        /// The headset can show the room and read its scan. In the editor this is off unless SANKOFA_ROOM=1 asks for it,
        /// as when recording the Meta XR Simulator with a synthetic room.
        /// </summary>
        static bool RoomCapable =>
#if UNITY_EDITOR
            Environment.GetEnvironmentVariable("SANKOFA_ROOM") == "1";
#elif UNITY_ANDROID
            true;
#else
            false;
#endif

        public void Configure(StoryTableView tableView, Transform eye, HandSource[] trackedHands = null)
        {
            view = tableView; head = eye;
            hands = trackedHands ?? Array.Empty<HandSource>();
            rests = hands.Select(_ => new RestGesture()).ToArray();
            Recenter();
        }

        /// <summary>The eyes, or a seated height when no headset is reporting one.</summary>
        Vector3 Eye() => head.position.y < .45f ? new Vector3(head.position.x, 1.15f, head.position.z) : head.position;

        Vector3 Facing()
        {
            var forward = Vector3.ProjectOnPlane(head.forward, Vector3.up).normalized;
            return forward.sqrMagnitude < .5f ? Vector3.forward : forward;
        }

        /// <summary>
        /// The way the story already faces, level. A visitor resting a hand looks down and toward that hand, so the
        /// head's direction at that moment is not where they sit facing; the story they have been reading is.
        /// </summary>
        Vector3 StoryFacing()
        {
            var forward = view != null ? Vector3.ProjectOnPlane(view.TableRoot.forward, Vector3.up).normalized : Vector3.zero;
            return forward.sqrMagnitude < .5f ? Facing() : forward;
        }

        /// <summary>Returns the story to its virtual stand in front of the visitor, in its own dark room.</summary>
        public void Recenter()
        {
            if (view == null || head == null) return;
            RestoreStand();
            Recentered?.Invoke();
        }

        void RestoreStand()
        {
            if (view == null || head == null) return;
            placementVersion++; fitted = false; loading = false;
            EndHandRequest();
            ShowRoom(false);
            var forward = Facing();
            usedFallbackHead = head.position.y < .45f;
            var eyePosition = Eye();
            var center = eyePosition + forward * .72f;
            center.y = Mathf.Clamp(eyePosition.y - .48f + heightAdjustment, .32f, 1.25f);
            view.FitToSurface(center, Quaternion.LookRotation(forward, Vector3.up), 1.12f, .68f);
            SetVirtualStand(true);
            SetStatus("Adjustable story stand");
        }

        public void AdjustHeight(float amount)
        {
            heightAdjustment = Mathf.Clamp(heightAdjustment + amount, -.24f, .24f);
            RestoreStand();
        }

        /// <summary>
        /// Called only after the visitor chooses to use their own table. The room scan is read on the headset and never
        /// sent anywhere. When it holds no table near the visitor, or cannot be read, they are asked to rest a hand on
        /// their table instead.
        /// </summary>
        public async void FitRoomTable()
        {
            if (loading || view == null || head == null) return;
            RoomPlacementRequested?.Invoke();
            if (!RoomCapable) { PlaceByHand(null); return; }
            var version = ++placementVersion;
            EndHandRequest();
            loading = true; SetStatus("Finding your table…");
            string problem;
            try { problem = await FitFromRoomScan(version); }
            catch (Exception) { problem = "Your room scan could not be read"; }
            if (this == null || version != placementVersion) return;
            loading = false;
            if (problem != null) PlaceByHand(problem);
        }

        /// <summary>Returns null once the story is fitted to a scanned table, or why it could not be.</summary>
        async System.Threading.Tasks.Task<string> FitFromRoomScan(int version)
        {
            if (!await RequestScenePermission()) return "Room access was not granted";
            if (this == null || version != placementVersion) return null;
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
            if (await System.Threading.Tasks.Task.WhenAny(load, System.Threading.Tasks.Task.Delay(8000)) != load) return "Your room scan took too long";
            var result = await load;
            if (this == null || version != placementVersion) return null;
            if (result != MRUK.LoadDeviceResult.Success)
                return result == MRUK.LoadDeviceResult.NoScenePermission ? "Room access was not granted" : "No room scan was found";
            var room = roomKit.GetCurrentRoom();
            var candidates = room == null ? null : room.Anchors.Where(a => a != null && a.HasAnyLabel(MRUKAnchor.SceneLabels.TABLE)).ToArray();
            MRUKAnchor best = null; Vector3 top = default; float across = 0, along = 0, nearest = float.MaxValue;
            var eye = Eye(); var facing = Facing();
            if (candidates != null)
                foreach (var table in candidates)
                {
                    var b = table.VolumeBounds;
                    var p = table.PlaneRect;
                    if (!b.HasValue && !p.HasValue) continue;
                    var center = b.HasValue ? table.transform.TransformPoint(new Vector3(b.Value.center.x, b.Value.center.y, b.Value.max.z)) : table.transform.TransformPoint(new Vector3(p.Value.center.x, p.Value.center.y, 0));
                    // A scanned tabletop's own forward axis points up out of it; anything tilted is not a table to sit at.
                    if (Vector3.Dot(table.transform.forward, Vector3.up) < .99f) continue;
                    var w = b.HasValue ? b.Value.size.x : p.Value.width;
                    var d = b.HasValue ? b.Value.size.y : p.Value.height;
                    if (Mathf.Max(w, d) < .90f || Mathf.Min(w, d) < .55f || center.y < eye.y - .85f || center.y > eye.y - .12f) continue;
                    // How far the visitor is from the nearest part of the tabletop, not from its middle: a long table
                    // is still theirs when they sit at one end of it.
                    var gap = GapToTable(table.transform.rotation, center, w, d, eye);
                    if (gap.magnitude > .9f || (gap.sqrMagnitude > .0004f && Vector3.Dot(gap.normalized, facing) < .25f)) continue;
                    if (gap.sqrMagnitude >= nearest) continue;
                    best = table; top = center; across = w; along = d; nearest = gap.sqrMagnitude;
                }
            if (best == null) return "No table was found near you in your room scan";
            SeatOnTable(best.transform.rotation, top, across, along, eye, facing, out var at, out var rotation, out var width, out var depth);
            view.FitToSurface(at + Vector3.up * .012f, rotation, width, depth);
            fitted = true; SetVirtualStand(false); ShowRoom(true);
            SetStatus("On your table • Recenter restores the stand");
            return null;
        }

        /// <summary>
        /// Asks the visitor to rest a hand on their table and shows their room so they can. Needs no room scan and no
        /// permission: only the hand's own position is read.
        /// </summary>
        public void PlaceByHand(string because)
        {
            if (view == null || head == null) return;
            placementVersion++; loading = false; fitted = false;
            foreach (var rest in rests) rest.Reset();
            handWaited = 0;
            ShowRoom(true);
            SetStatus((string.IsNullOrEmpty(because) ? "" : because + " • ") + "Rest a hand on your table");
            if (awaitingHand) return;
            awaitingHand = true;
            HandAsked?.Invoke();
        }

        void EndHandRequest()
        {
            if (!awaitingHand) return;
            awaitingHand = false;
            HandSettled?.Invoke();
        }

        void WatchForARestingHand()
        {
            handWaited += Time.unscaledDeltaTime;
            if (handWaited > HandWaitSeconds) { Recenter(); SetStatus("No table was set • the story stand is ready"); return; }
            var eye = Eye(); var forward = StoryFacing();
            for (var i = 0; i < hands.Length; i++)
            {
                var hand = hands[i];
                var tracked = hand != null && hand.Tracked;
                var palm = tracked ? hand.PalmCenter : Vector3.zero;
                if (!rests[i].Update(tracked, N(palm), N(eye), N(forward), Time.unscaledDeltaTime)) continue;
                var place = rests[i].Place;
                SetOnSurfaceUnderHand(new Vector3(place.X, place.Y, place.Z));
                return;
            }
        }

        static Num.Vector3 N(Vector3 v) => new Num.Vector3(v.x, v.y, v.z);

        /// <summary>
        /// The table is as high as the resting palm. The story keeps the direction it already faced and stays in front
        /// of the visitor, as far away as the hand shows the table to begin, so a hand resting off to one side, or a
        /// head turned to look at it, never pulls the stage round or out of view.
        /// </summary>
        void SetOnSurfaceUnderHand(Vector3 palm)
        {
            placementVersion++;
            var eye = Eye(); var forward = StoryFacing();
            var ahead = Vector3.Dot(Vector3.ProjectOnPlane(palm - eye, Vector3.up), forward);
            var center = eye + forward * Mathf.Clamp(ahead + BeyondHand, NearestCentre, FarthestCentre);
            center.y = palm.y - PalmAboveSurface;
            view.FitToSurface(center, Quaternion.LookRotation(forward, Vector3.up), 1.12f, .68f);
            fitted = true; SetVirtualStand(false); ShowRoom(true);
            SetStatus("On your table • Recenter restores the stand");
            EndHandRequest();
        }

        /// <summary>The tabletop's two level edges as world directions: across is its first measured axis, along its second.</summary>
        static void TableAxes(Quaternion surfaceRotation, out Vector3 across, out Vector3 along)
        {
            across = Vector3.ProjectOnPlane(surfaceRotation * Vector3.right, Vector3.up).normalized;
            along = Vector3.ProjectOnPlane(surfaceRotation * Vector3.up, Vector3.up).normalized;
            if (across.sqrMagnitude < .5f) across = Vector3.right;
            if (along.sqrMagnitude < .5f) along = Vector3.forward;
        }

        /// <summary>The level step from the eyes to the nearest point of the tabletop; zero when the visitor leans over it.</summary>
        public static Vector3 GapToTable(Quaternion surfaceRotation, Vector3 topCentre, float sizeAcross, float sizeAlong, Vector3 eye)
        {
            TableAxes(surfaceRotation, out var across, out var along);
            var from = Vector3.ProjectOnPlane(eye - topCentre, Vector3.up);
            var a = Vector3.Dot(from, across); var b = Vector3.Dot(from, along);
            return across * (Mathf.Clamp(a, -sizeAcross / 2, sizeAcross / 2) - a) + along * (Mathf.Clamp(b, -sizeAlong / 2, sizeAlong / 2) - b);
        }

        /// <summary>
        /// Which way the story faces on a measured tabletop. A scanned table has no front and its two level axes are
        /// arbitrary, so the story keeps the table's own edges and faces away from the edge the visitor sits at: the
        /// edge they are beyond, or at a corner or leaning over the table, the edge they are looking across. It never
        /// takes a new angle that would hang the story over a corner. <paramref name="sideways"/> is true when the
        /// story then runs along the table's first axis, so the measured sizes change places.
        /// </summary>
        public static Quaternion FacingAcrossTable(Quaternion surfaceRotation, Vector3 topCentre, float sizeAcross, float sizeAlong, Vector3 eye, Vector3 headFacing, out bool sideways)
        {
            TableAxes(surfaceRotation, out var across, out var along);
            var from = Vector3.ProjectOnPlane(eye - topCentre, Vector3.up);
            var a = Vector3.Dot(from, across); var b = Vector3.Dot(from, along);
            var beyondAcross = Mathf.Abs(a) > sizeAcross / 2; var beyondAlong = Mathf.Abs(b) > sizeAlong / 2;
            Vector3 forward;
            if (beyondAlong && !beyondAcross) { sideways = false; forward = b > 0 ? -along : along; }
            else if (beyondAcross && !beyondAlong) { sideways = true; forward = a > 0 ? -across : across; }
            else
            {
                var level = Vector3.ProjectOnPlane(headFacing, Vector3.up);
                // A tie keeps the table's second axis, so a diagonal look never flickers between sides.
                sideways = Mathf.Abs(Vector3.Dot(across, level)) > Mathf.Abs(Vector3.Dot(along, level)) + .01f;
                forward = sideways ? across : along;
                if (Vector3.Dot(forward, level) < 0) forward = -forward;
            }
            return Quaternion.LookRotation(forward, Vector3.up);
        }

        /// <summary>
        /// Where the story sits on a measured tabletop: turned to the visitor's edge, as near that edge as it fits, and
        /// in front of the visitor rather than in the middle of a long table. Returns the surface point under the
        /// story's centre and the room it may use.
        /// </summary>
        public static void SeatOnTable(Quaternion surfaceRotation, Vector3 topCentre, float sizeAcross, float sizeAlong, Vector3 eye, Vector3 headFacing,
            out Vector3 centre, out Quaternion rotation, out float width, out float depth)
        {
            rotation = FacingAcrossTable(surfaceRotation, topCentre, sizeAcross, sizeAlong, eye, headFacing, out var sideways);
            var wide = sideways ? sizeAlong : sizeAcross; var deep = sideways ? sizeAcross : sizeAlong;
            width = Mathf.Min(wide - .06f, 1.25f); depth = Mathf.Min(deep - .06f, .8f);
            var scale = StoryTableView.SurfaceScale(width, depth);
            var forward = rotation * Vector3.forward; var right = rotation * Vector3.right;
            var spareAside = wide / 2 - StoryTableView.DeckWidth * scale / 2 - .03f;
            var spareAhead = deep / 2 - StoryTableView.DeckDepth * scale / 2 - .03f;
            var aside = spareAside > 0 ? Mathf.Clamp(Vector3.Dot(eye - topCentre, right), -spareAside, spareAside) : 0;
            var ahead = spareAhead > 0 ? -spareAhead : 0;
            centre = topCentre + right * aside + forward * ahead;
        }

        /// <summary>The nearest of the tabletop's four sides to an approach direction, for a table whose extent is not known.</summary>
        public static Quaternion RotationAlongSurface(Quaternion surfaceRotation, Vector3 approach)
        {
            // With no extent the visitor is never beyond an edge, so only the direction they come from decides.
            return FacingAcrossTable(surfaceRotation, Vector3.zero, float.MaxValue, float.MaxValue, Vector3.zero, approach, out _);
        }

        static async System.Threading.Tasks.Task<bool> RequestScenePermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
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
#else
            // The simulator's synthetic room needs no permission.
            await System.Threading.Tasks.Task.CompletedTask;
            return true;
#endif
        }

        /// <summary>
        /// Shows or hides the visitor's room behind the story. Showing it clears the eye to transparent over a passthrough
        /// underlay; hiding it restores the scene's own dark background.
        /// </summary>
        void ShowRoom(bool value)
        {
            if (enabledRoom == value || !RoomCapable) return;
            var manager = FindFirstObjectByType<OVRManager>();
            var eye = head != null ? head.GetComponent<Camera>() : null;
            if (eye == null) eye = Camera.main;
            if (value)
            {
                if (manager == null || !OVRManager.IsInsightPassthroughSupported()) return;
                manager.isInsightPassthroughEnabled = true;
                // SDK 207 creates its internal overlay as Underlay; no public overlayType setter exists.
                if (passthrough == null) passthrough = manager.gameObject.AddComponent<OVRPassthroughLayer>();
                passthrough.textureOpacity = RoomOpacity;
                passthrough.enabled = true;
                if (eye != null)
                {
                    if (!eyeRemembered) { eyeClear = eye.clearFlags; eyeBackground = eye.backgroundColor; eyeRemembered = true; }
                    eye.clearFlags = CameraClearFlags.SolidColor; eye.backgroundColor = Color.clear;
                }
            }
            else
            {
                if (passthrough != null) passthrough.enabled = false;
                if (eye != null && eyeRemembered) { eye.clearFlags = eyeClear; eye.backgroundColor = eyeBackground; }
            }
            enabledRoom = value;
        }

        void SetStatus(string message) { Status = message; StatusChanged?.Invoke(message); }
        void SetVirtualStand(bool value)
        {
            var stand = view != null ? view.TableRoot.Find("Virtual table") : null;
            if (stand != null) stand.gameObject.SetActive(value);
        }
        void Update()
        {
            if (awaitingHand) { WatchForARestingHand(); return; }
            if (usedFallbackHead && head != null && head.position.y >= .45f && !fitted && !loading) Recenter();
        }
        void OnDestroy() { placementVersion++; if (passthrough != null) Destroy(passthrough); }
    }
}
