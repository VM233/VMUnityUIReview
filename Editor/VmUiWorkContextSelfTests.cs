#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace VMUnityAutomation.Editor
{
    internal static class VmUiWorkContextSelfTests
    {
        internal static IEnumerable<Dictionary<string, object>> Run()
        {
            const string start = "<ui:UXML xmlns:ui='UnityEngine.UIElements'>";
            const string finish = "</ui:UXML>";
            string Activity(string name, string context) =>
                "<!-- ui-structure: {\"role\":\"activity\",\"state\":\"" + context + "\",\"owner\":\"Producer\"} -->" +
                "<ui:VisualElement name='" + name + "'><ui:Button text='Action'/></ui:VisualElement>";
            string Panel(string name, string body) => "<!-- ui-structure: {\"role\":\"panel\"} --><ui:VisualElement name='" + name + "'>" + body + "</ui:VisualElement>";
            string Tab(string name) => "<!-- ui-structure: {\"role\":\"tab\",\"target\":\"" + name + "\"} --><ui:Button/>";
            var split = start + Tab("CombatPage") + Tab("CraftPage") +
                Panel("CombatPage", Activity("Combat", "combat")) + Panel("CraftPage", Activity("Craft", "craft")) + finish;
            var dump = start + Tab("Page") + Panel("Page", Activity("Combat", "combat") + Activity("Craft", "craft")) + finish;
            yield return Case("separate functional pages match actual producers", split, "", "Producer", false);
            yield return Case("original mixed combat and crafting page fails despite section labels", dump, "mixed-functional-work-contexts", "Producer", false);
            yield return Case("wrong actual producer fails", split, "work-context-owner-mismatch", "OtherProducer", false);
            yield return Case("unlinked panel marker does not establish categorization", split.Replace(Tab("CraftPage"), ""), "work-context-page-missing", "Producer", false);
            yield return Case("empty marked work area cannot qualify", split.Replace("<ui:Button text='Action'/>", ""), "work-context-empty", "Producer", false);
            yield return Case("Foldout and headings do not establish a separate work page", start + Activity("Combat", "combat") + Activity("Craft", "craft") + finish, "work-context-page-missing", "Producer", false);
            yield return Case("renaming a declared context cannot hide mismatched producer work", split, "work-context-owner-mismatch", "Producer", true);

            Dictionary<string, object> Case(string name, string text, string expected, string owner, bool wrongContext)
            {
                var report = new VmAutomationUxmlLayoutAuditReport(100);
                var document = VmUiWorkContextAuthoringAudit.Parse("Assets/Work.uxml", text, report);
                var pages = new Dictionary<XElement, string>();
                foreach (string target in new[] { "Combat", "Craft" })
                    VmUiWorkContextAuthoringAudit.Validate(document, document.Names[target][0], owner,
                        wrongContext ? "unrelated" : target.ToLowerInvariant(), pages, report);
                return new Dictionary<string, object>
                {
                    { "name", name },
                    { "passed", expected == "" ? report.ErrorCount == 0 : report.Issues.Any(issue => issue.Kind == expected) },
                    { "findings", report.Issues.Select(issue => issue.Kind).ToArray() }
                };
            }
        }
    }
}
#endif
