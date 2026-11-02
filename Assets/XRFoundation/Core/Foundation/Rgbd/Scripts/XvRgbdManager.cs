using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static API;

namespace Singray.Foundation
{
    /// <summary>RGBD lifecycle and RGB image-pixel to Unity-world queries.</summary>
    public sealed class XvRgbdManager : MonoBehaviour
    {
        private XvRgbdManager() { }
        [SerializeField] private XvCameraManager cameraManager;
        public XvCameraManager CameraManager
        {
            get
            {
                if (cameraManager == null) cameraManager = GetComponentInChildren<XvCameraManager>(true);
                if (cameraManager == null) cameraManager = FindObjectOfType<XvCameraManager>();
                if (cameraManager == null) cameraManager = new GameObject("XvCameraManager").AddComponent<XvCameraManager>();
                return cameraManager;
            }
        }

        private static readonly HashSet<XvRgbdManager> owners = new HashSet<XvRgbdManager>();
        private static readonly object sensorOwner = new object();
        private double hostTimestamp;
        private int frameWidth, frameHeight;
        private Coroutine startRoutine;
        private bool isRunning;

        public void StartRgbPose()
        {
#if !UNITY_ANDROID || UNITY_EDITOR
            MyDebugTool.LogDiagnosticWarning("RGBD", "editor", "RGBD requires the Android SDK and a connected device", 0f);
            return;
#else
            if (!isRunning && startRoutine == null) startRoutine = StartCoroutine(StartRgbPoseRoutine());
#endif
        }
        private IEnumerator StartRgbPoseRoutine()
        {
            yield return null;
            float deadline = Time.realtimeSinceStartup + 10f;
            while (!XvRgbdNative.Ready())
            {
                if (Time.realtimeSinceStartup >= deadline)
                { startRoutine = null; Debug.LogWarning("[RGBD-LIFE] SDK readiness timeout"); yield break; }
                yield return null;
            }
            try { BeginCapture(); }
            catch (Exception e) { StopRgbPose(); Debug.LogError("[RGBD-LIFE] start failed: " + e.Message); }
            finally { startRoutine = null; }
        }
        private void BeginCapture()
        {
            if (isRunning) return;
            hostTimestamp = 0; frameWidth = frameHeight = 0;
            if (owners.Count == 0)
            {
                bool native = false, rgb = false, depth = false;
                try
                {
                    XvRgbdNative.Start(); native = true;
                    CameraManager.AcquireCapture(XvCameraStreamType.ARCameraStream, sensorOwner); rgb = true;
                    CameraManager.AcquireCapture(XvCameraStreamType.TofDepthCameraStream, sensorOwner); depth = true;
                }
                catch
                {
                    if (depth) XvCameraManager.ReleaseCapture(XvCameraStreamType.TofDepthCameraStream, sensorOwner);
                    if (rgb) XvCameraManager.ReleaseCapture(XvCameraStreamType.ARCameraStream, sensorOwner);
                    if (native) XvRgbdNative.Stop();
                    throw;
                }
            }
            owners.Add(this);
            XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(onFrameArrived);
            XvCameraManager.onARCameraStreamFrameArrived.AddListener(onFrameArrived);
            isRunning = true;
            Debug.Log($"[RGBD-LIFE] started consumers={owners.Count}; waiting for a real RGB frame");
        }
        public void StopRgbPose()
        {
            if (startRoutine != null) StopCoroutine(startRoutine);
            startRoutine = null;
            XvCameraManager.onARCameraStreamFrameArrived.RemoveListener(onFrameArrived);
            hostTimestamp = 0; frameWidth = frameHeight = 0;
            if (!isRunning) return;
            isRunning = false; owners.Remove(this);
            Debug.Log($"[RGBD-LIFE] stopped consumers={owners.Count}");
            if (owners.Count != 0) return;
            try { XvRgbdNative.Stop(); }
            finally
            {
                try { XvCameraManager.ReleaseCapture(XvCameraStreamType.ARCameraStream, sensorOwner); }
                finally { XvCameraManager.ReleaseCapture(XvCameraStreamType.TofDepthCameraStream, sensorOwner); }
            }
        }
        private bool ValidPixel(Vector2 p) => isRunning && hostTimestamp > 0 && frameWidth > 0 && frameHeight > 0 &&
            Finite(p.x) && Finite(p.y) && p.x >= 0 && p.y >= 0 && p.x < frameWidth && p.y < frameHeight;
        private static bool Finite(float n) => !float.IsNaN(n) && !float.IsInfinity(n);
        private static bool Finite(Vector3 p) => Finite(p.x) && Finite(p.y) && Finite(p.z);

        public bool GetRgbPixel3DPose(Vector2 rgbPoint, ref Vector3 spacePoint)
        {
            if (!ValidPixel(rgbPoint)) return false;
            Vector2F pixel = new Vector2F { x = rgbPoint.x, y = rgbPoint.y };
            Vector3F point = default;
            if (!XvRgbdNative.Query(ref point, ref pixel, hostTimestamp, 30)) return false;
            Vector3 value = new Vector3(point.x, -point.y, point.z);
            if (!Finite(value)) return false;
            spacePoint = value;
            return true;
        }
        public bool GetRgbPixelPoseList(Vector2[] rgbPoint, ref pointer_3dpose[] spacePose)
        {
            if (rgbPoint == null || rgbPoint.Length == 0 || spacePose == null || spacePose.Length < rgbPoint.Length) return false;
            for (int i = 0; i < rgbPoint.Length; i++) if (!ValidPixel(rgbPoint[i])) return false;
            int size = rgbPoint.Length;
            if (!XvRgbdNative.QueryBatch(spacePose, rgbPoint, size, hostTimestamp, 30)) return false;
            for (int i = 0; i < size; i++)
            {
                if (spacePose[i].isValid && Finite(spacePose[i].pointerPose)) spacePose[i].pointerPose.y *= -1;
                else spacePose[i].isValid = false;
            }
            return true;
        }
        private void OnDisable() => StopRgbPose();
        private void onFrameArrived(cameraData frame)
        {
            if (!isRunning || frame == null || frame.tex == null || frame.parameter.timeStamp <= 0 ||
                double.IsNaN(frame.parameter.timeStamp) || double.IsInfinity(frame.parameter.timeStamp) || frame.parameter.timeStamp <= hostTimestamp) return;
            hostTimestamp = frame.parameter.timeStamp;
            frameWidth = frame.tex.width; frameHeight = frame.tex.height;
            MyDebugTool.LogDiagnostic("RGBD", "timestamp", $"actual={frameWidth}x{frameHeight} rgbTimestamp={hostTimestamp:F6}", 2f);
        }
    }
}
