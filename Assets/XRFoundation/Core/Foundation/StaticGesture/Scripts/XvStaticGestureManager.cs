using System;
using System.Collections.Generic;
using UnityEngine;
namespace Singray.Foundation
{
    public class XvStaticGestureManager : MonoBehaviour
    {
        private XvStaticGestureManager() { }
        private GestureKeyState left;
        private GestureKeyState right;
        private int sampledFrame = -1;
        private StaticGestureStatus leftGesture = StaticGestureStatus.UNKONOW;
        private StaticGestureStatus rightGesture = StaticGestureStatus.UNKONOW;
        private bool paused;
        private void Awake() => EnsureInitialized();
        private void OnEnable() { paused = false; ResetStates(); }
        private void OnDisable() => ResetStates();
        private void OnApplicationPause(bool pause) { paused = pause; ResetStates(); }
        private void EnsureInitialized()
        {
            if (left == null) left = new GestureKeyState(0);
            if (right == null) right = new GestureKeyState(1);
        }
        private void ResetStates()
        {
            EnsureInitialized(); left.Reset(); right.Reset(); sampledFrame = -1;
            leftGesture = rightGesture = StaticGestureStatus.UNKONOW;
        }
        private void SampleFrame()
        {
            EnsureInitialized();
            if (!isActiveAndEnabled || paused) return;
            int frame = StaticGestureInput.frameNumber();
            if (sampledFrame == frame) return;
            sampledFrame = frame;
            leftGesture = StaticGestureInput.readGesture(0);
            rightGesture = StaticGestureInput.readGesture(1);
            float now = StaticGestureInput.clock();
            left.Sample(leftGesture, now); right.Sample(rightGesture, now);
        }
        public StaticGestureStatus GetCurrentStaticGesture(HandType handType)
        {
            SampleFrame();
            return handType == HandType.Left ? leftGesture : handType == HandType.Right ? rightGesture : StaticGestureStatus.UNKONOW;
        }
        public bool GetKeyDown(StaticGestureStatus gestureStatus, HandType handType)
        {
            SampleFrame();return handType == HandType.Left ? left.GetKeyDown(gestureStatus) : handType == HandType.Right && right.GetKeyDown(gestureStatus);
        }
        public bool GetKeyUp(StaticGestureStatus buttonKey, HandType handType)
        {
            SampleFrame();return handType == HandType.Left ? left.GetKeyUp(buttonKey) : handType == HandType.Right && right.GetKeyUp(buttonKey);
        }
        public bool GetKey(StaticGestureStatus buttonKey, HandType handType)
        {
            SampleFrame();return handType == HandType.Left ? left.GetKey(buttonKey) : handType == HandType.Right && right.GetKey(buttonKey);
        }
        private void LateUpdate() => SampleFrame();
    }
    // Keep existing names and numeric values, including the legacy UNKONOW spelling.
    public enum StaticGestureStatus { UNKONOW = -1, GRAB = 0, Point, VICTORY, Three, Four, OpenHand, Call, Rock, Thumb, Pinch }
    public enum HandType { None, Left, Right }

    internal static class StaticGestureInput
    {
        internal static Func<int, StaticGestureStatus> readGesture = ReadGesture;
        internal static Func<KeyCode, bool> readKey = Input.GetKey;
        internal static Func<int> frameNumber = () => Time.frameCount;
        internal static Func<float> clock = () => Time.unscaledTime;
        internal static StaticGestureStatus ReadGesture(int handIndex)
        {
            var skeleton = XvXRInput.xvSkeleton;
            var hands = XvXRInput.handDatas;
            if (handIndex < 0 || handIndex > 1 || skeleton.status == null || handIndex >= skeleton.status.Length ||
                skeleton.size <= 0 || skeleton.size > XvXRInput.PRE_COUNT * 2 || hands == null || handIndex >= hands.Length || !hands[handIndex].isTracked)
                return StaticGestureStatus.UNKONOW;
            int status = skeleton.status[handIndex];
            return status >= 0 && status <= 9 ? (StaticGestureStatus)status : StaticGestureStatus.UNKONOW;
        }
    }
    public class GestureState
    {
        private readonly StaticGestureStatus buttonKey;
        private readonly int handIndex;
        private bool keyDown, keyUp, key, doubleClick;
        private float lastRelease = float.NegativeInfinity;
        private bool awaitingSecond;
        public GestureState(StaticGestureStatus buttonKey, int handIndex) { this.buttonKey = buttonKey; this.handIndex = handIndex; }
        public void Update()
        {
            bool valid = (int)buttonKey >= 0 && (int)buttonKey <= 9;
            bool pressed = valid && (StaticGestureInput.readGesture(handIndex) == buttonKey || StaticGestureInput.readKey((KeyCode)((int)KeyCode.Alpha0 + (int)buttonKey)));
            Advance(pressed, StaticGestureInput.clock());
        }
        internal void Advance(bool pressed, float now)
        {
            keyDown = pressed && !key; keyUp = !pressed && key; key = pressed; doubleClick = false;
            if (keyUp) lastRelease = now;
            if (keyDown)
            {
                // Preserve the existing short release-gap double-click behavior.
                if (awaitingSecond)
                {
                    doubleClick = now >= lastRelease && now - lastRelease < 0.2f;
                    awaitingSecond = false;
                }
                else awaitingSecond = true;
            }
            else if (!pressed && awaitingSecond && now - lastRelease > 0.3f) awaitingSecond = false;
        }
        internal void Reset() { keyDown = keyUp = key = doubleClick = false; lastRelease = float.NegativeInfinity; awaitingSecond = false; }
        public bool GetKeyDown() => keyDown;
        public bool GetKeyUp() => keyUp;
        public bool GetKey() => key;
        public bool GetDoubleClick() => doubleClick;
    }
    public class GestureKeyState
    {
        public Dictionary<StaticGestureStatus, GestureState> buttonKeyDic = new Dictionary<StaticGestureStatus, GestureState>();
        public GestureKeyState(int hanIndex)
        {
            for (int i = 0; i <= 9; i++)
            {
                var gesture = (StaticGestureStatus)i;
                buttonKeyDic.Add(gesture, new GestureState(gesture, hanIndex));
            }
        }
        public void LateUpdate()
        {
            if (buttonKeyDic == null) return;
            foreach (var state in buttonKeyDic.Values) state?.Update();
        }
        internal void Sample(StaticGestureStatus current, float now)
        {
            if (buttonKeyDic == null) return;
            foreach (var entry in buttonKeyDic)
            {
                int index = (int)entry.Key;
                bool pressed = index >= 0 && index <= 9 && (current == entry.Key || StaticGestureInput.readKey((KeyCode)((int)KeyCode.Alpha0 + index)));
                entry.Value?.Advance(pressed, now);
            }
        }
        internal void Reset()
        {
            if (buttonKeyDic != null) foreach (var state in buttonKeyDic.Values) state?.Reset();
        }
        public bool GetKeyDown(StaticGestureStatus buttonKey) => buttonKeyDic != null && buttonKeyDic.TryGetValue(buttonKey, out var state) && state != null && state.GetKeyDown();
        public bool GetKeyUp(StaticGestureStatus buttonKey) => buttonKeyDic != null && buttonKeyDic.TryGetValue(buttonKey, out var state) && state != null && state.GetKeyUp();
        public bool GetKey(StaticGestureStatus buttonKey) => buttonKeyDic != null && buttonKeyDic.TryGetValue(buttonKey, out var state) && state != null && state.GetKey();
        public bool GetDoubleClick(StaticGestureStatus buttonKey) => buttonKeyDic != null && buttonKeyDic.TryGetValue(buttonKey, out var state) && state != null && state.GetDoubleClick();
    }
}
