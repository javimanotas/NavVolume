using System;
using System.Linq;
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
        #region Auxiliary data structures

        [Serializable]
        struct NodeData
        {
            public uint MortonCode;
            public uint FirstChild;
            public uint Parent;
            public uint PosXNeighbour,
                NegXNeighbour,
                PosYNeighbour,
                NegYNeighbour,
                PosZNeighbour,
                NegZNeighbour;
        }

        [Serializable]
        struct Layer
        {
            public NodeData[] Nodes;
        }

        #endregion

        [SerializeField]
        [HideInInspector]
        BuildSettings _buildSettings;

        [SerializeField]
        [HideInInspector]
        SVOLeaf[] _leafnodes;

        [SerializeField]
        [HideInInspector]
        Layer[] _layers;

        /// <summary>
        /// Deterministic hash to ensure scene integrity.
        /// </summary>
        [SerializeField]
        [HideInInspector]
        ulong _sceneHash;

        public bool IsEmpty => _leafnodes == null || _layers == null || _layers.Length == 0;

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
            _leafnodes = ctx.Svo.LeafNodes;

            _layers = ctx
                .Svo.Layers.Select(l => new Layer()
                {
                    Nodes = l.Select(n =>
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
                            ) = n;

                            return new NodeData()
                            {
                                MortonCode = mortonCode,
                                FirstChild = firstChild,
                                Parent = parent,
                                PosXNeighbour = posXNeighbor,
                                NegXNeighbour = negXNeighbor,
                                PosYNeighbour = posYNeighbor,
                                NegYNeighbour = negYNeighbor,
                                PosZNeighbour = posZNeighbor,
                                NegZNeighbour = negZNeighbor,
                            };
                        })
                        .ToArray(),
                })
                .ToArray();
        }

        /// <summary>
        /// Retrieves the baked data into a <see cref="NavContext"/>.
        /// </summary>
        internal NavContext RetrieveBakedData()
        {
            var layers = _layers
                .Select(l =>
                    l.Nodes.Select(n => new SVONode(
                            n.MortonCode,
                            n.FirstChild,
                            n.Parent,
                            n.PosXNeighbour,
                            n.NegXNeighbour,
                            n.PosYNeighbour,
                            n.NegYNeighbour,
                            n.PosZNeighbour,
                            n.NegZNeighbour
                        ))
                        .ToList()
                )
                .ToArray();

            return new(new(_leafnodes, layers), _buildSettings);
        }
    }
}
