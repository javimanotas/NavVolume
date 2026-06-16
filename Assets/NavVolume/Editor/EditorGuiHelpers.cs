using UnityEditor;
using UnityEngine;

namespace NavVolume.Editor
{
    /// <summary>
    /// Shared editor-GUI primitives used across NavVolume custom inspectors.
    /// </summary>
    internal static class EditorGuiHelpers
    {
        static readonly Color s_HeadlineSeparatorColor = new(1f, 1f, 1f, 0.08f);
        static readonly Color s_GradientLowColor = new(0.90f, 0.30f, 0.30f, 1f);
        static readonly Color s_GradientMidColor = new(0.95f, 0.82f, 0.30f, 1f);
        static readonly Color s_GradientHighColor = new(0.40f, 0.85f, 0.45f, 1f);
        static readonly Color s_BarBgColor = new(0.18f, 0.18f, 0.18f, 1f);

        public static Color StatusSuccessColor => s_GradientHighColor;
        public static Color StatusFailureColor => s_GradientLowColor;

        static GUIStyle s_HeadlineKeyStyle;
        public static GUIStyle HeadlineKeyStyle =>
            s_HeadlineKeyStyle ??= new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
            };

        static GUIStyle s_HeadlineValueStyle;
        public static GUIStyle HeadlineValueStyle =>
            s_HeadlineValueStyle ??= new GUIStyle(EditorStyles.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                alignment = TextAnchor.MiddleRight,
            };

        /// <summary>
        /// Red, yellow, green color for a fraction in [0, 1] using a three-stop interpolation.
        /// </summary>
        public static Color HeatmapColor(float t) =>
            t < 0.5f
                ? Color.Lerp(s_GradientLowColor, s_GradientMidColor, t * 2f)
                : Color.Lerp(s_GradientMidColor, s_GradientHighColor, (t - 0.5f) * 2f);

        public static void DrawHeadlineRow(string key, string value, Color? valueTint = null)
        {
            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(22f)))
            {
                EditorGUILayout.LabelField(
                    key,
                    HeadlineKeyStyle,
                    GUILayout.Width(EditorGUIUtility.labelWidth)
                );

                if (valueTint.HasValue)
                {
                    var tinted = new GUIStyle(HeadlineValueStyle);
                    tinted.normal.textColor = valueTint.Value;
                    EditorGUILayout.LabelField(value, tinted);
                }
                else
                {
                    EditorGUILayout.LabelField(value, HeadlineValueStyle);
                }

                GUILayout.Space(10f);
            }
        }

        public static void DrawHeadlineSeparator()
        {
            EditorGUILayout.Space(2);
            var rect = EditorGUILayout.GetControlRect(false, 1f);
            EditorGUI.DrawRect(rect, s_HeadlineSeparatorColor);
            EditorGUILayout.Space(2);
        }

        /// <summary>
        /// Draws a horizontal heatmap bar where the fill width and color both encode <paramref name="fillFraction"/>.
        /// </summary>
        public static void DrawHeatmapBar(Rect rect, float fillFraction)
        {
            EditorGUI.DrawRect(rect, s_BarBgColor);
            var t = Mathf.Clamp01(fillFraction);
            var fill = new Rect(rect.x, rect.y, rect.width * t, rect.height);
            EditorGUI.DrawRect(fill, HeatmapColor(t));
        }

        public const string DocumentationUrl = "https://github.com/javimanotas/NavVolume";

        /// <summary>
        /// Draws a right-aligned help button that opens the NavVolume documentation.
        /// </summary>
        public static void DrawHelpRow(Object target)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var niceName = ObjectNames.NicifyVariableName(target.GetType().Name);
                var content = new GUIContent(EditorGUIUtility.IconContent("_Help"))
                {
                    tooltip = $"Open the {niceName} documentation.",
                };

                if (GUILayout.Button(content, EditorStyles.iconButton))
                {
                    Application.OpenURL(DocumentationUrl);
                }
            }
        }
    }
}
