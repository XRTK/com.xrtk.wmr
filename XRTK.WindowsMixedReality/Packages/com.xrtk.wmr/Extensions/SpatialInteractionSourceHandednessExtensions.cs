// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP
using XRTK.Definitions.Utilities;
using Windows.UI.Input.Spatial;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Extensions
{
    public static class SpatialInteractionSourceHandednessExtensions
    {
#if WINDOWS_UWP

        /// <summary>
        /// Converts the native <see cref="SpatialInteractionSourceHandedness"/> to a XRTK <see cref="Handedness"/>.
        /// </summary>
        /// <param name="spatialInteractionSourceHandedness">Value to convert.</param>
        /// <returns>The XRTK <see cref="Handedness"/> value.</returns>
        public static Handedness ToHandedness(this SpatialInteractionSourceHandedness spatialInteractionSourceHandedness)
        {
            switch (spatialInteractionSourceHandedness)
            {
                case SpatialInteractionSourceHandedness.Left:
                    return Handedness.Left;
                case SpatialInteractionSourceHandedness.Right:
                    return Handedness.Right;
                default:
                    return Handedness.None;
            }
        }

#endif // WINDOWS_UWP
    }
}