using System.Collections.Generic;

namespace VMUnityAutomation.Runtime
{
    public sealed class VmUiProjectedMarkerReport
    {
        public bool Passed { get; set; }
        public bool Complete { get; set; }
        public bool ManualVisualReviewRequired { get; set; } = true;
        public string SourceIdentity { get; set; }
        public string Scenario { get; set; }
        public string LocaleContext { get; set; }
        public string BuildGuid { get; set; }
        public string UnityVersion { get; set; }
        public int Frame { get; set; }
        public int ExpectedVisible { get; set; }
        public int Published { get; set; }
        public int MembershipChecks { get; set; }
        public int GeometryChecks { get; set; }
        public double ElapsedMilliseconds { get; set; }
        public VmUiProjectedMarkerSnapshot Snapshot { get; set; }
        public List<VmUiProjectedMarkerFinding> Findings { get; set; } = new List<VmUiProjectedMarkerFinding>();
    }

}
