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
        }
        public sealed class Report
        {
            public bool Passed;
            public int EvaluatedGlyphs, EvaluatedStrokes;
            public int StrokeWeightIssues;
            public readonly List<string> Findings = new List<string>();
        }
        public static Glyph Capture(Mesh mesh, Color ink)
        {
            if (mesh == null || mesh.vertexCount == 0 || mesh.vertexCount > 4096)
                throw new ArgumentException("Stroke capture requires one actual readable glyph with1..4096 vertices.");
            var vertices = mesh.vertices; var colors = mesh.colors; var triangles = mesh.triangles;
            if (colors.Length != vertices.Length || triangles.Length > 24576 || triangles.Length % 3 != 0)
                throw new ArgumentException("Stroke capture requires complete native pigment and bounded triangle data.");
            var result = new Glyph { Identity = mesh.name,
                CanvasSpan = Mathf.Max(mesh.bounds.size.x, mesh.bounds.size.y, mesh.bounds.size.z) };
            for (int offset = 0; offset + 5 < triangles.Length; offset += 6)
            {
                bool pigment = true;
                for (int index = 0; index < 6; index++)
                {
                    var color = colors[triangles[offset + index]];
                    if (Mathf.Abs(color.r - ink.r) > .002f || Mathf.Abs(color.g - ink.g) > .002f || Mathf.Abs(color.b - ink.b) > .002f)
                        pigment = false;
                }
                if (!pigment) continue;
                var points = Boundary(vertices, triangles, offset);
                if (points == null) continue;
                float width = float.PositiveInfinity;
                for (int side = 0; side < 2; side++)
                {
                    var a = points[(side + 1) % 4] - points[side];
                    var b = points[(side + 3) % 4] - points[(side + 2) % 4];
                    if (a.sqrMagnitude == 0 || b.sqrMagnitude == 0 ||
                        Mathf.Abs(Vector3.Dot(a.normalized, b.normalized)) < .999f) continue;
                    float candidate = Vector3.Cross(points[(side + 2) % 4] - points[side], a.normalized).magnitude;
                    if (candidate > 0) width = Mathf.Min(width, candidate);
                }
                if (!float.IsPositiveInfinity(width)) result.Widths.Add(width);
            }
            return result;
        }
        private static Vector3[] Boundary(Vector3[] vertices, int[] indices, int offset)
        {
            var from = new int[6]; var to = new int[6]; var boundary = new bool[6];
            int count = 0;
            for (int edge = 0; edge < 6; edge++)
            {
                int triangle = offset + edge / 3 * 3;
                from[edge] = indices[triangle + edge % 3]; to[edge] = indices[triangle + (edge + 1) % 3];
                boundary[edge] = true;
            }
            for (int edge = 0; edge < 6; edge++)
                for (int other = edge + 1; other < 6; other++)
                    if (from[edge] == to[other] && to[edge] == from[other]) boundary[edge] = boundary[other] = false;
            int first = -1;
            for (int edge = 0; edge < 6; edge++) if (boundary[edge]) { count++; if (first < 0) first = edge; }
            if (count != 4) return null;
            var result = new Vector3[4]; int current = first;
            for (int corner = 0; corner < 4; corner++)
            {
                result[corner] = vertices[from[current]]; int destination = to[current]; boundary[current] = false;
                if (corner == 3) return destination == from[first] ? result : null;
                current = -1;
                for (int edge = 0; edge < 6; edge++) if (boundary[edge] && from[edge] == destination) { current = edge; break; }
                if (current < 0) return null;
            }
            return result;
        }
        public static Report Review(IReadOnlyList<Glyph> glyphs, float normalizedPen, float relativeTolerance)
        {
            if (glyphs == null || glyphs.Count == 0 || glyphs.Count > 128 || !Finite(normalizedPen) || normalizedPen <= 0 ||
                !Finite(relativeTolerance) || relativeTolerance < 0 || relativeTolerance >= 1)
                throw new ArgumentException("Glyph review requires1..128 actual glyphs and a finite normalized pen contract.");
            var report = new Report();
            foreach (var glyph in glyphs)
            {
                if (glyph == null || string.IsNullOrEmpty(glyph.Identity) || !Finite(glyph.CanvasSpan) || glyph.CanvasSpan <= 0 ||
                    glyph.Widths.Count == 0 || glyph.Widths.Count > 4096)
                    throw new ArgumentException("Each glyph requires its actual identity, canvas and1..4096 measured stroke widths.");
                report.EvaluatedGlyphs++;
                int failures = 0; float minimum = float.PositiveInfinity, maximum = 0;
                foreach (float width in glyph.Widths)
                {
                    if (!Finite(width) || width <= 0) throw new ArgumentException("Native pen widths must be positive and finite.");
                    report.EvaluatedStrokes++;
                    float normalized = width / glyph.CanvasSpan;
                    minimum = Mathf.Min(minimum, normalized); maximum = Mathf.Max(maximum, normalized);
                    if (Mathf.Abs(normalized / normalizedPen - 1) > relativeTolerance)
                        failures++;
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
