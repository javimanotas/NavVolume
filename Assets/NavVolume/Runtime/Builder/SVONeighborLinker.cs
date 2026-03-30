using NavVolume.Runtime.Core;
using UnityEngine;

namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// Runs the last stage of the build pipeline: filling the six face-neighbor links on every node.
    /// </summary>
    internal static class SVONeighborLinker
    {
        /// <summary>
        /// Fills the links from the upper to lower layers.
        /// </summary>
        public static void FillNeighborLinks(SVO svo, BuildSettings settings)
        {
            for (var layer = (uint)svo.Layers.Length - 1; layer >= 0; layer--)
            {
                var gridRes = Mathf.RoundToInt(
                    settings.RootSize / settings.NodeSizeForLayer((int)layer)
                );

                var nodes = svo.Layers[layer];

                for (var nodeIdx = 0; nodeIdx < nodes.Count; nodeIdx++)
                {
                    var node = nodes[nodeIdx];

                    for (var d = 0U; d < 6; d++)
                    {
                        var dir = (NeighborDirection)d;

                        #region Same layer neighbor

                        if (node.MortonCode.TryGetNeighborCode(dir, gridRes, out MortonCode nCode))
                        {
                            if (svo.TryGetLink(layer, nCode, out SVOLink neighborLink))
                            {
                                node.Neighbors[dir] = neighborLink;
                                continue;
                            }
                        }

                        #endregion

                        #region Inherit from parent

                        // ── Case B: inherit from parent ───────────────
                        if (node.Parent.IsValid)
                        {
                            var inherited = svo.GetNode(node.Parent).Neighbors[dir];
                            node.Neighbors[dir] = inherited; // may be Invalid at world boundary
                            continue;
                        }

                        #endregion

                        node.Neighbors[dir] = SVOLink.Invalid;
                    }

                    nodes[nodeIdx] = node;
                }
            }
        }
    }
}
