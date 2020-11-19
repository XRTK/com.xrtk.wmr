// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using XRTK.Definitions.Controllers.Hands;

#if WINDOWS_UWP

using System.Collections.Generic;
using UnityEngine;
using Windows.Perception.People;
using Windows.UI.Input.Spatial;
using XRTK.Services;
using XRTK.WindowsMixedReality.Extensions;
using XRTK.Extensions;
using XRTK.Definitions.Devices;
using XRTK.Definitions.Utilities;

#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Utilities
{
    /// <summary>
    /// Converts windows mixed reality hand data to XRTK's <see cref="HandData"/>.
    /// </summary>
    [Obsolete]
    public sealed class WindowsMixedRealityHandDataConverter
    {
#if WINDOWS_UWP

        /// <summary>
        /// Destructor.
        /// </summary>
        ~WindowsMixedRealityHandDataConverter()
        {
            if (!conversionProxyRootTransform.IsNull())
            {
                conversionProxyTransforms.Clear();
                conversionProxyRootTransform.Destroy();
            }
        }

        private Transform conversionProxyRootTransform;
        private readonly Dictionary<TrackedHandJoint, Transform> conversionProxyTransforms = new Dictionary<TrackedHandJoint, Transform>();
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
                handData.PointerPose = GetPointerPose(spatialInteractionSourceState);

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
        /// Gets updated joint <see cref="MixedRealityPose"/>s for all <see cref="TrackedHandJoint"/>s of a hand.
        /// </summary>
        /// <param name="platformJointPoses"><see cref="JointPose"/>s retrieved from the platform.</param>
        /// <param name="handRootPose">The hand's root <see cref="MixedRealityPose"/>.</param>
        /// <returns>Joint <see cref="MixedRealityPose"/>s in <see cref="TrackedHandJoint"/> ascending order.</returns>
        private MixedRealityPose[] GetJointPoses(JointPose[] platformJointPoses, MixedRealityPose handRootPose)
        {
            for (int i = 0; i < platformJointPoses.Length; i++)
            {
                var handJoint = jointIndices[i].ToTrackedHandJoint();
                jointPoses[(int)handJoint] = GetJointPose(handJoint, handRootPose, platformJointPoses[i]);
            }

            return jointPoses;
        }

        /// <summary>
        /// Gets a single joint's <see cref="MixedRealityPose"/> relative to the hand root pose.
        /// </summary>
        /// <param name="trackedHandJoint">The <see cref="TrackedHandJoint"/> Id for the joint to get a <see cref="MixedRealityPose"/> for.</param>
        /// <param name="handRootPose">The hand's root <see cref="MixedRealityPose"/>. Joint poses are always relative to the root pose.</param>
        /// <param name="jointPose"><see cref="JointPose"/> retrieved from the platform.</param>
        /// <returns>Joint <see cref="MixedRealityPose"/> relative to the hand's root pose.</returns>
        private MixedRealityPose GetJointPose(TrackedHandJoint trackedHandJoint, MixedRealityPose handRootPose, JointPose jointPose)
        {
            var jointTransform = GetProxyTransform(trackedHandJoint);
            var playspaceTransform = MixedRealityToolkit.CameraSystem.MainCameraRig.PlayspaceTransform;

            if (trackedHandJoint == TrackedHandJoint.Wrist)
            {
                jointTransform.localPosition = handRootPose.Position;
                jointTransform.localRotation = handRootPose.Rotation;
            }
            else
            {
                jointTransform.parent = playspaceTransform;
                jointTransform.localPosition = playspaceTransform.InverseTransformPoint(playspaceTransform.position + playspaceTransform.rotation * jointPose.Position.ToUnity());
                jointTransform.localRotation = Quaternion.Inverse(playspaceTransform.rotation) * playspaceTransform.rotation * jointPose.Orientation.ToUnity();
                jointTransform.parent = conversionProxyRootTransform;
            }

            return new MixedRealityPose(
                conversionProxyRootTransform.InverseTransformPoint(jointTransform.position),
                Quaternion.Inverse(conversionProxyRootTransform.rotation) * jointTransform.rotation);
        }

        /// <summary>
        /// Gets the hand's root <see cref="MixedRealityPose"/> in playspace.
        /// </summary>
        /// <param name="platformJointPoses"><see cref="JointPose"/>s retrieved from the platform.</param>
        /// <returns>The hand's <see cref="HandData.RootPose"/> <see cref="MixedRealityPose"/>.</returns>
        private MixedRealityPose GetHandRootPose(JointPose[] platformJointPoses)
        {
            // For WMR we use the wrist pose as the hand root pose.
            var wristPose = platformJointPoses[(int)HandJointKind.Wrist];
            var wristProxyTransform = GetProxyTransform(TrackedHandJoint.Wrist);

            // Convert to playspace.
            var playspaceTransform = MixedRealityToolkit.CameraSystem.MainCameraRig.PlayspaceTransform;
            wristProxyTransform.position = playspaceTransform.InverseTransformPoint(playspaceTransform.position + playspaceTransform.rotation * wristPose.Position.ToUnity());
            wristProxyTransform.rotation = Quaternion.Inverse(playspaceTransform.rotation) * playspaceTransform.rotation * wristPose.Orientation.ToUnity();

            return new MixedRealityPose(wristProxyTransform.position, wristProxyTransform.rotation);
        }

        /// <summary>
        /// Gets the hand's spatial pointer <see cref="MixedRealityPose"/> in playspace.
        /// </summary>
        /// <param name="spatialInteractionSourceState">Current <see cref="SpatialInteractionSourceState"/> snapshot of the hand.</param>
        /// <returns>The hand's <see cref="HandData.PointerPose"/> in playspace.</returns>
        private MixedRealityPose GetPointerPose(SpatialInteractionSourceState spatialInteractionSourceState)
        {
            var spatialPointerPose = spatialInteractionSourceState.TryGetPointerPose(WindowsMixedRealityUtilities.SpatialCoordinateSystem);
            if (spatialPointerPose != null)
            {
                var interactionSourcePose = spatialPointerPose.TryGetInteractionSourcePose(spatialInteractionSourceState.Source);
                if (interactionSourcePose != null)
                {
                    var playspaceTransform = MixedRealityToolkit.CameraSystem.MainCameraRig.PlayspaceTransform;
                    var pointerPosition = playspaceTransform.InverseTransformPoint(playspaceTransform.position + playspaceTransform.rotation * interactionSourcePose.Position.ToUnity());
                    var pointerRotation = Quaternion.Inverse(playspaceTransform.rotation) * playspaceTransform.rotation * interactionSourcePose.Orientation.ToUnity();

                    return new MixedRealityPose(pointerPosition, pointerRotation);
                }
            }

            return MixedRealityPose.ZeroIdentity;
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

        private Transform GetProxyTransform(TrackedHandJoint handJointKind)
        {
            if (conversionProxyRootTransform.IsNull())
            {
                conversionProxyRootTransform = new GameObject("WMR Hand Conversion Proxy").transform;
                conversionProxyRootTransform.transform.SetParent(MixedRealityToolkit.CameraSystem.MainCameraRig.PlayspaceTransform, false);
                conversionProxyRootTransform.gameObject.SetActive(false);
            }

            if (handJointKind == TrackedHandJoint.Wrist)
            {
                return conversionProxyRootTransform;
            }

            if (conversionProxyTransforms.ContainsKey(handJointKind))
            {
                return conversionProxyTransforms[handJointKind];
            }

            var transform = new GameObject($"WMR Hand {handJointKind} Proxy").transform;
            transform.SetParent(MixedRealityToolkit.CameraSystem.MainCameraRig.PlayspaceTransform, false);
            conversionProxyTransforms.Add(handJointKind, transform);

            return transform;
        }

#endif // WINDOWS_UWP
    }
}