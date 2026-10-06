using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Unity.Profiling;
using UnityEngine;
using Singray.Foundation;

namespace Singray.MixedReality.Toolkit.XvXR.Input
{
    [MixedRealityDataProvider(typeof(IMixedRealityInputSystem), SupportedPlatforms.Android | SupportedPlatforms.WindowsEditor | SupportedPlatforms.MacEditor | SupportedPlatforms.LinuxEditor, "XvXR Joystick Manager")]
    public class XvXRJoystickManager : BaseInputDeviceManager, IMixedRealityCapabilityCheck
    {

        const string TAG = "XvXRJoystickManager";

        public XvXRJoystickManager(IMixedRealityInputSystem inputSystem, string name, uint priority, BaseMixedRealityProfile profile) : base(inputSystem, name, priority, profile) { }
        #region IMixedRealityCapabilityCheck Implementation

        /// <inheritdoc />
        public bool CheckCapability(MixedRealityCapability capability)
        {
            // Only supports Articulated Hands so far.
            return (capability == MixedRealityCapability.MotionController);
        }


        #endregion IMixedRealityCapabilityCheck Implementation

#if UNITY_EDITOR
        /// <summary>
        /// The distance between the index finger tip and the thumb tip required to enter the pinch/air tap selection gesture.
        /// The pinch gesture enter will be registered for all values less than the EnterPinchDistance. The default EnterPinchDistance value is 0.02 and must be between 0.015 and 0.1. 
        /// </summary>

        public float enterPinchDistance => 0.1f;//SettingsProfile.EnterPinchDistance;

        /// <summary>
        /// The distance between the index finger tip and the thumb tip required to exit the pinch/air tap gesture.
        /// The pinch gesture exit will be registered for all values greater than the ExitPinchDistance. The default ExitPinchDistance value is 0.05 and must be between 0.015 and 0.1. 
        /// </summary>
        public float exitPinchDistance => 0.1f;// SettingsProfile.ExitPinchDistance;
#else
        public float enterPinchDistance => 0.02f;
        public float exitPinchDistance => 0.05f;
#endif

        private static readonly ProfilerMarker UpdatePerfMarker = new ProfilerMarker("[MRTK] XvXRJoystickManager.Update");

        // private XvXRJoystickController4 mController4;

        private XvXRJoystickController mRightHandController;
        private XvXRJoystickController mLeftHandController;
        private XvXRJoystickController mSeerPadController;



        /// <inheritdoc />
        public override void Enable()
        {
            base.Enable();
        }

        /// <inheritdoc />
        public override void Disable()
        {
            base.Disable();
        }




        void SetupHandController( TrackerType trackerType)
        {

            switch (trackerType)
            {
                case TrackerType.Left:

                    if (mLeftHandController != null)
                    {
                        return;
                    }
                    var leftPointers = RequestPointers(SupportedControllerType.HPMotionController, Handedness.Left);
                    var leftInputSource = CoreServices.InputSystem?.RequestNewGenericInputSource("XvXR Left Joystick Controller", leftPointers, InputSourceType.Controller);
                    var leftHand = new XvXRJoystickController(TrackingState.Tracked, Handedness.Left, leftInputSource);

                    //Temporarily drive this using the right-hand pose
                    leftHand.SetTrackerType(TrackerType.Left);
                    //Set pinch thresholds
                    leftHand.HandDefinition.EnterPinchDistance = enterPinchDistance;
                    leftHand.HandDefinition.ExitPinchDistance = exitPinchDistance;

                    // Set the pointers for an articulated hand to the nreal hand
                    foreach (var pointer in leftPointers)
                    {
                        pointer.Controller = leftHand;
                    }

                    mLeftHandController = leftHand;

                    CoreServices.InputSystem.RaiseSourceDetected(leftInputSource, leftHand);
                    break;
                case TrackerType.Right:

                    if (mRightHandController != null)
                    {
                        return;
                    }

                    var rightPointers = RequestPointers(SupportedControllerType.HPMotionController, Handedness.Right);
                    var rightInputSource = CoreServices.InputSystem?.RequestNewGenericInputSource("XvXR Right Joystick Controller", rightPointers, InputSourceType.Controller);
                    var rightHand = new XvXRJoystickController(TrackingState.Tracked, Handedness.Right, rightInputSource);
                    rightHand.SetTrackerType(TrackerType.Right);

                    //Set pinch thresholds
                    rightHand.HandDefinition.EnterPinchDistance = enterPinchDistance;
                    rightHand.HandDefinition.ExitPinchDistance = exitPinchDistance;

                    // Set the pointers for an articulated hand to the nreal hand
                    foreach (var pointer in rightPointers)
                    {
                        pointer.Controller = rightHand;
                    }

                    mRightHandController = rightHand;
                    CoreServices.InputSystem.RaiseSourceDetected(rightInputSource, rightHand);
                    break;
                case TrackerType.SeerPad:

                    if (mSeerPadController != null)
                    {
                        return;
                    }

                    var seerPadPointers = RequestPointers(SupportedControllerType.HPMotionController, Handedness.Right);
                    var seerPadInputSource = CoreServices.InputSystem?.RequestNewGenericInputSource("XvXR SeerPad Joystick Controller", seerPadPointers, InputSourceType.Controller);
                    var seerPadHand = new XvXRJoystickController(TrackingState.Tracked, Handedness.Right, seerPadInputSource);
                    seerPadHand.SetTrackerType(TrackerType.SeerPad);

                    //Set pinch thresholds
                    seerPadHand.HandDefinition.EnterPinchDistance = enterPinchDistance;
                    seerPadHand.HandDefinition.ExitPinchDistance = exitPinchDistance;

                    // Set the pointers for an articulated hand to the nreal hand
                    foreach (var pointer in seerPadPointers)
                    {
                        pointer.Controller = seerPadHand;
                    }

                    mSeerPadController = seerPadHand;
                    CoreServices.InputSystem.RaiseSourceDetected(seerPadInputSource, seerPadHand);
                    break;
                default:
                    break;
            }

          
        }


        private void OnHandDetectionLost(TrackerType trackerType)
        {


            switch (trackerType)
            {
                case TrackerType.Left:

                    if (mLeftHandController!=null) {
                        if (CoreServices.InputSystem != null)
                        {
                            CoreServices.InputSystem.RaiseSourceLost(mLeftHandController.InputSource, mLeftHandController);
                        }

                        // Disable the pointers if the hand is not tracking
                        RecyclePointers(mLeftHandController.InputSource);

                        mLeftHandController = null;
                    }
                    
                    break;
                case TrackerType.Right:


                    if (mRightHandController!=null) {
                        if (CoreServices.InputSystem != null)
                        {
                            CoreServices.InputSystem.RaiseSourceLost(mRightHandController.InputSource, mRightHandController);
                        }

                        // Disable the pointers if the hand is not tracking
                        RecyclePointers(mRightHandController.InputSource);

                        mRightHandController = null;
                    }
                  
                    break;
                case TrackerType.SeerPad:
                    if (mSeerPadController!=null) {
                        if (CoreServices.InputSystem != null)
                        {
                            CoreServices.InputSystem.RaiseSourceLost(mSeerPadController.InputSource, mSeerPadController);
                        }

                        // Disable the pointers if the hand is not tracking
                        RecyclePointers(mSeerPadController.InputSource);

                        mSeerPadController = null;
                    }
                   
                    break;
                default:
                    break;
            }

          

        
        }

        /// <inheritdoc />
        public override void Update()
        {
            using (UpdatePerfMarker.Auto())
            {
                base.Update();

                if (XvJoystickManager.Instance)
                {
                    if (XvJoystickManager.Instance.useSeerPad)
                    {
                        if (XvJoystickManager.Instance.IsConnected(TrackerType.SeerPad))
                        {
                            SetupHandController(TrackerType.SeerPad);
                            mSeerPadController.UpdateState();
                        }
                        else
                        {
                            OnHandDetectionLost(TrackerType.SeerPad);
                        }
                    }
                    else {
                        if (XvJoystickManager.Instance.IsConnected(TrackerType.Right))
                        {
                            SetupHandController(TrackerType.Right);
                            mRightHandController.UpdateState();

                            MyDebugTool.Log(123);
                        }
                        else
                        {
                            OnHandDetectionLost(TrackerType.Right);
                        }


                        if (XvJoystickManager.Instance.IsConnected(TrackerType.Left))
                        {
                            SetupHandController(TrackerType.Left);
                            mLeftHandController.UpdateState();
                        }
                        else {
                            OnHandDetectionLost(TrackerType.Left);
                        }
                    }
                   
                    
                }
            }

        }
    }
}
