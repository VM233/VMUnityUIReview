using System;
using System.Diagnostics;
using UnityEditor;

namespace VMUnityAutomation.Editor
{
    /// <summary>Only isolated native review panels survive between bounded Editor updates.</summary>
    internal sealed class VmUiFeedbackContinuation
    {
        internal static int Pending { get; private set; }
        private readonly VmUiFeedbackWork work;
        private readonly Action completed;
        private readonly Stopwatch lifetime = Stopwatch.StartNew();
        private bool retired;

        internal static void Run(VmUiFeedbackWork work, Action completed)
        {
            if (work == null || work.Finished) { completed(); return; }
            if (!work.Coverage.Cooperative) { work.Drain(); completed(); return; }
            if (Pending == 8)
            {
                work.Cancel("At most eight native UI review continuations may be admitted.");
                completed(); return;
            }
            new VmUiFeedbackContinuation(work, completed);
        }

        private VmUiFeedbackContinuation(VmUiFeedbackWork work, Action completed)
        {
            this.work = work; this.completed = completed;
            Pending++;
            EditorApplication.update += Tick;
            EditorApplication.projectChanged += SourceChanged;
            EditorApplication.quitting += Closing;
            AssemblyReloadEvents.beforeAssemblyReload += Closing;
        }

        private void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
                work.Cancel("Native authoring review requires an unchanged, stable Edit Mode source.");
            if (lifetime.ElapsedMilliseconds > 60000) work.Cancel("Native UI review exceeded its 60000 ms continuation lifetime.");
            var slice = Stopwatch.StartNew();
            while (!work.Finished && slice.ElapsedMilliseconds < 8) work.Advance();
            if (work.Finished) Retire();
        }

        private void SourceChanged()
        {
            work.Cancel("Project assets changed during native UI review; the earlier source is not qualified.");
            Retire();
        }

        private void Closing()
        {
            work.Cancel("The owning Editor domain is retiring before native UI review completed.");
            Retire();
        }

        private void Retire()
        {
            if (retired) return;
            retired = true;
            EditorApplication.update -= Tick;
            EditorApplication.projectChanged -= SourceChanged;
            EditorApplication.quitting -= Closing;
            AssemblyReloadEvents.beforeAssemblyReload -= Closing;
            Pending--;
            completed();
        }
    }
}
