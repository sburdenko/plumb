"""Renders the Plumb icons from their vector definitions.

Outputs:
  src/Plumb.App/Assets/plumb.ico                   window and taskbar icon, 16-256 px
  src/Plumb.App/Assets/plumb-dock.png              macOS Dock icon when the app runs outside a bundle
  packaging/macos/Plumb.icns                       icon of the Plumb.app bundle
  viewer/Assets/Plumb/Icons/viewer-icon.png        3D viewer icon, 1024 px: the bob hanging inside a cube

Every icon is an ink tile with rounded corners and the mark in the middle. The macOS ones follow Apple's
template: a 824 px tile centred on a 1024 px canvas. macOS 26 puts icons of any other shape into a grey
frame. The Windows icon uses the whole canvas, because small sizes cannot spare the margin.

The mark is drawn from the same geometry as Plumb.Mark.Line / Plumb.Mark.Bob in the app theme
(48x48 box).

Run from the repository root: python3 tools/icons/render_icons.py
"""
from pathlib import Path

from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[2]
INK = (0x20, 0x1E, 0x1D, 255)
TEXT = (0xF8, 0xF4, 0xF4, 255)
ACCENT = (0xEC, 0x30, 0x13, 255)
RULE = (0x44, 0x41, 0x41, 255)
SUPERSAMPLE = 4
# Apple's macOS template: an 824 px tile on a 1024 px canvas, corners rounded by about 22% of the tile.
MACOS_MARGIN = 100 / 1024
TILE_RADIUS = 0.2237

MARK_RECTS = [(10, 2, 38, 6), (22.5, 5, 25.5, 20)]
MARK_BOB = [(24, 17), (34, 30), (24, 46), (14, 30)]

# An isometric cube outline in a 24x24 box, and the bob that hangs from the centre of its top face.
CUBE_LINES = [[(12, 2.6), (20.6, 7.3), (20.6, 16.7), (12, 21.4), (3.4, 16.7), (3.4, 7.3), (12, 2.6)],
              [(3.4, 7.3), (12, 12), (20.6, 7.3)], [(12, 12), (12, 21.4)]]
CUBE_STRING = [(12, 7.3), (12, 11.2)]
CUBE_BOB = [(12, 10.4), (14.4, 13.6), (12, 18.6), (9.6, 13.6)]


def draw_mark(draw, origin, scale):
    ox, oy = origin
    for x0, y0, x1, y1 in MARK_RECTS:
        draw.rectangle([ox + x0 * scale, oy + y0 * scale, ox + x1 * scale - 1, oy + y1 * scale - 1], fill=TEXT)
    draw.polygon([(ox + x * scale, oy + y * scale) for x, y in MARK_BOB], fill=ACCENT)


def tile(size, margin):
    """An ink tile with rounded corners on a transparent canvas, at SUPERSAMPLE times the size.
    Returns the image, a pen, the tile's offset and one of its 120 layout units."""
    big = size * SUPERSAMPLE
    inset = big * margin
    unit = (big - 2 * inset) / 120
    image = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rounded_rectangle([inset, inset, big - inset - 1, big - inset - 1], radius=TILE_RADIUS * 120 * unit,
                           fill=INK, outline=RULE, width=max(1, round(unit)))
    return image, draw, inset, unit


def app_icon(size, margin=0.0):
    """The Plumb mark centred on the tile, laid out as in plumb-app-icon.svg."""
    image, draw, inset, unit = tile(size, margin)
    draw_mark(draw, (inset + 22 * unit, inset + 22 * unit), 1.5833 * unit)
    return image.resize((size, size), Image.LANCZOS)


def viewer_icon(size, margin=0.0):
    """The 3D viewer: a light cube outline with the accent bob hanging inside it."""
    image, draw, inset, unit = tile(size, margin)
    scale = 92 * unit / 24
    origin = inset + 14 * unit

    def at(points):
        return [(origin + x * scale, origin + y * scale) for x, y in points]

    for line in CUBE_LINES:
        draw.line(at(line), fill=TEXT, width=max(1, round(4 * unit)), joint="curve")
    draw.line(at(CUBE_STRING), fill=ACCENT, width=max(1, round(3 * unit)))
    draw.polygon(at(CUBE_BOB), fill=ACCENT)
    return image.resize((size, size), Image.LANCZOS)


def main():
    app_dir = ROOT / "src/Plumb.App/Assets"
    app_dir.mkdir(parents=True, exist_ok=True)
    sizes = [16, 24, 32, 48, 64, 128, 256]
    app_icon(256).save(app_dir / "plumb.ico", sizes=[(s, s) for s in sizes])

    macos = app_icon(1024, margin=MACOS_MARGIN)
    macos.resize((512, 512), Image.LANCZOS).save(app_dir / "plumb-dock.png")
    bundle_dir = ROOT / "packaging/macos"
    bundle_dir.mkdir(parents=True, exist_ok=True)
    macos.save(bundle_dir / "Plumb.icns")

    viewer_dir = ROOT / "viewer/Assets/Plumb/Icons"
    viewer_dir.mkdir(parents=True, exist_ok=True)
    viewer_icon(1024, margin=MACOS_MARGIN).save(viewer_dir / "viewer-icon.png")
    print("icons written")


if __name__ == "__main__":
    main()
