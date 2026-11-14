#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UssRule = VMUnityAutomation.Editor.VmAutomationUssAuditContext.UssRule;
using UssSimpleSelector = VMUnityAutomation.Editor.VmAutomationUssAuditContext.UssSimpleSelector;
using UssAuthoredElement = VMUnityAutomation.Editor.VmAutomationUssAuditContext.UssAuthoredElement;
using UssAuthoredElementUsage = VMUnityAutomation.Editor.VmAutomationUssAuditContext.UssAuthoredElementUsage;
using UssAuthoredDocument = VMUnityAutomation.Editor.VmAutomationUssAuditContext.UssAuthoredDocument;
using UssCascadeRule = VMUnityAutomation.Editor.VmAutomationUssAuditContext.UssCascadeRule;
using UssResolvedDeclaration = VMUnityAutomation.Editor.VmAutomationUssAuditContext.UssResolvedDeclaration;
using UssCascadeDocument = VMUnityAutomation.Editor.VmAutomationUssAuditContext.UssCascadeDocument;
using UssCascadeIndex = VMUnityAutomation.Editor.VmAutomationUssAuditContext.UssCascadeIndex;
using UssUsageIndex = VMUnityAutomation.Editor.VmAutomationUssAuditContext.UssUsageIndex;
using static VMUnityAutomation.Editor.VmAutomationUssStyleSheetParser;
using static VMUnityAutomation.Editor.VmAutomationUssCascadeAuditor;

namespace VMUnityAutomation.Editor
{
    internal static class VmAutomationUssStyleAuditor
    {
        internal const string SUPPRESSION_MARKER = "uss-audit: allow-single-use";
        internal const string REDUNDANT_DECLARATION_SUPPRESSION_MARKER =
            "uss-audit: allow-redundant-declaration";
        internal const string ANCESTOR_DEFAULT_RESET_SUPPRESSION_MARKER =
            "uss-audit: allow-ancestor-default-reset";
        internal const string PIXEL_GRID_SUPPRESSION_MARKER =
            "uss-audit: allow-off-grid-pixels";
        internal const string TEXT_STYLE_CONTRACT_SUPPRESSION_MARKER =
            "uss-audit: allow-text-style-contract";

        private static readonly Regex panelThemeGuidRegex =
            new Regex(@"^\s*themeUss:\s*\{[^}\r\n]*\bguid:\s*(?<guid>[0-9a-fA-F]{32})\b[^}\r\n]*\}",
                RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly Regex simpleClassSelectorRegex =
            new Regex(@"^\.(?<token>[A-Za-z_][A-Za-z0-9_-]*)$", RegexOptions.Compiled);

        private static readonly Regex simpleIdSelectorRegex =
            new Regex(@"^#(?<token>[A-Za-z_][A-Za-z0-9_-]*)$", RegexOptions.Compiled);

        private static readonly Regex classTokenRegex =
            new Regex(@"\.(?<token>[A-Za-z_][A-Za-z0-9_-]*)",
                RegexOptions.Compiled);

        private static readonly Regex idTokenRegex =
            new Regex(@"#(?<token>[A-Za-z_][A-Za-z0-9_-]*)",
                RegexOptions.Compiled);

        private static readonly Regex relationalAnchorClassTokenRegex =
            new Regex(@"(?<![A-Za-z0-9_-])\.(?<token>[A-Za-z_][A-Za-z0-9_-]*)(?=\s|[>+~])",
                RegexOptions.Compiled);

        private static readonly Regex relationalTargetClassTokenRegex =
            new Regex(@"(?<![A-Za-z0-9_-])\.(?<token>[A-Za-z_][A-Za-z0-9_-]*)\s*$",
                RegexOptions.Compiled);

        private static readonly Regex relationalTargetIdTokenRegex =
            new Regex(@"(?<![A-Za-z0-9_-])#(?<token>[A-Za-z_][A-Za-z0-9_-]*)\s*$",
                RegexOptions.Compiled);

        private static readonly Regex quotedTokenRegex =
            new Regex(@"[""'](?<token>[A-Za-z_][A-Za-z0-9_-]*)[""']",
                RegexOptions.Compiled);

        private static readonly Regex yamlListTokenRegex =
            new Regex(@"^\s*-\s*(?<token>[A-Za-z_][A-Za-z0-9_-]*)\s*$",
                RegexOptions.Compiled | RegexOptions.Multiline);

        private static readonly string[] runtimeClassApiMarkers =
        {
            "AddToClassList",
            "RemoveFromClassList",
            "EnableInClassList",
            "ClassListContains",
            "classList",
            "ussClassName",
            "UssClassName"
        };

        private static readonly string[] inheritedTextStyleProperties =
        {
            "color",
            "font-size",
            "-unity-font",
            "-unity-font-definition",
            "-unity-font-style",
            "white-space",
            "letter-spacing",
            "word-spacing",
            "-unity-paragraph-spacing",
            "-unity-text-outline-color",
            "-unity-text-outline-width"
        };

        internal static VmAutomationUssStyleAuditReport Audit(IEnumerable<string> requestedPaths,
            bool includeSuppressed, int maxIssues, VmAutomationUIToolkitAuditOptions options)
        {
            options = options ?? VmAutomationUIToolkitAuditOptions.FromArguments(
                new Dictionary<string, object>());
            var report = new VmAutomationUssStyleAuditReport(maxIssues);
            var requestedPathList = (requestedPaths ?? Array.Empty<string>())
                .Where(path => string.IsNullOrWhiteSpace(path) == false).ToList();
            var requested = NormalizeRequestedPaths(requestedPathList, report.Errors);
            var commonThemePath = FindCommonPanelThemePath(options);
            var commonThemeStylePaths = EnumerateImportedStylePaths(commonThemePath,
                report.Errors);
            var allStyleSheetPaths = VmAutomationUIToolkitAuditUtility.FindAssetFiles(".uss", options)
                .Concat(requested)
                .Concat(commonThemeStylePaths)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            var targetPaths = requestedPathList.Count == 0 ? allStyleSheetPaths : requested;
            report.ScannedStyleSheetCount = targetPaths.Count;
            report.IndexedStyleSheetCount = allStyleSheetPaths.Count;

            var rulesByPath = new Dictionary<string, List<UssRule>>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in allStyleSheetPaths)
            {
                try
                {
                    rulesByPath[path] = ParseStyleSheet(path,
                        File.ReadAllText(VmAutomationUIToolkitAuditUtility.ToFullPath(path)));
                }
                catch (Exception exception)
                {
                    report.Errors.Add($"Failed to read '{path}': {exception.Message}");
                }
            }

            var usageIndex = BuildUsageIndex(rulesByPath.Values.SelectMany(rules => rules), options);
            var cascadeIndex = BuildCascadeIndex(commonThemePath, rulesByPath, usageIndex,
                report.Errors);
            report.IndexedUxmlCount = usageIndex.IndexedUxmlCount;
            report.IndexedRuntimeSourceCount = usageIndex.IndexedRuntimeSourceCount;

            var allRules = rulesByPath.Values.SelectMany(rules => rules).ToArray();
            foreach (var path in targetPaths)
            {
                if (rulesByPath.TryGetValue(path, out var rules) == false)
                {
                    if (File.Exists(VmAutomationUIToolkitAuditUtility.ToFullPath(path)) == false)
                    {
                        report.Errors.Add($"USS asset does not exist: {path}");
                    }

                    continue;
                }

                AuditAuthoringPolicyRules(rules, report);
                AuditPixelGridDeclarations(rules, options, report, includeSuppressed);
                AuditTextStyleContracts(rules, usageIndex, cascadeIndex, report,
                    includeSuppressed);
                AuditRules(rules, usageIndex, report, includeSuppressed);
                AuditRedundantDeclarations(rules, usageIndex, cascadeIndex, report,
                    includeSuppressed);
                VmAutomationUssBackgroundScaleModeAuditor.Audit(rules, usageIndex,
                    cascadeIndex, report, includeSuppressed);
                VmAutomationUssUnboundedFlexShrinkAuditor.Audit(rules, usageIndex,
                    cascadeIndex, report, includeSuppressed);
                VmAutomationUssAuthoredContentNaturalSizeAuditor.Audit(rules,
                    usageIndex, cascadeIndex, report, includeSuppressed);
                VmAutomationUssSharedClassDeclarationAuditor.Audit(rules, usageIndex,
                    report);
                VmAutomationUssGeneratedChildNaturalSizeAuditor.Audit(rules, usageIndex,
                    cascadeIndex, report, includeSuppressed);
                VmAutomationUssAncestorDefaultResetAuditor.Audit(rules, usageIndex,
                    cascadeIndex, report, includeSuppressed);
            }

            AuditProgrammaticDisplayClasses(targetPaths.SelectMany(path =>
                rulesByPath.TryGetValue(path, out var rules)
                    ? (IEnumerable<UssRule>)rules : Array.Empty<UssRule>()),
                allRules, usageIndex, report);
            report.ButtonFeedback = VmAutomationUssButtonFeedbackAuditor.Audit(
                usageIndex.Documents.Select(document => document.AssetPath), targetPaths,
                commonThemePath, issue => report.Record(issue, false), options.RejectDefaultControlAppearance);
            report.SortIssues();
            return report;
        }

        private static UssUsageIndex BuildUsageIndex(IEnumerable<UssRule> allRules,
            VmAutomationUIToolkitAuditOptions options)
        {
            var index = new UssUsageIndex();
            var rules = allRules.ToList();
            CollectSelectorContracts(rules, index);
            IndexUxmlUsage(index, options);
            IndexRuntimeClassReferences(index, options);
            return index;
        }

        internal static void CollectSelectorContracts(IEnumerable<UssRule> rules, UssUsageIndex index)
        {
            foreach (var rule in rules)
            {
                foreach (var selector in rule.Selectors)
                {
                    var simpleClass = simpleClassSelectorRegex.Match(selector);
                    var simpleId = simpleIdSelectorRegex.Match(selector);
                    if (simpleClass.Success)
                    {
                        index.AddSimpleClassRule(simpleClass.Groups["token"].Value);
                    }

                    if (simpleId.Success)
                    {
                        index.AddSimpleIdRule(simpleId.Groups["token"].Value);
                    }

                    foreach (Match match in classTokenRegex.Matches(selector))
                    {
                        var token = match.Groups["token"].Value;
                        index.AllClassTokens.Add(token);
                        if (simpleClass.Success == false)
                        {
                            index.ComplexClassTokens.Add(token);
                        }
                    }

                    foreach (Match match in idTokenRegex.Matches(selector))
                    {
                        var token = match.Groups["token"].Value;
                        index.AllIdTokens.Add(token);
                        var tokenEnd = match.Index + match.Length;
                        if (tokenEnd < selector.Length && selector[tokenEnd] == ':')
                        {
                            index.PseudoIdTokens.Add(token);
                        }

                        if (simpleId.Success == false)
                        {
                            index.ComplexIdTokens.Add(token);
                        }
                    }
                }
            }
        }

        private static void IndexUxmlUsage(UssUsageIndex index, VmAutomationUIToolkitAuditOptions options)
        {
            foreach (var path in VmAutomationUIToolkitAuditUtility.FindAssetFiles(".uxml", options))
            {
                try
                {
                    string text = File.ReadAllText(VmAutomationUIToolkitAuditUtility.ToFullPath(path));
                    var document = XDocument.Parse(text,
                        LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
                    index.IndexedUxmlCount++;
                    index.AddDocument(path, document);
                    foreach (XElement element in document.Descendants())
                    {
                        XAttribute classAttribute = GetAttribute(element, "class");
                        if (classAttribute != null)
                        {
                            string elementName = GetAttributeValue(element, "name");
                            foreach (string token in SplitWhitespace(classAttribute.Value))
                            {
                                if (index.AllClassTokens.Contains(token))
                                {
                                    index.AddClassUsage(token, path,
                                        GetLineNumber(classAttribute),
                                        GetColumnNumber(classAttribute), elementName);
                                }
                            }
                        }

                        XAttribute nameAttribute = GetAttribute(element, "name");
                        if (nameAttribute == null)
                            continue;

                        string name = nameAttribute.Value.Trim();
                        if (index.AllIdTokens.Contains(name))
                        {
                            index.AddIdUsage(name, path,
                                GetLineNumber(nameAttribute),
                                GetColumnNumber(nameAttribute));
                        }
                    }
                }
                catch
                {
                    continue;
                }
            }
        }

        private static void IndexRuntimeClassReferences(UssUsageIndex index,
            VmAutomationUIToolkitAuditOptions options)
        {
            foreach (var path in VmAutomationUIToolkitAuditUtility.FindRuntimeSourceFiles(options))
            {
                string text;
                try
                {
                    text = File.ReadAllText(VmAutomationUIToolkitAuditUtility.ToFullPath(path));
                }
                catch
                {
                    continue;
                }

                if (runtimeClassApiMarkers.Any(marker =>
                        text.Contains(marker, StringComparison.Ordinal)) == false)
                {
                    continue;
                }

                index.IndexedRuntimeSourceCount++;
                foreach (Match match in quotedTokenRegex.Matches(text).Cast<Match>()
                             .Concat(yamlListTokenRegex.Matches(text).Cast<Match>()))
                {
                    var token = match.Groups["token"].Value;
                    if (index.AllClassTokens.Contains(token))
                    {
                        index.AddRuntimeClassReference(token, path,
                            GetLineNumber(text, match.Index),
                            GetColumnNumber(text, match.Index));
                    }
                }

                VmAutomationUssRuntimeClassReferenceClassifier.Index(path, text, index);
            }
        }

        internal static string FindCommonPanelThemePath(VmAutomationUIToolkitAuditOptions options)
        {
            var themePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assetPath in VmAutomationUIToolkitAuditUtility.FindAssetFiles(".asset", options))
            {
                string text;
                try
                {
                    text = File.ReadAllText(VmAutomationUIToolkitAuditUtility.ToFullPath(assetPath));
                }
                catch
                {
                    continue;
                }

                if (text.Contains("UnityEngine.UIElements.PanelSettings",
                        StringComparison.Ordinal) == false)
                {
                    continue;
                }

                var match = panelThemeGuidRegex.Match(text);
                if (match.Success == false)
                {
                    continue;
                }

                var themePath = AssetDatabase.GUIDToAssetPath(match.Groups["guid"].Value);
                if (string.IsNullOrWhiteSpace(themePath) == false)
                {
                    themePaths.Add(VmAutomationUIToolkitAuditUtility.NormalizeAssetPath(themePath));
                }
            }

            return themePaths.Count == 1 ? themePaths.Single() : "";
        }

        private static IReadOnlyCollection<string> EnumerateImportedStylePaths(string rootPath,
            ICollection<string> errors)
        {
            var stylePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            CollectImportedStylePaths(rootPath, stylePaths, visited, errors);
            return stylePaths;
        }

        private static void CollectImportedStylePaths(string assetPath,
            ISet<string> stylePaths, ISet<string> visited, ICollection<string> errors)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || visited.Add(assetPath) == false)
            {
                return;
            }

            string text;
            try
            {
                text = File.ReadAllText(VmAutomationUIToolkitAuditUtility.ToFullPath(assetPath));
            }
            catch (Exception exception)
            {
                errors.Add($"Failed to read theme stylesheet '{assetPath}': {exception.Message}");
                return;
            }

            foreach (var importPath in GetImportedStylePaths(assetPath, text))
            {
                if (importPath.EndsWith(".uss", StringComparison.OrdinalIgnoreCase))
                {
                    stylePaths.Add(importPath);
                }

                CollectImportedStylePaths(importPath, stylePaths, visited, errors);
            }
        }

        internal static void AuditRules(IEnumerable<UssRule> rules, UssUsageIndex usageIndex,
            VmAutomationUssStyleAuditReport report, bool includeSuppressed)
        {
            var ruleList = rules.ToList();
            var variantFamilyTokens =
                VmAutomationUssVariantFamilyContract.FindContractTokens(ruleList, usageIndex);
            var templateContracts = new VmAutomationUssTemplateInstanceContracts(ruleList, usageIndex);
            VmAutomationUssRedundantComponentClassAuditor.Audit(ruleList, usageIndex,
                report, includeSuppressed);
            VmAutomationUssGeneratedChildStyleOwnershipAuditor.Audit(ruleList, usageIndex,
                report, includeSuppressed);
            VmAutomationUssSingleConsumerInstanceDeclarationAuditor.Audit(ruleList,
                usageIndex, variantFamilyTokens, report, templateContracts);
            AuditRelationalSelectorContracts(ruleList, usageIndex, report, includeSuppressed, templateContracts);

            foreach (var rule in ruleList)
            {
                foreach (var selector in rule.Selectors)
                {
                    var ownershipRule = templateContracts.InlineablePart(rule, selector);
                    if (ownershipRule.Declarations.Count == 0) continue;
                    var classMatch = simpleClassSelectorRegex.Match(selector);
                    if (classMatch.Success)
                    {
                        var token = classMatch.Groups["token"].Value;
                        if (usageIndex.ComplexClassTokens.Contains(token))
                        {
                            continue;
                        }

                        var authored = usageIndex.GetClassUsages(token);
                        var runtime = usageIndex.GetRuntimeClassReferences(token);
                        if (authored.Count == 1 && runtime.Count == 0 &&
                            variantFamilyTokens.Contains(token) == false)
                        {
                            const string message =
                                " serves one authored UXML element and has no pseudo, relational, " +
                                "or runtime class contract. Move its declarations to that element's inline style.";
                            if (IsFullyInlineableSingleSelectorRule(ownershipRule) &&
                                usageIndex.GetSimpleClassRuleCount(token) == 1)
                            {
                                AddUnsuppressibleError(report, ownershipRule, selector, token,
                                    "single-use-class", authored, runtime,
                                    $"Class selector '{selector}'{message}");
                            }
                            else
                            {
                                AddIssue(report, ownershipRule, selector, token, "single-use-class",
                                    authored, runtime, $"Class selector '{selector}'{message}",
                                    includeSuppressed);
                            }
                        }

                        continue;
                    }

                    var idMatch = simpleIdSelectorRegex.Match(selector);
                    if (idMatch.Success == false)
                    {
                        continue;
                    }

                    var idToken = idMatch.Groups["token"].Value;
                    if (usageIndex.PseudoIdTokens.Contains(idToken))
                    {
                        continue;
                    }

                    var idUsages = usageIndex.GetIdUsages(idToken);
                    if (idUsages.Count == 1)
                    {
                        const string message =
                            " serves one authored UXML element and has no direct pseudo-state contract. " +
                            "Move its ordinary declarations to that element's inline style; relational use " +
                            "of the same ID does not justify a separate simple selector block.";
                        if (IsFullyInlineableSingleSelectorRule(ownershipRule) &&
                            usageIndex.GetSimpleIdRuleCount(idToken) == 1)
                        {
                            AddUnsuppressibleError(report, ownershipRule, selector, idToken,
                                "single-use-id", idUsages, Array.Empty<UssUsageLocation>(),
                                $"ID selector '{selector}'{message}");
                        }
                        else
                        {
                            AddIssue(report, ownershipRule, selector, idToken, "single-use-id", idUsages,
                                Array.Empty<UssUsageLocation>(),
                                $"ID selector '{selector}'{message}", includeSuppressed);
                        }
                    }
                }
            }
        }

        internal static void AuditPixelGridDeclarations(IEnumerable<UssRule> rules,
            VmAutomationUIToolkitAuditOptions options, VmAutomationUssStyleAuditReport report,
            bool includeSuppressed)
        {
            if (options.PixelGridEnabled == false)
                return;

            foreach (var rule in rules)
            {
                var offGridDeclarations =
                    VmAutomationUIToolkitPixelGridAuditUtility.FindOffGridDeclarations(
                        rule.Declarations, options.PixelGridStep);
                if (offGridDeclarations.Count == 0)
                    continue;

                var orderedProperties = offGridDeclarations.Keys
                    .OrderBy(property => property, StringComparer.Ordinal)
                    .ToList();
                var selector = string.Join(", ", rule.Selectors);
                var suppressionReason = rule.PixelGridSuppressionReason;
                var issue = new VmAutomationUssStyleAuditIssue
                {
                    AssetPath = rule.AssetPath,
                    Line = rule.Line,
                    Selector = selector,
                    Token = string.Join(", ", orderedProperties),
                    Kind = "off-grid-pixel-declarations",
                    GridStep = options.PixelGridStep,
                    OffGridDeclarations = offGridDeclarations,
                    Suppressed = string.IsNullOrWhiteSpace(suppressionReason) == false,
                    SuppressionReason = suppressionReason,
                    Message =
                        $"Selector '{selector}' has structural offset, spacing, or padding " +
                        $"declarations outside the configured {options.PixelGridStep}px grid: " +
                        $"{string.Join(", ", orderedProperties)}. Align them to the project grid " +
                        "or add a reasoned suppression for a measured optical or seam correction."
                };
                report.Record(issue, includeSuppressed);
            }
        }

        internal static void AuditTextStyleContracts(IReadOnlyList<UssRule> rules,
            UssUsageIndex usageIndex, UssCascadeIndex cascadeIndex,
            VmAutomationUssStyleAuditReport report, bool includeSuppressed)
        {
            foreach (var rule in rules)
            {
                if (TryGetSupportedTextContractSelectors(rule, out var selectors) == false)
                {
                    continue;
                }

                AuditAdvancedTextGenerator(rule, selectors, usageIndex, cascadeIndex,
                    report, includeSuppressed);
                AuditSharedFontReset(rule, selectors, cascadeIndex, report);
                AuditFixedFontSizeWithAutoSize(rule, selectors, cascadeIndex, report);
                AuditShrinkWrappedTextAlignment(rule, selectors, usageIndex, cascadeIndex,
                    report, includeSuppressed);
                AuditInheritableOnlyChildTextStyles(rule, selectors, usageIndex,
                    cascadeIndex, report, includeSuppressed);
            }
        }

        internal static void AuditAuthoringPolicyRules(IEnumerable<UssRule> rules,
            VmAutomationUssStyleAuditReport report)
        {
            foreach (var rule in rules)
            {
                string selector = string.IsNullOrWhiteSpace(rule.SelectorGroup)
                    ? string.Join(", ", rule.Selectors)
                    : rule.SelectorGroup;
                if (rule.Selectors.Count > 1)
                {
                    AddAuthoringPolicyError(report, rule, selector,
                        "grouped-selector-list",
                        "Grouped USS selector lists are forbidden. Give each selector its own declaration block so ownership remains explicit.");
                }
                foreach (string property in new[] { "margin", "padding" })
                {
                    if (rule.Declarations.TryGetValue(property, out string value))
                    {
                        AddAuthoringPolicyError(report, rule, selector,
                            "box-shorthand", $"USS '{property}' shorthand is forbidden. Expand '{property}: {value}' into its four explicit edges.",
                            property, value);
                    }
                }
                if (rule.Declarations.Count == 0)
                {
                    AddAuthoringPolicyError(report, rule, selector,
                        "empty-selector-block",
                        "Empty USS selector blocks are forbidden. Remove the block or add its owned declarations.");
                }
            }
        }

        internal static void AuditProgrammaticDisplayClasses(IEnumerable<UssRule> targetRules,
            IEnumerable<UssRule> allRules, UssUsageIndex usageIndex,
            VmAutomationUssStyleAuditReport report)
        {
            var rules = allRules.ToArray();
            var classesWithOtherStyling = new HashSet<string>(StringComparer.Ordinal);
            foreach (var rule in rules.Where(rule => rule.Declarations.Keys.Any(property =>
                         string.Equals(property, "display", StringComparison.OrdinalIgnoreCase) == false)))
            {
                foreach (var selector in rule.Selectors)
                {
                    foreach (Match match in classTokenRegex.Matches(selector))
                    {
                        classesWithOtherStyling.Add(match.Groups["token"].Value);
                    }
                }
            }

            foreach (var rule in targetRules)
            {
                if (rule.Declarations.Count != 1 ||
                    rule.Declarations.ContainsKey("display") == false)
                {
                    continue;
                }

                foreach (var selector in rule.Selectors)
                {
                    var stateClass = classTokenRegex.Matches(selector).Cast<Match>()
                        .Select(match => match.Groups["token"].Value)
                        .FirstOrDefault(token =>
                            IsVisibilityClassName(token) ||
                            usageIndex.GetRuntimeClassSemanticReferences(token).Count > 0 &&
                            classesWithOtherStyling.Contains(token) == false);
                    if (stateClass == null)
                    {
                        continue;
                    }

                    AddAuthoringPolicyError(report, rule, selector,
                        "programmatic-display-class",
                        $"Class '{stateClass}' exists only to switch display. Set the target element's style.display directly in its runtime owner and author its initial display in UXML; do not use a visibility class.",
                        "display", rule.Declarations["display"]);
                }
            }
        }

        private static bool IsVisibilityClassName(string token)
        {
            return string.Equals(token, "hidden", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(token, "visible", StringComparison.OrdinalIgnoreCase) ||
                   token.StartsWith("show-", StringComparison.OrdinalIgnoreCase) ||
                   token.StartsWith("hide-", StringComparison.OrdinalIgnoreCase);
        }

        private static void AuditSharedFontReset(UssRule rule,
            IReadOnlyCollection<UssSimpleSelector> selectors,
            UssCascadeIndex cascadeIndex, VmAutomationUssStyleAuditReport report)
        {
            const string property = "-unity-font-definition";
            if (!rule.Declarations.TryGetValue(property, out var value) ||
                !StyleValuesEqual(value, "none") && !StyleValuesEqual(value, "initial"))
            {
                return;
            }

            var usages = FindWinningElementUsages(rule, property, selectors, cascadeIndex)
                .Where(usage => IsAuthoredTextElement(usage.Element))
                .Where(usage => HasConcreteFallbackFont(usage, rule, property))
                .ToList();
            if (usages.Count == 0)
                return;

            report.Record(new VmAutomationUssStyleAuditIssue
            {
                AssetPath = rule.AssetPath,
                Line = rule.Line,
                Selector = string.Join(", ", rule.Selectors),
                Token = property,
                Kind = "shared-font-definition-reset",
                Severity = "error",
                Property = property,
                Value = value,
                AuthoredUsageCount = usages.Count,
                UsageLocations = ToUsageLocations(usages).Take(20).ToList(),
                Message = $"Selector '{string.Join(", ", rule.Selectors)}' resets " +
                          $"-unity-font-definition to '{value}' on {usages.Count} text element(s), " +
                          "overriding a concrete font from the loaded styles. Remove the reset " +
                          "to use the shared font, or assign an explicit font asset."
            }, false);
        }

        private static bool HasConcreteFallbackFont(UssAuthoredElementUsage usage,
            UssRule resetRule, string property)
        {
            var fallback = usage.Document.Resolve(usage.Element, property, resetRule);
            if (fallback != null)
                return IsConcreteStyleValue(fallback.Value);

            for (var parent = usage.Element.Parent; parent != null; parent = parent.Parent)
            {
                var inherited = ResolveOwnStyle(usage.Document, parent, property);
                if (!string.IsNullOrWhiteSpace(inherited))
                    return IsConcreteStyleValue(inherited);
            }

            return false;
        }

        private static void AuditFixedFontSizeWithAutoSize(UssRule rule,
            IReadOnlyCollection<UssSimpleSelector> selectors,
            UssCascadeIndex cascadeIndex, VmAutomationUssStyleAuditReport report)
        {
            const string property = "font-size";
            if (!rule.Declarations.TryGetValue(property, out string value))
                return;
            bool sameRuleConflict = rule.Declarations.TryGetValue(
                "-unity-text-auto-size", out string authoredAutoSize) &&
                IsTextAutoSizeEnabled(authoredAutoSize);
            var usages = FindWinningElementUsages(rule, property, selectors, cascadeIndex)
                .Where(usage => IsAuthoredTextElement(usage.Element))
                .Where(usage => IsTextAutoSizeEnabled(ResolveEffectiveTextStyle(
                    usage.Document, usage.Element, "-unity-text-auto-size")))
                .ToList();
            if (!sameRuleConflict && usages.Count == 0)
                return;
            report.Record(new VmAutomationUssStyleAuditIssue
            {
                AssetPath = rule.AssetPath,
                Line = rule.Line,
                Selector = string.Join(", ", rule.Selectors),
                Token = property,
                Kind = "fixed-font-size-with-auto-size",
                Severity = "error",
                Property = property,
                Value = value,
                AuthoredUsageCount = usages.Count,
                UsageLocations = ToUsageLocations(usages).Take(20).ToList(),
                Message = sameRuleConflict
                    ? $"Selector '{string.Join(", ", rule.Selectors)}' authors both fixed font-size and enabled -unity-text-auto-size. Remove the fixed font-size or disable auto size."
                    : $"Selector '{string.Join(", ", rule.Selectors)}' fixes font-size while effective -unity-text-auto-size is enabled for {usages.Count} authored text element(s). Remove the fixed font-size or disable auto size."
            }, false);
        }

        private static void AddAuthoringPolicyError(VmAutomationUssStyleAuditReport report,
            UssRule rule, string selector, string kind, string message,
            string property = "", string value = "")
        {
            report.Record(new VmAutomationUssStyleAuditIssue
            {
                AssetPath = rule.AssetPath,
                Line = rule.Line,
                Selector = selector,
                Token = property,
                Kind = kind,
                Severity = "error",
                Property = property,
                Value = value,
                Message = message
            }, false);
        }

        private static void AuditAdvancedTextGenerator(UssRule rule,
            IReadOnlyCollection<UssSimpleSelector> selectors, UssUsageIndex usageIndex,
            UssCascadeIndex cascadeIndex, VmAutomationUssStyleAuditReport report,
            bool includeSuppressed)
        {
            const string property = "-unity-text-generator";
            if (rule.Declarations.TryGetValue(property, out var value) == false ||
                StyleValuesEqual(value, "advanced") == false)
            {
                return;
            }

            var usages = FindWinningElementUsages(rule, property, selectors, cascadeIndex)
                .Where(usage => IsAuthoredTextElement(usage.Element))
                .Where(usage => IsTextAutoSizeEnabled(
                    ResolveEffectiveTextStyle(usage.Document, usage.Element,
                        "-unity-text-auto-size")) == false)
                .ToList();
            if (usages.Count == 0)
            {
                return;
            }

            RecordTextStyleContractIssue(rule, usageIndex, report, includeSuppressed,
                usages, property, value, "advanced-text-generator-without-auto-size",
                $"Selector '{string.Join(", ", rule.Selectors)}' enables the advanced text " +
                $"generator for {usages.Count} authored Label element(s) without effective " +
                "-unity-text-auto-size. Keep the default generator unless auto sizing or another " +
                "documented advanced-text requirement owns this setting.");
        }

        private static void AuditShrinkWrappedTextAlignment(UssRule rule,
            IReadOnlyCollection<UssSimpleSelector> selectors, UssUsageIndex usageIndex,
            UssCascadeIndex cascadeIndex, VmAutomationUssStyleAuditReport report,
            bool includeSuppressed)
        {
            const string property = "-unity-text-align";
            if (rule.Declarations.TryGetValue(property, out var value) == false)
            {
                return;
            }

            var usages = FindWinningElementUsages(rule, property, selectors, cascadeIndex)
                .Where(usage => IsAuthoredTextElement(usage.Element))
                .Where(IsShrinkWrappedBySoleCenteredParent)
                .ToList();
            if (usages.Count == 0)
            {
                return;
            }

            RecordTextStyleContractIssue(rule, usageIndex, report, includeSuppressed,
                usages, property, value, "ineffective-text-align-on-shrink-wrapped-label",
                $"Selector '{string.Join(", ", rule.Selectors)}' sets text alignment on " +
                $"{usages.Count} shrink-wrapped Label element(s). Each Label is the sole child of " +
                "a parent that already centers it on both flex axes, and the Label has no authored " +
                "box expansion for text alignment to act within. Remove the ineffective text-align " +
                "declaration.");
        }

        private static void AuditInheritableOnlyChildTextStyles(UssRule rule,
            IReadOnlyCollection<UssSimpleSelector> selectors, UssUsageIndex usageIndex,
            UssCascadeIndex cascadeIndex, VmAutomationUssStyleAuditReport report,
            bool includeSuppressed)
        {
            var declarations = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
            var relatedUsages = new List<UssAuthoredElementUsage>();
            foreach (var property in inheritedTextStyleProperties)
            {
                if (rule.Declarations.TryGetValue(property, out var value) == false ||
                    IsConcreteStyleValue(value) == false)
                {
                    continue;
                }

                var usages = FindWinningElementUsages(rule, property, selectors, cascadeIndex);
                if (usages.Count == 0 || usages.All(IsOnlyAuthoredChildLabel) == false)
                {
                    continue;
                }

                declarations[property] = value;
                relatedUsages.AddRange(usages);
            }

            if (declarations.Count == 0)
            {
                return;
            }

            var usagesForIssue = DistinctElementUsages(relatedUsages);
            var selectorLabel = string.Join(", ", rule.Selectors);
            var runtimeReferences = GetRuntimeReferences(rule, usageIndex);
            var suppressionReason = rule.TextStyleContractSuppressionReason;
            var issue = new VmAutomationUssStyleAuditIssue
            {
                AssetPath = rule.AssetPath,
                Line = rule.Line,
                Selector = selectorLabel,
                Token = string.Join(", ", declarations.Keys
                    .OrderBy(property => property, StringComparer.Ordinal)),
                Kind = "inheritable-text-style-on-only-child-label",
                RelatedDeclarations = declarations,
                AuthoredUsageCount = usagesForIssue.Count,
                RuntimeReferenceCount = runtimeReferences.Count,
                UsageLocations = ToUsageLocations(usagesForIssue)
                    .Concat(runtimeReferences.Select(location => location.ToDictionary()))
                    .Take(20).ToList(),
                Suppressed = string.IsNullOrWhiteSpace(suppressionReason) == false,
                SuppressionReason = suppressionReason,
                Message =
                    $"Selector '{selectorLabel}' owns inheritable text declarations on " +
                    $"{usagesForIssue.Count} Label element(s), each the sole authored child of its " +
                    $"parent: {string.Join(", ", declarations.Keys.OrderBy(property => property, StringComparer.Ordinal))}. " +
                    "Move those declarations to the parent so the Label inherits them, then remove " +
                    "the child-only class if it has no remaining contract."
            };
            report.Record(issue, includeSuppressed);
        }

        private static bool TryGetSupportedTextContractSelectors(UssRule rule,
            out IReadOnlyCollection<UssSimpleSelector> selectors)
        {
            var parsed = new List<UssSimpleSelector>();
            foreach (var selectorText in rule.Selectors)
            {
                if (TryParseSimpleSelector(selectorText, out var selector) == false ||
                    selector.ClassNames.Count == 0 &&
                    string.IsNullOrWhiteSpace(selector.Id))
                {
                    selectors = Array.Empty<UssSimpleSelector>();
                    return false;
                }

                parsed.Add(selector);
            }

            selectors = parsed;
            return parsed.Count > 0;
        }

        private static List<UssAuthoredElementUsage> FindWinningElementUsages(
            UssRule rule, string property,
            IReadOnlyCollection<UssSimpleSelector> selectors, UssCascadeIndex cascadeIndex)
        {
            var usages = new List<UssAuthoredElementUsage>();
            foreach (var document in cascadeIndex.Documents.Where(document =>
                         document.LoadedAssetPaths.Contains(rule.AssetPath)))
            {
                foreach (var element in document.AuthoredDocument.Elements.Where(element =>
                             selectors.Any(selector => selector.Matches(element))))
                {
                    if (element.InlineDeclarations.ContainsKey(property))
                    {
                        continue;
                    }

                    var winner = document.Resolve(element, property, null);
                    if (winner != null && ReferenceEquals(winner.Rule, rule))
                    {
                        usages.Add(new UssAuthoredElementUsage(document, element));
                    }
                }
            }

            return DistinctElementUsages(usages);
        }

        private static List<UssAuthoredElementUsage> DistinctElementUsages(
            IEnumerable<UssAuthoredElementUsage> usages)
        {
            return usages.GroupBy(usage =>
                    $"{usage.Document.AuthoredDocument.AssetPath}:{usage.Element.Line}:{usage.Element.Column}",
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .OrderBy(usage => usage.Document.AuthoredDocument.AssetPath,
                    StringComparer.Ordinal)
                .ThenBy(usage => usage.Element.Line)
                .ThenBy(usage => usage.Element.Column)
                .ToList();
        }

        private static bool IsShrinkWrappedBySoleCenteredParent(
            UssAuthoredElementUsage usage)
        {
            var element = usage.Element;
            var parent = element.Parent;
            if (IsOnlyAuthoredChildLabel(usage) == false ||
                StyleValuesEqual(ResolveOwnStyle(usage.Document, parent, "align-items"),
                    "center") == false ||
                StyleValuesEqual(ResolveOwnStyle(usage.Document, parent, "justify-content"),
                    "center") == false)
            {
                return false;
            }

            var whiteSpace = ResolveEffectiveTextStyle(usage.Document, element,
                "white-space");
            if (StyleValuesEqual(whiteSpace, "normal") ||
                string.IsNullOrEmpty(element.Text) == false &&
                (element.Text.Contains('\n') || element.Text.Contains('\r')))
            {
                return false;
            }

            if (HasConcreteOwnStyle(usage.Document, element, "width") ||
                HasConcreteOwnStyle(usage.Document, element, "height") ||
                HasConcreteOwnStyle(usage.Document, element, "min-width") ||
                HasConcreteOwnStyle(usage.Document, element, "min-height") ||
                HasConcreteOwnStyle(usage.Document, element, "max-width") ||
                HasConcreteOwnStyle(usage.Document, element, "max-height") ||
                HasConcreteOwnStyle(usage.Document, element, "flex-basis"))
            {
                return false;
            }

            var alignSelf = ResolveOwnStyle(usage.Document, element, "align-self");
            if (StyleValuesEqual(alignSelf, "stretch"))
            {
                return false;
            }

            var flexGrow = ResolveOwnStyle(usage.Document, element, "flex-grow");
            if (HasPositiveNumber(flexGrow))
            {
                return false;
            }

            return new[]
                {
                    "padding", "padding-left", "padding-right", "padding-top",
                    "padding-bottom"
                }
                .All(property => HasNonZeroLength(
                    ResolveOwnStyle(usage.Document, element, property)) == false);
        }

        private static bool IsOnlyAuthoredChildLabel(UssAuthoredElementUsage usage)
        {
            var element = usage.Element;
            return IsAuthoredTextElement(element) &&
                   element.Parent != null &&
                   IsAuthoredTextElement(element.Parent) == false &&
                   element.Parent.Children.Count == 1 &&
                   ReferenceEquals(element.Parent.Children[0], element);
        }

        private static bool IsAuthoredTextElement(UssAuthoredElement element)
        {
            if (element == null)
            {
                return false;
            }

            return string.Equals(element.TypeName, "Label", StringComparison.Ordinal) ||
                   string.Equals(element.TypeName, "TextElement", StringComparison.Ordinal) ||
                   element.TypeName.EndsWith(".Label", StringComparison.Ordinal) ||
                   element.TypeName.EndsWith(".TextElement", StringComparison.Ordinal);
        }

        private static string ResolveOwnStyle(UssCascadeDocument document,
            UssAuthoredElement element, string property)
        {
            if (element == null)
            {
                return "";
            }

            if (element.InlineDeclarations.TryGetValue(property, out var inlineValue))
            {
                return inlineValue;
            }

            return document.Resolve(element, property, null)?.Value ?? "";
        }

        private static string ResolveEffectiveTextStyle(UssCascadeDocument document,
            UssAuthoredElement element, string property)
        {
            for (var current = element; current != null; current = current.Parent)
            {
                var value = ResolveOwnStyle(document, current, property);
                if (string.IsNullOrWhiteSpace(value) == false)
                {
                    return value;
                }
            }

            return "";
        }

        private static bool IsTextAutoSizeEnabled(string value)
        {
            return (value ?? "").Trim().StartsWith("best-fit",
                StringComparison.OrdinalIgnoreCase);
        }

        private static bool HasConcreteOwnStyle(UssCascadeDocument document,
            UssAuthoredElement element, string property)
        {
            return IsConcreteStyleValue(ResolveOwnStyle(document, element, property));
        }

        private static bool IsConcreteStyleValue(string value)
        {
            var normalized = (value ?? "").Trim();
            return normalized.Length > 0 &&
                   string.Equals(normalized, "auto", StringComparison.OrdinalIgnoreCase) == false &&
                   string.Equals(normalized, "none", StringComparison.OrdinalIgnoreCase) == false &&
                   string.Equals(normalized, "initial", StringComparison.OrdinalIgnoreCase) == false &&
                   string.Equals(normalized, "inherit", StringComparison.OrdinalIgnoreCase) == false &&
                   string.Equals(normalized, "unset", StringComparison.OrdinalIgnoreCase) == false;
        }

        private static bool HasPositiveNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return float.TryParse(value.Trim(), NumberStyles.Float,
                       CultureInfo.InvariantCulture, out var parsed)
                ? parsed > 0
                : IsConcreteStyleValue(value);
        }

        private static bool HasNonZeroLength(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var matches = Regex.Matches(value,
                @"[-+]?(?:\d+(?:\.\d*)?|\.\d+)");
            if (matches.Count == 0)
            {
                return IsConcreteStyleValue(value);
            }

            foreach (Match match in matches)
            {
                if (float.TryParse(match.Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out var parsed) == false ||
                    Math.Abs(parsed) > 0.0001f)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<UssUsageLocation> GetRuntimeReferences(UssRule rule,
            UssUsageIndex usageIndex)
        {
            return rule.Selectors
                .SelectMany(selector => classTokenRegex.Matches(selector).Cast<Match>())
                .Select(match => match.Groups["token"].Value)
                .Distinct(StringComparer.Ordinal)
                .SelectMany(usageIndex.GetRuntimeClassReferences)
                .GroupBy(location =>
                        $"{location.Path}:{location.Line}:{location.Column}",
                    StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToList();
        }

        private static IEnumerable<Dictionary<string, object>> ToUsageLocations(
            IEnumerable<UssAuthoredElementUsage> usages)
        {
            return usages.Select(usage => new UssUsageLocation(
                    usage.Document.AuthoredDocument.AssetPath,
                    usage.Element.Line, usage.Element.Column))
                .Select(location => location.ToDictionary());
        }

        private static void RecordTextStyleContractIssue(UssRule rule,
            UssUsageIndex usageIndex, VmAutomationUssStyleAuditReport report,
            bool includeSuppressed, IEnumerable<UssAuthoredElementUsage> usages,
            string property, string value, string kind, string message)
        {
            var authoredUsages = DistinctElementUsages(usages);
            var runtimeReferences = GetRuntimeReferences(rule, usageIndex);
            var suppressionReason = rule.TextStyleContractSuppressionReason;
            var issue = new VmAutomationUssStyleAuditIssue
            {
                AssetPath = rule.AssetPath,
                Line = rule.Line,
                Selector = string.Join(", ", rule.Selectors),
                Token = property,
                Kind = kind,
                Property = property,
                Value = value,
                AuthoredUsageCount = authoredUsages.Count,
                RuntimeReferenceCount = runtimeReferences.Count,
                UsageLocations = ToUsageLocations(authoredUsages)
                    .Concat(runtimeReferences.Select(location => location.ToDictionary()))
                    .Take(20).ToList(),
                Suppressed = string.IsNullOrWhiteSpace(suppressionReason) == false,
                SuppressionReason = suppressionReason,
                Message = message
            };
            report.Record(issue, includeSuppressed);
        }

        internal static void AuditRelationalSelectorContracts(IReadOnlyList<UssRule> rules,
            UssUsageIndex usageIndex, VmAutomationUssStyleAuditReport report, bool includeSuppressed,
            VmAutomationUssTemplateInstanceContracts templateContracts)
        {
            var reportedAnchors = new HashSet<string>(StringComparer.Ordinal);
            var reportedClassTargets = new HashSet<string>(StringComparer.Ordinal);
            var reportedIdTargets = new HashSet<string>(StringComparer.Ordinal);

            foreach (var rule in rules)
            {
                foreach (var selector in rule.Selectors)
                {
                    if (simpleClassSelectorRegex.IsMatch(selector) ||
                        simpleIdSelectorRegex.IsMatch(selector) ||
                        selector.Contains(':') ||
                        SelectorHasRuntimeClassContract(selector, usageIndex))
                    {
                        continue;
                    }

                    foreach (Match anchorMatch in relationalAnchorClassTokenRegex.Matches(selector))
                    {
                        var token = anchorMatch.Groups["token"].Value;
                        if (reportedAnchors.Contains(token))
                        {
                            continue;
                        }

                        var authored = usageIndex.GetClassUsages(token);
                        var runtime = usageIndex.GetRuntimeClassReferences(token);
                        var elementName = usageIndex.GetSingleClassUsageName(token);
                        if (authored.Count != 1 || runtime.Count != 0 ||
                            string.IsNullOrWhiteSpace(elementName))
                        {
                            continue;
                        }

                        var issueRule = rule;
                        var issueSelector = selector;
                        foreach (var candidateRule in rules)
                        {
                            var candidateSelector = candidateRule.Selectors.FirstOrDefault(candidate =>
                            {
                                var match = simpleClassSelectorRegex.Match(candidate);
                                return match.Success && match.Groups["token"].Value == token;
                            });
                            if (candidateSelector == null)
                            {
                                continue;
                            }

                            issueRule = candidateRule;
                            issueSelector = candidateSelector;
                            break;
                        }

                        AddIssue(report, issueRule, issueSelector, token,
                            "single-use-relational-class-anchor", authored, runtime,
                            $"Class anchor '.{token}' identifies one named authored UXML element " +
                            $"'{elementName}'. Move its ordinary declarations inline and replace the class " +
                            $"anchor in relational selectors with '#{elementName}'.",
                            includeSuppressed);
                        reportedAnchors.Add(token);
                    }

                    if (templateContracts.IsInstanceOverride(rule, selector)) continue;
                    var targetMatch = relationalTargetClassTokenRegex.Match(selector);
                    if (targetMatch.Success)
                    {
                        var targetToken = targetMatch.Groups["token"].Value;
                        if (reportedClassTargets.Contains(targetToken) == false)
                        {
                            var targetAuthored = usageIndex.GetClassUsages(targetToken);
                            var targetRuntime = usageIndex.GetRuntimeClassReferences(targetToken);
                            if (targetAuthored.Count == 1 && targetRuntime.Count == 0)
                            {
                                AddIssue(report, rule, selector, targetToken,
                                    "single-use-relational-class-target", targetAuthored, targetRuntime,
                                    $"Class target '.{targetToken}' in relational selector '{selector}' serves one " +
                                    "authored UXML element and has no pseudo or runtime contract. Move the declarations " +
                                    "to that element's inline style and remove the class token.",
                                    includeSuppressed);
                                reportedClassTargets.Add(targetToken);
                            }
                        }
                    }

                    var idTargetMatch = relationalTargetIdTokenRegex.Match(selector);
                    if (idTargetMatch.Success == false)
                    {
                        continue;
                    }

                    var idTargetToken = idTargetMatch.Groups["token"].Value;
                    if (reportedIdTargets.Contains(idTargetToken))
                    {
                        continue;
                    }

                    var idTargetUsages = usageIndex.GetIdUsages(idTargetToken);
                    if (idTargetUsages.Count != 1)
                    {
                        continue;
                    }

                    AddIssue(report, rule, selector, idTargetToken,
                        "single-use-relational-id-target", idTargetUsages,
                        Array.Empty<UssUsageLocation>(),
                        $"ID target '#{idTargetToken}' in relational selector '{selector}' identifies one " +
                        "authored UXML element. Move the declarations to that element's inline style; keep its " +
                        "name only when binding, lookup, or another real consumer still requires it.",
                        includeSuppressed);
                    reportedIdTargets.Add(idTargetToken);
                }
            }
        }

        internal static bool SelectorHasRuntimeClassContract(string selector, UssUsageIndex usageIndex)
        {
            return classTokenRegex.Matches(selector).Cast<Match>().Any(match =>
                usageIndex.GetRuntimeClassReferences(match.Groups["token"].Value).Count > 0);
        }

        internal static void AddIssue(VmAutomationUssStyleAuditReport report, UssRule rule, string selector,
            string token, string kind, IReadOnlyCollection<UssUsageLocation> authored,
            IReadOnlyCollection<UssUsageLocation> runtime, string message, bool includeSuppressed)
        {
            var issue = new VmAutomationUssStyleAuditIssue
            {
                AssetPath = rule.AssetPath,
                Line = rule.Line,
                Selector = selector,
                Token = token,
                Kind = kind,
                AuthoredUsageCount = authored.Count,
                RuntimeReferenceCount = runtime.Count,
                UsageLocations = authored.Concat(runtime).Take(20).Select(location => location.ToDictionary()).ToList(),
                Suppressed = string.IsNullOrEmpty(rule.SuppressionReason) == false,
                SuppressionReason = rule.SuppressionReason,
                Message = message
            };
            report.Record(issue, includeSuppressed);
        }

        private static bool IsFullyInlineableSingleSelectorRule(UssRule rule)
        {
            return rule.Selectors.Count == 1 && rule.Declarations.Count > 0 &&
                   rule.Declarations.Keys.All(property =>
                       property.StartsWith("--", StringComparison.Ordinal) == false);
        }

        private static void AddUnsuppressibleError(VmAutomationUssStyleAuditReport report,
            UssRule rule, string selector, string token, string kind,
            IReadOnlyCollection<UssUsageLocation> authored,
            IReadOnlyCollection<UssUsageLocation> runtime, string message)
        {
            if (string.IsNullOrWhiteSpace(rule.SuppressionReason) == false)
            {
                message += " The preceding allow-single-use marker cannot suppress this " +
                           "fully inlineable selector error.";
            }

            report.Record(new VmAutomationUssStyleAuditIssue
            {
                AssetPath = rule.AssetPath,
                Line = rule.Line,
                Selector = selector,
                Token = token,
                Kind = kind,
                Severity = "error",
                AuthoredUsageCount = authored.Count,
                RuntimeReferenceCount = runtime.Count,
                UsageLocations = authored.Concat(runtime).Take(20)
                    .Select(location => location.ToDictionary()).ToList(),
                Suppressed = false,
                SuppressionReason = "",
                Message = message
            }, false);
        }

        private static List<string> NormalizeRequestedPaths(IEnumerable<string> requestedPaths,
            ICollection<string> errors)
        {
            var requested = (requestedPaths ?? Array.Empty<string>())
                .Where(path => string.IsNullOrWhiteSpace(path) == false)
                .Select(VmAutomationUIToolkitAuditUtility.NormalizeAssetPath)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
            foreach (var path in requested)
            {
                if (path.StartsWith("Assets/", StringComparison.Ordinal) == false ||
                    path.EndsWith(".uss", StringComparison.OrdinalIgnoreCase) == false)
                {
                    errors.Add($"USS audit path must be an Assets-relative .uss path: {path}");
                }
                else if (File.Exists(VmAutomationUIToolkitAuditUtility.ToFullPath(path)) == false)
                {
                    errors.Add($"USS asset does not exist: {path}");
                }
            }

            return requested
                .Where(path => File.Exists(VmAutomationUIToolkitAuditUtility.ToFullPath(path)))
                .ToList();
        }
    }
}
#endif
