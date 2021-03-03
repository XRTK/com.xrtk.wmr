// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using Windows.UI.Input.Spatial;
using XRTK.Definitions.Utilities;

namespace XRTK.WindowsMixedReality.Extensions
{
    public static class SpatialInteractionSourceHandednessExtensions
    {
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

    }
}

#endif // WINDOWS_UWP