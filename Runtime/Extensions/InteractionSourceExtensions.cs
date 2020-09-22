// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if UNITY_WSA
using XRTK.WindowsMixedReality.Utilities;
using UnityEngine.XR.WSA.Input;
using Application = UnityEngine.Application;
#if WINDOWS_UWP
using System;
using System.Collections.Generic;
using Windows.Devices.Haptics;
using Windows.Perception;
using Windows.UI.Input.Spatial;
#elif UNITY_EDITOR_WIN
using System.Runtime.InteropServices;
#endif
#endif // UNITY_WSA

namespace XRTK.WindowsMixedReality.Extensions
{
    /// <summary>
    /// Extensions for the InteractionSource class to add haptics and expose the renderable model.
    /// </summary>
    public static class InteractionSourceExtensions
    {
#if UNITY_EDITOR_WIN && UNITY_WSA
        [DllImport("EditorMotionController")]
        private static extern bool StartHaptics([In] uint controllerId, [In] float intensity, [In] float durationInSeconds);

        [DllImport("EditorMotionController")]
        private static extern bool StopHaptics([In] uint controllerId);
#endif // UNITY_EDITOR_WIN && UNITY_WSA

        // This value is standardized according to www.usb.org/developers/hidpage/HUTRR63b_-_Haptics_Page_Redline.pdf
        private const ushort ContinuousBuzzWaveform = 0x1004;

#if UNITY_WSA
        public static void StartHaptics(this InteractionSource interactionSource, float intensity)
        {
            interactionSource.StartHaptics(intensity, float.MaxValue);
        }

        public static void StartHaptics(this InteractionSource interactionSource, float intensity, float durationInSeconds)
        {
            // GetForCurrentView and GetDetectedSourcesAtTimestamp were both introduced in the same Windows version.
            // We need only check for one of them.
            if ((!WindowsApiChecker.IsMethodAvailable(
                "Windows.UI.Input.Spatial",
                "SpatialInteractionManager",
                "GetForCurrentView") ||
                !WindowsApiChecker.IsTypeAvailable("Windows.Devices.Haptics", "SimpleHapticsController")) && !Application.isEditor)
            {
                return;
            }

#if WINDOWS_UWP
            UnityEngine.WSA.Application.InvokeOnUIThread(() =>
            {
                IReadOnlyList<SpatialInteractionSourceState> sources = SpatialInteractionManager.GetForCurrentView().GetDetectedSourcesAtTimestamp(PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now));

                foreach (SpatialInteractionSourceState sourceState in sources)
                {
                    if (sourceState.Source.Id.Equals(interactionSource.id))
                    {
                        SimpleHapticsController simpleHapticsController = sourceState.Source.Controller.SimpleHapticsController;
                        foreach (SimpleHapticsControllerFeedback hapticsFeedback in simpleHapticsController.SupportedFeedback)
                        {
                            if (hapticsFeedback.Waveform.Equals(ContinuousBuzzWaveform))
                            {
                                if (durationInSeconds.Equals(float.MaxValue))
                                {
                                    simpleHapticsController.SendHapticFeedback(hapticsFeedback, intensity);
                                }
                                else
                                {
                                    simpleHapticsController.SendHapticFeedbackForDuration(hapticsFeedback, intensity, TimeSpan.FromSeconds(durationInSeconds));
                                }
                                return;
                            }
                        }
                    }
                }
            }, true);
#elif UNITY_EDITOR_WIN
            StartHaptics(interactionSource.id, intensity, durationInSeconds);
#endif // WINDOWS_UWP
        }

        public static void StopHaptics(this InteractionSource interactionSource)
        {
            // GetForCurrentView and GetDetectedSourcesAtTimestamp were both introduced in the same Windows version.
            // We need only check for one of them.
            if ((!WindowsApiChecker.IsMethodAvailable(
                "Windows.UI.Input.Spatial",
                "SpatialInteractionManager",
                "GetForCurrentView") ||
                !WindowsApiChecker.IsTypeAvailable("Windows.Devices.Haptics", "SimpleHapticsController")) && !Application.isEditor)
            {
                return;
            }

#if WINDOWS_UWP
            UnityEngine.WSA.Application.InvokeOnUIThread(() =>
            {
                IReadOnlyList<SpatialInteractionSourceState> sources = SpatialInteractionManager.GetForCurrentView().GetDetectedSourcesAtTimestamp(PerceptionTimestampHelper.FromHistoricalTargetTime(DateTimeOffset.Now));

                foreach (SpatialInteractionSourceState sourceState in sources)
                {
                    if (sourceState.Source.Id.Equals(interactionSource.id))
                    {
                        sourceState.Source.Controller.SimpleHapticsController.StopFeedback();
                    }
                }
            }, true);
#elif UNITY_EDITOR_WIN
            StopHaptics(interactionSource.id);
#endif // WINDOWS_UWP
        }
#endif //UNITY_WSA
    }
}
