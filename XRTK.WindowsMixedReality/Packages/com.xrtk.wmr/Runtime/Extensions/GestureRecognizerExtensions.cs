// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XRTK.WindowsMixedReality.Extensions
{
    public static class GestureRecognizerExtensions
    {
#if !UNITY_2020_1_OR_NEWER
        public static void UpdateAndResetGestures(this UnityEngine.XR.WSA.Input.GestureRecognizer recognizer, UnityEngine.XR.WSA.Input.GestureSettings gestureSettings)
        {
            bool reset = recognizer.IsCapturingGestures();

            if (reset)
            {
                recognizer.CancelGestures();
            }

            recognizer.SetRecognizableGestures(gestureSettings);

            if (reset)
            {
                recognizer.StartCapturingGestures();
            }
        }
#endif
    }
}