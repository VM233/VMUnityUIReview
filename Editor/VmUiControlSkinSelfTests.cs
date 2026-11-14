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
    internal static class VmUiControlSkinSelfTests
    {
        internal static IEnumerable<Dictionary<string, object>> Run()
        {
            var results = new List<Dictionary<string, object>>();
#if UNITY_6000_6_OR_NEWER
            string package = PackageInfo.FindForAssembly(typeof(VmUiControlSkinSelfTests).Assembly).assetPath;
            string themePath = package + "/Editor/Fixtures/Default Theme.tss";
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
            if (theme == null) throw new InvalidOperationException("Native skin fixture theme is missing.");
            Case("raw Foldout exposes its generated default arrow and hover text", "", root =>
            {
                var foldout = new Foldout { text = "Foldout", value = true };
                root.Add(foldout);
                return foldout.Q<Toggle>();
            }, (findings, _, __) => findings.Any(finding => finding.Token == "background-image") &&
                findings.Any(finding => finding.Token == "color"));
            Case("a skin marker without paint cannot qualify", "", root =>
            {
                var foldout = new Foldout { text = "Foldout" };
                foldout.AddToClassList("project-skin"); root.Add(foldout);
                return foldout.Q<Toggle>();
            }, (findings, _, __) => findings.Count > 0);
            const string basePaint = ".project-skin { background-color: black; border-left-width: 0; border-top-width: 0; border-right-width: 0; border-bottom-width: 0; }";
            Case("project paint with no engine paint passes", basePaint, root =>
            {
                var button = new Button(); button.AddToClassList("project-skin"); root.Add(button); return button;
            }, (findings, _, __) => findings.Count == 0);
            Case("a partial state skin cannot qualify the other toggle value",
                ".project-skin:not(:checked) > .unity-toggle__input > .unity-toggle__checkmark { background-image: none; background-color: yellow; }",
                root => { var toggle = new Toggle(); toggle.AddToClassList("project-skin"); toggle.SetValueWithoutNotify(true); root.Add(toggle); return toggle; },
                (findings, _, __) => findings.Any(finding => finding.Token == "background-image"));
            Case("inline paint attribution is retained", "", root =>
            {
                var button = new Button(); button.style.backgroundColor = Color.magenta; root.Add(button); return button;
            }, (_, origin, control) => origin.Get(control, "background-color", false) == "inline");
            Case("inherited project color retains its producer", ".project-skin { color: green; }", root =>
            {
                root.AddToClassList("project-skin"); var label = new Label("Caption"); root.Add(label); return label;
            }, (_, origin, control) => !VmUiNativeStyleOrigin.IsUnity(origin.Get(control, "color", true)));
            return results;

            void Case(string name, string css, Func<VisualElement, VisualElement> make,
                Func<List<VmAutomationUssStyleAuditIssue>, VmUiNativeStyleOrigin, VisualElement, bool> check)
            {
                var sheet = ScriptableObject.CreateInstance<StyleSheet>();
                var root = new VisualElement();
                try
                {
                    VmUiButtonFeedbackSelfTests.Import(sheet, css);
                    root.styleSheets.Add(sheet);
                    VisualElement control = make(root);
                    var coverage = new VmUiFeedbackCoverage();
                    var findings = new List<VmAutomationUssStyleAuditIssue>();
                    using (var panel = new VmUiFeedbackPanel(root, theme))
                    {
                        var skins = new VmUiControlSkinAudit(coverage, panel.Theme, themePath);
                        panel.SetState(control, true, false, false);
                        skins.Inspect(panel.Root,
                            VmAutomationUssButtonFeedbackAuditor.Descendants(root, VmUiFeedbackCoverage.MaximumNodes),
                            "native control-skin fixture", "hover", findings.Add);
                        var origins = new VmUiNativeStyleOrigin(coverage, panel.Theme, themePath);
                        bool passed = check(findings, origins, control);
                        results.Add(new Dictionary<string, object> { { "name", name }, { "passed", passed },
                            { "findings", findings.Select(finding => finding.ToDictionary()).ToArray() } });
                    }
                    if (root.panel != null) throw new InvalidOperationException("Skin fixture leaked its native panel.");
                }
                finally { UnityEngine.Object.DestroyImmediate(sheet); }
            }
#else
            return results;
#endif
        }
    }
}
