using System.Collections.Generic;
using NavVolume.Runtime.Builder;
using NavVolume.Runtime.Core;
using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// A* pathfinder for the Sparse Voxel Octree
    /// </summary>
    internal class SVOPathfinder
    {
        readonly MinHeap<SearchNode> _openList = new(512);

        readonly Dictionary<SVOLink, float> _gCost = new(512);

        readonly Dictionary<SVOLink, SVOLink> _cameFrom = new(512);

        readonly HashSet<SVOLink> _closed = new();

        readonly List<SVOLink> _expandBuffer = new(8);

        void ClearState()
        {
            _openList.Clear();
            _gCost.Clear();
            _cameFrom.Clear();
            _closed.Clear();
            _expandBuffer.Clear();
        }

        /// <summary>
        /// Find a path synchronously.
        /// </summary>
        public PathResult FindPath(NavContext navCtx, PathRequest request)
        {
            var startLink = navCtx.PositionToLink(request.Start);
            var goalLink = navCtx.PositionToLink(request.Goal);

            if (
                !startLink.IsValid
                || !goalLink.IsValid
                || IsBlocked(navCtx.Svo, startLink)
                || IsBlocked(navCtx.Svo, goalLink)
            )
            {
                return PathResult.Failure(PathResultStatus.InvalidEndpoint);
            }

            if (startLink == goalLink)
            {
                var trivial = new List<Vector3> { request.Start, request.Goal };
                return PathResult.Success(trivial);
            }

            ClearState();

            var goalCenter = navCtx.LinkToCenter(goalLink);
            var startH = ComputeH(navCtx, startLink, goalCenter, request);
            var startF = 0 + startH;

            _openList.Push(new(startLink, 0), startF);
            _gCost[startLink] = 0f;

            var expanded = 0;

            while (!_openList.IsEmpty)
            {
                if (request.MaxNodesBudget > 0 && expanded >= request.MaxNodesBudget)
                {
                    return PathResult.Failure(PathResultStatus.BudgetExceeded);
                }

                var current = _openList.Pop();

                if (_closed.Contains(current.Link))
                {
                    continue;
                }

                _closed.Add(current.Link);
                expanded++;

                if (
                    current.Link == goalLink
                    || current.Link.IsVoxel(out _) // TODO: check if this is necessary
                        && goalLink.IsVoxel(out _)
                        && current.Link.Offset == goalLink.Offset
                )
                {
                    return ReconstructPath(navCtx, current.Link, request);
                }

                ExpandNeighbors(navCtx, current, goalCenter, request);
            }

            return PathResult.Failure(PathResultStatus.NoPathFound);
        }

        void ExpandNeighbors(
            NavContext navCtx,
            SearchNode current,
            Vector3 goalCenter,
            PathRequest request
        )
        {
            CalculateNeighbors(navCtx.Svo, current.Link, _expandBuffer);

            foreach (var neighbor in _expandBuffer)
            {
                if (
                    !neighbor.IsValid
                    || _closed.Contains(neighbor)
                    || IsBlocked(navCtx.Svo, neighbor)
                )
                {
                    continue;
                }

                if (NeedsChildExpansion(navCtx.Svo, neighbor))
                {
                    ExpandChildren(
                        navCtx,
                        neighbor,
                        current.Link,
                        goalCenter,
                        current.GCost,
                        request
                    );
                    continue;
                }

                var newG = ComputeG(navCtx, current, neighbor);

                if (_gCost.TryGetValue(neighbor, out var existingG) && newG >= existingG)
                {
                    continue;
                }

                _gCost[neighbor] = newG;
                _cameFrom[neighbor] = current.Link;

                var h = ComputeH(navCtx, neighbor, goalCenter, request);
                _openList.Push(new(neighbor, newG), newG + h);
            }
        }

        /// <summary>
        /// Populate <paramref name="buffer"/> with the navigable neighbors of a node.
        /// </summary>
        void CalculateNeighbors(SVO svo, SVOLink link, List<SVOLink> buffer)
        {
            buffer.Clear();

            if (link.IsNode(out _))
            {
                var node = svo.GetNode(link);

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
                var (x, y, z) = SVOLeaf.IndexToSubnodeCoords((int)subnodeIdx);
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
        void TryAddVoxelNeighbor(
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
                buffer.Add(SVOLink.VoxelLink(voxelLink.Offset, (uint)subnode));
                return;
            }

            #endregion

            var srcNode = svo.GetNode(SVOLink.NodeLink(0, voxelLink.Offset));
            var leafNeighbor = srcNode.Neighbors[overflowDir];

            if (!leafNeighbor.IsValid)
            {
                return;
            }

            nx = WrapVoxelCoord(nx);
            ny = WrapVoxelCoord(ny);
            nz = WrapVoxelCoord(nz);

            if (leafNeighbor.IsNode(out var layerIdx))
            {
                if (layerIdx == 0)
                {
                    var entrySubnode = SVOLeaf.SubnodeCoordsToIndex(nx, ny, nz);
                    buffer.Add(SVOLink.VoxelLink(leafNeighbor.Offset, (uint)entrySubnode));
                }
                else
                {
                    buffer.Add(leafNeighbor);
                }
            }
            else
            {
                Debug.LogError("[NavVolume][SVOPathfinder] This code should be unreachable.");
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

            var neighborNode = svo.GetNode(neighbor);
            return neighborNode.HasChildren;
        }

        void ExpandChildren(
            NavContext navCtx,
            SVOLink coarseNeighbor,
            SVOLink incomingLink,
            Vector3 goalCenter,
            float incomingGCost,
            PathRequest request
        )
        {
            var coarse = navCtx.Svo.GetNode(coarseNeighbor);

            if (!coarse.HasChildren)
            {
                return;
            }

            if (coarseNeighbor.IsNode(out var coarseNeighborLIdx)) { }
            else
            {
                Debug.LogError("[NavVolume][SVOPathfinder] This code should be unreachable.");
                return;
            }
            var childLayer = coarseNeighborLIdx - 1;
            var firstOffset = coarse.FirstChild.Offset;

            for (var c = 0u; c < 8; c++)
            {
                var childLink = SVOLink.NodeLink(childLayer, firstOffset + c);

                if (
                    !childLink.IsValid
                    || _closed.Contains(childLink)
                    || IsBlocked(navCtx.Svo, childLink)
                )
                {
                    continue;
                }

                var newG = incomingGCost + SVOHeuristic.UnitCost();

                if (_gCost.TryGetValue(childLink, out var existingG) && newG >= existingG)
                {
                    continue;
                }

                _gCost[childLink] = newG;
                _cameFrom[childLink] = incomingLink;

                var h = ComputeH(navCtx, childLink, goalCenter, request);
                _openList.Push(new(childLink, newG), newG + h);
            }
        }

        #region Cost functions

        float ComputeG(NavContext navCtx, SearchNode from, SVOLink toLink)
        {
            // TODO: check if use size compensation
            // return from.GCost + SVOHeuristic.UnitCost();

            var fromCenter = navCtx.LinkToCenter(from.Link);
            var toCenter = navCtx.LinkToCenter(toLink);
            return from.GCost + SVOHeuristic.EuclideanCost(fromCenter, toCenter);
        }

        float ComputeH(NavContext navCtx, SVOLink link, Vector3 goalCenter, PathRequest request)
        {
            var center = navCtx.LinkToCenter(link);

            // TODO: check if use size compensation
            // distinguish nodesize between layer or leaf
            // return SVOHeuristic.SizeCompensated(center, goalCenter, nodeSize, request.HeuristicWeight);

            return SVOHeuristic.EuclideanHeuristic(center, goalCenter, request.HeuristicWeight);
        }

        #endregion

        static bool IsBlocked(SVO svo, SVOLink link)
        {
            if (link.IsNode(out var layer) && layer > 0)
            {
                return false;
            }

            var leaf = svo.LeafNodes[link.Offset];

            if (leaf.IsEmpty)
            {
                return false;
            }

            if (leaf.IsFull)
            {
                return true;
            }

            link.IsVoxel(out var subnodeIdx);
            return leaf.IsOccupied((int)subnodeIdx);
        }

        PathResult ReconstructPath(NavContext navCtx, SVOLink reachedLink, PathRequest request)
        {
            var rawPath = new List<Vector3>();

            while (reachedLink.IsValid)
            {
                rawPath.Add(navCtx.LinkToCenter(reachedLink));

                if (!_cameFrom.TryGetValue(reachedLink, out var prev))
                {
                    break;
                }

                reachedLink = prev;
            }

            rawPath.Reverse();
            rawPath[0] = request.Start;
            rawPath.Add(request.Goal);

            return PathResult.Success(rawPath);
        }
    }
}
