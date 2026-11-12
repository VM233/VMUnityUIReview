using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor.PackageManager;
using static VMUnityAutomation.Editor.VmAutomationBuiltInRouteDescriptor;

namespace VMUnityAutomation.Editor
{
    internal sealed class VmUiReviewRouteProvider : IVmAutomationBuiltInRouteProvider
    {
        private static readonly IReadOnlyDictionary<string, VmAutomationBuiltInRouteContract> Contracts = LoadContracts();
        private static readonly VmAutomationBuiltInRouteDescriptor[] Routes =
        {
            CreateImmediate("uitoolkit/assert-layout", Contracts["uitoolkit/assert-layout"], a => VmAutomationUIToolkitAssetCommands.AssertUIToolkitLayout(a)),
            CreateImmediate("uitoolkit/asset-inspect", Contracts["uitoolkit/asset-inspect"], a => VmAutomationUIToolkitAssetCommands.InspectUIToolkitAsset(a)),
            CreateImmediate("uitoolkit/audit-uss-styles", Contracts["uitoolkit/audit-uss-styles"], a => VmAutomationUIToolkitUssAuditCommands.AuditUssStyles(a)),
            CreateImmediate("uitoolkit/audit-uxml-layout", Contracts["uitoolkit/audit-uxml-layout"], a => VmAutomationUIToolkitUxmlAuditCommands.AuditUxmlLayout(a)),
            CreateImmediate("uitoolkit/authoring-transaction", Contracts["uitoolkit/authoring-transaction"], a => VmAutomationUIAuthoringCommands.AuthoringTransaction(a)),
            CreateDeferred("uitoolkit/builder-preview", Contracts["uitoolkit/builder-preview"], (a, resolve, _) => VmAutomationUIBuilderPreviewCommands.OpenUIBuilderPreview(a, resolve)),
            CreateImmediate("uitoolkit/compare-element", Contracts["uitoolkit/compare-element"], a => VmUiReviewCommands.CompareUIToolkitElement(a)),
            CreateImmediate("uitoolkit/diagnose-runtime", Contracts["uitoolkit/diagnose-runtime"], a => VmUiReviewCommands.DiagnoseRuntimeUI(a)),
            CreateImmediate("uitoolkit/edit-uss", Contracts["uitoolkit/edit-uss"], a => VmAutomationUIAuthoringCommands.EditUss(a)),
            CreateImmediate("uitoolkit/edit-uxml", Contracts["uitoolkit/edit-uxml"], a => VmAutomationUIAuthoringCommands.EditUxml(a)),
            CreateImmediate("uitoolkit/resource-audit", Contracts["uitoolkit/resource-audit"], a => VmUiReviewCommands.AuditUIToolkitResources(a)),
            CreateImmediate("uitoolkit/visual-check", Contracts["uitoolkit/visual-check"], a => VmUiReviewCommands.VisualCheckRuntimeUI(a)),
        };

        public IReadOnlyList<VmAutomationBuiltInRouteDescriptor> Descriptors => Routes;
        public string AuditedRouteManifestSha256 => "3302d7efc59ac6958816a50d098a033bda8db2a205edadd9b1d044361cf877ae";

        private static IReadOnlyDictionary<string, VmAutomationBuiltInRouteContract> LoadContracts()
        {
            string root = PackageInfo.FindForAssembly(typeof(VmUiReviewRouteProvider).Assembly).resolvedPath;
            string[] names = { "assert-layout", "asset-inspect", "audit-uss-styles", "audit-uxml-layout",
                "authoring-transaction", "builder-preview", "compare-element", "diagnose-runtime",
                "edit-uss", "edit-uxml", "resource-audit", "visual-check" };
            var contracts = new Dictionary<string, VmAutomationBuiltInRouteContract>(StringComparer.Ordinal);
            foreach (string name in names)
            {
                string path = Path.Combine(root, "Editor", "Contracts", "uitoolkit-" + name + ".json");
                if (new FileInfo(path).Length > 1048576)
                    throw new InvalidOperationException("UI route contract exceeds its 1 MiB boundary: " + name);
                var data = (Dictionary<string, object>)MiniJson.Deserialize(File.ReadAllText(path));
                string route = "uitoolkit/" + name;
                if ((string)data["route"] != route)
                    throw new InvalidOperationException("UI route contract identity mismatch: " + path);
                contracts.Add(route, new VmAutomationBuiltInRouteContract(route, "com.vm233.unity-ui-review",
                    (string)data["description"], Profile(name),
                    (Dictionary<string, object>)data["inputSchema"],
                    (Dictionary<string, object>)data["outputSchema"],
                    ((IEnumerable<object>)data["errorCodes"]).Select(value => (string)value).ToArray()));
            }
            return contracts;
        }

        private static VmAutomationToolProfile Profile(string name)
        {
            switch (name)
            {
                case "builder-preview":
                    return VmAutomationToolProfile.Create(longRunning: true,
                        sideEffects: new[] { "readsProjectState", "writesScreenshotFiles", "changesEditorView" });
                case "authoring-transaction":
                    return VmAutomationToolProfile.Create(mutatesAssets: true,
                        transaction: VmAutomationTransactionProfile.Create("uxml-and-uss-files",
                            VmTransactionMechanics.Atomicity.VerifiedFileRollback,
                            VmTransactionMechanics.Isolation.RequestSerialized,
                            VmTransactionMechanics.Durability.EditorSession,
                            VmTransactionMechanics.RollbackKind.InMemoryTextSnapshot,
                            "atomic-file-publication", "asset-import-result"));
                case "edit-uss":
                case "edit-uxml":
                    return VmAutomationToolProfile.Create(mutatesAssets: true);
                case "audit-uss-styles":
                case "audit-uxml-layout":
                    return VmAutomationToolProfile.Create(readOnly: true, longRunning: true);
                case "assert-layout":
                case "asset-inspect":
                case "compare-element":
                case "diagnose-runtime":
                case "resource-audit":
                case "visual-check":
                    return VmAutomationToolProfile.Create(readOnly: true);
                default:
                    throw new ArgumentException("Unregistered UI review profile: " + name, nameof(name));
            }
        }
    }
}
