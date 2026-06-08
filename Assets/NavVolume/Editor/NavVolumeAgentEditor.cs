using System.Collections.Generic;
using NavVolume.Runtime.Pathfinding;
using UnityEditor;
using UnityEngine;

namespace NavVolume.Editor
{
    /// <summary>
    /// Draws path gizmos for a selected <see cref="NavVolumeAgent"/>.
    /// </summary>
    /// <remarks>
    /// Renders the raw A* waypoints alongside the smoothed/spline waypoints and highlights the next waypoint the agent is moving toward.
    /// </remarks>
    [CustomEditor(typeof(NavVolumeAgent))]
    public class NavVolumeAgentEditor : UnityEditor.Editor
    {
        static readonly Color s_RawColor = new(0.75f, 0.75f, 0.75f, 0.9f);
        static readonly Color s_SmoothedColor = new(0.30f, 0.85f, 1.00f, 1.0f);
        static readonly Color s_TargetColor = new(1.00f, 0.85f, 0.20f, 1.0f);

        const float _SMOOTHED_SPHERE_RADIUS = 0.12f;
        const float _RAW_SPHERE_RADIUS = 0.18f;
        const float _TARGET_SPHERE_RADIUS = 0.28f;

        static readonly string[] s_FreezeRotationFields =
        {
            "_freezeRotationX",
            "_freezeRotationY",
            "_freezeRotationZ",
        };

        static readonly string[] s_HiddenProperties =
        {
            "m_Script",
            "_freezeRotationX",
            "_freezeRotationY",
            "_freezeRotationZ",
        };

        static readonly GUIContent[] s_AxisLabels = { new("X"), new("Y"), new("Z") };

        static readonly GUIContent s_FreezeRotationLabel = new(
            "Freeze Rotation",
            "Locks rotation around the selected world axes. Locked axes stay at the agent's initial rotation."
        );

        static bool s_MovementSettingsFoldout = true;
        static bool s_LastPathStatsFoldout = true;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var agent = (NavVolumeAgent)target;

            DrawLiveStateLine(agent);
            DrawMovementSettings();

            serializedObject.ApplyModifiedProperties();

            DrawLastPathStats(agent);
        }

        void DrawLiveStateLine(NavVolumeAgent agent)
        {
            (string text, Color tint) = GetLiveStateLine(agent);

            var bgStyle = new GUIStyle(EditorStyles.helpBox);
            using (new EditorGUILayout.VerticalScope(bgStyle))
            {
                using (new EditorGUILayout.HorizontalScope(GUILayout.Height(20f)))
                {
                    var dot = GUILayoutUtility.GetRect(
                        10f,
                        10f,
                        GUILayout.Width(10f),
                        GUILayout.Height(20f)
                    );
                    var dotRect = new Rect(dot.x, dot.y + dot.height * 0.5f - 4f, 8f, 8f);
                    EditorGUI.DrawRect(dotRect, tint);

                    GUILayout.Space(4f);

                    var style = new GUIStyle(EditorStyles.label)
                    {
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleLeft,
                    };
                    EditorGUILayout.LabelField(text, style);
                }
            }

            EditorGUILayout.Space(2);
        }

        static (string text, Color tint) GetLiveStateLine(NavVolumeAgent agent)
        {
            var lastPath = agent.LastPath;

            if (!lastPath.HasValue)
            {
                return ("Idle (no path computed)", new Color(0.65f, 0.65f, 0.65f));
            }

            if (lastPath.Value.Status != PathResultStatus.Sucess)
            {
                return (
                    $"Path failed: {lastPath.Value.Status}",
                    EditorGuiHelpers.StatusFailureColor
                );
            }

            if (agent.HasActivePath)
            {
                var idx = agent.CurrentWaypointIndex + 1;
                var total = agent.SmoothedWaypoints.Count;
                return ($"Moving (waypoint {idx} of {total})", EditorGuiHelpers.StatusSuccessColor);
            }

            return ("Idle (reached goal)", EditorGuiHelpers.StatusSuccessColor);
        }

        void DrawMovementSettings()
        {
            s_MovementSettingsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                s_MovementSettingsFoldout,
                "Movement Settings"
            );

            if (s_MovementSettingsFoldout)
            {
                EditorGUI.indentLevel++;
                DrawPropertiesExcluding(serializedObject, s_HiddenProperties);
                DrawFreezeRotationRow();
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawLastPathStats(NavVolumeAgent agent)
        {
            EditorGUILayout.Space(4);

            s_LastPathStatsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                s_LastPathStatsFoldout,
                "Last Path Stats"
            );

            if (s_LastPathStatsFoldout)
            {
                EditorGUI.indentLevel++;

                var lastPath = agent.LastPath;

                if (!lastPath.HasValue)
                {
                    EditorGUILayout.HelpBox(
                        "No path computed yet. Call MoveTo to populate stats.",
                        MessageType.Info
                    );
                }
                else
                {
                    DrawLastPathStatsBox(lastPath.Value);
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        static void DrawLastPathStatsBox(PathResult result)
        {
            var stats = result.Stats;
            var statusTint =
                result.Status == PathResultStatus.Sucess
                    ? EditorGuiHelpers.StatusSuccessColor
                    : EditorGuiHelpers.StatusFailureColor;

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.Space(2);
                EditorGuiHelpers.DrawHeadlineRow("Status", result.Status.ToString(), statusTint);
                EditorGuiHelpers.DrawHeadlineSeparator();
                EditorGuiHelpers.DrawHeadlineRow(
                    "Nodes expanded",
                    stats.NodesExpanded.ToString("N0")
                );
                EditorGuiHelpers.DrawHeadlineSeparator();
                EditorGuiHelpers.DrawHeadlineRow("Elapsed", $"{stats.ElapsedMs:F2} ms");

                foreach (var phase in stats.Phases)
                {
                    EditorGuiHelpers.DrawHeadlineRow(
                        $"     {phase.Label}",
                        $"{phase.Milliseconds:F2} ms"
                    );
                }

                EditorGuiHelpers.DrawHeadlineSeparator();

                var pct =
                    stats.RawWaypointsCount > 0
                        ? (float)stats.WaypointsRemovedByLOS / stats.RawWaypointsCount
                        : 0f;
                EditorGuiHelpers.DrawHeadlineRow(
                    "Waypoints removed by LOS",
                    $"{stats.WaypointsRemovedByLOS:N0} / {stats.RawWaypointsCount:N0}  ({pct * 100f:F1}%)"
                );

                // Visual bar showing the LOS-removed fraction on the same red, yellow, green palette.
                var barRow = EditorGUILayout.GetControlRect(false, 8f);
                var barRect = new Rect(
                    barRow.x + EditorGUIUtility.labelWidth,
                    barRow.y,
                    barRow.width - EditorGUIUtility.labelWidth - 10f,
                    barRow.height
                );
                EditorGuiHelpers.DrawHeatmapBar(barRect, pct);

                EditorGUILayout.Space(2);
            }
        }

        void DrawFreezeRotationRow()
        {
            var x = serializedObject.FindProperty(s_FreezeRotationFields[0]);
            var y = serializedObject.FindProperty(s_FreezeRotationFields[1]);
            var z = serializedObject.FindProperty(s_FreezeRotationFields[2]);

            var props = new[] { x, y, z };

            using (new EditorGUILayout.HorizontalScope())
            {
                // Match indentation of sibling property labels rendered by PropertyField.
                var indentPx = EditorGUI.indentLevel * 15f;
                GUILayout.Space(indentPx);
                GUILayout.Label(
                    s_FreezeRotationLabel,
                    GUILayout.Width(EditorGUIUtility.labelWidth - indentPx)
                );

                // Reset indent locally — EditorGUILayout.Toggle re-applies indentLevel,
                // which pushes the checkbox 15 px away from its axis label otherwise.
                var savedIndent = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;

                for (var i = 0; i < props.Length; i++)
                {
                    GUILayout.Label(s_AxisLabels[i], GUILayout.Width(12f));
                    EditorGUI.BeginChangeCheck();
                    var value = EditorGUILayout.Toggle(props[i].boolValue, GUILayout.Width(14f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        props[i].boolValue = value;
                    }
                    GUILayout.Space(16f);
                }

                EditorGUI.indentLevel = savedIndent;

                GUILayout.FlexibleSpace();
            }
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawPathGizmo(NavVolumeAgent agent, GizmoType _)
        {
            DrawWaypoints(agent.RawWaypoints, s_RawColor, _RAW_SPHERE_RADIUS, wireSphere: true);
            DrawWaypoints(
                agent.SmoothedWaypoints,
                s_SmoothedColor,
                _SMOOTHED_SPHERE_RADIUS,
                wireSphere: false
            );

            if (!agent.HasActivePath)
            {
                return;
            }

            var idx = Mathf.Clamp(agent.CurrentWaypointIndex, 0, agent.SmoothedWaypoints.Count - 1);

            Gizmos.color = s_TargetColor;
            Gizmos.DrawSphere(agent.SmoothedWaypoints[idx], _TARGET_SPHERE_RADIUS);
        }

        static void DrawWaypoints(
            IReadOnlyList<Vector3> waypoints,
            Color color,
            float sphereRadius,
            bool wireSphere
        )
        {
            if (waypoints == null || waypoints.Count == 0)
            {
                return;
            }

            Gizmos.color = color;

            for (var i = 0; i < waypoints.Count; i++)
            {
                if (wireSphere)
                {
                    Gizmos.DrawWireSphere(waypoints[i], sphereRadius);
                }
                else
                {
                    Gizmos.DrawSphere(waypoints[i], sphereRadius);
                }

                if (i > 0)
                {
                    Gizmos.DrawLine(waypoints[i - 1], waypoints[i]);
                }
            }
        }
    }
}
