using System.Collections.Generic;

namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// Reports coarse bake progress to the host (e.g. an editor progress bar). The builder invokes
    /// this only at phase boundaries, where no jobs or native allocations are in flight, so the host
    /// is free to throw (e.g. on user cancel) to unwind the bake cleanly.
    /// </summary>
    /// <param name="phase">Human-readable name of the phase about to run.</param>
    /// <param name="fraction">Approximate overall completion, in [0, 1].</param>
    internal delegate void BakeProgress(string phase, float fraction);

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
