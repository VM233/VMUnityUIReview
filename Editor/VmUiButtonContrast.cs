using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    internal sealed class VmUiButtonContrast
    {
        internal bool Measured;
        internal bool HasText;
        internal double TextRatio;
        internal double BoundaryRatio;

        internal static VmUiButtonContrast Capture(VisualElement element, Vector4 borders)
        {
            var sample = new VmUiButtonContrast();
            // A style-only panel cannot measure textures, sibling overpaint or group
            // opacity. Keep those explicit pixel-review boundaries rather than guess.
            int depth = 0;
            for (VisualElement node = element; node != null; node = node.hierarchy.parent)
            {
                if (++depth > VmUiFeedbackCoverage.MaximumDepth)
                    throw new InvalidOperationException("Native contrast ancestor depth exceeds 64.");
                IResolvedStyle style = node.resolvedStyle;
                if (style.display == DisplayStyle.None || style.visibility == Visibility.Hidden || style.opacity != 1)
                    return sample;
            }
            if (!Backdrop(element.hierarchy.parent, out Color outside) ||
                !element.resolvedStyle.backgroundImage.Equals(default(Background))) return sample;
            IResolvedStyle own = element.resolvedStyle;
            Color fill = Over(own.backgroundColor, outside);
            sample.Measured = true;
            sample.BoundaryRatio = Ratio(fill, outside);
            Color[] colors = { own.borderLeftColor, own.borderTopColor, own.borderRightColor, own.borderBottomColor };
            for (int edge = 0; edge < 4; edge++)
                if (borders[edge] > 0)
                    sample.BoundaryRatio = Math.Max(sample.BoundaryRatio, Ratio(Over(colors[edge], fill), outside));
            sample.HasText = element is TextElement text && !string.IsNullOrWhiteSpace(text.text);
            if (sample.HasText) sample.TextRatio = Ratio(Over(own.color, fill), fill);
            return sample;
        }

        private static bool Backdrop(VisualElement element, out Color color)
        {
            color = Color.clear;
            int depth = 0;
            for (VisualElement node = element; node != null; node = node.hierarchy.parent)
            {
                if (++depth > VmUiFeedbackCoverage.MaximumDepth)
                    throw new InvalidOperationException("Native contrast ancestor depth exceeds 64.");
                IResolvedStyle style = node.resolvedStyle;
                if (!style.backgroundImage.Equals(default(Background))) return false;
                color = Over(color, style.backgroundColor);
                if (color.a == 1) return true;
            }
            return false;
        }

        private static Color Over(Color foreground, Color background)
        {
            float alpha = foreground.a + background.a * (1 - foreground.a);
            if (alpha == 0) return Color.clear;
            return new Color((foreground.r * foreground.a + background.r * background.a * (1 - foreground.a)) / alpha,
                (foreground.g * foreground.a + background.g * background.a * (1 - foreground.a)) / alpha,
                (foreground.b * foreground.a + background.b * background.a * (1 - foreground.a)) / alpha, alpha);
        }

        private static double Ratio(Color left, Color right)
        {
            double a = Luminance(left), b = Luminance(right);
            return (Math.Max(a, b) + 0.05) / (Math.Min(a, b) + 0.05);
        }

        private static double Luminance(Color color) =>
            0.2126 * Linear(color.r) + 0.7152 * Linear(color.g) + 0.0722 * Linear(color.b);

        private static double Linear(double value) => value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);

        internal static void Review(VmUiButtonPaint[] normal, VmUiButtonPaint[] current, string state,
            string path, Button button, VmUiFeedbackCoverage coverage, Action<VmAutomationUssStyleAuditIssue> record)
        {
            for (int index = 0; index < current.Length; index++)
            {
                VmUiButtonContrast sample = current[index].Contrast;
                if (!sample.Measured) { coverage.UnmeasuredContrastSamples++; continue; }
                coverage.ContrastSamples++;
                if (sample.HasText)
                {
                    coverage.TextContrastChecks++;
                    if (sample.TextRatio < 4.5) Issue("text", sample.TextRatio, 4.5);
                }
                if (index == 0 && normal[0].Contrast.Measured && normal[0].Contrast.BoundaryRatio >= 3)
                {
                    coverage.BoundaryContrastChecks++;
                    if (sample.BoundaryRatio < 3) Issue("boundary", sample.BoundaryRatio, 3);
                }
            }

            void Issue(string kind, double ratio, double minimum)
            {
                record(new VmAutomationUssStyleAuditIssue
                {
                    AssetPath = path, Selector = "#" + button.name, Token = button.name,
                    Severity = "error", Kind = "button-contrast-" + kind,
                    RelatedDeclarations = new Dictionary<string, string>
                    {
                        { "state", state }, { "ratio", ratio.ToString("R", CultureInfo.InvariantCulture) },
                        { "minimum", minimum.ToString("R", CultureInfo.InvariantCulture) }
                    },
                    Message = "Button '" + button.name + "' has insufficient " + kind + " contrast in " + state +
                        ". Native flat color composition requires " + minimum + ":1; textures and opacity require pixel review."
                });
            }
        }
    }
}
