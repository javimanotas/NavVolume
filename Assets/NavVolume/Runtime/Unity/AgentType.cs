using NavVolume.Runtime.Avoidance;
using NavVolume.Runtime.Pathfinding;
using UnityEngine;

namespace NavVolume.Runtime
{
    /// <summary>
    /// Defines parameters for an agent used both in the bake and pathfinding.
    /// </summary>
    [CreateAssetMenu(fileName = "NavVolumeAgentType", menuName = "NavVolume/Agent Type")]
    internal class AgentType : ScriptableObject
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

        [field: Header("Avoidance")]
        [field: SerializeField]
        [field: Tooltip("Maximum distance at which other agents are considered for avoidance.")]
        [field: Min(0f)]
        public float AvoidanceNeighborRange { get; private set; } = 10f;

        [field: SerializeField]
        [field: Tooltip(
            "Maximum number of nearby agents considered for avoidance. Lower values are cheaper."
        )]
        [field: Range(0, AvoidanceJob.MAX_NEIGHBORS)]
        public int AvoidanceMaxNeighbors { get; private set; } = 10;

        [field: SerializeField]
        [field: Tooltip(
            "How many seconds ahead collisions with other agents are anticipated. "
                + "Larger values react earlier but constrain movement more."
        )]
        [field: Range(0.05f, 10f)]
        public float AvoidanceTimeHorizonAgents { get; private set; } = 2f;

        [field: SerializeField]
        [field: Tooltip(
            "How many seconds ahead collisions with obstacles and baked geometry are anticipated."
        )]
        [field: Range(0.05f, 10f)]
        public float AvoidanceTimeHorizonObstacles { get; private set; } = 1f;
    }
}
