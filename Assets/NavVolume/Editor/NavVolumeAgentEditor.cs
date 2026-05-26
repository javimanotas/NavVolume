using System.Collections.Generic;
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

        [DrawGizmo(GizmoType.Selected | GizmoType.InSelectionHierarchy)]
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
