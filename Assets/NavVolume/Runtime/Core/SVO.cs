using System.Collections.Generic;
using NavVolume.Runtime.Builder;
using UnityEngine;
using UnityEngine.Assertions;

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
        public readonly List<SVONode>[] Layers;

        public readonly Dictionary<MortonCode, int>[] MortonToIndex;

        /// <summary>
        /// Raw constructor.
        /// </summary>
        public SVO(SVOLeaf[] leafNodes, List<SVONode>[] layers)
        {
            LeafNodes = leafNodes;
            Layers = layers;

            MortonToIndex = new Dictionary<MortonCode, int>[Layers.Length];

            for (var layerIndex = 0; layerIndex < Layers.Length; layerIndex++)
            {
                var layer = Layers[layerIndex];
                var dict = new Dictionary<MortonCode, int>(layer.Count);

                for (var i = 0; i < layer.Count; i++)
                {
                    dict[layer[i].MortonCode] = i;
                }

                MortonToIndex[layerIndex] = dict;
            }
        }

        public SVO(int numLayers)
        {
            Layers = new List<SVONode>[numLayers];
            MortonToIndex = new Dictionary<MortonCode, int>[numLayers];

            for (var i = 0; i < numLayers; i++)
            {
                Layers[i] = new();
                MortonToIndex[i] = new();
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

        public bool TryGetLink(uint layer, MortonCode mortonCode, out SVOLink link)
        {
            if (MortonToIndex[layer].TryGetValue(mortonCode, out var idx))
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
            if (!MortonToIndex[0].TryGetValue(morton, out var nodeIdx))
            {
                return false;
            }

            var leaf = LeafNodes[nodeIdx];
            var bitIdx = SVOLeaf.SubnodeCoordsToIndex(subNodeX, subNodeY, subNodeZ);
            return leaf.IsOccupied(bitIdx);
        }
    }
}
