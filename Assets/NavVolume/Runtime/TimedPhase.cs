namespace NavVolume.Runtime
{
    /// <summary>
    /// A single timed step (phase) recorded by a <see cref="StepProfiler"/>.
    /// </summary>
    internal readonly struct TimedPhase
    {
        public readonly string Label;
        public readonly double Milliseconds;

        public TimedPhase(string label, double milliseconds)
        {
            Label = label;
            Milliseconds = milliseconds;
        }
    }
}
