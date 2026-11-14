using System.ComponentModel;

namespace VMUnityAutomation.Editor
{
    public sealed class VmUiProjectedMarkerReviewRequest
    {
        [VmRequired, VmMinLength(1)]
        [Description("Exact active component identity implementing IVmUiProjectedMarkerSource. No scene-wide search or camera mutation.")]
        public string SourceInstanceId { get; set; }
        [VmRequired, VmMinLength(1)] public string SourceIdentity { get; set; }
        [VmRequired, VmMinLength(1)] public string Scenario { get; set; }
        public string LocaleContext { get; set; } = "";
    }
}
