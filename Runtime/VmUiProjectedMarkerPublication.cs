using System;

namespace VMUnityAutomation.Runtime
{
    public sealed class VmUiProjectedMarkerPublication
    {
        public string AnchorIdentity { get; set; }
        public string[] Members { get; set; } = Array.Empty<string>();
        public int DisplayedCount { get; set; }
        public bool Visible { get; set; }
        public float[] PresentedAnchorPixels { get; set; }
        public float[] RenderedGlyphBoundsPixels { get; set; }
        public float[] RenderedBoundsPixels { get; set; }
    }
}
