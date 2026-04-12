using System.Collections.Generic;
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
        NavVolumeSpace NavVolumeSpace;

        [Tooltip("World units per second.")]
        [SerializeField]
        float Speed = 1;

        [Tooltip("How close the agent must get to a waypoint before advancing.")]
        [SerializeField]
        float WaypointTolerance = 0.1f;

        List<Vector3> _waypoints = new();

        /// <summary>
        /// Request a path to a goal and begin flying.
        /// </summary>
        public void MoveTo(Vector3 goal)
        {
            if (NavVolumeSpace == null || !NavVolumeSpace.IsReady)
            {
                Debug.LogWarning("[SVOFlightAgent] NavVolumeSpace not ready.");
                return;
            }

            var result = NavVolumeSpace.FindPath(transform.position, goal);
            if (!result.Succeeded(out var status))
            {
                Debug.LogError($"[SVOFlightAgent] Pathfinding failed: {status} ");
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
                Speed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, _waypoints[0]) < WaypointTolerance)
            {
                _waypoints.RemoveAt(0);
            }
        }
    }
}
