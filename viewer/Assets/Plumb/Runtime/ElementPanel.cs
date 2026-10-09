using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>The small panel with name, type, storey and GlobalId of the selected element.</summary>
    public static class ElementPanel
    {
        private const float Width = 420f;
        private const float Height = 132f;
        private const float LabelWidth = 72f;
        private const string None = "—";

        /// <summary>Where the panel is drawn, in IMGUI coordinates (origin top left).</summary>
        public static Rect Area(float screenHeight) => new Rect(16, screenHeight - Height - 44, Width, Height);

        /// <param name="pointer">A position from <c>Input.mousePosition</c>, whose origin is bottom left.</param>
        public static bool Contains(Vector2 pointer, float screenHeight)
        {
            return Area(screenHeight).Contains(new Vector2(pointer.x, screenHeight - pointer.y));
        }

        public static void Draw(ElementInfo element)
        {
            GUILayout.BeginArea(Area(Screen.height), ViewerStyles.Panel);
            GUILayout.Label(element.Name ?? element.Type, ViewerStyles.Title);
            Row("Type", element.Type);
            Row("Storey", element.Storey);
            Row("GlobalId", element.Id);
            GUILayout.EndArea();
        }

        private static void Row(string label, string value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, ViewerStyles.Field, GUILayout.Width(LabelWidth));
            GUILayout.Label(string.IsNullOrEmpty(value) ? None : value, ViewerStyles.Value);
            GUILayout.EndHorizontal();
        }
    }
}
