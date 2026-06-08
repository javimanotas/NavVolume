using System.Collections.Generic;
using System.Threading;
using NavVolume.Runtime.Builder;
using NavVolume.Runtime.Core;
using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// A* pathfinder for the Sparse Voxel Octree.
    /// </summary>
    internal class SVOPathfinder
    {
        /// <summary>
        /// Per-link search state. Folding the former open-set membership, closed set, g-cost map and
        /// came-from map into one dictionary entry lets each neighbor touch the hash table at most
        /// twice (one lookup, one store) instead of four times.
        /// </summary>
        struct NodeRecord
        {
            public float G;
            public SVOLink CameFrom;
            public bool Closed;
        }

        readonly MinHeap<SearchNode> _openList = new(1024);

        readonly Dictionary<SVOLink, NodeRecord> _nodes = new(1024);

        readonly List<SVOLink> _expandBuffer = new(8);

        // Per-query state, set up once at the start of FindPath and constant for its duration.
        NavContext _ctx;
        Vector3 _goalCenter;
        float _hScale;
        bool _euclidean;

        /// <summary>
        /// This prevents from checking the cancellation token on every single expansion, which would be expensive.
        /// </summary>
        const int _CANCEL_CHECK_MASK = 1023;

        void ClearState()
        {
            _openList.Clear();
            _nodes.Clear();
            _expandBuffer.Clear();
        }

        /// <summary>
        /// Find a path synchronously.
        /// </summary>
        public PathResult FindPath(
            NavContext navCtx,
            PathRequest request,
            CancellationToken cancellationToken = default
        )
        {
            if (navCtx.Svo.IsEmpty)
            {
                return FindPathEmptyVolume(navCtx, request);
            }

            var svo = navCtx.Svo;
            var startLink = navCtx.PositionToLink(request.Start);
            var goalLink = navCtx.PositionToLink(request.Goal);

            if (
                !startLink.IsValid
                || !goalLink.IsValid
                || IsBlocked(svo, startLink)
                || IsBlocked(svo, goalLink)
            )
            {
                return PathResult.Failure(
                    PathResultStatus.InvalidEndpoint,
                    new PathStats(0, 0d, 0, 0)
                );
            }

            if (startLink == goalLink)
            {
                var trivial = new List<Vector3> { request.Start, request.Goal };
                return PathResult.Success(trivial, new PathStats(0, 0d, 0, 0));
            }

            ClearState();

            // Hoist everything that is constant for this query out of the inner loop.
            _ctx = navCtx;
            _goalCenter = navCtx.LinkToCenter(goalLink);
            _euclidean = request.CostMode == PathCostMode.EuclideanDistance;

            // The heuristic is "straight-line distance to goal x scale". Folding the weight (and, for
            // node-count cost, the largest node size that converts meters into hops) into a single
            // factor reduces ComputeH to one distance and one multiply.
            if (_euclidean)
            {
                _hScale = request.HeuristicWeight;
            }
            else
            {
                var settings = navCtx.BuildSettings;
                var largestNodeSize = settings.NodeSizeForLayer(settings.NumLayers - 1);
                _hScale = request.HeuristicWeight / largestNodeSize;
            }

            var goalIsVoxel = goalLink.IsVoxel(out _);
            var goalOffset = goalLink.Offset;
            var maxBudget = request.MaxNodesBudget;
            var expanded = 0;

            _nodes[startLink] = new NodeRecord
            {
                G = 0f,
                CameFrom = SVOLink.Invalid,
                Closed = false,
            };
            _openList.Push(new(startLink, 0f), ComputeH(startLink));

            while (!_openList.IsEmpty)
            {
                if ((expanded & _CANCEL_CHECK_MASK) == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (maxBudget > 0 && expanded >= maxBudget)
                {
                    return PathResult.Failure(
                        PathResultStatus.BudgetExceeded,
                        new PathStats(expanded, 0d, 0, 0)
                    );
                }

                var current = _openList.Pop();
                var currentLink = current.Link;

                // The popped entry may be a stale duplicate of a node we already finalized.
                _nodes.TryGetValue(currentLink, out var currentRec);
                if (currentRec.Closed)
                {
                    continue;
                }

                currentRec.Closed = true;
                _nodes[currentLink] = currentRec;
                expanded++;

                if (
                    currentLink == goalLink
                    || (
                        goalIsVoxel
                        && currentLink.IsVoxel(out _)
                        && currentLink.Offset == goalOffset
                    )
                )
                {
                    return ReconstructPath(currentLink, request, expanded);
                }

                ExpandNeighbors(svo, current);
            }

            return PathResult.Failure(
                PathResultStatus.NoPathFound,
                new PathStats(expanded, 0d, 0, 0)
            );
        }

        /// <summary>
        /// Trivial path through a volume with no obstacles.
        /// </summary>
        static PathResult FindPathEmptyVolume(NavContext navCtx, PathRequest request)
        {
            var settings = navCtx.BuildSettings;
            var start = request.Start - settings.Origin;
            var goal = request.Goal - settings.Origin;
            var size = settings.RootSize;

            var startInside =
                start.x >= 0f
                && start.y >= 0f
                && start.z >= 0f
                && start.x <= size
                && start.y <= size
                && start.z <= size;
            var goalInside =
                goal.x >= 0f
                && goal.y >= 0f
                && goal.z >= 0f
                && goal.x <= size
                && goal.y <= size
                && goal.z <= size;

            if (!startInside || !goalInside)
            {
                return PathResult.Failure(
                    PathResultStatus.InvalidEndpoint,
                    new PathStats(0, 0d, 0, 0)
                );
            }

            return PathResult.Success(
                new List<Vector3> { request.Start, request.Goal },
                new PathStats(0, 0d, 0, 0)
            );
        }

        void ExpandNeighbors(SVO svo, SearchNode current)
        {
            var currentLink = current.Link;
            var currentG = current.GCost;

            CalculateNeighbors(svo, currentLink, _expandBuffer);

            // Indexed loop over the reused buffer avoids the enumerator and re-reads Count locally.
            var buffer = _expandBuffer;
            for (var n = 0; n < buffer.Count; n++)
            {
                var neighbor = buffer[n];

                if (!neighbor.IsValid)
                {
                    continue;
                }

                var have = _nodes.TryGetValue(neighbor, out var rec);
                if (have && rec.Closed)
                {
                    continue;
                }

                if (
                    neighbor.IsNode(out var nLayer)
                    && nLayer == 0
                    && TryExpandPartialLeaf(svo, currentLink, currentG, neighbor)
                )
                {
                    continue;
                }

                if (IsBlocked(svo, neighbor))
                {
                    continue;
                }

                if (NeedsChildExpansion(svo, neighbor))
                {
                    ExpandChildren(svo, neighbor, currentLink, currentG);
                    continue;
                }

                var newG = ComputeG(currentLink, currentG, neighbor);

                if (have && newG >= rec.G)
                {
                    continue;
                }

                rec.G = newG;
                rec.CameFrom = currentLink;
                _nodes[neighbor] = rec;

                _openList.Push(new(neighbor, newG), newG + ComputeH(neighbor));
            }
        }

        /// <summary>
        /// Populate <paramref name="buffer"/> with the navigable neighbors of a node.
        /// </summary>
        static void CalculateNeighbors(SVO svo, SVOLink link, List<SVOLink> buffer)
        {
            buffer.Clear();

            if (link.IsNode(out _))
            {
                ref readonly var node = ref svo.GetNode(link);

                for (var d = 0; d < 6; d++)
                {
                    var neighbor = node.Neighbors[(NeighborDirection)d];

                    if (neighbor.IsValid)
                    {
                        buffer.Add(neighbor);
                    }
                }
            }
            else if (link.IsVoxel(out var subnodeIdx))
            {
                var (x, y, z) = SVOLeaf.IndexToSubnodeCoords(subnodeIdx);
                TryAddVoxelNeighbor(svo, link, x + 1, y, z, NeighborDirection.PosX, buffer);
                TryAddVoxelNeighbor(svo, link, x - 1, y, z, NeighborDirection.NegX, buffer);
                TryAddVoxelNeighbor(svo, link, x, y + 1, z, NeighborDirection.PosY, buffer);
                TryAddVoxelNeighbor(svo, link, x, y - 1, z, NeighborDirection.NegY, buffer);
                TryAddVoxelNeighbor(svo, link, x, y, z + 1, NeighborDirection.PosZ, buffer);
                TryAddVoxelNeighbor(svo, link, x, y, z - 1, NeighborDirection.NegZ, buffer);
            }
        }

        /// <summary>
        /// Try to add a voxel neighbor to the buffer.
        /// </summary>
        static void TryAddVoxelNeighbor(
            SVO svo,
            SVOLink voxelLink,
            int nx,
            int ny,
            int nz,
            NeighborDirection overflowDir,
            List<SVOLink> buffer
        )
        {
            #region Within the same leaf

            if (
                nx >= 0
                && nx < SVOLeaf.GRID_SIZE
                && ny >= 0
                && ny < SVOLeaf.GRID_SIZE
                && nz >= 0
                && nz < SVOLeaf.GRID_SIZE
            )
            {
                var subnode = SVOLeaf.SubnodeCoordsToIndex(nx, ny, nz);
                buffer.Add(SVOLink.VoxelLink(voxelLink.Offset, subnode));
                return;
            }

            #endregion

            ref readonly var srcNode = ref svo.GetNode(SVOLink.NodeLink(0, voxelLink.Offset));
            var leafNeighbor = srcNode.Neighbors[overflowDir];

            if (!leafNeighbor.IsValid)
            {
                return;
            }

            nx = WrapVoxelCoord(nx);
            ny = WrapVoxelCoord(ny);
            nz = WrapVoxelCoord(nz);

            leafNeighbor.IsNode(out var layerIdx);

            if (layerIdx == 0)
            {
                var entrySubnode = SVOLeaf.SubnodeCoordsToIndex(nx, ny, nz);
                buffer.Add(SVOLink.VoxelLink(leafNeighbor.Offset, entrySubnode));
            }
            else
            {
                buffer.Add(leafNeighbor);
            }
        }

        static int WrapVoxelCoord(int v) => (v + SVOLeaf.GRID_SIZE) % SVOLeaf.GRID_SIZE;

        /// <summary>
        /// True when the neighbor is a node that contains geometry and must be expanded to find a safe path.
        /// </summary>
        /// <remarks>
        /// This prevents the search from treating a dense block as a single cheap hop.
        /// </remarks>
        static bool NeedsChildExpansion(SVO svo, SVOLink neighbor)
        {
            if (!neighbor.IsNode(out _))
            {
                return false;
            }

            return svo.GetNode(neighbor).HasChildren;
        }

        void ExpandChildren(
            SVO svo,
            SVOLink coarseNeighbor,
            SVOLink incomingLink,
            float incomingGCost
        )
        {
            ref readonly var coarse = ref svo.GetNode(coarseNeighbor);

            if (!coarse.HasChildren)
            {
                return;
            }

            coarseNeighbor.IsNode(out var coarseNeighborLIdx);
            var childLayer = coarseNeighborLIdx - 1;
            var firstOffset = coarse.FirstChild.Offset;

            for (var c = 0; c < 8; c++)
            {
                var childLink = SVOLink.NodeLink(childLayer, firstOffset + c);

                if (!childLink.IsValid)
                {
                    continue;
                }

                var have = _nodes.TryGetValue(childLink, out var rec);
                if (have && rec.Closed)
                {
                    continue;
                }

                if (
                    childLink.IsNode(out var cLayerIdx)
                    && cLayerIdx == 0
                    && TryExpandPartialLeaf(svo, incomingLink, incomingGCost, childLink)
                )
                {
                    continue;
                }

                if (IsBlocked(svo, childLink))
                {
                    continue;
                }

                if (NeedsChildExpansion(svo, childLink))
                {
                    ExpandChildren(svo, childLink, incomingLink, incomingGCost);
                    continue;
                }

                var newG = ComputeG(incomingLink, incomingGCost, childLink);

                if (have && newG >= rec.G)
                {
                    continue;
                }

                rec.G = newG;
                rec.CameFrom = incomingLink;
                _nodes[childLink] = rec;

                _openList.Push(new(childLink, newG), newG + ComputeH(childLink));
            }
        }

        bool TryExpandPartialLeaf(SVO svo, SVOLink fromLink, float fromGCost, SVOLink leafLink)
        {
            var leaf = svo.LeafNodes[leafLink.Offset];
            if (leaf.IsEmpty || leaf.IsFull)
            {
                return false;
            }

            var leafOffset = leafLink.Offset;
            for (var i = 0; i < SVOLeaf.NUM_VOXELS; i++)
            {
                if (leaf.IsOccupied(i))
                {
                    continue;
                }

                var voxelLink = SVOLink.VoxelLink(leafOffset, i);

                var have = _nodes.TryGetValue(voxelLink, out var rec);
                if (have && rec.Closed)
                {
                    continue;
                }

                var newG = ComputeG(fromLink, fromGCost, voxelLink);

                if (have && newG >= rec.G)
                {
                    continue;
                }

                rec.G = newG;
                rec.CameFrom = fromLink;
                _nodes[voxelLink] = rec;

                _openList.Push(new(voxelLink, newG), newG + ComputeH(voxelLink));
            }

            return true;
        }

        #region Cost functions

        float ComputeG(SVOLink fromLink, float fromGCost, SVOLink toLink)
        {
            if (!_euclidean)
            {
                return fromGCost + SVOHeuristic.UnitCost();
            }

            return fromGCost
                + SVOHeuristic.EuclideanCost(
                    _ctx.LinkToCenter(fromLink),
                    _ctx.LinkToCenter(toLink)
                );
        }

        float ComputeH(SVOLink link) =>
            SVOHeuristic.ScaledDistance(_ctx.LinkToCenter(link), _goalCenter, _hScale);

        #endregion

        static bool IsBlocked(SVO svo, SVOLink link)
        {
            if (link.IsNode(out var layer))
            {
                if (layer > 0)
                {
                    return false;
                }

                // Layer-0 NODE link: only fully empty leaves are safe to use
                // as a single waypoint. The node center of a partial leaf sits
                // on the inner-octant boundary of the 4x4x4 voxel grid and may
                // land inside an occupied subnode, so partial (and full)
                // leaves must be entered via voxel links instead. The
                // cross-leaf voxel transitions in TryAddVoxelNeighbor already
                // produce the correct voxel entry links, so the search can
                // still reach those cells.
                return !svo.LeafNodes[link.Offset].IsEmpty;
            }

            var leaf = svo.LeafNodes[link.Offset];
            link.IsVoxel(out var subnodeIdx);
            return leaf.IsOccupied(subnodeIdx);
        }

        PathResult ReconstructPath(SVOLink reachedLink, PathRequest request, int nodesExpanded)
        {
            var rawPath = new List<Vector3>();

            while (reachedLink.IsValid)
            {
                rawPath.Add(_ctx.LinkToCenter(reachedLink));

                if (!_nodes.TryGetValue(reachedLink, out var rec))
                {
                    break;
                }

                reachedLink = rec.CameFrom;
            }

            rawPath.Reverse();
            rawPath[0] = request.Start;
            rawPath.Add(request.Goal);

            return PathResult.Success(rawPath, new PathStats(nodesExpanded, 0d, 0, 0));
        }
    }
}
