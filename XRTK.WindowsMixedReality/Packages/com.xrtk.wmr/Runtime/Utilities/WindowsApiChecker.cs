// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using System;
using Windows.Foundation.Metadata;

namespace XRTK.WindowsMixedReality.Utilities
{
    /// <summary>
    /// Helper class for determining if a Windows API contract is available.
    /// </summary>
    /// <remarks>
    /// See https://docs.microsoft.com/uwp/extension-sdks/windows-universal-sdk for a full list of contracts.
    /// </remarks>
    public static class WindowsApiChecker
    {
        /// <summary>
        /// Checks to see if the requested method is present on the current platform.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the class to check.</param>
        /// <param name="methodName">The name of the method (ex: "IsSourceKindSupported").</param>
        /// <returns>True if the method is available and can be called. Otherwise, false.</returns>
        public static bool IsMethodAvailable(Type type, string methodName)
        {
            if (type == null || string.IsNullOrWhiteSpace(type.FullName)) { throw new ArgumentException(); }
            if (string.IsNullOrWhiteSpace(methodName)) { throw new ArgumentException(); }
            return ApiInformation.IsMethodPresent(type.FullName, methodName);
        }

        /// <summary>
        /// Checks to see if the requested property is present on the current platform.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the class to check.</param>
        /// <param name="propertyName">The name of the method (ex: "Eyes").</param>
        /// <returns>True if the property is available and can be called. Otherwise, false.</returns>
        public static bool IsPropertyAvailable(Type type, string propertyName)
        {
            if (type == null || string.IsNullOrWhiteSpace(type.FullName)) { throw new ArgumentException(); }
            if (string.IsNullOrWhiteSpace(propertyName)) { throw new ArgumentException(); }
            return ApiInformation.IsPropertyPresent(type.FullName, propertyName);
        }

        /// <summary>
        /// Checks to see if the requested type is present on the current platform.
        /// </summary>
        /// <param name="type">The <see cref="Type"/> of the class to check.</param>
        /// <returns>True if the type is available and can be called. Otherwise, false.</returns>
        public static bool IsTypeAvailable(Type type)
        {
            if (type == null || string.IsNullOrWhiteSpace(type.FullName)) { throw new ArgumentException(); }
            return ApiInformation.IsTypePresent(type.FullName);
        }
    }
}

#endif // WINDOWS_UWP
