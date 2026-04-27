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

        static bool s_BuildSettingsFoldout = true;
        static bool s_BuildModeFoldout = true;

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

            serializedObject.ApplyModifiedProperties();
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
                    "[NavVolume][NavVolumeSpace] Can't bake: \"BakedData\" asset is not assigned."
                );
                return;
            }

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var navCtx = new SVOBuilder(space.CurrentSettings).Build();
            bakedData.PopulateData(navCtx);

            EditorUtility.SetDirty(bakedData);
            AssetDatabase.SaveAssetIfDirty(bakedData);

            Debug.Log(
                $"[NavVolume][NavVolumeSpace] NavVolume baked in {stopwatch.ElapsedMilliseconds} ms.\n"
                    + $"Stats: {new SVOStats(navCtx.Svo)}"
            );
        }
    }
}
