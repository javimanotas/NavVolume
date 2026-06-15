using System.Collections.Generic;
using System.Diagnostics;

namespace NavVolume.Runtime
{
    /// <summary>
    /// Lightweight step timer with a built-in stopwatch.
    /// </summary>
    internal sealed class StepProfiler
    {
        readonly List<TimedPhase> _phases = new();

        readonly Stopwatch _stopwatch = new();

        public IReadOnlyList<TimedPhase> Phases => _phases;

        /// <summary>
        /// Sum of every recorded phase, in milliseconds.
        /// </summary>
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

        /// <summary>
        /// Clears any recorded phases and (re)starts the stopwatch.
        /// </summary>
        public void Start()
        {
            _phases.Clear();
            _stopwatch.Restart();
        }

        /// <summary>
        /// Records the time elapsed since the previous lap or since it started a named phase.
        /// </summary>
        public void Lap(string label)
        {
            _phases.Add(new TimedPhase(label, _stopwatch.Elapsed.TotalMilliseconds));
            _stopwatch.Restart();
        }
    }
}
