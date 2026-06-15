using System.Collections.Generic;

namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// Reports coarse bake progress.
    /// </summary>
    /// <param name="phase">
    /// Human-readable name of the phase about to run.
    /// </param>
    /// <param name="fraction">
    /// Approximate overall completion, in [0, 1].
    /// </param>
    internal delegate void BakeProgress(string phase, float fraction);

    /// <summary>
    /// Timing breakdown of one bake, produced by <see cref="BakeProfiler"/>.
    /// </summary>
    internal sealed class BakeReport
    {
        public readonly double TotalMs;

        /// <summary>
        /// Time spent building the SVO in memory.
        /// </summary>
        public readonly double BuildMs;

        /// <summary>
        /// Time spent on post-build work (serialize + save the asset).
        /// </summary>
        public readonly double SaveMs;

        public readonly IReadOnlyList<TimedPhase> Phases;

        public BakeReport(
            double totalMs,
            double buildMs,
            double saveMs,
            IReadOnlyList<TimedPhase> phases
        )
        {
            TotalMs = totalMs;
            BuildMs = buildMs;
            SaveMs = saveMs;
            Phases = phases;
        }
    }
}
