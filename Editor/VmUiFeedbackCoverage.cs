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
        internal const int MaximumMatchQueries = 32768;
        private readonly Stopwatch watch = Stopwatch.StartNew();
        private readonly Stopwatch observationWatch = Stopwatch.StartNew();
        internal int Hosts, Buttons, DisabledButtons, StateUpdates, Samples;
        internal int StyleMatchQueries, UnaffectedButtons, StyleOriginCacheHits;
        internal int FieldOwners;
        internal bool Complete = true;
        internal string ThemePath = "";
        internal VmUiControlSkinAudit ControlSkins;

        internal void Finish() => watch.Stop();
        internal void BeginObservation() { observationWatch.Restart(); }

        internal void CheckBudget()
        {
            if (Samples > MaximumSamples || StyleMatchQueries > MaximumMatchQueries ||
                observationWatch.ElapsedMilliseconds > MaximumMilliseconds)
                throw new InvalidOperationException("Native UI review exceeded its 32768 total samples / 32768 total match queries / 2000 ms active observation budget; coverage is incomplete.");
        }

        internal Dictionary<string, object> ToDictionary() => new Dictionary<string, object>
        {
            { "complete", Complete }, { "scope", "AuthoredButtonsInNativeTemplateHostsWithNativeStyleScopeWhenAvailable" },
            { "hosts", Hosts }, { "buttons", Buttons }, { "disabledButtons", DisabledButtons },
            { "stateUpdates", StateUpdates }, { "visualSamples", Samples },
            { "styleMatchQueries", StyleMatchQueries }, { "unaffectedButtons", UnaffectedButtons },
            { "styleOriginCacheHits", StyleOriginCacheHits },
            { "fieldOwners", FieldOwners },
            { "observationBudgetMilliseconds", MaximumMilliseconds },
            { "observationBudgetUnit", "NativeControlObservation" },
            { "themePath", ThemePath }, { "elapsedMilliseconds", watch.Elapsed.TotalMilliseconds },
            { "manualVisualReviewRequired", true },
            { "runtimeGeneratedControlsEvaluated", false },
            { "controlSkins", ControlSkins?.ToDictionary() }
        };
    }
}
