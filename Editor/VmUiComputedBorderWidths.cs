using System;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    /// <summary>Target widths after Styles, before layout has produced resolved borders.</summary>
    internal static class VmUiComputedBorderWidths
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Func<VisualElement, float> Left = CreateReader("borderLeftWidth");
        private static readonly Func<VisualElement, float> Top = CreateReader("borderTopWidth");
        private static readonly Func<VisualElement, float> Right = CreateReader("borderRightWidth");
        private static readonly Func<VisualElement, float> Bottom = CreateReader("borderBottomWidth");

        internal static Vector4 Read(VisualElement element) =>
            new Vector4(Left(element), Top(element), Right(element), Bottom(element));

        private static Func<VisualElement, float> CreateReader(string property)
        {
            Type computedType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.ComputedStyle", true);
            MethodInfo computed = typeof(VisualElement).GetProperty("computedStyle", Members)?.GetGetMethod(true)
                ?? throw new MissingMemberException(typeof(VisualElement).FullName, "computedStyle");
            MethodInfo width = computedType.GetProperty(property, Members)?.GetGetMethod(true)
                ?? throw new MissingMemberException(computedType.FullName, property);
            if (computed.ReturnType != computedType.MakeByRefType() || width.ReturnType != typeof(float))
                throw new InvalidOperationException("The native computed border getter ABI is unsupported.");
            var reader = new DynamicMethod("VmUiFeedback_" + property, typeof(float),
                new[] { typeof(VisualElement) }, typeof(VmUiComputedBorderWidths), true);
            ILGenerator il = reader.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, computed);
            il.Emit(OpCodes.Call, width);
            il.Emit(OpCodes.Ret);
            return (Func<VisualElement, float>)reader.CreateDelegate(typeof(Func<VisualElement, float>));
        }
    }
}
