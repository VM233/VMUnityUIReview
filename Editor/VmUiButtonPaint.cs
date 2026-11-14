using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    internal sealed class VmUiButtonPaint
    {
        private static readonly Unity.Profiling.ProfilerMarker CaptureMarker = new Unity.Profiling.ProfilerMarker("VM.UIFeedback.Paint");
        private static readonly string[] TransformNames = { "scale", "translate", "rotate" };
        private static readonly PropertyInfo[] Transforms = TransformNames.Select(name =>
            typeof(IResolvedStyle).GetProperty(name) ?? throw new MissingMemberException(typeof(IResolvedStyle).FullName, name)).ToArray();
        private readonly object[] values;
        private readonly Vector3 nativeScale;
        internal VmUiButtonContrast Contrast { get; private set; }
        internal string ScaleEvidence => string.Join(",", nativeScale.x.ToString("R", CultureInfo.InvariantCulture),
            nativeScale.y.ToString("R", CultureInfo.InvariantCulture), nativeScale.z.ToString("R", CultureInfo.InvariantCulture));

        private VmUiButtonPaint(VisualElement element)
        {
            IResolvedStyle style = element.resolvedStyle;
            bool image = !style.backgroundImage.Equals(default(Background));
            bool text = element is TextElement label && !string.IsNullOrWhiteSpace(label.text);
            Vector4 borders = VmUiComputedStyleValues.Read(element);
            Contrast = VmUiButtonContrast.Capture(element, borders);
            var paint = new List<object>(24)
            {
                VisibleColor(style.backgroundColor), image ? style.backgroundImage : default(Background),
                image ? VisibleColor(style.unityBackgroundImageTintColor) : Color.clear,
                style.opacity,
                text ? VisibleColor(style.color) : Color.clear,
                text ? style.fontSize : 0,
                style.borderLeftColor.a > 0 ? borders.x : 0,
                style.borderTopColor.a > 0 ? borders.y : 0,
                style.borderRightColor.a > 0 ? borders.z : 0,
                style.borderBottomColor.a > 0 ? borders.w : 0,
                borders.x > 0 ? VisibleColor(style.borderLeftColor) : Color.clear,
                borders.y > 0 ? VisibleColor(style.borderTopColor) : Color.clear,
                borders.z > 0 ? VisibleColor(style.borderRightColor) : Color.clear,
                borders.w > 0 ? VisibleColor(style.borderBottomColor) : Color.clear
            };
            object scale = Transforms[0].GetValue(style);
            nativeScale = ((Scale)scale).value;
            paint.Add(scale);
            for (int index = 1; index < Transforms.Length; index++) paint.Add(Transforms[index].GetValue(style));
#if UNITY_2022_2_OR_NEWER
            paint.Add(image ? style.backgroundSize : default(BackgroundSize));
#endif
            values = paint.ToArray();
        }

        private static Color VisibleColor(Color color) => color.a > 0 ? color : Color.clear;

        internal static VmUiButtonPaint[] Capture(IReadOnlyList<VisualElement> elements,
            VmUiFeedbackCoverage coverage)
        {
            using var measurement = CaptureMarker.Auto();
            coverage.Samples += elements.Count;
            coverage.CheckBudget();
            return elements.Select(element => new VmUiButtonPaint(element)).ToArray();
        }

        internal static bool Same(VmUiButtonPaint[] left, VmUiButtonPaint[] right)
        {
            if (left.Length != right.Length) throw new InvalidOperationException("Feedback snapshot identity changed.");
            for (int i = 0; i < left.Length; i++)
                if (!left[i].values.SequenceEqual(right[i].values)) return false;
            return true;
        }
    }
}
