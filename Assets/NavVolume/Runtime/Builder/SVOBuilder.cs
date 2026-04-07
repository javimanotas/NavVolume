using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public BuildResult Build()
        {
            var stopWatch = Stopwatch.StartNew();
            var svo = new SVO(_settings.NumLayers);

            var occupiedL1 = SVORasterizer.RasterizeL1(_settings);

            if (occupiedL1.Count == 0)
            {
                // TODO: check if this has to be handled differently
            }

            AllocateLowerLayers(svo, occupiedL1);

            for (var layer = 1U; layer < svo.Layers.Length; layer++)
            {
                BuildUpperLayer(svo, layer);
            }

            SVONeighborLinker.FillNeighborLinks(svo, _settings);

            return new(svo, stopWatch.ElapsedMilliseconds);
        }

        void AllocateLowerLayers(SVO svo, List<MortonCode> l1Codes)
        {
            var l0Codes = CalculateL0Codes(l1Codes);

            AllocateL0(svo, l0Codes);

            var leafNodes = CalculateLeafNodes(l0Codes);
            svo.SetLeafNodes(leafNodes);
        }

        #region Lower layers allocation

        SortedSet<MortonCode> CalculateL0Codes(List<MortonCode> l1Codes)
        {
            var l0Codes = new SortedSet<MortonCode>();

            foreach (var l1Code in l1Codes)
            {
                for (var c = 0U; c < 8; c++)
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
            var leafNodes = new SVOLeaf[l0Codes.Count];
            var l0NodeSize = _settings.NodeSizeForLayer(0);
            var i = 0;

            foreach (var code in l0Codes)
            {
                var (x, y, z) = code.Decoded;
                var nodeWorld = _settings.Origin + new Vector3(x, y, z) * l0NodeSize;

                var leaf = SVORasterizer.RasterizeLeaf(_settings, nodeWorld);
                leafNodes[i] = leaf;
                i++;
            }

            return leafNodes;
        }

        #endregion

        void BuildUpperLayer(SVO svo, uint layer)
        {
            var childLayer = layer - 1;
            var parentCodes = CalculateParentCodes(svo, childLayer);

            AllocateMissingSiblings(svo, childLayer, parentCodes);
            AllocateParentNodes(svo, layer, parentCodes);
            LinkParentAndChildren(svo, layer, childLayer);
        }

        #region Upper layer build

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
                for (var c = 0U; c < 8; c++)
                {
                    var childCode = pCode.ChildCode(c);

                    if (!svo.MortonToIndex[childLayer].ContainsKey(childCode))
                    {
                        someSiblingMissed = true;

                        if (childLayer == 0)
                        {
                            throw new Exception(
                                $"Missing layer-0 child node for parent code {pCode}. "
                                    + $"This should never happen because we pre-computed all layer-0 codes in AllocateL0."
                            );
                        }

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
                if (svo.MortonToIndex[layer].ContainsKey(pCode))
                {
                    throw new Exception(
                        $"Parent code {pCode} at layer {layer} already exists. This should never happen because parent codes are collected from the child layer and guaranteed unique."
                    );
                }

                var parentIdx = svo.Layers[layer].Count;
                svo.Layers[layer].Add(new(pCode));
                svo.MortonToIndex[layer][pCode] = parentIdx;
            }
        }

        void LinkParentAndChildren(SVO svo, uint layer, uint childLayer)
        {
            var sortedChildren = svo.Layers[childLayer];

            for (var childIdx = 0; childIdx < sortedChildren.Count; childIdx++)
            {
                var child = sortedChildren[childIdx];
                var parentCode = child.MortonCode.ParentCode;

                if (!svo.TryGetLink(layer, parentCode, out var parentLink))
                {
                    throw new Exception(
                        $"Parent code {parentCode} at layer {layer} not found for child with Morton code {child.MortonCode} at layer {childLayer}. This should never happen because we guaranteed all parents are allocated in the previous step."
                    );
                }

                child.Parent = parentLink;
                svo.Layers[childLayer][childIdx] = child;

                var parentNode = svo.GetNode(parentLink);
                if (!parentNode.HasChildren)
                {
                    parentNode.FirstChild = new(childLayer, (uint)childIdx);
                    svo.SetNode(parentLink, parentNode);
                }
            }
        }

        #endregion
    }
}
