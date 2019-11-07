// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Definitions.Controllers;
using XRTK.Providers.Controllers.Hands;

namespace XRTK.WindowsMixedReality.Controllers
{
    public class WindowsMixedRealityHandControllerDataProvider : BaseHandControllerDataProvider
    {
        public WindowsMixedRealityHandControllerDataProvider(string name, uint priority, BaseMixedRealityControllerDataProviderProfile profile)
            : base(name, priority, profile) { }
    }
}