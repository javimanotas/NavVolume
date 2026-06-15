namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// Bake-specific front end over a shared <see cref="StepProfiler"/>.
    /// </summary>
    internal sealed class BakeProfiler
    {
        readonly StepProfiler _profiler = new();

        double _buildMs = -1;

        public void Start()
        {
            _profiler.Start();
            _buildMs = -1;
        }

        public void Lap(string label) => _profiler.Lap(label);

        /// <summary>
        /// Marks where the build ends and post-build (save) phases begin.
        /// </summary>
        public void MarkBuildComplete() => _buildMs = _profiler.TotalMs;

        /// <summary>
        /// Snapshots the collected laps into a transient <see cref="BakeReport"/>.
        /// </summary>
        public BakeReport ToReport()
        {
            var total = _profiler.TotalMs;
            var buildMs = _buildMs >= 0 ? _buildMs : total;

            var phases = new TimedPhase[_profiler.Phases.Count];
            for (var i = 0; i < phases.Length; i++)
            {
                phases[i] = _profiler.Phases[i];
            }

            return new BakeReport(total, buildMs, total - buildMs, phases);
        }
    }
}
