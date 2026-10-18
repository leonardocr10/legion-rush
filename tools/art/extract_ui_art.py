"""Recorta a arte de projeto/ em sprites para a interface (Assets/_Game/Resources/UIArt).

Uso: python tools/art/extract_ui_art.py
Ícones do atlas têm o fundo azul-escuro removido por preenchimento a partir das bordas.
"""
import os
from collections import deque

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "projeto")
OUT = os.path.join(ROOT, "Assets", "_Game", "Resources", "UIArt")

ATLAS = "Atlas de Ícones para Jogo Estratégico.png"
REINOS = "Quatro Reinos Fantásticos em 3D.png"
EMBLEMA = "Emblema Épico Legion Rush 3D.png"

# nome: (x0, y0, x1, y1) no atlas 1536x1024 (acima do rótulo de texto)
ICONS = {
    "icon_gold": (14, 54, 108, 150),
    "icon_gem": (118, 54, 202, 156),
    "icon_chest": (1088, 52, 1228, 168),
    "icon_chest_legend": (1228, 36, 1380, 168),
    "icon_play": (24, 650, 94, 722),
    "icon_battle": (110, 648, 184, 724),
    "icon_map": (196, 648, 276, 724),
    "icon_heroes": (290, 640, 358, 724),
    "icon_troops": (372, 648, 450, 722),
    "icon_inventory": (460, 640, 534, 724),
    "icon_shop": (546, 640, 626, 724),
    "icon_missions": (636, 640, 712, 724),
    "icon_settings": (732, 642, 810, 724),
    "icon_trophy": (1424, 640, 1516, 724),
    "icon_upgrade": (664, 262, 744, 350),
    "icon_gift": (1370, 796, 1446, 868),
    "icon_event": (1140, 796, 1210, 868),
    "icon_crown": (1450, 800, 1520, 866),
    "icon_hero_card": (966, 796, 1030, 868),
    "icon_achievement": (1040, 636, 1124, 726),
    "icon_alert": (288, 928, 342, 990),
    "icon_lock": (458, 928, 512, 992),
}

TOLERANCE = {"icon_inventory": 12}

WORLD_THUMBS = {
    "world_frozen": (14, 1130, 110, 1255),
    "world_desert": (320, 1130, 410, 1255),
    "world_forest": (614, 1130, 706, 1255),
    "world_volcanic": (912, 1130, 1002, 1255),
}


def flood_mask(rgb, seeds, tolerance, fixed=None):
    """Máscara booleana dos pixels conectados às sementes com cor próxima à da semente."""
    h, w, _ = rgb.shape
    mask = np.zeros((h, w), bool)
    data = rgb.astype(np.int32)
    queue = deque()
    for (x, y) in seeds:
        if not mask[y, x]:
            mask[y, x] = True
            queue.append((x, y, data[y, x] if fixed is None else np.array(fixed)))
    while queue:
        x, y, ref = queue.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h and not mask[ny, nx]:
                c = data[ny, nx]
                if np.abs(c - ref).sum() <= tolerance:
                    mask[ny, nx] = True
                    queue.append((nx, ny, c if fixed is None else ref))
    return mask


def remove_background(img, tolerance, upscale=1, fixed=None):
    rgb = np.array(img.convert("RGB"))
    h, w, _ = rgb.shape
    seeds = [(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)]
    seeds += [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)]
    mask = flood_mask(rgb, seeds, tolerance, fixed)
    alpha = Image.fromarray(np.where(mask, 0, 255).astype(np.uint8))
    alpha = alpha.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.GaussianBlur(1.0))
    out = img.convert("RGBA")
    out.putalpha(alpha)
    if upscale != 1:
        out = out.resize((w * upscale, h * upscale), Image.LANCZOS)
    return out


MENU = "Menu Heroico do Reino Congelado.png"

# Áreas com dados dinâmicos no mockup da tela inicial (941x1672), apagadas para receber texto ao vivo.
MENU_TEXT = [
    (133, 28, 330, 64),     # nome
    (130, 66, 218, 97),     # nível
    (222, 66, 402, 98),     # barra de xp
    (497, 34, 592, 74),     # ouro
    (712, 34, 800, 74),     # gemas
    (803, 181, 912, 217),   # timer baú grátis
    (836, 298, 912, 330),   # contador baú épico
    (682, 455, 870, 490),   # barra do passe
    (325, 1195, 615, 1235), # subtítulo do JOGAR
]
MENU_SPOTS = [
    (878, 448, 906, 480),   # nível do passe
    (88, 136, 134, 182), (88, 276, 134, 322), (88, 414, 134, 460),  # selos laterais
    (888, 353, 938, 399), (856, 1102, 904, 1148), (220, 1514, 266, 1559),
    (323, 1333, 377, 1387), (558, 1333, 612, 1387), (788, 1333, 842, 1387),  # cadeados
]


def inpaint_rows(px, box):
    """Preenche cada linha interpolando entre as cores das bordas esquerda e direita."""
    x0, y0, x1, y1 = box
    t = np.linspace(0.0, 1.0, x1 - x0)[None, :, None]
    left = px[y0:y1, x0 - 1:x0].astype(np.float32)
    right = px[y0:y1, x1:x1 + 1].astype(np.float32)
    px[y0:y1, x0:x1] = (left * (1 - t) + right * t).astype(np.uint8)


def inpaint_diffuse(px, box, iterations=400):
    x0, y0, x1, y1 = box
    region = px[y0 - 1:y1 + 1, x0 - 1:x1 + 1].astype(np.float32)
    inner = region[1:-1, 1:-1]
    inner[:] = region.mean(axis=(0, 1))
    for _ in range(iterations):
        inner[:] = (region[:-2, 1:-1] + region[2:, 1:-1] + region[1:-1, :-2] + region[1:-1, 2:]) / 4
    px[y0:y1, x0:x1] = inner.astype(np.uint8)


def build_menu(out_dir):
    img = np.array(Image.open(os.path.join(SRC, MENU)).convert("RGB"))
    for box in MENU_TEXT:
        inpaint_rows(img, box)
    for (x0, y0, x1, y1) in MENU_SPOTS:
        inpaint_diffuse(img, (x0 - 5, y0 - 5, min(x1 + 5, img.shape[1] - 1), y1 + 5), 800)
    Image.fromarray(img).save(os.path.join(out_dir, "menu_home.png"))


def main():
    os.makedirs(OUT, exist_ok=True)
    atlas = Image.open(os.path.join(SRC, ATLAS)).convert("RGB")
    for name, box in ICONS.items():
        remove_background(atlas.crop(box), tolerance=TOLERANCE.get(name, 26), upscale=2).save(os.path.join(OUT, name + ".png"))

    reinos = Image.open(os.path.join(SRC, REINOS)).convert("RGB")
    for name, box in WORLD_THUMBS.items():
        reinos.crop(box).resize(((box[2] - box[0]) * 2, (box[3] - box[1]) * 2), Image.LANCZOS).save(os.path.join(OUT, name + ".png"))
    # Fundo: faixa do Reino Congelado (castelo no alto, caminho até a base).
    reinos.crop((4, 70, 306, 835)).save(os.path.join(OUT, "bg_frozen.png"))

    emblem = Image.open(os.path.join(SRC, EMBLEMA)).convert("RGB")
    remove_background(emblem, tolerance=70, fixed=(255, 255, 255)).save(os.path.join(OUT, "logo.png"))
    build_menu(OUT)
    print("ok", len(os.listdir(OUT)))


if __name__ == "__main__":
    main()
