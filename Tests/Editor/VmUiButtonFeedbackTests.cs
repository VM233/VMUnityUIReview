using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.PackageManager;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
using UnityEngine;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor.Tests
{
    public sealed class VmUiButtonFeedbackTests
    {
        [Test]
        public void NativeCascadeFeedbackAndRetirementControls()
        {
            var cases = VmUiButtonFeedbackSelfTests.Run().ToArray();
            Assert.That(cases.Length, Is.GreaterThanOrEqualTo(12));
            foreach (var result in cases)
                Assert.That(result["passed"], Is.True, MiniJson.Serialize(result));
        }

#if UNITY_6000_6_OR_NEWER
        [Test]
        public void NativeStyleScopeIncludesStateOnlyAncestorsAndDescendantsWithoutUnrelatedButtons()
        {
            var selected = ScriptableObject.CreateInstance<StyleSheet>();
            var other = ScriptableObject.CreateInstance<StyleSheet>();
            string packagePath = PackageInfo.FindForAssembly(typeof(VmUiButtonFeedbackTests).Assembly).assetPath;
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(packagePath + "/Editor/Fixtures/Default Theme.tss");
            var root = new VisualElement();
            var direct = new Button { name = "state-only" };
            direct.AddToClassList("scoped");
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
            root.Add(direct);
            root.Add(disabled);
            root.Add(descendant);
            root.Add(group);
            root.Add(unrelated);
            try
            {
                VmUiButtonFeedbackSelfTests.Import(selected,
                    ".scoped:hover { background-color: white; }.scoped:active { background-color: gray; }" +
                    ".scope-icon { color: white; }.scope-parent { opacity: 0.9; }");
                VmUiButtonFeedbackSelfTests.Import(other, "Button { background-color: black; }");
                root.styleSheets.Add(selected);
                root.styleSheets.Add(other);
                var coverage = new VmUiFeedbackCoverage();
                using (var panel = new VmUiFeedbackPanel(root, theme))
                {
                    var nodes = new List<VisualElement> { root, direct, disabled, descendant, icon, group, ancestor, unrelated };
                    var actual = VmUiFeedbackStyleScope.SelectButtons(nodes,
                        new HashSet<StyleSheet> { selected }, coverage);
                    Assert.That(actual.Select(button => button.name),
                        Is.EquivalentTo(new[] { "state-only", "disabled", "descendant", "ancestor" }));
                    Assert.That(coverage.StyleMatchQueries, Is.EqualTo(nodes.Count));
                    Assert.That(coverage.UnaffectedButtons, Is.EqualTo(1));
                    var findings = new List<VmAutomationUssStyleAuditIssue>();
                    VmAutomationUssButtonFeedbackAuditor.AuditButton(panel, direct, "scope fixture", coverage, findings.Add);
                    Assert.That(findings.Select(issue => issue.Kind), Is.EquivalentTo(new[]
                        { "button-feedback-missing-hover", "button-feedback-missing-press" }),
                        "Scope admission must retain inline-overridden feedback failures.");
                    VmAutomationUssButtonFeedbackAuditor.AuditButton(panel, disabled, "scope fixture", coverage, findings.Add);
                    Assert.That(coverage.DisabledButtons, Is.EqualTo(1));
                }
                Assert.That(direct.panel, Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(selected);
                Object.DestroyImmediate(other);
            }
        }
#endif
    }
}
