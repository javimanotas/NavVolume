using System.Collections.Generic;
using NavVolume.Runtime.Builder;
using UnityEngine;

namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// Sparse Voxel Octree
    /// </summary>
    internal partial class SVO
    {
        public SVOLeaf[] LeafNodes;

        /// <summary>
        /// Lower layers of the octree are the ones containing higher resolution data.
        /// </summary>
        /// <remarks>
        /// Each layer is kept sorted by <see cref="MortonCode"/>; lookups use
        /// <see cref="TryFindNodeIndex"/> (binary search) rather than a hash table.
        /// </remarks>
        public readonly List<SVONode>[] Layers;

        /// <summary>
        /// Raw constructor.
        /// </summary>
        public SVO(SVOLeaf[] leafNodes, List<SVONode>[] layers)
        {
            LeafNodes = leafNodes;
            Layers = layers;
        }

        public SVO(int numLayers)
        {
            Layers = new List<SVONode>[numLayers];

            for (var i = 0; i < numLayers; i++)
            {
                Layers[i] = new();
            }
        }

        public bool IsEmpty => Layers[0].Count == 0;

        public SVONode GetNode(SVOLink link)
        {
            if (link.IsNode(out var layerIdx))
            {
                return Layers[layerIdx][(int)link.Offset];
            }

            Debug.LogError(
                "[NavVolume][SVO] This code should be never reached. Link is not a node link."
            );
            return Layers[layerIdx][(int)link.Offset];
        }

        public void SetNode(SVOLink link, in SVONode node)
        {
            if (link.IsNode(out var layerIdx))
            {
                Layers[layerIdx][(int)link.Offset] = node;
                return;
            }

            Debug.LogError(
                "[NavVolume][SVO] This code should be never reached. Link is not a node link."
            );
        }

        /// <summary>
        /// Binary-searches the (sorted) layer for the node carrying <paramref name="mortonCode"/>.
        /// </summary>
        /// <returns>true and the node's offset when found, false and -1 otherwise.</returns>
        public bool TryFindNodeIndex(uint layer, MortonCode mortonCode, out int idx)
        {
            var nodes = Layers[layer];
            var target = (uint)mortonCode;
            var lo = 0;
            var hi = nodes.Count - 1;

            while (lo <= hi)
            {
                var mid = (lo + hi) >> 1;
                var midCode = (uint)nodes[mid].MortonCode;

                if (midCode == target)
                {
                    idx = mid;
                    return true;
                }

                if (midCode < target)
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

        public bool TryGetLink(uint layer, MortonCode mortonCode, out SVOLink link)
        {
            if (TryFindNodeIndex(layer, mortonCode, out var idx))
            {
                link = SVOLink.NodeLink(layer, (uint)idx);
                return true;
            }

            link = SVOLink.Invalid;
            return false;
        }

        public bool IsVoxelOccupied(int x, int y, int z)
        {
            var (nodeX, nodeY, nodeZ) = (x >> 2, y >> 2, z >> 2);
            var (subNodeX, subNodeY, subNodeZ) = (x & 0b11, y & 0b11, z & 0b11);

            var morton = new MortonCode((uint)nodeX, (uint)nodeY, (uint)nodeZ);
            if (!TryFindNodeIndex(0, morton, out var nodeIdx))
            {
                return false;
            }

            var leaf = LeafNodes[nodeIdx];
            var bitIdx = SVOLeaf.SubnodeCoordsToIndex(subNodeX, subNodeY, subNodeZ);
            return leaf.IsOccupied(bitIdx);
        }
    }
}
