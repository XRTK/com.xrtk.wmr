// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using System;
using Windows.UI.Input.Spatial;
using XRTK.Providers.Controllers.Hands;
using XRTK.WindowsMixedReality.Providers.Controllers;

namespace XRTK.WindowsMixedReality.Extensions
{
    [Obsolete]
    public static class SpatialInteractionSourceKindExtensions
    {
        /// <summary>
        /// Maps the native <see cref="SpatialInteractionSourceKind"/> to a XRTK <see cref="Interfaces.Providers.Controllers.IMixedRealityController"/> type.
        /// </summary>
        /// <param name="spatialInteractionSourceKind">Value to map.</param>
        /// <returns>The XRTK <see cref="Interfaces.Providers.Controllers.IMixedRealityController"/> type representing the source kind.</returns>
        public static Type ToControllerType(this SpatialInteractionSourceKind spatialInteractionSourceKind)
        {
            switch (spatialInteractionSourceKind)
            {
                case SpatialInteractionSourceKind.Controller:
                    return typeof(WindowsMixedRealityMotionController);
                case SpatialInteractionSourceKind.Hand:
                    return typeof(MixedRealityHandController);
                default:
                    return null;
            }
        }

    }
}
#endif // WINDOWS_UWP
