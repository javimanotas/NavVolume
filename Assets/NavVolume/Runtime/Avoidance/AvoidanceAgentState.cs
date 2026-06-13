using Unity.Mathematics;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// Per-agent input of one avoidance step, submitted by <see cref="NavVolumeAgent"/> every frame.
    /// </summary>
    internal struct AvoidanceAgentState
    {
        public float3 Position;

        /// <summary>
        /// Velocity the agent actually realized last frame; the current velocity in the ORCA sense.
        /// </summary>
        public float3 Velocity;

        /// <summary>
        /// Velocity the agent would take if it were alone (path following).
        /// </summary>
        public float3 PrefVelocity;

        public float Radius;

        public float MaxSpeed;

        /// <summary>
        /// Maximum distance at which other agents are considered.
        /// </summary>
        public float NeighborRange;

        public float TimeHorizonAgents;

        public float TimeHorizonObstacles;

        public int MaxNeighbors;

        /// <summary>
        /// Index of the agent's <see cref="NavVolumeSpace"/> in the simulation's voxel grid table,
        /// or -1 when the agent is not bound to a space.
        /// </summary>
        public int SpaceIndex;
    }
}
