// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Providers.Controllers;
using XRTK.WindowsMixedReality.Profiles;
using XRTK.Providers.Controllers.Hands;
using System.Collections.Generic;

#if WINDOWS_UWP
using XRTK.WindowsMixedReality.Utilities;
using XRTK.WindowsMixedReality.Extensions;
using Windows.Perception.People;
using System.Linq;
using XRTK.Interfaces.InputSystem;
using XRTK.Definitions.Utilities;
using XRTK.Utilities;
using Windows.Perception;
using UnityEngine;
using XRTK.Definitions.Devices;
using XRTK.Services;
using System;
using Windows.UI.Input.Spatial;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Controllers
{
    /// <summary>
    /// The data provider for <see cref="Definitions.Utilities.SupportedPlatforms.WindowsUniversal"/> hand controller
    /// support. It's responsible for converting the platform data to agnostic data the <see cref="Providers.Controllers.Hands.MixedRealityHandController"/> can work with.
    /// </summary>
    public class WindowsMixedRealityHandControllerDataProvider : BaseControllerDataProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the data provider as assigned in the configuration profile.</param>
        /// <param name="priority">Data provider priority controls the order in the service registry.</param>
        /// <param name="profile">Controller data provider profile assigned to the provider instance in the configuration inspector.</param>
        public WindowsMixedRealityHandControllerDataProvider(string name, uint priority, WindowsMixedRealityHandControllerDataProviderProfile profile)
            : base(name, priority, profile)
        {
            this.profile = profile;
        }

        /// <summary>
        /// The active data provider profile.
        /// </summary>
        private readonly WindowsMixedRealityHandControllerDataProviderProfile profile;

        /// <summary>
        /// Dictionary of currently registered controllers with the data provider.
        /// </summary>
        private readonly Dictionary<uint, MixedRealityHandController> controllers = new Dictionary<uint, MixedRealityHandController>();

#if WINDOWS_UWP

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
        /// Dictionary capturing cached interaction states from a previous frame.
        /// </summary>
        private readonly Dictionary<uint, SpatialInteractionSourceState> cachedInteractionSourceStates = new Dictionary<uint, SpatialInteractionSourceState>();

        private SpatialInteractionManager spatialInteractionManager = null;
        /// <summary>
        /// Gets the native <see cref="Windows.UI.Input.Spatial.SpatialInteractionManager"/> instace for the current application
        /// state.
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

        #region IMixedRealityControllerDataProvider lifecycle implementation

        /// <inheritdoc/>
        public override void Update()
        {
            base.Update();

            // Update existing controllers or create a new one if needed.
            IReadOnlyList<SpatialInteractionSourceState> sources = GetCurrentSources();
            if (sources == null)
            {
                return;
            }

            for (int i = 0; i < sources.Count; i++)
            {
                SpatialInteractionSourceState sourceState = sources[i];
                SpatialInteractionSource spatialInteractionSource = sourceState.Source;

                // For now, this data provider only cares about hands.
                if (spatialInteractionSource.Kind == SpatialInteractionSourceKind.Hand)
                {
                    // If we already have a controller created for this source, update it.
                    if (TryGetController(spatialInteractionSource, out MixedRealityHandController existingController))
                    {
                        existingController.UpdateController(CreateHandData(sourceState));
                    }
                    else
                    {
                        // Try and create a new controller if not.
                        MixedRealityHandController controller = CreateController(spatialInteractionSource);
                        if (controller != null)
                        {
                            controller.UpdateController(CreateHandData(sourceState));
                        }
                    }

                    // Update cached state for this interactino source.
                    if (cachedInteractionSourceStates.ContainsKey(spatialInteractionSource.Id))
                    {
                        cachedInteractionSourceStates[spatialInteractionSource.Id] = sourceState;
                    }
                    else
                    {
                        cachedInteractionSourceStates.Add(spatialInteractionSource.Id, sourceState);
                    }
                }
            }

            // We need to cleanup any controllers, that are not detected / tracked anymore as well.
            foreach (var controllerRegistry in controllers)
            {
                uint id = controllerRegistry.Key;
                for (int i = 0; i < sources.Count; i++)
                {
                    if (sources[i].Source.Id.Equals(id))
                    {
                        continue;
                    }

                    // This controller is not in the up-to-date sources list,
                    // so we need to remove it.
                    RemoveController(cachedInteractionSourceStates[id]);
                    cachedInteractionSourceStates.Remove(id);
                }
            }
        }

        /// <inheritdoc/>
        public override void Disable()
        {
            List<KeyValuePair<uint, MixedRealityHandController>> controllersList = controllers.ToList();
            while (controllersList.Count > 0)
            {
                RemoveController(controllersList[0].Value);
                controllersList.RemoveAt(0);
            }

            controllers.Clear();
            cachedInteractionSourceStates.Clear();
            base.Disable();
        }

        #endregion IMixedRealityControllerDataProvider lifecycle implementation

        #region Controller Management

        /// <summary>
        /// Reads currently detected input sources by the current <see cref="SpatialInteractionManager"/> instance.
        /// </summary>
        /// <returns>List of sources. Can be null.</returns>
        private IReadOnlyList<SpatialInteractionSourceState> GetCurrentSources()
        {
            // Articulated hand support is only present in the 18362 version and beyond Windows
            // SDK (which contains the V8 drop of the Universal API Contract). In particular,
            // the HandPose related APIs are only present on this version and above.
            if (WindowsApiChecker.UniversalApiContractV8_IsAvailable && SpatialInteractionManager != null)
            {
                PerceptionTimestamp perceptionTimestamp = PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now);
                IReadOnlyList<SpatialInteractionSourceState> sources = SpatialInteractionManager.GetDetectedSourcesAtTimestamp(perceptionTimestamp);

                return sources;
            }

            return null;
        }

        /// <summary>
        /// Checks whether a <see cref="MixedRealityHandController"/> has already been created and registered
        /// for a given <see cref="SpatialInteractionSource"/>.
        /// </summary>
        /// <param name="spatialInteractionSource">Input source to lookup the controller for.</param>
        /// <param name="controller">Reference to found controller, if existing.</param>
        /// <returns>True, if the controller is registered and alive.</returns>
        private bool TryGetController(SpatialInteractionSource spatialInteractionSource, out MixedRealityHandController controller)
        {
            if (controllers.ContainsKey(spatialInteractionSource.Id))
            {
                controller = controllers[spatialInteractionSource.Id];
                if (controller == null)
                {
                    Debug.LogError($"Controller {spatialInteractionSource.Id} was not properly unregistered or unexpectedly destroyed.");
                    controllers.Remove(spatialInteractionSource.Id);
                    return false;
                }

                return true;
            }

            controller = null;
            return false;
        }

        /// <summary>
        /// Creates the controller for a new device and registers it.
        /// </summary>
        /// <param name="spatialInteractionSource">Source State provided by the SDK.</param>
        /// <returns>New controller input source.</returns>
        private MixedRealityHandController CreateController(SpatialInteractionSource spatialInteractionSource)
        {
            // We are creating a new controller for the source, determine the type of controller to use.
            Type controllerType = spatialInteractionSource.Kind.ToControllerType();
            if (controllerType == null || controllerType != typeof(MixedRealityHandController))
            {
                // This data provider only cares about hands.
                return null;
            }

            // Ready to create the controller intance.
            Handedness controllingHand = spatialInteractionSource.Handedness.ToHandedness();
            IMixedRealityPointer[] pointers = spatialInteractionSource.IsPointingSupported ? RequestPointers(controllerType, controllingHand, true) : null;
            string nameModifier = controllingHand == Handedness.None ? spatialInteractionSource.Kind.ToString() : controllingHand.ToString();
            IMixedRealityInputSource inputSource = MixedRealityToolkit.InputSystem?.RequestNewGenericInputSource($"Mixed Reality Hand Controller {nameModifier}", pointers);
            MixedRealityHandController detectedController = new MixedRealityHandController(TrackingState.NotApplicable, controllingHand, inputSource, null);

            if (!detectedController.SetupConfiguration(controllerType))
            {
                // Controller failed to be setup correctly.
                // Return null so we don't raise the source detected.
                return null;
            }

            for (int i = 0; i < detectedController.InputSource?.Pointers?.Length; i++)
            {
                detectedController.InputSource.Pointers[i].Controller = detectedController;
            }

            MixedRealityToolkit.InputSystem.RaiseSourceDetected(detectedController.InputSource, detectedController);
            if (MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.RenderMotionControllers)
            {
                detectedController.TryRenderControllerModel(controllerType);
            }

            controllers.Add(spatialInteractionSource.Id, detectedController);
            return detectedController;
        }

        /// <summary>
        /// Removes the selected controller from the active store.
        /// </summary>
        /// <param name="spatialInteractionSourceState">Source State provided by the SDK to remove.</param>
        private void RemoveController(SpatialInteractionSourceState spatialInteractionSourceState)
        {
            if (TryGetController(spatialInteractionSourceState.Source, out MixedRealityHandController controller))
            {
                MixedRealityToolkit.InputSystem?.RaiseSourceLost(controller.InputSource, controller);
                controllers.Remove(spatialInteractionSourceState.Source.Id);

                if (cachedInteractionSourceStates.ContainsKey(spatialInteractionSourceState.Source.Id))
                {
                    cachedInteractionSourceStates.Remove(spatialInteractionSourceState.Source.Id);
                }
            }
        }

        #endregion Controller Management


        private HandData CreateHandData(SpatialInteractionSourceState spatialInteractionSourceState)
        {
            HandPose handPose = spatialInteractionSourceState.TryGetHandPose();

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
                        unityJointOrientations[i] = jointPoses[i].Orientation.ToUnity();
                        unityJointPositions[i] = jointPoses[i].Position.ToUnity();

                        // We want the controller to follow the Playspace, so fold in the playspace transform here to 
                        // put the controller pose into world space.
                        unityJointPositions[i] = MixedRealityToolkit.CameraSystem.CameraRig.PlayspaceTransform.TransformPoint(unityJointPositions[i]);
                        unityJointOrientations[i] = MixedRealityToolkit.CameraSystem.CameraRig.PlayspaceTransform.rotation * unityJointOrientations[i];

                        TrackedHandJoint handJoint = jointIndices[i].ToTrackedHandJoint();
                        updatedHandData.Joints[(int)handJoint] = new MixedRealityPose(unityJointPositions[i], unityJointOrientations[i]);
                    }
                }
            }

            return updatedHandData;
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