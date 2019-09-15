// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Definitions.Controllers;
using XRTK.Interfaces.Providers.Controllers;
using XRTK.Providers.Controllers;

namespace XRTK.WindowsMixedReality.Controllers
{
    public class WindowsMixedRealityHandControllerDataProvider : BaseControllerDataProvider, IMixedRealityPlatformHandControllerDataProvider
    {
        /// <inheritdoc />
        public event HandDataUpdate OnHandDataUpdate;

        public WindowsMixedRealityHandControllerDataProvider(string name, uint priority, BaseMixedRealityControllerDataProviderProfile profile)
            : base(name, priority, profile)
        {
        }
    }
}