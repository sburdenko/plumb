"""Renders the Plumb icons from their vector definitions.

Outputs:
  src/Plumb.App/Assets/plumb.ico                   window icon, 16-256 px
  viewer/Assets/Plumb/Icons/viewer-icon.png        Unity player icon, 1024 px, with the Unity badge

The mark is drawn from the same geometry as Plumb.Mark.Line / Plumb.Mark.Bob in the app theme
(48x48 box). The Unity badge comes from unity-mark.path (Simple Icons, CC0 path data); Unity is a
trademark of Unity Technologies.

Run from the repository root: python3 tools/icons/render_icons.py
"""
import re
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
INK = (0x20, 0x1E, 0x1D, 255)
TEXT = (0xF8, 0xF4, 0xF4, 255)
ACCENT = (0xEC, 0x30, 0x13, 255)
RULE = (0x44, 0x41, 0x41, 255)
SUPERSAMPLE = 4

MARK_RECTS = [(10, 2, 38, 6), (22.5, 5, 25.5, 20)]
MARK_BOB = [(24, 17), (34, 30), (24, 46), (14, 30)]


def draw_mark(draw, origin, scale):
    ox, oy = origin
    for x0, y0, x1, y1 in MARK_RECTS:
        draw.rectangle([ox + x0 * scale, oy + y0 * scale, ox + x1 * scale - 1, oy + y1 * scale - 1], fill=TEXT)
    draw.polygon([(ox + x * scale, oy + y * scale) for x, y in MARK_BOB], fill=ACCENT)


def app_icon(size, mark_box=(22, 22, 1.5833), badge=False):
    """The app icon on a 120-unit canvas: ink square and the light mark, as in plumb-app-icon.svg."""
    big = size * SUPERSAMPLE
    unit = big / 120
    image = Image.new("RGBA", (big, big), INK)
    draw = ImageDraw.Draw(image)
    mx, my, mscale = mark_box
    draw_mark(draw, (mx * unit, my * unit), mscale * unit)
    if badge:
        draw_unity_badge(draw, unit)
    return image.resize((size, size), Image.LANCZOS)


def draw_unity_badge(draw, unit):
    """A light square in the bottom-right corner with the Unity mark in ink, ruled like the app's panels."""
    left, top, side = 66, 66, 46
    draw.rectangle([left * unit, top * unit, (left + side) * unit, (top + side) * unit], fill=TEXT, outline=RULE, width=max(1, int(2 * unit)))
    inset = 8
    scale = (side - 2 * inset) / 24
    polygon = path_to_polygon((ROOT / "tools/icons/unity-mark.path").read_text().strip())
    draw.polygon([((left + inset + x * scale) * unit, (top + inset + y * scale) * unit) for x, y in polygon], fill=INK)


def path_to_polygon(d):
    """Reads an SVG path made of one outline. Curves and arcs become straight segments to their end
    points, which only removes corner roundings far smaller than a badge pixel."""
    tokens = re.findall(r"[a-zA-Z]|-?\d*\.?\d+(?:e-?\d+)?", d)
    arity = {"m": 2, "l": 2, "h": 1, "v": 1, "c": 6, "s": 4, "q": 4, "t": 2, "a": 7, "z": 0}
    points, x, y, i, command = [], 0.0, 0.0, 0, None
    while i < len(tokens):
        if re.match(r"[a-zA-Z]", tokens[i]):
            command = tokens[i]
            i += 1
            if command in "zZ":
                continue
        n = arity[command.lower()]
        args = [float(t) for t in tokens[i:i + n]]
        i += n
        relative = command.islower()
        kind = command.lower()
        if kind == "h":
            x = x + args[0] if relative else args[0]
        elif kind == "v":
            y = y + args[0] if relative else args[0]
        else:
            ex, ey = args[-2], args[-1]
            x, y = (x + ex, y + ey) if relative else (ex, ey)
        points.append((x, y))
        if command == "m":
            command = "l"
        elif command == "M":
            command = "L"
    return points


def main():
    app_dir = ROOT / "src/Plumb.App/Assets"
    app_dir.mkdir(parents=True, exist_ok=True)
    sizes = [16, 24, 32, 48, 64, 128, 256]
    largest = app_icon(256)
    largest.save(app_dir / "plumb.ico", sizes=[(s, s) for s in sizes])

    viewer_dir = ROOT / "viewer/Assets/Plumb/Icons"
    viewer_dir.mkdir(parents=True, exist_ok=True)
    app_icon(1024, mark_box=(10, 8, 1.5833), badge=True).save(viewer_dir / "viewer-icon.png")
    print("icons written")


if __name__ == "__main__":
    main()
