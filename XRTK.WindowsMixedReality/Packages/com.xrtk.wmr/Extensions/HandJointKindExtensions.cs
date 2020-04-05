// Copyright (c) XRTK. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

#if WINDOWS_UWP
using Windows.Perception.People;
using XRTK.Definitions.Controllers.Hands;
#endif // WINDOWS_UWP

namespace XRTK.WindowsMixedReality.Extensions
{
    public static class HandJointKindExtensions
    {
#if WINDOWS_UWP
        public static TrackedHandJoint ToTrackedHandJoint(this HandJointKind handJointKind)
        {
            switch (handJointKind)
            {
                case HandJointKind.Palm: return TrackedHandJoint.Palm;

                case HandJointKind.Wrist: return TrackedHandJoint.Wrist;

                case HandJointKind.ThumbMetacarpal: return TrackedHandJoint.ThumbMetacarpalJoint;
                case HandJointKind.ThumbProximal: return TrackedHandJoint.ThumbProximalJoint;
                case HandJointKind.ThumbDistal: return TrackedHandJoint.ThumbDistalJoint;
                case HandJointKind.ThumbTip: return TrackedHandJoint.ThumbTip;

                case HandJointKind.IndexMetacarpal: return TrackedHandJoint.IndexMetacarpal;
                case HandJointKind.IndexProximal: return TrackedHandJoint.IndexKnuckle;
                case HandJointKind.IndexIntermediate: return TrackedHandJoint.IndexMiddleJoint;
                case HandJointKind.IndexDistal: return TrackedHandJoint.IndexDistalJoint;
                case HandJointKind.IndexTip: return TrackedHandJoint.IndexTip;

                case HandJointKind.MiddleMetacarpal: return TrackedHandJoint.MiddleMetacarpal;
                case HandJointKind.MiddleProximal: return TrackedHandJoint.MiddleKnuckle;
                case HandJointKind.MiddleIntermediate: return TrackedHandJoint.MiddleMiddleJoint;
                case HandJointKind.MiddleDistal: return TrackedHandJoint.MiddleDistalJoint;
                case HandJointKind.MiddleTip: return TrackedHandJoint.MiddleTip;

                case HandJointKind.RingMetacarpal: return TrackedHandJoint.RingMetacarpal;
                case HandJointKind.RingProximal: return TrackedHandJoint.RingKnuckle;
                case HandJointKind.RingIntermediate: return TrackedHandJoint.RingMiddleJoint;
                case HandJointKind.RingDistal: return TrackedHandJoint.RingDistalJoint;
                case HandJointKind.RingTip: return TrackedHandJoint.RingTip;

                case HandJointKind.LittleMetacarpal: return TrackedHandJoint.PinkyMetacarpal;
                case HandJointKind.LittleProximal: return TrackedHandJoint.PinkyKnuckle;
                case HandJointKind.LittleIntermediate: return TrackedHandJoint.PinkyMiddleJoint;
                case HandJointKind.LittleDistal: return TrackedHandJoint.PinkyDistalJoint;
                case HandJointKind.LittleTip: return TrackedHandJoint.PinkyTip;

                default: return TrackedHandJoint.None;
            }
        }
#endif
    }
}