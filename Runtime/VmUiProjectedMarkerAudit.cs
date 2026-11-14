using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace VMUnityAutomation.Runtime
{
    public static class VmUiProjectedMarkerAudit
    {
        public const int MaximumMarkers = 1024, MaximumMembers = 4096;
        public const double MaximumMilliseconds = 500;
        public const float TolerancePixels = .5f;

        public static VmUiProjectedMarkerReport Review(VmUiProjectedMarkerSnapshot snapshot, string sourceIdentity,
            string scenario, string localeContext = "")
        {
            var watch = Stopwatch.StartNew();
            if (snapshot == null || !Text(snapshot.Owner, 256) ||
                !Text(sourceIdentity, 512) || !Text(scenario, 512) || localeContext == null || localeContext.Length > 64 ||
                !Positive(Bounds(snapshot.ViewportPixels)) || !Finite(snapshot.CollisionPaddingPixels) || snapshot.CollisionPaddingPixels < 0 ||
                snapshot.Candidates == null || snapshot.Publications == null)
                throw new ArgumentException("Supply a fresh owner snapshot, viewport, padding, source and scenario.");
            if (snapshot.Candidates.Length > MaximumMarkers || snapshot.Publications.Length > MaximumMarkers)
                throw new VmUiAuditLimitException("Projected marker source/publication count exceeds1024.");
            var report = new VmUiProjectedMarkerReport { SourceIdentity = sourceIdentity, Scenario = scenario,
                LocaleContext = localeContext, BuildGuid = Application.buildGUID, UnityVersion = Application.unityVersion,
                Frame = Time.frameCount, Snapshot = snapshot, Published = snapshot.Publications.Length };
            var candidates = new Dictionary<string, VmUiProjectedMarkerCandidate>(StringComparer.Ordinal);
            var represented = new HashSet<string>(StringComparer.Ordinal);
            foreach (var candidate in snapshot.Candidates)
            {
                if (candidate == null || !Text(candidate.Identity, 160) || !Text(candidate.Family, 160) ||
                    !Finite(Point(candidate.GeographicAnchorPixels)) || !Finite(Point(candidate.ExpectedGlyphSizePixels)) ||
                    candidate.ExpectedGlyphSizePixels[0] <= 0 || candidate.ExpectedGlyphSizePixels[1] <= 0 || candidates.ContainsKey(candidate.Identity))
                    throw new ArgumentException("Candidates need unique source identities, families, finite anchors and positive authored glyph sizes.");
                candidates.Add(candidate.Identity, candidate);
                if (candidate.Required) report.ExpectedVisible++;
                CheckTime(watch);
            }
            if (report.ExpectedVisible == 0) Add(report, "coverage-empty", snapshot.Owner, "No required source marker was evaluated.");
            foreach (var publication in snapshot.Publications)
            {
                if (publication == null || publication.Members == null || !Text(publication.AnchorIdentity, 160))
                    throw new ArgumentException("Publications require an exact anchor identity and member collection.");
                if (publication.Members.Length > MaximumMembers - report.MembershipChecks)
                    throw new VmUiAuditLimitException("Projected marker memberships exceed4096.");
                VmUiProjectedMarkerCandidate anchor;
                if (!candidates.TryGetValue(publication.AnchorIdentity, out anchor))
                    Add(report, "anchor-unknown", publication.AnchorIdentity, "Published anchor has no authoritative source candidate.");
                if (!publication.Visible || !Positive(Bounds(publication.RenderedBoundsPixels)) ||
                    !Bounds(publication.RenderedBoundsPixels).Overlaps(Bounds(snapshot.ViewportPixels)))
                    Add(report, "marker-not-rendered", publication.AnchorIdentity, "The required publication has no visible native bounds in the viewport.");
                if (publication.DisplayedCount != publication.Members.Length || publication.Members.Length == 0)
                    Add(report, "count-mismatch", publication.AnchorIdentity, "Displayed count must equal the exact nonempty member product.");
                if (anchor != null)
                {
                    report.GeometryChecks++;
                    if (!Finite(Point(publication.PresentedAnchorPixels)) ||
                        Vector2.Distance(Point(anchor.GeographicAnchorPixels), Point(publication.PresentedAnchorPixels)) > TolerancePixels)
                        Add(report, "anchor-displaced", anchor.Identity, "Publication moved away from its actual geographic projection.");
                    if (publication.Members.Length == 1)
                    {
                        report.GeometryChecks++;
                        var actual = Bounds(publication.RenderedGlyphBoundsPixels); var size = actual.size;
                        if (!Positive(actual) ||
                            Mathf.Abs(size.x - anchor.ExpectedGlyphSizePixels[0]) > TolerancePixels ||
                            Mathf.Abs(size.y - anchor.ExpectedGlyphSizePixels[1]) > TolerancePixels)
                            Add(report, "glyph-size", anchor.Identity, "Native glyph size differs from the source's normalized screen footprint.");
                        if (Vector2.Distance(actual.center, Point(anchor.GeographicAnchorPixels)) > TolerancePixels)
                            Add(report, "glyph-offset", anchor.Identity, "Native glyph center differs from its geographic anchor.");
                    }
                }
                bool containsAnchor = false;
                foreach (string member in publication.Members)
                {
                    report.MembershipChecks++;
                    VmUiProjectedMarkerCandidate candidate;
                    if (!Text(member, 160)) throw new ArgumentException("Member identities must contain1..160 characters.");
                    if (!candidates.TryGetValue(member, out candidate))
                    {
                        Add(report, "member-unknown", member, "Published member has no authoritative source candidate.");
                        continue;
                    }
                    containsAnchor |= member == publication.AnchorIdentity;
                    if (!represented.Add(member)) Add(report, "member-duplicated", member, "A source marker is published more than once.");
                    if (!candidate.Required) Add(report, "member-ineligible", member, "A source-ineligible marker was included in the visible publication.");
                    if (anchor != null && member != anchor.Identity &&
                        !Expanded(anchor.ExpectedBounds(), snapshot.CollisionPaddingPixels).Overlaps(candidate.ExpectedBounds()))
                        Add(report, "cluster-distant-member", member, "Member footprint does not directly overlap the representative's footprint.");
                    CheckTime(watch);
                }
                if (!containsAnchor) Add(report, "anchor-not-member", publication.AnchorIdentity, "The representative is absent from its own member product.");
            }
            foreach (var candidate in snapshot.Candidates)
                if (candidate.Required && !represented.Contains(candidate.Identity))
                    Add(report, "marker-missing", candidate.Identity, "An eligible in-viewport source marker has no visible publication.");
            CheckTime(watch);
            report.ElapsedMilliseconds = watch.Elapsed.TotalMilliseconds;
            report.Complete = true; report.Passed = report.Findings.Count == 0;
            return report;
        }

        private static Rect Expanded(Rect bounds, float padding) => Rect.MinMaxRect(bounds.xMin - padding, bounds.yMin - padding,
            bounds.xMax + padding, bounds.yMax + padding);
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static bool Text(string value, int maximum) => !string.IsNullOrWhiteSpace(value) && value.Length <= maximum;
        private static bool Finite(Vector2 value) => Finite(value.x) && Finite(value.y);
        private static bool Positive(Rect value) => Finite(value.position) && Finite(value.size) && value.width > 0 && value.height > 0;
        private static Vector2 Point(float[] values)
        {
            if (values == null || values.Length != 2) throw new ArgumentException("Pixel point/size arrays require exactly two values.");
            return new Vector2(values[0], values[1]);
        }
        private static Rect Bounds(float[] values)
        {
            if (values == null || values.Length != 4) throw new ArgumentException("Pixel bounds arrays require exactly four values.");
            return new Rect(values[0], values[1], values[2], values[3]);
        }
        private static void CheckTime(Stopwatch watch)
        {
            if (watch.Elapsed.TotalMilliseconds > MaximumMilliseconds)
                throw new VmUiAuditLimitException("Projected marker review exceeded500ms; no partial pass is published.");
        }
        private static void Add(VmUiProjectedMarkerReport report, string rule, string identity, string message)
        {
            if (report.Findings.Count >= MaximumMarkers)
                throw new VmUiAuditLimitException("Projected marker findings exceed1024; no partial pass is published.");
            report.Findings.Add(new VmUiProjectedMarkerFinding { Rule = rule, Identity = identity, Message = message });
        }
    }
}
