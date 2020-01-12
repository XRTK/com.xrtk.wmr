// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Definitions.Devices;
using XRTK.Definitions.Utilities;
using XRTK.Interfaces.InputSystem;
using XRTK.Providers.Controllers.Hands;
using XRTK.WindowsMixedReality.Interfaces.Providers.Controllers;

#if WINDOWS_UWP
using UnityEngine;
using System.Collections.Generic;
using XRTK.Services;
using XRTK.WindowsMixedReality.Extensions;
using XRTK.WindowsMixedReality.Utilities;
using System;
using Windows.UI.Input.Spatial;
using Windows.Perception.People;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Controllers
{
    /// <summary>
    /// The default hand controller implementation for the Windows Universal platform.
    /// </summary>
    public class WindowsMixedRealityHandController : BaseHandController, IWindowsMixedRealityController
    {
        /// <summary>
        /// Controller constructor.
        /// </summary>
        /// <param name="trackingState">The controller's tracking state.</param>
        /// <param name="controllerHandedness">The controller's handedness.</param>
        /// <param name="inputSource">Optional input source of the controller.</param>
        /// <param name="interactions">Optional controller interactions mappings.</param>
        public WindowsMixedRealityHandController(TrackingState trackingState, Handedness controllerHandedness, IMixedRealityInputSource inputSource = null, MixedRealityInteractionMapping[] interactions = null)
            : base(trackingState, controllerHandedness, inputSource, interactions) { }

#if WINDOWS_UWP

        private readonly Vector3[] unityJointPositions = new Vector3[jointIndices.Length];
        private readonly Quaternion[] unityJointOrientations = new Quaternion[jointIndices.Length];
        private readonly Dictionary<SpatialInteractionSourceHandedness, HandMeshObserver> handMeshObservers = new Dictionary<SpatialInteractionSourceHandedness, HandMeshObserver>();

        private int[] handMeshTriangleIndices = null;
        private bool hasRequestedHandMeshObserverLeftHand = false;
        private bool hasRequestedHandMeshObserverRightHand = false;
        private Vector2[] handMeshUVs;
        private SpatialInteractionManager spatialInteractionManager = null;
        private SpatialInteractionSourceState state;

        private static readonly HandJointKind[] jointIndices = new HandJointKind[]
        {
            HandJointKind.Palm,
            HandJointKind.Wrist,
            HandJointKind.ThumbMetacarpal,
            HandJointKind.ThumbProximal,
            HandJointKind.ThumbDistal,
            HandJointKind.ThumbTip,
            HandJointKind.IndexMetacarpal,
            HandJointKind.IndexProximal,
            HandJointKind.IndexIntermediate,
            HandJointKind.IndexDistal,
            HandJointKind.IndexTip,
            HandJointKind.MiddleMetacarpal,
            HandJointKind.MiddleProximal,
            HandJointKind.MiddleIntermediate,
            HandJointKind.MiddleDistal,
            HandJointKind.MiddleTip,
            HandJointKind.RingMetacarpal,
            HandJointKind.RingProximal,
            HandJointKind.RingIntermediate,
            HandJointKind.RingDistal,
            HandJointKind.RingTip,
            HandJointKind.LittleMetacarpal,
            HandJointKind.LittleProximal,
            HandJointKind.LittleIntermediate,
            HandJointKind.LittleDistal,
            HandJointKind.LittleTip
        };

        /// <inheritdoc/>
        public void UpdateController(SpatialInteractionSourceState spatialInteractionSourceState)
        {
            state = spatialInteractionSourceState;
        }

        public override void UpdateController()
        {
            base.UpdateController();

            HandPose handPose = state.TryGetHandPose();

            // Hand is being tracked by the device, update controller using
            // current data, beginning with converting the WMR hand data
            // to the XRTK generic hand data model.
            HandData updatedHandData = new HandData
            {
                IsTracked = handPose != null,
                TimeStamp = DateTimeOffset.UtcNow.Ticks
            };

            if (updatedHandData.IsTracked)
            {
                // Accessing the hand mesh data involves copying quite a bit of data, so only do it if application requests it.
                //if (MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.HandVisualizationProfile.EnableHandMeshVisualization)
                //{
                //    if (!handMeshObservers.ContainsKey(state.Source.Handedness) && !HasRequestedHandMeshObserver(state.Source.Handedness))
                //    {
                //        SetHandMeshObserver(state);
                //    }

                //    if (handMeshObservers.TryGetValue(state.Source.Handedness, out HandMeshObserver handMeshObserver) && handMeshTriangleIndices == null)
                //    {
                //        uint indexCount = handMeshObserver.TriangleIndexCount;
                //        ushort[] indices = new ushort[indexCount];
                //        handMeshObserver.GetTriangleIndices(indices);
                //        handMeshTriangleIndices = new int[indexCount];
                //        Array.Copy(indices, handMeshTriangleIndices, (int)handMeshObserver.TriangleIndexCount);

                //        // Compute neutral pose
                //        Vector3[] neutralPoseVertices = new Vector3[handMeshObserver.VertexCount];
                //        HandPose neutralPose = handMeshObserver.NeutralPose;
                //        var vertexAndNormals = new HandMeshVertex[handMeshObserver.VertexCount];
                //        HandMeshVertexState handMeshVertexState = handMeshObserver.GetVertexStateForPose(neutralPose);
                //        handMeshVertexState.GetVertices(vertexAndNormals);

                //        for (int i = 0; i < handMeshObserver.VertexCount; i++)
                //        {
                //            neutralPoseVertices[i] = WindowsMixedRealityUtilities.SystemVector3ToUnity(vertexAndNormals[i].Position);
                //        }

                //        // Compute UV mapping
                //        InitializeHandMeshUVs(neutralPoseVertices);
                //    }

                //    if (handMeshObserver != null && handMeshTriangleIndices != null)
                //    {
                //        var vertexAndNormals = new HandMeshVertex[handMeshObserver.VertexCount];
                //        var handMeshVertexState = handMeshObserver.GetVertexStateForPose(handPose);
                //        handMeshVertexState.GetVertices(vertexAndNormals);

                //        var meshTransform = handMeshVertexState.CoordinateSystem.TryGetTransformTo(WindowsMixedRealityUtilities.SpatialCoordinateSystem);
                //        if (meshTransform.HasValue)
                //        {
                //            System.Numerics.Vector3 scale;
                //            System.Numerics.Quaternion rotation;
                //            System.Numerics.Vector3 translation;
                //            System.Numerics.Matrix4x4.Decompose(meshTransform.Value, out scale, out rotation, out translation);

                //            var handMeshVertices = new Vector3[handMeshObserver.VertexCount];
                //            var handMeshNormals = new Vector3[handMeshObserver.VertexCount];

                //            for (int i = 0; i < handMeshObserver.VertexCount; i++)
                //            {
                //                handMeshVertices[i] = WindowsMixedRealityUtilities.SystemVector3ToUnity(vertexAndNormals[i].Position);
                //                handMeshNormals[i] = WindowsMixedRealityUtilities.SystemVector3ToUnity(vertexAndNormals[i].Normal);
                //            }

                //            updatedHandData.Mesh = new HandMeshData(
                //                handMeshVertices,
                //                handMeshTriangleIndices,
                //                handMeshNormals,
                //                handMeshUVs,
                //                WindowsMixedRealityUtilities.SystemVector3ToUnity(translation),
                //                WindowsMixedRealityUtilities.SystemQuaternionToUnity(rotation));
                //        }
                //    }
                //}
                //else if (handMeshObservers.ContainsKey(state.Source.Handedness))
                //{
                //    // if hand mesh visualization is disabled make sure to destroy our hand mesh observer if it has already been created
                //    if (state.Source.Handedness == SpatialInteractionSourceHandedness.Left)
                //    {
                //        hasRequestedHandMeshObserverLeftHand = false;
                //    }
                //    else if (state.Source.Handedness == SpatialInteractionSourceHandedness.Right)
                //    {
                //        hasRequestedHandMeshObserverRightHand = false;
                //    }

                //    handMeshObservers.Remove(state.Source.Handedness);
                //}

                JointPose[] jointPoses = new JointPose[jointIndices.Length];
                if (handPose.TryGetJoints(WindowsMixedRealityUtilities.SpatialCoordinateSystem, jointIndices, jointPoses))
                {
                    for (int i = 0; i < jointPoses.Length; i++)
                    {
                        unityJointOrientations[i] = WindowsMixedRealityUtilities.SystemQuaternionToUnity(jointPoses[i].Orientation);
                        unityJointPositions[i] = WindowsMixedRealityUtilities.SystemVector3ToUnity(jointPoses[i].Position);

                        // We want the controller to follow the Playspace, so fold in the playspace transform here to 
                        // put the controller pose into world space.
                        unityJointPositions[i] = MixedRealityToolkit.CameraSystem.CameraRig.PlayspaceTransform.TransformPoint(unityJointPositions[i]);
                        unityJointOrientations[i] = MixedRealityToolkit.CameraSystem.CameraRig.PlayspaceTransform.rotation * unityJointOrientations[i];

                        TrackedHandJoint handJoint = jointIndices[i].ToTrackedHandJoint();
                        updatedHandData.Joints[(int)handJoint] = new MixedRealityPose(unityJointPositions[i], unityJointOrientations[i]);
                    }
                }
            }

            UpdateBase(updatedHandData);
        }

        /// <summary>
        /// Gets the native spatial interaction manager instance.
        /// </summary>
        private SpatialInteractionManager SpatialInteractionManager
        {
            get
            {
                if (spatialInteractionManager == null)
                {
                    UnityEngine.WSA.Application.InvokeOnUIThread(() =>
                    {
                        spatialInteractionManager = SpatialInteractionManager.GetForCurrentView();
                    }, true);
                }

                return spatialInteractionManager;
            }
        }

        protected void InitializeHandMeshUVs(Vector3[] neutralPoseVertices)
        {
            if (neutralPoseVertices.Length == 0)
            {
                Debug.LogError("Loaded 0 verts for neutralPoseVertices");
            }

            float minY = neutralPoseVertices[0].y;
            float maxY = minY;

            float maxMagnitude = 0.0f;

            for (int ix = 1; ix < neutralPoseVertices.Length; ix++)
            {
                Vector3 p = neutralPoseVertices[ix];

                if (p.y < minY)
                {
                    minY = p.y;
                }
                else if (p.y > maxY)
                {
                    maxY = p.y;
                }
                float d = p.x * p.x + p.y * p.y;
                if (d > maxMagnitude) maxMagnitude = d;
            }

            maxMagnitude = Mathf.Sqrt(maxMagnitude);
            float scale = 1.0f / (maxY - minY);

            handMeshUVs = new Vector2[neutralPoseVertices.Length];

            for (int ix = 0; ix < neutralPoseVertices.Length; ix++)
            {
                Vector3 p = neutralPoseVertices[ix];

                handMeshUVs[ix] = new Vector2(p.x * scale + 0.5f, (p.y - minY) * scale);
            }
        }

        private Handedness ConvertHandedness(SpatialInteractionSourceHandedness input)
        {
            switch (input)
            {
                case SpatialInteractionSourceHandedness.Left:
                    return Handedness.Left;
                case SpatialInteractionSourceHandedness.Right:
                    return Handedness.Right;
                case SpatialInteractionSourceHandedness.Unspecified:
                default:
                    return Handedness.Other;
            }
        }

        private async void SetHandMeshObserver(SpatialInteractionSourceState sourceState)
        {
            if (handMeshObservers.ContainsKey(sourceState.Source.Handedness))
            {
                handMeshObservers[sourceState.Source.Handedness] = await sourceState.Source.TryCreateHandMeshObserverAsync();
            }
            else
            {
                handMeshObservers.Add(sourceState.Source.Handedness, await sourceState.Source.TryCreateHandMeshObserverAsync());
            }

            hasRequestedHandMeshObserverLeftHand = sourceState.Source.Handedness == SpatialInteractionSourceHandedness.Left;
            hasRequestedHandMeshObserverRightHand = sourceState.Source.Handedness == SpatialInteractionSourceHandedness.Right;
        }

        private bool HasRequestedHandMeshObserver(SpatialInteractionSourceHandedness handedness)
        {
            return handedness == SpatialInteractionSourceHandedness.Left ? hasRequestedHandMeshObserverLeftHand :
                handedness == SpatialInteractionSourceHandedness.Right ? hasRequestedHandMeshObserverRightHand : false;
        }

#endif // WINDOWS_UWP
    }
}