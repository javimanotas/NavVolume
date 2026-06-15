namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// Bake-specific front end over a shared <see cref="StepProfiler"/>. Adds the build-vs-save split
    /// and snapshots the collected laps into a transient <see cref="BakeReport"/>.
    /// </summary>
    internal sealed class BakeProfiler
    {
        readonly StepProfiler _profiler = new();

        // Running total (ms) captured when the build finished; -1 until then.
        double _buildMs = -1;

        public void Start()
        {
            _profiler.Start();
            _buildMs = -1;
        }

        public void Lap(string label) => _profiler.Lap(label);

        /// <summary>Marks where the build ends and post-build (save) phases begin.</summary>
        public void MarkBuildComplete() => _buildMs = _profiler.TotalMs;

        /// <summary>Snapshots the collected laps into a transient <see cref="BakeReport"/>.</summary>
        public BakeReport ToReport()
        {
            var total = _profiler.TotalMs;
            var buildMs = _buildMs >= 0 ? _buildMs : total;

            // Copy so the report stays independent of the (reusable) profiler's live phase list.
            var phases = new TimedPhase[_profiler.Phases.Count];
            for (var i = 0; i < phases.Length; i++)
            {
                phases[i] = _profiler.Phases[i];
            }

            return new BakeReport(total, buildMs, total - buildMs, phases);
        }
    }
}
