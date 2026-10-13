using UnityEngine;

namespace Singray.InputSystem.Internal
{
    [AddComponentMenu("")]
    [DefaultExecutionOrder(-1000)]
    internal sealed class DeviceEventPump : MonoBehaviour
    {
        private void Update() => DeviceEventHub.Tick(Time.realtimeSinceStartupAsDouble);
        private void OnApplicationPause(bool paused) => DeviceEventHub.Pause(paused);
        private void OnApplicationQuit() => DeviceEventHub.Shutdown();
        private void OnDestroy() => DeviceEventHub.Shutdown();
    }
}
