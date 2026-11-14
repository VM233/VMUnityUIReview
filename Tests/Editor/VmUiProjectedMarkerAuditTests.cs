using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using VMUnityAutomation.Runtime;

namespace VMUnityAutomation.Tests
{
    public sealed class VmUiProjectedMarkerAuditTests
    {
        private static VmUiProjectedMarkerSnapshot Source(float secondX = 220)
        {
            var candidates = new[] { Candidate("a", 100), Candidate("b", secondX) };
            return new VmUiProjectedMarkerSnapshot { Owner = "Fixture source", ViewportPixels = new[] { 0f, 0f, 512f, 512f },
                CollisionPaddingPixels = 2, Candidates = candidates,
                Publications = candidates.Select(c => new VmUiProjectedMarkerPublication { AnchorIdentity = c.Identity,
                    Members = new[] { c.Identity }, DisplayedCount = 1, Visible = true,
                    PresentedAnchorPixels = c.GeographicAnchorPixels, RenderedGlyphBoundsPixels = Bounds(c.ExpectedBounds()),
                    RenderedBoundsPixels = Bounds(c.ExpectedBounds()) }).ToArray() };
        }
        private static VmUiProjectedMarkerCandidate Candidate(string id, float x) => new VmUiProjectedMarkerCandidate
        { Identity = id, Family = "same-family", Required = true, GeographicAnchorPixels = new[] { x, 100f },
            ExpectedGlyphSizePixels = new[] { 64f, 64f }, CollisionSizePixels = new[] { 64f, 64f } };
        private static float[] Bounds(Rect value) => new[] { value.x, value.y, value.width, value.height };
        private static VmUiProjectedMarkerReport Review(VmUiProjectedMarkerSnapshot source) => VmUiProjectedMarkerAudit.Review(source, "fixture-source", "matched geometry");
        private static bool Has(VmUiProjectedMarkerSnapshot source, string rule) => Review(source).Findings.Any(f => f.Rule == rule);
        private static void Cluster(VmUiProjectedMarkerSnapshot source)
        {
            var first = source.Publications[0]; first.Members = new[] { "a", "b" }; first.DisplayedCount = 2;
            source.Publications = new[] { first };
        }
        [Test] public void CompleteNativeFootprintsPass() { var report = Review(Source()); Assert.That(report.Passed && report.Complete); Assert.That(report.GeometryChecks, Is.EqualTo(4)); }
        [Test] public void MissingSameFamilyMarkerFails() { var s = Source(); s.Publications = s.Publications.Take(1).ToArray(); Assert.That(Has(s, "marker-missing")); }
        [Test] public void ParcelShrunkGlyphFails() { var s = Source(); s.Publications[1].RenderedGlyphBoundsPixels = new[] { 204f, 84f, 32f, 32f }; Assert.That(Has(s, "glyph-size")); }
        [Test] public void DistantNearestAssignmentFails() { var s = Source(); Cluster(s); Assert.That(Has(s, "cluster-distant-member")); }
        [Test] public void DirectlyOverlappingMembersPass() { var s = Source(145); Cluster(s); Assert.That(Review(s).Passed); }
        [Test] public void DisplacedRepresentativeFails() { var s = Source(); s.Publications[0].PresentedAnchorPixels = new[] { 110f, 110f }; Assert.That(Has(s, "anchor-displaced")); }
        [Test] public void DuplicateMembershipFails() { var s = Source(145); s.Publications[0].Members = new[] { "a", "b" }; s.Publications[0].DisplayedCount = 2; Assert.That(Has(s, "member-duplicated")); }
        [Test] public void UnknownMembershipFails() { var s = Source(); s.Publications[0].Members = new[] { "unknown" }; Assert.That(Has(s, "member-unknown")); }
        [Test] public void WrongCountFails() { var s = Source(); s.Publications[0].DisplayedCount = 5; Assert.That(Has(s, "count-mismatch")); }
        [Test] public void InactiveRendererFails() { var s = Source(); s.Publications[0].Visible = false; Assert.That(Has(s, "marker-not-rendered")); }
        [Test] public void AbsentNativeBoundsFail() { var s = Source(); s.Publications[0].RenderedBoundsPixels = new float[4]; Assert.That(Has(s, "marker-not-rendered")); }
        [Test] public void ZeroRequiredCoverageFails() { var s = Source(); foreach (var c in s.Candidates) c.Required = false; s.Publications = Array.Empty<VmUiProjectedMarkerPublication>(); Assert.That(Has(s, "coverage-empty")); }
        [Test] public void IneligibleMemberFails() { var s = Source(); s.Candidates[1].Required = false; Assert.That(Has(s, "member-ineligible")); }
        [Test] public void FreshIdentityRejectsStalePublication() { var s = Source(); s.Candidates[1].Identity = "replacement"; Assert.That(Has(s, "member-unknown") && Has(s, "marker-missing")); }
        [Test] public void OverBudgetDoesNotPartiallyPass() { var s = Source(); s.Candidates = new VmUiProjectedMarkerCandidate[1025]; Assert.Throws<VmUiAuditLimitException>(() => Review(s)); }
        [Test] public void DuplicateSourceDoesNotQualify() { var s = Source(); s.Candidates[1].Identity = "a"; Assert.Throws<ArgumentException>(() => Review(s)); }
        [Test] public void NarrowGlyphCanUseAnIndependentTouchFootprint()
        {
            var s = Source(145);
            foreach (var candidate in s.Candidates) candidate.ExpectedGlyphSizePixels = new[] { 12f, 64f };
            Cluster(s); Assert.That(Review(s).Passed);
        }
        [Test] public void CollisionFootprintCannotExcuseGlyphSizeDrift()
        {
            var s = Source(); s.Candidates[0].ExpectedGlyphSizePixels = new[] { 12f, 64f };
            Assert.That(Has(s, "glyph-size"));
        }
    }
}
