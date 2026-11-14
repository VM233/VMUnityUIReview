using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VMUnityAutomation.Runtime;
using static VMUnityAutomation.Editor.VmAutomationUICommandArguments;
using static VMUnityAutomation.Editor.VmAutomationUIToolkitElementUtility;
using static VMUnityAutomation.Editor.VmAutomationUIToolkitAssetCommands;
using static VMUnityAutomation.Editor.VmAutomationUIToolkitCommands;

namespace VMUnityAutomation.Editor
{
    public static class VmUiReviewCommands
    {

    public static object DiagnoseRuntimeUI(Dictionary<string, object> args)
    {
        var document = FindRuntimeUIDocument(args, out string error);
        if (document == null)
            return new { error };

        var root = document.rootVisualElement;
        if (root == null)
            return new { error = $"UIDocument '{document.name}' has no rootVisualElement" };

        var checks = new List<Dictionary<string, object>>();
        var queryObjects = GetObjectList(args, "queries");
        if (queryObjects.Count == 0)
            queryObjects.Add(args);

        for (int i = 0; i < queryObjects.Count; i++)
        {
            var query = AsDictionary(queryObjects[i]);
            var element = FindRuntimeElement(query, document, out string elementPath, out string elementError);
            var check = new Dictionary<string, object>
            {
                { "index", i },
                { "query", query },
                { "found", element != null },
                { "path", elementPath },
                { "error", element == null ? elementError : "" },
            };

            if (element != null)
            {
                check["element"] = BuildElementInfo(element, elementPath, true);
                check["parent"] = element.parent == null
                    ? null
                    : BuildElementInfo(element.parent, GetElementPath(root, element.parent), false);
                check["children"] = element.hierarchy.Children()
                    .Select(child => BuildElementInfo(child, GetElementPath(root, child), false))
                    .ToList();
                check["pixel"] = BuildPixelInfo(element, GetFloat(query, "pixelScale",
                    GetFloat(args, "pixelScale", 1f)));
                check["backgroundScale"] = BuildBackgroundScaleInfo(element);
                if (element is TextElement nativeText) check["textLayout"] = VmUiNativeText.Describe(nativeText);
            }

            checks.Add(check);
        }

        return new Dictionary<string, object>
        {
            { "success", true },
            { "document", BuildUIDocumentInfo(document) },
            { "valid", checks.All(check => GetBool(check, "found", false)) },
            { "count", checks.Count },
            { "checks", checks },
        };
    }


    public static object VisualCheckRuntimeUI(Dictionary<string, object> args)
    {
        var document = FindRuntimeUIDocument(args, out string error);
        if (document == null)
            return new { error };

        var root = document.rootVisualElement;
        if (root == null)
            return new { error = $"UIDocument '{document.name}' has no rootVisualElement" };

        var checkArgs = GetObjectList(args, "checks");
        if (checkArgs.Count == 0)
            checkArgs.Add(args);

        float defaultPixelScale = GetFloat(args, "pixelScale", 1f);
        float defaultTolerance = GetFloat(args, "tolerance", 0.01f);
        var results = new List<Dictionary<string, object>>();
        bool valid = true;

        for (int i = 0; i < checkArgs.Count; i++)
        {
            var check = AsDictionary(checkArgs[i]);
            string kind = GetString(check, "type");
            if (string.IsNullOrEmpty(kind))
                kind = GetString(check, "kind");
            if (string.IsNullOrEmpty(kind))
                kind = "pixel-grid";

            var element = FindAssertionElement(root, check, "", out string path, out string elementError);
            var result = new Dictionary<string, object>
            {
                { "index", i },
                { "type", kind },
                { "path", path },
            };

            if (element == null)
            {
                result["passed"] = false;
                result["error"] = elementError;
                results.Add(result);
                valid = false;
                continue;
            }

            switch (kind.ToLowerInvariant())
            {
                case "pixel-grid":
                case "pixel":
                    AddPixelGridResult(result, element,
                        GetFloat(check, "pixelScale", defaultPixelScale),
                        GetFloat(check, "tolerance", defaultTolerance));
                    break;
                case "background-scale":
                case "sprite-scale":
                case "texture-scale":
                    AddBackgroundScaleResult(result, element,
                        GetFloat(check, "expectedScale", GetFloat(check, "scale", defaultPixelScale)),
                        GetFloat(check, "tolerance", defaultTolerance));
                    break;
                case "size":
                    AddRuntimeSizeResult(result, element, check, defaultTolerance);
                    break;
                default:
                    result["passed"] = false;
                    result["error"] = $"Unknown visual check type '{kind}'";
                    break;
            }

            if (GetBool(result, "passed", false) == false)
                valid = false;

            results.Add(result);
        }

        return new Dictionary<string, object>
        {
            { "success", true },
            { "valid", valid },
            { "document", BuildUIDocumentInfo(document) },
            { "count", results.Count },
            { "results", results },
        };
    }


    public static object CompareUIToolkitElement(Dictionary<string, object> args)
    {
        string referencePath = GetString(args, "referencePath");
        if (string.IsNullOrEmpty(referencePath))
            return new { error = "referencePath is required" };

        string actualPath = GetString(args, "actualPath");
        if (string.IsNullOrEmpty(actualPath))
            actualPath = $"Temp/VmAutomation_UIToolkitCompare_{DateTime.Now:yyyyMMdd_HHmmss}.png";

        var captureArgs = new Dictionary<string, object>(args)
        {
            ["outputPath"] = actualPath,
        };
        var capture = CaptureUIToolkitElement(captureArgs) as Dictionary<string, object>;
        if (capture == null || GetBool(capture, "success", false) == false)
            return capture ?? new Dictionary<string, object> { { "success", false }, { "error", "Element capture failed" } };

        var compareArgs = new Dictionary<string, object>
        {
            { "referencePath", referencePath },
            { "actualPath", actualPath },
            { "tolerance", GetFloat(args, "tolerance", 0) },
            { "maxSamples", GetInt(args, "maxSamples", 20) },
        };

        string diffOutputPath = GetString(args, "diffOutputPath");
        if (string.IsNullOrEmpty(diffOutputPath) == false)
            compareArgs["diffOutputPath"] = diffOutputPath;

        if (args.TryGetValue("referenceRect", out object referenceRect))
            compareArgs["referenceRect"] = referenceRect;
        if (args.TryGetValue("expectedRect", out object expectedRect))
            compareArgs["expectedRect"] = expectedRect;
        if (args.TryGetValue("actualRect", out object actualRect))
            compareArgs["actualRect"] = actualRect;

        var comparison = VmAutomationGraphicsCommands.CompareImages(compareArgs);
        return new Dictionary<string, object>
        {
            { "success", true },
            { "referencePath", referencePath },
            { "actualPath", actualPath },
            { "capture", capture },
            { "comparison", comparison },
        };
    }


    public static object AuditUIToolkitResources(Dictionary<string, object> args)
    {
        bool runtime = GetBool(args, "runtime", false);
        int maxDepth = GetInt(args, "maxDepth", 3);
        var queryObjects = GetObjectList(args, "queries");
        if (queryObjects.Count == 0)
            queryObjects.Add(args);

        UnityEngine.UIElements.VisualElement root;
        Dictionary<string, object> context;
        string setupError;
        UnityEngine.UIElements.UIDocument document = null;
        if (runtime)
        {
            document = FindRuntimeUIDocument(args, out setupError);
            if (document == null)
                return new { error = setupError };

            root = document.rootVisualElement;
            if (root == null)
                return new { error = $"UIDocument '{document.name}' has no rootVisualElement" };

            context = BuildUIDocumentInfo(document);
        }
        else
        {
            var window = FindEditorWindow(args, out setupError, out string selectionCode);
            if (window == null)
                return VmAutomationResponse.Error(setupError, selectionCode);

            root = window.rootVisualElement;
            if (root == null)
                return new { error = $"EditorWindow '{window.GetType().FullName}' has no rootVisualElement" };

            context = BuildWindowInfo(window);
        }

        var audits = new List<Dictionary<string, object>>();
        bool valid = true;
        VmUiFeedbackCoverage skinCoverage = null;
        var skinFindings = new List<VmAutomationUssStyleAuditIssue>();
        if (runtime && VmAutomationUIToolkitAuditProjectSettings.Load().RejectDefaultControlAppearance)
        {
            skinCoverage = new VmUiFeedbackCoverage();
            var theme = document.panelSettings.themeStyleSheet;
            skinCoverage.ControlSkins = new VmUiControlSkinAudit(skinCoverage, theme,
                AssetDatabase.GetAssetPath(theme));
            skinCoverage.ControlSkins.Inspect(root,
                VmAutomationUssButtonFeedbackAuditor.Descendants(root, VmUiFeedbackCoverage.MaximumNodes),
                AssetDatabase.GetAssetPath(document.visualTreeAsset), "attached-runtime-state", skinFindings.Add);
            skinCoverage.Finish();
            valid = skinFindings.Count == 0;
        }
        for (int i = 0; i < queryObjects.Count; i++)
        {
            var query = AsDictionary(queryObjects[i]);
            UnityEngine.UIElements.VisualElement element;
            string path;
            string error;
            if (runtime)
                element = FindRuntimeElement(query, document, out path, out error);
            else
                element = FindEditorElement(root, query, out path, out error);

            var audit = new Dictionary<string, object>
            {
                { "index", i },
                { "query", query },
                { "found", element != null },
                { "path", path },
                { "error", element == null ? error : "" },
            };

            if (element == null)
            {
                valid = false;
                audits.Add(audit);
                continue;
            }

            audit["element"] = BuildElementInfo(element, path, true);
            audit["resources"] = CollectElementResources(root, element, path, maxDepth);
            audit["warnings"] = BuildResourceWarnings(element, query);
            if (((List<string>)audit["warnings"]).Count > 0)
                valid = false;

            audits.Add(audit);
        }

        return new Dictionary<string, object>
        {
            { "success", true },
            { "valid", valid },
            { "runtime", runtime },
            { "context", context },
            { "count", audits.Count },
            { "audits", audits },
            { "controlSkins", skinCoverage?.ControlSkins.ToDictionary() },
            { "skinFindings", skinFindings.Select(finding => finding.ToDictionary()).ToArray() },
        };
    }

    }
}
