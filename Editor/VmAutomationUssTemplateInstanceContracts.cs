#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static VMUnityAutomation.Editor.VmAutomationUssAuditContext;

namespace VMUnityAutomation.Editor
{
    internal sealed class VmAutomationUssTemplateInstanceContracts
    {
        private const int MaximumWork = 262144;
        private static readonly Regex ScopedSelector = new Regex(
            @"^(?<anchor>[#.][A-Za-z_][A-Za-z0-9_-]*)\s+(?<target>[#.][A-Za-z_][A-Za-z0-9_-]*)$",
            RegexOptions.Compiled);
        private readonly Dictionary<(string Path, string Selector), HashSet<string>> externalProperties =
            new Dictionary<(string, string), HashSet<string>>();
        private int work;

        internal VmAutomationUssTemplateInstanceContracts(IReadOnlyList<UssRule> rules, UssUsageIndex index)
        {
            var targets = new Dictionary<(string Path, string Token), int>();
            var anchors = new Dictionary<(string Path, string Token), HashSet<string>>();
            foreach (var document in index.Documents)
            {
                foreach (var element in document.Elements)
                {
                    Visit();
                    foreach (string token in Tokens(element))
                    {
                        Visit();
                        var key = (document.AssetPath, token);
                        targets[key] = targets.TryGetValue(key, out int count) ? count + 1 : 1;
                    }
                }
                var instances = document.Elements.Where(element => element.TemplatePath != null)
                    .GroupBy(element => element.TemplatePath, StringComparer.Ordinal);
                foreach (var group in instances)
                {
                    var members = group.ToArray();
                    Visit(members.Length);
                    if (members.Length < 2) continue;
                    var anchorCounts = new Dictionary<string, int>(StringComparer.Ordinal);
                    foreach (var member in members)
                        foreach (string token in Tokens(member))
                        {
                            Visit();
                            anchorCounts[token] = anchorCounts.TryGetValue(token, out int count) ? count + 1 : 1;
                        }
                    foreach (var anchor in anchorCounts)
                    {
                        Visit();
                        if (anchor.Value == members.Length) continue;
                        foreach (string style in document.StylePaths)
                        {
                            Visit();
                            var key = (style, anchor.Key);
                            if (!anchors.TryGetValue(key, out var paths))
                                anchors.Add(key, paths = new HashSet<string>(StringComparer.Ordinal));
                            paths.Add(group.Key);
                        }
                    }
                }
            }
            foreach (var rule in rules)
                foreach (string selector in rule.Selectors)
                {
                    Visit();
                    if (selector.Length > 4096) throw BudgetExceeded();
                    Match match = ScopedSelector.Match(selector);
                    if (!match.Success || !anchors.TryGetValue((rule.AssetPath, match.Groups["anchor"].Value), out var paths)) continue;
                    string target = match.Groups["target"].Value;
                    bool admitted = false;
                    foreach (string path in paths)
                    {
                        Visit();
                        if (targets.TryGetValue((path, target), out int count) && count == 1) admitted = true;
                    }
                    if (!admitted) continue;
                    foreach (string property in rule.Declarations.Keys)
                    {
                        Visit();
                        Add(rule.AssetPath, selector, property);
                        Add(rule.AssetPath, target, property);
                    }
                }
        }

        internal bool RequiresExternal(UssRule rule, string selector, string property)
        {
            Visit();
            return externalProperties.TryGetValue((rule.AssetPath, selector), out var properties) && properties.Contains(property);
        }

        internal bool IsInstanceOverride(UssRule rule, string selector)
        {
            Visit();
            return ScopedSelector.IsMatch(selector) && externalProperties.ContainsKey((rule.AssetPath, selector));
        }

        internal UssRule InlineablePart(UssRule rule, string selector)
        {
            var declarations = rule.Declarations.Where(pair => !RequiresExternal(rule, selector, pair.Key))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
            return declarations.Count == rule.Declarations.Count ? rule : new UssRule
            {
                AssetPath = rule.AssetPath, Line = rule.Line, Selectors = rule.Selectors,
                SelectorGroup = rule.SelectorGroup, Declarations = declarations, SuppressionReason = rule.SuppressionReason
            };
        }

        private void Add(string path, string selector, string property)
        {
            var key = (path, selector);
            if (!externalProperties.TryGetValue(key, out var properties))
                externalProperties.Add(key, properties = new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            properties.Add(property);
        }

        private IEnumerable<string> Tokens(UssAuthoredElement element)
        {
            if (!string.IsNullOrEmpty(element.Name))
            {
                if (element.Name.Length > 4095) throw BudgetExceeded();
                yield return "#" + element.Name;
            }
            foreach (string name in element.AuthoredClasses)
            {
                if (name.Length > 4095) throw BudgetExceeded();
                yield return "." + name;
            }
        }

        private void Visit(int count = 1)
        {
            if (count > MaximumWork - work) throw BudgetExceeded();
            work += count;
        }

        private static InvalidOperationException BudgetExceeded() =>
            new InvalidOperationException("Template-instance style ownership review exceeded its bounded input/work contract.");
    }
}
#endif
