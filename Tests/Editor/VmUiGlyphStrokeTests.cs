using NUnit.Framework;
using UnityEngine;
using VMUnityAutomation.Runtime;

namespace VMUnityAutomation.Editor.Tests
{
    public sealed class VmUiGlyphStrokeTests
    {
        [TestCase(.03f, false)]
        [TestCase(.09f, true)]
        [TestCase(.16f, false)]
        public void MeasureOriginalThinAndThickNativeQuads(float width, bool accepted)
        {
            var mesh = new Mesh { name = "Native map pen witness" };
            try
            {
                mesh.vertices = new[] { new Vector3(-.5f, -width / 2, 0), new Vector3(.5f, -width / 2, 0),
                    new Vector3(.5f, width / 2, 0), new Vector3(-.5f, width / 2, 0) };
                mesh.colors = new[] { Color.black, Color.black, Color.black, Color.black };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1.3f);
                var actual = VmUiGlyphStrokeAudit.Capture(mesh, Color.black);
                Assert.That(actual.Widths.Count, Is.EqualTo(1));
                Assert.That(actual.Widths[0], Is.EqualTo(width).Within(.0001f));
                var review = VmUiGlyphStrokeAudit.Review(new[] { actual }, .09f / 1.3f, .1f);
                Assert.That(review.EvaluatedStrokes, Is.EqualTo(1));
                Assert.That(review.Passed, Is.EqualTo(accepted));
            }
            finally { Object.DestroyImmediate(mesh); }
        }
    }
}
