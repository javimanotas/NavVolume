using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NavVolume.Runtime;
using NavVolume.Runtime.Builder;
using NavVolume.Runtime.Core;
using NavVolume.Runtime.Pathfinding;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NavVolume
{
    /// <summary>
    /// MonoBehaviour that owns a Navigation Volume and exposes 3D flight pathfinding to agents.
    /// </summary>
    [AddComponentMenu("NavVolume/NavVolume Space")]
    [DisallowMultipleComponent]
    [HelpURL("https://github.com/javimanotas/NavVolume")]
    public class NavVolumeSpace : MonoBehaviour
    {
        static readonly List<NavVolumeSpace> s_Instances = new();

        #region Unity inspector fields

        [SerializeField]
        int _priority;

        [SerializeField]
        AgentType _agentType;

        [SerializeField]
        [Tooltip("Side length of the cubic world volume (meters).")]
        [Min(0)]
        float _rootSize = 100f;

        /// <summary>
        /// Number of layers in the octree (the higher the value means the finer the detail).
        /// <para>
        /// Minimum 2: the coarse-to-fine sweep in <see cref="SVORasterizer"/> seeds one layer below the root.
        /// A tree needs at least a leaf layer plus a root layer.
        /// </para>
        /// <para>
        /// Maximum 10: the <see cref="MortonCode"/> encoded into 32 bit integer so 11 bits * 3 coordinates will exceed their capacity.
        /// WARNING: in compact scenes the pointers might not be able to reference all the nodes of the layer 10.
        /// </para>
        /// </summary>
        [SerializeField]
        [Tooltip("The detail of the navigable space.")]
        [Range(2, 10)]
        int _numLayers = 6;

        [SerializeField]
        [Tooltip("Physics layers that count as solid obstacles.")]
        LayerMask _collisionMask = ~0;

        [SerializeField]
        BuildMode _buildMode;

        [SerializeField]
        NavVolumeBakedData _bakedData;

        #endregion

        internal NavContext NavCtx;

        // Reused across the synchronous FindPath calls.
        readonly SVOPathfinder _pathfinder = new();

        // Pool for async searches.
        readonly ConcurrentBag<SVOPathfinder> _pathfinderPool = new();

        internal NavVolumeBakedData BakedData => _bakedData;

        internal BuildMode BuildMode => _buildMode;

        /// <summary>
        /// Timing breakdown of the most recent build.
        /// </summary>
        internal BakeReport LastBuildReport { get; set; }

        internal BuildSettings CurrentSettings =>
            new(transform.position, _rootSize, _numLayers, _collisionMask, 0);

        /// <summary>
        /// World-space axis-aligned bounding box of the cubic volume managed by this space.
        /// </summary>
        public Bounds VolumeBounds => new(transform.position, Vector3.one * _rootSize);

        public bool IsReady => NavCtx.Svo != null;

        /// <summary>
        /// Raised on the main thread right after the volume's data is (re)built.
        /// </summary>
        public event Action Rebuilt;

        void Awake()
        {
            s_Instances.Add(this);

            switch (_buildMode)
            {
                case BuildMode.Baked:
                    LoadBakedData();
                    break;

                case BuildMode.BuildOnAwake:
                    Build();
                    break;

                case BuildMode.Manual:
                    break;
            }
        }

        void OnDestroy()
        {
            s_Instances.Remove(this);
        }

        /// <summary>
        /// Returns the active NavVolume instance that better suits the target agent.
        /// </summary>
        public static bool FindBetterInstanceFor(
            NavVolumeAgent agent,
            out NavVolumeSpace navVolumeSpace
        )
        {
            navVolumeSpace = s_Instances
                .Where(n =>
                    n._agentType == agent.AgentType
                    && n.VolumeBounds.Contains(agent.transform.position)
                )
                .OrderBy(n => n._priority)
                .FirstOrDefault();

            return navVolumeSpace != null;
        }

        void LoadBakedData()
        {
            if (_bakedData == null)
            {
                Debug.LogError(
                    "[NavVolume][NavVolumeSpace] Build mode is \"Baked\" but no baked data asset is assigned. "
                        + "Assign it and bake the volume."
                );
            }
            else if (_bakedData.IsEmpty)
            {
                Debug.LogError(
                    "[NavVolume][NavVolumeSpace] Build mode is \"Baked\" but the baked data asset is empty. "
                        + "Bake the volume first."
                );
            }
            else
            {
                if (_bakedData.HasSceneBeenModifiedSinceBake(CurrentSettings))
                {
                    Debug.LogWarning(
                        "[NavVolume][NavVolumeSpace] The scene has been modified since the last bake. "
                            + "Navigation may be inaccurate. Bake the volume again."
                    );
                }

                NavCtx = _bakedData.RetrieveBakedData();
            }
        }

        /// <summary>
        /// Builds the NavVolume synchronously.
        /// </summary>
        public void Build()
        {
            var builder = new SVOBuilder(CurrentSettings);
            var profiler = new BakeProfiler();

            NavCtx = builder.Build(profiler);
            LastBuildReport = profiler.ToReport();

            Rebuilt?.Invoke();
        }

        /// <summary>
        /// Find a path synchronously.
        /// </summary>
        internal PathResult FindPath(PathRequest request)
        {
            if (!IsReady)
            {
                return PathResult.Failure(PathResultStatus.NoTree);
            }

            return RunQuery(_pathfinder, NavCtx, request, CancellationToken.None);
        }

        /// <summary>
        /// Find a path on a background thread.
        /// </summary>
        internal Task<PathResult> FindPathAsync(
            PathRequest request,
            CancellationToken cancellationToken
        )
        {
            if (!IsReady)
            {
                return Task.FromResult(PathResult.Failure(PathResultStatus.NoTree));
            }

            var ctx = NavCtx;

            return Task.Run(
                () =>
                {
                    var pathfinder = _pathfinderPool.TryTake(out var pooled)
                        ? pooled
                        : new SVOPathfinder();

                    try
                    {
                        return RunQuery(pathfinder, ctx, request, cancellationToken);
                    }
                    finally
                    {
                        _pathfinderPool.Add(pathfinder);
                    }
                },
                cancellationToken
            );
        }

        /// <summary>
        /// Runs the A* search with <paramref name="pathfinder"/> and post-processes the raw path into the smoothed waypoints.
        /// </summary>
        static PathResult RunQuery(
            SVOPathfinder pathfinder,
            NavContext ctx,
            PathRequest request,
            CancellationToken cancellationToken
        )
        {
            var profiler = new StepProfiler();
            profiler.Start();

            var raw = pathfinder.FindPath(ctx, request, cancellationToken);
            profiler.Lap("A* search");

            if (!raw.Succeeded)
            {
                var failStats = new PathStats(
                    raw.Stats.NodesExpanded,
                    profiler.TotalMs,
                    0,
                    0,
                    profiler.Phases
                );
                return PathResult.Failure(raw.Status, failStats);
            }

            var rawWaypoints = raw.Waypoints;

            var shortcut = PathSmoother.GreedyShortcut(rawWaypoints, in ctx);
            profiler.Lap("Shortcut (LOS)");

            var smoothed = PathSmoother.CatmullRomSpline(shortcut);
            profiler.Lap("Spline");

            var stats = new PathStats(
                raw.Stats.NodesExpanded,
                profiler.TotalMs,
                Mathf.Max(0, rawWaypoints.Count - shortcut.Count),
                rawWaypoints.Count,
                profiler.Phases
            );

            return PathResult.Success(smoothed, rawWaypoints, stats);
        }

        /// <summary>
        /// Returns true if the point lies inside the cubic root bounds of this volume, regardless of whether the underlying voxel is free or occupied.
        /// </summary>
        public bool IsInsideVolume(Vector3 worldPos) => VolumeBounds.Contains(worldPos);

        /// <summary>
        /// Returns true when the straight segment between two points does not cross any occupied voxel.
        /// </summary>
        internal bool IsSegmentNavigable(Vector3 from, Vector3 to)
        {
            if (!IsReady)
            {
                return true;
            }

            if (!IsInsideVolume(from) || !IsInsideVolume(to))
            {
                return true;
            }

            return SVORaycast.HasLineOfSight(NavCtx, from, to);
        }

        /// <summary>
        /// Returns true if the point lies inside the volume and the voxel containing it is free.
        /// </summary>
        public bool IsNavigable(Vector3 worldPos)
        {
            if (!IsReady)
            {
                return false;
            }

            if (!IsInsideVolume(worldPos))
            {
                return false;
            }

            var settings = NavCtx.BuildSettings;
            var local = worldPos - settings.Origin;
            var x = Mathf.FloorToInt(local.x / settings.VoxelSize);
            var y = Mathf.FloorToInt(local.y / settings.VoxelSize);
            var z = Mathf.FloorToInt(local.z / settings.VoxelSize);

            var gridDim = Mathf.RoundToInt(settings.RootSize / settings.VoxelSize);
            if (x < 0 || y < 0 || z < 0 || x >= gridDim || y >= gridDim || z >= gridDim)
            {
                return false;
            }

            return !NavCtx.Svo.IsVoxelOccupied(x, y, z);
        }

        /// <summary>
        /// Clamps <paramref name="worldPos"/> to the closest point that lies inside volume AABB.
        /// </summary>
        public Vector3 ClampToVolume(Vector3 worldPos)
        {
            var bounds = VolumeBounds;
            var clamped = new Vector3(
                Mathf.Clamp(worldPos.x, bounds.min.x, bounds.max.x),
                Mathf.Clamp(worldPos.y, bounds.min.y, bounds.max.y),
                Mathf.Clamp(worldPos.z, bounds.min.z, bounds.max.z)
            );

            return clamped;
        }

        /// <summary>
        /// Finds the closest navigable voxel center to <paramref name="worldPos"/> within <paramref name="maxDistance"/> meters.
        /// </summary>
        /// <remarks>
        /// The search is performed against the finest voxel grid in concentric shells around the query point.
        /// </remarks>
        public bool TrySnapToNavigable(Vector3 worldPos, float maxDistance, out Vector3 result)
        {
            result = default;

            if (!IsReady || maxDistance < 0f)
            {
                return false;
            }

            worldPos = ClampToVolume(worldPos);

            var settings = NavCtx.BuildSettings;
            var voxelSize = settings.VoxelSize;
            var gridDim = Mathf.RoundToInt(settings.RootSize / voxelSize);
            var maxDistSq = maxDistance * maxDistance;

            var local = worldPos - settings.Origin;
            var cx = Mathf.FloorToInt(local.x / voxelSize);
            var cy = Mathf.FloorToInt(local.y / voxelSize);
            var cz = Mathf.FloorToInt(local.z / voxelSize);

            var maxShell =
                maxDistance >= settings.RootSize
                    ? gridDim
                    : Mathf.Min(Mathf.CeilToInt(maxDistance / voxelSize), gridDim);

            var bestDistSq = float.MaxValue;
            var found = false;

            for (var r = 0; r <= maxShell; r++)
            {
                var shellNearVoxels = Mathf.Max(0, r - 1);
                var shellNearDistSq = (shellNearVoxels * voxelSize) * (shellNearVoxels * voxelSize);
                if (found && shellNearDistSq > bestDistSq)
                {
                    break;
                }

                for (var dx = -r; dx <= r; dx++)
                {
                    for (var dy = -r; dy <= r; dy++)
                    {
                        for (var dz = -r; dz <= r; dz++)
                        {
                            if (
                                Mathf.Max(Mathf.Abs(dx), Mathf.Max(Mathf.Abs(dy), Mathf.Abs(dz)))
                                != r
                            )
                            {
                                continue;
                            }

                            var x = cx + dx;
                            var y = cy + dy;
                            var z = cz + dz;

                            if (
                                x < 0
                                || y < 0
                                || z < 0
                                || x >= gridDim
                                || y >= gridDim
                                || z >= gridDim
                            )
                            {
                                continue;
                            }

                            if (NavCtx.Svo.IsVoxelOccupied(x, y, z))
                            {
                                continue;
                            }

                            var voxelCenter =
                                settings.Origin
                                + new Vector3(
                                    (x + 0.5f) * voxelSize,
                                    (y + 0.5f) * voxelSize,
                                    (z + 0.5f) * voxelSize
                                );

                            var distSq = (voxelCenter - worldPos).sqrMagnitude;
                            if (distSq > maxDistSq)
                            {
                                continue;
                            }

                            if (distSq < bestDistSq)
                            {
                                bestDistSq = distSq;
                                result = voxelCenter;
                                found = true;
                            }
                        }
                    }
                }
            }

            return found;
        }

        /// <summary>
        /// Samples a uniformly random navigable point inside the volume.
        /// </summary>
        public Vector3 GetRandomPoint()
        {
            TryGetRandomPoint(out var point, int.MaxValue);
            return point;
        }

        /// <summary>
        /// Samples a uniformly random navigable point inside the intersection of <paramref name="bounds"/> and the volume.
        /// </summary>
        public Vector3 GetRandomPointInBounds(Bounds bounds)
        {
            TryGetRandomPointInBounds(bounds, out var point, int.MaxValue);
            return point;
        }

        /// <summary>
        /// Samples a uniformly random navigable point inside the intersection of the given sphere and the volume.
        /// </summary>
        public Vector3 GetRandomPointInSphere(Vector3 center, float radius)
        {
            TryGetRandomPointInSphere(center, radius, out var point, int.MaxValue);
            return point;
        }

        /// <summary>
        /// Tries to sample a uniformly random navigable point inside the volume.
        /// </summary>
        /// <remarks>
        /// This method uses rejection sampling, so it will give up after <paramref name="maxAttempts"/> rejected samples.
        /// </remarks>
        public bool TryGetRandomPoint(out Vector3 point, int maxAttempts = 30)
        {
            return TryGetRandomPointInBounds(VolumeBounds, out point, maxAttempts);
        }

        /// <summary>
        /// Tries to sample a uniformly random navigable point inside the intersection of <paramref name="bounds"/> and the volume.
        /// </summary>
        /// <remarks>
        /// This method uses rejection sampling, so it will give up after <paramref name="maxAttempts"/> rejected samples.
        /// </remarks>
        public bool TryGetRandomPointInBounds(
            Bounds bounds,
            out Vector3 point,
            int maxAttempts = 30
        )
        {
            point = default;

            if (!IsReady || maxAttempts <= 0)
            {
                return false;
            }

            var volume = VolumeBounds;
            var min = Vector3.Max(bounds.min, volume.min);
            var max = Vector3.Min(bounds.max, volume.max);

            if (min.x > max.x || min.y > max.y || min.z > max.z)
            {
                return false;
            }

            for (var i = 0; i < maxAttempts; i++)
            {
                var candidate = new Vector3(
                    Random.Range(min.x, max.x),
                    Random.Range(min.y, max.y),
                    Random.Range(min.z, max.z)
                );

                if (IsNavigable(candidate))
                {
                    point = candidate;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Tries to sample a uniformly random navigable point inside the intersection of the given sphere and the volume.
        /// </summary>
        /// <remarks>
        /// This method uses rejection sampling, so it will give up after <paramref name="maxAttempts"/> rejected samples.
        /// </remarks>
        public bool TryGetRandomPointInSphere(
            Vector3 center,
            float radius,
            out Vector3 point,
            int maxAttempts = 30
        )
        {
            point = default;

            if (!IsReady || maxAttempts <= 0 || radius < 0f)
            {
                return false;
            }

            var volume = VolumeBounds;
            var sphereBox = new Bounds(center, Vector3.one * (radius * 2f));
            var min = Vector3.Max(sphereBox.min, volume.min);
            var max = Vector3.Min(sphereBox.max, volume.max);

            if (min.x > max.x || min.y > max.y || min.z > max.z)
            {
                return false;
            }

            var radiusSq = radius * radius;

            for (var i = 0; i < maxAttempts; i++)
            {
                var candidate = new Vector3(
                    Random.Range(min.x, max.x),
                    Random.Range(min.y, max.y),
                    Random.Range(min.z, max.z)
                );

                if ((candidate - center).sqrMagnitude > radiusSq)
                {
                    continue;
                }

                if (IsNavigable(candidate))
                {
                    point = candidate;
                    return true;
                }
            }

            return false;
        }
    }
}
