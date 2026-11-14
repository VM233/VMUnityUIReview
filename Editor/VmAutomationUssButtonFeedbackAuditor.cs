using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace VMUnityAutomation.Editor
{
    internal static class VmAutomationUssButtonFeedbackAuditor
    {
        internal static VmUiFeedbackCoverage Audit(IEnumerable<string> uxmlPaths,
            IReadOnlyCollection<string> targetPaths, string themePath,
            Action<VmAutomationUssStyleAuditIssue> record, bool rejectDefaultControlAppearance = false)
        {
            var coverage = new VmUiFeedbackCoverage { ThemePath = themePath ?? "" };
            try
            {
                if (rejectDefaultControlAppearance)
                    coverage.ControlSkins = new VmUiControlSkinAudit(coverage);
                var hosts = ResolveHosts(uxmlPaths, targetPaths, themePath);
#if UNITY_6000_6_OR_NEWER
                VmUiFeedbackStyleScope targetScope = null;
#endif
                foreach (var host in hosts)
                {
                    string path = host.Path;
                    coverage.CheckBudget();
                    var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(path);
                    if (asset == null) throw new InvalidOperationException("Native UXML is unavailable: " + path);
                    VisualElement root = asset.CloneTree();
                    List<VisualElement> nodes = Descendants(root, VmUiFeedbackCoverage.MaximumNodes);
                    Button[] buttons = nodes.OfType<Button>().ToArray();
                    if (buttons.Length == 0 && coverage.ControlSkins == null) continue;
                    if (buttons.Length > VmUiFeedbackCoverage.MaximumButtons)
                        throw new InvalidOperationException("Native feedback host exceeds 128 Buttons: " + path);
                    var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
                    if (theme == null)
                        throw new InvalidOperationException("Feedback review requires one unambiguous imported PanelSettings ThemeStyleSheet. Complete the authoring roots: " + path);
                    using (var panel = new VmUiFeedbackPanel(root, theme))
                    {
                        if (coverage.ControlSkins != null)
                        {
                            coverage.ControlSkins.RegisterTheme(panel.Theme, themePath);
                            panel.SetState(root, false, false, false);
                            coverage.ControlSkins.Inspect(panel.Root, nodes, path, "neutral", record);
                            AuditFieldSkins(panel, nodes, path, coverage, record);
                        }
#if UNITY_6000_6_OR_NEWER
                        if (!host.WholeHost)
                            buttons = (targetScope ?? (targetScope = new VmUiFeedbackStyleScope(
                                LoadTargetSheets(targetPaths), coverage))).SelectButtons(nodes, coverage);
#endif
                        coverage.Hosts++;
                        foreach (Button button in buttons)
                        {
                            AuditButton(panel, button, path, coverage, record);
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                coverage.Complete = false;
                record(new VmAutomationUssStyleAuditIssue
                {
                    AssetPath = targetPaths.FirstOrDefault() ?? "",
                    Kind = "button-feedback-incomplete", Severity = "error", Selector = "native feedback scope",
                    Message = "Native button feedback coverage is incomplete: " + exception.GetBaseException().Message
                });
            }
            finally { coverage.Finish(); }
            return coverage;
        }

        private static void AuditFieldSkins(VmUiFeedbackPanel panel, List<VisualElement> nodes,
            string path, VmUiFeedbackCoverage coverage, Action<VmAutomationUssStyleAuditIssue> record)
        {
            VisualElement[] fields = nodes.Where(node => node is Toggle ||
                (node.focusable && !(node is Button))).ToArray();
            if (fields.Length > VmUiFeedbackCoverage.MaximumButtons)
                throw new InvalidOperationException("Native skin host exceeds 128 field state owners: " + path);
            foreach (VisualElement field in fields)
            {
                bool enabled = field.enabledSelf;
                var toggle = field as Toggle;
                bool originalValue = toggle != null && toggle.value;
                var sampled = Descendants(field, VmUiFeedbackCoverage.MaximumDescendants);
                VisualElement target = field;
                if (toggle != null)
                    target = field.Q(className: Toggle.inputUssClassName) ??
                        throw new InvalidOperationException("Native Toggle input is missing: " + field.name);
                try
                {
                    for (int value = 0; value < (toggle == null ? 1 : 2); value++)
                    {
                        toggle?.SetValueWithoutNotify(value == 1);
                        for (int state = 0; state < 5; state++)
                        {
                            field.SetEnabled(state != 4);
                            panel.SetState(target, state is 1 or 2, state == 2, state == 3, field);
                            coverage.StateUpdates++;
                            coverage.ControlSkins.Inspect(panel.Root, sampled, path,
                                field.GetType().Name + ":value=" + value + ":" +
                                new[] { "normal", "hover", "pressed", "focused", "disabled" }[state], record);
                        }
                    }
                }
                finally
                {
                    toggle?.SetValueWithoutNotify(originalValue);
                    field.SetEnabled(enabled);
                    panel.SetState(field, false, false, false);
                }
            }
        }

        private static FeedbackHost[] ResolveHosts(IEnumerable<string> uxmlPaths,
            IReadOnlyCollection<string> targets, string themePath)
        {
            string[] paths = uxmlPaths.Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.Ordinal).ToArray();
            if (paths.Length > VmUiFeedbackCoverage.MaximumHosts)
                throw new InvalidOperationException("Feedback authoring scope exceeds 128 indexed UXML assets.");
            var dependencies = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
            var templatePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in paths)
            {
                string[] values = AssetDatabase.GetDependencies(path, true);
                if (values.Length > 4096)
                    throw new InvalidOperationException("Feedback host dependency graph exceeds 4096 assets: " + path);
                dependencies.Add(path, new HashSet<string>(values, StringComparer.OrdinalIgnoreCase));
                foreach (string dependency in values)
                    if (dependency != path && dependency.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase))
                        templatePaths.Add(dependency);
            }
            bool themeChanged = !string.IsNullOrEmpty(themePath) &&
                AssetDatabase.GetDependencies(themePath, true).Intersect(targets, StringComparer.OrdinalIgnoreCase).Any();
            var uxmlTargets = new HashSet<string>(targets.Where(path =>
                path.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase)), StringComparer.OrdinalIgnoreCase);
            return paths.Where(path => !templatePaths.Contains(path) &&
                    (themeChanged || targets.Contains(path) || dependencies[path].Overlaps(targets)))
                .Select(path => new FeedbackHost { Path = path,
                    WholeHost = themeChanged || dependencies[path].Overlaps(uxmlTargets) })
                .ToArray();
        }

#if UNITY_6000_6_OR_NEWER
        private static HashSet<StyleSheet> LoadTargetSheets(IEnumerable<string> targets)
        {
            var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string target in targets.Where(path => path.EndsWith(".uss", StringComparison.OrdinalIgnoreCase)))
                foreach (string path in AssetDatabase.GetDependencies(target, true))
                    if (path.EndsWith(".uss", StringComparison.OrdinalIgnoreCase) ||
                        path.EndsWith(".tss", StringComparison.OrdinalIgnoreCase))
                    {
                        paths.Add(path);
                        if (paths.Count > 4096)
                            throw new InvalidOperationException("Feedback target styles exceed 4096 dependencies.");
                    }
            return new HashSet<StyleSheet>(paths.Select(path =>
                AssetDatabase.LoadAssetAtPath<StyleSheet>(path) ??
                throw new InvalidOperationException("Native target stylesheet is unavailable: " + path)));
        }
#endif

        private sealed class FeedbackHost
        {
            internal string Path;
            internal bool WholeHost;
        }

        internal static void AuditButton(VmUiFeedbackPanel panel, Button button, string assetPath,
            VmUiFeedbackCoverage coverage, Action<VmAutomationUssStyleAuditIssue> record)
        {
            if (!button.enabledInHierarchy) { coverage.DisabledButtons++; return; }
            List<VisualElement> nodes = Descendants(button, VmUiFeedbackCoverage.MaximumDescendants);
            foreach (VisualElement node in nodes)
            {
                node.style.transitionDuration = new List<TimeValue> { new TimeValue(0) };
                node.style.transitionDelay = new List<TimeValue> { new TimeValue(0) };
            }
            coverage.Buttons++;
            panel.SetState(button, false, false, false);
            coverage.StateUpdates++;
            VmUiButtonPaint[] normal = VmUiButtonPaint.Capture(nodes, coverage);
            coverage.ControlSkins?.Inspect(panel.Root, nodes, assetPath, "button:normal", record);
            var stateScales = new Dictionary<string, string> { { "normal", normal[0].ScaleEvidence } };
            panel.SetState(button, true, false, false);
            coverage.StateUpdates++;
            VmUiButtonPaint[] hover = VmUiButtonPaint.Capture(nodes, coverage);
            coverage.ControlSkins?.Inspect(panel.Root, nodes, assetPath, "button:hover", record);
            stateScales.Add("hover", hover[0].ScaleEvidence);
            if (VmUiButtonPaint.Same(normal, hover)) Record("hover");
            panel.SetState(button, true, false, true);
            coverage.StateUpdates++;
            VmUiButtonPaint[] focusedHover = VmUiButtonPaint.Capture(nodes, coverage);
            coverage.ControlSkins?.Inspect(panel.Root, nodes, assetPath, "button:focused-hover", record);
            stateScales.Add("focusedHover", focusedHover[0].ScaleEvidence);
            panel.SetState(button, true, true, true);
            coverage.StateUpdates++;
            VmUiButtonPaint[] pressed = VmUiButtonPaint.Capture(nodes, coverage);
            coverage.ControlSkins?.Inspect(panel.Root, nodes, assetPath, "button:pressed", record);
            stateScales.Add("pressed", pressed[0].ScaleEvidence);
            if (VmUiButtonPaint.Same(focusedHover, pressed)) Record("press");
            panel.SetState(button, true, false, true);
            coverage.StateUpdates++;
            VmUiButtonPaint[] released = VmUiButtonPaint.Capture(nodes, coverage);
            coverage.ControlSkins?.Inspect(panel.Root, nodes, assetPath, "button:released", record);
            stateScales.Add("released", released[0].ScaleEvidence);
            if (!VmUiButtonPaint.Same(focusedHover, released)) Record("release");
            if (coverage.ControlSkins != null)
            {
                try
                {
                    button.SetEnabled(false);
                    panel.SetState(button, false, false, false);
                    coverage.StateUpdates++;
                    coverage.ControlSkins.Inspect(panel.Root, nodes, assetPath, "button:disabled", record);
                }
                finally
                {
                    button.SetEnabled(true);
                    panel.SetState(button, true, false, true);
                }
            }

            void Record(string state)
            {
                string selector = string.IsNullOrEmpty(button.name) ? button.GetType().Name : "#" + button.name;
                record(new VmAutomationUssStyleAuditIssue
                {
                    AssetPath = assetPath, Selector = selector, Token = button.name,
                    Severity = "error", Kind = "button-feedback-missing-" + state,
                    NativeStateScales = stateScales,
                    Message = "Button '" + selector + "' has no effective distinct " + state +
                        " feedback in its native template host. Compare normal/hover and focused-hover/pressed; " +
                        "overridden declarations and persistent focus do not qualify. Native path: " +
                        VmAutomationUIToolkitElementUtility.GetElementPath(panel.Root, button)
                });
            }
        }

        internal static List<VisualElement> Descendants(VisualElement root, int maximum)
        {
            var result = new List<VisualElement>();
            var pending = new Stack<KeyValuePair<VisualElement, int>>();
            pending.Push(new KeyValuePair<VisualElement, int>(root, 0));
            while (pending.Count > 0)
            {
                var entry = pending.Pop();
                if (entry.Value > VmUiFeedbackCoverage.MaximumDepth || result.Count == maximum)
                    throw new InvalidOperationException("Feedback hierarchy exceeds its node/depth boundary.");
                result.Add(entry.Key);
                for (int i = entry.Key.hierarchy.childCount - 1; i >= 0; i--)
                    pending.Push(new KeyValuePair<VisualElement, int>(entry.Key.hierarchy[i], entry.Value + 1));
            }
            return result;
        }
    }
}
