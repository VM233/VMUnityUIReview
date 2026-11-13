#if UNITY_6000_6_OR_NEWER
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    /// <summary>Imported, pseudo-independent Unity rule matches own USS feedback scope.</summary>
    internal sealed class VmUiFeedbackStyleScope
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type ExtractorType = typeof(VisualElement).Assembly.GetType(
            "UnityEngine.UIElements.MatchedRulesExtractor", true);
        private static readonly ConstructorInfo Constructor = ExtractorType.GetConstructor(Members, null,
            new[] { typeof(Func<StyleSheet, string>) }, null) ??
            throw new MissingMethodException(ExtractorType.FullName, ".ctor(Func<StyleSheet,string>)");
        private static readonly MethodInfo Find = ExtractorType.GetMethod("FindMatchingRules", Members, null,
            new[] { typeof(VisualElement) }, null) ??
            throw new MissingMethodException(ExtractorType.FullName, "FindMatchingRules(VisualElement)");
        private static readonly MethodInfo Clear = ExtractorType.GetMethod("Clear", Members, null,
            Type.EmptyTypes, null) ?? throw new MissingMethodException(ExtractorType.FullName, "Clear()");
        private static readonly FieldInfo Records = ExtractorType.GetField("matchRecords", Members) ??
            throw new MissingFieldException(ExtractorType.FullName, "matchRecords");
        private static readonly FieldInfo Sheet = Records.FieldType.GetGenericArguments()[0].GetField("sheet", Members) ??
            throw new MissingFieldException(Records.FieldType.FullName, "sheet");
        private readonly object extractor = Constructor.Invoke(new object[]
            { new Func<StyleSheet, string>(AssetDatabase.GetAssetPath) });

        internal static Button[] SelectButtons(IReadOnlyList<VisualElement> nodes,
            HashSet<StyleSheet> sheets, VmUiFeedbackCoverage coverage)
        {
            var scope = new VmUiFeedbackStyleScope();
            var scopedSubtrees = new HashSet<VisualElement>();
            var affected = new HashSet<Button>();
            int total = 0;
            foreach (VisualElement node in nodes)
            {
                bool matched = scope.Matches(node, sheets, coverage);
                if (matched || scopedSubtrees.Contains(node.hierarchy.parent))
                    scopedSubtrees.Add(node);
                if (node is Button button)
                {
                    total++;
                    if (scopedSubtrees.Contains(node)) affected.Add(button);
                }
                if (!matched) continue;
                int depth = 0;
                for (var ancestor = node.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
                {
                    if (++depth > VmUiFeedbackCoverage.MaximumDepth)
                        throw new InvalidOperationException("Native feedback scope ancestor depth exceeds 64.");
                    if (ancestor is Button owner) affected.Add(owner);
                }
            }
            coverage.UnaffectedButtons += total - affected.Count;
            return nodes.OfType<Button>().Where(affected.Contains).ToArray();
        }

        private bool Matches(VisualElement node, HashSet<StyleSheet> sheets, VmUiFeedbackCoverage coverage)
        {
            coverage.StyleMatchQueries++;
            coverage.CheckBudget();
            Clear.Invoke(extractor, null);
            Find.Invoke(extractor, new object[] { node });
            coverage.CheckBudget();
            var records = (IList)Records.GetValue(extractor);
            if (records.Count > 32768)
                throw new InvalidOperationException("Native feedback scope exceeds 32768 matched records per node.");
            foreach (object record in records)
                if (sheets.Contains((StyleSheet)Sheet.GetValue(record))) return true;
            return false;
        }
    }
}
#endif
