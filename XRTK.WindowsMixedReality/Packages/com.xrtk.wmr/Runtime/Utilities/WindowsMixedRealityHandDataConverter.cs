// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using UnityEngine;
using Windows.Perception.People;
using Windows.UI.Input.Spatial;
using XRTK.Definitions.Controllers.Hands;
using XRTK.Definitions.Utilities;
using XRTK.Extensions;
using XRTK.Services;
using XRTK.WindowsMixedReality.Extensions;

namespace XRTK.WindowsMixedReality.Utilities
{
    /// <summary>
    /// Converts windows mixed reality hand data to <see cref="HandData"/>.
    /// </summary>
    public sealed class WindowsMixedRealityHandDataConverter
    {
        /// <summary>
        /// Gets or sets whether hand mesh data should be read and converted.
        /// </summary>
        public static bool HandMeshingEnabled { get; set; }

        private readonly Vector3[] unityJointPositions = new Vector3[jointIndices.Length];
        private readonly Quaternion[] unityJointOrientations = new Quaternion[jointIndices.Length];
        private readonly Dictionary<SpatialInteractionSourceHandedness, HandMeshObserver> handMeshObservers = new Dictionary<SpatialInteractionSourceHandedness, HandMeshObserver>();

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
        /// <returns>Platform agnostics hand data.</returns>
        public HandData GetHandData(SpatialInteractionSourceState spatialInteractionSourceState)
        {
            HandPose handPose = spatialInteractionSourceState.TryGetHandPose();
            HandData updatedHandData = new HandData
            {
                IsTracked = handPose != null,
                TimeStamp = DateTimeOffset.UtcNow.Ticks
            };

            if (updatedHandData.IsTracked)
            {
                // Accessing the hand mesh data involves copying quite a bit of data, so only do it if application requests it.
                if (HandMeshingEnabled)
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

                            updatedHandData.Mesh = new HandMeshData(
                                handMeshVertices,
                                handMeshTriangleIndices,
                                handMeshNormals,
                                handMeshUVs,
                                translation.ToUnity(),
                                rotation.ToUnity());
                        }
                    }
                }
                else if (handMeshObservers.ContainsKey(spatialInteractionSourceState.Source.Handedness))
                {
                    // if hand mesh visualization is disabled make sure to destroy our hand mesh observer if it has already been created
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

                JointPose[] jointPoses = new JointPose[jointIndices.Length];
                if (handPose.TryGetJoints(WindowsMixedRealityUtilities.SpatialCoordinateSystem, jointIndices, jointPoses))
                {
                    for (int i = 0; i < jointPoses.Length; i++)
                    {
                        unityJointOrientations[i] = jointPoses[i].Orientation.ToUnity();
                        unityJointPositions[i] = jointPoses[i].Position.ToUnity();

                        // We want the controller to follow the Playspace, so fold in the playspace transform here to 
                        // put the controller pose into world space.
                        unityJointPositions[i] = MixedRealityToolkit.CameraSystem.MainCameraRig.PlayspaceTransform.TransformPoint(unityJointPositions[i]);
                        unityJointOrientations[i] = MixedRealityToolkit.CameraSystem.MainCameraRig.PlayspaceTransform.rotation * unityJointOrientations[i];

                        TrackedHandJoint handJoint = jointIndices[i].ToTrackedHandJoint();
                        updatedHandData.Joints[(int)handJoint] = new MixedRealityPose(unityJointPositions[i], unityJointOrientations[i]);
                    }
                }
            }

            return updatedHandData;
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

        private bool HasRequestedHandMeshObserver(SpatialInteractionSourceHandedness handedness)
        {
            return handedness == SpatialInteractionSourceHandedness.Left
                ? hasRequestedHandMeshObserverLeftHand
                : handedness == SpatialInteractionSourceHandedness.Right && hasRequestedHandMeshObserverRightHand;
        }

    }
}
#endif // WINDOWS_UWP