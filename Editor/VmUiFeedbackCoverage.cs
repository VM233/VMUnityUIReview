using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VMUnityAutomation.Editor
{
    internal sealed class VmUiFeedbackCoverage
    {
        internal const int MaximumHosts = 128;
        internal const int MaximumNodes = 8192;
        internal const int MaximumButtons = 128;
        internal const int MaximumDescendants = 256;
        internal const int MaximumDepth = 64;
        internal const int MaximumSamples = 32768;
        internal const int MaximumMilliseconds = 2000;
        private readonly Stopwatch watch = Stopwatch.StartNew();
        internal int Hosts, Buttons, DisabledButtons, StateUpdates, Samples;
        internal bool Complete = true;
        internal string ThemePath = "";

        internal void Finish() => watch.Stop();

        internal void CheckBudget()
        {
            if (Samples > MaximumSamples || watch.ElapsedMilliseconds > MaximumMilliseconds)
                throw new InvalidOperationException("Native button feedback review exceeded its 32768-sample / 2000 ms observation budget; coverage is incomplete.");
        }

        internal Dictionary<string, object> ToDictionary() => new Dictionary<string, object>
        {
            { "complete", Complete }, { "scope", "AuthoredButtonsInNativeTemplateHosts" },
            { "hosts", Hosts }, { "buttons", Buttons }, { "disabledButtons", DisabledButtons },
            { "stateUpdates", StateUpdates }, { "visualSamples", Samples },
            { "themePath", ThemePath }, { "elapsedMilliseconds", watch.Elapsed.TotalMilliseconds },
            { "manualVisualReviewRequired", true },
            { "runtimeGeneratedControlsEvaluated", false }
        };
    }
}
