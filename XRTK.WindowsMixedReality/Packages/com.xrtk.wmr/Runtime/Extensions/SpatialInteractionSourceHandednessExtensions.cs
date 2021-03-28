// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using System;
using Windows.UI.Input.Spatial;
using XRTK.Definitions.Utilities;

namespace XRTK.WindowsMixedReality.Extensions
{
    /// <summary>
    /// Provides extensions for the native <see cref="SpatialInteractionSourceHandedness"/> type.
    /// </summary>
    public static class SpatialInteractionSourceHandednessExtensions
    {
        /// <summary>
        /// Converts the native <see cref="SpatialInteractionSourceHandedness"/> to a XRTK <see cref="Handedness"/>.
        /// </summary>
        /// <param name="spatialInteractionSourceHandedness">Value to convert.</param>
        /// <returns>The XRTK <see cref="Handedness"/> value.</returns>
        public static Handedness ToHandedness(this SpatialInteractionSourceHandedness spatialInteractionSourceHandedness)
        {
            return spatialInteractionSourceHandedness switch
            {
                SpatialInteractionSourceHandedness.Left => Handedness.Left,
                SpatialInteractionSourceHandedness.Right => Handedness.Right,
                SpatialInteractionSourceHandedness.Unspecified => Handedness.Other,
                _ => throw new ArgumentOutOfRangeException($"{nameof(SpatialInteractionSourceHandedness)}.{spatialInteractionSourceHandedness} could not be mapped to {nameof(Handedness)}")
            };
        }

    }
}

#endif // WINDOWS_UWP