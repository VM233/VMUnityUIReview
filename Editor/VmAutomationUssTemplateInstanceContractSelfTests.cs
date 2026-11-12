#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using static VMUnityAutomation.Editor.VmAutomationUssAuditContext;

namespace VMUnityAutomation.Editor
{
    internal static class VmAutomationUssTemplateInstanceContractSelfTests
    {
        internal static IReadOnlyList<Dictionary<string, object>> Run()
        {
            const string source = "<ui:UXML xmlns:ui='UnityEngine.UIElements'><ui:Label name='Hint' class='hint'/></ui:UXML>";
            const string host = "<ui:UXML xmlns:ui='UnityEngine.UIElements'><Style src='Styles.uss'/>" +
                                "<ui:Template name='Card' src='Leaf.uxml'/>" +
                                "<ui:Instance name='Open' template='Card' class='all'/>" +
                                "<ui:Instance name='Closed' template='Card' class='all'/></ui:UXML>";
            const string styles = "#Hint { display:none; } #Open #Hint { display:flex; }";
            var positive = Audit(styles, source, host);
            var ordinaryGeometry = Audit("#Hint { display:none; width:30px; } #Open #Hint { display:flex; }", source, host);
            var classTarget = Audit(".hint { display:none; } #Open .hint { display:flex; }", source, host);
            var noTemplate = Audit(styles, source, host.Replace("ui:Instance", "ui:VisualElement"));
            var oneInstance = Audit(styles, source, host.Replace("<ui:Instance name='Closed' template='Card' class='all'/>", ""));
            var allInstances = Audit("#Hint { display:none; } .all #Hint { display:flex; }", source, host);
            var wrongSource = Audit(styles, source, host.Replace("src='Leaf.uxml'", "src='Different.uxml'"));
            var missingStyle = Audit(styles, source, host.Replace("<Style src='Styles.uss'/>", ""));
            return new[]
            {
                Case("native named template ID display variants remain external", positive.ErrorCount == 0 && positive.WarningCount == 0),
                Case("native named template class display variants remain external", classTarget.ErrorCount == 0 && classTarget.WarningCount == 0),
                Case("template variants do not waive invariant geometry", ordinaryGeometry.ErrorCount > 0),
                Case("ordinary named ancestors do not qualify as template instances", noTemplate.ErrorCount > 0),
                Case("one template instance does not qualify a shared variant", oneInstance.ErrorCount > 0),
                Case("an all-instance anchor cannot qualify a variant", allInstances.ErrorCount > 0),
                Case("unrelated template sources cannot qualify a target", wrongSource.ErrorCount > 0),
                Case("a host must actually import its variant stylesheet", missingStyle.ErrorCount > 0)
            };
        }

        private static VmAutomationUssStyleAuditReport Audit(string styles, string source, string host)
        {
            var rules = VmAutomationUssStyleSheetParser.ParseStyleSheet("Assets/Styles.uss", styles);
            var index = new UssUsageIndex();
            VmAutomationUssStyleAuditor.CollectSelectorContracts(rules, index);
            index.AddDocument("Assets/Leaf.uxml", XDocument.Parse(source, LoadOptions.SetLineInfo));
            index.AddDocument("Assets/Host.uxml", XDocument.Parse(host, LoadOptions.SetLineInfo));
            foreach (var document in index.Documents)
                foreach (var element in document.Elements)
                {
                    if (!string.IsNullOrEmpty(element.Name)) index.AddIdUsage(element.Name, document.AssetPath, element.Line, element.Column);
                    foreach (string token in element.AuthoredClasses)
                        index.AddClassUsage(token, document.AssetPath, element.Line, element.Column, element.Name);
                }
            var report = new VmAutomationUssStyleAuditReport(100);
            VmAutomationUssStyleAuditor.AuditRules(rules, index, report, false);
            return report;
        }

        private static Dictionary<string, object> Case(string name, bool passed) =>
            new Dictionary<string, object> { ["name"] = name, ["passed"] = passed };
    }
}
#endif
