using System;
using System.Collections.Generic;
using UnityEngine;

namespace VMUnityAutomation.Runtime
{
    /// <summary>Reviews bounded natural-frame witnesses. The host owns input, destination semantics and capture.</summary>
    public static class VmUiTransitionAudit
    {
        public enum SamplePhase { CompletedFrame, InputBaseline }

        public struct Frame
        {
            public int Number;
            public SamplePhase Phase;
            public double Seconds;
            public Vector2 Position;
            public bool Published;
        }

        public sealed class Report
        {
            public bool Passed;
            public int Frames;
            public int IntermediateFrames;
            public double PublicationMilliseconds;
            public double SettlementMilliseconds;
            public readonly List<string> Findings = new List<string>();
        }

        public static Report Review(IReadOnlyList<Frame> frames, bool requireMotion,
            double maximumMilliseconds, float tolerancePixels, bool requireMonotonicProgress = false)
        {
            if (frames == null || frames.Count < 3 || frames.Count > 600 ||
                maximumMilliseconds <= 0 || double.IsNaN(maximumMilliseconds) || double.IsInfinity(maximumMilliseconds) ||
                tolerancePixels <= 0 || float.IsNaN(tolerancePixels) || float.IsInfinity(tolerancePixels))
                throw new ArgumentException("Transition review requires3..600 frames and positive timing/pixel contracts.");
            var result = new Report { Frames = frames.Count, PublicationMilliseconds = double.PositiveInfinity };
            var first = frames[0];
            var last = frames[frames.Count - 1];
            if (double.IsNaN(first.Seconds) || double.IsInfinity(first.Seconds))
                throw new ArgumentException("Transition times must be finite.");
            bool observedMotion = false;
            int settled = 0;
            for (int index = 0; index < frames.Count; index++)
            {
                var frame = frames[index];
                bool inputToPaint = index == 1 && first.Phase == SamplePhase.InputBaseline &&
                    frame.Phase == SamplePhase.CompletedFrame;
                if (double.IsNaN(frame.Seconds) || double.IsInfinity(frame.Seconds) ||
                    float.IsNaN(frame.Position.x) || float.IsInfinity(frame.Position.x) ||
                    float.IsNaN(frame.Position.y) || float.IsInfinity(frame.Position.y) ||
                    frame.Phase != SamplePhase.CompletedFrame && frame.Phase != SamplePhase.InputBaseline ||
                    index > 0 && (frame.Phase != SamplePhase.CompletedFrame ||
                        frame.Number < frames[index - 1].Number ||
                        frame.Number == frames[index - 1].Number && !inputToPaint ||
                        frame.Seconds < frames[index - 1].Seconds))
                    throw new ArgumentException("Witnesses require ordered native frames; only an initial input baseline may share its frame with the first completed paint.");
                if (frame.Published && double.IsPositiveInfinity(result.PublicationMilliseconds))
                    result.PublicationMilliseconds = (frame.Seconds - first.Seconds) * 1000;
                float fromFirst = Vector2.Distance(frame.Position, first.Position);
                float fromLast = Vector2.Distance(frame.Position, last.Position);
                observedMotion |= fromFirst > tolerancePixels;
                if (requireMotion && fromFirst > tolerancePixels && fromLast > tolerancePixels)
                    result.IntermediateFrames++;
                if (fromLast > tolerancePixels) settled = index + 1;
            }
            result.SettlementMilliseconds = (frames[settled].Seconds - first.Seconds) * 1000;
            if (!first.Published) result.Findings.Add("delayed-publication");
            if (!last.Published) result.Findings.Add("destination-missing");
            if (requireMotion && !observedMotion) result.Findings.Add("motion-coverage-empty");
            if (requireMotion && observedMotion && result.IntermediateFrames == 0)
                result.Findings.Add("position-snap");
            if (settled >= frames.Count - 1 || Vector2.Distance(frames[frames.Count - 2].Position, last.Position) > tolerancePixels)
                result.Findings.Add("position-unsettled");
            if (result.SettlementMilliseconds > maximumMilliseconds) result.Findings.Add("late-settlement");
            if (requireMonotonicProgress && observedMotion)
            {
                var direction = last.Position - first.Position;
                float distance = direction.magnitude;
                float furthest = 0;
                if (distance <= tolerancePixels) result.Findings.Add("position-reversal");
                else
                {
                    direction /= distance;
                    foreach (var frame in frames)
                    {
                        float progress = Vector2.Dot(frame.Position - first.Position, direction);
                        if (progress < furthest - tolerancePixels || progress > distance + tolerancePixels)
                        {
                            result.Findings.Add("position-reversal");
                            break;
                        }
                        furthest = Mathf.Max(furthest, progress);
                    }
                }
            }
            result.Passed = result.Findings.Count == 0;
            return result;
        }
    }
}
