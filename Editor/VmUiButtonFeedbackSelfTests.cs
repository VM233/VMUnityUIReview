using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    internal static class VmUiButtonFeedbackSelfTests
    {
        internal static IEnumerable<Dictionary<string, object>> Run()
        {
            string packagePath = PackageInfo.FindForAssembly(typeof(VmUiButtonFeedbackSelfTests).Assembly).assetPath;
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(packagePath + "/Editor/Fixtures/Default Theme.tss");
            if (theme == null) throw new InvalidOperationException("The native feedback default-theme fixture is unavailable.");
            const string baseline = ".feedback { background-color: black; color: white; border-left-width: 0; border-top-width: 0; border-right-width: 0; border-bottom-width: 0; }";
            var cases = new List<Dictionary<string, object>>();
            Case("missing opaque feedback is an error", baseline, "feedback", null, "hover", "press");
            Case("hover alone cannot qualify press", baseline + ".feedback:hover { background-color: gray; }", "feedback", null, "press");
            Case("identical hover and press fail", baseline + ".feedback:hover { background-color: gray; }.feedback:active { background-color: gray; }", "feedback", null, "press");
            Case("ID cascade overriding both states fails", baseline + "#host #action.feedback { background-color: black; }.feedback:hover { background-color: gray; }.feedback:active { background-color: white; }", "feedback", null, "hover", "press");
            Case("inline blocking both states fails", baseline + ".feedback:hover { background-color: gray; }.feedback:active { background-color: white; }", "feedback", b => b.style.backgroundColor = Color.black, "hover", "press");
            Case("persistent focus cannot qualify press", baseline + ".feedback:focus { background-color: gray; }", "feedback", null, "hover", "press");
            Case("later winning state declaration is measured", baseline + ".feedback:hover { background-color: gray; }.feedback:active { background-color: white; }.feedback:active { background-color: gray; }", "feedback", null, "press");
            Case("complete effective feedback passes", baseline + ".feedback:hover { background-color: gray; }.feedback:active { background-color: white; }", "feedback", null);
            Case("press may restore normal paint", baseline + ".feedback:hover { background-color: gray; }.feedback:active { background-color: black; }", "feedback", null);
            Case("native default-theme feedback passes", "", "", null);
            Case("computed two-pixel border feedback passes without layout",
                baseline + ".feedback { border-left-width: 2px; border-left-color: transparent; }.feedback:hover { border-left-color: white; }.feedback:active { border-left-color: yellow; }", "feedback", null);
            Case("zero-width border colors cannot qualify feedback",
                baseline + ".feedback:hover { border-left-color: white; }.feedback:active { border-left-color: yellow; }", "feedback", null, "hover", "press");
            Case("descendant paint feedback passes", baseline + ".icon { background-color: black; }.feedback:hover .icon { background-color: gray; }.feedback:active .icon { background-color: white; }", "feedback", b =>
            {
                var icon = new VisualElement();
                icon.style.width = 20;
                icon.style.height = 20;
                icon.AddToClassList("icon");
                b.Add(icon);
            });
            Case("disabled button does not advertise hover or press", baseline, "feedback", b => b.SetEnabled(false));
            return cases;

            void Case(string name, string css, string className, Action<Button> configure, params string[] expected)
            {
                StyleSheet sheet = ScriptableObject.CreateInstance<StyleSheet>();
                var root = new VisualElement { name = "host" };
                var button = new Button { name = "action", text = "Action" };
                var findings = new List<VmAutomationUssStyleAuditIssue>();
                var coverage = new VmUiFeedbackCoverage();
                try
                {
                    Import(sheet, css);
                    root.styleSheets.Add(sheet);
                    root.Add(button);
                    if (className.Length > 0) button.AddToClassList(className);
                    configure?.Invoke(button);
                    using (var panel = new VmUiFeedbackPanel(root, theme))
                        VmAutomationUssButtonFeedbackAuditor.AuditButton(panel, button, "native fixture", coverage, findings.Add);
                    string[] actual = findings.Select(issue => issue.Kind.Replace("button-feedback-missing-", ""))
                        .OrderBy(value => value, StringComparer.Ordinal).ToArray();
                    bool passed = actual.SequenceEqual(expected.OrderBy(value => value, StringComparer.Ordinal)) &&
                        findings.All(issue => issue.IsError) && button.panel == null &&
                        (button.enabledInHierarchy ? coverage.Buttons == 1 && coverage.StateUpdates == 5 : coverage.DisabledButtons == 1);
                    cases.Add(new Dictionary<string, object> { { "name", name }, { "passed", passed },
                        { "expected", expected }, { "actual", actual } });
                }
                finally { UnityEngine.Object.DestroyImmediate(sheet); }
            }
        }

        private static void Import(StyleSheet target, string text)
        {
            Type importerType = VmAutomationUIToolkitInitialStyleComparer.RequireUniqueLoadedType(
                "UnityEditor.UIElements.StyleSheets.StyleSheetImporterImpl");
            const BindingFlags members = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            ConstructorInfo constructor = importerType.GetConstructor(members, null, Type.EmptyTypes, null)
                ?? throw new MissingMethodException(importerType.FullName, ".ctor()");
            MethodInfo import = importerType.GetMethod("Import", members, null,
                new[] { typeof(StyleSheet), typeof(string) }, null)
                ?? throw new MissingMethodException(importerType.FullName, "Import(StyleSheet,string)");
            import.Invoke(constructor.Invoke(null), new object[] { target, text });
        }
    }
}
