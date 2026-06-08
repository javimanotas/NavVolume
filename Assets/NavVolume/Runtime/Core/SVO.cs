using System;

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
        /// Each layer is a flat <see cref="SVONode"/> array kept sorted by <see cref="MortonCode"/>;
        /// lookups use <see cref="TryFindNodeIndex"/> (binary search) rather than a hash table. Plain
        /// arrays (instead of <c>List</c>) let callers read a single field through an array element
        /// without copying the whole ~36-byte node, which is the dominant cost on the pathfinding hot
        /// path (see <see cref="GetNode"/>).
        /// </remarks>
        public readonly SVONode[][] Layers;

        /// <summary>
        /// Raw constructor.
        /// </summary>
        public SVO(SVOLeaf[] leafNodes, SVONode[][] layers)
        {
            LeafNodes = leafNodes;
            Layers = layers;
        }

        public SVO(int numLayers)
        {
            Layers = new SVONode[numLayers][];

            for (var i = 0; i < numLayers; i++)
            {
                Layers[i] = Array.Empty<SVONode>();
            }
        }

        public bool IsEmpty => Layers[0].Length == 0;

        /// <summary>
        /// Returns a read-only reference to the node, avoiding a copy of the whole struct. Callers that
        /// only read fields should bind it with <c>ref readonly var node = ref svo.GetNode(link);</c>.
        /// </summary>
        public ref readonly SVONode GetNode(SVOLink link)
        {
            link.IsNode(out var layerIdx);
            return ref Layers[layerIdx][link.Offset];
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
            var hi = nodes.Length - 1;

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
