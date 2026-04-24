using System.Collections.Generic;

namespace NavVolume.Core
{
    /// <summary>
    /// Sparse Voxel Octree
    /// </summary>
    internal partial class SVO
    {
        // TODO: consider encapsulating all fields

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

        // TODO: consider getting a ref instead of a copy. this would allow to remove SetNode
        public SVONode GetNode(SVOLink link)
        {
            return Layers[link.LayerIdx][(int)link.NodeIdx];
        }

        public void SetNode(SVOLink link, in SVONode node)
        {
            Layers[link.LayerIdx][(int)link.NodeIdx] = node;
        }

        public bool TryGetLink(uint layer, MortonCode mortonCode, out SVOLink link)
        {
            if (MortonToIndex[layer].TryGetValue(mortonCode, out var idx))
            {
                link = new(layer, (uint)idx);
                return true;
            }

            link = SVOLink.Invalid;
            return false;
        }
    }
}
