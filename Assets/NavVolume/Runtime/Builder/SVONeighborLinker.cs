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
        /// <remarks>
        /// Same-layer neighbor lookups dominate this pass (6 binary searches per node). To keep them
        /// cache-friendly we search a flat <c>uint[]</c> of Morton codes per layer instead of the
        /// <see cref="SVONode"/> list, which would copy the whole ~40-byte struct at every search step.
        /// </remarks>
        public static void FillNeighborLinks(SVO svo, BuildSettings settings)
        {
            var layerCount = svo.Layers.Length;

            // Flat, sorted Morton codes per layer for copy-free binary search.
            var mortonByLayer = new uint[layerCount][];
            for (var layer = 0; layer < layerCount; layer++)
            {
                var nodes = svo.Layers[layer];
                var mortons = new uint[nodes.Count];
                for (var i = 0; i < nodes.Count; i++)
                {
                    mortons[i] = nodes[i].MortonCode;
                }
                mortonByLayer[layer] = mortons;
            }

            for (var layer = layerCount - 1; layer >= 0; layer--)
            {
                var gridRes = Mathf.RoundToInt(
                    settings.RootSize / settings.NodeSizeForLayer(layer)
                );

                var nodes = svo.Layers[layer];
                var mortons = mortonByLayer[layer];

                for (var nodeIdx = 0; nodeIdx < nodes.Count; nodeIdx++)
                {
                    var node = nodes[nodeIdx];

                    // Parent's neighbors are inherited on any side without a same-layer neighbor.
                    // Fetch them once per node rather than re-copying the parent for all six sides.
                    var hasParent = node.Parent.IsValid;
                    var parentNeighbors = hasParent ? svo.GetNode(node.Parent).Neighbors : default;

                    for (var d = 0; d < 6; d++)
                    {
                        var dir = (NeighborDirection)d;

                        #region Same layer neighbor

                        if (
                            node.MortonCode.TryGetNeighborCode(dir, gridRes, out var nCode)
                            && TryFindIndex(mortons, nCode, out var nIdx)
                        )
                        {
                            node.Neighbors[dir] = SVOLink.NodeLink(layer, nIdx);
                            continue;
                        }

                        #endregion

                        #region Inherit from parent

                        if (hasParent)
                        {
                            node.Neighbors[dir] = parentNeighbors[dir];
                            continue;
                        }

                        #endregion

                        node.Neighbors[dir] = SVOLink.Invalid;
                    }

                    nodes[nodeIdx] = node;
                }
            }
        }

        /// <summary>
        /// Binary-searches a sorted Morton array for <paramref name="target"/>.
        /// </summary>
        static bool TryFindIndex(uint[] sorted, uint target, out int idx)
        {
            var lo = 0;
            var hi = sorted.Length - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                var value = sorted[mid];

                if (value == target)
                {
                    idx = mid;
                    return true;
                }

                if (value < target)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }

            idx = -1;
            return false;
        }
    }
}
