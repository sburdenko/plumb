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

        public static void Draw(ElementInfo element)
        {
            var area = new Rect(16, Screen.height - Height - 44, Width, Height);
            GUILayout.BeginArea(area, ViewerStyles.Panel);
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
