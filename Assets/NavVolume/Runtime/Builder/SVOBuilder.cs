using System.Collections.Generic;
using NavVolume.Runtime.Core;
using UnityEngine;

namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// Builds an SVO and returns the tree with some stats.
    /// </summary>
    internal class SVOBuilder
    {
        readonly BuildSettings _settings;

        BakeProfiler _profiler;
        BakeProgress _progress;

        public SVOBuilder(BuildSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Builds the SVO, recording per-phase timings into <paramref name="profiler"/> and, when
        /// supplied, reporting coarse progress through <paramref name="progress"/>. The host's
        /// reporter may throw at a phase boundary to cancel the bake.
        /// </summary>
        internal NavContext Build(BakeProfiler profiler, BakeProgress progress = null)
        {
            _profiler = profiler;
            _progress = progress;
            _profiler.Start();

            var svo = new SVO(_settings.NumLayers);

            _progress?.Invoke("Rasterizing volume", 0.05f);
            var occupiedL1 = SVORasterizer.RasterizeL1(_settings);
            _profiler.Lap($"RasterizeL1 ({occupiedL1.Count} cells)");

            AllocateLowerLayers(svo, occupiedL1);

            _progress?.Invoke("Building upper layers", 0.68f);
            for (var layer = 1; layer < svo.Layers.Length; layer++)
            {
                BuildUpperLayer(svo, layer);
            }
            _profiler.Lap("BuildUpperLayers");

            _progress?.Invoke("Linking parents and children", 0.74f);
            for (var layer = 1; layer < svo.Layers.Length; layer++)
            {
                LinkParentAndChildren(svo, layer, layer - 1);
            }
            _profiler.Lap("LinkParentAndChildren");

            _progress?.Invoke("Linking neighbors", 0.80f);
            SVONeighborLinker.FillNeighborLinks(svo, _settings);
            _profiler.Lap("FillNeighborLinks");

            _profiler.MarkBuildComplete();

            var navCtx = new NavContext(svo, _settings);
            return navCtx;
        }

        #region Lower layers allocation

        void AllocateLowerLayers(SVO svo, List<MortonCode> l1Codes)
        {
            _progress?.Invoke("Rasterizing leaf voxels", 0.12f);

            var l0Codes = CalculateL0Codes(l1Codes);
            _profiler.Lap($"CalculateL0Codes ({l0Codes.Count})");

            AllocateL0(svo, l0Codes);
            _profiler.Lap("AllocateL0");

            var leafNodes = CalculateLeafNodes(l0Codes);
            svo.LeafNodes = leafNodes;
            _profiler.Lap("RasterizeLeaves");
        }

        List<MortonCode> CalculateL0Codes(List<MortonCode> l1Codes)
        {
            var dedup = new HashSet<MortonCode>(l1Codes.Count * 8);

            foreach (var l1Code in l1Codes)
            {
                for (var c = 0; c < 8; c++)
                {
                    dedup.Add(l1Code.ChildCode(c));
                }
            }

            var l0Codes = new List<MortonCode>(dedup);
            l0Codes.Sort();
            return l0Codes;
        }

        void AllocateL0(SVO svo, List<MortonCode> l0Codes)
        {
            var layer = new SVONode[l0Codes.Count];
            for (var i = 0; i < l0Codes.Count; i++)
            {
                layer[i] = new SVONode(l0Codes[i]);
            }

            svo.Layers[0] = layer;
        }

        SVOLeaf[] CalculateLeafNodes(List<MortonCode> l0Codes)
        {
            var l0NodeSize = _settings.NodeSizeForLayer(0);
            var corners = new Vector3[l0Codes.Count];

            for (var i = 0; i < l0Codes.Count; i++)
            {
                var (x, y, z) = l0Codes[i].Decoded;
                corners[i] = _settings.Origin + new Vector3(x, y, z) * l0NodeSize;
            }

            return SVORasterizer.RasterizeLeaves(_settings, corners);
        }

        #endregion

        #region Upper layer build

        void BuildUpperLayer(SVO svo, int layer)
        {
            var childLayer = layer - 1;
            var parentCodes = CalculateParentCodes(svo, childLayer);

            AllocateMissingSiblings(svo, childLayer, parentCodes);
            AllocateParentNodes(svo, layer, parentCodes);
        }

        List<MortonCode> CalculateParentCodes(SVO svo, int childLayer)
        {
            var children = svo.Layers[childLayer];
            var dedup = new HashSet<MortonCode>(children.Length);

            for (var i = 0; i < children.Length; i++)
            {
                dedup.Add(children[i].MortonCode.ParentCode);
            }

            var parentCodes = new List<MortonCode>(dedup);
            parentCodes.Sort();
            return parentCodes;
        }

        /// <summary>
        /// Ensures every parent in <paramref name="parentCodes"/> has all 8 of its children
        /// present in <c>Layer[childLayer]</c>, in Morton-sorted order.
        /// </summary>
        /// <remarks>
        /// Instead of appending missing padding nodes and re-sorting the whole layer (an extra
        /// O(N log N) sort every time a sibling is missing), this generates the complete
        /// expected child set, sorts it once, and rebuilds the layer + lookup in a single pass.
        /// Short-circuits cheaply when nothing is missing.
        /// </remarks>
        void AllocateMissingSiblings(SVO svo, int childLayer, List<MortonCode> parentCodes)
        {
            var layer = svo.Layers[childLayer];
            var expectedCount = parentCodes.Count * 8;

            // Every existing child's parent is by construction in parentCodes, so the layer
            // size can only be <= expectedCount. Equality means no siblings are missing and
            // the layer is already sorted from the previous build step.
            if (layer.Length == expectedCount)
            {
                return;
            }

            var allChildren = new List<MortonCode>(expectedCount);
            foreach (var pCode in parentCodes)
            {
                for (var c = 0; c < 8; c++)
                {
                    allChildren.Add(pCode.ChildCode(c));
                }
            }
            allChildren.Sort();

            var rebuilt = new SVONode[expectedCount];
            for (var i = 0; i < allChildren.Count; i++)
            {
                rebuilt[i] = new SVONode(allChildren[i]);
            }

            svo.Layers[childLayer] = rebuilt;
        }

        void AllocateParentNodes(SVO svo, int layer, List<MortonCode> parentCodes)
        {
            var parentLayer = new SVONode[parentCodes.Count];
            for (var i = 0; i < parentCodes.Count; i++)
            {
                parentLayer[i] = new SVONode(parentCodes[i]);
            }

            svo.Layers[layer] = parentLayer;
        }

        #endregion

        void LinkParentAndChildren(SVO svo, int layer, int childLayer)
        {
            var children = svo.Layers[childLayer];
            var parents = svo.Layers[layer];

            for (var childIdx = 0; childIdx < children.Length; childIdx++)
            {
                var parentCode = children[childIdx].MortonCode.ParentCode;
                svo.TryFindNodeIndex(layer, parentCode, out var parentIdx);

                children[childIdx].Parent = SVOLink.NodeLink(layer, parentIdx);

                ref var parent = ref parents[parentIdx];
                if (!parent.HasChildren)
                {
                    parent.FirstChild = SVOLink.NodeLink(childLayer, childIdx);
                }
            }
        }
    }
}
