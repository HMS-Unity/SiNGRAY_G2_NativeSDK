using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;
using Unity.Profiling;
using UnityEngine;
using Singray.utils;

namespace Singray.MixedReality.Toolkit.XvXR.Input
{
    [MixedRealityDataProvider(typeof(IMixedRealityInputSystem),SupportedPlatforms.Android | SupportedPlatforms.WindowsEditor | SupportedPlatforms.MacEditor | SupportedPlatforms.LinuxEditor,"XvXR Device Manager")]
    public class XvXRInputDeviceManager : BaseInputDeviceManager, IMixedRealityCapabilityCheck
    {

        public XvXRInputDeviceManager(IMixedRealityInputSystem inputSystem, string name, uint priority, BaseMixedRealityProfile profile): base(inputSystem, name, priority, profile) { }
        #region IMixedRealityCapabilityCheck Implementation

        /// <inheritdoc />
        public bool CheckCapability(MixedRealityCapability capability)
        {
            // Only supports Articulated Hands so far.
            return (capability == MixedRealityCapability.ArticulatedHand);
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


     
         private readonly Dictionary<Handedness, XvXRController> trackedHands = new Dictionary<Handedness,XvXRController>();

        private static readonly ProfilerMarker UpdatePerfMarker = new ProfilerMarker("[MRTK] XvXRDeviceManager.Update");

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

      private void OnHandDetected(Handedness handedness)
        {
           
            // Only create a new hand if the hand does not exist
            if (!trackedHands.ContainsKey(handedness))
            {
                var pointers = RequestPointers(SupportedControllerType.ArticulatedHand, handedness);
                var inputSource = CoreServices.InputSystem?.RequestNewGenericInputSource($"XvXR {handedness} Controller", pointers, InputSourceType.Hand);
                var xvxrHand = new XvXRController(Microsoft.MixedReality.Toolkit.TrackingState.Tracked, handedness, inputSource);

                //Set pinch thresholds
                xvxrHand.HandDefinition.EnterPinchDistance = enterPinchDistance;
                xvxrHand.HandDefinition.ExitPinchDistance = exitPinchDistance;

                // Set the nreal attachment hand to the corresponding handedness
                if (handedness == Handedness.Left)
                {
                    xvxrHand.SetAttachmentHands(XvXRInput.Hands.GetHand(HandEnum.LeftHand));
                }
                else // handedness == Handedness.Right
                {
                    xvxrHand.SetAttachmentHands(XvXRInput.Hands.GetHand(HandEnum.RightHand));
                }
                
                // Set the pointers for an articulated hand to the nreal hand
                foreach (var pointer in pointers)
                {
                    pointer.Controller = xvxrHand;
                }

                trackedHands.Add(handedness, xvxrHand);

                CoreServices.InputSystem.RaiseSourceDetected(inputSource, xvxrHand);
            }
        }

         private void OnHandDetectionLost(Handedness handedness)
        {
            if (CoreServices.InputSystem != null)
            {
                 CoreServices.InputSystem.RaiseSourceLost(trackedHands[handedness].InputSource, trackedHands[handedness]);
            }

            // Disable the pointers if the hand is not tracking
            RecyclePointers(trackedHands[handedness].InputSource);

            // Remove hand from tracked hands
            trackedHands.Remove(trackedHands[handedness].ControllerHandedness);
        }
        private void UpdateXvXRTrackedHands(bool isLeftTracked, bool isRightTracked)
        {
            
            // Left Hand Update
            if (isLeftTracked && !trackedHands.ContainsKey(Handedness.Left))
            {
                OnHandDetected(Handedness.Left);
            }            
            else if (!isLeftTracked && trackedHands.ContainsKey(Handedness.Left))
            {
                OnHandDetectionLost(Handedness.Left);
            }

            // Right Hand Update
            if (isRightTracked && !trackedHands.ContainsKey(Handedness.Right))
            {

                OnHandDetected(Handedness.Right);
            }
            else if (!isRightTracked && trackedHands.ContainsKey(Handedness.Right))
            {
                OnHandDetectionLost(Handedness.Right);
            }
        }


        
        /// <inheritdoc />
        public override void Update()
        {
            base.Update();

            if(XvXRInput.IsInitialized){
                UpdateXvXRTrackedHands(XvXRInput.Hands.GetHandState(HandEnum.LeftHand).isTracked, XvXRInput.Hands.GetHandState(HandEnum.RightHand).isTracked);
                //XvXRLog.LogInfo("XvXRInputDeviceManager .........:"+XvXRInput.Hands.GetHandState(HandEnum.LeftHand).isTracked+","+ XvXRInput.Hands.GetHandState(HandEnum.RightHand).isTracked);
                // Update the hand/hands that are in trackedhands
                foreach (KeyValuePair<Handedness, XvXRController> hand in trackedHands)
                {           
                    hand.Value.UpdateState();
                }
            }
           
            
    
        }
    }
}
