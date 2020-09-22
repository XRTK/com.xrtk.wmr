// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP
using Windows.Foundation.Metadata;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Utilities
{
    /// <summary>
    /// Helper class for determining if a Windows API contract is available.
    /// </summary>
    /// <remarks> See https://docs.microsoft.com/uwp/extension-sdks/windows-universal-sdk
    /// for a full list of contracts.</remarks>
    public static class WindowsApiChecker
    {
        /// <summary>
        /// Checks to see if the requested method is present on the current platform.
        /// </summary>
        /// <param name="namespaceName">The namespace (ex: "Windows.UI.Input.Spatial") containing the class.</param>
        /// <param name="className">The name of the class containing the method (ex: "SpatialInteractionMananger").</param>
        /// <param name="methodName">The name of the method (ex: "IsSourceKindSupported").</param>
        /// <returns>True if the method is available and can be called. Otherwise, false.</returns>
        public static bool IsMethodAvailable(
            string namespaceName,
            string className,
            string methodName)
        {
#if WINDOWS_UWP
            return ApiInformation.IsMethodPresent($"{namespaceName}.{className}", methodName);
#else
            return false;
#endif // WINDOWS_UWP
        }

        /// <summary>
        /// Checks to see if the requested property is present on the current platform.
        /// </summary>
        /// <param name="namespaceName">The namespace (ex: "Windows.UI.Input.Spatial") containing the class.</param>
        /// <param name="className">The name of the class containing the method (ex: "SpatialPointerPose").</param>
        /// <param name="propertyName">The name of the method (ex: "Eyes").</param>
        /// <returns>True if the property is available and can be called. Otherwise, false.</returns>
        public static bool IsPropertyAvailable(
            string namespaceName,
            string className,
            string propertyName)
        {
#if WINDOWS_UWP
            return ApiInformation.IsPropertyPresent($"{namespaceName}.{className}", propertyName);
#else
            return false;
#endif // WINDOWS_UWP
        }

        /// <summary>
        /// Checks to see if the requested type is present on the current platform.
        /// </summary>
        /// <param name="namespaceName">The namespace (ex: "Windows.UI.Input.Spatial") containing the class.</param>
        /// <param name="typeName">The name of the class containing the method (ex: "SpatialPointerPose").</param>
        /// <returns>True if the type is available and can be called. Otherwise, false.</returns>
        public static bool IsTypeAvailable(
            string namespaceName,
            string typeName)
        {
#if WINDOWS_UWP
            return ApiInformation.IsTypePresent($"{namespaceName}.{typeName}");
#else
            return false;
#endif // WINDOWS_UWP
        }
    }
}
