using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// IMGUI text styles in Archivo, created lazily because GUI.skin is only available inside OnGUI.
    /// Styles are single line unless they wrap on purpose: the skin's label wraps, which breaks measured labels.
    /// </summary>
    public static class ViewerStyles
    {
        private static Font _regular;
        private static Font _semiBold;
        private static Font _extraBold;
        private static GUIStyle _brand;
        private static GUIStyle _crumbStrong;
        private static GUIStyle _badge;
        private static GUIStyle _kicker;
        private static GUIStyle _title;
        private static GUIStyle _heading;
        private static GUIStyle _key;
        private static GUIStyle _value;
        private static GUIStyle _status;

        public static GUIStyle Brand => _brand ??= Make(ExtraBold, 18, Graphite.Text);

        public static GUIStyle CrumbStrong => _crumbStrong ??= Make(SemiBold, 13, Graphite.Text);

        public static GUIStyle Badge => _badge ??= Make(SemiBold, 11, Graphite.TextDim);

        public static GUIStyle Kicker => _kicker ??= Make(SemiBold, 11, Graphite.AccentOnDark);

        public static GUIStyle Title => _title ??= Make(ExtraBold, 18, Graphite.Text);

        public static GUIStyle Heading => _heading ??= Make(ExtraBold, 22, Graphite.Text, wraps: true);

        public static GUIStyle Key => _key ??= Make(Regular, 13, Graphite.TextMuted);

        public static GUIStyle Value => _value ??= Make(SemiBold, 13, Graphite.Text);

        public static GUIStyle Status => _status ??= Make(Regular, 12, Graphite.TextDim);

        private static Font Regular => _regular ??= Resources.Load<Font>("Fonts/Archivo-Regular");

        private static Font SemiBold => _semiBold ??= Resources.Load<Font>("Fonts/Archivo-SemiBold");

        private static Font ExtraBold => _extraBold ??= Resources.Load<Font>("Fonts/Archivo-ExtraBold");

        private static GUIStyle Make(Font font, int size, Color color, bool wraps = false)
        {
            return new GUIStyle(GUI.skin.label)
            {
                font = font,
                fontSize = size,
                normal = { textColor = color },
                padding = new RectOffset(0, 0, 0, 0),
                margin = new RectOffset(0, 0, 0, 0),
                wordWrap = wraps,
                clipping = TextClipping.Clip,
            };
        }
    }
}
