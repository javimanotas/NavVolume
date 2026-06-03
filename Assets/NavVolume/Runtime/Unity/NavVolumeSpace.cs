using System;
using System.Collections.Generic;
using System.Linq;
using NavVolume.Runtime;
using NavVolume.Runtime.Builder;
using NavVolume.Runtime.Core;
using NavVolume.Runtime.Pathfinding;
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

        [SerializeField]
        [Tooltip("The detail of the navigable space.")]
        [Range(1, 9)]
        int _numLayers = 5;

        [SerializeField]
        [Tooltip("Physics layers that count as solid obstacles.")]
        LayerMask _collisionMask = ~0;

        [SerializeField]
        BuildMode _buildMode;

        [SerializeField]
        NavVolumeBakedData _bakedData;

        #endregion

        internal NavContext NavCtx;

        internal NavVolumeBakedData BakedData => _bakedData;

        internal BuildMode BuildMode => _buildMode;

        internal BuildSettings CurrentSettings =>
            new(transform.position, _rootSize, _numLayers, _collisionMask, 0);

        Bounds VolumeBounds => new(transform.position, Vector3.one * _rootSize);

        public bool IsReady => NavCtx.Svo != null;

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
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                if (_bakedData.HasSceneBeenModifiedSinceBake(CurrentSettings))
                {
                    Debug.LogWarning(
                        "[NavVolume][NavVolumeSpace] The scene has been modified since the last bake. "
                            + "Navigation may be inaccurate. Bake the volume again."
                    );
                }

                NavCtx = _bakedData.RetrieveBakedData();

                Debug.Log(
                    $"[NavVolume][NavVolumeSpace] Baked data retrieved in {stopwatch.ElapsedMilliseconds} ms."
                );
            }
        }

        /// <summary>
        /// Builds synchronously the NavVolume.
        /// </summary>
        public void Build()
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var builder = new SVOBuilder(CurrentSettings);
            NavCtx = builder.Build();

            Debug.Log(
                $"[NavVolume][NavVolumeSpace] NavVolume built in {stopwatch.ElapsedMilliseconds} ms."
            );
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

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var raw = new SVOPathfinder().FindPath(NavCtx, request);

            if (!raw.Succeeded)
            {
                stopwatch.Stop();
                var failStats = new PathStats(
                    raw.Stats.NodesExpanded,
                    stopwatch.Elapsed.TotalMilliseconds,
                    0,
                    0
                );
                return PathResult.Failure(raw.Status, failStats);
            }

            var rawWaypoints = raw.Waypoints;
            var shortcut = PathSmoother.GreedyShortcut(rawWaypoints, in NavCtx);
            var smoothed = PathSmoother.CatmullRomSpline(shortcut);

            stopwatch.Stop();

            var stats = new PathStats(
                raw.Stats.NodesExpanded,
                stopwatch.Elapsed.TotalMilliseconds,
                Mathf.Max(0, rawWaypoints.Count - shortcut.Count),
                rawWaypoints.Count
            );

            return PathResult.Success(smoothed, rawWaypoints, stats);
        }
    }
}
