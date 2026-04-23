using System;
using NavVolume.Builder;
using NavVolume.Pathfinding;
using UnityEngine;

namespace NavVolume
{
    /// <summary>
    /// MonoBehaviour that owns a Navigation Volume and exposes 3D flight pathfinding to agents.
    /// </summary>
    [AddComponentMenu("NavVolume/NavVolume Space")]
    [DisallowMultipleComponent]
    public class NavVolumeSpace : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Side length of the cubic world volume (meters).")]
        [Min(0)]
        float _rootSize = 100f;

        [SerializeField]
        [Tooltip("The detail of the navigable space.")]
        [Range(1, 9)]
        int _numLayers = 5;

        [SerializeField]
        [Tooltip("Physics layers that count as solid obstacles.")]
        LayerMask _collisionMask = ~0;

        [Header("Pathfinding")]
        [Tooltip("Heuristic weight. Greater values imply faster results but with worse quality.")]
        [SerializeField]
        [Range(1f, 5f)]
        float HeuristicWeight = 1.5f;

        [Tooltip("Maximum A* nodes expanded before giving up. 0 = unlimited.")]
        [SerializeField]
        int MaxNodesBudget = 100_000;

        internal NavContext NavCtx;

        public bool IsReady => NavCtx.Svo != null;

        void Awake()
        {
            var settings = new BuildSettings(
                _numLayers,
                transform.position,
                _rootSize,
                _collisionMask
            );

            var builder = new SVOBuilder(settings);

            NavCtx = builder.Build();
            Debug.Log(NavCtx);
        }

        /// <summary>
        /// Find a path synchronously.
        /// </summary>
        internal PathResult FindPath(Vector3 start, Vector3 goal)
        {
            if (!IsReady)
            {
                return PathResult.Failed(PathStatus.NoTree);
            }

            var request = new PathRequest
            {
                Start = start,
                Goal = goal,
                HeuristicWeight = HeuristicWeight,
                MaxNodesBudget = MaxNodesBudget,
            };

            return new SVOPathfinder().FindPath(NavCtx, request);
        }

        public void OnDrawGizmos()
        {
            // TODO: add propper gizmos and editor visualization
            Gizmos.DrawWireCube(transform.position, Vector3.one * _rootSize);
        }
    }
}
