#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;

/// <summary>
/// Provides functionality to display custom icons for GameObjects in the Unity Hierarchy window during editor time.
/// </summary>
/// <remarks>
/// This static class hooks into Unity Editor events to render component icons next to GameObjects in the Hierarchy window.
/// It is initialized automatically on editor load and does not require manual instantiation.
/// The display adapts based on selection and focus state to enhance visual feedback for users working in the Unity Editor.
/// </remarks>
[InitializeOnLoad]
public static class HierarchyIconDisplay
{
    static bool _hierarchyHasFocus = false;

    static EditorWindow _hierarchyWindow;

    static HierarchyIconDisplay()
    {
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyWindowItemOnGUI;
        EditorApplication.update += OnUpdate;
    }

    /// <summary>
    /// Attempts to retrieve a representative component from the specified GameObject.
    /// </summary>
    /// <remarks>
    /// The method prioritizes non-transform components and skips CanvasRenderer if present.
    /// </remarks>
    /// <param name="gameObject">
    /// The GameObject from which to select a representative component.
    /// </param>
    /// <param name="component">
    /// When this method returns, contains the representative component found, or null if no suitable component exists.
    /// </param>
    /// <returns>
    /// true if a representative component was found, otherwise false.
    /// </returns>
    static bool GetRepresentativeComponent(GameObject gameObject, out Component component)
    {
        if (gameObject == null)
        {
            component = null;
            return false;
        }

        var components = gameObject.GetComponents<Component>();

        if (components == null || components.Length == 0)
        {
            component = null;
            return false;
        }

        if (components.Length == 1)
        {
            component = components[0];
        }
        else
        {
            // Avoid getting the Transform or RectTransform components.
            component = components[1];

            if (component is CanvasRenderer && components.Length > 2)
            {
                component = components[2];
            }
        }

        return true;
    }

    static void OnHierarchyWindowItemOnGUI(int instanceId, Rect selectionRect)
    {
        var gameObject = EditorUtility.EntityIdToObject(instanceId) as GameObject;

        if (!GetRepresentativeComponent(gameObject, out var component))
        {
            return;
        }

        var type = component.GetType();
        var content = EditorGUIUtility.ObjectContent(component, type);
        content.text = null;
        content.tooltip = type.Name;

        if (content.image == null)
        {
            return;
        }

        var color = BackgroundColorHelper.Get(
            Selection.entityIds.Contains(instanceId),
            selectionRect.Contains(Event.current.mousePosition),
            _hierarchyHasFocus
        );

        var bgRect = selectionRect;
        bgRect.width = 18.5f;
        EditorGUI.DrawRect(bgRect, color);

        EditorGUI.LabelField(selectionRect, content);
    }

    static void OnUpdate()
    {
        if (_hierarchyWindow == null)
        {
            var type = Type.GetType("UnityEditor.SceneHierarchyWindow,UnityEditor");
            _hierarchyWindow = EditorWindow.GetWindow(type);
        }

        _hierarchyHasFocus =
            EditorWindow.focusedWindow != null && EditorWindow.focusedWindow == _hierarchyWindow;
    }
}
#endif
