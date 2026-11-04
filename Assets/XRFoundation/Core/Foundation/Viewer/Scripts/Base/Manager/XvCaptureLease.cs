using System;
using System.Collections.Generic;

namespace Singray.Foundation
{
    // RGBD shares the same native RGB/depth readers as Viewer and recording.
    // Legacy Start/Stop remains idempotent per camera manager; internal users have distinct tokens.
    internal static class XvCaptureLease
    {
        private static readonly Dictionary<XvCameraStreamType, HashSet<object>> owners =
            new Dictionary<XvCameraStreamType, HashSet<object>>();
        internal static bool Managed(XvCameraStreamType type) =>
            type == XvCameraStreamType.ARCameraStream || type == XvCameraStreamType.TofDepthCameraStream;
        internal static bool Register(XvCameraStreamType type, object owner)
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));
            if (!owners.TryGetValue(type, out var set)) owners[type] = set = new HashSet<object>();
            return set.Add(owner);
        }
        internal static bool HasOwners(XvCameraStreamType type) => owners.TryGetValue(type, out var set) && set.Count > 0;
        internal static bool HasOtherOwners(XvCameraStreamType type, object owner) =>
            owners.TryGetValue(type, out var set) && set.Count > (set.Contains(owner) ? 1 : 0);
        internal static bool Release(XvCameraStreamType type, object owner)
        {
            if (!owners.TryGetValue(type, out var set)) return true;
            set.Remove(owner);
            if (set.Count != 0) return false;
            owners.Remove(type);
            return true;
        }
    }
}
