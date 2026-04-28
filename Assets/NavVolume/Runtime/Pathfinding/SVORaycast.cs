using NavVolume.Runtime.Builder;
using UnityEngine;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// 3D DDA line-of-sight check against the SVO's finest voxel resolution.
    /// </summary>
    internal static class SVORaycast
    {
        /// <summary>
        /// Returns true if the segment [worldStart, worldEnd] is unobstructed in the baked SVO.
        /// </summary>
        public static bool HasLineOfSight(in NavContext ctx, Vector3 worldStart, Vector3 worldEnd)
        {
            var settings = ctx.BuildSettings;
            var gridDim = Mathf.RoundToInt(settings.RootSize / settings.VoxelSize);

            // Convert world-space endpoints to voxel-space
            var localStart = (worldStart - settings.Origin) / settings.VoxelSize;
            var localEnd = (worldEnd - settings.Origin) / settings.VoxelSize;

            var ray = localEnd - localStart;
            var length = ray.magnitude;

            if (length < 1e-5f)
            {
                return true;
            }

            var dir = ray / length;

            var cell = new Vector3Int(
                Mathf.FloorToInt(localStart.x),
                Mathf.FloorToInt(localStart.y),
                Mathf.FloorToInt(localStart.z)
            );

            var step = new Vector3Int(
                dir.x >= 0f ? 1 : -1,
                dir.y >= 0f ? 1 : -1,
                dir.z >= 0f ? 1 : -1
            );

            var tDelta = new Vector3(TDelta(dir.x), TDelta(dir.y), TDelta(dir.z));

            var tToCross = new Vector3(
                TInitial(localStart.x, step.x, tDelta.x),
                TInitial(localStart.y, step.y, tDelta.y),
                TInitial(localStart.z, step.z, tDelta.z)
            );

            while (true)
            {
                if (
                    cell.x < 0
                    || cell.y < 0
                    || cell.z < 0
                    || cell.x >= gridDim
                    || cell.y >= gridDim
                    || cell.z >= gridDim
                )
                    return false;

                if (ctx.Svo.IsVoxelOccupied(cell.x, cell.y, cell.z))
                {
                    return false;
                }

                if (tToCross.x < tToCross.y && tToCross.x < tToCross.z)
                {
                    if (tToCross.x >= length)
                    {
                        break;
                    }

                    cell.x += step.x;
                    tToCross.x += tDelta.x;
                }
                else if (tToCross.y < tToCross.z)
                {
                    if (tToCross.y >= length)
                    {
                        break;
                    }

                    cell.y += step.y;
                    tToCross.y += tDelta.y;
                }
                else
                {
                    if (tToCross.z >= length)
                    {
                        break;
                    }

                    cell.z += step.z;
                    tToCross.z += tDelta.z;
                }
            }

            return true;
        }

        static float TDelta(float dirComponent) =>
            Mathf.Abs(dirComponent) > 1e-9f ? Mathf.Abs(1f / dirComponent) : float.MaxValue;

        static float TInitial(float localPos, int step, float tDelta)
        {
            if (tDelta == float.MaxValue)
            {
                return float.MaxValue;
            }

            var distToBoundary =
                step > 0
                    ? Mathf.Floor(localPos) + 1f - localPos
                    : localPos - Mathf.Ceil(localPos) + 1;

            return distToBoundary * tDelta;
        }
    }
}
