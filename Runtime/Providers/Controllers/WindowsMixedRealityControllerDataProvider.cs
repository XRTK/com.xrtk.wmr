// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Attributes;
using XRTK.Definitions.Platforms;
using XRTK.Interfaces.InputSystem;
using XRTK.Providers.Controllers.Hands;
using XRTK.WindowsMixedReality.Profiles;

#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using UnityEngine;
using Windows.Perception;
using Windows.UI.Input.Spatial;
using XRTK.Definitions.Controllers.Hands;
using XRTK.Definitions.InputSystem;
using XRTK.Definitions.Devices;
using XRTK.Providers.Controllers;
using XRTK.Services;
using XRTK.WindowsMixedReality.Extensions;
using XRTK.WindowsMixedReality.Utilities;

#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Providers.Controllers
{
    /// <summary>
    /// This data provider feeds controller data for Windows Mixed Reality controllers to the <see cref="IMixedRealityInputSystem"/>
    /// and manages <see cref="Interfaces.Providers.Controllers.IMixedRealityController"/> instances as needed.
    /// </summary>
    [RuntimePlatform(typeof(UniversalWindowsPlatform))]
    [System.Runtime.InteropServices.Guid("12E02EF8-4177-46AB-BC50-19AF7148BD4A")]
    public class WindowsMixedRealityControllerDataProvider : BaseHandControllerDataProvider
    {
        /// <inheritdoc />
        public WindowsMixedRealityControllerDataProvider(string name, uint priority,
            WindowsMixedRealityControllerDataProviderProfile profile, IMixedRealityInputSystem parentService)
            : base(name, priority, profile, parentService)
        {
#if WINDOWS_UWP
            if (!MixedRealityToolkit.TryGetSystemProfile<IMixedRealityInputSystem, MixedRealityInputSystemProfile>(out var inputSystemProfile))
            {
                throw new ArgumentException($"Unable to get a valid {nameof(MixedRealityInputSystemProfile)}!");
            }

            var isGrippingThreshold = profile.GripThreshold != inputSystemProfile.GripThreshold
                ? profile.GripThreshold
                : inputSystemProfile.GripThreshold;

            postProcessor = new HandDataPostProcessor(TrackedPoses, isGrippingThreshold)
            {
                PlatformProvidesPointerPose = true
            };
        }

        private readonly HandDataPostProcessor postProcessor;

        private readonly Dictionary<uint, BaseController> activeControllers = new Dictionary<uint, BaseController>();
        private WindowsMixedRealityHandDataConverter handDataProvider;
        private readonly List<uint> preservedSpatialInteractionSourceIdCache = new List<uint>();
        private readonly List<uint> untrackedSpatialInteractionSourceIdCache = new List<uint>();

        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();

            if (Application.isPlaying)
            {
                handDataProvider = new WindowsMixedRealityHandDataConverter(WindowsMixedRealityUtilities.SpatialCoordinateSystem);
            }
        }

        /// <inheritdoc/>
        public override void Update()
        {
            base.Update();

            var sources = GetDetectedSources();
            for (var i = 0; i < sources.Count; i++)
            {
                var sourceState = sources[i];
                var spatialInteractionSource = sourceState.Source;

                if (!TryGetController(spatialInteractionSource.Id, out BaseController controller))
                {
                    controller = CreateController(spatialInteractionSource);
                }

                if (controller != null && controller is MixedRealityHandController)
                {
                    if (handDataProvider.TryGetHandData(sourceState, RenderingMode == HandRenderingMode.Mesh, out var handData))
                    {
                        ((MixedRealityHandController)controller).UpdateController(postProcessor.PostProcess(spatialInteractionSource.Handedness.ToHandedness(), handData));
                        preservedSpatialInteractionSourceIdCache.Add(spatialInteractionSource.Id);
                    }
                }
                else if (controller != null)
                {
                    ((WindowsMixedRealityMotionController)controller).UpdateController(sourceState);
                    preservedSpatialInteractionSourceIdCache.Add(spatialInteractionSource.Id);
                }
            }

            RemoveUntrackedControllers();
            preservedSpatialInteractionSourceIdCache.Clear();
        }

        /// <inheritdoc/>
        public override void Disable()
        {
            foreach (var activeController in activeControllers)
            {
                RemoveController(activeController.Key, false);
            }

            activeControllers.Clear();
            preservedSpatialInteractionSourceIdCache.Clear();
            untrackedSpatialInteractionSourceIdCache.Clear();

            base.Disable();
        }

        #region Controller Management

        /// <summary>
        /// Attempts to retrieve a <see cref="BaseController"/> for the provided <see cref="SpatialInteractionSource"/> ID from the active controllers storage.
        /// </summary>
        /// <param name="spatialInteractionSourceId">Source ID as provided by the SDK.</param>
        /// <param name="controller">Controller reference from active store.</param>
        /// <returns>True, if controller was found and retrieved.</returns>
        private bool TryGetController(uint spatialInteractionSourceId, out BaseController controller)
        {
            if (activeControllers.ContainsKey(spatialInteractionSourceId))
            {
                controller = activeControllers[spatialInteractionSourceId];
                Debug.Assert(controller != null, $"Controller {spatialInteractionSourceId} has been destroyed but was not removed from the active controller registry. This should not happen!");

                return true;
            }

            controller = null;
            return false;
        }

        /// <summary>
        /// Creates a new <see cref="BaseController"/> instance for the provided <see cref="SpatialInteractionSource"/>.
        /// </summary>
        /// <param name="spatialInteractionSource">Source provided by the SDK.</param>
        /// <returns>The created controller instance ready for use.</returns>
        private BaseController CreateController(SpatialInteractionSource spatialInteractionSource)
        {
            var handedness = spatialInteractionSource.Handedness.ToHandedness();
            var controllerType = spatialInteractionSource.Kind.ToControllerType();

            try
            {
                BaseController controller = null;
                if (controllerType == typeof(WindowsMixedRealityMotionController))
                {
                    controller = new WindowsMixedRealityMotionController(this, TrackingState.NotApplicable, handedness, GetControllerMappingProfile(controllerType, handedness));
                }
                else if (controllerType == typeof(WindowsMixedRealityHololensOneController))
                {
                    controller = new WindowsMixedRealityHololensOneController(this, TrackingState.NotApplicable, handedness, GetControllerMappingProfile(controllerType, handedness));
                }
                else if (controllerType == typeof(MixedRealityHandController))
                {
                    controller = new MixedRealityHandController(this, TrackingState.NotApplicable, handedness, GetControllerMappingProfile(controllerType, handedness));
                }
                else
                {
                    return null;
                }

                InputSystem?.RaiseSourceDetected(controller.InputSource, controller);
                controller.TryRenderControllerModel();
                activeControllers.Add(spatialInteractionSource.Id, controller);
                AddController(controller);

                return controller;
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create {nameof(controllerType)}!\n{e}");
                return null;
            }
        }

        /// <summary>
        /// Removes a controller from the active store.
        /// </summary>
        /// <param name="spatialInteractionSourceStateId">Source ID as provided by the SDK.</param>
        /// <param name="removeFromRegistry">Should the controller be removed from the registry?</param>
        private void RemoveController(uint spatialInteractionSourceStateId, bool removeFromRegistry = true)
        {
            if (TryGetController(spatialInteractionSourceStateId, out var controller))
            {
                InputSystem?.RaiseSourceLost(controller.InputSource, controller);

                if (removeFromRegistry)
                {
                    RemoveController(controller);
                    activeControllers.Remove(spatialInteractionSourceStateId);
                }
            }
        }

        /// <summary>
        /// Removes any controllers from the active storage that are not listed in <see cref="preservedSpatialInteractionSourceIdCache"/>.
        /// </summary>
        private void RemoveUntrackedControllers()
        {
            untrackedSpatialInteractionSourceIdCache.Clear();
            foreach (var controller in activeControllers)
            {
                if (preservedSpatialInteractionSourceIdCache.Contains(controller.Key))
                {
                    continue;
                }

                untrackedSpatialInteractionSourceIdCache.Add(controller.Key);
            }

            for (var i = 0; i < untrackedSpatialInteractionSourceIdCache.Count; i++)
            {
                RemoveController(untrackedSpatialInteractionSourceIdCache[i]);
            }
        }

        /// <summary>
        /// Gets currently detected <see cref="SpatialInteractionSource"/>s. This list may
        /// differ from the data provider's internal <see cref="activeControllers"/> registry.
        /// </summary>
        /// <returns>List of tracked <see cref="SpatialInteractionSource"/>s.</returns>
        private IReadOnlyList<SpatialInteractionSourceState> GetDetectedSources()
        {
            if (WindowsMixedRealityUtilities.SpatialInteractionManager == null)
            {
                return new List<SpatialInteractionSourceState>();
            }

            var perceptionTimestamp = PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now);
            return WindowsMixedRealityUtilities.SpatialInteractionManager.GetDetectedSourcesAtTimestamp(perceptionTimestamp);
        }

        #endregion Controller Management
#else // WINDOWS_UWP
        }
#endif
    }
}
