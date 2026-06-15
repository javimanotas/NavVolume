using System;
using NavVolume.Runtime.Builder;
using UnityEngine;

namespace NavVolume
{
    /// <summary>
    /// Utilities for computing the hash of the scene objects involved in the build process.
    /// </summary>
    internal static class BakedDataHasher
    {
        /// <summary>
        /// Computes the FNV-1a hash of every collider involved in the build process covering:
        /// <list type="bullet">
        ///     <item> world-space transform (detects move / rotate / scale) </item>
        ///     <item> shape-specific parameters (detects resize) </item>
        /// </list>
        /// </summary>
        public static ulong ComputeSceneHash(BuildSettings settings)
        {
            var halfExtents = Vector3.one * (settings.RootSize * 0.5f);
            var center = settings.Origin + halfExtents;

            var colliders = Physics.OverlapBox(
                center,
                halfExtents,
                Quaternion.identity,
                settings.CollisionMask,
                QueryTriggerInteraction.Ignore
            );

            EnsureOrderInvariance(colliders);

            var hash = new FNV1a();

            foreach (var collider in colliders)
            {
                FeedHashWithColliderData(hash, collider);
            }

            return hash;
        }

        #region Order invariance

        /// <summary>
        /// I really don't know if Unity <see cref="Physics"/> will return the colliders always in the same order.
        /// This function tries to sort them using always the same attributes.
        /// </summary>
        static void EnsureOrderInvariance(Collider[] colliders)
        {
            Array.Sort(
                colliders,
                (a, b) => OrderInvariantFootprint(a).CompareTo(OrderInvariantFootprint(b))
            );
        }

        // With position, rotation and scale should be enough.
        static (
            float,
            float,
            float,
            float,
            float,
            float,
            float,
            float,
            float
        ) OrderInvariantFootprint(Collider collider)
        {
            var pos = collider.transform.position;
            var euler = collider.transform.eulerAngles;
            var scale = collider.transform.localScale;

            return (pos.x, pos.y, pos.z, euler.x, euler.y, euler.z, scale.x, scale.y, scale.z);
        }

        #endregion

        static void FeedHashWithColliderData(FNV1a hash, Collider collider)
        {
            hash.Feed(collider.transform.position);
            hash.Feed(collider.transform.eulerAngles);
            hash.Feed(collider.transform.lossyScale);

            switch (collider)
            {
                case BoxCollider box:
                    hash.Feed(box.center);
                    hash.Feed(box.size);
                    break;

                case SphereCollider sphere:
                    hash.Feed(sphere.center);
                    hash.Feed(sphere.radius);
                    break;

                case CapsuleCollider capsule:
                    hash.Feed(capsule.center);
                    hash.Feed(capsule.radius);
                    hash.Feed(capsule.height);
                    hash.Feed(capsule.direction);
                    break;

                case MeshCollider mesh:
                    hash.Feed(mesh.convex);
                    if (mesh.sharedMesh != null)
                    {
                        hash.Feed(mesh.sharedMesh.GetInstanceID());
                    }
                    break;

                case TerrainCollider terrain:
                    if (terrain.terrainData != null)
                    {
                        hash.Feed(terrain.terrainData.GetInstanceID());
                    }
                    break;
            }
        }
    }
}
