// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Interfaces.Providers.Controllers;

#if WINDOWS_UWP
using Windows.UI.Input.Spatial;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Interfaces.Providers.Controllers
{
    /// <summary>
    /// Windows Mixed Reality extensions to the core <see cref="IMixedRealityController"/> interface.
    /// </summary>
    public interface IWindowsMixedRealityController : IMixedRealityController
    {
#if WINDOWS_UWP

        /// <summary>
        /// Update the controller data from the provided platform state.
        /// </summary>
        /// <param name="spatialInteractionSourceState">The <see cref="SpatialInteractionSourceState"/> retrieved from the platform.</param>
        void UpdateController(SpatialInteractionSourceState spatialInteractionSourceState);

#endif
    }
}
