using System.Collections.Generic;

namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// A single timed phase of a bake.
    /// </summary>
    internal readonly struct BakePhase
    {
        public readonly string Label;
        public readonly double Milliseconds;

        public BakePhase(string label, double milliseconds)
        {
            Label = label;
            Milliseconds = milliseconds;
        }
    }

    /// <summary>
    /// Transient timing breakdown of one bake, produced by <see cref="BakeProfiler"/>.
    /// </summary>
    /// <remarks>
    /// Held only in memory for display (e.g. the bake-stats popup); it is never serialized to the
    /// asset or the component, since timings are per-run and machine-dependent.
    /// </remarks>
    internal sealed class BakeReport
    {
        /// <summary>Whole bake time (sum of every phase).</summary>
        public readonly double TotalMs;

        /// <summary>Time spent building the SVO in memory.</summary>
        public readonly double BuildMs;

        /// <summary>Time spent on post-build work (serialize + save the asset).</summary>
        public readonly double SaveMs;

        /// <summary>Per-phase timings, in execution order.</summary>
        public readonly IReadOnlyList<BakePhase> Phases;

        public BakeReport(
            double totalMs,
            double buildMs,
            double saveMs,
            IReadOnlyList<BakePhase> phases
        )
        {
            TotalMs = totalMs;
            BuildMs = buildMs;
            SaveMs = saveMs;
            Phases = phases;
        }
    }
}
