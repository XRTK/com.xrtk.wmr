// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Providers.Controllers.Hands;
using XRTK.WindowsMixedReality.Profiles;

#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using UnityEngine;
using Windows.Perception;
using Windows.UI.Input.Spatial;
using XRTK.Definitions.Devices;
using XRTK.Definitions.Utilities;
using XRTK.Interfaces.InputSystem;
using XRTK.Services;
using XRTK.Utilities;
using XRTK.WindowsMixedReality.Extensions;

#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Controllers
{
    /// <summary>
    /// The Windows Mixed Reality Data Provider for hand controller support.
    /// It's responsible for converting the platform data to agnostic data the <see cref="MixedRealityHandController"/> can work with.
    /// </summary>
    public class WindowsMixedRealityHandControllerDataProvider : BaseHandDataProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the data provider as assigned in the configuration profile.</param>
        /// <param name="priority">Data provider priority controls the order in the service registry.</param>
        /// <param name="profile">Controller data provider profile assigned to the provider instance in the configuration inspector.</param>
        public WindowsMixedRealityHandControllerDataProvider(string name, uint priority, WindowsMixedRealityHandControllerDataProviderProfile profile)
            : base(name, priority, profile) { }

#if WINDOWS_UWP

        private readonly WindowsMixedRealityHandDataConverter handDataConverter = new WindowsMixedRealityHandDataConverter();
        private readonly Dictionary<uint, MixedRealityHandController> activeControllers = new Dictionary<uint, MixedRealityHandController>();

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
        public override void Initialize()
        {
            base.Initialize();

            WindowsMixedRealityHandDataConverter.HandMeshingEnabled = HandMeshingEnabled;
        }

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

                // If we already have a controller created for this source, update it.
                if (TryGetController(spatialInteractionSource.Id, out MixedRealityHandController existingController))
                {
                    existingController.UpdateController(handDataConverter.GetHandData(sourceState));
                }
                else
                {
                    // Try and create a new controller if not.
                    MixedRealityHandController controller = CreateController(spatialInteractionSource);
                    if (controller != null)
                    {
                        controller.UpdateController(handDataConverter.GetHandData(sourceState));
                    }
                }
            }

            // We need to cleanup any controllers, that are not detected / tracked anymore as well.
            List<uint> markedForRemoval = new List<uint>();
            foreach (var controllerRegistry in activeControllers)
            {
                uint registeredId = controllerRegistry.Key;
                for (int i = 0; i < sources.Count; i++)
                {
                    uint currentSourceId = sources[i].Source.Id;
                    if (currentSourceId.Equals(registeredId))
                    {
                        // Registered controller is still active.
                        continue;
                    }

                    // This controller is not in the up-to-date sources list,
                    // so we need to remove it.
                    RemoveController(registeredId, false);
                    markedForRemoval.Add(registeredId);
                }
            }

            for (int i = 0; i < markedForRemoval.Count; i++)
            {
                activeControllers.Remove(markedForRemoval[i]);
            }
        }

        /// <inheritdoc/>
        public override void Disable()
        {
            foreach (var controller in activeControllers)
            {
                RemoveController(controller.Key, false);
            }

            activeControllers.Clear();
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
        /// <param name="spatialInteractionSourceId">Input source ID to lookup the controller for.</param>
        /// <param name="controller">Reference to found controller, if existing.</param>
        /// <returns>True, if the controller is registered and alive.</returns>
        private bool TryGetController(uint spatialInteractionSourceId, out MixedRealityHandController controller)
        {
            if (activeControllers.ContainsKey(spatialInteractionSourceId))
            {
                controller = activeControllers[spatialInteractionSourceId];
                Debug.Assert(controller != null, $"Controller {spatialInteractionSourceId} was not properly unregistered or unexpectedly destroyed.");
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

            activeControllers.Add(spatialInteractionSource.Id, detectedController);
            return detectedController;
        }

        /// <summary>
        /// Removes the selected controller from the active store.
        /// </summary>
        /// <param name="spatialInteractionSourceId">ID of the input source to remove.</param>
        /// <param name="removeFromRegistry">Should the controller be removed from the <see cref="activeControllers"/>
        /// registry as well? Defaults to true.</param>
        private void RemoveController(uint spatialInteractionSourceId, bool removeFromRegistry = true)
        {
            if (TryGetController(spatialInteractionSourceId, out MixedRealityHandController controller))
            {
                MixedRealityToolkit.InputSystem?.RaiseSourceLost(controller.InputSource, controller);
                if (removeFromRegistry)
                {
                    activeControllers.Remove(spatialInteractionSourceId);
                }
            }
        }

        #endregion Controller Management

#endif // WINDOWS_UWP
    }
}