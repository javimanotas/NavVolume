using System.Collections.Generic;
using NavVolume.Runtime.Core;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace NavVolume.Runtime.Avoidance
{
    /// <summary>
    /// Runtime-created singleton that runs ORCA local avoidance for every
    /// <see cref="NavVolumeAgent"/> and <see cref="NavVolumeObstacle"/> in play mode.
    /// </summary>
    /// <remarks>
    /// Scheduling is pipelined to keep the main thread free: jobs are kicked off in
    /// <c>LateUpdate</c> with the state agents submitted during their <c>Update</c>, run across
    /// worker threads while the frame renders, and are completed at the start of the next frame
    /// (this component updates before agents, see <see cref="_EXECUTION_ORDER"/>). Agents therefore
    /// read velocities computed from one-frame-old state, the standard trade-off for asynchronous
    /// avoidance. Steady state performs no managed allocations.
    /// </remarks>
    [DefaultExecutionOrder(_EXECUTION_ORDER)]
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    internal sealed class AvoidanceSimulation : MonoBehaviour
    {
        const int _EXECUTION_ORDER = -100;
        const int _INITIAL_AGENT_CAPACITY = 64;
        const int _INITIAL_NODE_CAPACITY = 1024;
        const int _HASH_BATCH_SIZE = 64;
        const int _COMPUTE_BATCH_SIZE = 2;

        /// <summary>
        /// Lower bound for the spatial hash cell size, guarding against degenerate agent parameters.
        /// </summary>
        const float _MIN_CELL_SIZE = 1f;

        static AvoidanceSimulation s_Instance;

        /// <summary>
        /// The active simulation, or null when none has been created (or it was torn down).
        /// </summary>
        public static AvoidanceSimulation Instance => s_Instance;

        NativeList<AvoidanceAgentState> _agents;
        NativeList<float3> _newVelocities;
        NativeList<AvoidanceObstacleState> _obstacles;
        NativeList<VoxelGrid> _voxelGrids;
        NativeList<uint> _voxelMortons;
        NativeList<ulong> _voxelMasks;
        NativeParallelMultiHashMap<int, int> _spatialHash;

        readonly List<NavVolumeAgent> _agentOwners = new();
        readonly List<NavVolumeObstacle> _obstacleOwners = new();
        readonly List<NavVolumeSpace> _spaces = new();

        JobHandle _scheduledJobs;
        bool _hasScheduledJobs;
        bool _isVoxelDataDirty;

        /// <summary>
        /// Returns the singleton, creating its hidden GameObject on first use.
        /// Returns null outside play mode.
        /// </summary>
        public static AvoidanceSimulation GetOrCreate()
        {
            if (s_Instance == null && Application.isPlaying)
            {
                var gameObject = new GameObject("NavVolume Avoidance");
                DontDestroyOnLoad(gameObject);
                s_Instance = gameObject.AddComponent<AvoidanceSimulation>();
            }

            return s_Instance;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticState() => s_Instance = null;

        void Awake()
        {
            _agents = new(_INITIAL_AGENT_CAPACITY, Allocator.Persistent);
            _newVelocities = new(_INITIAL_AGENT_CAPACITY, Allocator.Persistent);
            _obstacles = new(_INITIAL_AGENT_CAPACITY, Allocator.Persistent);
            _voxelGrids = new(4, Allocator.Persistent);
            _voxelMortons = new(_INITIAL_NODE_CAPACITY, Allocator.Persistent);
            _voxelMasks = new(_INITIAL_NODE_CAPACITY, Allocator.Persistent);
            _spatialHash = new(_INITIAL_AGENT_CAPACITY, Allocator.Persistent);
        }

        void OnDestroy()
        {
            CompleteScheduledJobs();

            foreach (var space in _spaces)
            {
                if (space != null)
                {
                    space.Rebuilt -= HandleSpaceRebuilt;
                }
            }

            _agents.Dispose();
            _newVelocities.Dispose();
            _obstacles.Dispose();
            _voxelGrids.Dispose();
            _voxelMortons.Dispose();
            _voxelMasks.Dispose();
            _spatialHash.Dispose();

            if (s_Instance == this)
            {
                s_Instance = null;
            }
        }

        void Update() => CompleteScheduledJobs();

        void LateUpdate()
        {
            var deltaTime = Time.deltaTime;

            if (deltaTime <= 0f)
            {
                return;
            }

            // Synced even without agents so obstacle velocities never spike from stale positions
            // when the first agent appears.
            SyncObstacles(deltaTime);

            if (_agents.Length == 0)
            {
                return;
            }

            RefreshVoxelDataIfNeeded();
            ScheduleJobs(deltaTime);
        }

        #region Agent registry

        /// <summary>
        /// Registers an agent and returns its handle. Handles are dense indices: removing an agent
        /// moves the last one into its slot, notifying the moved owner.
        /// </summary>
        public int RegisterAgent(NavVolumeAgent owner, in AvoidanceAgentState initialState)
        {
            CompleteScheduledJobs();

            _agentOwners.Add(owner);
            _agents.Add(initialState);
            _newVelocities.Add(float3.zero);

            return _agents.Length - 1;
        }

        public void UnregisterAgent(int handle)
        {
            CompleteScheduledJobs();

            var last = _agentOwners.Count - 1;
            _agents.RemoveAtSwapBack(handle);
            _newVelocities.RemoveAtSwapBack(handle);
            _agentOwners[handle] = _agentOwners[last];
            _agentOwners.RemoveAt(last);

            if (handle < _agentOwners.Count)
            {
                _agentOwners[handle].UpdateAvoidanceHandle(handle);
            }
        }

        public void SubmitAgentState(int handle, in AvoidanceAgentState state)
        {
            CompleteScheduledJobs();
            _agents[handle] = state;
        }

        /// <summary>
        /// Velocity computed by the last avoidance step for this agent.
        /// </summary>
        public Vector3 GetNewVelocity(int handle)
        {
            CompleteScheduledJobs();
            return _newVelocities[handle];
        }

        #endregion

        #region Obstacle registry

        public int RegisterObstacle(NavVolumeObstacle owner)
        {
            CompleteScheduledJobs();

            _obstacleOwners.Add(owner);
            _obstacles.Add(owner.ComputeState());

            return _obstacles.Length - 1;
        }

        public void UnregisterObstacle(int handle)
        {
            CompleteScheduledJobs();

            var last = _obstacleOwners.Count - 1;
            _obstacles.RemoveAtSwapBack(handle);
            _obstacleOwners[handle] = _obstacleOwners[last];
            _obstacleOwners.RemoveAt(last);

            if (handle < _obstacleOwners.Count)
            {
                _obstacleOwners[handle].UpdateAvoidanceHandle(handle);
            }
        }

        #endregion

        #region Space registry

        /// <summary>
        /// Registers a space's baked occupancy for voxel constraints and returns its index.
        /// Spaces are never unregistered, a destroyed space keeps its slot with an empty grid so
        /// agent space indices stay valid.
        /// </summary>
        public int RegisterSpace(NavVolumeSpace space)
        {
            var index = _spaces.IndexOf(space);

            if (index >= 0)
            {
                return index;
            }

            _spaces.Add(space);
            space.Rebuilt += HandleSpaceRebuilt;
            _isVoxelDataDirty = true;

            return _spaces.Count - 1;
        }

        void HandleSpaceRebuilt() => _isVoxelDataDirty = true;

        void RefreshVoxelDataIfNeeded()
        {
            if (!_isVoxelDataDirty)
            {
                // A destroyed space must drop its grid so agents stop avoiding stale geometry.
                for (var i = 0; i < _spaces.Count; i++)
                {
                    if (_spaces[i] == null && _voxelGrids[i].NodeCount > 0)
                    {
                        _isVoxelDataDirty = true;
                        break;
                    }
                }
            }

            if (!_isVoxelDataDirty)
            {
                return;
            }

            CompleteScheduledJobs();
            _isVoxelDataDirty = false;

            _voxelGrids.Clear();
            _voxelMortons.Clear();
            _voxelMasks.Clear();

            foreach (var space in _spaces)
            {
                var grid = default(VoxelGrid);

                if (space != null && space.IsReady)
                {
                    var ctx = space.NavCtx;
                    var nodes = ctx.Svo.Layers[0];
                    var leaves = ctx.Svo.LeafNodes;
                    var settings = ctx.BuildSettings;

                    grid.NodeStart = _voxelMortons.Length;
                    grid.NodeCount = nodes.Length;
                    grid.NodeGridDim =
                        Mathf.RoundToInt(settings.RootSize / settings.VoxelSize)
                        / SVOLeaf.GRID_SIZE;
                    grid.Origin = settings.Origin;
                    grid.VoxelSize = settings.VoxelSize;
                    grid.NodeSize = settings.VoxelSize * SVOLeaf.GRID_SIZE;

                    for (var i = 0; i < nodes.Length; i++)
                    {
                        _voxelMortons.Add(nodes[i].MortonCode);
                        _voxelMasks.Add(leaves[i].RawBits);
                    }
                }

                _voxelGrids.Add(grid);
            }
        }

        #endregion

        #region Simulation step

        void SyncObstacles(float deltaTime)
        {
            var invDeltaTime = 1f / deltaTime;

            for (var i = 0; i < _obstacleOwners.Count; i++)
            {
                var state = _obstacleOwners[i].ComputeState();
                state.Velocity = (state.Position - _obstacles[i].Position) * invDeltaTime;
                _obstacles[i] = state;
            }
        }

        void ScheduleJobs(float deltaTime)
        {
            var agentCount = _agents.Length;

            var cellSize = _MIN_CELL_SIZE;

            for (var i = 0; i < agentCount; i++)
            {
                cellSize = Mathf.Max(cellSize, _agents[i].NeighborRange);
            }

            if (_spatialHash.Capacity < agentCount)
            {
                _spatialHash.Capacity = agentCount * 2;
            }

            _spatialHash.Clear();

            var hashJob = new SpatialHashJob
            {
                Agents = _agents.AsArray(),
                InvCellSize = 1f / cellSize,
                Writer = _spatialHash.AsParallelWriter(),
            }.Schedule(agentCount, _HASH_BATCH_SIZE);

            _scheduledJobs = new AvoidanceJob
            {
                Agents = _agents.AsArray(),
                Obstacles = _obstacles.AsArray(),
                Hash = _spatialHash,
                VoxelGrids = _voxelGrids.AsArray(),
                VoxelMortons = _voxelMortons.AsArray(),
                VoxelMasks = _voxelMasks.AsArray(),
                CellSize = cellSize,
                TimeStep = deltaTime,
                NewVelocities = _newVelocities.AsArray(),
            }.Schedule(agentCount, _COMPUTE_BATCH_SIZE, hashJob);

            _hasScheduledJobs = true;
            JobHandle.ScheduleBatchedJobs();
        }

        /// <summary>
        /// Blocks until the in-flight step finishes. In steady state the jobs already ran during
        /// rendering, so the early-frame call returns immediately, the guards on every accessor
        /// only pay this cost when something touches the simulation between schedule and frame end.
        /// </summary>
        void CompleteScheduledJobs()
        {
            if (!_hasScheduledJobs)
            {
                return;
            }

            _scheduledJobs.Complete();
            _hasScheduledJobs = false;
        }

        #endregion
    }
}
