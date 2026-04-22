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
        // FIME: delete this
        public static NavVolumeSpace Instance;

        [SerializeField]
        BuildSettings BuildSettings;

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
            Instance = this;

            var builder = new SVOBuilder(BuildSettings);
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
            Gizmos.DrawWireCube(
                BuildSettings.Origin + Vector3.one * BuildSettings.RootSize / 2,
                Vector3.one * BuildSettings.RootSize
            );
        }
    }
}
