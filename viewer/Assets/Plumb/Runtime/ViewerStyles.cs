using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>IMGUI styles, created lazily because GUI.skin is only available inside OnGUI.</summary>
    public static class ViewerStyles
    {
        private static GUIStyle _message;
        private static GUIStyle _hint;
        private static GUIStyle _panel;
        private static GUIStyle _title;
        private static GUIStyle _field;
        private static GUIStyle _value;

        public static GUIStyle Message => _message ??= new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleLeft, fontSize = 14, padding = new RectOffset(12, 12, 6, 6) };

        public static GUIStyle Hint => _hint ??= new GUIStyle(GUI.skin.label) { fontSize = 12, normal = { textColor = new Color(1f, 1f, 1f, 0.55f) } };

        public static GUIStyle Panel => _panel ??= new GUIStyle(GUI.skin.box) { padding = new RectOffset(14, 14, 12, 12) };

        public static GUIStyle Title => _title ??= new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, wordWrap = true, normal = { textColor = Color.white } };

        public static GUIStyle Field => _field ??= new GUIStyle(GUI.skin.label) { fontSize = 13, normal = { textColor = new Color(1f, 1f, 1f, 0.6f) } };

        public static GUIStyle Value => _value ??= new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true, normal = { textColor = Color.white } };
    }
}
