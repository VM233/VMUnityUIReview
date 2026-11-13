#if UNITY_6000_6_OR_NEWER
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    /// <summary>Native structural selector matches own USS feedback scope; paint keeps its real states.</summary>
    internal sealed class VmUiFeedbackStyleScope
    {
        private const BindingFlags Members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private static readonly Type ComplexType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.StyleComplexSelector", true);
        private static readonly Type SelectorType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.StyleSelector", true);
        private static readonly Type LegacyType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.StyleSheets.LegacySelectorHelper", true);
        private static readonly PropertyInfo Rules = Property(typeof(StyleSheet), "rules");
        private static readonly PropertyInfo RuleSelectors = Property(Rules.PropertyType.GetElementType(), "complexSelectors");
        private static readonly PropertyInfo Selectors = Property(ComplexType, "selectors");
        private static readonly PropertyInfo Parts = Property(SelectorType, "parts");
        private static readonly PropertyInfo Relationship = Property(SelectorType, "previousRelationship");
        private static readonly PropertyInfo PartType = Property(Parts.PropertyType.GetElementType(), "type");
        private static readonly PropertyInfo PartValue = Property(Parts.PropertyType.GetElementType(), "value");
        private static readonly PropertyInfo ElementType = Property(typeof(VisualElement), "typeName");
        private static readonly FieldInfo PositiveMask = Field("pseudoStateMask");
        private static readonly FieldInfo NegativeMask = Field("negatedPseudoStateMask");
        private static readonly MethodInfo Match = LegacyType.GetMethod("MatchRightToLeft",
            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null,
            new[] { typeof(VisualElement), ComplexType }, null) ??
            throw new MissingMethodException(LegacyType.FullName, "MatchRightToLeft(VisualElement,StyleComplexSelector)");
        private readonly Dictionary<string, List<object>> index = new(StringComparer.Ordinal);

        internal VmUiFeedbackStyleScope(IEnumerable<StyleSheet> sheets, VmUiFeedbackCoverage coverage)
        {
            var signatures = new HashSet<string>(StringComparer.Ordinal);
            int ruleCount = 0, selectorCount = 0, partCount = 0, textCount = 0;
            foreach (StyleSheet sheet in sheets)
                foreach (object rule in (Array)Rules.GetValue(sheet))
                {
                    if (++ruleCount > 8192) throw new InvalidOperationException("Feedback scope exceeds 8192 native rules.");
                    foreach (object selector in (Array)RuleSelectors.GetValue(rule))
                    {
                        coverage.CheckBudget();
                        if (++selectorCount > 8192) throw new InvalidOperationException("Feedback scope exceeds 8192 native selectors.");
                        object projection = Project(selector, ref partCount, out string key, out string signature);
                        if (!signatures.Add(signature)) continue;
                        textCount += signature.Length;
                        if (textCount > 8 * 1024 * 1024)
                            throw new InvalidOperationException("Feedback scope exceeds 8 MiB structural signature text.");
                        if (!index.TryGetValue(key, out var values)) index.Add(key, values = new List<object>());
                        values.Add(projection);
                    }
                }
        }

        internal Button[] SelectButtons(IReadOnlyList<VisualElement> nodes, VmUiFeedbackCoverage coverage)
        {
            var scopedSubtrees = new HashSet<VisualElement>();
            var affected = new HashSet<Button>();
            int total = 0;
            foreach (VisualElement node in nodes)
            {
                bool inherited = scopedSubtrees.Contains(node.hierarchy.parent);
                bool matched = !inherited && Matches(node, coverage);
                if (matched || inherited)
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

        private bool Matches(VisualElement node, VmUiFeedbackCoverage coverage)
        {
            if (Bucket("*") || Bucket("#" + node.name) || Bucket("T" + ElementType.GetValue(node))) return true;
            int classes = 0;
            foreach (string name in node.GetClasses())
            {
                if (++classes > 64) throw new InvalidOperationException("Feedback scope exceeds 64 classes per element.");
                if (Bucket("." + name)) return true;
            }
            return false;

            bool Bucket(string key)
            {
                if (!index.TryGetValue(key, out var candidates)) return false;
                foreach (object candidate in candidates)
                {
                    coverage.StyleMatchQueries++;
                    coverage.CheckBudget();
                    bool matched = (bool)Match.Invoke(null, new[] { node, candidate });
                    coverage.CheckBudget();
                    if (matched) return true;
                }
                return false;
            }
        }

        private static object Project(object source, ref int partCount, out string key, out string signature)
        {
            var components = (Array)Selectors.GetValue(source);
            if (components.Length is < 1 or > 64)
                throw new InvalidOperationException("Feedback scope requires 1..64 native selector components.");
            var result = Array.CreateInstance(SelectorType, components.Length);
            var text = new StringBuilder();
            key = "*";
            for (int i = 0; i < components.Length; i++)
            {
                object component = components.GetValue(i);
                var retained = new List<object>();
                object relationship = Relationship.GetValue(component);
                text.Append(relationship).Append('[');
                int priority = 3;
                foreach (object part in (Array)Parts.GetValue(component))
                {
                    if (++partCount > 32768) throw new InvalidOperationException("Feedback scope exceeds 32768 native parts.");
                    string type = PartType.GetValue(part).ToString();
                    if (type.EndsWith("PseudoClass", StringComparison.Ordinal)) continue;
                    string value = (string)PartValue.GetValue(part) ?? "";
                    if (value.Length > 1024) throw new InvalidOperationException("Feedback scope token exceeds 1024 characters.");
                    int rank = type switch { "ID" => 0, "Class" => 1, "Type" => 2, "Wildcard" => 3,
                        _ => throw new InvalidOperationException("Unsupported native stylesheet selector part: " + type) };
                    retained.Add(part);
                    text.Append(type).Append(':').Append(value.Length).Append(':').Append(value).Append(';');
                    if (text.Length > 8192) throw new InvalidOperationException("Feedback scope signature exceeds 8192 characters.");
                    if (i == components.Length - 1 && rank < priority)
                    {
                        priority = rank;
                        key = (rank == 0 ? "#" : rank == 1 ? "." : "T") + value;
                    }
                }
                text.Append(']');
                var parts = Array.CreateInstance(Parts.PropertyType.GetElementType(), retained.Count);
                for (int j = 0; j < retained.Count; j++) parts.SetValue(retained[j], j);
                object clone = Activator.CreateInstance(SelectorType, true);
                Parts.SetValue(clone, parts);
                Relationship.SetValue(clone, relationship);
                PositiveMask.SetValue(clone, 0);
                NegativeMask.SetValue(clone, 0);
                result.SetValue(clone, i);
            }
            object complex = Activator.CreateInstance(ComplexType, true);
            Selectors.SetValue(complex, result);
            signature = text.ToString();
            return complex;
        }

        private static PropertyInfo Property(Type type, string name) => type.GetProperty(name, Members) ??
            throw new MissingMemberException(type.FullName, name);
        private static FieldInfo Field(string name) => SelectorType.GetField(name, Members) ??
            throw new MissingFieldException(SelectorType.FullName, name);
    }
}
#endif
