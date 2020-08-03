// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Definitions.Controllers.Hands;
using XRTK.Definitions.Utilities;

#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using UnityEngine;
using Windows.Perception.People;
using Windows.UI.Input.Spatial;
using XRTK.Services;
using XRTK.WindowsMixedReality.Extensions;
using XRTK.Extensions;
using XRTK.Definitions.Devices;
using XRTK.Utilities;

#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Utilities
{
    /// <summary>
    /// Converts windows mixed reality hand data to XRTK's <see cref="HandData"/>.
    /// </summary>
    public sealed class WindowsMixedRealityHandDataConverter
    {
#if WINDOWS_UWP

        private readonly Dictionary<SpatialInteractionSourceHandedness, HandMeshObserver> handMeshObservers = new Dictionary<SpatialInteractionSourceHandedness, HandMeshObserver>();
        private readonly MixedRealityPose[] jointPoses = new MixedRealityPose[HandData.JointCount];

        private int[] handMeshTriangleIndices = null;
        private bool hasRequestedHandMeshObserverLeftHand = false;
        private bool hasRequestedHandMeshObserverRightHand = false;
        private Vector2[] handMeshUVs;

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

        /// <summary>
        /// Gets updated hand data for the current frame.
        /// </summary>
        /// <param name="spatialInteractionSourceState">Platform provided current input source state for the hand.</param>
        /// <param name="includeMeshData">If set, hand mesh information will be included in <see cref="HandData.Mesh"/>.</param>
        /// <param name="handData">The output <see cref="HandData"/>.</param>
        /// <returns>True, if data conversion was a success.</returns>
        public bool TryGetHandData(SpatialInteractionSourceState spatialInteractionSourceState, bool includeMeshData, out HandData handData)
        {
            // Here we check whether the hand is being tracked at all by the WMR system.
            HandPose handPose = spatialInteractionSourceState.TryGetHandPose();
            if (handPose == null)
            {
                handData = default;
                return false;
            }

            // The hand is being tracked, next we verify it meets our confidence requirements to consider
            // it tracked.
            var platformJointPoses = new JointPose[jointIndices.Length];
            handData = new HandData
            {
                TrackingState = handPose.TryGetJoints(WindowsMixedRealityUtilities.SpatialCoordinateSystem, jointIndices, platformJointPoses) ? TrackingState.Tracked : TrackingState.NotTracked,
                UpdatedAt = DateTimeOffset.UtcNow.Ticks
            };

            // If the hand is tracked per requirements, we get updated joint data
            // and other data needed for updating the hand controller's state.
            if (handData.TrackingState == TrackingState.Tracked)
            {
                handData.RootPose = GetHandRootPose(platformJointPoses);
                handData.Joints = GetJointPoses(platformJointPoses, handData.RootPose);

                if (includeMeshData && TryGetUpdatedHandMeshData(spatialInteractionSourceState, handPose, out HandMeshData data))
                {
                    handData.Mesh = data;
                }
                else
                {
                    // if hand mesh visualization is disabled make sure to destroy our hand mesh observer
                    // if it has already been created.
                    if (handMeshObservers.ContainsKey(spatialInteractionSourceState.Source.Handedness))
                    {
                        if (spatialInteractionSourceState.Source.Handedness == SpatialInteractionSourceHandedness.Left)
                        {
                            hasRequestedHandMeshObserverLeftHand = false;
                        }
                        else if (spatialInteractionSourceState.Source.Handedness == SpatialInteractionSourceHandedness.Right)
                        {
                            hasRequestedHandMeshObserverRightHand = false;
                        }

                        handMeshObservers.Remove(spatialInteractionSourceState.Source.Handedness);
                    }

                    handData.Mesh = HandMeshData.Empty;
                }
            }

            // Even if the hand is being tracked by the system but the confidence did not
            // meet our requirements, we return true. This allows the hand controller and visualizers
            // to react to tracking loss and keep the hand up for a given time before destroying the controller.
            return true;
        }

        /// <summary>
        /// Gets updated joint poses for all <see cref="TrackedHandJoint"/>s.
        /// </summary>
        /// <param name="platformJointPoses">Joint poses retrieved from the platform.</param>
        /// <param name="handRootPose">The hand's root pose.</param>
        /// <returns>Joint poses in <see cref="TrackedHandJoint"/> order.</returns>
        private MixedRealityPose[] GetJointPoses(JointPose[] platformJointPoses, MixedRealityPose handRootPose)
        {
            for (int i = 0; i < platformJointPoses.Length; i++)
            {
                var handJoint = jointIndices[i].ToTrackedHandJoint();
                jointPoses[(int)handJoint] = GetJointPose(platformJointPoses[i], handRootPose);
            }

            return jointPoses;
        }

        /// <summary>
        /// Gets a single joint's pose relative to the hand root pose.
        /// </summary>
        /// <param name="jointPose">Joint pose data retrieved from the platform.</param>
        /// <param name="handRootPose">The hand's root pose.</param>
        /// <returns>Converted joint pose in hand space.</returns>
        private MixedRealityPose GetJointPose(JointPose jointPose, MixedRealityPose handRootPose)
        {
            var jointPosition = jointPose.Position.ToUnity();
            var jointRotation = jointPose.Orientation.ToUnity();

            // We want the controller to follow the Playspace, so fold in the playspace transform here to 
            // put the controller pose into world space. We also want all joint poses to be
            // relative to the hand's root pose, so we account for that as well.
            var playspaceTransform = MixedRealityToolkit.CameraSystem.MainCameraRig.PlayspaceTransform;
            jointPosition = playspaceTransform.InverseTransformPoint(playspaceTransform.position + playspaceTransform.rotation * jointPosition);
            jointRotation = Quaternion.Inverse(playspaceTransform.rotation) * playspaceTransform.rotation * jointRotation;

            // To camera space
            var cameraTransform = MixedRealityToolkit.CameraSystem != null
                ? MixedRealityToolkit.CameraSystem.MainCameraRig.PlayerCamera.transform
                : CameraCache.Main.transform;

            // To hand root space
            jointPosition -= handRootPose.Position;
            //jointRotation = cameraTransform.rotation * jointRotation;

            return new MixedRealityPose(jointPosition, jointRotation);
        }

        /// <summary>
        /// Gets the hand's root pose.
        /// </summary>
        /// <param name="platformJointPoses">Joint poses retrieved from the platform.</param>
        /// <returns>The hands <see cref="HandData.RootPose"/> value.</returns>
        private MixedRealityPose GetHandRootPose(JointPose[] platformJointPoses)
        {
            // For WMR we use the wrist pose as the hand root pose.
            var wristPose = platformJointPoses[(int)HandJointKind.Wrist];

            // Convert to playspace.
            var playspaceTransform = MixedRealityToolkit.CameraSystem.MainCameraRig.PlayspaceTransform;
            var rootPosition = playspaceTransform.InverseTransformPoint(playspaceTransform.position + playspaceTransform.rotation * wristPose.Position.ToUnity());
            var rootRotation = Quaternion.Inverse(playspaceTransform.rotation) * playspaceTransform.rotation * wristPose.Orientation.ToUnity();

            return new MixedRealityPose(rootPosition, rootRotation);
        }

        /// <summary>
        /// Attempts to get updated hand mesh data.
        /// </summary>
        /// <param name="spatialInteractionSourceState">Platform provided current input source state for the hand.</param>
        /// <param name="handPose">Hand pose information retrieved for joint conversion.</param>
        /// <param name="data">Mesh information retrieved in case of success.</param>
        /// <returns>True, if mesh data could be loaded.</returns>
        private bool TryGetUpdatedHandMeshData(SpatialInteractionSourceState spatialInteractionSourceState, HandPose handPose, out HandMeshData data)
        {
            if (!handMeshObservers.ContainsKey(spatialInteractionSourceState.Source.Handedness) &&
                        !HasRequestedHandMeshObserver(spatialInteractionSourceState.Source.Handedness))
            {
                SetHandMeshObserver(spatialInteractionSourceState);
            }

            if (handMeshObservers.TryGetValue(spatialInteractionSourceState.Source.Handedness, out var handMeshObserver) && handMeshTriangleIndices == null)
            {
                var indexCount = handMeshObserver.TriangleIndexCount;
                var indices = new ushort[indexCount];
                handMeshObserver.GetTriangleIndices(indices);
                handMeshTriangleIndices = new int[indexCount];
                Array.Copy(indices, handMeshTriangleIndices, (int)handMeshObserver.TriangleIndexCount);

                // Compute neutral pose
                var neutralPoseVertices = new Vector3[handMeshObserver.VertexCount];
                var neutralPose = handMeshObserver.NeutralPose;
                var vertexAndNormals = new HandMeshVertex[handMeshObserver.VertexCount];
                var handMeshVertexState = handMeshObserver.GetVertexStateForPose(neutralPose);
                handMeshVertexState.GetVertices(vertexAndNormals);

                for (int i = 0; i < handMeshObserver.VertexCount; i++)
                {
                    neutralPoseVertices[i] = vertexAndNormals[i].Position.ToUnity();
                }

                // Compute UV mapping
                InitializeHandMeshUVs(neutralPoseVertices);
            }

            if (handMeshObserver != null && handMeshTriangleIndices != null)
            {
                var vertexAndNormals = new HandMeshVertex[handMeshObserver.VertexCount];
                var handMeshVertexState = handMeshObserver.GetVertexStateForPose(handPose);
                handMeshVertexState.GetVertices(vertexAndNormals);

                var meshTransform = handMeshVertexState.CoordinateSystem.TryGetTransformTo(WindowsMixedRealityUtilities.SpatialCoordinateSystem);
                if (meshTransform.HasValue)
                {
                    System.Numerics.Matrix4x4.Decompose(meshTransform.Value, out var scale, out var rotation, out var translation);

                    var handMeshVertices = new Vector3[handMeshObserver.VertexCount];
                    var handMeshNormals = new Vector3[handMeshObserver.VertexCount];

                    for (int i = 0; i < handMeshObserver.VertexCount; i++)
                    {
                        handMeshVertices[i] = vertexAndNormals[i].Position.ToUnity();
                        handMeshNormals[i] = vertexAndNormals[i].Normal.ToUnity();
                    }

                    data = new HandMeshData(
                        handMeshVertices,
                        handMeshTriangleIndices,
                        handMeshNormals,
                        handMeshUVs);

                    return true;
                }
            }

            return false;
        }

        private void InitializeHandMeshUVs(Vector3[] neutralPoseVertices)
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

        private bool HasRequestedHandMeshObserver(SpatialInteractionSourceHandedness handedness) =>
            handedness == SpatialInteractionSourceHandedness.Left ?
            hasRequestedHandMeshObserverLeftHand :
            handedness == SpatialInteractionSourceHandedness.Right && hasRequestedHandMeshObserverRightHand;

#endif // WINDOWS_UWP
    }
}