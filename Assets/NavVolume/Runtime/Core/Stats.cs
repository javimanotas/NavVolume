namespace NavVolume.Core
{
    internal partial class SVO
    {
        struct Stats
        {
            // TODO: implement SVO stats (memory used, nodes per layer...)
            // consider if the best approach is to be defined inside the SVO to access private data.
            // consider if this struct should be tottaly private or if should be exposed in some way for displaying data in the editor or for debugging purposes (not using .ToString()).

            public double BuildTimeMs;

            public override readonly string ToString() => $"SVO built in {BuildTimeMs} ms";
        }

        Stats? _stats;

        public void CalculateStats(double buildTimeMs)
        {
            _stats = new() { BuildTimeMs = buildTimeMs };
        }

        public override string ToString() => _stats?.ToString() ?? "SVO stats not calculated yet";
    }
}
