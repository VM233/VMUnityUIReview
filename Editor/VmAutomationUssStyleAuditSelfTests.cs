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

using static VMUnityAutomation.Editor.VmAutomationUssStyleAuditor;
namespace VMUnityAutomation.Editor
{
    internal static class VmAutomationUssStyleAuditSelfTests
    {

        internal static Dictionary<string, object> Run()
        {
            const string path = "Assets/__UssAuditSelfTest.uss";
            const string text =
                ".single { width: 10px; }\n" +
                "#Unique { height: 10px; }\n" +
                "#IdContainer { width: 20px; }\n" +
                "#IdContainer .generated { height: 10px; }\n" +
                "#Parent #UniqueChild { margin-left: 10px; }\n" +
                "#interactive-id { color: white; }\n" +
                "#interactive-id:hover { color: red; }\n" +
                ".shared { color: white; }\n" +
                ".interactive { color: white; }\n" +
                ".interactive:hover { color: red; }\n" +
                ".runtime-state { display: none; }\n" +
                ".container { width: 20px; }\n" +
                ".container .child { width: 10px; }\n" +
                "/* uss-audit: allow-single-use fixture owns a custom-property cascade */\n" +
                ".suppressed { --fixture-opacity: 0.5; }\n" +
                "/* uss-audit: allow-single-use invalid inlineable exception */\n" +
                ".inlineable-suppression { opacity: 0.5; }\n" +
                ".unused { width: 1px; }\n";

            var rules = ParseStyleSheet(path, text);
            var index = new UssUsageIndex();
            CollectSelectorContracts(rules, index);
            index.AddClassUsage("single", "Assets/Single.uxml", 1);
            index.AddIdUsage("Unique", "Assets/Single.uxml", 2);
            index.AddIdUsage("IdContainer", "Assets/IdContainer.uxml", 1);
            index.AddIdUsage("Parent", "Assets/Parent.uxml", 1);
            index.AddIdUsage("UniqueChild", "Assets/Parent.uxml", 2);
            index.AddIdUsage("interactive-id", "Assets/InteractiveId.uxml", 1);
            index.AddClassUsage("shared", "Assets/SharedA.uxml", 1);
            index.AddClassUsage("shared", "Assets/SharedB.uxml", 1);
            index.AddClassUsage("interactive", "Assets/Interactive.uxml", 1);
            index.AddClassUsage("runtime-state", "Assets/Runtime.uxml", 1);
            index.AddRuntimeClassReference("runtime-state", "Assets/Scripts/Runtime.cs", 4);
            index.AddClassUsage("container", "Assets/Container.uxml", 1, "Container");
            index.AddClassUsage("child", "Assets/Container.uxml", 2);
            index.AddClassUsage("suppressed", "Assets/Suppressed.uxml", 1);
            index.AddClassUsage("inlineable-suppression",
                "Assets/InlineableSuppression.uxml", 1);

            var report = new VmAutomationUssStyleAuditReport(100)
            {
                ScannedStyleSheetCount = 1,
                IndexedStyleSheetCount = 1
            };
            AuditRules(rules, index, report, true);
            report.SortIssues();

            const string themePath = "Assets/__UssAuditSelfTestTheme.uss";
            const string duplicatePath = "Assets/__UssAuditSelfTestDuplicate.uss";
            var themeRules = ParseStyleSheet(themePath,
                "* { -unity-slice-scale: 3px; }\n");
            var duplicateRules = ParseStyleSheet(duplicatePath,
                ".duplicate { -unity-slice-scale: 3px; }\n" +
                ".different { -unity-slice-scale: 2px; }\n" +
                ".initial-default { position: relative; }\n" +
                ".initial-margin { margin-top: 0; }\n" +
                ".button-reset { background-color: rgba(0, 0, 0, 0); margin-left: 0; padding-left: 0; }\n" +
                ".toggle-reset { background-color: rgba(0, 0, 0, 0); margin-left: 0; padding-left: 0; }\n" +
                ".mixed-reset { background-color: rgba(0, 0, 0, 0); margin-left: 0; padding-left: 0; }\n" +
                ".control-duplicate { -unity-slice-scale: 3px; }\n" +
                "/* uss-audit: allow-redundant-declaration fixture documents ownership */\n" +
                ".suppressed-duplicate { -unity-slice-scale: 3px; }\n");
            var duplicateUsageIndex = new UssUsageIndex();
            CollectSelectorContracts(duplicateRules, duplicateUsageIndex);
            var duplicateDocument = new UssAuthoredDocument("Assets/Duplicate.uxml",
                XDocument.Parse(
                    "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\">" +
                    "<ui:VisualElement class=\"duplicate\"/>" +
                    "<ui:VisualElement class=\"different\"/>" +
                    "<ui:VisualElement class=\"initial-default\"/>" +
                    "<ui:VisualElement class=\"initial-margin\"/>" +
                    "<ui:Button class=\"button-reset\"/>" +
                    "<ui:Toggle class=\"toggle-reset\"/>" +
                    "<ui:VisualElement class=\"mixed-reset\"/>" +
                    "<ui:Button class=\"mixed-reset\"/>" +
                    "<ui:Button class=\"control-duplicate\"/>" +
                    "<ui:VisualElement class=\"suppressed-duplicate\"/>" +
                    "</ui:UXML>", LoadOptions.SetLineInfo));
            duplicateUsageIndex.Documents.Add(duplicateDocument);
            var duplicateCascade = new UssCascadeIndex();
            var duplicateCascadeDocument = new UssCascadeDocument(duplicateDocument);
            AppendSelfTestRules(duplicateCascadeDocument, themeRules, 0);
            AppendSelfTestRules(duplicateCascadeDocument, duplicateRules, 1);
            duplicateCascade.Documents.Add(duplicateCascadeDocument);
            var duplicateReport = new VmAutomationUssStyleAuditReport(100)
            {
                ScannedStyleSheetCount = 1,
                IndexedStyleSheetCount = 2,
                IndexedUxmlCount = 1
            };
            AuditRedundantDeclarations(duplicateRules, duplicateUsageIndex, duplicateCascade,
                duplicateReport, true);
            duplicateReport.SortIssues();

            var pixelGridRules = ParseStyleSheet(path,
                ".grid-pass { left: -6px; margin-right: 9px; padding: 3px 6px; }\n" +
                ".grid-fail { top: 4px; padding: 3px 7px; }\n" +
                ".grid-non-pixel { left: 50%; font-size: 7px; }\n" +
                "/* uss-audit: allow-off-grid-pixels fixture documents optical alignment */\n" +
                ".grid-suppressed { margin-left: 1px; }\n");
            var pixelGridOptions = VmAutomationUIToolkitAuditOptions.FromArguments(
                new Dictionary<string, object>
                {
                    { "useProjectSettings", false },
                    { "pixelGridEnabled", true },
                    { "pixelGridStep", 3 }
                });
            var pixelGridReport = new VmAutomationUssStyleAuditReport(100);
            AuditPixelGridDeclarations(pixelGridRules, pixelGridOptions, pixelGridReport, true);
            pixelGridReport.SortIssues();

            const string textContractPath = "Assets/__UssAuditSelfTestTextContracts.uss";
            var textContractRules = ParseStyleSheet(textContractPath,
                ".centered-text-owner { align-items: center; justify-content: center; }\n" +
                ".problem-text { color: white; font-size: 18px; -unity-font-style: bold; " +
                "-unity-text-generator: advanced; -unity-text-align: middle-center; }\n" +
                ".auto-sized-text { -unity-text-generator: advanced; " +
                "-unity-text-auto-size: best-fit 8px 18px; }\n" +
                ".fixed-auto-text { font-size: 16px; -unity-text-auto-size: best-fit 8px 18px; }\n" +
                ".boxed-text { width: 30px; -unity-text-align: middle-center; }\n" +
                ".sibling-text { color: white; }\n" +
                "/* uss-audit: allow-text-style-contract fixture documents advanced shaping */\n" +
                ".suppressed-text { -unity-text-generator: advanced; }\n");
            var textContractUsageIndex = new UssUsageIndex();
            CollectSelectorContracts(textContractRules, textContractUsageIndex);
            var textContractDocument = new UssAuthoredDocument(
                "Assets/TextContracts.uxml",
                XDocument.Parse(
                    "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\">" +
                    "<ui:VisualElement class=\"centered-text-owner\">" +
                    "<ui:Label class=\"problem-text\" text=\"1\"/>" +
                    "</ui:VisualElement>" +
                    "<ui:VisualElement><ui:Label class=\"auto-sized-text\" text=\"Auto\"/>" +
                    "</ui:VisualElement>" +
                    "<ui:VisualElement><ui:Label class=\"fixed-auto-text\" text=\"Conflict\"/>" +
                    "<ui:VisualElement/></ui:VisualElement>" +
                    "<ui:VisualElement class=\"centered-text-owner\">" +
                    "<ui:Label class=\"boxed-text\" text=\"Box\"/>" +
                    "</ui:VisualElement>" +
                    "<ui:VisualElement><ui:Label class=\"sibling-text\" text=\"Sibling\"/>" +
                    "<ui:VisualElement/></ui:VisualElement>" +
                    "<ui:VisualElement class=\"centered-text-owner\">" +
                    "<ui:Label class=\"suppressed-text\" text=\"Suppressed\"/>" +
                    "</ui:VisualElement>" +
                    "</ui:UXML>", LoadOptions.SetLineInfo));
            textContractUsageIndex.Documents.Add(textContractDocument);
            var textContractCascade = new UssCascadeIndex();
            var textContractCascadeDocument =
                new UssCascadeDocument(textContractDocument);
            AppendSelfTestRules(textContractCascadeDocument, textContractRules, 1);
            textContractCascade.Documents.Add(textContractCascadeDocument);
            var textContractReport = new VmAutomationUssStyleAuditReport(100);
            AuditTextStyleContracts(textContractRules, textContractUsageIndex,
                textContractCascade, textContractReport, true);
            textContractReport.SortIssues();

            const string fontResetPath = "Assets/__UssAuditSelfTestFontReset.uss";
            var sharedFontRules = ParseStyleSheet("Assets/__UssAuditSelfTestSharedFont.uss",
                ".unity-text-element { -unity-font-definition: url('project-font.asset'); }\n");
            var fontResetRules = ParseStyleSheet(fontResetPath,
                ".unity-text-element.font-none { -unity-font-definition: none; }\n" +
                ".unity-text-element.font-initial { -unity-font-definition: initial; }\n" +
                ".unity-text-element.font-explicit { -unity-font-definition: url('other-font.asset'); }\n" +
                ".unity-text-element.font-inline { -unity-font-definition: none; }\n");
            var fontResetDocument = new UssAuthoredDocument("Assets/FontReset.uxml",
                XDocument.Parse(
                    "<ui:UXML xmlns:ui=\"UnityEngine.UIElements\">" +
                    "<ui:Label class=\"font-none\" text=\"None\"/>" +
                    "<ui:Label class=\"font-initial\" text=\"Initial\"/>" +
                    "<ui:Label class=\"font-explicit\" text=\"Explicit\"/>" +
                    "<ui:Label class=\"font-inline\" text=\"Inline\" " +
                    "style=\"-unity-font-definition: url('inline-font.asset');\"/>" +
                    "</ui:UXML>", LoadOptions.SetLineInfo));
            var fontResetCascadeDocument = new UssCascadeDocument(fontResetDocument);
            AppendSelfTestRules(fontResetCascadeDocument, sharedFontRules, 0);
            AppendSelfTestRules(fontResetCascadeDocument, fontResetRules, 1);
            var fontResetCascade = new UssCascadeIndex();
            fontResetCascade.Documents.Add(fontResetCascadeDocument);
            var fontResetReport = new VmAutomationUssStyleAuditReport(20);
            AuditTextStyleContracts(fontResetRules, new UssUsageIndex(), fontResetCascade,
                fontResetReport, false);
            var fontResetIssues = fontResetReport.Issues
                .Where(issue => issue.Kind == "shared-font-definition-reset")
                .OrderBy(issue => issue.Selector, StringComparer.Ordinal)
                .ToArray();

            var noSharedFontDocument = new UssCascadeDocument(fontResetDocument);
            AppendSelfTestRules(noSharedFontDocument, fontResetRules, 1);
            var noSharedFontCascade = new UssCascadeIndex();
            noSharedFontCascade.Documents.Add(noSharedFontDocument);
            var noSharedFontReport = new VmAutomationUssStyleAuditReport(20);
            AuditTextStyleContracts(fontResetRules, new UssUsageIndex(),
                noSharedFontCascade, noSharedFontReport, false);

            var authoringPolicyRules = ParseStyleSheet(path,
                ".group-a, .group-b { color: white; }\n" +
                ".shorthand { margin: 3px; padding: 6px; }\n" +
                ".empty { /* intentionally empty */ }\n");
            var authoringPolicyReport = new VmAutomationUssStyleAuditReport(100);
            AuditAuthoringPolicyRules(authoringPolicyRules, authoringPolicyReport);
            var authoringPolicyKinds = authoringPolicyReport.Issues
                .Select(issue => issue.Kind).OrderBy(kind => kind, StringComparer.Ordinal)
                .ToArray();

            var displayClassRules = ParseStyleSheet(path,
                ".badge.hidden { display: none; }\n" +
                ".card.show-badge .badge { display: flex; }\n" +
                ".card.runtime-visible .badge { display: flex; }\n" +
                ".card.selected .body { display: flex; }\n" +
                ".card.selected { color: white; }\n" +
                ".layout { display: flex; }\n");
            var displayClassUsage = new UssUsageIndex();
            displayClassUsage.AddRuntimeClassSemanticReference("runtime-visible",
                "Assets/Scripts/Panel.cs", 10);
            displayClassUsage.AddRuntimeClassSemanticReference("selected",
                "Assets/Scripts/Panel.cs", 11);
            var displayClassReport = new VmAutomationUssStyleAuditReport(20);
            AuditProgrammaticDisplayClasses(displayClassRules, displayClassRules,
                displayClassUsage, displayClassReport);
            var displayClassSelectors = displayClassReport.Issues
                .Select(issue => issue.Selector)
                .OrderBy(selector => selector, StringComparer.Ordinal).ToArray();

            var activeTokens = report.Issues.Where(issue => issue.Suppressed == false)
                .Select(issue => issue.Token).OrderBy(token => token, StringComparer.Ordinal).ToArray();
            var errorTokens = report.Issues.Where(issue => issue.IsError)
                .Select(issue => issue.Token).OrderBy(token => token, StringComparer.Ordinal).ToArray();
            var suppressedTokens = report.Issues.Where(issue => issue.Suppressed)
                .Select(issue => issue.Token).OrderBy(token => token, StringComparer.Ordinal).ToArray();
            var activeRedundantSelectors = duplicateReport.Issues
                .Where(issue => issue.Suppressed == false)
                .Select(issue => issue.Selector)
                .OrderBy(selector => selector, StringComparer.Ordinal)
                .ToArray();
            var suppressedRedundantSelectors = duplicateReport.Issues
                .Where(issue => issue.Suppressed)
                .Select(issue => issue.Selector)
                .OrderBy(selector => selector, StringComparer.Ordinal)
                .ToArray();
            var activePixelGridSelectors = pixelGridReport.Issues
                .Where(issue => issue.Suppressed == false)
                .Select(issue => issue.Selector)
                .OrderBy(selector => selector, StringComparer.Ordinal)
                .ToArray();
            var suppressedPixelGridSelectors = pixelGridReport.Issues
                .Where(issue => issue.Suppressed)
                .Select(issue => issue.Selector)
                .OrderBy(selector => selector, StringComparer.Ordinal)
                .ToArray();
            var activeTextContractKinds = textContractReport.Issues
                .Where(issue => issue.Suppressed == false)
                .Select(issue => issue.Kind)
                .OrderBy(kind => kind, StringComparer.Ordinal)
                .ToArray();
            var suppressedTextContractKinds = textContractReport.Issues
                .Where(issue => issue.Suppressed)
                .Select(issue => issue.Kind)
                .OrderBy(kind => kind, StringComparer.Ordinal)
                .ToArray();
            var cases = new List<Dictionary<string, object>>();
            cases.AddRange(VmAutomationUssAncestorDefaultResetAuditor.RunSelfTests());
            cases.AddRange(VmAutomationUssBackgroundScaleModeAuditor.RunSelfTests());
            cases.AddRange(VmAutomationUssGeneratedChildNaturalSizeAuditor.RunSelfTests());
            cases.AddRange(VmAutomationUssGeneratedChildStyleOwnershipAuditor.RunSelfTests());
            cases.AddRange(VmAutomationUssUnboundedFlexShrinkAuditor.RunSelfTests());
            cases.AddRange(VmAutomationUssAuthoredContentNaturalSizeAuditor.RunSelfTests());
            cases.AddRange(VmAutomationUssVariantFamilyContract.RunSelfTests());
            cases.AddRange(VmAutomationUssRedundantComponentClassAuditor.RunSelfTests());
            cases.AddRange(VmAutomationUssSharedClassDeclarationAuditor.RunSelfTests());
            cases.AddRange(VmAutomationUssTemplateInstanceContractSelfTests.Run());
            cases.AddRange(
                VmAutomationUssSingleConsumerInstanceDeclarationAuditor.RunSelfTests());
            cases.AddRange(VmUiButtonFeedbackSelfTests.Run());
            cases.AddRange(VmUiButtonContrastSelfTests.Run());
            cases.AddRange(VmUiControlSkinSelfTests.Run());
            AddSelfTestCase(cases, "display-only visibility classes are rejected",
                displayClassSelectors.SequenceEqual(new[]
                {
                    ".badge.hidden", ".card.runtime-visible .badge",
                    ".card.show-badge .badge"
                }) && displayClassReport.Issues.All(issue =>
                    issue.Kind == "programmatic-display-class" && issue.IsError));
            AddSelfTestCase(cases, "multi-property selected state and layout classes remain valid",
                displayClassReport.Issues.All(issue =>
                    issue.Selector.Contains("selected", StringComparison.Ordinal) == false &&
                    issue.Selector != ".layout"));

            AddSelfTestCase(cases, "fully inlineable single class is an error",
                errorTokens.Contains("single"));
            AddSelfTestCase(cases, "fully inlineable single ID is an error",
                errorTokens.Contains("Unique"));
            AddSelfTestCase(cases, "inlineable simple ID with relational contract is an error",
                errorTokens.Contains("IdContainer"));
            AddSelfTestCase(cases, "single relational ID target warns",
                activeTokens.Contains("UniqueChild"));
            AddSelfTestCase(cases, "pseudo ID contract passes",
                activeTokens.Contains("interactive-id") == false);
            AddSelfTestCase(cases, "zero-consumer selector is outside the single-use gate",
                activeTokens.Contains("unused") == false);
            AddSelfTestCase(cases, "shared class passes", activeTokens.Contains("shared") == false);
            AddSelfTestCase(cases, "pseudo contract passes", activeTokens.Contains("interactive") == false);
            AddSelfTestCase(cases, "runtime class passes", activeTokens.Contains("runtime-state") == false);
            AddSelfTestCase(cases, "single named relational anchor warns", activeTokens.Contains("container"));
            AddSelfTestCase(cases, "single relational target warns", activeTokens.Contains("child"));
            AddSelfTestCase(cases, "reasoned suppression is reported as suppressed",
                suppressedTokens.SequenceEqual(new[] { "suppressed" }));
            AddSelfTestCase(cases, "inlineable selector suppression is rejected",
                errorTokens.Contains("inlineable-suppression") &&
                report.Issues.Single(issue => issue.Token == "inlineable-suppression")
                    .Suppressed == false);
            AddSelfTestCase(cases, "inlineable selector errors are counted separately",
                report.ErrorCount == 5 && report.WarningCount == 3);
            AddSelfTestCase(cases, "active finding set is exact",
                activeTokens.SequenceEqual(
                    new[] { "IdContainer", "Unique", "UniqueChild", "child", "container",
                        "container", "inlineable-suppression", "single" }));
            AddSelfTestCase(cases, "same winning theme value warns",
                activeRedundantSelectors.SequenceEqual(
                    new[] { ".control-duplicate", ".duplicate", ".initial-default", ".initial-margin" }));
            AddSelfTestCase(cases, "Button theme resets are not engine-initial duplicates",
                duplicateReport.Issues.All(issue => issue.Selector != ".button-reset"));
            AddSelfTestCase(cases, "Toggle theme resets are not engine-initial duplicates",
                duplicateReport.Issues.All(issue => issue.Selector != ".toggle-reset"));
            AddSelfTestCase(cases, "shared reset retains the control theme boundary",
                duplicateReport.Issues.All(issue => issue.Selector != ".mixed-reset"));
            AddSelfTestCase(cases, "indexed control theme duplicates still warn",
                duplicateReport.Issues.Any(issue => issue.Selector == ".control-duplicate" &&
                    issue.StylesheetRules.All(source => Equals(source["sourceKind"], "stylesheet"))));
            AddSelfTestCase(cases, "Unity engine initial value warns",
                duplicateReport.Issues.Any(issue =>
                    issue.Selector == ".initial-default" &&
                    issue.StylesheetRules.Any(source =>
                        source.TryGetValue("sourceKind", out var sourceKind) &&
                        Equals(sourceKind, "initial-style"))));
            AddSelfTestCase(cases, "engine initial margin warns",
                duplicateReport.Issues.Any(issue =>
                    issue.Selector == ".initial-margin" &&
                    issue.Property == "margin-top" &&
                    issue.StylesheetRules.Any(source =>
                        source.TryGetValue("sourceKind", out var sourceKind) &&
                        Equals(sourceKind, "initial-style"))));
            AddSelfTestCase(cases, "different-value override passes",
                activeRedundantSelectors.Contains(".different") == false);
            AddSelfTestCase(cases, "reasoned redundant-declaration suppression is retained",
                suppressedRedundantSelectors.SequenceEqual(new[] { ".suppressed-duplicate" }));
            AddSelfTestCase(cases, "only off-grid structural declarations warn",
                activePixelGridSelectors.SequenceEqual(new[] { ".grid-fail" }));
            AddSelfTestCase(cases, "off-grid shorthand values are retained",
                pixelGridReport.Issues.Single(issue => issue.Suppressed == false)
                    .OffGridDeclarations.Keys.OrderBy(value => value, StringComparer.Ordinal)
                    .SequenceEqual(new[] { "padding", "top" }));
            AddSelfTestCase(cases, "reasoned pixel-grid suppression is retained",
                suppressedPixelGridSelectors.SequenceEqual(new[] { ".grid-suppressed" }));
            AddSelfTestCase(cases,
                "advanced generator without auto size warns independently",
                activeTextContractKinds.Contains(
                    "advanced-text-generator-without-auto-size"));
            AddSelfTestCase(cases,
                "text align on shrink-wrapped centered label warns independently",
                activeTextContractKinds.Contains(
                    "ineffective-text-align-on-shrink-wrapped-label"));
            AddSelfTestCase(cases,
                "inheritable only-child text styles warn independently",
                activeTextContractKinds.Contains(
                    "inheritable-text-style-on-only-child-label"));
            AddSelfTestCase(cases, "text contract active finding set is exact",
                activeTextContractKinds.SequenceEqual(new[]
                {
                    "advanced-text-generator-without-auto-size",
                    "fixed-font-size-with-auto-size",
                    "ineffective-text-align-on-shrink-wrapped-label",
                    "inheritable-text-style-on-only-child-label"
                }));
            AddSelfTestCase(cases, "fixed font size with auto size is an error",
                textContractReport.Issues.Any(issue =>
                    issue.Kind == "fixed-font-size-with-auto-size" && issue.IsError));
            AddSelfTestCase(cases,
                "advanced generator with auto size passes",
                textContractReport.Issues.All(issue =>
                    issue.Selector != ".auto-sized-text"));
            AddSelfTestCase(cases,
                "text align with an explicit box passes",
                textContractReport.Issues.All(issue =>
                    issue.Selector != ".boxed-text"));
            AddSelfTestCase(cases,
                "inheritable text style with a sibling passes",
                textContractReport.Issues.All(issue =>
                    issue.Selector != ".sibling-text"));
            AddSelfTestCase(cases,
                "reasoned text style contract suppression is retained",
                suppressedTextContractKinds.SequenceEqual(new[]
                {
                    "advanced-text-generator-without-auto-size"
                }));
            AddSelfTestCase(cases, "none and initial cannot override a loaded shared font",
                fontResetIssues.Select(issue => issue.Selector).SequenceEqual(new[]
                {
                    ".unity-text-element.font-initial",
                    ".unity-text-element.font-none"
                }) && fontResetIssues.All(issue => issue.IsError &&
                    issue.AuthoredUsageCount == 1));
            AddSelfTestCase(cases, "explicit and inline fonts remain valid",
                fontResetIssues.All(issue => issue.Selector !=
                    ".unity-text-element.font-explicit" && issue.Selector !=
                    ".unity-text-element.font-inline"));
            AddSelfTestCase(cases, "a reset without a loaded shared font is outside the rule",
                noSharedFontReport.Issues.All(issue =>
                    issue.Kind != "shared-font-definition-reset"));
            var compoundSelectorParsed = TryParseSimpleSelector(
                "Label.primary.secondary#Target", out var compoundSelector);
            var compoundElement = new UssAuthoredElement
            {
                TypeName = "Label",
                Name = "Target"
            };
            compoundElement.Classes.Add("primary");
            compoundElement.Classes.Add("secondary");
            AddSelfTestCase(cases, "compound selectors keep every class and ID",
                compoundSelectorParsed && compoundSelector.ClassNames.SequenceEqual(new[]
                {
                    "primary", "secondary"
                }) && compoundSelector.Id == "Target" &&
                compoundSelector.Matches(compoundElement));
            compoundElement.Classes.Remove("secondary");
            AddSelfTestCase(cases, "compound selectors reject a missing class",
                compoundSelectorParsed && !compoundSelector.Matches(compoundElement));
            AddSelfTestCase(cases, "authoring policy errors are exact",
                authoringPolicyKinds.SequenceEqual(new[]
                {
                    "box-shorthand", "box-shorthand", "empty-selector-block",
                    "grouped-selector-list"
                }) && authoringPolicyReport.ErrorCount == 4);

            return new Dictionary<string, object>
            {
                { "passed", cases.All(testCase => (bool)testCase["passed"]) },
                { "cases", cases },
                { "activeTokens", activeTokens },
                { "suppressedTokens", suppressedTokens },
                { "activeRedundantSelectors", activeRedundantSelectors },
                { "suppressedRedundantSelectors", suppressedRedundantSelectors },
                { "activePixelGridSelectors", activePixelGridSelectors },
                { "suppressedPixelGridSelectors", suppressedPixelGridSelectors },
                { "activeTextContractKinds", activeTextContractKinds },
                { "suppressedTextContractKinds", suppressedTextContractKinds }
            };
        }


        private static void AppendSelfTestRules(UssCascadeDocument document,
            IEnumerable<UssRule> rules, int origin)
        {
            foreach (var rule in rules)
            {
                document.LoadedAssetPaths.Add(rule.AssetPath);
                foreach (var selectorText in rule.Selectors)
                {
                    TryParseSimpleSelector(selectorText, out var selector);
                    document.Rules.Add(new UssCascadeRule
                    {
                        Rule = rule,
                        SelectorText = selectorText,
                        Selector = selector,
                        Origin = origin,
                        SourceOrder = document.NextSourceOrder()
                    });
                }
            }
        }


        private static void AddSelfTestCase(ICollection<Dictionary<string, object>> cases, string name,
            bool passed)
        {
            cases.Add(new Dictionary<string, object>
            {
                { "name", name },
                { "passed", passed }
            });
        }

    }
}
#endif
