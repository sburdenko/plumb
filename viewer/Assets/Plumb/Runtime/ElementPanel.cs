using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// The selected element: a ruled panel with an accent bar, the IFC type, the name, storey and GlobalId.
    /// </summary>
    public static class ElementPanel
    {
        private const float Width = 420f;
        private const float Height = 104f;
        private const float Margin = 16f;
        private const float Padding = 16f;
        private const float LabelWidth = 72f;
        private const string None = "—";

        /// <summary>Where the panel is drawn, in IMGUI coordinates (origin top left).</summary>
        public static Rect Area(float screenHeight) => new Rect(Margin, screenHeight - ViewerChrome.StatusBarHeight - Margin - Height, Width, Height);

        /// <param name="pointer">A position from <c>Input.mousePosition</c>, whose origin is bottom left.</param>
        public static bool Contains(Vector2 pointer, float screenHeight)
        {
            return Area(screenHeight).Contains(new Vector2(pointer.x, screenHeight - pointer.y));
        }

        public static void Draw(ElementInfo element)
        {
            var area = Area(Screen.height);
            Graphite.Fill(area, Graphite.Panel);
            Graphite.Rules(area, Graphite.Rule, 2, 2, 2, 2);
            Graphite.Fill(new Rect(area.x, area.y, 3, area.height), Graphite.Accent);

            var inner = new Rect(area.x + Padding, area.y + 12, area.width - 2 * Padding, area.height - 24);
            GUILayout.BeginArea(inner);
            GUILayout.Label(element.Type.ToUpperInvariant(), ViewerStyles.Kicker);
            GUILayout.Space(4);
            GUILayout.Label(element.Name ?? element.Type, ViewerStyles.Title);
            GUILayout.Space(8);
            Row("Storey", element.Storey);
            Row("GlobalId", element.Id);
            GUILayout.EndArea();
        }

        private static void Row(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, ViewerStyles.Key, GUILayout.Width(LabelWidth));
            GUILayout.Label(string.IsNullOrEmpty(value) ? None : value, ViewerStyles.Value);
            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }
    }
}
