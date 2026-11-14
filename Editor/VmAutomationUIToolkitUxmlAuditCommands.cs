#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace VMUnityAutomation.Editor
{
    public static class VmAutomationUIToolkitUxmlAuditCommands
    {
        public static object AuditUxmlLayout(Dictionary<string, object> args)
            => AuditAndPublish(args, null);

        public static void BeginUxmlLayoutAudit(Dictionary<string, object> args, Action<object> completed)
            => AuditAndPublish(args, completed);

        private static object AuditAndPublish(Dictionary<string, object> args, Action<object> completed)
        {
            args = args ?? new Dictionary<string, object>();
            var options = VmAutomationUIToolkitAuditOptions.FromArguments(args);
            var report = VmAutomationUxmlLayoutAuditor.Audit(
                VmAutomationUIToolkitAuditUtility.GetStringList(args, "paths"),
                VmAutomationUIToolkitAuditUtility.GetBool(args, "includeSuppressed"),
                Mathf.Clamp(VmAutomationUIToolkitAuditUtility.GetInt(args, "maxIssues", 200), 1, 5000),
                options, deferNative: completed != null);

            if (completed != null)
            {
                VmUiFeedbackContinuation.Run(report.FeedbackWork, () =>
                {
                    try { completed(Publish(args, options, report)); }
                    catch (Exception exception) { completed(VmAutomationResponse.Error(exception.GetBaseException().Message, "operation_failed")); }
                });
                return null;
            }
            return Publish(args, options, report);
        }

        private static object Publish(Dictionary<string, object> args, VmAutomationUIToolkitAuditOptions options,
            VmAutomationUxmlLayoutAuditReport report)
        {
            report.SortIssues();

            if (VmAutomationUIToolkitAuditUtility.GetBool(args, "logWarnings"))
                VmAutomationUxmlLayoutAuditConsoleReporter.Log(report, false);

            var result = report.ToDictionary();
            result["scope"] = options.ToDictionary();
            result["automaticAudit"] =
                VmAutomationUIToolkitAutomaticAuditCoordinator.GetStatus(".uxml");
            if (VmAutomationUIToolkitAuditUtility.GetBool(args, "runSelfTests"))
            {
                var selfTests = VmAutomationUxmlLayoutAuditSelfTests.RunSelfTests();
                result["selfTests"] = selfTests;
                object passed;
                if (selfTests.TryGetValue("passed", out passed) &&
                    passed is bool && !(bool)passed)
                    result["success"] = false;
            }

            return result;
        }

        [MenuItem("Tools/UI Toolkit/Audit UXML Layout Contracts")]
        private static void AuditAllFromMenu()
        {
            var options = VmAutomationUIToolkitAuditOptions.FromProjectSettings(
                VmAutomationUIToolkitAuditProjectSettings.Load());
            var report = VmAutomationUxmlLayoutAuditor.Audit(
                Array.Empty<string>(), true, 5000, options, deferNative: true);
            VmUiFeedbackContinuation.Run(report.FeedbackWork, () =>
            {
                report.SortIssues();
                VmAutomationUxmlLayoutAuditConsoleReporter.Log(report, false);
            });
        }
    }
}
#endif
