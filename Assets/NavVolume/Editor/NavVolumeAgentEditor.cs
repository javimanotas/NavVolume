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

        static bool s_LastPathStatsFoldout = true;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, s_HiddenProperties);
            DrawFreezeRotationRow();

            serializedObject.ApplyModifiedProperties();

            DrawLastPathStats();
        }

        void DrawLastPathStats()
        {
            EditorGUILayout.Space(4);

            s_LastPathStatsFoldout = EditorGUILayout.BeginFoldoutHeaderGroup(
                s_LastPathStatsFoldout,
                "Last Path Stats"
            );

            if (s_LastPathStatsFoldout)
            {
                EditorGUI.indentLevel++;

                var agent = (NavVolumeAgent)target;
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
                    var stats = lastPath.Value.Stats;
                    EditorGUILayout.LabelField("Status", lastPath.Value.Status.ToString());
                    EditorGUILayout.LabelField("Nodes expanded", stats.NodesExpanded.ToString("N0"));
                    EditorGUILayout.LabelField("Elapsed", $"{stats.ElapsedMs:F2} ms");

                    var pct =
                        stats.RawWaypointsCount > 0
                            ? 100f * stats.WaypointsRemovedByLOS / stats.RawWaypointsCount
                            : 0f;
                    EditorGUILayout.LabelField(
                        "Waypoints removed by LOS",
                        $"{stats.WaypointsRemovedByLOS:N0} / {stats.RawWaypointsCount:N0}  ({pct:F1}%)"
                    );
                }

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        void DrawFreezeRotationRow()
        {
            var x = serializedObject.FindProperty(s_FreezeRotationFields[0]);
            var y = serializedObject.FindProperty(s_FreezeRotationFields[1]);
            var z = serializedObject.FindProperty(s_FreezeRotationFields[2]);

            var props = new[] { x, y, z };

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label(
                    s_FreezeRotationLabel,
                    GUILayout.Width(EditorGUIUtility.labelWidth)
                );

                var toggleWidth = GUILayout.Width(30f);
                var labelWidth = GUILayout.Width(15f);

                for (var i = 0; i < props.Length; i++)
                {
                    GUILayout.Label(s_AxisLabels[i], labelWidth);
                    EditorGUI.BeginChangeCheck();
                    var value = EditorGUILayout.Toggle(props[i].boolValue, toggleWidth);
                    if (EditorGUI.EndChangeCheck())
                    {
                        props[i].boolValue = value;
                    }
                }

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
