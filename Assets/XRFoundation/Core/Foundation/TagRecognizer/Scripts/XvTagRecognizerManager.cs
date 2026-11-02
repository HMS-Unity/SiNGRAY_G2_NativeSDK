using AOT;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;
using Debug = UnityEngine.Debug;

namespace Singray.Foundation
{
    /// <summary>AprilTag/QR recognition with shared native sessions and main-thread results.</summary>
    public sealed class XvTagRecognizerManager : MonoBehaviour
    {
        private XvTagRecognizerManager() { }
        [SerializeField] private XvCameraManager xvCameraManager;
        public XvCameraManager XvCameraManager
        {
            get
            {
                if (xvCameraManager == null) xvCameraManager = FindFirstObjectByType<XvCameraManager>();
                if (xvCameraManager == null) xvCameraManager = new GameObject("XvCameraManager").AddComponent<XvCameraManager>();
                return xvCameraManager;
            }
        }
        private string tagGroupName = "36h11";
        public string TagGroupName
        {
            get => tagGroupName;
            set
            {
                if (string.IsNullOrEmpty(value) || value == tagGroupName) return;
                if (session != null && session.Owners.Count > 1)
                { Debug.LogWarning("[TAG-LIFE] Family change refused while another manager shares this detector."); return; }
                if (currentRecognizerMode != RecognizerMode.None) StartConfigured(currentRecognizerMode, value);
                else tagGroupName = value;
            }
        }
        [SerializeField, Tooltip("Physical tag size in meters")] private double size = 0.16f;
        public double Size
        {
            get => size;
            set
            {
                if (!XvAprilTag.ValidSize(value)) { Debug.LogWarning("[TAG-LIFE] Invalid tag size ignored."); return; }
                if (size == value) return;
                if (session != null && session.Owners.Count > 1)
                { Debug.LogWarning("[TAG-LIFE] Size change refused while another manager shares this detector."); return; }
                size = value;
                if (currentRecognizerMode != RecognizerMode.None) StartConfigured(currentRecognizerMode, tagGroupName);
            }
        }
        [SerializeField, Tooltip("Confidence threshold")] private float confidence;
        public float Confidence { get => confidence; set => confidence = value; }
        private bool IsDetection;
        private UnityEvent<TagDetection[]> onDetectedAprilTagEvent;
        public UnityEvent<TagDetection[]> OnDetectedAprilTagEvent => onDetectedAprilTagEvent ?? (onDetectedAprilTagEvent = new UnityEvent<TagDetection[]>());
        [SerializeField] private RecognizerMode currentRecognizerMode;
        public RecognizerMode RecognizerMode => currentRecognizerMode;

        private sealed class Session
        {
            internal RecognizerMode Mode;
            internal string Family;
            internal double Size;
            internal readonly HashSet<XvTagRecognizerManager> Owners = new HashSet<XvTagRecognizerManager>();
            internal readonly object FrameLock = new object();
            internal volatile bool Cancelled, Complete;
            internal bool NativeStarted;
            internal Thread Worker;
            internal TagDetection[] Latest;
            internal double ReceivedAt;
            internal long Frames;
            internal volatile string Failure;
        }
        private static volatile Session activeSession;
        private Session session;
        private Coroutine startRoutine;
        private RecognizerMode resumeMode;
        private readonly object rgbOwner = new object();
        private bool rgbAcquired;
        private float nextLog;
        private static Func<bool> readyNative = PlatformReady;
        private static Func<string, double, TagDetection[]> fishNative = XvAprilTag.StartFishEyeDetector;
        private static Action stopFishNative = XvAprilTag.StopFishEyeDetector;
        private static Action<string, double, XvAprilTag.TagArrayCallback> startRgbNative = XvAprilTag.StartRgbDetector;
        private static Action stopRgbNative = XvAprilTag.StopRgbDetector;
        private static readonly XvAprilTag.TagArrayCallback rgbCallback = OnDetecterTags;
        private static Action<XvTagRecognizerManager> acquireRgb = m => m.XvCameraManager.AcquireCapture(XvCameraStreamType.ARCameraStream, m.rgbOwner);
        private static Action<XvTagRecognizerManager> releaseRgb = m => Singray.Foundation.XvCameraManager.ReleaseCapture(XvCameraStreamType.ARCameraStream, m.rgbOwner);
        internal bool IsStarting => startRoutine != null;
        private static double Clock() => (double)Stopwatch.GetTimestamp() / Stopwatch.Frequency;
        private static bool PlatformReady()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return API.xslam_ready();
#else
            return false;
#endif
        }
        private void OnEnable()
        {
            var mode = currentRecognizerMode != RecognizerMode.None ? currentRecognizerMode : resumeMode;
            if (mode != RecognizerMode.None) StartTagDetector(mode);
        }
        private void OnDisable()
        {
            var mode = currentRecognizerMode;
            StopTagDetector(); resumeMode = mode;
        }
        private void OnDestroy() => StopTagDetector();
        private void Update()
        {
            RetireCompletedSession();
            if (!IsDetection || session == null) return;
            if (session.Failure != null)
            {
                Debug.LogWarning("[TAG-LIFE] detector failed: " + session.Failure);
                StopTagDetector(); return;
            }
            TagDetection[] frame; double age; long frames;
            lock (session.FrameLock)
            { age = Clock() - session.ReceivedAt; frame = age <= 2.0 ? session.Latest : null; frames = session.Frames; }
            var running = session;
            // One dispatch per render frame, retaining existing continuous pose/Found event behavior.
            OnDetectedAprilTagEvent.Invoke(frame);
            if (!ReferenceEquals(session, running)) return;
            if (Time.unscaledTime >= nextLog)
            {
                Debug.Log($"[TAG-LIFE] mode={currentRecognizerMode} batches={frames} tags={frame?.Length ?? 0} fresh={age <= 2.0} owners={session.Owners.Count}");
                nextLog = Time.unscaledTime + 5f;
            }
        }
        private static void RetireCompletedSession()
        {
            if (activeSession != null && activeSession.Cancelled && activeSession.Complete && activeSession.Owners.Count == 0)
                activeSession = null;
        }
        public void SetDetectStatus(bool isDetect)
        {
            if (isDetect)
            {
                var mode = currentRecognizerMode != RecognizerMode.None ? currentRecognizerMode : resumeMode;
                if (mode != RecognizerMode.None) StartTagDetector(mode);
            }
            else
            {
                var mode = currentRecognizerMode; StopTagDetector(); resumeMode = mode;
            }
        }
        public void StartTagDetector(RecognizerMode recognizerMode) =>
            StartConfigured(recognizerMode, recognizerMode == RecognizerMode.RGB_QRCode ? "qr-code" : "36h11");
        private void StartConfigured(RecognizerMode recognizerMode, string family)
        {
            if (!Enum.IsDefined(typeof(RecognizerMode), recognizerMode)) { Debug.LogWarning("[TAG-LIFE] Unknown recognition mode ignored."); return; }
            if (recognizerMode == RecognizerMode.None) { StopTagDetector(); return; }
            if (IsDetection && session != null && session.Mode == recognizerMode && session.Size == size && session.Family == family) return;
            if (session != null && session.Owners.Count > 1)
            { Debug.LogWarning("[TAG-LIFE] Mode change refused while another manager shares this detector."); return; }
            StopTagDetector();
            currentRecognizerMode = recognizerMode; resumeMode = recognizerMode; tagGroupName = family;
            if (!isActiveAndEnabled) return;
            Debug.Log($"[TAG-LIFE] start requested mode={recognizerMode} size={size}");
            startRoutine = StartCoroutine(StartWhenReady(Time.realtimeSinceStartup + 10f));
        }
        private IEnumerator StartWhenReady(float deadline)
        {
            yield return null;
            try
            {
                while (true)
                {
                    RetireCompletedSession();
                    if (readyNative() && (activeSession == null || !activeSession.Cancelled)) break;
                    if (Time.realtimeSinceStartup >= deadline)
                    {
                        Debug.LogWarning("[TAG-LIFE] Start timeout: SDK not ready or previous native worker still stopping.");
                        currentRecognizerMode = RecognizerMode.None; yield break;
                    }
                    yield return null;
                }
                if (!XvAprilTag.ValidSize(size)) { Debug.LogWarning("[TAG-LIFE] Start rejected: invalid physical tag size."); currentRecognizerMode = RecognizerMode.None; yield break; }
                if (activeSession != null && (activeSession.Mode != currentRecognizerMode || activeSession.Family != tagGroupName || activeSession.Size != size))
                { Debug.LogWarning("[TAG-LIFE] Another manager owns a different detector configuration."); currentRecognizerMode = RecognizerMode.None; yield break; }
                bool first = activeSession == null;
                if (first) activeSession = new Session { Mode = currentRecognizerMode, Family = tagGroupName, Size = size };
                session = activeSession; session.Owners.Add(this);
                if (currentRecognizerMode != RecognizerMode.FishEye_Apriltag)
                { acquireRgb(this); rgbAcquired = true; }
                if (first)
                {
                    var started = session;
                    started.NativeStarted = true;
                    if (currentRecognizerMode == RecognizerMode.FishEye_Apriltag)
                    {
                        started.Worker = new Thread(() => RunFish(started)) { IsBackground = true, Name = "SDK TagRecognition" };
                        started.Worker.Start();
                    }
                    else startRgbNative(started.Family, started.Size, rgbCallback);
                }
                IsDetection = true;
                Debug.Log($"[TAG-LIFE] started mode={currentRecognizerMode} owners={session.Owners.Count}");
            }
            finally
            {
                startRoutine = null;
                if (!IsDetection && session != null) StopTagDetector();
            }
        }
        public void StopTagDetector()
        {
            if (startRoutine != null) StopCoroutine(startRoutine);
            startRoutine = null;
            var stopped = session; session = null;
            bool wasActive = IsDetection || currentRecognizerMode != RecognizerMode.None;
            IsDetection = false; currentRecognizerMode = RecognizerMode.None; resumeMode = RecognizerMode.None;
            try
            {
                if (stopped != null)
                {
                    stopped.Owners.Remove(this);
                    if (stopped.Owners.Count == 0)
                    {
                        stopped.Cancelled = true;
                        lock (stopped.FrameLock) stopped.Latest = null;
                        if (stopped.Mode != RecognizerMode.FishEye_Apriltag)
                        {
                            try { if (stopped.NativeStarted) stopRgbNative(); stopped.Complete = true; RetireCompletedSession(); }
                            catch (Exception e) { stopped.Failure = e.Message; Debug.LogWarning("[TAG-LIFE] Native RGB stop failed; detector gate retained: " + e.Message); }
                        }
                    }
                }
            }
            finally
            {
                if (rgbAcquired) { rgbAcquired = false; releaseRgb(this); }
                if (wasActive)
                {
                    OnDetectedAprilTagEvent.Invoke(null);
                    Debug.Log($"[TAG-LIFE] stopped owners={stopped?.Owners.Count ?? 0} nativePending={stopped != null && stopped.Cancelled && !stopped.Complete}");
                }
            }
        }
        private static void RunFish(Session s)
        {
            try
            {
                while (!s.Cancelled)
                {
                    var frame = fishNative(s.Family, s.Size);
                    Publish(s, frame);
                    // Poll at most 50 times/second; never busy-spin while no tags are visible.
                    for (int i = 0; i < 4 && !s.Cancelled; i++) Thread.Sleep(5);
                }
            }
            catch (Exception e) { s.Failure = e.Message; }
            finally
            {
                try { if (s.NativeStarted) stopFishNative(); s.Complete = true; Debug.Log("[TAG-LIFE] fisheye worker exited; native stop returned."); }
                catch (Exception e) { s.Failure = e.Message; Debug.LogWarning("[TAG-LIFE] Native fisheye stop failed; detector gate retained: " + e.Message); }
            }
        }
        private static void Publish(Session s, TagDetection[] frame)
        {
            lock (s.FrameLock)
            {
                if (s.Cancelled) return;
                s.Latest = frame; s.ReceivedAt = Clock(); s.Frames++;
            }
        }
        [MonoPInvokeCallback(typeof(XvAprilTag.TagArrayCallback))]
        private static void OnDetecterTags(IntPtr pointer, int count)
        {
            var s = activeSession;
            if (s == null || s.Cancelled || s.Mode == RecognizerMode.FishEye_Apriltag) return;
            try
            {
                if (count < 0 || count > 64 || (count > 0 && pointer == IntPtr.Zero)) return;
                var frame = count == 0 ? null : XvAprilTag.ConvertTags(Marshal.PtrToStructure<API.TagData>(pointer), count);
                Publish(s, frame);
            }
            catch (Exception e) { Debug.LogWarning("[TAG-LIFE] invalid RGB callback: " + e.Message); }
        }
    }
    public enum CameraType { Rgb = 0, FishEye = 1 }
    public enum RecognizerMode { None, RGB_QRCode, RGB_Apriltag, FishEye_Apriltag }
}
