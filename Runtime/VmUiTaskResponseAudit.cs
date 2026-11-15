using System;
using System.Collections.Generic;

namespace VMUnityAutomation.Runtime
{
    public static class VmUiTaskResponseAudit
    {
        public struct Frame
        {
            public int Number;
            public double Seconds;
            public bool Published;
            public float PaintedFraction;
            public float GeometryOffsetPixels;
        }

        public sealed class Report
        {
            public bool Passed;
            public int Evaluated;
            public double? PublicationMilliseconds;
            public double? VisibleResponseMilliseconds;
            public double? GeometryResponseMilliseconds;
            public double? SettlementMilliseconds;
            public bool GeometryRequired;
            public readonly List<string> Findings = new List<string>();
        }

        public struct ReturnVisit
        {
            public string Context, Action, ExpectedDestination, ActualDestination;
        }

        public static Report Review(IReadOnlyList<Frame> frames, double inputSeconds,
            double maximumResponseMilliseconds, double maximumSettlementMilliseconds, bool requireGeometry = false)
        {
            if (frames == null || frames.Count < 2 || frames.Count > 600 || !Finite(inputSeconds) ||
                !Positive(maximumResponseMilliseconds) || !Positive(maximumSettlementMilliseconds))
                throw new ArgumentException("Response review requires2..600 native frames and finite input/budgets.");
            var report = new Report { Evaluated = frames.Count, GeometryRequired = requireGeometry };
            for (int index = 0; index < frames.Count; index++)
            {
                var frame = frames[index];
                if (!Finite(frame.Seconds) || frame.Seconds < inputSeconds || !Finite(frame.PaintedFraction) ||
                    frame.PaintedFraction < 0 || frame.PaintedFraction > 1 || !Finite(frame.GeometryOffsetPixels) ||
                    frame.GeometryOffsetPixels < 0 || index > 0 &&
                    (frame.Number <= frames[index - 1].Number || frame.Seconds < frames[index - 1].Seconds))
                    throw new ArgumentException("Response observations require ordered native frames and real visible fractions.");
                double elapsed = (frame.Seconds - inputSeconds) * 1000;
                if (frame.Published && !report.PublicationMilliseconds.HasValue) report.PublicationMilliseconds = elapsed;
                if (frame.Published && frame.PaintedFraction > .01f && !report.VisibleResponseMilliseconds.HasValue)
                    report.VisibleResponseMilliseconds = elapsed;
                if (frame.Published && frame.GeometryOffsetPixels >= .5f && !report.GeometryResponseMilliseconds.HasValue)
                    report.GeometryResponseMilliseconds = elapsed;
                if (frame.Published && frame.PaintedFraction >= .999f && !report.SettlementMilliseconds.HasValue)
                    report.SettlementMilliseconds = elapsed;
            }
            if ((report.PublicationMilliseconds ?? double.PositiveInfinity) > maximumResponseMilliseconds) report.Findings.Add("delayed-publication");
            if ((report.VisibleResponseMilliseconds ?? double.PositiveInfinity) > maximumResponseMilliseconds) report.Findings.Add("delayed-visible-response");
            if (requireGeometry && (report.GeometryResponseMilliseconds ?? double.PositiveInfinity) > maximumResponseMilliseconds)
                report.Findings.Add("delayed-geometry-response");
            if ((report.SettlementMilliseconds ?? double.PositiveInfinity) > maximumSettlementMilliseconds) report.Findings.Add("late-settlement");
            var last = frames[frames.Count - 1];
            if (!last.Published || last.PaintedFraction < .999f) report.Findings.Add("destination-unsettled");
            report.Passed = report.Findings.Count == 0;
            return report;
        }

        public static Report ReviewReturns(IReadOnlyList<ReturnVisit> visits)
        {
            if (visits == null || visits.Count < 2 || visits.Count > 128)
                throw new ArgumentException("Return consistency requires2..128 actual visits.");
            var report = new Report { Evaluated = visits.Count };
            var actual = new Dictionary<(string Context, string Action), string>();
            int repeats = 0;
            foreach (var visit in visits)
            {
                RequireIdentity(visit.Context); RequireIdentity(visit.Action);
                RequireIdentity(visit.ExpectedDestination); RequireIdentity(visit.ActualDestination);
                if (visit.ExpectedDestination != visit.ActualDestination) report.Findings.Add("wrong-return-destination");
                var key = (visit.Context, visit.Action);
                if (actual.TryGetValue(key, out string previous))
                {
                    repeats++;
                    if (previous != visit.ActualDestination) report.Findings.Add("inconsistent-return-destination");
                }
                else actual.Add(key, visit.ActualDestination);
            }
            if (repeats == 0) report.Findings.Add("return-repetition-coverage-empty");
            report.Passed = report.Findings.Count == 0;
            return report;
        }

        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        private static bool Positive(double value) => Finite(value) && value > 0;
        private static void RequireIdentity(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 512)
                throw new ArgumentException("Navigation requires an actual nonempty identity of at most512 units.");
        }
    }
}
