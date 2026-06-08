using System.Collections.Generic;
using System.Diagnostics;

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

    /// <summary>
    /// Lightweight step timer with a built-in stopwatch. Call <see cref="Start"/> once, then
    /// <see cref="Lap"/> after each step: every lap records the time elapsed since the previous lap
    /// as a named <see cref="TimedPhase"/>. Shared by the bake (via <c>BakeProfiler</c>) and the
    /// pathfinder so both report per-step timings the same way.
    /// </summary>
    internal sealed class StepProfiler
    {
        readonly List<TimedPhase> _phases = new();
        readonly Stopwatch _stopwatch = new();

        /// <summary>Phases recorded so far, in execution order.</summary>
        public IReadOnlyList<TimedPhase> Phases => _phases;

        /// <summary>Sum of every recorded phase, in milliseconds.</summary>
        public double TotalMs
        {
            get
            {
                var sum = 0.0;
                foreach (var phase in _phases)
                {
                    sum += phase.Milliseconds;
                }
                return sum;
            }
        }

        /// <summary>Clears any recorded phases and (re)starts the stopwatch.</summary>
        public void Start()
        {
            _phases.Clear();
            _stopwatch.Restart();
        }

        /// <summary>
        /// Records the time elapsed since the previous lap (or <see cref="Start"/>) as a named phase,
        /// then restarts the stopwatch for the next step.
        /// </summary>
        public void Lap(string label)
        {
            _phases.Add(new TimedPhase(label, _stopwatch.Elapsed.TotalMilliseconds));
            _stopwatch.Restart();
        }
    }
}
