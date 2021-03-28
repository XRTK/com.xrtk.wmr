// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using System;
using Windows.Perception.People;
using XRTK.Definitions.Controllers.Hands;

namespace XRTK.WindowsMixedReality.Extensions
{
    /// <summary>
    /// Provides extensions for the native <see cref="HandJointKind"/> type.
    /// </summary>
    public static class HandJointKindExtensions
    {
        /// <summary>
        /// Converts a native <see cref="HandJointKind"/> to XRTK's <see cref="TrackedHandJoint"/>.
        /// </summary>
        /// <param name="handJointKind">The native <see cref="HandJointKind"/> value to convert.</param>
        /// <returns><see cref="TrackedHandJoint"/> value equivalent to <paramref name="handJointKind"/>.</returns>
        public static TrackedHandJoint ToTrackedHandJoint(this HandJointKind handJointKind)
        {
            return handJointKind switch
            {
                HandJointKind.Palm => TrackedHandJoint.Palm,
                HandJointKind.Wrist => TrackedHandJoint.Wrist,
                HandJointKind.ThumbMetacarpal => TrackedHandJoint.ThumbMetacarpal,
                HandJointKind.ThumbProximal => TrackedHandJoint.ThumbProximal,
                HandJointKind.ThumbDistal => TrackedHandJoint.ThumbDistal,
                HandJointKind.ThumbTip => TrackedHandJoint.ThumbTip,
                HandJointKind.IndexMetacarpal => TrackedHandJoint.IndexMetacarpal,
                HandJointKind.IndexProximal => TrackedHandJoint.IndexProximal,
                HandJointKind.IndexIntermediate => TrackedHandJoint.IndexIntermediate,
                HandJointKind.IndexDistal => TrackedHandJoint.IndexDistal,
                HandJointKind.IndexTip => TrackedHandJoint.IndexTip,
                HandJointKind.MiddleMetacarpal => TrackedHandJoint.MiddleMetacarpal,
                HandJointKind.MiddleProximal => TrackedHandJoint.MiddleProximal,
                HandJointKind.MiddleIntermediate => TrackedHandJoint.MiddleIntermediate,
                HandJointKind.MiddleDistal => TrackedHandJoint.MiddleDistal,
                HandJointKind.MiddleTip => TrackedHandJoint.MiddleTip,
                HandJointKind.RingMetacarpal => TrackedHandJoint.RingMetacarpal,
                HandJointKind.RingProximal => TrackedHandJoint.RingProximal,
                HandJointKind.RingIntermediate => TrackedHandJoint.RingIntermediate,
                HandJointKind.RingDistal => TrackedHandJoint.RingDistal,
                HandJointKind.RingTip => TrackedHandJoint.RingTip,
                HandJointKind.LittleMetacarpal => TrackedHandJoint.LittleMetacarpal,
                HandJointKind.LittleProximal => TrackedHandJoint.LittleProximal,
                HandJointKind.LittleIntermediate => TrackedHandJoint.LittleIntermediate,
                HandJointKind.LittleDistal => TrackedHandJoint.LittleDistal,
                HandJointKind.LittleTip => TrackedHandJoint.LittleTip,
                _ => throw new ArgumentOutOfRangeException($"{nameof(HandJointKind)}.{handJointKind} could not be mapped to {nameof(TrackedHandJoint)}")
            };
        }
    }
}

#endif // WINDOWS_UWP