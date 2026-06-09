using System.Collections.Generic;
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
            var rootLayer = svo.Layers.Length - 1;
            var rootCount = svo.Layers[rootLayer].Length;

            var drawn = 0;

            for (var i = 0; i < rootCount; i++)
            {
                DrawNodeRecursive(navCtx, rootLayer, i, camPos, ref drawn);

                if (drawn >= _SVO_GIZMO_DRAW_BUDGET)
                {
                    break;
                }
            }
        }

        static void DrawNodeRecursive(
            NavContext navCtx,
            int layer,
            int offset,
            Vector3 camPos,
            ref int drawn
        )
        {
            if (drawn >= _SVO_GIZMO_DRAW_BUDGET)
            {
                return;
            }

            var svo = navCtx.Svo;
            if (layer >= svo.Layers.Length)
            {
                return;
            }

            var layerList = svo.Layers[layer];
            if (offset >= layerList.Length)
            {
                return;
            }

            var node = layerList[offset];
            var bounds = navCtx.NodeBounds(layer, node.MortonCode);

            var nodeSize = navCtx.BuildSettings.NodeSizeForLayer(layer);
            var cullRadius = _SVO_GIZMO_LAYER_RADIUS_FACTOR * nodeSize;

            if (Vector3.Distance(bounds.center, camPos) > cullRadius + bounds.extents.magnitude)
            {
                return;
            }

            var isRoot = layer == svo.Layers.Length - 1;

            if (layer == 0)
            {
                DrawLeafNode(navCtx, offset, bounds, ref drawn);
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
            for (var c = 0; c < 8; c++)
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

        // Repaint live while playing so build timings captured by a runtime build (BuildOnAwake /
        // Manual) appear in the Stats foldout without needing to reselect the object.
        public override bool RequiresConstantRepaint() => Application.isPlaying;

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
                    EditorGUILayout.HelpBox(NoSvoMessage(space.BuildMode), MessageType.Info);
                }
                else
                {
                    DrawStatsRows(new SVOStats(navCtx));
                    DrawBuildTimeSection(space);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>
        /// Help-box text for the Stats foldout when no SVO exists yet. The instruction on how to get
        /// one depends on how the volume is configured to build.
        /// </summary>
        static string NoSvoMessage(BuildMode mode)
        {
            const string lead = "No SVO available. ";

            return mode switch
            {
                BuildMode.Baked => lead + "Bake the volume first to see its stats.",
                BuildMode.BuildOnAwake => lead
                    + "Enter Play Mode to build the volume and see its stats.",
                BuildMode.Manual => lead
                    + "Enter Play Mode, then call Build() or press the Rebuild button to see its stats.",
                _ => lead + "Build the volume to see its stats.",
            };
        }

        /// <summary>
        /// Per-build timing breakdown, shown in the same foldout as the structural stats for every
        /// build mode. The timing report is transient (see <see cref="NavVolumeSpace.LastBuildReport"/>),
        /// so when none is available yet a short hint explains how to produce one.
        /// </summary>
        static void DrawBuildTimeSection(NavVolumeSpace space)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Build Time", EditorStyles.boldLabel);

            if (space.LastBuildReport == null)
            {
                EditorGUILayout.HelpBox(NoTimingMessage(space.BuildMode), MessageType.Info);
                return;
            }

            BakeStatsView.Draw(space.LastBuildReport);
        }

        /// <summary>
        /// Hint for the Build Time section when no timing report exists yet. Timings are transient and
        /// per-run, so an SVO can be present (e.g. loaded from a baked asset) without one.
        /// </summary>
        static string NoTimingMessage(BuildMode mode)
        {
            const string lead = "No build timing captured this session. ";

            return mode switch
            {
                BuildMode.Baked => lead
                    + "Timings aren't saved with the asset; bake again to capture them.",
                BuildMode.BuildOnAwake => lead + "Enter Play Mode to build the volume.",
                BuildMode.Manual => lead
                    + "Call Build() or press Rebuild in Play Mode to capture them.",
                _ => lead + "Build the volume to capture them.",
            };
        }

        static void DrawStatsRows(SVOStats stats)
        {
            var savedPct =
                stats.TheoreticalVoxelsCount == 0
                    ? 0f
                    : (1f - (float)stats.VoxelsCount / stats.TheoreticalVoxelsCount) * 100f;
            var memoryKB = stats.MemoryUsedBytes / 1024f;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(2);
                EditorGuiHelpers.DrawHeadlineRow("Voxel Size", $"{stats.VoxelSize:F3} m");
                EditorGuiHelpers.DrawHeadlineSeparator();
                EditorGuiHelpers.DrawHeadlineRow(
                    "Allocated Voxels",
                    $"{stats.VoxelsCount:N0} / {stats.TheoreticalVoxelsCount:N0}  ({savedPct:F2}% saved)"
                );
                EditorGuiHelpers.DrawHeadlineSeparator();
                EditorGuiHelpers.DrawHeadlineRow("Memory (approx.)", $"{memoryKB:N1} KB");
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Nodes per Layer (log scale)", EditorStyles.boldLabel);
            DrawNodesPerLayerChart(stats.NodesPerLayer);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Sparse Savings per Layer", EditorStyles.boldLabel);
            DrawSavingsHorizontalListHeatmap(stats.NodesPerLayer, stats.TheoreticalNodesPerLayer);
        }

        static readonly Color s_ChartBarColor = new(0.40f, 0.75f, 0.95f, 1f);
        static readonly Color s_ChartBgColor = new(0.18f, 0.18f, 0.18f, 1f);

        static void DrawNodesPerLayerChart(int[] nodesPerLayer)
        {
            if (nodesPerLayer == null || nodesPerLayer.Length == 0)
            {
                return;
            }

            var maxSparse = 0;
            foreach (var c in nodesPerLayer)
            {
                if (c > maxSparse)
                {
                    maxSparse = c;
                }
            }

            var logMax = Mathf.Log10(maxSparse + 1f);
            if (logMax <= 0f)
            {
                EditorGUILayout.LabelField("(no nodes)");
                return;
            }

            const float _CHART_HEIGHT = 80f;
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
                var sparse = nodesPerLayer[i];

                // log10(x + 1) keeps 0 => 0 and stays monotonic.
                var sparseRatio = Mathf.Log10(sparse + 1f) / logMax;
                var sparseHeight = sparseRatio * (barsRect.height - 2f);

                var barX = barsRect.x + i * (barWidth + _BAR_SPACING);

                var sparseBar = new Rect(
                    barX,
                    barsRect.yMax - sparseHeight,
                    barWidth,
                    sparseHeight
                );
                EditorGUI.DrawRect(sparseBar, s_ChartBarColor);

                var topLabel = new Rect(barX, labelTop.y, barWidth, _LABEL_HEIGHT);
                var bottomLabel = new Rect(barX, labelBottom.y, barWidth, _LABEL_HEIGHT);

                EditorGUI.LabelField(topLabel, sparse.ToString(), topStyle);
                EditorGUI.LabelField(bottomLabel, LayerName(i, n), bottomStyle);
            }
        }

        static float SavingsFraction(int sparse, long dense) =>
            dense > 0 ? 1f - Mathf.Clamp01((float)sparse / dense) : 0f;

        /// <summary>
        /// Horizontal progress-bar list where each bar is a single solid color picked from a red, yellow, green heatmap based on the savings value.
        /// </summary>
        static void DrawSavingsHorizontalListHeatmap(
            int[] nodesPerLayer,
            long[] theoreticalPerLayer
        )
        {
            if (nodesPerLayer == null || nodesPerLayer.Length == 0)
            {
                return;
            }

            const float _ROW_HEIGHT = 18f;
            const float _LABEL_WIDTH = 50f;
            const float _PCT_WIDTH = 56f;
            const float _GAP = 4f;

            var n = nodesPerLayer.Length;
            var labelStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleLeft,
            };
            var pctStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
            };

            for (var i = 0; i < n; i++)
            {
                var rowRect = EditorGUILayout.GetControlRect(false, _ROW_HEIGHT);

                var labelRect = new Rect(rowRect.x, rowRect.y, _LABEL_WIDTH, rowRect.height);
                var barRect = new Rect(
                    labelRect.xMax + _GAP,
                    rowRect.y + 2f,
                    rowRect.width - _LABEL_WIDTH - _PCT_WIDTH - _GAP * 2,
                    rowRect.height - 4f
                );
                var pctRect = new Rect(barRect.xMax + _GAP, rowRect.y, _PCT_WIDTH, rowRect.height);

                var savings = SavingsFraction(nodesPerLayer[i], theoreticalPerLayer[i]);

                EditorGUI.LabelField(labelRect, LayerName(i, n), labelStyle);
                EditorGUI.DrawRect(barRect, s_ChartBgColor);
                var fill = new Rect(barRect.x, barRect.y, barRect.width * savings, barRect.height);
                EditorGUI.DrawRect(fill, EditorGuiHelpers.HeatmapColor(savings));
                EditorGUI.LabelField(pctRect, $"{savings * 100f:F2}%", pctStyle);
            }
        }

        static string LayerName(int chartIndex, int total)
        {
            if (chartIndex == 0)
            {
                return "root";
            }

            var layer = total - 1 - chartIndex;
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
                }

                EditorGUILayout.Space(4);

                DrawBuildButton(space, mode, dataProp);

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        /// <summary>
        /// Single bake/rebuild button. In <see cref="BuildMode.Baked"/> it bakes the asset while
        /// editing and live-rebuilds the in-memory volume while playing. In the other modes there is
        /// nothing to bake, so it only offers a live rebuild, which is possible only in Play Mode and
        /// is shown disabled otherwise.
        /// </summary>
        void DrawBuildButton(NavVolumeSpace space, BuildMode mode, SerializedProperty dataProp)
        {
            var isPlaying = Application.isPlaying;
            var canBake = mode == BuildMode.Baked && !isPlaying;

            var label =
                canBake ? "Bake"
                : isPlaying ? "Rebuild"
                : "Rebuild (Play Mode)";

            using (new EditorGUI.DisabledScope(!canBake && !isPlaying))
            {
                if (GUILayout.Button(label, GUILayout.Height(28)))
                {
                    if (canBake)
                    {
                        Bake(space, dataProp);
                    }
                    else
                    {
                        space.Build();
                        InvalidateNavContextCache(space);
                        SceneView.RepaintAll();
                    }
                }
            }
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

            // Shared profiler: the builder records its phases, then we append the post-build
            // (serialize + disk save) phases so the whole bake is reported as one unified log.
            var profiler = new BakeProfiler();

            // Cancelable progress bar. The reporter throws on cancel so the in-flight bake unwinds
            // at the next phase boundary without writing a partial asset.
            static void Report(string phase, float fraction)
            {
                if (EditorUtility.DisplayCancelableProgressBar("Baking NavVolume", phase, fraction))
                {
                    throw new System.OperationCanceledException();
                }
            }

            try
            {
                var navCtx = new SVOBuilder(space.CurrentSettings).Build(profiler, Report);

                Report("Serializing data", 0.90f);
                bakedData.PopulateData(navCtx);
                profiler.Lap("PopulateData");

                Report("Saving asset", 0.95f);
                EditorUtility.SetDirty(bakedData);
                AssetDatabase.SaveAssetIfDirty(bakedData);
                profiler.Lap("SaveAsset");

                InvalidateNavContextCache(space);
                SceneView.RepaintAll();

                space.LastBuildReport = profiler.ToReport();
            }
            catch (System.OperationCanceledException)
            {
                Debug.Log("[NavVolume][NavVolumeBakeEditor] Bake cancelled.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }
    }
}
