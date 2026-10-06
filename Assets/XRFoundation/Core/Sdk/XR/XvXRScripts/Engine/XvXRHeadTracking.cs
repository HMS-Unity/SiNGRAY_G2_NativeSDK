using UnityEngine;
using System.Collections;

namespace Singray.Engine
{
    [DefaultExecutionOrder(-1000)]
    public class XvXRHeadTracking : MonoBehaviour
    {

        public bool trackRotation = true;

        public bool trackPosition = true;


        private bool updated;

        public bool updateEarly = false;

        [Header("Origin Settings")]
        [Tooltip("Position of the origin")]
        public Vector3 positionOrigin = new Vector3(0, 0, 0);


        void Update()
        {
            updated = false;
            if (updateEarly)
            {
                UpdateHead();
            }

        }

        void LateUpdate()
        {
            UpdateHead();
        }


        // Compute new head pose.
        private void UpdateHead()
        {
            //Debug.LogError("dddd"); ;
            if (updated)
            {  // Only one update per frame, please.
                return;
            }
            updated = true;
            XvXRManager.SDK.UpdateState();
            //HeadPose is updated in UpdateState()
            if (trackRotation && XvXRManager.SDK.IsVRMode)
            {
                var rot = XvXRManager.SDK.HeadPose.Orientation;

                transform.localRotation = rot * XvXRManager.SDK.ZeroQuaternion;


            }

            if (trackPosition)
            {
                Vector3 pos = XvXRManager.SDK.HeadPose.Position;

                transform.localPosition = pos + positionOrigin;

            }

            XvXRAndroidDevice androidDevice =
                XvXRManager.SDK.GetDevice() as XvXRAndroidDevice;
            if (androidDevice != null)
            {
                androidDevice.CaptureRenderSourcePose();
            }
        }
    }
}
