using System.Collections.Generic;
using System.Data;
using NavVolume.Runtime.Builder;
using NavVolume.Runtime.Core;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace NavVolume.Editor
{
    /// <summary>
    /// Custom editor for the <see cref="NavVolumeSpace"/> component.
    /// </summary>
    /// <remarks>
    /// Its responsibility is to manage the serialized data and provide a way to bake it.
    /// </remarks>
    [CustomEditor(typeof(NavVolumeSpace))]
    public class NavVolumeBakeEditor : UnityEditor.Editor
    {
        static readonly Color s_VisibleColor = new(0.57f, 0.95f, 0.54f, 1.00f);
        static readonly Color s_OccludedColor = new(0.57f, 0.95f, 0.54f, 0.25f);
        static readonly Color s_UnselectedColor = new(0.57f, 0.95f, 0.54f, 0.50f);

        #region SVO gizmo palette

        static readonly Color s_NodeWithChildrenColor = new(0.55f, 0.55f, 0.55f, 0.18f);
        static readonly Color s_NodeEmptyColor = new(0.40f, 0.85f, 0.55f, 0.18f);
        static readonly Color s_LeafEmptyColor = new(0.40f, 0.85f, 0.55f, 0.45f);
        static readonly Color s_LeafPartialColor = new(0.95f, 0.85f, 0.30f, 0.65f);
        static readonly Color s_LeafFullColor = new(0.95f, 0.35f, 0.35f, 0.75f);
        static readonly Color s_VoxelOccupiedColor = new(0.95f, 0.30f, 0.30f, 0.85f);

        const int _SVO_GIZMO_DRAW_BUDGET = 20000;
        const float _SVO_GIZMO_LAYER_RADIUS_FACTOR = 4f;

        static readonly Dictionary<int, NavContext> s_GizmoNavCtxCache = new();
        static readonly Dictionary<int, int> s_GizmoNavCtxCacheKey = new();

        #endregion

        static bool s_BuildSettingsFoldout = true;
        static bool s_BuildModeFoldout = true;
        static bool s_StatsFoldout = true;

        static NavVolumeBakeEditor s_ActiveEditor;

        bool IsEditing => s_ActiveEditor == this;

        void OnDisable()
        {
            if (IsEditing)
            {
                s_ActiveEditor = null;
                Tools.hidden = false;
            }
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.NotInSelectionHierarchy)]
        static void DrawUnselectedGizmo(NavVolumeSpace space, GizmoType _)
        {
            var prev = Gizmos.matrix;
            Gizmos.matrix = space.transform.localToWorldMatrix;
            Gizmos.color = s_UnselectedColor;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one * space.CurrentSettings.RootSize);
            Gizmos.matrix = prev;
        }

        #region SVO scene-view gizmos

        [DrawGizmo(GizmoType.Selected | GizmoType.InSelectionHierarchy)]
        static void DrawSvoSelectedGizmo(NavVolumeSpace space, GizmoType _)
        {
            if (!TryGetCachedNavContext(space, out var navCtx))
            {
                return;
            }

            var cam = Camera.current;
            if (cam == null)
            {
                return;
            }

            var prevZTest = Handles.zTest;
            Handles.zTest = CompareFunction.LessEqual;

            DrawSvoHierarchy(navCtx, cam.transform.position);

            Handles.zTest = prevZTest;
        }

        static bool TryGetCachedNavContext(NavVolumeSpace space, out NavContext navCtx)
        {
            if (space.NavCtx.Svo != null)
            {
                navCtx = space.NavCtx;
                return true;
            }

            if (space.BuildMode != BuildMode.Baked)
            {
                navCtx = default;
                return false;
            }

            var baked = space.BakedData;
            if (baked == null || baked.IsEmpty)
            {
                navCtx = default;
                return false;
            }

            var spaceId = space.GetInstanceID();
            var bakedId = baked.GetInstanceID();

            if (
                !s_GizmoNavCtxCacheKey.TryGetValue(spaceId, out var cachedKey)
                || cachedKey != bakedId
            )
            {
                s_GizmoNavCtxCache[spaceId] = baked.RetrieveBakedData();
                s_GizmoNavCtxCacheKey[spaceId] = bakedId;
            }

            navCtx = s_GizmoNavCtxCache[spaceId];
            return true;
        }

        static void InvalidateNavContextCache(NavVolumeSpace space)
        {
            var id = space.GetInstanceID();
            s_GizmoNavCtxCache.Remove(id);
            s_GizmoNavCtxCacheKey.Remove(id);
        }

        static void DrawSvoHierarchy(NavContext navCtx, Vector3 camPos)
        {
            var svo = navCtx.Svo;
            var rootLayer = (uint)(svo.Layers.Length - 1);
            var rootCount = svo.Layers[(int)rootLayer].Count;

            var drawn = 0;

            for (var i = 0; i < rootCount; i++)
            {
                DrawNodeRecursive(navCtx, rootLayer, (uint)i, camPos, ref drawn);

                if (drawn >= _SVO_GIZMO_DRAW_BUDGET)
                {
                    break;
                }
            }
        }

        static void DrawNodeRecursive(
            NavContext navCtx,
            uint layer,
            uint offset,
            Vector3 camPos,
            ref int drawn
        )
        {
            if (drawn >= _SVO_GIZMO_DRAW_BUDGET)
            {
                return;
            }

            var svo = navCtx.Svo;
            if ((int)layer >= svo.Layers.Length)
            {
                return;
            }

            var layerList = svo.Layers[(int)layer];
            if ((int)offset >= layerList.Count)
            {
                return;
            }

            var node = layerList[(int)offset];
            var bounds = navCtx.NodeBounds((int)layer, node.MortonCode);

            var nodeSize = navCtx.BuildSettings.NodeSizeForLayer((int)layer);
            var cullRadius = _SVO_GIZMO_LAYER_RADIUS_FACTOR * nodeSize;

            if (Vector3.Distance(bounds.center, camPos) > cullRadius + bounds.extents.magnitude)
            {
                return;
            }

            var isRoot = layer == svo.Layers.Length - 1;

            if (layer == 0)
            {
                DrawLeafNode(navCtx, (int)offset, bounds, ref drawn);
                return;
            }

            if (!isRoot)
            {
                Handles.color = node.HasChildren ? s_NodeWithChildrenColor : s_NodeEmptyColor;
                Handles.DrawWireCube(bounds.center, bounds.size);
                drawn++;
            }

            if (!node.HasChildren)
            {
                return;
            }

            var firstChild = node.FirstChild.Offset;
            for (var c = 0u; c < 8; c++)
            {
                DrawNodeRecursive(navCtx, layer - 1, firstChild + c, camPos, ref drawn);

                if (drawn >= _SVO_GIZMO_DRAW_BUDGET)
                {
                    return;
                }
            }
        }

        static void DrawLeafNode(NavContext navCtx, int leafIdx, Bounds bounds, ref int drawn)
        {
            var leaf = navCtx.Svo.LeafNodes[leafIdx];

            Handles.color =
                leaf.IsEmpty ? s_LeafEmptyColor
                : leaf.IsFull ? s_LeafFullColor
                : s_LeafPartialColor;
            Handles.DrawWireCube(bounds.center, bounds.size);
            drawn++;

            if (leaf.IsEmpty || leaf.IsFull)
            {
                return;
            }

            var voxelSize = navCtx.BuildSettings.VoxelSize;
            var voxelExtent = Vector3.one * voxelSize;
            var nodeMin = bounds.min;

            Handles.color = s_VoxelOccupiedColor;

            for (var idx = 0; idx < SVOLeaf.NUM_VOXELS; idx++)
            {
                if (!leaf.IsOccupied(idx))
                {
                    continue;
                }

                Handles.DrawWireCube(navCtx.VoxelCenter(nodeMin, idx), voxelExtent);
                drawn++;

                if (drawn >= _SVO_GIZMO_DRAW_BUDGET)
                {
                    return;
                }
            }
        }

        #endregion

        #region Editor GUI

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(
                serializedObject,
                "m_Script",
                "_rootSize",
                "_numLayers",
                "_collisionMask",
                "_buildMode",
                "_bakedData"
            );

            BuildSettingsFoldoutGUI();
            BuildModeFoldoutGUI();
            StatsFoldoutGUI();

            serializedObject.ApplyModifiedProperties();
        }

        void StatsFoldoutGUI()
        {
            EditorGUILayout.Space(2);

            s_StatsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(s_StatsFoldout, "Stats");

            if (s_StatsFoldout)
            {
                EditorGUI.indentLevel++;

                var space = (NavVolumeSpace)target;

                if (!TryGetCachedNavContext(space, out var navCtx))
                {
                    EditorGUILayout.HelpBox(
                        "No SVO available. Bake the volume (or enter Play Mode with BuildOnAwake) "
                            + "to see stats.",
                        MessageType.Info
                    );
                }
                else
                {
                    DrawStatsRows(new SVOStats(navCtx.Svo));
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        static void DrawStatsRows(SVOStats stats)
        {
            var savedPct =
                stats.TheoreticalVoxelsCount == 0
                    ? 0f
                    : (1f - (float)stats.VoxelsCount / stats.TheoreticalVoxelsCount) * 100f;
            var memoryKB = stats.MemoryUsedBytes / 1024f;

            EditorGUILayout.LabelField("Num Layers", stats.NumLayers.ToString());
            EditorGUILayout.LabelField(
                "Allocated Voxels",
                $"{stats.VoxelsCount:N0} / {stats.TheoreticalVoxelsCount:N0}"
            );
            EditorGUILayout.LabelField("Sparse Savings", $"{savedPct:F2} %");
            EditorGUILayout.LabelField("Memory (approx.)", $"{memoryKB:N1} KB");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Nodes per Layer", EditorStyles.boldLabel);
            DrawNodesPerLayerChart(stats.NodesPerLayer);
        }

        static readonly Color s_ChartBarColor = new(0.40f, 0.75f, 0.95f, 1f);
        static readonly Color s_ChartBgColor = new(0.18f, 0.18f, 0.18f, 1f);

        static void DrawNodesPerLayerChart(int[] nodesPerLayer)
        {
            if (nodesPerLayer == null || nodesPerLayer.Length == 0)
            {
                return;
            }

            var maxCount = 0;
            foreach (var c in nodesPerLayer)
            {
                if (c > maxCount)
                {
                    maxCount = c;
                }
            }

            if (maxCount == 0)
            {
                EditorGUILayout.LabelField("(no nodes)");
                return;
            }

            const float _CHART_HEIGHT = 64f;
            const float _LABEL_HEIGHT = 14f;
            const float _BAR_SPACING = 2f;

            var totalRect = EditorGUILayout.GetControlRect(
                false,
                _CHART_HEIGHT + _LABEL_HEIGHT * 2 + 4f
            );

            var labelTop = new Rect(totalRect.x, totalRect.y, totalRect.width, _LABEL_HEIGHT);
            var barsRect = new Rect(
                totalRect.x,
                totalRect.y + _LABEL_HEIGHT,
                totalRect.width,
                _CHART_HEIGHT
            );
            var labelBottom = new Rect(
                totalRect.x,
                totalRect.y + _LABEL_HEIGHT + _CHART_HEIGHT + 2f,
                totalRect.width,
                _LABEL_HEIGHT
            );

            EditorGUI.DrawRect(barsRect, s_ChartBgColor);

            var n = nodesPerLayer.Length;
            var barWidth = (barsRect.width - _BAR_SPACING * (n - 1)) / n;

            var topStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.LowerCenter,
            };
            var bottomStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.UpperCenter,
            };

            for (var i = 0; i < n; i++)
            {
                var count = nodesPerLayer[i];
                var ratio = (float)count / maxCount;
                var barHeight = ratio * (barsRect.height - 2f);

                var bar = new Rect(
                    barsRect.x + i * (barWidth + _BAR_SPACING),
                    barsRect.yMax - barHeight,
                    barWidth,
                    barHeight
                );
                EditorGUI.DrawRect(bar, s_ChartBarColor);

                var topLabel = new Rect(
                    barsRect.x + i * (barWidth + _BAR_SPACING),
                    labelTop.y,
                    barWidth,
                    _LABEL_HEIGHT
                );
                var bottomLabel = new Rect(
                    barsRect.x + i * (barWidth + _BAR_SPACING),
                    labelBottom.y,
                    barWidth,
                    _LABEL_HEIGHT
                );

                EditorGUI.LabelField(topLabel, count.ToString(), topStyle);
                EditorGUI.LabelField(bottomLabel, LayerName(i, n), bottomStyle);
            }
        }

        static string LayerName(int chartIndex, int total)
        {
            // NodesPerLayer is ordered root → ... → layer 0 → leaves.
            if (chartIndex == 0)
            {
                return "root";
            }

            if (chartIndex == total - 1)
            {
                return "leaves";
            }

            // chartIndex maps to layer (total-2 - chartIndex) when 1 <= chartIndex <= total-2
            var layer = total - 2 - chartIndex;
            return $"L{layer}";
        }

        void BuildSettingsFoldoutGUI()
        {
            var sizeProp = serializedObject.FindProperty("_rootSize");
            var layerProp = serializedObject.FindProperty("_numLayers");
            var maskProp = serializedObject.FindProperty("_collisionMask");

            EditorGUILayout.Space(4);

            s_BuildSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                s_BuildSettingsFoldout,
                "Build Settings"
            );

            if (s_BuildSettingsFoldout)
            {
                EditorGUI.indentLevel++;

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PropertyField(sizeProp);

                    var icon = EditorGUIUtility.IconContent("EditCollider");
                    icon.tooltip = "Toggle scene-view handles to resize the NavVolume.";

                    var nowEditing = GUILayout.Toggle(
                        IsEditing,
                        icon,
                        "Button",
                        GUILayout.Width(25),
                        GUILayout.Height(EditorGUIUtility.singleLineHeight)
                    );

                    if (nowEditing != IsEditing)
                    {
                        s_ActiveEditor = nowEditing ? this : null;
                        Tools.hidden = nowEditing;
                        SceneView.RepaintAll();
                    }
                }

                EditorGUILayout.PropertyField(layerProp);
                EditorGUILayout.PropertyField(maskProp);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void BuildModeFoldoutGUI()
        {
            var modeProp = serializedObject.FindProperty("_buildMode");
            var dataProp = serializedObject.FindProperty("_bakedData");

            var space = (NavVolumeSpace)target;
            var mode = (BuildMode)modeProp.enumValueIndex;

            EditorGUILayout.Space(2);

            s_BuildModeFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                s_BuildModeFoldout,
                "Build Mode"
            );

            if (s_BuildModeFoldout)
            {
                EditorGUI.indentLevel++;

                EditorGUILayout.PropertyField(modeProp);

                if (mode == BuildMode.Baked)
                {
                    EditorGUILayout.PropertyField(dataProp);

                    if (dataProp.objectReferenceValue == null)
                    {
                        EditorGUILayout.HelpBox(
                            "BakedData slot is empty. "
                                + "Create an asset via Assets > Create > NavVolume > Baked Data, assign it here, then press Bake.",
                            MessageType.Warning
                        );
                    }

                    EditorGUILayout.Space(4);

                    if (GUILayout.Button("Bake", GUILayout.Height(28)))
                    {
                        Bake(space, dataProp);
                    }
                }

                EditorGUILayout.Space(4);

                GUI.enabled = Application.isPlaying;

                if (GUILayout.Button("Rebuild Now (Play Mode)", GUILayout.Height(28)))
                {
                    space.Build();
                    InvalidateNavContextCache(space);
                    SceneView.RepaintAll();
                }

                GUI.enabled = true;

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        #endregion

        #region Scene GUI

        void OnSceneGUI()
        {
            Tools.hidden = IsEditing;

            var sizeProp = serializedObject.FindProperty("_rootSize");

            var prevMatrix = Handles.matrix;
            Handles.matrix = (target as NavVolumeSpace).transform.localToWorldMatrix;

            DrawBounds(sizeProp.floatValue);

            if (IsEditing)
            {
                DrawHandles(sizeProp);
            }

            Handles.matrix = prevMatrix;
            Handles.zTest = CompareFunction.Always;
        }

        void DrawBounds(float rootSize)
        {
            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = s_VisibleColor;
            Handles.DrawWireCube(Vector3.zero, Vector3.one * rootSize);

            Handles.zTest = CompareFunction.Greater;
            Handles.color = s_OccludedColor;
            Handles.DrawWireCube(Vector3.zero, Vector3.one * rootSize);
        }

        void DrawHandles(SerializedProperty sizeProp)
        {
            var halfSize = sizeProp.floatValue * 0.5f;

            Handles.zTest = CompareFunction.LessEqual;
            Handles.color = s_VisibleColor;

            var directions = new Vector3[]
            {
                Vector3.right,
                Vector3.up,
                Vector3.forward,
                Vector3.left,
                Vector3.down,
                Vector3.forward,
            };

            foreach (var direction in directions)
            {
                EditorGUI.BeginChangeCheck();

                var pos = direction * halfSize;
                var newPos = Handles.Slider(
                    pos,
                    direction,
                    HandleSizeAt(pos),
                    Handles.DotHandleCap,
                    0f
                );

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(target, "Resize NavVolume");

                    var newHalfSize = Vector3.Dot(newPos, direction);

                    sizeProp.floatValue = Mathf.Max(0f, newHalfSize) * 2f;
                    serializedObject.ApplyModifiedProperties();
                }
            }

            Handles.zTest = CompareFunction.Greater;
            Handles.color = s_OccludedColor;

            foreach (var direction in directions)
            {
                var pos = direction * halfSize;
                Handles.DotHandleCap(
                    0,
                    pos,
                    Quaternion.identity,
                    HandleSizeAt(pos),
                    EventType.Repaint
                );
            }
        }

        float HandleSizeAt(Vector3 pos) =>
            0.045f * HandleUtility.GetHandleSize(Handles.matrix.MultiplyPoint3x4(pos));

        #endregion

        void Bake(NavVolumeSpace space, SerializedProperty dataProp)
        {
            var bakedData = dataProp.objectReferenceValue as NavVolumeBakedData;

            if (bakedData == null)
            {
                Debug.LogError(
                    "[NavVolume][NavVolumeBakeEditor] Can't bake: \"BakedData\" asset is not assigned."
                );
                return;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var navCtx = new SVOBuilder(space.CurrentSettings).Build();
            bakedData.PopulateData(navCtx);

            EditorUtility.SetDirty(bakedData);
            AssetDatabase.SaveAssetIfDirty(bakedData);

            InvalidateNavContextCache(space);
            SceneView.RepaintAll();

            Debug.Log(
                $"[NavVolume][NavVolumeBakeEditor] NavVolume baked in {stopwatch.ElapsedMilliseconds} ms.\n"
                    + $"Stats: {new SVOStats(navCtx.Svo)}"
            );
        }
    }
}
