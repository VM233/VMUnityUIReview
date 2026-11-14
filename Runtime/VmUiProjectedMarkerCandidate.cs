using UnityEngine;

namespace VMUnityAutomation.Runtime
{
    public sealed class VmUiProjectedMarkerCandidate
    {
        public string Identity { get; set; }
        public string Family { get; set; }
        public bool Required { get; set; }
        public float[] GeographicAnchorPixels { get; set; }
        public float[] ExpectedGlyphSizePixels { get; set; }
        public float[] CollisionSizePixels { get; set; }
        public Rect ExpectedBounds()
        {
            var size = new Vector2(CollisionSizePixels[0], CollisionSizePixels[1]);
            return new Rect(new Vector2(GeographicAnchorPixels[0], GeographicAnchorPixels[1]) - size * .5f, size);
        }
    }
}
