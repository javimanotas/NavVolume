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
    public class NavVolumeSpace : MonoBehaviour
    {
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

        NavContext _navCtx;

        public bool IsReady => _navCtx.Svo != null;

        void Awake()
        {
            var builder = new SVOBuilder(BuildSettings);
            _navCtx = builder.Build();
            Debug.Log(_navCtx);
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

            return new SVOPathfinder().FindPath(_navCtx, request);
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
