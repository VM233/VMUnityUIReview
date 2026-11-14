#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VMUnityAutomation.Runtime;

namespace VMUnityAutomation.Editor
{
    internal static class VmUiWorkContextAuthoringAudit
    {
        internal sealed class Document
        {
            internal string Path;
            internal XDocument Xml;
            internal Dictionary<XElement, Dictionary<string, string>> Contracts;
            internal Dictionary<string, XElement[]> Names;
            internal HashSet<string> TabTargets;
            internal HashSet<XElement> ContainsControls;
        }

        internal static void Audit(VmAutomationUIToolkitAuditOptions options, VmAutomationUxmlLayoutAuditReport report)
        {
            var clock = Stopwatch.StartNew();
            var documents = new Dictionary<string, Document>(StringComparer.OrdinalIgnoreCase);
            int components = 0, nodes = 0, bytes = 0, bindings = 0, nativeDocuments = 0;
            var prefabs = VmAutomationUIToolkitAuditUtility.FindAssetFiles(".prefab", options).Take(129).ToArray();
            try
            {
                if (prefabs.Length > 128) throw new InvalidOperationException("Work-context Prefab budget exceeded.");
                foreach (string path in prefabs)
                {
                    CheckTime();
                    var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    var behaviours = prefab.GetComponentsInChildren<MonoBehaviour>(true);
                    if ((components += behaviours.Length) > 32768) throw new InvalidOperationException("Work-context component budget exceeded.");
                    var sources = behaviours.OfType<IVmUiWorkContextSource>().ToArray();
                    var uiDocuments = prefab.GetComponentsInChildren<UIDocument>(true);
                    foreach (var uiDocument in uiDocuments)
                    {
                        if (++nativeDocuments > 128) throw new InvalidOperationException("Work-context native document budget exceeded.");
                        CheckTime();
                        var asset = uiDocument.visualTreeAsset;
                        if (asset == null) throw new InvalidOperationException("A UI Prefab has no VisualTreeAsset: " + path);
                        var graph = new List<Document>();
                        foreach (string dependency in AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(asset), true)
                                     .Where(value => value.EndsWith(".uxml", StringComparison.OrdinalIgnoreCase)))
                        {
                            if (!documents.TryGetValue(dependency, out var document))
                            {
                                if (documents.Count == 128) throw new InvalidOperationException("Work-context UXML budget exceeded.");
                                string text = File.ReadAllText(VmAutomationUIToolkitAuditUtility.ToFullPath(dependency));
                                bytes += text.Length * 2;
                                if (bytes > 8 * 1024 * 1024) throw new InvalidOperationException("Work-context source budget exceeded.");
                                document = Parse(dependency, text, report);
                                nodes += document.Xml.Descendants().Count();
                                if (nodes > 32768) throw new InvalidOperationException("Work-context XML node budget exceeded.");
                                documents.Add(dependency, document);
                            }
                            graph.Add(document);
                        }
                        var pageContexts = new Dictionary<XElement, string>();
                        var matched = new HashSet<XElement>();
                        var nativeNames = new Dictionary<string, int>(StringComparer.Ordinal);
                        if (sources.Length > 0)
                        {
                            var tree = asset.CloneTree();
                            try
                            {
                            var pending = new Stack<VisualElement>();
                            pending.Push(tree);
                            int nativeNodes = 0;
                            while (pending.Count > 0)
                            {
                                var element = pending.Pop();
                                if (++nativeNodes > 8192) throw new InvalidOperationException("Work-context native tree budget exceeded.");
                                if (!string.IsNullOrEmpty(element.name))
                                    nativeNames[element.name] = nativeNames.TryGetValue(element.name, out int count) ? count + 1 : 1;
                                for (int index = 0; index < element.hierarchy.childCount; index++) pending.Push(element.hierarchy[index]);
                            }
                            }
                            finally { tree.Clear(); }
                        }
                        foreach (var source in sources)
                        {
                            if (((Component)source).GetComponentInParent<UIDocument>(true) != uiDocument) continue;
                            report.WorkContextSources++;
                            var sourceBindings = source.GetWorkContextBindings();
                            if (sourceBindings == null) throw new InvalidOperationException("Work-context source returned no binding product: " + source.GetType().FullName);
                            int sourceCount = 0;
                            foreach (var binding in sourceBindings)
                            {
                                sourceCount++;
                                CheckTime();
                                if (++bindings > 512) throw new InvalidOperationException("Work-context binding budget exceeded.");
                                report.WorkContextBindings++;
                                var matches = graph.Where(value => value.Names.ContainsKey(binding.ElementName ?? "")).ToArray();
                                if (matches.Length != 1 || matches[0].Names[binding.ElementName].Length != 1 ||
                                    !nativeNames.TryGetValue(binding.ElementName, out int count) || count != 1)
                                {
                                    Issue(path, report, "work-context-binding-target", "Expected one authored target for " + source.GetType().FullName + ": " + binding.ElementName);
                                    continue;
                                }
                                Document document = matches[0];
                                XElement target = document.Names[binding.ElementName][0];
                                Validate(document, target, source.GetType().FullName, binding.Context, pageContexts, report);
                                matched.Add(target);
                            }
                            if (sourceCount == 0) throw new InvalidOperationException("Work-context source returned an empty binding product: " + source.GetType().FullName);
                        }
                        foreach (var document in graph)
                        foreach (var entry in document.Contracts.Where(value => value.Value["role"] == "activity"))
                        {
                            report.WorkContextActivities++;
                            if (!matched.Contains(entry.Key))
                                Issue(document.Path, report, "work-context-source-missing", "Activity has no actual serialized producer: " + Name(entry.Key));
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                report.Errors.Add("Work-context authoring coverage incomplete: " + exception.GetBaseException().Message);
            }

            void CheckTime()
            {
                if (clock.ElapsedMilliseconds > 2000) throw new InvalidOperationException("Work-context native observation exceeded 2000ms.");
            }
        }

        internal static Document Parse(string path, string text, VmAutomationUxmlLayoutAuditReport report)
        {
            var xml = XDocument.Parse(text, LoadOptions.SetLineInfo);
            var elements = xml.Descendants().Take(32769).ToArray();
            if (elements.Length > 32768) throw new InvalidOperationException("Work-context XML node budget exceeded.");
            var contracts = VmAutomationUxmlInteractionStructureAuditor.ReadContracts(path, xml, report);
            var hasControls = new HashSet<XElement>();
            var types = new Dictionary<XName, bool>();
            for (int index = elements.Length - 1; index >= 0; index--)
            {
                XElement element = elements[index];
                if (!types.TryGetValue(element.Name, out bool control))
                {
                    if (types.Count == 128) throw new InvalidOperationException("Work-context XML type budget exceeded.");
                    Type type = VmAutomationUIToolkitElementStyleBaseline.ResolveType(element.Name.NamespaceName, element.Name.LocalName);
                    control = type != null && (typeof(Button).IsAssignableFrom(type) || typeof(Toggle).IsAssignableFrom(type) ||
                        typeof(Slider).IsAssignableFrom(type) || typeof(SliderInt).IsAssignableFrom(type));
                    types.Add(element.Name, control);
                }
                if ((control || hasControls.Contains(element)) && element.Parent != null) hasControls.Add(element.Parent);
            }
            return new Document
            {
                Path = path, Xml = xml, Contracts = contracts, ContainsControls = hasControls,
                Names = xml.Descendants().Where(element => Name(element) != "").GroupBy(Name)
                    .ToDictionary(group => group.Key, group => group.ToArray()),
                TabTargets = new HashSet<string>(contracts.Where(entry => entry.Value["role"] == "tab")
                    .Select(entry => entry.Value["target"]), StringComparer.Ordinal)
            };
        }

        internal static void Validate(Document document, XElement target, string owner, string context,
            Dictionary<XElement, string> pageContexts, VmAutomationUxmlLayoutAuditReport report)
        {
            if (!document.Contracts.TryGetValue(target, out var activity) || activity["role"] != "activity" ||
                !activity.TryGetValue("owner", out string declaredOwner) || declaredOwner != owner ||
                !activity.TryGetValue("state", out string declaredContext) || declaredContext != context)
                Issue(document.Path, report, "work-context-owner-mismatch", "Actual producer " + owner + " owns '" + context + "', but its target is not that declared activity: " + Name(target));
            XElement panel = null;
            int depth = 0;
            foreach (var ancestor in target.Ancestors())
            {
                if (++depth > 128) throw new InvalidOperationException("Work-context ancestry budget exceeded.");
                if (document.Contracts.TryGetValue(ancestor, out var contract) && contract["role"] == "panel") { panel = ancestor; break; }
            }
            if (panel == null || !document.TabTargets.Contains(Name(panel)))
                Issue(document.Path, report, "work-context-page-missing", "Independent work needs its actual navigated page; a Foldout or heading is insufficient: " + Name(target));
            else if (pageContexts.TryGetValue(panel, out string previous) && previous != context)
                Issue(document.Path, report, "mixed-functional-work-contexts", "One page mixes independent work contexts '" + previous + "' and '" + context + "'.");
            else pageContexts[panel] = context;
            if (!document.ContainsControls.Contains(target))
                Issue(document.Path, report, "work-context-empty", "An activity needs its real representative controls: " + Name(target));
        }

        private static string Name(XElement element) => (string)element.Attribute("name") ?? "";

        private static void Issue(string path, VmAutomationUxmlLayoutAuditReport report, string kind, string message) =>
            report.Record(new VmAutomationUxmlLayoutAuditIssue { AssetPath = path, Kind = kind, Severity = "error", Message = message }, false);
    }
}
#endif
