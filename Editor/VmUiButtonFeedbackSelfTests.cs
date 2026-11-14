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
            Case("native scale-only feedback passes", baseline + ".feedback:hover { scale: 1.025 1.025; }.feedback:active { scale: 0.96 0.96; }", "feedback", null);
            Case("inline scale defeats authored scale feedback", baseline + ".feedback:hover { scale: 1.025 1.025; }.feedback:active { scale: 0.96 0.96; }", "feedback", b => b.style.scale = new Scale(Vector3.one), "hover", "press");
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
            cases.Add(SharedAncestorState(theme));
#if UNITY_6000_6_OR_NEWER
            cases.Add(StructuralScope(theme, baseline));
#endif
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
                        findings.All(issue => issue.IsError && issue.NativeStateScales?.Count == 5) && button.panel == null &&
                        (button.enabledInHierarchy ? coverage.Buttons == 1 && coverage.StateUpdates == 5 : coverage.DisabledButtons == 1);
                    cases.Add(new Dictionary<string, object> { { "name", name }, { "passed", passed },
                        { "expected", expected }, { "actual", actual } });
                }
                finally { UnityEngine.Object.DestroyImmediate(sheet); }
            }
        }

        private static Dictionary<string, object> SharedAncestorState(ThemeStyleSheet theme)
        {
            var sheet = ScriptableObject.CreateInstance<StyleSheet>();
            var root = new VisualElement();
            root.AddToClassList("group");
            var left = new Button();
            var right = new Button();
            root.Add(left);
            root.Add(right);
            int ancestorResolutions = 0;
            root.RegisterCallback<CustomStyleResolvedEvent>(evt =>
            {
                if (evt.target == root) ancestorResolutions++;
            });
            bool passed;
            int afterHover, afterStates;
            try
            {
                Import(sheet, ".group { --feedback-state: 0; }.group:hover { --feedback-state: 1; }" +
                    ".group:hover Button { color: red; }Button:hover { background-color: gray; }" +
                    "Button:focus { color: yellow; }Button:active { background-color: white; }");
                root.styleSheets.Add(sheet);
                using (var panel = new VmUiFeedbackPanel(root, theme))
                {
                    panel.SetState(left, true, false, false);
                    afterHover = ancestorResolutions;
                    bool initial = left.resolvedStyle.color == Color.red;
                    panel.SetState(left, true, false, true);
                    panel.SetState(left, true, true, true);
                    panel.SetState(left, true, false, true);
                    afterStates = ancestorResolutions;
                    panel.SetState(right, true, false, false);
                    bool switched = right.resolvedStyle.color == Color.red && left.resolvedStyle.color == Color.red;
                    panel.SetState(right, false, false, false);
                    passed = initial && switched && afterHover > 0 && afterStates == afterHover &&
                        ancestorResolutions > afterStates;
                }
                passed &= left.panel == null && right.panel == null;
            }
            finally { UnityEngine.Object.DestroyImmediate(sheet); }
            return new Dictionary<string, object>
            {
                { "name", "shared ancestor hover remains continuous across focused press and release" },
                { "passed", passed }, { "ancestorResolutionsAfterHover", afterHover },
                { "ancestorResolutionsAfterStates", afterStates }, { "ancestorResolutionsAfterLeave", ancestorResolutions }
            };
        }

#if UNITY_6000_6_OR_NEWER
        private static Dictionary<string, object> StructuralScope(ThemeStyleSheet theme, string baseline)
        {
            var selected = ScriptableObject.CreateInstance<StyleSheet>();
            var other = ScriptableObject.CreateInstance<StyleSheet>();
            var root = new VisualElement();
            var direct = new Button { name = "state-only" };
            direct.AddToClassList("scoped");
            direct.AddToClassList("feedback");
            direct.style.backgroundColor = Color.black;
            var disabled = new Button { name = "disabled" };
            disabled.AddToClassList("scoped");
            disabled.SetEnabled(false);
            var descendant = new Button { name = "descendant" };
            var icon = new VisualElement();
            icon.AddToClassList("scope-icon");
            descendant.Add(icon);
            var group = new VisualElement();
            group.AddToClassList("scope-parent");
            var ancestor = new Button { name = "ancestor" };
            group.Add(ancestor);
            var unrelated = new Button { name = "unrelated" };
            var structuralParent = new VisualElement();
            structuralParent.AddToClassList("structural-parent");
            var conditional = new Button { name = "conditional" };
            conditional.AddToClassList("conditional");
            structuralParent.Add(conditional);
            var wrongParent = new Button { name = "wrong-parent" };
            wrongParent.AddToClassList("conditional");
            root.Add(direct); root.Add(disabled); root.Add(descendant);
            root.Add(group); root.Add(unrelated); root.Add(structuralParent); root.Add(wrongParent);
            try
            {
                Import(selected, ".scoped:hover { background-color: white; }.scoped:active { background-color: gray; }" +
                    ".scope-icon { color: white; }.scope-parent { opacity: 0.9; }" +
                    ".structural-parent > .conditional:hover { color: white; }");
                Import(other, baseline);
                root.styleSheets.Add(selected); root.styleSheets.Add(other);
                var coverage = new VmUiFeedbackCoverage();
                var findings = new List<VmAutomationUssStyleAuditIssue>();
                string[] actual;
                using (var panel = new VmUiFeedbackPanel(root, theme))
                {
                    var nodes = new List<VisualElement> { root, direct, disabled, descendant, icon, group,
                        ancestor, unrelated, structuralParent, conditional, wrongParent };
                    actual = new VmUiFeedbackStyleScope(new[] { selected }, coverage).SelectButtons(nodes, coverage)
                        .Select(button => button.name).OrderBy(name => name, StringComparer.Ordinal).ToArray();
                    VmAutomationUssButtonFeedbackAuditor.AuditButton(panel, direct, "scope fixture", coverage, findings.Add);
                    VmAutomationUssButtonFeedbackAuditor.AuditButton(panel, disabled, "scope fixture", coverage, findings.Add);
                }
                string[] expected = { "ancestor", "conditional", "descendant", "disabled", "state-only" };
                string[] missing = findings.Select(issue => issue.Kind).OrderBy(kind => kind, StringComparer.Ordinal).ToArray();
                return new Dictionary<string, object>
                {
                    { "name", "native structural scope retains state-only and relational owners" },
                    { "passed", actual.SequenceEqual(expected) && coverage.StyleMatchQueries == 6 &&
                        coverage.UnaffectedButtons == 2 && coverage.DisabledButtons == 1 && direct.panel == null &&
                        missing.SequenceEqual(new[] { "button-feedback-missing-hover", "button-feedback-missing-press" }) },
                    { "expected", expected }, { "actual", actual }, { "nativeQueries", coverage.StyleMatchQueries },
                    { "unaffectedButtons", coverage.UnaffectedButtons }, { "retainedMissingFeedback", missing }
                };
            }
            finally { UnityEngine.Object.DestroyImmediate(selected); UnityEngine.Object.DestroyImmediate(other); }
        }
#endif

        internal static void Import(StyleSheet target, string text)
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
