// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Providers.Controllers;
using XRTK.WindowsMixedReality.Profiles;

#if WINDOWS_UWP
using XRTK.WindowsMixedReality.Extensions;
using System.Linq;
using XRTK.Interfaces.InputSystem;
using XRTK.Definitions.Utilities;
using XRTK.Utilities;
using Windows.Perception;
using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Devices;
using XRTK.Services;
using System;
using Windows.UI.Input.Spatial;
using XRTK.WindowsMixedReality.Interfaces.Providers.Controllers;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Controllers
{
    /// <summary>
    /// The device manager for Windows Mixed Reality hand controllers.
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

        private readonly WindowsMixedRealityHandControllerDataProviderProfile profile;

#if WINDOWS_UWP

        /// <summary>
        /// Dictionary of currently registered controllers with the data provider.
        /// </summary>
        private readonly Dictionary<uint, IWindowsMixedRealityController> controllers = new Dictionary<uint, IWindowsMixedRealityController>();

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

            Debug.Log($"Detected {sources.Count} input sources");
            for (int i = 0; i < sources.Count; i++)
            {
                SpatialInteractionSourceState sourceState = sources[i];
                SpatialInteractionSource spatialInteractionSource = sourceState.Source;

                // For now, this data provider only cares about hands.
                if (spatialInteractionSource.Kind == SpatialInteractionSourceKind.Hand)
                {
                    // If we already have a controller created for this source, update it.
                    if (TryGetController(spatialInteractionSource, out IWindowsMixedRealityController existingController))
                    {
                        existingController.UpdateController(sourceState);
                    }
                    else
                    {
                        // Try and create a new controller if not.
                        IWindowsMixedRealityController controller = CreateController(spatialInteractionSource);
                        if (controller != null)
                        {
                            controller.UpdateController(sourceState);
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
            while (controllers.Count > 0)
            {
                RemoveController(cachedInteractionSourceStates.ElementAt(0).Value);
            }

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
        /// Checks whether a <see cref="IWindowsMixedRealityController"/> has already been created and registered
        /// for a given <see cref="SpatialInteractionSource"/>.
        /// </summary>
        /// <param name="spatialInteractionSource">Input source to lookup the controller for.</param>
        /// <param name="controller">Reference to found controller, if existing.</param>
        /// <returns>True, if the controller is registered and alive.</returns>
        private bool TryGetController(SpatialInteractionSource spatialInteractionSource, out IWindowsMixedRealityController controller)
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
        private IWindowsMixedRealityController CreateController(SpatialInteractionSource spatialInteractionSource)
        {
            // We are creating a new controller for the source, determine the type of controller to use.
            Type controllerType = spatialInteractionSource.Kind.ToControllerType();
            if (controllerType == null)
            {
                Debug.LogError($"Windows Mixed Reality controller type {spatialInteractionSource.Kind} not supported.");
                return null;
            }

            // Ready to create the controller intance.
            Handedness controllingHand = spatialInteractionSource.Handedness.ToHandedness();
            IMixedRealityPointer[] pointers = spatialInteractionSource.IsPointingSupported ? RequestPointers(controllerType, controllingHand) : null;
            string nameModifier = controllingHand == Handedness.None ? spatialInteractionSource.Kind.ToString() : controllingHand.ToString();
            IMixedRealityInputSource inputSource = MixedRealityToolkit.InputSystem?.RequestNewGenericInputSource($"Mixed Reality Controller {nameModifier}", pointers);
            IWindowsMixedRealityController detectedController = Activator.CreateInstance(controllerType, TrackingState.NotApplicable, controllingHand, inputSource, null) as IWindowsMixedRealityController;

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
            if (TryGetController(spatialInteractionSourceState.Source, out IWindowsMixedRealityController controller))
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

#endif // WINDOWS_UWP
    }
}