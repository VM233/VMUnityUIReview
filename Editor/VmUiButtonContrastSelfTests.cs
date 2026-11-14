using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace VMUnityAutomation.Editor
{
    internal static class VmUiButtonContrastSelfTests
    {
        internal static IEnumerable<Dictionary<string, object>> Run()
        {
            string package = PackageInfo.FindForAssembly(typeof(VmUiButtonContrastSelfTests).Assembly).assetPath;
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(package + "/Editor/Fixtures/Default Theme.tss");
            const string baseline = "#host { background-color: white; }Button { background-color: black; color: white; border-width: 0; }";
            const string press = "Button:active { background-color: #FDC052; color: black; border-width: 2px; border-color: black; }";
            var cases = new List<Dictionary<string, object>>();
            Case("original white hover disappears into its white host", baseline +
                "Button:hover { background-color: white; color: black; }" + press, null, "button-contrast-boundary");
            Case("near-white hover also loses its host boundary", baseline +
                "Button:hover { background-color: #eeeeee; color: black; }" + press, null, "button-contrast-boundary");
            Case("changed but unreadable hover text is rejected", baseline +
                "Button:hover { background-color: #444444; color: #555555; }" + press, null, "button-contrast-text");
            Case("shared dark hover and gold press stay readable", baseline +
                "Button:hover { background-color: #404040; }" + press, null);
            Case("border preserves a light hover boundary", baseline +
                "Button:hover { background-color: white; color: black; border-width: 2px; border-color: black; }" + press, null);
            Case("alpha hover is composited against the host", baseline +
                "Button:hover { background-color: rgba(0,0,0,0.1); color: black; }" + press, null, "button-contrast-boundary");
            Case("native disabled control does not advertise hover", baseline,
                button => button.SetEnabled(false));
            Type groupType = typeof(VisualElement).Assembly.GetType("UnityEngine.UIElements.ToggleButtonGroup");
            if (groupType != null)
            {
                Case("selected hover cannot hide unreadable text", baseline +
                    "Button:hover { background-color: #404040; }" + press +
                    "Button:checked:hover { background-color: #777777; color: #777777; }", Group,
                    "button-contrast-text");
                Case("selected and unselected dark controls retain readable states", baseline +
                    "Button:hover { background-color: #404040; }" + press, Group);
            }
            return cases;

            void Group(Button button)
            {
                VisualElement root = button.hierarchy.parent;
                button.RemoveFromHierarchy();
                var group = (VisualElement)Activator.CreateInstance(groupType);
                root.Add(group);
                group.Add(button);
            }

            void Case(string name, string css, Action<Button> configure, params string[] expected)
            {
                var sheet = ScriptableObject.CreateInstance<StyleSheet>();
                var root = new VisualElement { name = "host" };
                var button = new Button { name = "action", text = "Action" };
                root.Add(button);
                var coverage = new VmUiFeedbackCoverage();
                var issues = new List<VmAutomationUssStyleAuditIssue>();
                try
                {
                    VmUiButtonFeedbackSelfTests.Import(sheet, css);
                    root.styleSheets.Add(sheet);
                    configure?.Invoke(button);
                    using (var panel = new VmUiFeedbackPanel(root, theme))
                        VmAutomationUssButtonFeedbackAuditor.AuditButton(panel, button, "contrast fixture", coverage, issues.Add);
                    string[] actual = issues.Select(issue => issue.Kind).Distinct().OrderBy(value => value).ToArray();
                    bool measured = button.enabledSelf ? coverage.TextContrastChecks > 0 &&
                        coverage.BoundaryContrastChecks > 0 : coverage.DisabledButtons == 1;
                    cases.Add(new Dictionary<string, object>
                    {
                        { "name", name }, { "passed", measured && button.panel == null &&
                            actual.SequenceEqual(expected.OrderBy(value => value)) && issues.All(issue => issue.IsError) },
                        { "expected", expected }, { "actual", actual },
                        { "textChecks", coverage.TextContrastChecks }, { "boundaryChecks", coverage.BoundaryContrastChecks }
                    });
                }
                finally { UnityEngine.Object.DestroyImmediate(sheet); }
            }
        }
    }
}
