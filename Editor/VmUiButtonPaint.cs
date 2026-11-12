using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    internal sealed class VmUiButtonPaint
    {
        private static readonly string[] TransformNames = { "scale", "translate", "rotate" };
        private static readonly PropertyInfo[] Transforms = TransformNames.Select(name =>
            typeof(IResolvedStyle).GetProperty(name) ?? throw new MissingMemberException(typeof(IResolvedStyle).FullName, name)).ToArray();
        private readonly object[] values;

        private VmUiButtonPaint(VisualElement element)
        {
            IResolvedStyle style = element.resolvedStyle;
            bool image = !style.backgroundImage.Equals(default(Background));
            bool text = element is TextElement label && !string.IsNullOrWhiteSpace(label.text);
            var paint = new List<object>(24)
            {
                VisibleColor(style.backgroundColor), image ? style.backgroundImage : default(Background),
                image ? VisibleColor(style.unityBackgroundImageTintColor) : Color.clear,
                style.opacity,
                text ? VisibleColor(style.color) : Color.clear,
                text ? style.fontSize : 0,
                style.borderLeftColor.a > 0 ? style.borderLeftWidth : 0,
                style.borderTopColor.a > 0 ? style.borderTopWidth : 0,
                style.borderRightColor.a > 0 ? style.borderRightWidth : 0,
                style.borderBottomColor.a > 0 ? style.borderBottomWidth : 0,
                style.borderLeftWidth > 0 ? VisibleColor(style.borderLeftColor) : Color.clear,
                style.borderTopWidth > 0 ? VisibleColor(style.borderTopColor) : Color.clear,
                style.borderRightWidth > 0 ? VisibleColor(style.borderRightColor) : Color.clear,
                style.borderBottomWidth > 0 ? VisibleColor(style.borderBottomColor) : Color.clear
            };
            foreach (PropertyInfo property in Transforms) paint.Add(property.GetValue(style));
#if UNITY_2022_2_OR_NEWER
            paint.Add(image ? style.backgroundSize : default(BackgroundSize));
#endif
            values = paint.ToArray();
        }

        private static Color VisibleColor(Color color) => color.a > 0 ? color : Color.clear;

        internal static VmUiButtonPaint[] Capture(IReadOnlyList<VisualElement> elements,
            VmUiFeedbackCoverage coverage)
        {
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
