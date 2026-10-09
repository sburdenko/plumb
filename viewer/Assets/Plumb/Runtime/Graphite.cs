using System.Collections.Generic;
using UnityEngine;

namespace Plumb.Viewer
{
    /// <summary>
    /// The Graphite palette and IMGUI drawing helpers shared with the desktop app's look:
    /// flat panels, ruled edges, zero radius.
    /// </summary>
    public static class Graphite
    {
        public static readonly Color Ink = Hex(0x201E1D);
        public static readonly Color Panel = Hex(0x2D2B2B);
        public static readonly Color Rule = Hex(0x444141);
        public static readonly Color Border = Hex(0x605D5D);
        public static readonly Color Text = Hex(0xF8F4F4);
        public static readonly Color TextDim = Hex(0xBAB6B6);
        public static readonly Color TextMuted = Hex(0x9B9797);
        public static readonly Color Accent = Hex(0xEC3013);
        public static readonly Color AccentOnDark = Hex(0xFF9783);

        private static readonly Dictionary<Color, Texture2D> Swatches = new Dictionary<Color, Texture2D>();

        public static void Fill(Rect area, Color color)
        {
            GUI.DrawTexture(area, Swatch(color));
        }

        /// <summary>Draws rules inside the edges of <paramref name="area"/>.</summary>
        public static void Rules(Rect area, Color color, float left, float top, float right, float bottom)
        {
            Fill(new Rect(area.x, area.y, left, area.height), color);
            Fill(new Rect(area.x, area.y, area.width, top), color);
            Fill(new Rect(area.xMax - right, area.y, right, area.height), color);
            Fill(new Rect(area.x, area.yMax - bottom, area.width, bottom), color);
        }

        private static Texture2D Swatch(Color color)
        {
            if (!Swatches.TryGetValue(color, out var texture) || texture == null)
            {
                texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
                texture.SetPixel(0, 0, color);
                texture.Apply();
                Swatches[color] = texture;
            }

            return texture;
        }

        private static Color Hex(int rgb)
        {
            return new Color(((rgb >> 16) & 0xFF) / 255f, ((rgb >> 8) & 0xFF) / 255f, (rgb & 0xFF) / 255f, 1f);
        }
    }
}
