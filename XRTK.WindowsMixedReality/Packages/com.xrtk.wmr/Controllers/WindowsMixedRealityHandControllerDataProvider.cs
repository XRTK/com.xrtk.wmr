// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Providers.Controllers.Hands;
using XRTK.WindowsMixedReality.Profiles;
using System.Linq;

#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using UnityEngine;
using Windows.Perception;
using Windows.UI.Input.Spatial;
using XRTK.Definitions.Devices;
using XRTK.Definitions.Utilities;
using XRTK.Services;
using XRTK.Utilities;
using XRTK.WindowsMixedReality.Extensions;
using XRTK.WindowsMixedReality.Utilities;

#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Controllers
{
    /// <summary>
    /// The Windows Mixed Reality Data Provider for hand controller support.
    /// It's responsible for converting the platform data to agnostic data the <see cref="MixedRealityHandController"/> can work with.
    /// </summary>
    public class WindowsMixedRealityHandControllerDataProvider : BaseHandControllerDataProvider
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="name">Name of the data provider as assigned in the configuration profile.</param>
        /// <param name="priority">Data provider priority controls the order in the service registry.</param>
        /// <param name="profile">Controller data provider profile assigned to the provider instance in the configuration inspector.</param>
        public WindowsMixedRealityHandControllerDataProvider(string name, uint priority, WindowsMixedRealityHandControllerDataProviderProfile profile)
            : base(name, priority, profile)
        { }

#if WINDOWS_UWP

        private readonly WindowsMixedRealityHandDataConverter handDataConverter = new WindowsMixedRealityHandDataConverter();
        private readonly Dictionary<Handedness, MixedRealityHandController> activeControllers = new Dictionary<Handedness, MixedRealityHandController>();

        private SpatialInteractionManager spatialInteractionManager = null;

        /// <summary>
        /// Gets the native <see cref="Windows.UI.Input.Spatial.SpatialInteractionManager"/> instance for the current application
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
            var sources = GetCurrentSources();

            if (sources == null)
            {
                return;
            }

            bool isLeftHandTracked = false;
            bool isRightHandTracked = false;

            for (int i = 0; i < sources.Count; i++)
            {
                var sourceState = sources[i];
                var spatialInteractionSource = sourceState.Source;

                if (spatialInteractionSource.Handedness == SpatialInteractionSourceHandedness.Left)
                {
                    isLeftHandTracked = true;

                    if (TryGetController(spatialInteractionSource.Handedness.ToHandedness(), out MixedRealityHandController leftHandController))
                    {
                        leftHandController.UpdateController(handDataConverter.GetHandData(sourceState));
                    }
                    else
                    {
                        leftHandController = CreateController(spatialInteractionSource);
                        leftHandController.UpdateController(handDataConverter.GetHandData(sourceState));
                    }
                }

                if (spatialInteractionSource.Handedness == SpatialInteractionSourceHandedness.Right)
                {
                    isRightHandTracked = true;

                    if (TryGetController(spatialInteractionSource.Handedness.ToHandedness(), out MixedRealityHandController rightHandController))
                    {
                        rightHandController.UpdateController(handDataConverter.GetHandData(sourceState));
                    }
                    else
                    {
                        rightHandController = CreateController(spatialInteractionSource);
                        rightHandController.UpdateController(handDataConverter.GetHandData(sourceState));
                    }
                }
            }

            if (!isLeftHandTracked)
            {
                RemoveController(Handedness.Left);
            }

            if (!isRightHandTracked)
            {
                RemoveController(Handedness.Right);
            }
        }

        /// <inheritdoc/>
        public override void Disable()
        {
            foreach (var activeController in activeControllers)
            {
                RemoveController(activeController.Key, false);
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
                var perceptionTimestamp = PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now);
                var sources = SpatialInteractionManager.GetDetectedSourcesAtTimestamp(perceptionTimestamp);

                if (sources != null)
                {
                    return sources.Where(s => s.Source.Kind == SpatialInteractionSourceKind.Hand).ToList();
                }
            }

            return null;
        }

        private bool TryGetController(Handedness handedness, out MixedRealityHandController controller)
        {
            if (activeControllers.ContainsKey(handedness))
            {
                var existingController = activeControllers[handedness];
                Debug.Assert(existingController != null, $"Hand Controller {handedness} has been destroyed but remains in the active controller registry.");
                controller = existingController;
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

            // Ready to create the controller instance.
            var controllingHand = spatialInteractionSource.Handedness.ToHandedness();
            var pointers = spatialInteractionSource.IsPointingSupported ? RequestPointers(controllerType, controllingHand, true) : null;
            var nameModifier = controllingHand == Handedness.None ? spatialInteractionSource.Kind.ToString() : controllingHand.ToString();
            var inputSource = MixedRealityToolkit.InputSystem?.RequestNewGenericInputSource($"Mixed Reality Hand Controller {nameModifier}", pointers);
            var detectedController = new MixedRealityHandController(this, TrackingState.NotApplicable, controllingHand, inputSource);

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

            MixedRealityToolkit.InputSystem?.RaiseSourceDetected(detectedController.InputSource, detectedController);

            if (MixedRealityToolkit.Instance.ActiveProfile.InputSystemProfile.ControllerVisualizationProfile.RenderMotionControllers)
            {
                detectedController.TryRenderControllerModel(controllerType);
            }

            AddController(detectedController);
            activeControllers.Add(controllingHand, detectedController);
            return detectedController;
        }

        private void RemoveController(Handedness handedness, bool removeFromRegistry = true)
        {
            if (TryGetController(handedness, out var controller))
            {
                MixedRealityToolkit.InputSystem?.RaiseSourceLost(controller.InputSource, controller);

                if (removeFromRegistry)
                {
                    RemoveController(controller);
                    activeControllers.Remove(handedness);
                }
            }
        }

        #endregion Controller Management

#endif // WINDOWS_UWP
    }
}