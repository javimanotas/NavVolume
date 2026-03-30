using System.Collections.Generic;

namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// Sparse Voxel Octree
    /// </summary>
    internal partial class SVO
    {
        SVOLeaf[] _leafNodes;

        public readonly List<SVONode>[] Layers;

        public readonly Dictionary<MortonCode, int>[] MortonToIndex;

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
                link = new SVOLink(layer, (uint)idx);
                return true;
            }

            link = SVOLink.Invalid;
            return false;
        }

        public void SetLeafNodes(SVOLeaf[] leafNodes)
        {
            _leafNodes = leafNodes;
        }

        public Stats ComputeStats() => new();
    }
}
