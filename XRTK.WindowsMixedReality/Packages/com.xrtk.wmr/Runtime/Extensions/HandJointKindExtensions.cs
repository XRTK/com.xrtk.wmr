// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP

using System;
using Windows.Perception.People;
using XRTK.Definitions.Controllers.Hands;

namespace XRTK.WindowsMixedReality.Extensions
{
    public static class HandJointKindExtensions
    {
        public static TrackedHandJoint ToTrackedHandJoint(this HandJointKind handJointKind)
        {
            switch (handJointKind)
            {
                case HandJointKind.Palm: return TrackedHandJoint.Palm;

                case HandJointKind.Wrist: return TrackedHandJoint.Wrist;

                case HandJointKind.ThumbMetacarpal: return TrackedHandJoint.ThumbMetacarpal;
                case HandJointKind.ThumbProximal: return TrackedHandJoint.ThumbProximal;
                case HandJointKind.ThumbDistal: return TrackedHandJoint.ThumbDistal;
                case HandJointKind.ThumbTip: return TrackedHandJoint.ThumbTip;

                case HandJointKind.IndexMetacarpal: return TrackedHandJoint.IndexMetacarpal;
                case HandJointKind.IndexProximal: return TrackedHandJoint.IndexProximal;
                case HandJointKind.IndexIntermediate: return TrackedHandJoint.IndexIntermediate;
                case HandJointKind.IndexDistal: return TrackedHandJoint.IndexDistal;
                case HandJointKind.IndexTip: return TrackedHandJoint.IndexTip;

                case HandJointKind.MiddleMetacarpal: return TrackedHandJoint.MiddleMetacarpal;
                case HandJointKind.MiddleProximal: return TrackedHandJoint.MiddleProximal;
                case HandJointKind.MiddleIntermediate: return TrackedHandJoint.MiddleIntermediate;
                case HandJointKind.MiddleDistal: return TrackedHandJoint.MiddleDistal;
                case HandJointKind.MiddleTip: return TrackedHandJoint.MiddleTip;

                case HandJointKind.RingMetacarpal: return TrackedHandJoint.RingMetacarpal;
                case HandJointKind.RingProximal: return TrackedHandJoint.RingProximal;
                case HandJointKind.RingIntermediate: return TrackedHandJoint.RingIntermediate;
                case HandJointKind.RingDistal: return TrackedHandJoint.RingDistal;
                case HandJointKind.RingTip: return TrackedHandJoint.RingTip;

                case HandJointKind.LittleMetacarpal: return TrackedHandJoint.LittleMetacarpal;
                case HandJointKind.LittleProximal: return TrackedHandJoint.LittleProximal;
                case HandJointKind.LittleIntermediate: return TrackedHandJoint.LittleIntermediate;
                case HandJointKind.LittleDistal: return TrackedHandJoint.LittleDistal;
                case HandJointKind.LittleTip: return TrackedHandJoint.LittleTip;

                default: throw new ArgumentOutOfRangeException($"{typeof(HandJointKind).Name}.{handJointKind} could not be mapped to {typeof(TrackedHandJoint).Name}");
            }
        }
    }
}

#endif // WINDOWS_UWP