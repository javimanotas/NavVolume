using System.Collections.Generic;
using System.IO;
using NavVolume.Runtime.Builder;
using NavVolume.Runtime.Core;
using UnityEngine;

namespace NavVolume
{
    /// <summary>
    /// Serializable data with the already built <see cref="NavContext"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "NavVolumeBakedData", menuName = "NavVolume/Baked Data")]
    public class NavVolumeBakedData : ScriptableObject
    {
        [SerializeField]
        [HideInInspector]
        BuildSettings _buildSettings;

        /// <summary>
        /// The whole SVO (leaf bitmasks + every layer's nodes) packed into a single binary blob.
        /// </summary>
        /// <remarks>
        /// Stored as a raw <see cref="byte"/> array rather than nested struct arrays because Unity
        /// serializes a <c>byte[]</c> as one compact blob, whereas an array of hundreds of thousands
        /// of structs is written as millions of YAML lines. For large trees this turns multi-second
        /// asset saves into a few milliseconds.
        /// </remarks>
        [SerializeField]
        [HideInInspector]
        byte[] _blob;

        /// <summary>
        /// Deterministic hash to ensure scene integrity.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        ulong _sceneHash;

        /// <summary>
        /// Bytes per serialized node: morton + first child + parent + 6 neighbors, all <see cref="uint"/>.
        /// </summary>
        const int _BYTES_PER_NODE = 9 * sizeof(uint);

        public bool IsEmpty => _blob == null || _blob.Length == 0;

        /// <summary>
        /// Ensures all the variables involved on the build process match the current ones.
        /// </summary>
        internal bool HasSceneBeenModifiedSinceBake(BuildSettings buildSettings) =>
            buildSettings != _buildSettings
            || _sceneHash != BakedDataHasher.ComputeSceneHash(buildSettings);

        /// <summary>
        /// Stores the data in the <see cref="ScriptableObject"/> given the <see cref="NavContext"/>.
        /// </summary>
        internal void PopulateData(NavContext ctx)
        {
            _sceneHash = BakedDataHasher.ComputeSceneHash(ctx.BuildSettings);
            _buildSettings = ctx.BuildSettings;

            var leaves = ctx.Svo.LeafNodes;
            var layers = ctx.Svo.Layers;

            var totalBytes = sizeof(int) + leaves.Length * sizeof(ulong) + sizeof(int);
            foreach (var layer in layers)
            {
                totalBytes += sizeof(int) + layer.Count * _BYTES_PER_NODE;
            }

            using var stream = new MemoryStream(totalBytes);
            using var writer = new BinaryWriter(stream);

            writer.Write(leaves.Length);
            foreach (var leaf in leaves)
            {
                writer.Write(leaf.RawBits);
            }

            writer.Write(layers.Length);
            foreach (var layer in layers)
            {
                writer.Write(layer.Count);
                foreach (var node in layer)
                {
                    var (
                        mortonCode,
                        firstChild,
                        parent,
                        posXNeighbor,
                        negXNeighbor,
                        posYNeighbor,
                        negYNeighbor,
                        posZNeighbor,
                        negZNeighbor
                    ) = node;

                    writer.Write((uint)mortonCode);
                    writer.Write((uint)firstChild);
                    writer.Write((uint)parent);
                    writer.Write((uint)posXNeighbor);
                    writer.Write((uint)negXNeighbor);
                    writer.Write((uint)posYNeighbor);
                    writer.Write((uint)negYNeighbor);
                    writer.Write((uint)posZNeighbor);
                    writer.Write((uint)negZNeighbor);
                }
            }

            writer.Flush();
            _blob = stream.ToArray();
        }

        /// <summary>
        /// Retrieves the baked data into a <see cref="NavContext"/>.
        /// </summary>
        internal NavContext RetrieveBakedData()
        {
            using var stream = new MemoryStream(_blob);
            using var reader = new BinaryReader(stream);

            var leafCount = reader.ReadInt32();
            var leaves = new SVOLeaf[leafCount];
            for (var i = 0; i < leafCount; i++)
            {
                leaves[i] = SVOLeaf.FromRawBits(reader.ReadUInt64());
            }

            var layerCount = reader.ReadInt32();
            var layers = new List<SVONode>[layerCount];
            for (var l = 0; l < layerCount; l++)
            {
                var nodeCount = reader.ReadInt32();
                var nodes = new List<SVONode>(nodeCount);

                for (var n = 0; n < nodeCount; n++)
                {
                    nodes.Add(
                        new SVONode(
                            reader.ReadUInt32(), // morton code
                            reader.ReadUInt32(), // first child
                            reader.ReadUInt32(), // parent
                            reader.ReadUInt32(), // +X neighbor
                            reader.ReadUInt32(), // -X neighbor
                            reader.ReadUInt32(), // +Y neighbor
                            reader.ReadUInt32(), // -Y neighbor
                            reader.ReadUInt32(), // +Z neighbor
                            reader.ReadUInt32() // -Z neighbor
                        )
                    );
                }

                layers[l] = nodes;
            }

            return new(new(leaves, layers), _buildSettings);
        }
    }
}
