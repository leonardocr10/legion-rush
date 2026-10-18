"""Separa o "Pacote de Assets Gráficos" (projeto/Pacote de Assets Graficos UI.png, 1500x1000) em sprites para o Unity.

A imagem é um atlas visual sobre fundo azul-escuro, não um spritesheet: cada elemento é recortado, o fundo é
removido por preenchimento a partir das bordas do recorte (alpha real, conferido na folha de contato) e
ampliado 2x. Botões e molduras viram 9-slice: pontas originais + uma coluna limpa do miolo (sem o texto),
com as bordas gravadas em borders.json (lido pelo importador do Unity e em tempo de execução).

Uso: python tools/art/extract_ui_pack.py
Saída: Assets/LegionRush/Art/UI/Resources/<Categoria>/<nome>.png e Assets/LegionRush/Art/UI/Resources/borders.json
"""
import json
import os
import sys

import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from extract_ui_art import flood_mask, inpaint_rows  # noqa: E402

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "projeto", "Pacote de Assets Graficos UI.png")
OUT = os.path.join(ROOT, "Assets", "LegionRush", "Art", "UI", "Resources")
SCALE = 2

# Botões: caixa, faixa do texto/ícone a remover (x0, x1). Resultado: ponta esquerda + miolo + ponta direita.
BUTTONS = {
    "btn_gold": ((379, 45, 574, 116), (397, 555)),
    "btn_green": ((583, 48, 738, 93), (612, 709)),
    "btn_blue": ((583, 96, 738, 141), (624, 698)),
    "btn_purple": ((559, 141, 738, 188), (590, 708)),
    "btn_orange": ((382, 191, 499, 234), (408, 476)),
    "btn_red": ((505, 239, 619, 282), (518, 603)),
    "btn_gray": ((382, 291, 499, 337), (399, 482)),
}

# Elementos recortados inteiros (fundo removido). (categoria, nome): caixa
PARTS = {
    ("Icons", "coin"): (17, 422, 73, 479), ("Icons", "gem"): (79, 422, 137, 479), ("Icons", "energy"): (148, 422, 189, 479),
    ("Icons", "crown"): (198, 422, 251, 479), ("Icons", "shield"): (256, 420, 304, 479), ("Icons", "trophy"): (314, 422, 369, 479),
    ("Icons", "star_badge"): (379, 420, 437, 479), ("Icons", "sword"): (19, 485, 73, 542), ("Icons", "helmet"): (83, 485, 123, 542),
    ("Icons", "armor"): (134, 485, 188, 542), ("Icons", "key"): (190, 485, 232, 542), ("Icons", "potion_red"): (245, 485, 287, 542),
    ("Icons", "potion_blue"): (297, 485, 338, 542), ("Icons", "potion_green"): (350, 485, 388, 542), ("Icons", "scroll"): (397, 485, 435, 542),
    ("Icons", "bag"): (190, 547, 241, 595), ("Icons", "crystal"): (250, 547, 291, 595), ("Icons", "book"): (347, 547, 391, 595),
    ("Icons", "map"): (398, 547, 437, 595), ("Icons", "plus"): (1199, 40, 1228, 74),
    ("Chests", "chest_gold"): (17, 547, 69, 595), ("Chests", "chest_royal"): (75, 547, 129, 595),
    ("Chests", "chest_steel"): (294, 547, 344, 595), ("Chests", "chest_rare"): (1171, 428, 1244, 498),
    ("Chests", "chest_epic"): (1249, 428, 1329, 498), ("Chests", "chest_blue"): (1337, 428, 1406, 498),
    ("Chests", "chest_wood"): (1415, 428, 1488, 498), ("Chests", "chest_red"): (1251, 503, 1329, 580),
    ("Rewards", "gift"): (1171, 503, 1244, 580), ("Rewards", "gem_bag"): (1337, 503, 1400, 580),
    ("Rewards", "coin_pile"): (1380, 503, 1488, 582), ("Rewards", "gift_small"): (134, 547, 179, 595),
    ("Badges", "badge_new"): (385, 649, 457, 684), ("Badges", "badge_discount"): (467, 638, 517, 690),
    ("Badges", "badge_epic"): (530, 640, 611, 684), ("Badges", "badge_legendary"): (620, 638, 724, 686),
    ("Badges", "badge_level"): (388, 696, 430, 744), ("Badges", "badge_max"): (449, 696, 490, 744),
    ("Badges", "arrow_up"): (510, 696, 553, 744), ("Badges", "orb_red"): (566, 696, 607, 744),
    ("Badges", "check"): (621, 696, 665, 744), ("Badges", "lock"): (680, 696, 724, 746),
    ("Badges", "star"): (390, 753, 423, 789), ("Badges", "star_empty"): (495, 753, 528, 789),
    ("Rankings", "rank_bronze"): (763, 648, 819, 724), ("Rankings", "rank_silver"): (824, 648, 880, 724),
    ("Rankings", "rank_gold"): (880, 646, 944, 724), ("Rankings", "rank_platinum"): (945, 646, 1003, 724),
    ("Rankings", "rank_diamond"): (1013, 646, 1078, 724),
    ("Avatars", "avatar_king"): (1253, 795, 1307, 866), ("Avatars", "avatar_archer"): (1309, 795, 1365, 866),
    ("Avatars", "avatar_warrior"): (1368, 795, 1425, 866), ("Avatars", "avatar_mage"): (1428, 795, 1484, 866),
    ("HeroCards", "portrait_king"): (465, 429, 543, 520), ("HeroCards", "portrait_archer"): (553, 429, 632, 520),
    ("HeroCards", "portrait_warrior"): (640, 429, 719, 520), ("HeroCards", "portrait_mage"): (728, 429, 807, 520),
    ("TroopCards", "troop_lancer"): (831, 426, 902, 507), ("TroopCards", "troop_archer"): (912, 426, 984, 507),
    ("TroopCards", "troop_warrior"): (994, 426, 1066, 507), ("TroopCards", "troop_knight"): (1075, 426, 1147, 507),
    ("Decorations", "banner_blue"): (590, 788, 643, 895), ("Decorations", "banner_red"): (657, 788, 710, 890),
    ("Decorations", "pine"): (655, 895, 720, 980), ("Effects", "fx_swirl"): (943, 790, 1028, 868),
    ("Effects", "fx_coins"): (1033, 790, 1110, 868), ("Effects", "fx_gem"): (1112, 790, 1168, 868),
    ("Effects", "fx_sparkle"): (1168, 790, 1230, 868), ("Effects", "fx_star"): (945, 880, 1025, 975),
    ("Navigation", "arrow_green"): (1250, 918, 1293, 975), ("Navigation", "arrow_red"): (1290, 918, 1330, 975),
    ("Navigation", "arrow_yellow"): (1325, 918, 1363, 975), ("Navigation", "target"): (1360, 918, 1404, 975),
    ("Navigation", "pin"): (1406, 918, 1444, 975), ("Navigation", "flag"): (1446, 918, 1490, 975),
    ("Navigation", "tab_home"): (1095, 240, 1158, 322), ("Navigation", "tab_heroes"): (1158, 240, 1220, 322),
    ("Navigation", "tab_troops"): (1220, 240, 1285, 322), ("Navigation", "tab_battle"): (1288, 240, 1350, 322),
    ("Navigation", "tab_inventory"): (1350, 240, 1420, 322), ("Navigation", "tab_upgrades"): (1423, 240, 1493, 322),
    ("Buttons", "icon_btn_settings"): (1097, 328, 1143, 377), ("Buttons", "icon_btn_mail"): (1148, 328, 1194, 377),
    ("Buttons", "icon_btn_sound"): (1199, 328, 1245, 377), ("Buttons", "icon_btn_music"): (1249, 328, 1293, 377),
    ("Buttons", "icon_btn_language"): (1297, 328, 1342, 377), ("Buttons", "icon_btn_ranking"): (1344, 328, 1389, 377),
    ("Buttons", "icon_btn_friends"): (1393, 328, 1439, 377), ("Buttons", "icon_btn_menu"): (1442, 328, 1487, 377),
    ("Buttons", "btn_close"): (1053, 240, 1078, 264),
    ("Panels", "plank"): (760, 353, 884, 374), ("Panels", "scroll"): (1008, 240, 1077, 337),
    ("Logos", "logo"): (12, 8, 362, 272),
    ("Backgrounds", "bg_frozen_castle"): (13, 838, 446, 992), ("Backgrounds", "bg_frozen_a"): (452, 820, 562, 896),
    ("Backgrounds", "bg_frozen_b"): (452, 905, 562, 990),
}
# Recortes que não devem ter o fundo removido (cenas retangulares).
OPAQUE = {"bg_frozen_castle", "bg_frozen_a", "bg_frozen_b", "portrait_king", "portrait_archer", "portrait_warrior",
          "portrait_mage", "troop_lancer", "troop_archer", "troop_warrior", "troop_knight", "avatar_king",
          "avatar_archer", "avatar_warrior", "avatar_mage"}

# Molduras 9-slice: caixa e borda (pixels da fonte, antes da ampliação). O miolo é preenchido com a cor interna.
FRAMES = {
    ("Frames", "frame_gold"): ((760, 240, 884, 349), 24),
    ("Frames", "frame_gold_b"): ((895, 295, 1002, 374), 20),
}
# Barras de progresso: trilho vazio e preenchimentos (pontas + coluna do miolo).
BARS = {
    ("ProgressBars", "bar_fill_green"): ((1104, 128, 1208, 147), (1116, 1196)),
    ("ProgressBars", "bar_fill_blue"): ((1284, 128, 1426, 147), (1296, 1414)),
    ("ProgressBars", "bar_fill_orange"): ((1104, 164, 1176, 183), (1116, 1164)),
    ("ProgressBars", "bar_fill_purple"): ((1284, 164, 1353, 183), (1296, 1341)),
}
TRACK = (1100, 123, 1268, 152)

# Cabeçalho de popup: topo da janela ornamentada com o texto do título apagado.
POPUP_HEADER = ((760, 42, 1072, 128), [(845, 85, 990, 113)])


def alpha_cut(img, tolerance=26):
    rgb = np.array(img.convert("RGB"))
    h, w, _ = rgb.shape
    seeds = [(x, 0) for x in range(w)] + [(x, h - 1) for x in range(w)] + [(0, y) for y in range(h)] + [(w - 1, y) for y in range(h)]
    mask = flood_mask(rgb, seeds, tolerance)
    alpha = Image.fromarray(np.where(mask, 0, 255).astype(np.uint8))
    out = img.convert("RGBA")
    out.putalpha(alpha)
    return out


def pill(img):
    from PIL import ImageDraw
    w, h = img.size
    mask = Image.new("L", (w * 4, h * 4), 0)
    ImageDraw.Draw(mask).rounded_rectangle((0, 0, w * 4 - 1, h * 4 - 1), h * 2, fill=255)
    out = img.convert("RGBA")
    out.putalpha(mask.resize((w, h), Image.LANCZOS))
    return out


def upscale(img):
    return img.resize((img.width * SCALE, img.height * SCALE), Image.LANCZOS)


def three_slice(src, box, cut, middle=6):
    """Junta ponta esquerda, coluna limpa repetida e ponta direita; devolve imagem e (esq, dir)."""
    x0, y0, x1, y1 = box
    c0, c1 = cut
    left = src.crop((x0, y0, c0, y1))
    column = src.crop((c0 - 1, y0, c0, y1)).resize((middle, y1 - y0))
    right = src.crop((c1, y0, x1, y1))
    out = Image.new("RGB", (left.width + middle + right.width, y1 - y0))
    out.paste(left, (0, 0))
    out.paste(column, (left.width, 0))
    out.paste(right, (left.width + middle, 0))
    return out, left.width, right.width


def save(category, name, img, borders, border=None):
    folder = os.path.join(OUT, category)
    os.makedirs(folder, exist_ok=True)
    img.save(os.path.join(folder, name + ".png"))
    if border is not None:
        borders[f"{category}/{name}"] = [int(v) for v in border]  # esquerda, baixo, direita, topo (pixels finais)


def main():
    src = Image.open(SRC).convert("RGB")
    borders = {}

    for name, (box, cut) in BUTTONS.items():
        img, left, right = three_slice(src, box, cut)
        img = upscale(alpha_cut(img))
        h = img.height
        vertical = (h - 4) // 2
        save("Buttons", name, img, borders, (left * SCALE, vertical, right * SCALE, vertical))

    # Preenchimentos: máscara de pílula (o degradê das pontas confunde o preenchimento por cor).
    for (category, name), (box, cut) in BARS.items():
        img, left, right = three_slice(src, box, cut)
        img = upscale(pill(img))
        vertical = (img.height - 2) // 2
        save(category, name, img, borders, (left * SCALE, vertical, right * SCALE, vertical))

    # Trilho vazio: ponta direita do trilho verde espelhada para a esquerda (a esquerda original tem preenchimento).
    tx0, ty0, tx1, ty1 = TRACK
    right = src.crop((tx1 - 30, ty0, tx1, ty1))
    column = src.crop((tx1 - 31, ty0, tx1 - 30, ty1)).resize((6, ty1 - ty0))
    track = Image.new("RGB", (66, ty1 - ty0))
    track.paste(right.transpose(Image.FLIP_LEFT_RIGHT), (0, 0))
    track.paste(column, (30, 0))
    track.paste(right, (36, 0))
    track = upscale(alpha_cut(track, 20))
    vertical = (track.height - 2) // 2
    save("ProgressBars", "bar_track", track, borders, (30 * SCALE, vertical, 30 * SCALE, vertical))

    for (category, name), (box, edge) in FRAMES.items():
        crop = src.crop(box)
        px = np.array(crop)
        inner = px[edge + 6:-edge - 6, edge + 6:-edge - 6].reshape(-1, 3)
        fill = np.median(inner, axis=0).astype(np.uint8)
        px[edge:-edge, edge:-edge] = fill
        img = upscale(alpha_cut(Image.fromarray(px), 22))
        e = edge * SCALE
        save(category, name, img, borders, (e, e, e, e))

    header_box, texts = POPUP_HEADER
    px = np.array(src)
    for (x0, y0, x1, y1) in texts:
        inpaint_rows(px, (x0, y0, x1, y1))
    header = Image.fromarray(px).crop(header_box)
    save("Popups", "popup_header", upscale(alpha_cut(header, 24)), borders)

    for (category, name), box in PARTS.items():
        crop = src.crop(box)
        img = crop.convert("RGBA") if name in OPAQUE else alpha_cut(crop)
        save(category, name, upscale(img), borders)

    build_battle_hud(borders)

    # Uma entrada por linha: lido também por um parser simples no Unity (editor e runtime).
    lines = [f'  "{key}": [{", ".join(str(v) for v in value)}]' for key, value in sorted(borders.items())]
    with open(os.path.join(OUT, "borders.json"), "w", encoding="utf-8") as f:
        f.write("{\n" + ",\n".join(lines) + "\n}\n")
    print("ok", len(PARTS) + len(BUTTONS) + len(BARS) + len(FRAMES) + 1, "sprites")


# ---------------- HUD de batalha (projeto/Prancha Componentes Batalha.png, 1168x1346) ----------------
BATTLE = os.path.join(ROOT, "projeto", "Prancha Componentes Batalha.png")
HUD_PARTS = {
    "hud_gold": (20, 1000, 162, 1051), "hud_gems": (384, 1000, 512, 1051), "hud_settings": (517, 1003, 562, 1050),
    "hud_level_start": (172, 1022, 198, 1049), "hud_crown": (341, 1017, 381, 1053), "hud_skull": (926, 1153, 979, 1209),
    "hud_people": (855, 1153, 912, 1201),
    "portrait_king": (602, 1002, 690, 1100), "portrait_archer": (711, 1002, 803, 1100),
    "portrait_mage": (824, 1002, 913, 1100), "portrait_dark": (934, 1002, 1025, 1100), "portrait_healer": (1048, 1002, 1140, 1100),
    "skill_arrows": (12, 1160, 110, 1292), "skill_charge": (114, 1160, 213, 1292), "skill_storm": (217, 1160, 316, 1292),
}
HUD_SKILL_BADGES = [(90, 1275), (191, 1275), (293, 1275)]
HUD_ERASE = [(78, 1012, 142, 1040), (434, 1012, 486, 1040)]
HUD_BARS = {"hud_boss_fill": ((989, 1163, 1086, 1191), (1001, 1074)), "hud_boss_track": ((1090, 1163, 1149, 1192), (1104, 1136))}


def build_battle_hud(borders):
    src = Image.open(BATTLE).convert("RGB")
    px = np.array(src)
    for box in HUD_ERASE:
        inpaint_rows(px, box)
    # Selos com números fixos das habilidades: o miolo azul do círculo vira cor sólida (contador real fica no HUD).
    for cx, cy in HUD_SKILL_BADGES:
        yy, xx = np.ogrid[:px.shape[0], :px.shape[1]]
        inside = (xx - cx) ** 2 + (yy - cy) ** 2 <= 13 ** 2
        px[inside] = np.array([12, 32, 92], dtype=np.uint8)
    clean = Image.fromarray(px)
    for name, box in HUD_PARTS.items():
        part = alpha_cut(clean.crop(box), 24)
        if name.startswith("skill_"):
            # O preenchimento por cor vaza para dentro do selo; repinta o miolo opaco depois do recorte.
            arr = np.array(part)
            yy, xx = np.ogrid[:arr.shape[0], :arr.shape[1]]
            for cx, cy in HUD_SKILL_BADGES:
                inside = (xx - (cx - box[0])) ** 2 + (yy - (cy - box[1])) ** 2 <= 14 ** 2
                arr[inside] = np.array([12, 32, 92, 255], dtype=np.uint8)
            part = Image.fromarray(arr)
        save("Hud", name, upscale(part), borders)
    for name, (box, cut) in HUD_BARS.items():
        img, left, right = three_slice(clean, box, cut)
        img = upscale(pill(img))
        vertical = (img.height - 2) // 2
        save("Hud", name, img, borders, (left * SCALE, vertical, right * SCALE, vertical))
    # Moldura de habilidade vazia (para habilidades sem arte própria): miolo da arte das flechas preenchido.
    frame = np.array(clean.crop(HUD_PARTS["skill_arrows"]))
    inset = 11
    frame[inset:-inset, inset:-inset] = np.array([14, 30, 70], dtype=np.uint8)
    save("Hud", "skill_frame", upscale(alpha_cut(Image.fromarray(frame), 24)), borders)


if __name__ == "__main__":
    main()
