using NavVolume.Runtime.Core;

namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// Wrapper for the built SVO.
    /// </summary>
    /// <remarks>
    /// The <see cref="ToString"/> displays a lot of information about its build process.
    /// </remarks>
    internal readonly struct BuildResult
    {
        public readonly SVO Svo;

        readonly double _buildTimeMs;

        public BuildResult(SVO svo, double buildTimeMs)
        {
            Svo = svo;
            _buildTimeMs = buildTimeMs;
        }

        public override string ToString() =>
            $"SVO built in {_buildTimeMs:F1} ms\n{Svo.ComputeStats()}";
    }
}
