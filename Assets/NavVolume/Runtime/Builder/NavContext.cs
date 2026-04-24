using NavVolume.Runtime.Core;
using UnityEngine;

namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// Wrapper for the already built SVO with more information about the build process.
    /// </summary>
    internal readonly struct NavContext
    {
        public readonly SVO Svo;

        public readonly BuildSettings BuildSettings;

        public NavContext(SVO svo, BuildSettings settings)
        {
            Svo = svo;
            BuildSettings = settings;
        }

        /// <summary>
        /// Calculates the world-space center of a voxel within a layer-0 node.
        /// </summary>
        /// <param name="nodeWorldMin">
        /// Node's world-space minimum corner.
        /// </param>
        /// <param name="subnode">
        /// The subnode index.
        /// </param>
        public Vector3 VoxelCenter(Vector3 nodeWorldMin, int subnode)
        {
            var (vx, vy, vz) = SVOLeaf.IndexToSubnodeCoords(subnode);
            return nodeWorldMin
                + new Vector3(
                    (vx + 0.5f) * BuildSettings.VoxelSize,
                    (vy + 0.5f) * BuildSettings.VoxelSize,
                    (vz + 0.5f) * BuildSettings.VoxelSize
                );
        }

        public Bounds NodeBounds(int layer, MortonCode mortonCode)
        {
            var (x, y, z) = mortonCode.Decoded;
            var size = BuildSettings.NodeSizeForLayer(layer);
            var center =
                BuildSettings.Origin
                + new Vector3((x + 0.5f) * size, (y + 0.5f) * size, (z + 0.5f) * size);

            return new(center, Vector3.one * size);
        }

        Bounds NodeBounds(SVOLink link)
        {
            var layer = (int)link.LayerIdx;
            var mortonCode = Svo.Layers[link.LayerIdx][(int)link.NodeIdx].MortonCode;
            return NodeBounds(layer, mortonCode);
        }

        /// <summary>
        /// World-space minimum corner of the node referenced by a link.
        /// </summary>
        public Vector3 NodeMin(SVOLink link)
        {
            var code = Svo.Layers[link.LayerIdx][(int)link.NodeIdx].MortonCode;
            var (x, y, z) = code.Decoded;
            var s = BuildSettings.NodeSizeForLayer((int)link.LayerIdx);
            return BuildSettings.Origin + new Vector3(x * s, y * s, z * s);
        }

        /// <summary>
        /// World-space center of a node or, for layer-0 links with a subnode set, the voxel center.
        /// Used by the pathfinder to place waypoints.
        /// </summary>
        public Vector3 LinkToCenter(SVOLink link)
        {
            // TODO: check if this is wrong.
            if (link.LayerIdx == 0 && link.SubnodeIdx != 0)
            {
                var nodeMin = NodeMin(link);
                return VoxelCenter(nodeMin, (int)link.SubnodeIdx);
            }

            return NodeBounds(link).center;
        }

        /// <summary>
        /// Find the most specific SVO node that contains the given world-space point.
        /// </summary>
        public SVOLink PositionToLink(Vector3 worldPos)
        {
            var local = worldPos - BuildSettings.Origin;

            if (
                local.x < 0f
                || local.y < 0f
                || local.z < 0f
                || local.x > BuildSettings.RootSize
                || local.y > BuildSettings.RootSize
                || local.z > BuildSettings.RootSize
            )
            {
                return SVOLink.Invalid;
            }

            for (var layer = 0; layer < Svo.Layers.Length; layer++)
            {
                var nodeSize = BuildSettings.NodeSizeForLayer(layer);
                var x = (uint)(local.x / nodeSize);
                var y = (uint)(local.y / nodeSize);
                var z = (uint)(local.z / nodeSize);
                var code = new MortonCode(x, y, z);

                if (!Svo.MortonToIndex[layer].TryGetValue(code, out var idx))
                {
                    continue;
                }

                var link = new SVOLink((uint)layer, (uint)idx);

                if (layer == 0)
                {
                    var (nx, ny, nz) = code.Decoded;
                    var nodeMin =
                        BuildSettings.Origin
                        + new Vector3(nx * nodeSize, ny * nodeSize, nz * nodeSize);

                    var localVoxel = worldPos - nodeMin;
                    var vx = Mathf.Clamp((int)(localVoxel.x / BuildSettings.VoxelSize), 0, 3);
                    var vy = Mathf.Clamp((int)(localVoxel.y / BuildSettings.VoxelSize), 0, 3);
                    var vz = Mathf.Clamp((int)(localVoxel.z / BuildSettings.VoxelSize), 0, 3);

                    return link.WithSubnode((uint)SVOLeaf.SubnodeCoordsToIndex(vx, vy, vz));
                }

                return link;
            }

            return SVOLink.Invalid;
        }
    }
}
