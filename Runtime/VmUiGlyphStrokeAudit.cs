using System;
using System.Collections.Generic;
using UnityEngine;

namespace VMUnityAutomation.Runtime
{
    public static class VmUiGlyphStrokeAudit
    {
        public sealed class Glyph
        {
            public string Identity;
            public float CanvasSpan;
            public readonly List<float> Widths = new List<float>();
            public readonly List<Vector3> Centers = new List<Vector3>();
        }
        public sealed class Report
        {
            public bool Passed;
            public int EvaluatedGlyphs, EvaluatedStrokes, MissingStrokeGlyphs;
            public int StrokeWeightIssues;
            public List<string> Findings { get; set; } = new List<string>();
        }
        private struct EdgeUse { public int First, Second; }

        // UV endpoint/side roles identify boundaries; every width comes from native positions.
        public static Glyph Capture(string identity, float canvasSpan, IReadOnlyList<Vector3> vertices,
            IReadOnlyList<Vector2> penCoordinates, IReadOnlyList<int> triangles)
        {
            if (string.IsNullOrEmpty(identity) || !Finite(canvasSpan) || canvasSpan <= 0 || vertices == null ||
                vertices.Count is < 1 or > 4096 || penCoordinates == null || penCoordinates.Count != vertices.Count ||
                triangles == null || triangles.Count > 24576 || triangles.Count % 3 != 0)
                throw new ArgumentException("Native pen capture requires identity, canvas,1..4096 positions/UV roles and<=24576 triangle indices.");
            var result = new Glyph { Identity = identity, CanvasSpan = canvasSpan };
            var edges = new Dictionary<(int, int), EdgeUse>();
            int penTriangles = 0;
            for (int offset = 0; offset < triangles.Count; offset += 3)
            {
                bool pen = true;
                for (int corner = 0; corner < 3; corner++)
                {
                    int index = triangles[offset + corner];
                    if (index < 0 || index >= vertices.Count)
                        throw new ArgumentException(identity + ":native triangle index is outside its position stream.");
                    pen &= IsPen(penCoordinates[index]);
                }
                if (!pen) continue;
                penTriangles++;
                for (int corner = 0; corner < 3; corner++)
                {
                    int a = triangles[offset + corner], b = triangles[offset + (corner + 1) % 3];
                    var key = a < b ? (a, b) : (b, a);
                    if (!edges.TryGetValue(key, out var use)) use = new EdgeUse { First = offset, Second = -1 };
                    else if (use.Second >= 0) throw new ArgumentException(identity + ":native pen edge is nonmanifold.");
                    else use.Second = offset;
                    edges[key] = use;
                }
            }
            foreach (var use in edges.Values)
            {
                if (use.Second < 0) continue;
                var corners = new int[] { -1, -1, -1, -1 };
                bool quad = true;
                for (int corner = 0; corner < 6; corner++)
                {
                    int index = triangles[(corner < 3 ? use.First : use.Second) + corner % 3];
                    var uv = penCoordinates[index];
                    int role = uv.y < 0 ? (uv.x < .5f ? 0 : 1) : (uv.x > .5f ? 2 : 3);
                    if (corners[role] >= 0 && corners[role] != index) quad = false;
                    corners[role] = index;
                }
                if (!quad || Array.Exists(corners, index => index < 0)) continue;
                Vector3 a = vertices[corners[0]], b = vertices[corners[1]],
                    c = vertices[corners[2]], d = vertices[corners[3]];
                var along = b - a; var opposite = d - c;
                if (along.sqrMagnitude == 0 || opposite.sqrMagnitude == 0 ||
                    Mathf.Abs(Vector3.Dot(along.normalized, opposite.normalized)) < .999f)
                    throw new ArgumentException(identity + ":native pen lost its parallel authored boundaries.");
                float width = Vector3.Cross(d - a, along.normalized).magnitude;
                if (!Finite(width) || width <= 0) throw new ArgumentException(identity + ":native pen has no finite physical width.");
                result.Widths.Add(width);
                result.Centers.Add((a + b + c + d) * .25f);
            }
            if (result.Widths.Count * 2 != penTriangles)
                throw new ArgumentException(identity + ":not every native pen triangle belongs to one complete directional strip.");
            return result;
        }
        private static bool IsPen(Vector2 uv) => (uv.x == 0 || uv.x == 1) && (uv.y == -1 || uv.y == 1);

        public static Report Review(IReadOnlyList<Glyph> glyphs, float normalizedPen, float relativeTolerance)
        {
            if (glyphs == null || glyphs.Count == 0 || glyphs.Count > 128 || !Finite(normalizedPen) || normalizedPen <= 0 ||
                !Finite(relativeTolerance) || relativeTolerance < 0 || relativeTolerance >= 1)
                throw new ArgumentException("Glyph review requires1..128 actual glyphs and a finite normalized pen contract.");
            var report = new Report();
            foreach (var glyph in glyphs)
            {
                if (glyph == null || string.IsNullOrEmpty(glyph.Identity) || !Finite(glyph.CanvasSpan) || glyph.CanvasSpan <= 0 ||
                    glyph.Widths.Count > 4096)
                    throw new ArgumentException("Each glyph requires its actual identity, canvas and<=4096 measured stroke widths.");
                report.EvaluatedGlyphs++;
                if (glyph.Widths.Count == 0)
                {
                    report.MissingStrokeGlyphs++;
                    report.Findings.Add(glyph.Identity + ":no-native-pen-strips");
                    continue;
                }
                int failures = 0; float minimum = float.PositiveInfinity, maximum = 0;
                foreach (float width in glyph.Widths)
                {
                    if (!Finite(width) || width <= 0) throw new ArgumentException(glyph.Identity + ":native pen widths must be positive and finite.");
                    report.EvaluatedStrokes++;
                    float normalized = width / glyph.CanvasSpan;
                    minimum = Mathf.Min(minimum, normalized); maximum = Mathf.Max(maximum, normalized);
                    if (Mathf.Abs(normalized / normalizedPen - 1) > relativeTolerance) failures++;
                }
                report.StrokeWeightIssues += failures;
                if (failures > 0) report.Findings.Add(glyph.Identity + ":stroke-weight:" +
                    minimum.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ".." +
                    maximum.ToString("R", System.Globalization.CultureInfo.InvariantCulture) + ":issues=" + failures);
            }
            report.Passed = report.Findings.Count == 0;
            return report;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
