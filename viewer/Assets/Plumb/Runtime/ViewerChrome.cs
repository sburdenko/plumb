using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// The frame around the 3D view: a top bar naming the package and a status bar with the controls.
    /// </summary>
    public static class ViewerChrome
    {
        public const float TopBarHeight = 48f;
        public const float StatusBarHeight = 28f;
        private const float Gutter = 14f;
        public const string HelpText = "Left drag  rotate     Shift or middle drag  pan     Wheel  zoom     Click  select     F  frame";

        public static void DrawTopBar(string packageName)
        {
            var bar = new Rect(0, 0, Screen.width, TopBarHeight);
            Graphite.Fill(bar, Graphite.Panel);
            Graphite.Fill(new Rect(0, TopBarHeight - 2, Screen.width, 2), Graphite.Rule);

            var x = Gutter;
            x = Label(new GUIContent("Plumb"), ViewerStyles.Brand, x, 13) + 24;
            if (!string.IsNullOrEmpty(packageName))
            {
                x = Label(new GUIContent(packageName), ViewerStyles.CrumbStrong, x, 16) + 10;
            }

            var badge = new GUIContent("UNITY VIEWER");
            var size = ViewerStyles.Badge.CalcSize(badge);
            var box = new Rect(x, 14, size.x + 14, 20);
            Graphite.Rules(box, Graphite.Border, 1, 1, 1, 1);
            GUI.Label(new Rect(box.x + 7, box.y + 3, size.x, size.y), badge, ViewerStyles.Badge);
        }

        public static void DrawStatusBar(string text)
        {
            var bar = new Rect(0, Screen.height - StatusBarHeight, Screen.width, StatusBarHeight);
            Graphite.Fill(bar, Graphite.Panel);
            Graphite.Fill(new Rect(0, bar.y, Screen.width, 2), Graphite.Rule);
            GUI.Label(new Rect(Gutter, bar.y + 7, Screen.width - 2 * Gutter, 18), text, ViewerStyles.Status);
        }

        /// <summary>A kicker and message where the app shows its empty and error states: 48 px left, 120 px down.</summary>
        public static void DrawNotice(string kicker, string message)
        {
            var area = new Rect(48, 120, Mathf.Min(520, Screen.width - 96), 200);
            GUILayout.BeginArea(area);
            GUILayout.Label(kicker, ViewerStyles.Kicker);
            GUILayout.Space(8);
            GUILayout.Label(message, ViewerStyles.Heading);
            GUILayout.EndArea();
        }

        private static float Label(GUIContent content, GUIStyle style, float x, float y)
        {
            var size = style.CalcSize(content);
            GUI.Label(new Rect(x, y, size.x, size.y), content, style);
            return x + size.x;
        }
    }
}
