using System;
using System.Collections.Generic;
using System.Threading;
using NavVolume.Runtime;
using NavVolume.Runtime.Avoidance;
using NavVolume.Runtime.Pathfinding;
using UnityEngine;

namespace NavVolume
{
    /// <summary>
    /// A simple flying agent that uses <see cref="NavVolumeSpace"/> to navigate between a start and goal position in 3D space.
    /// </summary>
    [AddComponentMenu("NavVolume/NavVolume Agent")]
    [DisallowMultipleComponent]
    public class NavVolumeAgent : MonoBehaviour
    {
        [field: SerializeField]
        [Tooltip("Type of the agent which determines its base parameters and behaviour.")]
        public AgentType AgentType { get; private set; }

        [Tooltip("Movement speed in world units per second.")]
        [SerializeField]
        float _speed = 1;

        [Tooltip(
            "Maximum rotation speed in degrees per second when turning to face the movement direction."
        )]
        [SerializeField]
        float _angularSpeed = 360f;

        [SerializeField]
        internal bool _freezeRotationX;

        [SerializeField]
        internal bool _freezeRotationY;

        [SerializeField]
        internal bool _freezeRotationZ;

        [Tooltip(
            "Enables ORCA local avoidance against other agents, obstacles and the baked volume. "
                + "Even with avoidance disabled the agent is still registered, so others keep "
                + "avoiding it."
        )]
        [SerializeField]
        bool _isAvoidanceEnabled = true;

        const int _MAX_NODES_BUDGET = 10000_000;

        #region Avoidance fallbacks when no AgentType is assigned

        const float _FALLBACK_RADIUS = 0.5f;
        const float _FALLBACK_NEIGHBOR_RANGE = 10f;
        const int _FALLBACK_MAX_NEIGHBORS = 10;
        const float _FALLBACK_TIME_HORIZON_AGENTS = 2f;
        const float _FALLBACK_TIME_HORIZON_OBSTACLES = 1f;

        #endregion

        /// <summary>
        /// Velocities below this are treated as standing still (no movement, no rotation).
        /// </summary>
        const float _MIN_SPEED_SQ = 1e-8f;

        int _avoidanceHandle = -1;

        int _spaceIndex = -1;

        Vector3 _initialEuler;

        NavVolumeSpace _navVolumeSpace;

        /// <summary>
        /// The <see cref="NavVolumeSpace"/> this agent is bound to.
        /// </summary>
        public NavVolumeSpace NavVolumeSpace => _navVolumeSpace;

        const float _WAYPOINT_TOLERANCE = 0.1f;

        List<Vector3> _smoothedWaypoints = new();

        List<Vector3> _rawWaypoints = new();

        int _currentWaypointIndex;

        PathResult? _lastPath;

        CancellationTokenSource _pathCts;

        Vector3 _currentGoal;

        bool _hasDestination;

        internal IReadOnlyList<Vector3> SmoothedWaypoints => _smoothedWaypoints;

        internal IReadOnlyList<Vector3> RawWaypoints => _rawWaypoints;

        internal int CurrentWaypointIndex => _currentWaypointIndex;

        internal bool HasActivePath => _currentWaypointIndex < _smoothedWaypoints.Count;

        internal PathResult? LastPath => _lastPath;

        void Start()
        {
            _initialEuler = transform.rotation.eulerAngles;

            if (!NavVolumeSpace.FindBetterInstanceFor(this, out var navVolumeSpace))
            {
                Debug.LogError(
                    "[NavVolume][NavVolumeAgent] No suitable NavVolumeSpace found for agent"
                );
                return;
            }

            _navVolumeSpace = navVolumeSpace;
            _navVolumeSpace.Rebuilt += OnNavVolumeRebuilt;

            var simulation = AvoidanceSimulation.Instance;

            if (simulation != null)
            {
                _spaceIndex = simulation.RegisterSpace(_navVolumeSpace);
            }
        }

        void OnEnable()
        {
            var simulation = AvoidanceSimulation.GetOrCreate();

            if (simulation != null)
            {
                _avoidanceHandle = simulation.RegisterAgent(
                    this,
                    BuildAvoidanceState(Vector3.zero, Vector3.zero)
                );
            }
        }

        void OnDisable()
        {
            if (_avoidanceHandle >= 0 && AvoidanceSimulation.Instance != null)
            {
                AvoidanceSimulation.Instance.UnregisterAgent(_avoidanceHandle);
            }

            _avoidanceHandle = -1;
        }

        void OnDestroy()
        {
            if (_navVolumeSpace != null)
            {
                _navVolumeSpace.Rebuilt -= OnNavVolumeRebuilt;
            }

            _pathCts?.Cancel();
        }

        void OnNavVolumeRebuilt()
        {
            if (_hasDestination)
            {
                SetDestination(_currentGoal);
            }
        }

        /// <summary>
        /// Request a path to a goal and begin flying once it is found.
        /// </summary>
        /// <remarks>
        /// The path is computed on a background thread.
        /// Calling this during the calculation cancels the previous request.
        /// </remarks>
        public void SetDestination(Vector3 goal)
        {
            _currentGoal = goal;
            _hasDestination = true;

            if (_navVolumeSpace == null || !_navVolumeSpace.IsReady)
            {
                Debug.LogWarning(
                    "[NavVolume][NavVolumeAgent] Can't find path: NavVolumeSpace not ready."
                );
                return;
            }

            _pathCts?.Cancel();

            var cts = new CancellationTokenSource();
            _pathCts = cts;

            var request = new PathRequest(
                transform.position,
                goal,
                AgentType.HeuristicWeight,
                _MAX_NODES_BUDGET,
                AgentType.CostMode
            );

            FindPathAndFollow(request, cts);
        }

        async void FindPathAndFollow(PathRequest request, CancellationTokenSource cts)
        {
            try
            {
                var result = await _navVolumeSpace.FindPathAsync(request, cts.Token);

                if (cts.IsCancellationRequested)
                {
                    return;
                }

                _lastPath = result;

                if (!result.Succeeded)
                {
                    Debug.LogError(
                        $"[NavVolume][NavVolumeAgent] Pathfinding failed: {result.Status} "
                    );
                    return;
                }

                _smoothedWaypoints = result.Waypoints;
                _rawWaypoints = result.RawWaypoints ?? new List<Vector3>();
                _currentWaypointIndex = 0;
            }
            catch (OperationCanceledException) { }
            finally
            {
                if (_pathCts == cts)
                {
                    _pathCts = null;
                }

                cts.Dispose();
            }
        }

        void Update()
        {
            var deltaTime = Time.deltaTime;

            if (deltaTime <= 0f)
            {
                return;
            }

            var velocity =
                _isAvoidanceEnabled && _avoidanceHandle >= 0 && AvoidanceSimulation.Instance != null
                    ? MoveWithAvoidance(deltaTime)
                    : MoveAlongPath(deltaTime);

            SubmitAvoidanceState(velocity, deltaTime);
        }

        #region Movement

        /// <summary>
        /// Direct path following without local avoidance.
        /// </summary>
        Vector3 MoveAlongPath(float deltaTime)
        {
            if (!HasActivePath)
            {
                return Vector3.zero;
            }

            var target = _smoothedWaypoints[_currentWaypointIndex];
            var toTarget = target - transform.position;

            UpdateRotation(toTarget);

            var previousPosition = transform.position;
            transform.position = Vector3.MoveTowards(previousPosition, target, _speed * deltaTime);

            if (Vector3.Distance(transform.position, target) < _WAYPOINT_TOLERANCE)
            {
                _currentWaypointIndex++;
            }

            return (transform.position - previousPosition) / deltaTime;
        }

        /// <summary>
        /// Integrates the velocity computed by the avoidance simulation in the previous step.
        /// </summary>
        /// <returns>
        /// The velocity actually realized, which feeds the next avoidance step.
        /// </returns>
        Vector3 MoveWithAvoidance(float deltaTime)
        {
            var velocity = AvoidanceSimulation.Instance.GetNewVelocity(_avoidanceHandle);
            var hasMoved = false;

            if (velocity.sqrMagnitude >= _MIN_SPEED_SQ)
            {
                var from = transform.position;
                var to = from + velocity * deltaTime;

                // Last-resort clamp: ORCA already steers around baked geometry, but the solver can
                // degrade when over-constrained, so never commit a step crossing an occupied voxel.
                if (_navVolumeSpace == null || _navVolumeSpace.IsSegmentNavigable(from, to))
                {
                    UpdateRotation(velocity);
                    transform.position = to;
                    hasMoved = true;
                }
            }

            AdvanceReachedWaypoints();

            return hasMoved ? velocity : Vector3.zero;
        }

        /// <summary>
        /// Consumes every waypoint already within tolerance.
        /// With avoidance the agent rarely passes exactly over a waypoint, so intermediate ones complete within the agent radius.
        /// </summary>
        void AdvanceReachedWaypoints()
        {
            var agentType = AgentType;
            var radius = agentType != null ? agentType.Radius : _FALLBACK_RADIUS;

            while (HasActivePath)
            {
                var isLast = _currentWaypointIndex == _smoothedWaypoints.Count - 1;
                var tolerance = isLast
                    ? Mathf.Max(_WAYPOINT_TOLERANCE, radius * 0.5f)
                    : Mathf.Max(_WAYPOINT_TOLERANCE, radius);
                var target = _smoothedWaypoints[_currentWaypointIndex];

                if (Vector3.Distance(transform.position, target) >= tolerance)
                {
                    break;
                }

                _currentWaypointIndex++;
            }
        }

        #endregion

        #region Avoidance state exchange

        /// <summary>
        /// Velocity the agent would take if it were alone: straight toward the current waypoint, slowing down on the final approach so it does not orbit the goal.
        /// </summary>
        Vector3 ComputePreferredVelocity(float deltaTime)
        {
            if (!HasActivePath)
            {
                return Vector3.zero;
            }

            var toTarget = _smoothedWaypoints[_currentWaypointIndex] - transform.position;
            var distance = toTarget.magnitude;

            if (distance < 1e-5f)
            {
                return Vector3.zero;
            }

            var desiredSpeed = Mathf.Min(_speed, distance / deltaTime);
            return toTarget * (desiredSpeed / distance);
        }

        void SubmitAvoidanceState(Vector3 velocity, float deltaTime)
        {
            if (_avoidanceHandle < 0)
            {
                return;
            }

            var simulation = AvoidanceSimulation.Instance;

            if (simulation == null)
            {
                return;
            }

            simulation.SubmitAgentState(
                _avoidanceHandle,
                BuildAvoidanceState(velocity, ComputePreferredVelocity(deltaTime))
            );
        }

        AvoidanceAgentState BuildAvoidanceState(Vector3 velocity, Vector3 prefVelocity)
        {
            var agentType = AgentType;

            return new AvoidanceAgentState
            {
                Position = transform.position,
                Velocity = velocity,
                PrefVelocity = prefVelocity,
                Radius = agentType != null ? agentType.Radius : _FALLBACK_RADIUS,
                MaxSpeed = _speed,
                NeighborRange =
                    agentType != null ? agentType.AvoidanceNeighborRange : _FALLBACK_NEIGHBOR_RANGE,
                TimeHorizonAgents =
                    agentType != null
                        ? agentType.AvoidanceTimeHorizonAgents
                        : _FALLBACK_TIME_HORIZON_AGENTS,
                TimeHorizonObstacles =
                    agentType != null
                        ? agentType.AvoidanceTimeHorizonObstacles
                        : _FALLBACK_TIME_HORIZON_OBSTACLES,
                MaxNeighbors =
                    agentType != null ? agentType.AvoidanceMaxNeighbors : _FALLBACK_MAX_NEIGHBORS,
                SpaceIndex = _spaceIndex,
            };
        }

        internal void UpdateAvoidanceHandle(int handle) => _avoidanceHandle = handle;

        #endregion

        void UpdateRotation(Vector3 direction)
        {
            if (direction.sqrMagnitude < 1e-6f)
            {
                return;
            }

            var desired = Quaternion.LookRotation(direction, Vector3.up);

            if (_freezeRotationX || _freezeRotationY || _freezeRotationZ)
            {
                var e = desired.eulerAngles;
                if (_freezeRotationX)
                    e.x = _initialEuler.x;
                if (_freezeRotationY)
                    e.y = _initialEuler.y;
                if (_freezeRotationZ)
                    e.z = _initialEuler.z;
                desired = Quaternion.Euler(e);
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                desired,
                _angularSpeed * Time.deltaTime
            );
        }

        void OnValidate()
        {
            if (GetComponent<NavVolumeObstacle>() != null)
            {
                Debug.LogError(
                    "[NavVolume][NavVolumeAgent] NavVolumeAgent and NavVolumeObstacle cannot be on the same GameObject."
                );
            }
        }
    }
}
