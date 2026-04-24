using System;
using NavVolume.Runtime.Builder;
using NavVolume.Runtime.Core;
using NavVolume.Runtime.Pathfinding;
using UnityEngine;

namespace NavVolume
{
    /// <summary>
    /// MonoBehaviour that owns a Navigation Volume and exposes 3D flight pathfinding to agents.
    /// </summary>
    [AddComponentMenu("NavVolume/Space")]
    [DisallowMultipleComponent]
    public class NavVolumeSpace : MonoBehaviour
    {
        #region Unity inspector fields

        [Header("Build settings")]
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

        [Header("Build Mode")]
        [SerializeField]
        BuildMode _buildMode;

        [SerializeField]
        NavVolumeBakedData _bakedData;

        #endregion
        BuildSettings CurrentSettings =>
            new(transform.position, _rootSize, _numLayers, _collisionMask);

        NavContext _navCtx;

        public bool IsReady => _navCtx.Svo != null;

        void Awake()
        {
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

        void LoadBakedData()
        {
            if (_bakedData == null)
            {
                Debug.LogError(
                    "[NavVolume][Space] Build mode is \"Baked\" but no baked data asset is assigned. "
                        + "Assign it and bake the volume."
                );
            }
            else if (_bakedData.IsEmpty)
            {
                Debug.LogError(
                    "[NavVolume][Space] Build mode is \"Baked\" but the baked data asset is empty. "
                        + "Bake the volume first."
                );
            }
            else
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                _bakedData.RetrieveBakedData();
                Debug.Log(
                    $"[NavVolume][Space] Baked data retrieved in {stopwatch.ElapsedMilliseconds} ms."
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
            _navCtx = builder.Build();

            Debug.Log(
                $"[NavVolume][Space] NavVolume built in {stopwatch.ElapsedMilliseconds} ms\n."
                    + $"Stats: {new SVOStats(_navCtx.Svo)}"
            );
        }

        /// <summary>
        /// Find a path synchronously.
        /// </summary>
        internal PathResult FindPath(PathRequest request)
        {
            if (!IsReady)
            {
                return PathResult.Failed(PathStatus.NoTree);
            }

            return new SVOPathfinder().FindPath(_navCtx, request);
        }

        void OnDrawGizmos()
        {
            // TODO: add propper gizmos and editor visualization
            Gizmos.DrawWireCube(transform.position, Vector3.one * _rootSize);
        }

#if UNITY_EDITOR
        internal void EditorBake()
        {
            if (_bakedData == null)
            {
                Debug.LogError(
                    "[NavVolume][Space] Can't bake navigable space because \"BakedData\" asset is not assigned."
                );
            }
            else
            {
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();

                var builder = new SVOBuilder(CurrentSettings);
                _navCtx = builder.Build();
                _bakedData.PopulateData(_navCtx);

                Debug.Log(
                    $"[NavVolume][Space] NavVolume baked in {stopwatch.ElapsedMilliseconds} ms\n."
                        + $"Stats: {new SVOStats(_navCtx.Svo)}"
                );
            }
        }
#endif
    }
}
