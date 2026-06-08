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
        /// Builds the SVO and logs its own phase timings. Used by runtime (non-editor) callers.
        /// </summary>
        public NavContext Build() => Build(new());

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
        void AllocateMissingSiblings(SVO svo, int childLayer, List<MortonCode> parentCodes)
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
                for (var c = 0; c < 8; c++)
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

        void AllocateParentNodes(SVO svo, int layer, List<MortonCode> parentCodes)
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

        void LinkParentAndChildren(SVO svo, int layer, int childLayer)
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
                    parentNode.FirstChild = SVOLink.NodeLink(childLayer, childIdx);
                    svo.SetNode(parentLink, parentNode);
                }
            }
        }
    }

    /// <summary>
    /// Bake-specific front end over a shared <see cref="StepProfiler"/>. Adds the build-vs-save split
    /// and snapshots the collected laps into a transient <see cref="BakeReport"/>.
    /// </summary>
    internal sealed class BakeProfiler
    {
        readonly StepProfiler _profiler = new();

        // Running total (ms) captured when the build finished; -1 until then.
        double _buildMs = -1;

        public void Start()
        {
            _profiler.Start();
            _buildMs = -1;
        }

        public void Lap(string label) => _profiler.Lap(label);

        /// <summary>Marks where the build ends and post-build (save) phases begin.</summary>
        public void MarkBuildComplete() => _buildMs = _profiler.TotalMs;

        /// <summary>Snapshots the collected laps into a transient <see cref="BakeReport"/>.</summary>
        public BakeReport ToReport()
        {
            var total = _profiler.TotalMs;
            var buildMs = _buildMs >= 0 ? _buildMs : total;

            // Copy so the report stays independent of the (reusable) profiler's live phase list.
            var phases = new TimedPhase[_profiler.Phases.Count];
            for (var i = 0; i < phases.Length; i++)
            {
                phases[i] = _profiler.Phases[i];
            }

            return new BakeReport(total, buildMs, total - buildMs, phases);
        }
    }
}
