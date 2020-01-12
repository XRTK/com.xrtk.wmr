// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP
using XRTK.WindowsMixedReality.Controllers;
using System;
using Windows.UI.Input.Spatial;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Extensions
{
    public static class SpatialInteractionSourceKindExtensions
    {
#if WINDOWS_UWP

        /// <summary>
        /// Maps the native <see cref="SpatialInteractionSourceKind"/> to a XRTK <see cref="Interfaces.Providers.Controllers.IWindowsMixedRealityController"/> type.
        /// </summary>
        /// <param name="spatialInteractionSourceKind">Value to map.</param>
        /// <returns>The XRTK <see cref="Interfaces.Providers.Controllers.IWindowsMixedRealityController"/> type representing the source kind.</returns>
        public static Type ToControllerType(this SpatialInteractionSourceKind spatialInteractionSourceKind)
        {
            switch (spatialInteractionSourceKind)
            {
                case SpatialInteractionSourceKind.Controller:
                    return typeof(WindowsMixedRealityController);
                case SpatialInteractionSourceKind.Hand:
                    return typeof(WindowsMixedRealityHandController);
                case SpatialInteractionSourceKind.Voice:
                case SpatialInteractionSourceKind.Other:
                default:
                    return null;
            }
        }

#endif // WINDOWS_UWP
    }
}