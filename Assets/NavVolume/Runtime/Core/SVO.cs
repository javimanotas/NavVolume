using System.Collections.Generic;

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
            link.IsNode(out var layerIdx);
            return Layers[layerIdx][link.Offset];
        }

        public void SetNode(SVOLink link, in SVONode node)
        {
            link.IsNode(out var layerIdx);
            Layers[layerIdx][link.Offset] = node;
        }

        /// <summary>
        /// Binary-searches the (sorted) layer for the node carrying <paramref name="mortonCode"/>.
        /// </summary>
        /// <returns>true and the node's offset when found, false and -1 otherwise.</returns>
        public bool TryFindNodeIndex(int layer, MortonCode mortonCode, out int idx)
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

        public bool TryGetLink(int layer, MortonCode mortonCode, out SVOLink link)
        {
            if (TryFindNodeIndex(layer, mortonCode, out var idx))
            {
                link = SVOLink.NodeLink(layer, idx);
                return true;
            }

            link = SVOLink.Invalid;
            return false;
        }

        public bool IsVoxelOccupied(int x, int y, int z)
        {
            var (nodeX, nodeY, nodeZ) = (x >> 2, y >> 2, z >> 2);
            var (subNodeX, subNodeY, subNodeZ) = (x & 0b11, y & 0b11, z & 0b11);

            var morton = new MortonCode(nodeX, nodeY, nodeZ);
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
