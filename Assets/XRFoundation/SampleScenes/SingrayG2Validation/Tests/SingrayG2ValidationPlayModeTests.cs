#if UNITY_INCLUDE_TESTS && SINGRAY_G2_ENABLE_TESTS
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace Singray.G2.Validation.Tests
{
    public sealed class SingrayG2ValidationPlayModeTests
    {
        [UnityTest]
        public IEnumerator ValidationUiAndSensorEntrypointsAreSafeWithoutDevice()
        {
            GameObject validationObject = new GameObject("Singray G2 Validation Test");
            validationObject.AddComponent<SingrayG2ValidationController>();
            yield return null;

            Assert.IsNotNull(validationObject.GetComponentInChildren<Canvas>(true));
            Assert.IsNotNull(validationObject.GetComponentInChildren<RawImage>(true));
            Assert.GreaterOrEqual(validationObject.GetComponentsInChildren<Button>(true).Length, 12);

            SingrayG2Manager manager = SingrayG2Manager.Instance;
            Assert.IsTrue(manager.IsSimulation, "Auto mode must select simulation in the Editor.");
            Assert.IsTrue(manager.IsSdkReady, "The simulation backend must be ready for validation controls.");

            Assert.DoesNotThrow(() => manager.StartSensor(SingrayG2SensorStream.Rgb));
            Assert.DoesNotThrow(() => manager.StartSensor(SingrayG2SensorStream.TofDepth));
            Assert.DoesNotThrow(() => manager.StartSensor(SingrayG2SensorStream.TofInfrared));
            Assert.DoesNotThrow(manager.StartTofPointCloud);
            Assert.DoesNotThrow(() => manager.StartRgbd());
            Assert.DoesNotThrow(() => manager.StartImu());
            Assert.DoesNotThrow(manager.StartDeviceEvents);
            Assert.DoesNotThrow(() => manager.SetDisplayBrightness(6));

            yield return null;

            RawImage preview = validationObject.GetComponentInChildren<RawImage>(true);
            Assert.IsNotNull(preview.texture, "Simulation must publish a camera preview texture.");
            Assert.IsTrue(manager.TryGetTofPointCloud(out Vector3[] cloud));
            Assert.AreEqual(160 * 120, cloud.Length);
            Assert.IsTrue(manager.TryGetImuSample(out SingrayG2ImuSample imu));
            Assert.Greater(imu.Acceleration.magnitude, 9f);

            Assert.DoesNotThrow(() => manager.StopSensor(SingrayG2SensorStream.Rgb));
            Assert.DoesNotThrow(() => manager.StopSensor(SingrayG2SensorStream.TofDepth));
            Assert.DoesNotThrow(() => manager.StopSensor(SingrayG2SensorStream.TofInfrared));
            Assert.DoesNotThrow(manager.StopTofPointCloud);
            Assert.DoesNotThrow(manager.StopRgbd);
            Assert.DoesNotThrow(() => manager.StopImu());
            Assert.DoesNotThrow(manager.StopDeviceEvents);

            Object.Destroy(validationObject);
            Object.Destroy(manager.gameObject);
            yield return null;
        }

        [UnityTest]
        public IEnumerator ExplicitNativeModeRemainsSafeInEditorWithoutDevice()
        {
            SingrayG2Manager manager = SingrayG2Manager.Instance;
            Assert.DoesNotThrow(() => manager.SetRuntimeMode(SingrayG2RuntimeMode.NativeDevice));
            Assert.IsFalse(manager.IsSimulation);
            Assert.IsFalse(manager.IsSdkReady);

            Assert.DoesNotThrow(() => manager.StartSensor(SingrayG2SensorStream.Rgb));
            Assert.DoesNotThrow(() => manager.StartSensor(SingrayG2SensorStream.TofDepth));
            Assert.DoesNotThrow(() => manager.StartSensor(SingrayG2SensorStream.TofInfrared));
            Assert.DoesNotThrow(manager.StartTofPointCloud);
            Assert.DoesNotThrow(manager.StartRgbd);
            Assert.DoesNotThrow(() => manager.StartImu());
            Assert.DoesNotThrow(manager.StartDeviceEvents);
            Assert.DoesNotThrow(() => manager.SetDisplayBrightness(6));

            Assert.DoesNotThrow(() => manager.StopSensor(SingrayG2SensorStream.Rgb));
            Assert.DoesNotThrow(() => manager.StopSensor(SingrayG2SensorStream.TofDepth));
            Assert.DoesNotThrow(() => manager.StopSensor(SingrayG2SensorStream.TofInfrared));
            Assert.DoesNotThrow(manager.StopTofPointCloud);
            Assert.DoesNotThrow(manager.StopRgbd);
            Assert.DoesNotThrow(() => manager.StopImu());
            Assert.DoesNotThrow(manager.StopDeviceEvents);

            Assert.DoesNotThrow(() => manager.SetRuntimeMode(SingrayG2RuntimeMode.Simulation));
            Assert.IsTrue(manager.IsSimulation);

            Object.Destroy(manager.gameObject);
            yield return null;
        }
    }
}
#endif
