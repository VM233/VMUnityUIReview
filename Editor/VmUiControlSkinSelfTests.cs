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
                ".project-skin:checked > .unity-toggle__input > .unity-toggle__checkmark { background-image: none; background-color: yellow; }",
                root => { var toggle = new Toggle(); toggle.AddToClassList("project-skin"); toggle.SetValueWithoutNotify(false); root.Add(toggle); return toggle; },
                (findings, _, __) => findings.Any(finding => finding.Token == "background-color"));
            Case("inline paint attribution is retained", "", root =>
            {
                var button = new Button(); button.style.backgroundColor = Color.magenta; root.Add(button); return button;
            }, (_, origin, control) => origin.Get(control, "background-color", false) == "inline");
            Case("imported UXML inline paint retains its producer", "", root =>
            {
                var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(package + "/Editor/Fixtures/Inline Skin.uxml");
                if (asset == null) throw new InvalidOperationException("Native inline skin fixture is missing.");
                root.Add(asset.CloneTree()); return root.Q<Button>();
            }, (findings, origin, control) => findings.Count == 0 &&
                origin.Get(control, "background-color", false) == "UXML inline");
            Case("inherited project color retains its producer", ".project-skin { color: green; }", root =>
            {
                root.AddToClassList("project-skin"); var label = new Label("Caption"); root.Add(label); return label;
            }, (_, origin, control) => !VmUiNativeStyleOrigin.IsUnity(origin.Get(control, "color", true)));
            FieldOwners();
            RuleCacheTransitions();
            WorkLifetime(0);
            WorkLifetime(1);
            WorkLifetime(2);
            return results;

            void WorkLifetime(int outcome)
            {
                var root = new VisualElement();
                var coverage = new VmUiFeedbackCoverage { Cooperative = true };
                var errors = new List<string>();
                var work = new VmUiFeedbackWork(coverage, Observe().GetEnumerator(), errors.Add);
                bool pending = !work.Finished && !coverage.Complete;
                work.Advance();
                pending &= root.panel != null && !work.Finished && !coverage.Complete;
                if (outcome == 1) { work.Cancel("fixture cancellation"); work.Cancel("duplicate cancellation"); }
                else work.Drain();
                results.Add(new Dictionary<string, object>
                {
                    { "name", outcome == 0 ? "complete native observation publishes only after panel retirement" :
                        outcome == 2 ? "native observation failure retires its panel without publication" :
                        "pending native observation cancels once and retires its panel" },
                    { "passed", pending && work.Finished && coverage.Complete == (outcome == 0) &&
                        root.panel == null && errors.Count == (outcome == 0 ? 0 : 1) },
                    { "errors", errors.ToArray() }
                });

                IEnumerable<object> Observe()
                {
                    using (var panel = new VmUiFeedbackPanel(root, theme))
                    {
                        panel.SetState(root, false, false, false);
                        yield return null;
                        if (outcome == 2) throw new InvalidOperationException("fixture observation failure");
                    }
                }
            }

            void RuleCacheTransitions()
            {
                var sheet = ScriptableObject.CreateInstance<StyleSheet>();
                var button = new Button(); button.AddToClassList("project-skin");
                var coverage = new VmUiFeedbackCoverage();
                try
                {
                    VmUiButtonFeedbackSelfTests.Import(sheet, ".project-skin:enabled { background-color: magenta; }");
                    button.styleSheets.Add(sheet);
                    using (var panel = new VmUiFeedbackPanel(button, theme))
                    {
                        var origin = new VmUiNativeStyleOrigin(coverage, panel.Theme, themePath);
                        bool correct = true;
                        var observed = new List<Dictionary<string, object>>();
                        for (int state = 0; state < 3; state++)
                        {
                            button.SetEnabled(state != 1);
                            panel.SetState(button, false, false, false);
                            var fresh = new VmUiNativeStyleOrigin(new VmUiFeedbackCoverage(), panel.Theme, themePath);
                            observed.Add(new Dictionary<string, object>
                            {
                                { "state", state }, { "paint", button.resolvedStyle.backgroundColor.ToString() },
                                { "cachedOrigin", origin.Get(button, "background-color", false) },
                                { "freshOrigin", fresh.Get(button, "background-color", false) }
                            });
                            for (int sample = 0; sample < 2; sample++)
                                correct &= VmUiNativeStyleOrigin.IsUnity(origin.Get(button, "background-color", false)) == (state == 1);
                        }
                        results.Add(new Dictionary<string, object>
                        {
                            { "name", "native rule-cache identity preserves enabled-disabled-return paint provenance" },
                            { "passed", correct && coverage.StyleOriginCacheHits == 6 && coverage.StyleMatchQueries == 3 },
                            { "observed", observed },
                            { "cacheHits", coverage.StyleOriginCacheHits }, { "nativeQueries", coverage.StyleMatchQueries }
                        });
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(sheet); }
            }

            void FieldOwners()
            {
                var root = new VisualElement();
                for (int index = 0; index < 129; index++) root.Add(new VisualElement { focusable = true });
                var toggle = new Toggle { value = true }; var radio = new RadioButton { value = true };
                var custom = new RootField();
                root.Add(toggle); root.Add(radio); root.Add(new Slider()); root.Add(custom);
                var coverage = new VmUiFeedbackCoverage();
                var findings = new List<VmAutomationUssStyleAuditIssue>();
                using (var panel = new VmUiFeedbackPanel(root, theme))
                {
                    coverage.ControlSkins = new VmUiControlSkinAudit(coverage, panel.Theme, themePath);
                    panel.SetState(root, false, false, false);
                    VmAutomationUssButtonFeedbackAuditor.AuditFieldSkins(panel,
                        VmAutomationUssButtonFeedbackAuditor.Descendants(root, 256), "native field fixture", coverage, findings.Add);
                }
                results.Add(new Dictionary<string, object>
                {
                    { "name", "native field owners cover both boolean values without treating focusable containers as fields" },
                    { "passed", coverage.StateUpdates == 64 && toggle.value && radio.value && root.panel == null &&
                        findings.Any(finding => finding.Message.Contains("RadioButton:value=1:")) &&
                        findings.Any(finding => finding.Message.Contains("Toggle:value=1:input-focused") && finding.Token == "background-color") },
                    { "stateUpdates", coverage.StateUpdates }, { "findings", findings.Count }
                });
            }

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
                            { "findings", findings.Select(finding => finding.ToDictionary()).ToArray() },
                            { "nativePaint", VmAutomationUssButtonFeedbackAuditor.Descendants(root, 256).Select(node =>
                                new Dictionary<string, object>
                                {
                                    { "type", node.GetType().Name }, { "name", node.name },
                                    { "image", node.resolvedStyle.backgroundImage.ToString() },
                                    { "imageOrigin", origins.Get(node, "background-image", false) },
                                    { "color", node.resolvedStyle.color.ToString() },
                                    { "colorOrigin", origins.Get(node, "color", true) }
                                }).ToArray() } });
                    }
                    if (root.panel != null) throw new InvalidOperationException("Skin fixture leaked its native panel.");
                }
                finally { UnityEngine.Object.DestroyImmediate(sheet); }
            }
#else
            return results;
#endif
        }

        private sealed class RootField : BaseField<bool>
        {
            internal RootField() : base(null, new VisualElement())
            {
                this.Q(className: inputUssClassName).RemoveFromClassList(inputUssClassName);
                RemoveFromClassList(ussClassName);
            }
        }
    }
}
