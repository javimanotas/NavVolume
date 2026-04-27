using System.Collections.Generic;
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
        [Tooltip("Navigation component to query.")]
        [SerializeField]
        // TODO: instead of assigning this in the inspector, we should find it at runtime.
        // my current idea is to find which overlaps this agent's collider. if multiple overlap logwarning
        NavVolumeSpace _navVolumeSpace;

        [Tooltip("World units per second.")]
        [SerializeField]
        float _speed = 1;

        [Tooltip("How close the agent must get to a waypoint before advancing.")]
        [SerializeField]
        float _waypointTolerance = 0.1f;

        [Tooltip("Heuristic weight. Greater values imply faster results but with worse quality.")]
        [SerializeField]
        [Range(1f, 5f)]
        float _heuristicWeight = 1.5f;

        [Tooltip("Maximum A* nodes expanded before giving up. 0 = unlimited.")]
        [SerializeField]
        int _maxNodesBudget = 100_000;

        List<Vector3> _waypoints = new();

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
                _heuristicWeight,
                _maxNodesBudget
            );
            var result = _navVolumeSpace.FindPath(request);

            if (!result.Succeeded(out var status))
            {
                Debug.LogError($"[NavVolume][NavVolumeAgent] Pathfinding failed: {status} ");
                return;
            }

            _waypoints = result.Waypoints;
        }

        void Update()
        {
            if (_waypoints.Count == 0)
            {
                return;
            }

            // TODO: add rotation
            // TODO: consider using physics optional (check how unity navmesh works)

            transform.position = Vector3.MoveTowards(
                transform.position,
                _waypoints[0],
                _speed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, _waypoints[0]) < _waypointTolerance)
            {
                _waypoints.RemoveAt(0);
            }
        }
    }
}
