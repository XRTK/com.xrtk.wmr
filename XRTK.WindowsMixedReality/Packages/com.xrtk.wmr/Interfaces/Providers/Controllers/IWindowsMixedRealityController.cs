// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Interfaces.Providers.Controllers;

#if UNITY_WSA
using UnityEngine.XR.WSA.Input;
#endif

namespace XRTK.WindowsMixedReality.Interfaces.Providers.Controllers
{
    /// <summary>
    /// Windows Mixed Reality extensions to the core <see cref="IMixedRealityController"/> interface.
    /// </summary>
    public interface IWindowsMixedRealityController : IMixedRealityController
    {
#if UNITY_WSA

        /// <summary>
        /// Update the controller data from the provided platform state.
        /// </summary>
        /// <param name="interactionSourceState">The InteractionSourceState retrieved from the platform.</param>
        void UpdateController(InteractionSourceState interactionSourceState);

#endif
    }
}
