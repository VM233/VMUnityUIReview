using System;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    /// <summary>Native computed values needed before layout has produced resolved borders.</summary>
    internal static class VmUiComputedStyleValues
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Func<VisualElement, float> Left = CreateReader<float>("borderLeftWidth");
        private static readonly Func<VisualElement, float> Top = CreateReader<float>("borderTopWidth");
        private static readonly Func<VisualElement, float> Right = CreateReader<float>("borderRightWidth");
        private static readonly Func<VisualElement, float> Bottom = CreateReader<float>("borderBottomWidth");
#if UNITY_6000_6_OR_NEWER
        internal static readonly Func<VisualElement, long> MatchingRulesHash = CreateReader<long>("matchingRulesHash");
#endif

        internal static Vector4 Read(VisualElement element) =>
            new Vector4(Left(element), Top(element), Right(element), Bottom(element));

        private static Func<VisualElement, T> CreateReader<T>(string property)
        {
            Type computedType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.ComputedStyle", true);
            MethodInfo computed = typeof(VisualElement).GetProperty("computedStyle", Members)?.GetGetMethod(true)
                ?? throw new MissingMemberException(typeof(VisualElement).FullName, "computedStyle");
            MethodInfo getter = computedType.GetProperty(property, Members)?.GetGetMethod(true);
            FieldInfo field = computedType.GetField(property, Members);
            if (getter == null && field == null) throw new MissingMemberException(computedType.FullName, property);
            if (computed.ReturnType != computedType.MakeByRefType() || (getter?.ReturnType ?? field.FieldType) != typeof(T))
                throw new InvalidOperationException("The native computed border getter ABI is unsupported.");
            var reader = new DynamicMethod("VmUiFeedback_" + property, typeof(T),
                new[] { typeof(VisualElement) }, typeof(VmUiComputedStyleValues), true);
            ILGenerator il = reader.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, computed);
            if (getter != null) il.Emit(OpCodes.Call, getter);
            else il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<VisualElement, T>)reader.CreateDelegate(typeof(Func<VisualElement, T>));
        }
    }
}
