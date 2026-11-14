using System;
using System.Diagnostics;
using UnityEngine;
using VMUnityAutomation.Runtime;

namespace VMUnityAutomation.Editor
{
    [VmProjectTool("ui/review-projected-markers",
        Description = "Review a fresh game-owned projected-marker source against actual native marker bounds and exact memberships. Reject missing/inactive markers, normalized size drift, displaced anchors, distant/duplicate/unknown cluster members and count mismatches. At most1024 candidates/publications,4096 memberships,500ms; zero required coverage cannot pass. Domain source owns eligibility and projections; manual pixels and motion remain separate.",
        RequiresPlayMode = true, ReadOnly = true, SideEffects = VmProjectToolSideEffect.ReadsProjectState,
        ErrorCodes = new[] { "ui_projected_source", "ui_projected_arguments", "ui_projected_limit" },
        Preconditions = new[] { "stable Play Mode", "exact active source component", "settled current presentation" },
        CompletionEvidence = "Complete source and native geometry snapshot, nonzero coverage, exact findings and frame/build context. No screenshot, input, world or camera mutation.")]
    public sealed class VmUiProjectedMarkerReviewTool : IVmProjectTool<VmUiProjectedMarkerReviewRequest, VmUiProjectedMarkerReport>
    {
        public VmUiProjectedMarkerReport Execute(VmUiProjectedMarkerReviewRequest request)
        {
            var component = VmObjectId.ToObject(request.SourceInstanceId) as MonoBehaviour;
            if (component == null || !component.isActiveAndEnabled || !(component is IVmUiProjectedMarkerSource))
                throw new VmProjectToolException("ui_projected_source", "Select the exact active projected-marker source component.");
            try
            {
                var watch = Stopwatch.StartNew();
                var report = VmUiProjectedMarkerAudit.Review(((IVmUiProjectedMarkerSource)component).CaptureProjectedMarkers(),
                    request.SourceIdentity, request.Scenario, request.LocaleContext);
                if (watch.Elapsed.TotalMilliseconds > VmUiProjectedMarkerAudit.MaximumMilliseconds)
                    throw new VmUiAuditLimitException("Source capture and projected marker review exceeded500ms.");
                report.ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds;
                return report;
            }
            catch (VmUiAuditLimitException error) { throw new VmProjectToolException("ui_projected_limit", error.Message); }
            catch (ArgumentException error) { throw new VmProjectToolException("ui_projected_arguments", error.Message); }
        }
    }
}
