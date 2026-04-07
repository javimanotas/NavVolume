#if UNITY_EDITOR
using UnityEngine;

/// <summary>
/// Provides helper methods for determining the appropriate background color based on selection, hover, and window focus states.
/// </summary>
public static class BackgroundColorHelper
{
    static readonly Color Default = new(0.2196f, 0.2196f, 0.2196f);

    static readonly Color Selected = new(0.1725f, 0.3647f, 0.5294f);

    static readonly Color SelectedUnfocused = new(0.3f, 0.3f, 0.3f);

    static readonly Color Hovered = new(0.2706f, 0.2706f, 0.2706f);

    public static Color Get(bool isSelected, bool isHovered, bool isWindowFocused)
    {
        if (isSelected)
        {
            if (isWindowFocused)
            {
                return Selected;
            }

            return SelectedUnfocused;
        }

        if (isHovered)
        {
            return Hovered;
        }

        return Default;
    }
}
#endif
