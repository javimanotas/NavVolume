using System.Collections.Generic;
using NavVolume.Runtime.Core;
using UnityEngine;

namespace NavVolume.Runtime.Builder
{
    /// <summary>
    /// Gathers SVO voxel data by sampling geometry of the scene.
    /// </summary>
    /// <remarks>
    /// All check are done with <see cref="Physics.CheckBox"/>.
    /// No mesh data is read directly.
    /// </remarks>
    internal static class SVORasterizer
    {
        /// <summary>
        /// Epsilon value used to avoid detecting overlaps on edges.
        /// </summary>
        const float _OVERLAP_BOX_SHRINK = 1e-3f;

        /// <summary>
        /// Sweeps the navigation volume with layer 1 resolution while checking for geometry.
        /// </summary>
        /// <returns>
        /// A sorted list of morton codes for every cell that contains geometry.
        /// </returns>
        public static List<MortonCode> RasterizeL1(BuildSettings settings)
        {
            var gridResolution = settings.NodeSizeForLayer(1);
            var gridSize = Mathf.RoundToInt(settings.RootSize / gridResolution);
            var halfExtents = Vector3.one * (gridSize * 0.5f - _OVERLAP_BOX_SHRINK);

            // TODO: check if this can be optimized with a sortedset
            var occupied = new HashSet<MortonCode>();

            for (var i = 0u; i < gridSize; i++)
            {
                for (var j = 0u; j < gridSize; j++)
                {
                    for (var k = 0u; k < gridSize; k++)
                    {
                        var center =
                            settings.Origin
                            + new Vector3(
                                (i + 0.5f) * gridResolution,
                                (j + 0.5f) * gridResolution,
                                (k + 0.5f) * gridResolution
                            );

                        if (
                            Physics.CheckBox(
                                center,
                                halfExtents,
                                Quaternion.identity,
                                settings.CollisionMask,
                                QueryTriggerInteraction.Ignore
                            )
                        )
                        {
                            occupied.Add(new(i, j, k));
                        }
                    }
                }
            }

            var result = new List<MortonCode>(occupied);
            result.Sort();
            return result;
        }

        /// <summary>
        /// Rasterizes the geometry for a single leaf node.
        /// </summary>
        public static SVOLeaf RasterizeLeaf(BuildSettings settings, Vector3 leafOriginCornerPos)
        {
            var leaf = SVOLeaf.Empty;
            var halfExt = Vector3.one * (settings.VoxelSize * 0.5f - _OVERLAP_BOX_SHRINK);

            for (var i = 0; i < SVOLeaf.GRID_SIZE; i++)
            {
                for (var j = 0; j < SVOLeaf.GRID_SIZE; j++)
                {
                    for (var k = 0; k < SVOLeaf.GRID_SIZE; k++)
                    {
                        var center =
                            leafOriginCornerPos
                            + new Vector3(
                                (i + 0.5f) * settings.VoxelSize,
                                (j + 0.5f) * settings.VoxelSize,
                                (k + 0.5f) * settings.VoxelSize
                            );

                        if (
                            Physics.CheckBox(
                                center,
                                halfExt,
                                Quaternion.identity,
                                settings.CollisionMask,
                                QueryTriggerInteraction.Ignore
                            )
                        )
                        {
                            leaf.SetOccupied(i, j, k);
                        }
                    }
                }
            }

            return leaf;
        }
    }
}
