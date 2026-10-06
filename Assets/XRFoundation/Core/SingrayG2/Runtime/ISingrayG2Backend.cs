using System;
using UnityEngine;

namespace Singray.G2
{
    internal interface ISingrayG2Backend : IDisposable
    {
        bool IsReady { get; }
        bool IsSimulation { get; }

        event Action<SingrayG2SensorStream, SingrayG2CameraFrame> CameraFrameArrived;
        event Action<SingrayG2DeviceEvent> DeviceEventArrived;

        void Tick(double timeSeconds);
        void ConfigureRgb(SingrayG2RgbSettings settings);
        void ConfigureTof(SingrayG2TofSettings settings);
        void StartSensor(SingrayG2SensorStream stream);
        void StopSensor(SingrayG2SensorStream stream);
        bool IsSensorRunning(SingrayG2SensorStream stream);
        void StartTofPointCloud();
        bool TryGetTofPointCloud(out Vector3[] points);
        void StopTofPointCloud();
        void StartRgbd();
        void StopRgbd();
        bool TryGetRgbPixelWorldPosition(Vector2 pixel, out Vector3 worldPosition);
        int StartImu();
        int StopImu();
        bool TryGetImuSample(out SingrayG2ImuSample sample);
        void StartDeviceEvents();
        void StopDeviceEvents();
        void SetDisplayBrightness(int level);
    }
}
