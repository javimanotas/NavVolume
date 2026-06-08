using NavVolume.Runtime.Pathfinding;
using UnityEngine;

namespace NavVolume.Runtime
{
    /// <summary>
    /// Defines parameters for an agent used both in the bake and pathfinding.
    /// </summary>
    [CreateAssetMenu(fileName = "NavVolumeAgentType", menuName = "NavVolume/Agent Type")]
    public class AgentType : ScriptableObject
    {
        [field: Header("Agent")]
        [field: SerializeField]
        [field: Tooltip("The radius of the agent.")]
        public float Radius { get; private set; } = 1;

        [field: Header("Pathfinding")]
        [field: SerializeField]
        [field: Tooltip(
            "How the g-cost is measured. Node Count biases the path toward large, open nodes and "
                + "keeps waypoints clear of surfaces; Euclidean Distance produces the geometrically "
                + "shortest path, which hugs surfaces."
        )]
        public PathCostMode CostMode { get; private set; } = PathCostMode.NodeCount;

        [field: SerializeField]
        [field: Tooltip(
            "Heuristic weight. Greater values imply faster results but with worse quality."
        )]
        [field: Range(1f, 5f)]
        public float HeuristicWeight { get; private set; } = 1.5f;
    }
}
