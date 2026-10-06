using System;
using UnityEngine;

namespace Singray.G2
{
    public enum SingrayG2RuntimeMode
    {
        /// <summary>Simulation outside Android; native backend on an Android player.</summary>
        Auto,

        NativeDevice,
        Simulation
    }

    /// <summary>
    /// Product-level sensor names. Vendor stream names are intentionally not
    /// exposed so application code does not depend on the underlying SDK.
    /// </summary>
    public enum SingrayG2SensorStream
    {
        Rgb,
        TofDepth,
        TofInfrared,
        LeftTrackingCamera,
        RightTrackingCamera,
        ComputeCamera
    }

    public enum SingrayG2RgbResolution
    {
        R320x240,
        R640x480,
        R1280x720,
        R1920x1080,
        R2560x1920,
        R3840x2160
    }

    public enum SingrayG2TofResolution
    {
        Hqvga,
        Qvga,
        Vga
    }

    public enum SingrayG2TofFrameRate
    {
        Fps5 = 5,
        Fps10 = 10,
        Fps15 = 15,
        Fps20 = 20,
        Fps25 = 25,
        Fps30 = 30
    }

    [Serializable]
    public sealed class SingrayG2RgbSettings
    {
        public SingrayG2RgbResolution resolution = SingrayG2RgbResolution.R1280x720;

        [Range(1, 60)]
        public int framesPerSecond = 30;
    }

    [Serializable]
    public sealed class SingrayG2TofSettings
    {
        public SingrayG2TofResolution resolution = SingrayG2TofResolution.Qvga;

        public SingrayG2TofFrameRate frameRate = SingrayG2TofFrameRate.Fps30;

        public bool enableInfraredGamma;
    }

    /// <summary>
    /// Product-facing camera frame. It contains the useful calibrated values
    /// without leaking the vendor cameraData type into application code.
    /// </summary>
    public readonly struct SingrayG2CameraFrame
    {
        public SingrayG2CameraFrame(
            int width,
            int height,
            Texture texture,
            double timestamp,
            Vector3 position,
            Quaternion rotation,
            Vector3 extrinsicPosition,
            Quaternion extrinsicRotation,
            Vector4 intrinsics,
            Vector4 distortion,
            float distortionK3)
        {
            Width = width;
            Height = height;
            Texture = texture;
            Timestamp = timestamp;
            Position = position;
            Rotation = rotation;
            ExtrinsicPosition = extrinsicPosition;
            ExtrinsicRotation = extrinsicRotation;
            Intrinsics = intrinsics;
            Distortion = distortion;
            DistortionK3 = distortionK3;
        }

        public int Width { get; }
        public int Height { get; }
        public Texture Texture { get; }
        public double Timestamp { get; }
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Vector3 ExtrinsicPosition { get; }
        public Quaternion ExtrinsicRotation { get; }

        /// <summary>fx, fy, cx, cy.</summary>
        public Vector4 Intrinsics { get; }

        /// <summary>k1, k2, p1, p2.</summary>
        public Vector4 Distortion { get; }

        public float DistortionK3 { get; }
    }

    public readonly struct SingrayG2ImuSample
    {
        public SingrayG2ImuSample(double timestamp, Vector3 acceleration, Vector3 angularVelocity, Vector3 magneticField)
        {
            Timestamp = timestamp;
            Acceleration = acceleration;
            AngularVelocity = angularVelocity;
            MagneticField = magneticField;
        }

        public double Timestamp { get; }
        public Vector3 Acceleration { get; }
        public Vector3 AngularVelocity { get; }
        public Vector3 MagneticField { get; }
    }

    public readonly struct SingrayG2DeviceEvent
    {
        public SingrayG2DeviceEvent(double hostTimestamp, long deviceTimestampMicroseconds, int type, int state)
        {
            HostTimestamp = hostTimestamp;
            DeviceTimestampMicroseconds = deviceTimestampMicroseconds;
            Type = type;
            State = state;
        }

        public double HostTimestamp { get; }
        public long DeviceTimestampMicroseconds { get; }
        public int Type { get; }
        public int State { get; }
        public bool IsAmbientLight => Type == 6;
    }
}
