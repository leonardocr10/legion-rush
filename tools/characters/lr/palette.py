"""Shared character palette atlas.

Every character uses ONE material (LegionToon) with this atlas: each palette key is a 32x32 swatch
in a 512x512 texture. Faces get UVs inside their swatch; the vertical position inside the swatch
follows the vertex height in the part, so the swatch gradient reads as hand-painted shading.
"""
import math

ATLAS_SIZE = 512
SWATCH = 32
GRID = ATLAS_SIZE // SWATCH  # 16 x 16 swatches

# key: (hex sRGB, style, emissive)
#   style: 'grad' soft vertical gradient, 'metal' bright band in the middle, 'flat' uniform
COLORS = {
    # Blue army
    "blue": ("#2a55d8", "grad", False),
    "blue_dark": ("#1a3496", "grad", False),
    "blue_light": ("#4f86f7", "grad", False),
    "blue_plume": ("#1f4ce0", "grad", False),
    # Metals
    "gold": ("#f0b93c", "metal", False),
    "gold_dark": ("#b77c1e", "metal", False),
    "steel": ("#c3cad6", "metal", False),
    "steel_dark": ("#7c8594", "metal", False),
    "iron": ("#5b5560", "metal", False),
    "iron_dark": ("#36313a", "metal", False),
    "bronze": ("#a8692c", "metal", False),
    # Leather / cloth
    "leather": ("#7d4a28", "grad", False),
    "leather_dark": ("#4b2b17", "grad", False),
    "leather_light": ("#a0683a", "grad", False),
    "boot": ("#5d3820", "grad", False),
    "cloth_brown": ("#6a4128", "grad", False),
    "white": ("#f3efe6", "grad", False),
    "cream": ("#e9dcc0", "grad", False),
    "fur": ("#ece8e0", "grad", False),
    "rope": ("#b08a5a", "grad", False),
    "wood": ("#7c4d2b", "grad", False),
    "wood_dark": ("#4c2d18", "grad", False),
    # Skin / hair / face
    "skin": ("#eab38c", "grad", False),
    "skin_dark": ("#c98a62", "grad", False),
    "skin_tan": ("#d99a6c", "grad", False),
    "hair_dark": ("#33200f", "grad", False),
    "hair_brown": ("#6b3a1c", "grad", False),
    "hair_auburn": ("#7d3f1e", "grad", False),
    "hair_red": ("#a8341c", "grad", False),
    "eye_white": ("#fbf8f2", "flat", False),
    "iris_blue": ("#2f74d8", "flat", False),
    "iris_brown": ("#5a361c", "flat", False),
    "iris_red": ("#b0281c", "flat", False),
    "pupil": ("#120d10", "flat", False),
    "mouth": ("#5e1b1d", "flat", False),
    "teeth": ("#f5f0e4", "flat", False),
    "lip": ("#c86f62", "flat", False),
    "brow_dark": ("#25160c", "flat", False),
    # Red army
    "red": ("#d8262a", "grad", False),
    "red_dark": ("#8f1518", "grad", False),
    "red_light": ("#f2483e", "grad", False),
    "red_crest": ("#e01f22", "grad", False),
    "purple_dark": ("#2f1f3a", "grad", False),
    "purple": ("#4b2c5c", "grad", False),
    "black_cloth": ("#241c22", "grad", False),
    # Horse
    "horse_white": ("#ece9e4", "grad", False),
    "horse_grey": ("#aba59f", "grad", False),
    "hoof": ("#3f3129", "grad", False),
    # Emissive (also written to the emission map)
    "glow_blue": ("#5fd3ff", "flat", True),
    "glow_gold": ("#ffe486", "flat", True),
    "glow_red": ("#ff4a18", "flat", True),
    "glow_orange": ("#ffa22c", "flat", True),
    "glow_white": ("#ffffff", "flat", True),
    "gem_blue": ("#2d63ff", "metal", False),
    "gem_red": ("#e0242a", "metal", False),
}

KEYS = list(COLORS.keys())
INDEX = {k: i for i, k in enumerate(KEYS)}
assert len(KEYS) <= GRID * GRID


def hex_to_linear(h):
    h = h.lstrip("#")
    srgb = [int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)]
    return tuple(c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4 for c in srgb)


def hex_to_srgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def swatch_rect(key):
    """UV rect (u0, v0, u1, v1) of the usable swatch interior (inset to survive mip bleeding)."""
    i = INDEX[key]
    col, row = i % GRID, i // GRID
    inset = 6 / ATLAS_SIZE
    s = SWATCH / ATLAS_SIZE
    u0 = col * s + inset
    u1 = (col + 1) * s - inset
    # row 0 at the top of the image; UV v grows upward
    v1 = 1.0 - row * s - inset
    v0 = 1.0 - (row + 1) * s + inset
    return u0, v0, u1, v1


def _shade(style, t):
    """Brightness multiplier for vertical position t (0 bottom .. 1 top)."""
    if style == "flat":
        return 1.0
    if style == "metal":
        band = math.exp(-((t - 0.62) ** 2) / 0.02)
        return 0.78 + 0.22 * t + 0.28 * band
    return 0.84 + 0.24 * t


def atlas_pixels():
    """Returns (albedo, emission) float RGBA lists in Blender image order (bottom row first)."""
    n = ATLAS_SIZE
    albedo = [0.0] * (n * n * 4)
    emission = [0.0] * (n * n * 4)
    for key, (hx, style, emissive) in COLORS.items():
        base = hex_to_srgb(hx)
        i = INDEX[key]
        col, row = i % GRID, i // GRID
        x0 = col * SWATCH
        y_top = n - row * SWATCH  # exclusive, in bottom-up pixel rows
        for py in range(SWATCH):
            y = y_top - SWATCH + py
            t = py / (SWATCH - 1)
            k = _shade(style, t)
            c = [min(1.0, max(0.0, ch * k)) for ch in base]
            for px in range(SWATCH):
                o = (y * n + x0 + px) * 4
                albedo[o:o + 4] = [c[0], c[1], c[2], 1.0]
                if emissive:
                    emission[o:o + 4] = [base[0], base[1], base[2], 1.0]
                else:
                    emission[o:o + 4] = [0.0, 0.0, 0.0, 1.0]
    return albedo, emission
