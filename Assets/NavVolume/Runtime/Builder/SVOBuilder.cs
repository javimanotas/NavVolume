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

        public SVOBuilder(BuildSettings settings)
        {
            _settings = settings;
        }

        // TODO: implement an async builder that can be cancelled and reports progress.

        /// <summary>
        /// Builds the SVO and logs its own phase timings. Used by runtime (non-editor) callers.
        /// </summary>
        public NavContext Build() => Build(new BakeProfiler(), report: true);

        /// <summary>
        /// Builds the SVO, recording per-phase timings into <paramref name="profiler"/>. When
        /// <paramref name="report"/> is false the caller owns reporting, so it can append its own
        /// post-build phases (e.g. asset serialization) and emit a single unified log.
        /// </summary>
        internal NavContext Build(BakeProfiler profiler, bool report)
        {
            _profiler = profiler;
            _profiler.Start();

            var svo = new SVO(_settings.NumLayers);

            var occupiedL1 = SVORasterizer.RasterizeL1(_settings);
            _profiler.Lap($"RasterizeL1 ({occupiedL1.Count} cells)");

            AllocateLowerLayers(svo, occupiedL1);

            for (var layer = 1u; layer < svo.Layers.Length; layer++)
            {
                BuildUpperLayer(svo, layer);
            }
            _profiler.Lap("BuildUpperLayers");

            for (var layer = 1u; layer < svo.Layers.Length; layer++)
            {
                LinkParentAndChildren(svo, layer, layer - 1);
            }
            _profiler.Lap("LinkParentAndChildren");

            SVONeighborLinker.FillNeighborLinks(svo, _settings);
            _profiler.Lap("FillNeighborLinks");

            _profiler.MarkBuildComplete();

            var navCtx = new NavContext(svo, _settings);

            if (report)
            {
                _profiler.Report();
            }

            return navCtx;
        }

        #region Lower layers allocation

        void AllocateLowerLayers(SVO svo, List<MortonCode> l1Codes)
        {
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

    /// <summary>
    /// Lightweight per-phase bake timer. Collects named laps and emits a single unified log: a
    /// header with the total split into build vs. save (post-build) time, then every phase below.
    /// </summary>
    internal sealed class BakeProfiler
    {
        readonly List<(string Label, double Ms)> _laps = new();
        readonly System.Diagnostics.Stopwatch _phase = new();

        // Running total (ms) captured when the build finished; -1 until then.
        double _buildMs = -1;

        public void Start()
        {
            _laps.Clear();
            _buildMs = -1;
            _phase.Restart();
        }

        public void Lap(string label)
        {
            _laps.Add((label, _phase.Elapsed.TotalMilliseconds));
            _phase.Restart();
        }

        /// <summary>Marks where the build ends and post-build (save) phases begin.</summary>
        public void MarkBuildComplete() => _buildMs = Sum();

        /// <summary>Snapshots the collected laps into a transient <see cref="BakeReport"/>.</summary>
        public BakeReport ToReport()
        {
            var total = Sum();
            var buildMs = _buildMs >= 0 ? _buildMs : total;

            var phases = new BakePhase[_laps.Count];
            for (var i = 0; i < _laps.Count; i++)
            {
                phases[i] = new BakePhase(_laps[i].Label, _laps[i].Ms);
            }

            return new BakeReport(total, buildMs, total - buildMs, phases);
        }

        public void Report()
        {
            var total = Sum();
            var buildMs = _buildMs >= 0 ? _buildMs : total;
            var saveMs = total - buildMs;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(
                saveMs > 0.05
                    ? $"[NavVolume] Bake: {total:F1} ms  (build {buildMs:F1} ms + save {saveMs:F1} ms)"
                    : $"[NavVolume] Bake: {total:F1} ms"
            );

            foreach (var (label, ms) in _laps)
            {
                sb.AppendLine($"    {label,-26} {ms,8:F1} ms");
            }

            UnityEngine.Debug.Log(sb.ToString());
        }

        double Sum()
        {
            var sum = 0.0;
            foreach (var (_, ms) in _laps)
            {
                sum += ms;
            }
            return sum;
        }
    }
}
