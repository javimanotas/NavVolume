using NavVolume.Core;

namespace NavVolume.Pathfinding
{
    /// <summary>
    /// Node with relevant information for A* search.
    /// </summary>
    internal readonly struct SearchNode
    {
        public readonly SVOLink Link;

        public readonly float GCost;

        public SearchNode(SVOLink link, float gCost)
        {
            Link = link;
            GCost = gCost;
        }
    }
}
