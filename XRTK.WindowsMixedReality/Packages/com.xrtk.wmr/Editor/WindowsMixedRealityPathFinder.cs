// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using UnityEngine;
using XRTK.Utilities.Editor;

namespace XRTK.WindowsMixedReality.Editor
{
    /// <summary>
    /// Dummy scriptable object used to find the relative path of the com.xrtk.wmr.
    /// </summary>
    /// <inheritdoc cref="IPathFinder" />
    public class WindowsMixedRealityPathFinder : ScriptableObject, IPathFinder
    {
        /// <inheritdoc />
        public string Location => $"/Editor/{nameof(WindowsMixedRealityPathFinder)}.cs";
    }
}