#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Provides functionality to toggle the active state of GameObjects directly from the Unity Hierarchy window.
/// </summary>
/// <remarks>
/// This class is initialized automatically when the Unity Editor loads.
/// Changing the toggle also marks the scene as dirty, enabling undo support.
/// </remarks>
[InitializeOnLoad]
public static class HierarchyToggle
{
    static HierarchyToggle()
    {
        EditorApplication.hierarchyWindowItemOnGUI += HandleHierarchyWindowItemOnGUI;
    }

    static void HandleHierarchyWindowItemOnGUI(int instanceId, Rect selectionRect)
    {
        var gameObject = EditorUtility.EntityIdToObject(instanceId) as GameObject;

        if (gameObject != null)
        {
            var rect = new Rect(selectionRect);
            rect.x -= 27;
            rect.width = 13;

            var isActive = EditorGUI.Toggle(rect, gameObject.activeSelf);

            if (isActive != gameObject.activeSelf)
            {
                Undo.RecordObject(gameObject, "Changing active state of a game object");
                gameObject.SetActive(isActive);
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
    }
}
#endif
