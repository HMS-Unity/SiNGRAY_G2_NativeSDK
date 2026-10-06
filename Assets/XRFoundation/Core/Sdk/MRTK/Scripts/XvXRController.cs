using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.MixedReality.Toolkit;
using Microsoft.MixedReality.Toolkit.Input;
using Microsoft.MixedReality.Toolkit.Utilities;

using Unity.Profiling;
using UnityEngine;
using Singray;

namespace Singray.MixedReality.Toolkit.XvXR.Input
{
    [MixedRealityController(SupportedControllerType.ArticulatedHand, new[] { Handedness.Left, Handedness.Right })]
    public class XvXRController : BaseHand
    {
        /// <summary>
        /// Constructor for a XvXR Articulated Hand
        /// </summary>
        /// <param name="trackingState">Tracking state for the controller</param>
        /// <param name="controllerHandedness">Handedness of this controller (Left or Right)</param>
        /// <param name="inputSource">The origin of user input for this controller</param>
        /// <param name="interactions">The controller interaction map between physical inputs and the logical representation in MRTK</param>
        public XvXRController(
            Microsoft.MixedReality.Toolkit.TrackingState trackingState,
            Handedness controllerHandedness,
            IMixedRealityInputSource inputSource = null,
            MixedRealityInteractionMapping[] interactions = null)
            : base(trackingState, controllerHandedness, inputSource, interactions, new ArticulatedHandDefinition(inputSource, controllerHandedness))
        { }

        // Joint poses of the MRTK hand based on the XvXR hand data
        private readonly Dictionary<TrackedHandJoint, MixedRealityPose> jointPoses = new Dictionary<TrackedHandJoint, MixedRealityPose>();

        #region IMixedRealityHand Implementation

        /// <inheritdoc/>
        public override bool TryGetJoint(TrackedHandJoint joint, out MixedRealityPose pose) => jointPoses.TryGetValue(joint, out pose);

        #endregion IMixedRealityHand Implementation

        private ArticulatedHandDefinition handDefinition;
        internal ArticulatedHandDefinition HandDefinition => handDefinition ?? (handDefinition = Definition as ArticulatedHandDefinition);

        /// <summary>
        /// If true, the current joint pose supports far interaction via the default controller ray.  
        /// </summary>
        public override bool IsInPointingPose => HandDefinition.IsInPointingPose;

        /// <summary>
        /// If true, the hand is in air tap gesture, also called the pinch gesture.
        /// </summary>
        public bool IsPinching => HandDefinition.IsPinching;

        // Array of TrackedHandJoint names
        private static readonly TrackedHandJoint[] TrackedHandJointEnum = (TrackedHandJoint[])Enum.GetValues(typeof(TrackedHandJoint));

        // The XvXR AttachmentHand contains the joint poses for the current XvXR hand in frame. There is one AttachmentHand, either 
        // left or right, associated with a XvXRMotionArticulatedHand.
        private XvXRHand xvxrHand = null;

      


        private List<TrackedHandJoint> metacarpals = new List<TrackedHandJoint>
        {
            TrackedHandJoint.IndexMetacarpal,
            TrackedHandJoint.MiddleMetacarpal,
            TrackedHandJoint.RingMetacarpal,
        };

        /// <summary>
        /// Set the XvXR hands required for retrieving joint pose data.  A XvXR AttachmentHand contains AttachmentPointFlags which are equivalent to 
        /// MRTK's TrackedHandJoint.  The XvXR AttachmentHand contains all joint poses for a hand except the metacarpals.  The XvXR Hand is 
        /// used to retrieve the metacarpal joint poses.
        /// </summary>
        internal void SetAttachmentHands(XvXRHand xvhand)
        {
            // Set the XvXR hand with the corresponding handedness
            xvxrHand = xvhand;          
        }


        /// <summary>
        /// Adds the joint poses calculated from the NRSDK to the jointPoses Dictionary.
        /// </summary>
        private void SetJointPoses()
        {
            foreach (TrackedHandJoint joint in TrackedHandJointEnum)
            {
                if (xvxrHand != null && xvxrHand.GetHandState().isTracked)
                {
                    IsPositionAvailable = IsRotationAvailable = true;

                    HandJointID jointID = ConvertMRTKJointToXvXRJoint(joint);
                    // Get the pose of the XvXR joint
                    HandState hand= xvxrHand.GetHandState();
                    Pose p= hand.jointsPoseDict[jointID];
                    Vector3 forward = p.up;
                    Vector3 up = p.forward;
                    
                    // Set the pose calculated by the XvXR to a mixed reality pose
                    MixedRealityPose pose = new MixedRealityPose(p.position, Quaternion.LookRotation(forward, -up));
                    jointPoses[joint] = pose;
                }
                else
                {
                    IsPositionAvailable = IsRotationAvailable = false;

                    jointPoses[joint] = MixedRealityPose.ZeroIdentity;
                }
            }
        }



        /// <summary>
        /// Converts a TrackedHandJoint to a XvXR AttachmentPointFlag. An AttachmentPointFlag is XvXR's version of MRTK's TrackedHandJoint.
        /// </summary>
        /// <param name="joint">TrackedHandJoint to be mapped to a XvXR Hand Joint</param>
        /// <returns>XvXR AttachmentPointFlag pose</returns>
        static internal HandJointID ConvertMRTKJointToXvXRJoint(TrackedHandJoint joint)
        {
            switch (joint)
            {
                case TrackedHandJoint.Palm: return HandJointID.Palm;//AttachmentPointFlags.Palm;
                case TrackedHandJoint.Wrist: return HandJointID.Wrist;//AttachmentPointFlags.Wrist;

                case TrackedHandJoint.ThumbProximalJoint: return HandJointID.ThumbProximalJoint;//AttachmentPointFlags.ThumbProximalJoint;
                case TrackedHandJoint.ThumbDistalJoint: return HandJointID.ThumbDistalJoint;//AttachmentPointFlags.ThumbDistalJoint;
                case TrackedHandJoint.ThumbTip: return HandJointID.ThumbTip;//AttachmentPointFlags.ThumbTip;
                case TrackedHandJoint.ThumbMetacarpalJoint: return HandJointID.ThumbMetacarpalJoint;

                case TrackedHandJoint.IndexMetacarpal: return HandJointID.IndexMetacarpal;//AttachmentPointFlags.IndexKnuckle;
                case TrackedHandJoint.IndexKnuckle: return HandJointID.IndexKnuckle;//AttachmentPointFlags.IndexKnuckle;
                case TrackedHandJoint.IndexMiddleJoint: return HandJointID.IndexMiddleJoint;//.IndexMiddleJoint;
                case TrackedHandJoint.IndexDistalJoint: return HandJointID.IndexDistalJoint;//AttachmentPointFlags.IndexDistalJoint;
                case TrackedHandJoint.IndexTip: return HandJointID.IndexTip;//AttachmentPointFlags.IndexTip;

                case TrackedHandJoint.MiddleMetacarpal: return HandJointID.MiddleMetacarpal;
                case TrackedHandJoint.MiddleKnuckle: return HandJointID.MiddleKnuckle;//AttachmentPointFlags.MiddleKnuckle;
                case TrackedHandJoint.MiddleMiddleJoint: return HandJointID.MiddleMiddleJoint;//AttachmentPointFlags.MiddleMiddleJoint;
                case TrackedHandJoint.MiddleDistalJoint: return HandJointID.MiddleDistalJoint;//AttachmentPointFlags.MiddleDistalJoint;
                case TrackedHandJoint.MiddleTip: return HandJointID.MiddleTip;//AttachmentPointFlags.MiddleTip;

                case TrackedHandJoint.RingMetacarpal: return HandJointID.RingMetacarpal;//AttachmentPointFlags.RingKnuckle;
                case TrackedHandJoint.RingKnuckle: return HandJointID.RingKnuckle;//AttachmentPointFlags.RingKnuckle;
                case TrackedHandJoint.RingMiddleJoint: return HandJointID.RingMiddleJoint;//AttachmentPointFlags.RingMiddleJoint;
                case TrackedHandJoint.RingDistalJoint: return HandJointID.RingDistalJoint;//AttachmentPointFlags.RingDistalJoint;
                case TrackedHandJoint.RingTip: return HandJointID.RingTip;//AttachmentPointFlags.RingTip;

                case TrackedHandJoint.PinkyKnuckle: return HandJointID.PinkyKnuckle;//AttachmentPointFlags.PinkyKnuckle;
                case TrackedHandJoint.PinkyMiddleJoint: return HandJointID.PinkyMiddleJoint;//AttachmentPointFlags.PinkyMiddleJoint;
                case TrackedHandJoint.PinkyDistalJoint: return HandJointID.PinkyDistalJoint;//AttachmentPointFlags.PinkyDistalJoint;
                case TrackedHandJoint.PinkyTip: return HandJointID.PinkyTip;//AttachmentPointFlags.PinkyTip;
                case TrackedHandJoint.PinkyMetacarpal: return HandJointID.PinkyMetacarpal;

                // Metacarpals are not included in AttachmentPointFlags
                default: return HandJointID.Wrist;//AttachmentPointFlags.Wrist;
            }
        }

        private static readonly ProfilerMarker UpdateStatePerfMarker = new ProfilerMarker("[MRTK] XvXRMotionArticulatedHand.UpdateState");

        /// <summary>
        /// Updates the joint poses and interactions for the articulated hand.
        /// </summary>
        public void UpdateState()
        {

            // Debug.Log("XvXRController .........:UpdateState");
            using (UpdateStatePerfMarker.Auto())
            {
                // Get and set the joint poses provided by the XvXR Controller 
                SetJointPoses();

                // Update hand joints and raise event via handDefinition
                HandDefinition?.UpdateHandJoints(jointPoses);

                UpdateInteractions();

                UpdateVelocity();
                if(xvxrHand!=null){
                    //MyDebugTool.Log("XvXRController .........:"+(int)xvxrHand.HandEnum);
                }
               

               
            }
        }

        /// <summary>
        /// Updates the visibility of the hand ray and raises input system events based on joint pose data.
        /// </summary>
        protected void UpdateInteractions()
        {
            MixedRealityPose pointerPose = jointPoses[TrackedHandJoint.Palm];
            MixedRealityPose gripPose = jointPoses[TrackedHandJoint.Palm];
            MixedRealityPose indexPose = jointPoses[TrackedHandJoint.IndexTip];
            // Only update the hand ray if the hand is in pointing pose
            if (IsInPointingPose)
            {
                /*
                if (xvxrHand.HandEnum == HandEnum.LeftHand)
                {
                    HandRay.Update(pointerPose.Position , GetPalmNormal() - new Vector3(1f, 0, 0), CameraCache.Main.transform, ControllerHandedness);
                }
                else
                {
                    HandRay.Update(pointerPose.Position, GetPalmNormal(), CameraCache.Main.transform, ControllerHandedness);
                }
                */
                HandRay.Update(pointerPose.Position, GetPalmNormal(), CameraCache.Main.transform, ControllerHandedness);
                Ray ray = HandRay.Ray;
                
                pointerPose.Position = jointPoses[TrackedHandJoint.IndexKnuckle].Position;//ray.origin;
                /*
                if (xvxrHand.HandEnum == HandEnum.LeftHand)
                {
                    Quaternion v = Quaternion.LookRotation(ray.direction) * Quaternion.Euler(0, 15, 0);
                    v.x = v.x * 0.8f;
                    pointerPose.Rotation = v;
                }
                else
                {
                    Quaternion v = Quaternion.LookRotation(ray.direction);
                    v.x = v.x * 0.8f;
                    pointerPose.Rotation = v;
                }
                */
                Quaternion v = Quaternion.LookRotation(ray.direction);
                v.x = v.x * 0.8f;
                pointerPose.Rotation = v;

                //pointerPose.Rotation = Quaternion.LookRotation(ray.direction);
                //pointerPose.Rotation = Quaternion.LookRotation(ray.direction, transform.up); //Quaternion.LookRotation(ray.direction, transform.up);
                // pointerPose.Rotation = Quaternion.LookRotation(ray.direction + new Vector3(0.1f, 0, 0));
                //pointerPose.Rotation = Quaternion.LookRotation(Vector3.forward);
            }

            CoreServices.InputSystem?.RaiseSourcePoseChanged(InputSource, this, gripPose);
           
            for (int i = 0; i < Interactions?.Length; i++)
            {
                switch (Interactions[i].InputType)
                {
                    case DeviceInputType.SpatialPointer:
                        Interactions[i].PoseData = pointerPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, pointerPose);
                        }
                        break;
                    case DeviceInputType.SpatialGrip:
                        Interactions[i].PoseData = gripPose;
                        if (Interactions[i].Changed)
                        {
                            CoreServices.InputSystem?.RaisePoseInputChanged(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction, gripPose);
                        }
                        break;
                    case DeviceInputType.Select:
                    case DeviceInputType.TriggerPress:
                    case DeviceInputType.GripPress:
                        Interactions[i].BoolData = IsPinching;
                        if (Interactions[i].Changed)
                        {
                            if (Interactions[i].BoolData)
                            {
                                CoreServices.InputSystem?.RaiseOnInputDown(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                            else
                            {
                                CoreServices.InputSystem?.RaiseOnInputUp(InputSource, ControllerHandedness, Interactions[i].MixedRealityInputAction);
                            }
                        }
                        break;
                    case DeviceInputType.IndexFinger:
                        HandDefinition?.UpdateCurrentIndexPose(Interactions[i]);
                        break;
                    case DeviceInputType.ThumbStick:
                        HandDefinition?.UpdateCurrentTeleportPose(Interactions[i]);
                        break;
                }
            }
        }
    }
}
