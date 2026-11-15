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
                mesh.uv = new[] { new Vector2(0, -1), new Vector2(1, -1), new Vector2(1, 1), new Vector2(0, 1) };
                mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
                mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1.3f);
                var actual = VmUiGlyphStrokeCapture.Capture(mesh);
                Assert.That(actual.Widths.Count, Is.EqualTo(1));
                Assert.That(actual.Widths[0], Is.EqualTo(width).Within(.0001f));
                var review = VmUiGlyphStrokeAudit.Review(new[] { actual }, .09f / 1.3f, .1f);
                Assert.That(review.EvaluatedStrokes, Is.EqualTo(1));
                Assert.That(review.Passed, Is.EqualTo(accepted));
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void ReadDiscardedCpuMeshWithoutColorOrTrianglePairAssumptions()
        {
            var mesh = new Mesh { name = "Imported reordered native pens" };
            try
            {
                mesh.vertices = new[] { new Vector3(-.5f, -.045f), new Vector3(.5f, -.045f),
                    new Vector3(.5f, .045f), new Vector3(-.5f, .045f),
                    new Vector3(-.5f, .2f), new Vector3(.5f, .2f), new Vector3(.5f, .29f), new Vector3(-.5f, .29f) };
                mesh.colors32 = new[] { new Color32(26,26,23,0), new Color32(26,26,23,0), new Color32(26,26,23,0), new Color32(26,26,23,0),
                    new Color32(26,26,23,0), new Color32(26,26,23,0), new Color32(26,26,23,0), new Color32(26,26,23,0) };
                mesh.uv = new[] { new Vector2(0,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(0,1),
                    new Vector2(0,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(0,1) };
                mesh.triangles = new[] { 0,2,3, 4,6,7, 0,1,2, 4,5,6 };
                mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1.3f);
                mesh.UploadMeshData(true);
                Assert.That(mesh.isReadable, Is.False);
                var actual = VmUiGlyphStrokeCapture.Capture(mesh);
                Assert.That(actual.Widths, Has.Count.EqualTo(2));
                Assert.That(VmUiGlyphStrokeAudit.Review(new[] { actual }, .09f/1.3f, .025f).Passed, Is.True);
            }
            finally { Object.DestroyImmediate(mesh); }
        }

        [Test]
        public void MeasureShortStrokeAcrossItsAuthoredDirection()
        {
            var actual = VmUiGlyphStrokeAudit.Capture("Short actual stroke", 1.3f,
                new[] { new Vector3(0,-.045f),new Vector3(.02f,-.045f),new Vector3(.02f,.045f),new Vector3(0,.045f) },
                new[] { new Vector2(0,-1),new Vector2(1,-1),new Vector2(1,1),new Vector2(0,1) }, new[] {0,1,2,0,2,3});
            Assert.That(actual.Widths[0], Is.EqualTo(.09f).Within(.0001f));
        }

        [Test]
        public void EmptyCoverageRetainsItsActualIdentityAsAFailedFinding()
        {
            var report = VmUiGlyphStrokeAudit.Review(new[] { new VmUiGlyphStrokeAudit.Glyph {
                Identity = "Unmarked imported glyph", CanvasSpan = 1.3f } }, .09f/1.3f, .025f);
            Assert.That(report.Passed, Is.False);
            Assert.That(report.MissingStrokeGlyphs, Is.EqualTo(1));
            Assert.That(report.Findings[0], Does.StartWith("Unmarked imported glyph:"));
        }
    }
}
