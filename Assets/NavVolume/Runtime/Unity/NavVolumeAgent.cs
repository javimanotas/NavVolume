using System.Collections.Generic;
using NavVolume.Runtime;
using NavVolume.Runtime.Pathfinding;
using UnityEngine;

namespace NavVolume
{
    /// <summary>
    /// A simple flying agent that uses <see cref="Pathfinding.NavVolumeSpace"/> to navigate between a start and goal position in 3D space.
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

        const int _MAX_NODES_BUDGET = 10000_000;

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
        }

        /// <summary>
        /// Request a path to a goal and begin flying.
        /// </summary>
        public void MoveTo(Vector3 goal)
        {
            if (_navVolumeSpace == null || !_navVolumeSpace.IsReady)
            {
                Debug.LogWarning(
                    "[NavVolume][NavVolumeAgent] Can't find path: NavVolumeSpace not ready."
                );
                return;
            }

            var request = new PathRequest(
                transform.position,
                goal,
                AgentType.HeuristicWeight,
                _MAX_NODES_BUDGET
            );

            var result = _navVolumeSpace.FindPath(request);
            _lastPath = result;

            if (!result.Succeeded)
            {
                Debug.LogError($"[NavVolume][NavVolumeAgent] Pathfinding failed: {result.Status} ");
                return;
            }

            _smoothedWaypoints = result.Waypoints;
            _rawWaypoints = result.RawWaypoints ?? new List<Vector3>();
            _currentWaypointIndex = 0;
        }

        void Update()
        {
            if (!HasActivePath)
            {
                return;
            }

            var target = _smoothedWaypoints[_currentWaypointIndex];
            var toTarget = target - transform.position;

            UpdateRotation(toTarget);

            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                _speed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, target) < _WAYPOINT_TOLERANCE)
            {
                _currentWaypointIndex++;
            }
        }

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
    }
}
