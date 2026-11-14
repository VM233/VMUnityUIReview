using System;

namespace VMUnityAutomation.Runtime
{
    public sealed class VmUiProjectedMarkerSnapshot
    {
        public string Owner { get; set; }
        public float[] ViewportPixels { get; set; }
        public float CollisionPaddingPixels { get; set; }
        public VmUiProjectedMarkerCandidate[] Candidates { get; set; } = Array.Empty<VmUiProjectedMarkerCandidate>();
        public VmUiProjectedMarkerPublication[] Publications { get; set; } = Array.Empty<VmUiProjectedMarkerPublication>();
    }

}
