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

        public SVOBuilder(BuildSettings settings)
        {
            _settings = settings;
        }

        // TODO: implement an async builder that can be cancelled and reports progress.

        public NavContext Build()
        {
            var svo = new SVO(_settings.NumLayers);

            var occupiedL1 = SVORasterizer.RasterizeL1(_settings);

            AllocateLowerLayers(svo, occupiedL1);

            for (var layer = 1u; layer < svo.Layers.Length; layer++)
            {
                BuildUpperLayer(svo, layer);
            }

            for (var layer = 1u; layer < svo.Layers.Length; layer++)
            {
                LinkParentAndChildren(svo, layer, layer - 1);
            }

            SVONeighborLinker.FillNeighborLinks(svo, _settings);

            var navCtx = new NavContext(svo, _settings);
            return navCtx;
        }

        #region Lower layers allocation

        void AllocateLowerLayers(SVO svo, List<MortonCode> l1Codes)
        {
            var l0Codes = CalculateL0Codes(l1Codes);

            AllocateL0(svo, l0Codes);

            var leafNodes = CalculateLeafNodes(l0Codes);
            svo.LeafNodes = leafNodes;
        }

        List<MortonCode> CalculateL0Codes(List<MortonCode> l1Codes)
        {
            var dedup = new HashSet<MortonCode>(l1Codes.Count * 8);

            foreach (var l1Code in l1Codes)
            {
                for (var c = 0u; c < 8; c++)
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
            var layer = svo.Layers[0];
            if (layer.Capacity < l0Codes.Count)
            {
                layer.Capacity = l0Codes.Count;
            }

            foreach (var code in l0Codes)
            {
                layer.Add(new(code));
            }
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

        void BuildUpperLayer(SVO svo, uint layer)
        {
            var childLayer = layer - 1;
            var parentCodes = CalculateParentCodes(svo, childLayer);

            AllocateMissingSiblings(svo, childLayer, parentCodes);
            AllocateParentNodes(svo, layer, parentCodes);
        }

        List<MortonCode> CalculateParentCodes(SVO svo, uint childLayer)
        {
            var children = svo.Layers[childLayer];
            var dedup = new HashSet<MortonCode>(children.Count);

            foreach (var child in children)
            {
                dedup.Add(child.MortonCode.ParentCode);
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
        void AllocateMissingSiblings(SVO svo, uint childLayer, List<MortonCode> parentCodes)
        {
            var layer = svo.Layers[childLayer];
            var expectedCount = parentCodes.Count * 8;

            // Every existing child's parent is by construction in parentCodes, so the layer
            // size can only be <= expectedCount. Equality means no siblings are missing and
            // the layer is already sorted from the previous build step.
            if (layer.Count == expectedCount)
            {
                return;
            }

            var allChildren = new List<MortonCode>(expectedCount);
            foreach (var pCode in parentCodes)
            {
                for (var c = 0u; c < 8; c++)
                {
                    allChildren.Add(pCode.ChildCode(c));
                }
            }
            allChildren.Sort();

            layer.Clear();
            if (layer.Capacity < expectedCount)
            {
                layer.Capacity = expectedCount;
            }

            for (var i = 0; i < allChildren.Count; i++)
            {
                layer.Add(new(allChildren[i]));
            }
        }

        void AllocateParentNodes(SVO svo, uint layer, List<MortonCode> parentCodes)
        {
            var parentLayer = svo.Layers[layer];
            if (parentLayer.Capacity < parentLayer.Count + parentCodes.Count)
            {
                parentLayer.Capacity = parentLayer.Count + parentCodes.Count;
            }

            foreach (var pCode in parentCodes)
            {
                parentLayer.Add(new(pCode));
            }
        }

        #endregion

        void LinkParentAndChildren(SVO svo, uint layer, uint childLayer)
        {
            var sortedChildren = svo.Layers[childLayer];

            for (var childIdx = 0; childIdx < sortedChildren.Count; childIdx++)
            {
                var child = sortedChildren[childIdx];
                var parentCode = child.MortonCode.ParentCode;

                svo.TryGetLink(layer, parentCode, out var parentLink);

                child.Parent = parentLink;
                svo.Layers[childLayer][childIdx] = child;

                var parentNode = svo.GetNode(parentLink);
                if (!parentNode.HasChildren)
                {
                    parentNode.FirstChild = SVOLink.NodeLink(childLayer, (uint)childIdx);
                    svo.SetNode(parentLink, parentNode);
                }
            }
        }
    }
}
