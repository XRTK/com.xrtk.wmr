// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Attributes;
using XRTK.Definitions.Platforms;
using XRTK.Interfaces.InputSystem;
using XRTK.Providers.Controllers.Hands;
using XRTK.WindowsMixedReality.Profiles;
using XRTK.Definitions.Utilities;
using XRTK.WindowsMixedReality.Utilities;

#if WINDOWS_UWP

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Windows.Perception;
using Windows.UI.Input.Spatial;
using XRTK.Definitions.Devices;
using XRTK.Services;
using XRTK.Utilities;
using XRTK.WindowsMixedReality.Extensions;

#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Providers.InputSystem.Controllers
{
    /// <summary>
    /// The Windows Mixed Reality Data Provider for hand controller support.
    /// It's responsible for converting the platform data to agnostic data the <see cref="MixedRealityHandController"/> can work with.
    /// </summary>
    [RuntimePlatform(typeof(UniversalWindowsPlatform))]
    [System.Runtime.InteropServices.Guid("F2E0D0EF-6393-4F96-90CC-DF78CA1DC8A2")]
    public class WindowsMixedRealityHandControllerDataProvider : BaseHandControllerDataProvider
    {
        /// <inheritdoc />
        public WindowsMixedRealityHandControllerDataProvider(string name, uint priority, WindowsMixedRealityHandControllerDataProviderProfile profile, IMixedRealityInputSystem parentService)
            : base(name, priority, profile, parentService)
        {
            leftHandConverter = new WindowsMixedRealityHandDataConverter(Handedness.Left, TrackedPoses);
            rightHandConverter = new WindowsMixedRealityHandDataConverter(Handedness.Right, TrackedPoses);
        }

        private readonly WindowsMixedRealityHandDataConverter leftHandConverter;
        private readonly WindowsMixedRealityHandDataConverter rightHandConverter;

#if WINDOWS_UWP

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
                        leftHandController.UpdateController(leftHandConverter.GetHandData(sourceState));
                    }
                    else
                    {
                        leftHandController = CreateController(spatialInteractionSource);
                        leftHandController.UpdateController(leftHandConverter.GetHandData(sourceState));
                    }
                }

                if (spatialInteractionSource.Handedness == SpatialInteractionSourceHandedness.Right)
                {
                    isRightHandTracked = true;

                    if (TryGetController(spatialInteractionSource.Handedness.ToHandedness(), out MixedRealityHandController rightHandController))
                    {
                        rightHandController.UpdateController(rightHandConverter.GetHandData(sourceState));
                    }
                    else
                    {
                        rightHandController = CreateController(spatialInteractionSource);
                        rightHandController.UpdateController(rightHandConverter.GetHandData(sourceState));
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
            var controllerType = spatialInteractionSource.Kind.ToControllerType();

            if (controllerType == null || controllerType != typeof(MixedRealityHandController))
            {
                // This data provider only cares about hands.
                return null;
            }

            var handedness = spatialInteractionSource.Handedness.ToHandedness();

            MixedRealityHandController detectedController;

            try
            {
                detectedController = new MixedRealityHandController(this, TrackingState.NotApplicable, handedness, GetControllerMappingProfile(typeof(MixedRealityHandController), handedness));
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to create {nameof(MixedRealityHandController)}!\n{e}");
                return null;
            }

            for (int i = 0; i < detectedController.InputSource?.Pointers?.Length; i++)
            {
                detectedController.InputSource.Pointers[i].Controller = detectedController;
            }

            MixedRealityToolkit.InputSystem?.RaiseSourceDetected(detectedController.InputSource, detectedController);

            detectedController.TryRenderControllerModel();

            AddController(detectedController);
            activeControllers.Add(handedness, detectedController);
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