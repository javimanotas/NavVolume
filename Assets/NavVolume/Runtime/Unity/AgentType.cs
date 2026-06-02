using UnityEngine;

namespace NavVolume.Runtime
{
    /// <summary>
    /// Defines parameters for an agent used both in the bake and pathfinding.
    /// </summary>
    [CreateAssetMenu(fileName = "NavVolumeAgentType", menuName = "NavVolume/Agent Type")]
    public class AgentType : ScriptableObject
    {
        [field: SerializeField]
        [Tooltip("The radius of the agent.")]
        public float Radius { get; private set; } = 0;

        // TODO: revisit pathfinding parameters and heuristics.

        [field: SerializeField]
        [Tooltip("How close the agent must get to a waypoint before advancing.")]
        public float WaypointTolerance { get; private set; } = 0.1f;

        [field: SerializeField]
        [Tooltip("Heuristic weight. Greater values imply faster results but with worse quality.")]
        [Range(1f, 5f)]
        public float HeuristicWeight { get; private set; } = 1.5f;

        [field: SerializeField]
        [Tooltip("Maximum A* nodes expanded before giving up. 0 = unlimited.")]
        public int MaxNodesBudget { get; private set; } = 100_000;
    }
}
