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

        SortedSet<MortonCode> CalculateL0Codes(List<MortonCode> l1Codes)
        {
            var l0Codes = new SortedSet<MortonCode>();

            foreach (var l1Code in l1Codes)
            {
                for (var c = 0u; c < 8; c++)
                {
                    l0Codes.Add(l1Code.ChildCode(c));
                }
            }

            return l0Codes;
        }

        void AllocateL0(SVO svo, SortedSet<MortonCode> l0Codes)
        {
            foreach (var code in l0Codes)
            {
                var nodeIdx = svo.Layers[0].Count;

                svo.Layers[0].Add(new(code));
                svo.MortonToIndex[0][code] = nodeIdx;
            }
        }

        SVOLeaf[] CalculateLeafNodes(SortedSet<MortonCode> l0Codes)
        {
            var l0NodeSize = _settings.NodeSizeForLayer(0);
            var corners = new Vector3[l0Codes.Count];
            var i = 0;

            foreach (var code in l0Codes)
            {
                var (x, y, z) = code.Decoded;
                corners[i] = _settings.Origin + new Vector3(x, y, z) * l0NodeSize;
                i++;
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

        SortedSet<MortonCode> CalculateParentCodes(SVO svo, uint childLayer)
        {
            var children = svo.Layers[childLayer];

            var parentCodes = new SortedSet<MortonCode>();

            foreach (var child in children)
            {
                parentCodes.Add(child.MortonCode.ParentCode);
            }

            return parentCodes;
        }

        void AllocateMissingSiblings(SVO svo, uint childLayer, SortedSet<MortonCode> parentCodes)
        {
            var someSiblingMissed = false;

            foreach (var pCode in parentCodes)
            {
                for (var c = 0u; c < 8; c++)
                {
                    var childCode = pCode.ChildCode(c);

                    if (!svo.MortonToIndex[childLayer].ContainsKey(childCode))
                    {
                        someSiblingMissed = true;

                        var paddingIdx = svo.Layers[childLayer].Count;
                        svo.Layers[childLayer].Add(new(childCode));
                        svo.MortonToIndex[childLayer][childCode] = paddingIdx;
                    }
                }
            }

            if (someSiblingMissed)
            {
                ResortLayer(svo, childLayer);
            }
        }

        void ResortLayer(SVO svo, uint layer)
        {
            var list = svo.Layers[layer];

            list.Sort((a, b) => a.MortonCode.CompareTo(b.MortonCode));

            var lookup = svo.MortonToIndex[layer];
            lookup.Clear();

            for (var i = 0; i < list.Count; i++)
            {
                lookup[list[i].MortonCode] = i;
            }
        }

        void AllocateParentNodes(SVO svo, uint layer, SortedSet<MortonCode> parentCodes)
        {
            foreach (var pCode in parentCodes)
            {
                var parentIdx = svo.Layers[layer].Count;
                svo.Layers[layer].Add(new(pCode));
                svo.MortonToIndex[layer][pCode] = parentIdx;
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
