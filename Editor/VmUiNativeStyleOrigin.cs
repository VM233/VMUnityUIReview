#if UNITY_6000_6_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    /// <summary>Current-state attribution using the same ordered records as Unity's Styles debugger.</summary>
    internal sealed class VmUiNativeStyleOrigin
    {
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags Static = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type Extractor = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.MatchedRulesExtractor", true);
        private static readonly ConstructorInfo Constructor = Extractor.GetConstructor(Instance, null,
            new[] { typeof(Func<StyleSheet, string>) }, null) ?? throw new MissingMethodException(Extractor.FullName, ".ctor");
        private static readonly MethodInfo Clear = Extractor.GetMethod("Clear", Instance, null, Type.EmptyTypes, null)
            ?? throw new MissingMethodException(Extractor.FullName, "Clear");
        private static readonly MethodInfo Find = Extractor.GetMethod("FindMatchingRules", Instance, null,
            new[] { typeof(VisualElement) }, null) ?? throw new MissingMethodException(Extractor.FullName, "FindMatchingRules");
        private static readonly FieldInfo Records = Extractor.GetField("matchRecords", Instance)
            ?? throw new MissingFieldException(Extractor.FullName, "matchRecords");
        private static readonly Type Record = Records.FieldType.GetGenericArguments()[0];
        private static readonly FieldInfo Sheet = Record.GetField("sheet", Instance)
            ?? throw new MissingFieldException(Record.FullName, "sheet");
        private static readonly FieldInfo Selector = Record.GetField("complexSelector", Instance)
            ?? throw new MissingFieldException(Record.FullName, "complexSelector");
        private static readonly PropertyInfo Rule = Property(Selector.FieldType, "rule");
        private static readonly PropertyInfo Properties = Property(Rule.PropertyType, "properties");
        private static readonly PropertyInfo Name = Property(Properties.PropertyType.GetElementType(), "name");
        private static readonly PropertyInfo DefaultSheet = Property(typeof(StyleSheet), "isDefaultStyleSheet");
        private static readonly Type Debug = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.StyleDebug", true);
        private static readonly MethodInfo Longhands = Debug.GetMethod("GetLonghandPropertyNames", Static, null,
            new[] { typeof(string) }, null) ?? throw new MissingMethodException(Debug.FullName, "GetLonghandPropertyNames");
        private static readonly MethodInfo Inline = Debug.GetMethod("GetInlineStyleValue", Static, null,
            new[] { typeof(IStyle), typeof(string) }, null) ?? throw new MissingMethodException(Debug.FullName, "GetInlineStyleValue");
        private readonly Dictionary<StyleSheet, string> sheets = new Dictionary<StyleSheet, string>();
        private readonly Dictionary<VisualElement, Dictionary<string, string>> nodes =
            new Dictionary<VisualElement, Dictionary<string, string>>();
        private readonly VmUiFeedbackCoverage coverage;
        private readonly object extractor;
        private int declarationVisits;

        internal VmUiNativeStyleOrigin(VmUiFeedbackCoverage coverage, StyleSheet theme = null, string themePath = null)
        {
            this.coverage = coverage;
            if (theme != null && !string.IsNullOrEmpty(themePath)) sheets.Add(theme, themePath);
            extractor = Constructor.Invoke(new object[] { new Func<StyleSheet, string>(Source) });
        }

        internal void BeginState() => nodes.Clear();
        internal void RegisterTheme(StyleSheet theme, string path) => sheets.Add(theme, path);

        internal string Get(VisualElement node, string property, bool inherited)
        {
            for (int depth = 0; node != null; node = node.hierarchy.parent)
            {
                if (++depth > VmUiFeedbackCoverage.MaximumDepth)
                    throw new InvalidOperationException("Skin attribution exceeds 64 inherited ancestors.");
                object inline = Inline.Invoke(null, new object[] { node.style, property });
                if (inline != null && Property(inline.GetType(), "keyword").GetValue(inline).ToString() != "Null")
                    return "inline";
                if (!nodes.TryGetValue(node, out var values))
                {
                    if (nodes.Count == VmUiFeedbackCoverage.MaximumNodes)
                        throw new InvalidOperationException("Skin attribution exceeds 8192 cached nodes.");
                    values = Read(node);
                    nodes.Add(node, values);
                }
                if (values.TryGetValue(property, out string source)) return source;
                if (!inherited) return "Unity initial style";
            }
            return "Unity initial style";
        }

        private Dictionary<string, string> Read(VisualElement node)
        {
            coverage.StyleMatchQueries++;
            coverage.CheckBudget();
            Clear.Invoke(extractor, null);
            Find.Invoke(extractor, new object[] { node });
            var records = (IList)Records.GetValue(extractor);
            if (records.Count > 4096) throw new InvalidOperationException("Skin attribution exceeds 4096 matched rules per node.");
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (object record in records)
            {
                string source = Source((StyleSheet)Sheet.GetValue(record));
                object rule = Rule.GetValue(Selector.GetValue(record));
                foreach (object property in (IEnumerable)Properties.GetValue(rule))
                {
                    if (++declarationVisits > 1048576)
                        throw new InvalidOperationException("Skin attribution exceeds 1048576 declaration visits.");
                    string name = Name.GetValue(property).ToString();
                    string[] longhands = (string[])Longhands.Invoke(null, new object[] { name });
                    if (longhands == null) values[name] = source;
                    else
                    {
                        if (longhands.Length > 32) throw new InvalidOperationException("Skin attribution exceeds 32 longhands.");
                        foreach (string longhand in longhands) values[longhand] = source;
                    }
                }
            }
            coverage.CheckBudget();
            return values;
        }

        private string Source(StyleSheet sheet)
        {
            if (sheets.TryGetValue(sheet, out string source)) return source;
            string path = AssetDatabase.GetAssetPath(sheet);
            source = IsProjectAsset(path) ? path :
                (bool)DefaultSheet.GetValue(sheet) || !string.IsNullOrEmpty(path)
                    ? "Unity default theme (" + path + ")" : "runtime stylesheet";
            sheets.Add(sheet, source);
            return source;
        }

        internal static bool IsUnity(string source) => source.StartsWith("Unity ", StringComparison.Ordinal) ||
            source.StartsWith("Resources/unity_builtin", StringComparison.OrdinalIgnoreCase) ||
            source.StartsWith("Library/unity default", StringComparison.OrdinalIgnoreCase);

        internal static bool IsProjectAsset(string path) => path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) ||
            (path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase) &&
             !path.StartsWith("Packages/com.unity.", StringComparison.OrdinalIgnoreCase));

        private static PropertyInfo Property(Type type, string name) => type.GetProperty(name, Instance)
            ?? throw new MissingMemberException(type.FullName, name);
    }
}
#endif
