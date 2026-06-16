using System;
using NavVolume.Runtime;
using NavVolume.Runtime.Builder;
using UnityEditor;
using UnityEngine;

namespace NavVolume.Editor
{
    /// <summary>
    /// Inline drawer for a build's timing breakdown.
    /// </summary>
    internal static class BakeStatsView
    {
        static readonly Color s_BarBg = new(0.16f, 0.16f, 0.16f, 1f);
        static readonly Color s_SegmentSeparator = new(0.10f, 0.10f, 0.10f, 1f);
        static readonly Color s_RowHighlight = new(1f, 1f, 1f, 0.06f);

        /// <summary>
        /// Draws the timing breakdown for <paramref name="report"/>.
        /// </summary>
        public static void Draw(BakeReport report)
        {
            if (report == null || report.Phases.Count == 0)
            {
                return;
            }

            var ordered = new TimedPhase[report.Phases.Count];
            for (var i = 0; i < ordered.Length; i++)
            {
                ordered[i] = report.Phases[i];
            }
            Array.Sort(ordered, (a, b) => b.Milliseconds.CompareTo(a.Milliseconds));

            DrawHeader(report);
            EditorGUILayout.Space(6);

            var hovered = DrawTimelineBar(report, ordered);
            EditorGUILayout.Space(2);
            DrawCaption(report, ordered, hovered);
            EditorGUILayout.Space(8);
            DrawLegend(report, ordered, hovered);
        }

        static void DrawHeader(BakeReport report)
        {
            EditorGuiHelpers.DrawHeadlineRow("Total", $"{report.TotalMs:F1} ms");

            if (report.SaveMs > 0)
            {
                EditorGuiHelpers.DrawHeadlineRow(
                    "Build / Save",
                    $"{report.BuildMs:F1} / {report.SaveMs:F1} ms"
                );
            }
        }

        /// <summary>
        /// Draws the stacked bar and returns the hovered segment index, or -1.
        /// </summary>
        static int DrawTimelineBar(BakeReport report, TimedPhase[] ordered)
        {
            const float _BAR_HEIGHT = 26f;

            var rect = EditorGUILayout.GetControlRect(false, _BAR_HEIGHT);
            EditorGUI.DrawRect(rect, s_BarBg);

            if (report.TotalMs <= 0)
            {
                return -1;
            }

            var mouse = Event.current.mousePosition;
            var hovered = -1;
            var x = rect.x;

            for (var i = 0; i < ordered.Length; i++)
            {
                var frac = (float)(ordered[i].Milliseconds / report.TotalMs);
                var width = frac * rect.width;
                if (width <= 0f)
                {
                    continue;
                }

                var segment = new Rect(x, rect.y, width, rect.height);
                EditorGUI.DrawRect(segment, PhaseColor(i, ordered.Length));

                if (width > 1.5f)
                {
                    EditorGUI.DrawRect(
                        new Rect(segment.xMax - 1f, segment.y, 1f, segment.height),
                        s_SegmentSeparator
                    );
                }

                if (segment.Contains(mouse))
                {
                    hovered = i;
                    EditorGUI.DrawRect(
                        new Rect(segment.x, segment.y, segment.width, 2f),
                        Color.white
                    );
                }

                x += width;
            }

            return hovered;
        }

        static void DrawCaption(BakeReport report, TimedPhase[] ordered, int hovered)
        {
            var caption =
                hovered >= 0
                    ? $"{ordered[hovered].Label}  —  {ordered[hovered].Milliseconds:F1} ms  ({Percent(report, ordered, hovered):F1}%)"
                    : $"Hover a segment for details  ·  {ordered.Length} phases";

            EditorGUILayout.LabelField(caption, EditorStyles.miniLabel);
        }

        static void DrawLegend(BakeReport report, TimedPhase[] ordered, int hovered)
        {
            const float _ROW_HEIGHT = 18f;
            const float _SWATCH = 11f;
            const float _MS_WIDTH = 84f;
            const float _PCT_WIDTH = 60f;

            var msStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            var pctStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
            };

            for (var i = 0; i < ordered.Length; i++)
            {
                var row = EditorGUILayout.GetControlRect(false, _ROW_HEIGHT);

                if (i == hovered)
                {
                    EditorGUI.DrawRect(row, s_RowHighlight);
                }

                var swatch = new Rect(
                    row.x + 2f,
                    row.y + (row.height - _SWATCH) * 0.5f,
                    _SWATCH,
                    _SWATCH
                );
                EditorGUI.DrawRect(swatch, PhaseColor(i, ordered.Length));

                var nameRect = new Rect(
                    swatch.xMax + 8f,
                    row.y,
                    row.width - _SWATCH - _MS_WIDTH - _PCT_WIDTH - 18f,
                    row.height
                );
                var msRect = new Rect(
                    row.xMax - _MS_WIDTH - _PCT_WIDTH,
                    row.y,
                    _MS_WIDTH,
                    row.height
                );
                var pctRect = new Rect(row.xMax - _PCT_WIDTH, row.y, _PCT_WIDTH, row.height);

                var nameStyle = i == hovered ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUI.LabelField(nameRect, ordered[i].Label, nameStyle);
                EditorGUI.LabelField(msRect, $"{ordered[i].Milliseconds:F1} ms", msStyle);
                EditorGUI.LabelField(pctRect, $"{Percent(report, ordered, i):F1}%", pctStyle);
            }
        }

        static double Percent(BakeReport report, TimedPhase[] ordered, int index) =>
            report.TotalMs > 0 ? ordered[index].Milliseconds / report.TotalMs * 100.0 : 0.0;

        static Color PhaseColor(int orderedIndex, int count)
        {
            var n = Mathf.Max(1, count);
            return Color.HSVToRGB((float)orderedIndex / n, 0.55f, 0.92f);
        }
    }
}
