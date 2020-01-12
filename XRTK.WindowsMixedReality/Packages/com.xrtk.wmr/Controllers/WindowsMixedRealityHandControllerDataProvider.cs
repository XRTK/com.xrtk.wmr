// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Providers.Controllers;
using XRTK.WindowsMixedReality.Profiles;

#if WINDOWS_UWP
using Windows.Perception;
using System.Collections.Generic;
using UnityEngine;
using XRTK.Definitions.Devices;
using XRTK.Interfaces.Providers.Controllers;
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

#if WINDOWS_UWP

        private readonly WindowsMixedRealityHandControllerDataProviderProfile profile;
        private readonly Dictionary<uint, IWindowsMixedRealityController> controllers = new Dictionary<uint, IWindowsMixedRealityController>();

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

            PerceptionTimestamp perceptionTimestamp = PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now);
            IReadOnlyList<SpatialInteractionSourceState> sources = SpatialInteractionManager?.GetDetectedSourcesAtTimestamp(perceptionTimestamp);

            for (int i = 0; i < sources.Count; i++)
            {

            }
        }

        #endregion IMixedRealityControllerDataProvider lifecycle implementation

        #region Controller Management

        /// <summary>
        /// Looks up a controller for a given ID and if it exists assigns it to the out reference.
        /// </summary>
        /// <param name="id">Controller ID to look for.</param>
        /// <param name="controller">Controller reference if found, otherwise null.</param>
        /// <returns>True, if controller found.</returns>
        private bool TryGetController(uint id, out IWindowsMixedRealityController controller)
        {
            if (controllers.ContainsKey(id))
            {
                controller = controllers[id];
                Debug.Assert(controller != null, $"A {controller.GetType().Name} was still registered with the {GetType().Name} but the instance was already destroyed!");

                return true;
            }

            controller = null;
            return false;
        }

        private IMixedRealityController GetOrAddController(SpatialInteractionInputSourceState)
        {
            if (TryGetController(handedness, out IMixedRealityController existingController))
            {
                return existingController;
            }

            IMixedRealityPointer[] pointers = RequestPointers(profile.SimulatedControllerType, handedness);
            IMixedRealityInputSource inputSource = MixedRealityToolkit.InputSystem.RequestNewGenericInputSource($"{profile.SimulatedControllerType.Type.Name} {handedness}", pointers);
            IMixedRealityController controller = (IMixedRealityController)Activator.CreateInstance(profile.SimulatedControllerType, TrackingState.Tracked, handedness, inputSource, null);

            if (controller == null || !controller.SetupConfiguration(profile.SimulatedControllerType))
            {
                // Controller failed to be setup correctly.
                // Return null so we don't raise the source detected.
                return null;
            }

            for (int i = 0; i < controller.InputSource?.Pointers?.Length; i++)
            {
                controller.InputSource.Pointers[i].Controller = controller;
            }

            MixedRealityToolkit.InputSystem.RaiseSourceDetected(controller.InputSource, controller);
            if (MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.RenderMotionControllers)
            {
                controller.TryRenderControllerModel(profile.SimulatedControllerType);
            }

            AddController(controller);
            return controller as IMixedRealityHandController;
        }

        #endregion Controller Management

#endif // WINDOWS_UWP
    }
}