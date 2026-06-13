using NavVolume.Runtime.Avoidance;
using Unity.Mathematics;
using UnityEngine;

namespace NavVolume
{
    /// <summary>
    /// Volume that every <see cref="NavVolumeAgent"/> avoids entirely while flying.
    /// </summary>
    /// <remarks>
    /// Unlike geometry baked into the navigation volume, obstacles are dynamic: they can be
    /// enabled, disabled, moved and resized at runtime without rebaking. Agents treat them as hard
    /// constraints and take full responsibility for the avoidance, so an obstacle is never expected
    /// to move out of the way. Note that obstacles only affect local steering; paths are still
    /// planned against the baked data, so a large obstacle dropped on a narrow corridor can make
    /// agents wait in front of it rather than reroute.
    /// </remarks>
    [AddComponentMenu("NavVolume/NavVolume Obstacle")]
    [DisallowMultipleComponent]
    public class NavVolumeObstacle : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Shape of the obstacle volume.")]
        ObstacleShape _shape = ObstacleShape.Sphere;

        [SerializeField]
        [Tooltip("Local-space center of the obstacle volume.")]
        Vector3 _center;

        [SerializeField]
        [Tooltip("Sphere radius, scaled by the largest axis of the transform scale.")]
        [Min(0f)]
        float _radius = 0.5f;

        [SerializeField]
        [Tooltip("Full box size, scaled by the transform scale.")]
        Vector3 _size = Vector3.one;

        int _avoidanceHandle = -1;

        public ObstacleShape Shape
        {
            get => _shape;
            set => _shape = value;
        }

        public Vector3 Center
        {
            get => _center;
            set => _center = value;
        }

        public float Radius
        {
            get => _radius;
            set => _radius = Mathf.Max(0f, value);
        }

        public Vector3 Size
        {
            get => _size;
            set => _size = value;
        }

        void OnEnable()
        {
            var simulation = AvoidanceSimulation.GetOrCreate();

            if (simulation != null)
            {
                _avoidanceHandle = simulation.RegisterObstacle(this);
            }
        }

        void OnDisable()
        {
            if (_avoidanceHandle >= 0 && AvoidanceSimulation.Instance != null)
            {
                AvoidanceSimulation.Instance.UnregisterObstacle(_avoidanceHandle);
            }

            _avoidanceHandle = -1;
        }

        internal void UpdateAvoidanceHandle(int handle) => _avoidanceHandle = handle;

        /// <summary>
        /// World-space snapshot of the obstacle with zero velocity; the simulation derives the
        /// velocity from successive snapshots.
        /// </summary>
        internal AvoidanceObstacleState ComputeState()
        {
            var cachedTransform = transform;
            var position = (float3)cachedTransform.TransformPoint(_center);
            var scale = cachedTransform.lossyScale;
            var absScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            if (_shape == ObstacleShape.Sphere)
            {
                var radius = _radius * Mathf.Max(absScale.x, Mathf.Max(absScale.y, absScale.z));

                return new AvoidanceObstacleState
                {
                    Position = position,
                    Rotation = quaternion.identity,
                    HalfExtents = new float3(radius, 0f, 0f),
                    BoundingRadius = radius,
                    Shape = ObstacleShape.Sphere,
                };
            }

            var halfExtents = (float3)(0.5f * Vector3.Scale(_size, absScale));

            return new AvoidanceObstacleState
            {
                Position = position,
                Rotation = cachedTransform.rotation,
                HalfExtents = halfExtents,
                BoundingRadius = math.length(halfExtents),
                Shape = ObstacleShape.Box,
            };
        }
    }
}
