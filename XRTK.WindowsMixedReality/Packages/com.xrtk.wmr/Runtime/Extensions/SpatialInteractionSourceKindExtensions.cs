﻿// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using System;
using Windows.UI.Input.Spatial;
using XRTK.Providers.Controllers.Hands;
using XRTK.WindowsMixedReality.Providers.Controllers;

namespace XRTK.WindowsMixedReality.Extensions
{
    /// <summary>
    /// Provides convenience extension for converting native <see cref="SpatialInteractionSourceKind"/> to
    /// XRTK's <see cref="Interfaces.Providers.Controllers.IMixedRealityController"/> type.
    /// </summary>
    public static class SpatialInteractionSourceKindExtensions
    {
        /// <summary>
        /// Maps the native <see cref="SpatialInteractionSourceKind"/> to a XRTK <see cref="Interfaces.Providers.Controllers.IMixedRealityController"/> type.
        /// </summary>
        /// <param name="spatialInteractionSourceKind">Value to map.</param>
        /// <returns>The XRTK <see cref="Interfaces.Providers.Controllers.IMixedRealityController"/> type representing the source kind.</returns>
        public static Type ToControllerType(this SpatialInteractionSourceKind spatialInteractionSourceKind)
        {
            return spatialInteractionSourceKind switch
            {
                SpatialInteractionSourceKind.Controller => typeof(WindowsMixedRealityMotionController),
                SpatialInteractionSourceKind.Other => null,
                SpatialInteractionSourceKind.Hand => typeof(MixedRealityHandController),
                SpatialInteractionSourceKind.Voice => null,
                _ => throw new ArgumentOutOfRangeException($"{nameof(SpatialInteractionSourceKind)}.{spatialInteractionSourceKind} could not be mapped to {nameof(Interfaces.Providers.Controllers.IMixedRealityController)}")
            };
        }

    }
}

#endif // WINDOWS_UWP
