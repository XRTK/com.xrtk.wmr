// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XRTK.Definitions.Controllers.Hands;
using XRTK.Providers.Controllers.Hands;

namespace XRTK.WindowsMixedReality.Controllers
{
    public class WindowsMixedRealityHandControllerDataProvider : BaseHandControllerDataProvider
    {
        public WindowsMixedRealityHandControllerDataProvider(string name, uint priority, HandControllerDataProviderProfile profile)
            : base(name, priority, profile) { }
    }
}