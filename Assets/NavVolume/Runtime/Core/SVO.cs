using System.Collections.Generic;

namespace NavVolume.Runtime.Core
{
    /// <summary>
    /// Sparse Voxel Octree
    /// </summary>
    internal partial class SVO
    {
        readonly SVONode[][] _layers;

        SVOLeaf[] _leafNodes;

        public SVO(int numLayers)
        {
            _layers = new SVONode[numLayers][];
        }

        public int NumLayers => _layers.Length;

        public IReadOnlyList<SVONode> NodesFromLayer(int layer) => _layers[layer];

        public ref SVONode GetNodeRef(SVOLink link) => ref _layers[link.LayerIdx][link.NodeIdx];

        public void AddLayer(uint layer, List<MortonCode> codes)
        {
            _layers[layer] = new SVONode[codes.Count];

            for (var i = 0; i < _layers[layer].Length; i++)
            {
                _layers[layer][i] = new(codes[i]);
            }
        }

        public void SetLeafNodes(SVOLeaf[] leafNodes)
        {
            _leafNodes = leafNodes;
        }
    }
}
