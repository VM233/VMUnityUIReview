using System;
using System.Collections.Generic;

namespace VMUnityAutomation.Editor
{
    internal sealed class VmUiFeedbackWork
    {
        private static readonly Unity.Profiling.ProfilerMarker AdvanceMarker = new Unity.Profiling.ProfilerMarker("VM.UIFeedback.Advance");
        internal readonly VmUiFeedbackCoverage Coverage;
        private readonly IEnumerator<object> steps;
        private readonly Action<string> fail;
        internal bool Finished { get; private set; }

        internal VmUiFeedbackWork(VmUiFeedbackCoverage coverage, IEnumerator<object> steps, Action<string> fail)
        {
            Coverage = coverage; this.steps = steps; this.fail = fail;
            Coverage.Complete = false;
        }

        internal void Advance()
        {
            if (Finished) return;
            using var measurement = AdvanceMarker.Auto();
            try
            {
                Coverage.BeginObservation();
                if (steps.MoveNext()) return;
                Finish(true, null);
            }
            catch (Exception exception) { Finish(false, exception.GetBaseException().Message); }
        }

        internal void Drain() { while (!Finished) Advance(); }
        internal void Cancel(string reason) { if (!Finished) Finish(false, reason); }

        private void Finish(bool complete, string error)
        {
            Finished = true;
            try { steps.Dispose(); }
            catch (Exception exception)
            {
                complete = false;
                error = (error == null ? "" : error + "; ") + "Native panel retirement failed: " + exception.GetBaseException().Message;
            }
            Coverage.Complete = complete;
            Coverage.Finish();
            if (!complete) fail(error);
        }
    }
}
