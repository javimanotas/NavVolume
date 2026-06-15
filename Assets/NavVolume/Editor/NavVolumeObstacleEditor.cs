using UnityEditor;
using UnityEngine;

namespace NavVolume.Editor
{
    /// <summary>
    /// Inspector and gizmos for <see cref="NavVolumeObstacle"/>.
    /// Shows only the size field relevant to the selected shape.
    /// </summary>
    [CustomEditor(typeof(NavVolumeObstacle))]
    [CanEditMultipleObjects]
    public class NavVolumeObstacleEditor : UnityEditor.Editor
    {
        static readonly Color s_ObstacleColor = new(1.00f, 0.45f, 0.20f, 1.0f);

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var shape = serializedObject.FindProperty("_shape");
            EditorGUILayout.PropertyField(shape);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("_center"));

            if (!shape.hasMultipleDifferentValues)
            {
                var sizeProperty =
                    (ObstacleShape)shape.enumValueIndex == ObstacleShape.Sphere
                        ? "_radius"
                        : "_size";
                EditorGUILayout.PropertyField(serializedObject.FindProperty(sizeProperty));
            }

            serializedObject.ApplyModifiedProperties();
        }

        [DrawGizmo(GizmoType.NonSelected | GizmoType.Selected)]
        static void DrawObstacleGizmo(NavVolumeObstacle obstacle, GizmoType _)
        {
            Gizmos.color = s_ObstacleColor;

            var transform = obstacle.transform;
            var center = transform.TransformPoint(obstacle.Center);
            var scale = transform.lossyScale;
            var absScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));

            if (obstacle.Shape == ObstacleShape.Sphere)
            {
                var radius =
                    obstacle.Radius * Mathf.Max(absScale.x, Mathf.Max(absScale.y, absScale.z));
                Gizmos.DrawWireSphere(center, radius);
            }
            else
            {
                Gizmos.matrix = Matrix4x4.TRS(
                    center,
                    transform.rotation,
                    Vector3.Scale(obstacle.Size, absScale)
                );
                Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
                Gizmos.matrix = Matrix4x4.identity;
            }
        }
    }
}
