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
        public float Radius { get; private set; } = 1;

        [field: SerializeField]
        [Tooltip("Heuristic weight. Greater values imply faster results but with worse quality.")]
        [Range(1f, 5f)]
        public float HeuristicWeight { get; private set; } = 1.5f;
    }
}
