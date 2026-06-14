using UnityEngine;

namespace Project.EditorPlugins
{
    /// <summary>
    /// Provides helper methods for determining the appropriate background color based on selection, hover, and window focus states.
    /// </summary>
    public static class BackgroundColorHelper
    {
        static readonly Color s_Default = new(0.2196f, 0.2196f, 0.2196f);

        static readonly Color s_Selected = new(0.1725f, 0.3647f, 0.5294f);

        static readonly Color s_SelectedUnfocused = new(0.3f, 0.3f, 0.3f);

        static readonly Color s_Hovered = new(0.2706f, 0.2706f, 0.2706f);

        public static Color Get(bool isSelected, bool isHovered, bool isWindowFocused)
        {
            if (isSelected)
            {
                if (isWindowFocused)
                {
                    return s_Selected;
                }

                return s_SelectedUnfocused;
            }

            if (isHovered)
            {
                return s_Hovered;
            }

            return s_Default;
        }
    }
}
