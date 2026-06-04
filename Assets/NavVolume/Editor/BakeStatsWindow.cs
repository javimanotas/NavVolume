using System;
using NavVolume.Runtime.Builder;
using UnityEditor;
using UnityEngine;

namespace NavVolume.Editor
{
    /// <summary>
    /// Floating popup that visualizes the timing breakdown of the most recent bake as a stacked
    /// timeline bar plus a ranked legend.
    /// </summary>
    /// <remarks>
    /// Purely transient: the report lives in memory only (and is cleared on domain reload). It is
    /// never written to the asset or the component, so no timing data is ever saved.
    /// </remarks>
    internal sealed class BakeStatsWindow : EditorWindow
    {
        // Kept static so the popup can be reopened during a session without re-baking. This is still
        // in-memory only and does not survive a domain reload.
        static BakeReport s_Last;

        [NonSerialized]
        BakeReport _report;

        [NonSerialized]
        BakePhase[] _ordered; // phases sorted by cost, descending (drives both bar and legend)

        int _hovered = -1;

        static readonly Color s_BarBg = new(0.16f, 0.16f, 0.16f, 1f);
        static readonly Color s_SegmentSeparator = new(0.10f, 0.10f, 0.10f, 1f);
        static readonly Color s_RowHighlight = new(1f, 1f, 1f, 0.06f);

        public static bool HasReport => s_Last != null;

        public static void Show(BakeReport report)
        {
            s_Last = report;

            var window = GetWindow<BakeStatsWindow>(
                utility: true,
                title: "NavVolume Bake Stats",
                focus: true
            );
            window.minSize = new Vector2(480, 420);
            window.SetReport(report);
            window.Repaint();
        }

        public static void ShowLast()
        {
            if (s_Last != null)
            {
                Show(s_Last);
            }
        }

        void OnEnable()
        {
            wantsMouseMove = true;
            if (_report == null)
            {
                SetReport(s_Last);
            }
        }

        void SetReport(BakeReport report)
        {
            _report = report;
            _hovered = -1;

            if (report == null)
            {
                _ordered = Array.Empty<BakePhase>();
                return;
            }

            _ordered = new BakePhase[report.Phases.Count];
            for (var i = 0; i < report.Phases.Count; i++)
            {
                _ordered[i] = report.Phases[i];
            }
            Array.Sort(_ordered, (a, b) => b.Milliseconds.CompareTo(a.Milliseconds));
        }

        void OnGUI()
        {
            if (_report == null)
            {
                EditorGUILayout.HelpBox(
                    "No bake stats yet. Bake a NavVolume to see its timing breakdown.",
                    MessageType.Info
                );
                return;
            }

            // Hover is recomputed from scratch each frame; the bar's hit-test (run before the
            // legend) sets it so the matching legend row can highlight in the same pass.
            _hovered = -1;

            EditorGUILayout.Space(8);
            DrawHeader();
            EditorGUILayout.Space(8);
            DrawTimelineBar();
            EditorGUILayout.Space(2);
            DrawCaption();
            EditorGUILayout.Space(10);
            DrawLegend();

            if (Event.current.type == EventType.MouseMove)
            {
                Repaint();
            }
        }

        void DrawHeader()
        {
            var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18 };
            EditorGUILayout.LabelField($"Bake: {_report.TotalMs:F1} ms", titleStyle);
            EditorGUILayout.LabelField(
                $"build {_report.BuildMs:F1} ms     ·     save {_report.SaveMs:F1} ms",
                EditorStyles.miniLabel
            );
        }

        void DrawTimelineBar()
        {
            const float _BAR_HEIGHT = 30f;

            var rect = EditorGUILayout.GetControlRect(false, _BAR_HEIGHT);
            EditorGUI.DrawRect(rect, s_BarBg);

            if (_report.TotalMs <= 0)
            {
                return;
            }

            var mouse = Event.current.mousePosition;
            var x = rect.x;

            for (var i = 0; i < _ordered.Length; i++)
            {
                var frac = (float)(_ordered[i].Milliseconds / _report.TotalMs);
                var width = frac * rect.width;
                if (width <= 0f)
                {
                    continue;
                }

                var segment = new Rect(x, rect.y, width, rect.height);
                EditorGUI.DrawRect(segment, PhaseColor(i));

                // Thin separator on the right edge to keep adjacent segments legible.
                if (width > 1.5f)
                {
                    EditorGUI.DrawRect(
                        new Rect(segment.xMax - 1f, segment.y, 1f, segment.height),
                        s_SegmentSeparator
                    );
                }

                if (segment.Contains(mouse))
                {
                    _hovered = i;
                    // White top accent to mark the hovered segment.
                    EditorGUI.DrawRect(
                        new Rect(segment.x, segment.y, segment.width, 2f),
                        Color.white
                    );
                }

                x += width;
            }
        }

        void DrawCaption()
        {
            var caption =
                _hovered >= 0
                    ? $"{_ordered[_hovered].Label}  —  {_ordered[_hovered].Milliseconds:F1} ms  ({Percent(_hovered):F1}%)"
                    : $"Hover a segment for details  ·  {_ordered.Length} phases";

            EditorGUILayout.LabelField(caption, EditorStyles.miniLabel);
        }

        void DrawLegend()
        {
            const float _ROW_HEIGHT = 19f;
            const float _SWATCH = 12f;
            const float _MS_WIDTH = 90f;
            const float _PCT_WIDTH = 70f;

            var msStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleRight };
            var pctStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleRight,
            };

            for (var i = 0; i < _ordered.Length; i++)
            {
                var row = EditorGUILayout.GetControlRect(false, _ROW_HEIGHT);

                if (i == _hovered)
                {
                    EditorGUI.DrawRect(row, s_RowHighlight);
                }

                var swatch = new Rect(
                    row.x + 2f,
                    row.y + (row.height - _SWATCH) * 0.5f,
                    _SWATCH,
                    _SWATCH
                );
                EditorGUI.DrawRect(swatch, PhaseColor(i));

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

                var nameStyle = i == _hovered ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUI.LabelField(nameRect, _ordered[i].Label, nameStyle);
                EditorGUI.LabelField(msRect, $"{_ordered[i].Milliseconds:F1} ms", msStyle);
                EditorGUI.LabelField(pctRect, $"{Percent(i):F1}%", pctStyle);
            }
        }

        double Percent(int orderedIndex) =>
            _report.TotalMs > 0
                ? _ordered[orderedIndex].Milliseconds / _report.TotalMs * 100.0
                : 0.0;

        Color PhaseColor(int orderedIndex)
        {
            var n = Mathf.Max(1, _ordered.Length);
            // Evenly spaced hues give distinct yet cohesive colors for any phase count.
            return Color.HSVToRGB((float)orderedIndex / n, 0.55f, 0.92f);
        }
    }
}
