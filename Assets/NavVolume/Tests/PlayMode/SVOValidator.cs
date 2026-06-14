using System;
using System.Collections.Generic;
using NavVolume.Runtime.Builder;
using NavVolume.Runtime.Core;
using UnityEngine;

namespace NavVolume.Tests.PlayMode
{
    /// <summary>
    /// Static inspection utility that validates a fully built <see cref="NavContext"/>.
    /// </summary>
    internal static class SVOValidator
    {
        #region Helpers

        static readonly NeighborDirection[] AllDirections = (NeighborDirection[])
            Enum.GetValues(typeof(NeighborDirection));

        static readonly NeighborDirection[] OppositeDirection =
        {
            NeighborDirection.NegX,
            NeighborDirection.PosX,
            NeighborDirection.NegY,
            NeighborDirection.PosY,
            NeighborDirection.NegZ,
            NeighborDirection.PosZ,
        };

        static bool BoundsAreAdjacent(Bounds a, Bounds b)
        {
            const float EPSILON = 1e-3f;

            var gap = Vector3.Max(
                a.min - b.max - Vector3.one * EPSILON,
                b.min - a.max - Vector3.one * EPSILON
            );

            return Mathf.Max(gap.x, Mathf.Max(gap.y, gap.z)) <= 0f;
        }

        #endregion

        /// <summary>
        /// Runs all validation passes over <paramref name="ctx"/> and returns if there are no errors.
        /// <para>
        /// Each function checks a list of different aspects. Those aspects are documented in the summary and labeled on the code.
        /// </para>
        /// </summary>
        public static bool IsValid(NavContext ctx, out List<string> errors)
        {
            errors = new List<string>();
            var svo = ctx.Svo;
            var settings = ctx.BuildSettings;

            CheckTopLevelCounts(svo, settings, errors);
            CheckLayerSortednessAndUniqueness(svo, errors);
            CheckParentLinks(svo, errors);
            CheckChildLinks(svo, errors);
            CheckSiblingInvariant(svo, errors);
            CheckNeighborLinks(ctx, errors);

            return errors.Count == 0;
        }

        /// <summary>
        /// Checks that:
        /// <list type="bullet">
        ///     <item> Layer array length matches the intended settings depth. </item>
        ///     <item> LeafNodes must match the number of layer-0 nodes. </item>
        /// </list>
        /// </summary>
        static void CheckTopLevelCounts(SVO svo, BuildSettings settings, List<string> report)
        {
            // Layer array length matches the intended settings depth.
            if (svo.Layers.Length != settings.NumLayers)
            {
                report.Add(
                    $"Layer count mismatch: SVO has {svo.Layers.Length} layer(s) "
                        + $"but BuildSettings.NumLayers = {settings.NumLayers}."
                );
            }

            // LeafNodes must match the number of layer-0 nodes.
            if (svo.Layers.Length > 0 && svo.LeafNodes.Length != svo.Layers[0].Length)
            {
                report.Add(
                    $"LeafNodes.Length = {svo.LeafNodes.Length} "
                        + $"but Layer 0 has {svo.Layers[0].Length} node(s). They must match 1-to-1."
                );
            }
        }

        /// <summary>
        /// For every layer, checks that:
        /// <list type="bullet">
        ///     <item> MortonCodes are in strictly ascending order (required for binary-search lookups). </item>
        ///     <item> No two nodes in the same layer share a MortonCode. </item>
        ///     <item> SVO.TryFindNodeIndex round-trips: every node can be looked up by its code. </item>
        /// </list>
        /// </summary>
        static void CheckLayerSortednessAndUniqueness(SVO svo, List<string> report)
        {
            for (var layerIdx = 0; layerIdx < svo.Layers.Length; layerIdx++)
            {
                var layer = svo.Layers[layerIdx];
                var prefix = $"Layer {layerIdx}";

                for (var nodeIdx = 0; nodeIdx < layer.Length; nodeIdx++)
                {
                    var code = layer[nodeIdx].MortonCode;

                    if (nodeIdx > 0)
                    {
                        var prevCode = layer[nodeIdx - 1].MortonCode;
                        var cmp = ((uint)prevCode).CompareTo((uint)code);
                        if (cmp == 0)
                        {
                            report.Add(
                                $"{prefix}, node {nodeIdx}: Duplicate MortonCode 0x{(uint)code:X8}."
                            );
                            continue;
                        }
                        if (cmp > 0)
                        {
                            report.Add(
                                $"{prefix}, node {nodeIdx}: MortonCode 0x{(uint)code:X8} "
                                    + $"is out of order (previous was 0x{(uint)prevCode:X8})."
                            );
                            continue;
                        }
                    }

                    // Round-trip: binary search must find this node at this index.
                    if (!svo.TryFindNodeIndex(layerIdx, code, out var foundIdx))
                    {
                        report.Add(
                            $"{prefix}, node {nodeIdx}: MortonCode 0x{(uint)code:X8} not found by binary search."
                        );
                    }
                    else if (foundIdx != nodeIdx)
                    {
                        report.Add(
                            $"{prefix}, node {nodeIdx}: Binary search returned index {foundIdx} "
                                + $"for MortonCode 0x{(uint)code:X8}, expected {nodeIdx}."
                        );
                    }
                }
            }
        }

        /// <summary>
        /// For every node in every layer, checks that:
        /// <list type="bullet">
        ///     <item> Root nodes have invalid parent links. </item>
        ///     <item> Non-root nodes have valid parent links pointing to the next coarser layer. </item>
        ///     <item> Parent's 8 child range contains this node. </item>
        /// </list>
        /// </summary>
        static void CheckParentLinks(SVO svo, List<string> report)
        {
            var numLayers = svo.Layers.Length;
            var coarsestLayer = numLayers - 1;

            for (var layerIdx = 0; layerIdx < numLayers; layerIdx++)
            {
                var layer = svo.Layers[layerIdx];
                var isRoot = layerIdx == coarsestLayer;
                var prefix = $"Layer {layerIdx}";

                for (var nodeIdx = 0; nodeIdx < layer.Length; nodeIdx++)
                {
                    var node = layer[nodeIdx];
                    var nodePfx = $"{prefix}, node {nodeIdx} (Morton 0x{(uint)node.MortonCode:X8})";

                    if (isRoot)
                    {
                        // Root nodes have invalid parent links.
                        if (node.Parent.IsValid)
                        {
                            report.Add(
                                $"{nodePfx}: Root node has a non-invalid Parent link (0x{(uint)node.Parent:X8})."
                            );
                        }
                    }
                    else
                    {
                        // Non-root nodes have valid parent links pointing to the next coarser layer.
                        #region Check

                        if (!node.Parent.IsValid)
                        {
                            report.Add($"{nodePfx}: Non-root node has an invalid Parent link.");
                            return;
                        }

                        if (!node.Parent.IsNode(out var parentLayerIdx))
                        {
                            report.Add(
                                $"{nodePfx}: Parent link is a voxel link, expected a node link."
                            );
                            return;
                        }

                        var expectedParentLayer = layerIdx + 1;

                        if (parentLayerIdx != expectedParentLayer)
                        {
                            report.Add(
                                $"{nodePfx}: Parent link targets layer {parentLayerIdx}, "
                                    + $"expected layer {expectedParentLayer}."
                            );
                        }

                        #endregion

                        var parentOffset = node.Parent.Offset;
                        var parentLayer = svo.Layers[parentLayerIdx];
                        var parent = parentLayer[parentOffset];
                        var parentFirstOffset = parent.FirstChild.IsValid
                            ? parent.FirstChild.Offset
                            : -1;

                        // Parent's 8 child range contains this node.
                        if (
                            parentFirstOffset < 0
                            || nodeIdx < parentFirstOffset
                            || nodeIdx >= parentFirstOffset + 8
                        )
                        {
                            report.Add(
                                $"{nodePfx}: Node index {nodeIdx} is outside parent's child range "
                                    + $"[{parentFirstOffset}, {parentFirstOffset + 7}]."
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// For every non leaf node with children in every layer, checks that:
        /// <list type="bullet">
        ///     <item> Child links point to the next coarser layer. </item>
        ///     <item> All 8 child offsets must be in range. </item>
        ///     <item> Each child's Parent link points back to the correct parent node. </item>
        ///     <item> Each child's MortonCode matches the expected ChildCode of the parent. </item>
        /// </list>
        /// </summary>
        static void CheckChildLinks(SVO svo, List<string> report)
        {
            var numLayers = svo.Layers.Length;

            for (var layerIdx = 0; layerIdx < numLayers; layerIdx++)
            {
                var layer = svo.Layers[layerIdx];
                var isLeaf = layerIdx == 0;
                var prefix = $"Layer {layerIdx}";

                for (var nodeIdx = 0; nodeIdx < layer.Length; nodeIdx++)
                {
                    var node = layer[nodeIdx];
                    var selfLink = SVOLink.NodeLink(layerIdx, nodeIdx);
                    var nodePfx = $"{prefix}, node {nodeIdx} (Morton 0x{(uint)node.MortonCode:X8})";

                    if (isLeaf || !node.FirstChild.IsValid)
                    {
                        continue;
                    }

                    node.FirstChild.IsNode(out var childLayerIdx);

                    var expectedChildLayer = layerIdx - 1;

                    // Child links point to the next coarser layer.
                    if (childLayerIdx != expectedChildLayer)
                    {
                        report.Add(
                            $"{nodePfx}: FirstChild targets layer {childLayerIdx}, "
                                + $"expected layer {expectedChildLayer}."
                        );
                    }

                    var childLayer = svo.Layers[childLayerIdx];
                    var firstChildOff = node.FirstChild.Offset;
                    var lastChildOff = firstChildOff + 7;

                    // All 8 child offsets must be in range.
                    if (lastChildOff >= childLayer.Length)
                    {
                        report.Add(
                            $"{nodePfx}: Child range [{firstChildOff}, {lastChildOff}] "
                                + $"exceeds layer {childLayerIdx} count ({childLayer.Length}). "
                                + "Node does not have 8 valid children."
                        );
                        continue;
                    }

                    for (var slot = 0; slot < 8; slot++)
                    {
                        var childIdx = firstChildOff + slot;
                        var child = childLayer[childIdx];
                        var childPfx = $"Layer {childLayerIdx}, node {childIdx} (slot {slot})";

                        // Each child's Parent link points back to the correct parent node.
                        if (child.Parent != selfLink)
                        {
                            report.Add(
                                $"{childPfx}: Parent back-link (0x{(uint)child.Parent:X8}) "
                                    + $"does not point to expected parent (0x{(uint)selfLink:X8})."
                            );
                        }

                        // Each child's MortonCode matches the expected ChildCode of the parent.
                        var expectedCode = node.MortonCode.ChildCode(slot);

                        if (child.MortonCode != expectedCode)
                        {
                            report.Add(
                                $"{childPfx}: MortonCode 0x{(uint)child.MortonCode:X8} "
                                    + $"does not match expected ChildCode(slot {slot}) = 0x{(uint)expectedCode:X8} "
                                    + $"of parent Morton 0x{(uint)node.MortonCode:X8}."
                            );
                        }
                    }
                }
            }
        }

        /// <summary>
        /// For every sibling group (8 children under the same parent):
        /// <list type="bullet">
        ///     <item> At least one sibling must carry geometry. </item>
        /// </list>
        /// </summary>
        static void CheckSiblingInvariant(SVO svo, List<string> report)
        {
            var checkedGroups = new HashSet<uint>();

            for (var layerIdx = 0; layerIdx < svo.Layers.Length; layerIdx++)
            {
                var layer = svo.Layers[layerIdx];

                for (var nodeIdx = 0; nodeIdx < layer.Length; nodeIdx++)
                {
                    var node = layer[nodeIdx];

                    if (!node.Parent.IsValid || !checkedGroups.Add((uint)node.Parent))
                    {
                        continue;
                    }

                    var parentLayerIdx = layerIdx + 1;
                    var parentOffset = (int)node.Parent.Offset;
                    var parent = svo.Layers[parentLayerIdx][parentOffset];

                    var firstSiblingOffset = parent.FirstChild.Offset;
                    var siblingLayer = svo.Layers[layerIdx];
                    var isSiblingLeaf = layerIdx == 0;

                    var anyWithGeometry = false;

                    for (var slot = 0; slot < 8; slot++)
                    {
                        var sibIdx = firstSiblingOffset + slot;
                        var sibling = siblingLayer[sibIdx];

                        if (isSiblingLeaf ? !svo.LeafNodes[sibIdx].IsEmpty : sibling.HasChildren)
                        {
                            anyWithGeometry = true;
                            break;
                        }
                    }

                    // At least one sibling must carry geometry.
                    if (!anyWithGeometry)
                    {
                        report.Add(
                            $"Sibling group under parent (Layer {parentLayerIdx}, "
                                + $"node {parentOffset}, Morton 0x{(uint)parent.MortonCode:X8}): "
                                + "None of the 8 siblings contains geometry. "
                                + "The parent should not exist if no child is solid."
                        );
                    }
                }
            }
        }

        /// <summary>
        /// For every node, checks each of the six face-neighbor links:
        /// <list type="bullet">
        ///     <item> There cant be unliked direct neighbors. </item>
        ///     <item> Must be a node link, not a voxel link. </item>
        ///     <item> Neighbor must not link to itself. </item>
        ///     <item> If points to same layer, the neighbor's back-link must point exactly back to this node. </item>
        ///     <item> If points to different layer, the bounding box should be adjacent or overlapping. </item>
        /// </list>
        /// </summary>
        static void CheckNeighborLinks(NavContext ctx, List<string> report)
        {
            var svo = ctx.Svo;
            var settings = ctx.BuildSettings;

            var numLayers = svo.Layers.Length;

            for (var layerIdx = 0; layerIdx < numLayers; layerIdx++)
            {
                var layer = svo.Layers[layerIdx];
                var nodeSize = settings.NodeSizeForLayer(layerIdx);
                var gridRes = Mathf.RoundToInt(settings.RootSize / nodeSize);
                var prefix = $"Layer {layerIdx}";

                for (var nodeIdx = 0; nodeIdx < layer.Length; nodeIdx++)
                {
                    var node = layer[nodeIdx];
                    var selfLink = SVOLink.NodeLink(layerIdx, nodeIdx);
                    var nodePfx = $"{prefix}, node {nodeIdx} (Morton 0x{(uint)node.MortonCode:X8})";

                    foreach (var dir in AllDirections)
                    {
                        var neighborLink = node.Neighbors[dir];
                        var opp = OppositeDirection[(int)dir];

                        var hasAdjacentCode = node.MortonCode.TryGetNeighborCode(
                            dir,
                            gridRes,
                            out var adjCode
                        );

                        if (!neighborLink.IsValid)
                        {
                            if (!hasAdjacentCode)
                            {
                                continue;
                            }

                            // There cant be unliked direct neighbors.
                            if (svo.TryFindNodeIndex(layerIdx, adjCode, out _))
                            {
                                report.Add(
                                    $"{nodePfx}, dir {dir}: Neighbor link is invalid but an adjacent "
                                        + $"same-layer node (Morton 0x{(uint)adjCode:X8}) exists. "
                                        + "This may indicate a missing link (cross-layer linking is legal, "
                                        + "but investigate if pathfinding produces unexpected gaps)."
                                );
                            }

                            continue;
                        }

                        // Must be a node link, not a voxel link.
                        if (!neighborLink.IsNode(out var neighborLayerIdx))
                        {
                            report.Add(
                                $"{nodePfx}, dir {dir}: Neighbor link is a voxel link, expected a node link."
                            );
                            continue;
                        }

                        var neighborOffset = neighborLink.Offset;
                        var neighborLayer = svo.Layers[neighborLayerIdx];

                        // Neighbor must not link to itself.
                        if (neighborLink == selfLink)
                        {
                            report.Add(
                                $"{nodePfx}, dir {dir}: Neighbor link points to the node itself (self-loop)."
                            );
                            continue;
                        }

                        var neighbor = neighborLayer[neighborOffset];
                        var backLink = neighbor.Neighbors[opp];
                        var isSameLayer = neighborLayerIdx == layerIdx;

                        if (isSameLayer)
                        {
                            // The neighbor's back-link must point exactly back to this node.
                            if (backLink != selfLink)
                            {
                                report.Add(
                                    $"{nodePfx}, dir {dir}: Same-layer neighbor "
                                        + $"(node {neighborOffset}) back-link in direction {opp} "
                                        + $"is 0x{(uint)backLink:X8}, expected 0x{(uint)selfLink:X8}."
                                );
                            }
                        }
                        else
                        {
                            var thisBounds = ctx.NodeBounds(layerIdx, node.MortonCode);
                            var neighborBounds = ctx.NodeBounds(
                                (int)neighborLayerIdx,
                                neighbor.MortonCode
                            );

                            // The bounding box should be adjacent or overlapping.
                            if (!BoundsAreAdjacent(thisBounds, neighborBounds))
                            {
                                report.Add(
                                    $"{nodePfx}, dir {dir}: Cross-layer neighbor "
                                        + $"(Layer {neighborLayerIdx}, node {neighborOffset}, "
                                        + $"Morton 0x{(uint)neighbor.MortonCode:X8}) bounding boxes "
                                        + "are not adjacent or overlapping. This is a dangling reference."
                                );
                            }
                        }
                    }
                }
            }
        }
    }
}
