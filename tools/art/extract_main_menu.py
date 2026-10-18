"""Separa os recortes da tela inicial (projeto/tela-inicial) em sprites independentes para a UI do menu.

Cada recorte do artista vem com textos e valores de exemplo desenhados por cima ("Leonardo", "125.4K",
"LOJA"...). Aqui eles são separados em:
  - molduras vazias (o miolo é reconstruído com um gradiente ajustado à arte ao redor);
  - ícones com alpha próprio (bordas suaves e cor descontaminada do fundo);
  - fundos e personagens em camadas.
Os textos voltam no Unity como TextMeshPro com dados reais.

Uso:  python tools/art/extract_main_menu.py
Saída: Assets/LegionRush/UI/MainMenu/<Pasta>/<Nome>.png e Assets/LegionRush/UI/MainMenu/Resources/MainMenuBorders.json
"""

import json
import os
import sys
from collections import deque

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(ROOT, "projeto", "tela-inicial")
OUT = os.path.join(ROOT, "Assets", "LegionRush", "UI", "MainMenu")
PREVIEW = os.environ.get("MAIN_MENU_PREVIEW")  # pasta opcional para folhas de conferência

borders = {}
saved = []


# ---------------------------------------------------------------- utilidades

def load(name):
    return np.array(Image.open(os.path.join(SRC, name)).convert("RGBA")).astype(np.float32)


def save(arr, folder, name, border=None):
    path = os.path.join(OUT, folder)
    os.makedirs(path, exist_ok=True)
    img = Image.fromarray(np.clip(arr, 0, 255).astype(np.uint8), "RGBA")
    img.save(os.path.join(path, name + ".png"), optimize=True)
    if border:
        borders[name] = [int(b) for b in border]  # esquerda, baixo, direita, topo (ordem do Unity)
    saved.append((folder, name, img))
    return img


def to_u8(mask):
    return Image.fromarray((mask * 255).astype(np.uint8), "L")


def dilate(mask, r):
    if r <= 0:
        return mask
    return np.array(to_u8(mask).filter(ImageFilter.MaxFilter(2 * r + 1))) > 127


def erode(mask, r):
    if r <= 0:
        return mask
    return np.array(to_u8(mask).filter(ImageFilter.MinFilter(2 * r + 1))) > 127


def blur(values, radius):
    img = Image.fromarray(np.clip(values * 255, 0, 255).astype(np.uint8), "L")
    return np.array(img.filter(ImageFilter.GaussianBlur(radius))).astype(np.float32) / 255.0


def components(mask):
    """Rótulos 4-conexos: devolve (rótulos, lista de tamanhos indexada pelo rótulo)."""
    h, w = mask.shape
    labels = np.zeros((h, w), np.int32)
    sizes = [0]
    ys, xs = np.nonzero(mask)
    for y0, x0 in zip(ys, xs):
        if labels[y0, x0]:
            continue
        n = len(sizes)
        labels[y0, x0] = n
        queue = deque([(y0, x0)])
        count = 0
        while queue:
            y, x = queue.popleft()
            count += 1
            for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
                if 0 <= ny < h and 0 <= nx < w and mask[ny, nx] and not labels[ny, nx]:
                    labels[ny, nx] = n
                    queue.append((ny, nx))
        sizes.append(count)
    return labels, sizes


def largest(mask):
    labels, sizes = components(mask)
    if len(sizes) <= 1:
        return mask
    return labels == int(np.argmax(sizes))


def keep_big(mask, min_size):
    labels, sizes = components(mask)
    keep = np.zeros(len(sizes), bool)
    keep[1:] = np.array(sizes[1:]) >= min_size
    return keep[labels]


def fill_holes(mask):
    h, w = mask.shape
    outside = np.zeros((h, w), bool)
    queue = deque()
    for x in range(w):
        for y in (0, h - 1):
            if not mask[y, x] and not outside[y, x]:
                outside[y, x] = True
                queue.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if not mask[y, x] and not outside[y, x]:
                outside[y, x] = True
                queue.append((y, x))
    while queue:
        y, x = queue.popleft()
        for ny, nx in ((y + 1, x), (y - 1, x), (y, x + 1), (y, x - 1)):
            if 0 <= ny < h and 0 <= nx < w and not mask[ny, nx] and not outside[ny, nx]:
                outside[ny, nx] = True
                queue.append((ny, nx))
    return ~outside


def region(shape, box):
    m = np.zeros(shape[:2], bool)
    x0, y0, x1, y1 = box
    m[max(0, y0):y1, max(0, x0):x1] = True
    return m


def fit_surface(img, sample):
    """Gradiente suave (polinômio de 2º grau em x,y por canal) ajustado aos pixels de fundo."""
    h, w = img.shape[:2]
    ys, xs = np.nonzero(sample)
    if len(xs) < 12:
        raise ValueError("poucos pixels de fundo para ajustar")
    xn, yn = xs / w, ys / h

    def basis(x, y):
        return np.stack([np.ones_like(x), x, y, x * x, y * y, x * y], 1)

    coef, *_ = np.linalg.lstsq(basis(xn, yn), img[ys, xs, :3], rcond=None)
    gy, gx = np.mgrid[0:h, 0:w]
    surface = basis(gx.ravel() / w, gy.ravel() / h) @ coef
    return surface.reshape(h, w, 3)


def color_dist(img, ref):
    return np.abs(img[..., :3] - ref).max(axis=-1)


def is_navy(img, max_bright=125):
    r, g, b = img[..., 0], img[..., 1], img[..., 2]
    return (img[..., 3] > 200) & (r < 22) & (b > 35) & (b < max_bright) & (g < b * 0.62 + 6)


def erase(img, area, bg_sample, thr=26, grow=2, feather=1.2, full=False):
    """Troca o que destoa do fundo dentro de `area` pelo gradiente ajustado (ou a área toda, com `full`).
    Devolve (nova, fundo, conteúdo)."""
    surface = fit_surface(img, bg_sample & area)
    content = area.copy() if full else dilate((color_dist(img, surface) > thr) & area, grow) & area
    weight = blur(content.astype(np.float32), feather) * area
    weight = np.maximum(weight, content)
    out = img.copy()
    out[..., :3] = img[..., :3] * (1 - weight[..., None]) + surface * weight[..., None]
    return out, surface, content


def cut(img, surface, mask, ramp=(14, 46), pad=2, alpha_floor=None):
    """Isola `mask` como sprite: alpha suave pela distância ao fundo e cor descontaminada."""
    dist = color_dist(img, surface)
    soft = np.clip((dist - ramp[0]) / (ramp[1] - ramp[0]), 0, 1)
    core = erode(mask, 1)
    band = dilate(mask, 1)
    alpha = np.where(core, 1.0, np.where(band, soft, 0.0))
    alpha = np.maximum(alpha, blur(core.astype(np.float32), 0.6) * band)
    alpha *= img[..., 3] / 255.0
    if alpha_floor is not None:
        alpha = np.maximum(alpha, alpha_floor * band)
    out = np.zeros_like(img)
    a = np.clip(alpha, 1e-3, 1)[..., None]
    fg = (img[..., :3] - (1 - a) * surface) / a
    out[..., :3] = np.where(alpha[..., None] > 0.98, img[..., :3], np.clip(fg, 0, 255))
    out[..., 3] = alpha * 255
    return trim(out, pad)


def trim(arr, pad=2):
    ys, xs = np.nonzero(arr[..., 3] > 3)
    if len(xs) == 0:
        return arr
    y0, y1 = max(0, ys.min() - pad), min(arr.shape[0], ys.max() + pad + 1)
    x0, x1 = max(0, xs.min() - pad), min(arr.shape[1], xs.max() + pad + 1)
    return arr[y0:y1, x0:x1]


def isolate_main(img, thr=40):
    """Mantém só o maior bloco opaco (tira lascas de elementos vizinhos que vieram no recorte)."""
    main = fill_holes(dilate(largest(img[..., 3] > thr), 1))
    out = img.copy()
    out[..., 3] *= main
    return trim(out, 1)


def rounded_mask(h, w, radius, aa=1.0):
    gy, gx = np.mgrid[0:h, 0:w].astype(np.float32) + 0.5
    cx = np.clip(gx, radius, w - radius)
    cy = np.clip(gy, radius, h - radius)
    d = np.sqrt((gx - cx) ** 2 + (gy - cy) ** 2)
    return np.clip(radius - d + 0.5 * aa, 0, 1)


def capsule_from_column(column, width, radius):
    """Barra 9-slice feita a partir de um perfil vertical real da arte (cor + alpha)."""
    h = column.shape[0]
    arr = np.repeat(column[:, None, :], width, axis=1).copy()
    arr[..., 3] *= rounded_mask(h, width, radius)
    return arr


def open_mask(mask, r):
    """Abertura morfológica: some com linhas finas (bordas de painel) sem encolher o resto."""
    return mask & dilate(erode(mask, r), r + 1)


def red_badge_mask(img):
    """Selo "!" vermelho: o bloco vermelho no canto superior direito, devolvido como círculo."""
    h, w = img.shape[:2]
    r, g, b = img[..., 0], img[..., 1], img[..., 2]
    red = (r > 150) & (g < 90) & (b < 100) & (img[..., 3] > 100)
    labels, sizes = components(red)
    gy, gx = np.mgrid[0:h, 0:w]
    for n in range(1, len(sizes)):
        if sizes[n] < 40:
            continue
        sel = labels == n
        cx, cy = gx[sel].mean(), gy[sel].mean()
        if cx > w * 0.62 and cy < h * 0.32:
            radius = np.sqrt(sizes[n] / np.pi) + 3.5
            return (gx - cx) ** 2 + (gy - cy) ** 2 <= radius ** 2
    return np.zeros((h, w), bool)


def tile_background(img, inner, exclude):
    """Amostra do fundo do botão: pixels do miolo com a cor da borda interna (azul-escuro ou azul aceso)."""
    ring = inner & ~erode(inner, 6) & ~exclude
    ref = np.median(img[ring][:, :3], axis=0)
    return inner & ~exclude & (color_dist(img, ref) < 42)


# ---------------------------------------------------------------- peças

def logo_and_scenery():
    save(isolate_main(load("01_logo_legion_rush.png")), "Sprites", "Logo_LegionRush")
    castle = isolate_main(load("16_fundo_castelo.png"), 10)
    save(feather_edges(castle, top=46, bottom=40, sides=18), "Backgrounds", "Background_FrozenKingdom")
    save(vertical_gradient([(0.0, (58, 128, 226)), (0.55, (128, 186, 244)), (1.0, (206, 228, 252))], 512),
         "Backgrounds", "Background_Sky")
    shade = vertical_gradient([(0.0, (2, 8, 30)), (1.0, (2, 8, 30))], 256)
    shade[..., 3] = 255 * np.linspace(0, 1, 256)[:, None] ** 1.6 * 0.92
    save(shade, "Backgrounds", "Shade_Vertical")
    save(vertical_gradient([(0.0, (214, 230, 250)), (0.18, (150, 182, 228)), (0.55, (34, 66, 140)), (1.0, (6, 18, 52))], 512,
                           fade_top=0.12), "Backgrounds", "Background_Ground")
    save(isolate_main(load("17_rei_e_exercito.png"), 10), "Characters", "Character_KingArmy")
    save(trim(load("26_montanhas.png"), 1), "Backgrounds", "Background_Mountains")
    split_sheet("27_pinheiros_e_rochas.png", "Sprites", "Deco_Pine", 400)
    split_sheet("28_bandeiras.png", "Sprites", "Deco_Banner", 300)
    split_sheet("29_blocos_neve.png", "Sprites", "Deco_SnowRock", 300)
    split_sheet("30_tochas.png", "Sprites", "Deco_Torch", 300)


def feather_edges(img, top, bottom, sides):
    """Some com a moldura de "cartão" da arte: bordas viram degradê para fundir com o céu e o chão."""
    h, w = img.shape[:2]
    out = img.copy()
    out[..., 3] = 255.0 * (img[..., 3] > 0)
    gy, gx = np.mgrid[0:h, 0:w].astype(np.float32)
    ramp = np.minimum.reduce([np.clip(gy / top, 0, 1), np.clip((h - 1 - gy) / bottom, 0, 1),
                              np.clip(gx / sides, 0, 1), np.clip((w - 1 - gx) / sides, 0, 1)])
    # Cantos arredondados do cartão: preenche com a linha/coluna vizinha antes de aplicar o degradê.
    holes = out[..., 3] < 250
    for y in range(h):
        row = ~holes[y]
        if row.any() and (~row).any():
            xs = np.nonzero(row)[0]
            out[y, :xs.min(), :3] = out[y, xs.min(), :3]
            out[y, xs.max() + 1:, :3] = out[y, xs.max(), :3]
    out[..., 3] = 255.0 * ramp * ramp * (3 - 2 * ramp)
    return out


def vertical_gradient(stops, height, width=8, fade_top=0.0):
    t = np.linspace(0, 1, height)
    cols = np.zeros((height, 3), np.float32)
    for c in range(3):
        cols[:, c] = np.interp(t, [p for p, _ in stops], [v[c] for _, v in stops])
    arr = np.zeros((height, width, 4), np.float32)
    arr[..., :3] = cols[:, None, :]
    alpha = np.ones(height) if fade_top <= 0 else np.clip(t / fade_top, 0, 1)
    arr[..., 3] = 255 * alpha[:, None]
    return arr


def split_sheet(file, folder, prefix, min_size):
    img = load(file)
    labels, sizes = components(dilate(img[..., 3] > 30, 1))
    parts = []
    for n in range(1, len(sizes)):
        if sizes[n] < min_size:
            continue
        ys, xs = np.nonzero(labels == n)
        parts.append((xs.min(), n))
    for i, (_, n) in enumerate(sorted(parts)):
        piece = img.copy()
        piece[..., 3] *= labels == n
        save(trim(piece, 1), folder, f"{prefix}_{i + 1}")


def profile_panel():
    img = isolate_main(load("02_perfil_jogador.png"))
    h, w = img.shape[:2]
    # Avatar com a moldura azul própria (troca por outro retrato no Unity).
    avatar = img[0:h, 0:150].copy()
    avatar[..., 3] *= rounded_mask(h, 150, 18)
    save(trim(avatar, 0), "Characters", "Avatar_King")
    # Coroa do nível.
    navy = is_navy(img)
    crown_area = region(img.shape, (156, 50, 198, 84))
    surface = fit_surface(img, navy & region(img.shape, (150, 40, 300, 86)))
    crown = keep_big((color_dist(img, surface) > 40) & crown_area & (img[..., 0] > 90), 30)
    save(cut(img, surface, fill_holes(crown)), "Icons", "Icon_Crown")
    # Painel sem nome, nível e barra de XP.
    text_area = region(img.shape, (154, 6, w - 9, h - 8))
    clean, _, _ = erase(img, text_area, navy, full=True, feather=2.0)
    save(clean, "Panels", "Panel_PlayerProfile")
    # Barra de XP: trilho e preenchimento com o perfil vertical real da arte.
    track_col = img[84:122, 440].copy()
    fill_col = img[84:122, 200].copy()
    for name, col in (("Bar_Track", track_col), ("Bar_FillBlue", fill_col)):
        alpha_mask = (col[:, 3] > 0).astype(np.float32)
        col[:, 3] = 255 * alpha_mask
        save(capsule_from_column(col, 48, 15), "Panels", name, (16, 0, 16, 0))


def currency_bar(file, icon_name, bar_name):
    img = isolate_main(load(file))
    h, w = img.shape[:2]
    opaque = img[..., 3] > 200
    navy = is_navy(img, 140) & opaque
    # Colunas do trilho: a faixa onde só há trilho (entre o ícone e o "+").
    green = (img[..., 1] > 120) & (img[..., 0] < 120) & (img[..., 1] > img[..., 2]) & opaque
    plus_x0 = int(np.nonzero(green.any(axis=0))[0].min())
    surface = fit_surface(img, navy)
    # Ícone (moeda / gema): tudo à esquerda que não é trilho.
    left = region(img.shape, (0, 0, int(w * 0.38), h))
    icon = (color_dist(img, surface) > 38) & left & (img[..., 3] > 60)
    icon = fill_holes(largest(open_mask(dilate(icon, 1), 3)))
    icon_img = img.copy()
    outside = (img[..., 3] > 0) & ~navy
    icon_img[..., 3] = np.where(icon, img[..., 3], 0)
    save(cut(icon_img, surface, icon & outside | icon), "Icons", icon_name)
    # Botão "+": bloco verde com o contorno escuro em volta.
    plus = fill_holes(dilate(largest(green), 3)) & opaque & region(img.shape, (plus_x0 - 6, 0, w, h))
    plus_img = img.copy()
    plus_img[..., 3] = np.where(plus, img[..., 3], 0)
    plus_img = trim(plus_img, 0)
    ph, pw = plus_img.shape[:2]
    plus_img[..., 3] *= rounded_mask(ph, pw, 9)
    save(plus_img, "Buttons", "Button_Add")
    # Trilho vazio: perfil vertical real numa coluna limpa, logo antes do "+".
    col = img[:, plus_x0 - 4].copy()
    rows = np.nonzero(col[:, 3] > 30)[0]
    col = col[rows.min():rows.max() + 1]
    save(capsule_from_column(col, 40, 8), "Panels", bar_name, (12, 0, 12, 0))


def settings_button():
    save(isolate_main(load("05_configuracoes.png")), "Buttons", "Button_Settings")


TILES = [
    ("06_loja.png", "Icon_Store"), ("07_missoes.png", "Icon_Missions"), ("08_eventos.png", "Icon_Events"),
    ("09_melhorias.png", "Icon_Upgrades"), ("10_ranking.png", "Icon_Ranking"), ("11_recompensas.png", "Icon_Rewards"),
    ("12_temporada.png", "Icon_Season"), ("13_amigos.png", "Icon_Friends"), ("14_mensagens.png", "Icon_Messages"),
    ("15_bau_temporizado.png", "Icon_TimedChest"),
]

NAV = [
    ("21_menu_inicio.png", "Icon_Home"), ("22_menu_herois.png", "Icon_Heroes"), ("23_menu_tropas.png", "Icon_Troops"),
    ("24_menu_passe.png", "Icon_BattlePass"), ("25_menu_mais.png", "Icon_More"),
]


def tile_parts(file, label_from_bottom):
    """Separa um botão de menu: ícone (sem selo e sem rótulo) e a moldura com o miolo limpo."""
    img = isolate_main(load(file))
    h, w = img.shape[:2]
    badge = red_badge_mask(img)
    inner = erode(img[..., 3] > 200, 7)
    sample = tile_background(img, inner, dilate(badge, 2))
    surface = fit_surface(img, sample)
    content = (color_dist(img, surface) > 34) & inner
    label_top = h - label_from_bottom
    gy, gx = np.mgrid[0:h, 0:w]
    icon_zone = content & ~dilate(badge, 2) & (gy < label_top) & (gx > 9) & (gx < w - 9)
    icon = keep_big(open_mask(icon_zone, 1), 60)
    labels, sizes = components(icon)
    if len(sizes) > 1:
        biggest = max(sizes[1:])
        keep = np.array([False] + [s >= biggest * 0.08 for s in sizes[1:]])
        icon = keep[labels]
    icon = fill_holes(icon)
    return img, surface, icon, badge, label_top


def side_and_nav_icons():
    clock_done = False
    badge_done = False
    for file, name in TILES:
        timer = file.startswith(("11_", "15_"))
        img, surface, icon, badge, label_top = tile_parts(file, 58 if file.startswith("11_") else 46 if timer else 40)
        save(cut(img, surface, icon), "Icons", name)
        if badge.any() and not badge_done:
            piece = img.copy()
            piece[..., 3] = np.where(badge, img[..., 3], 0)
            piece = trim(piece, 0)
            bh, bw = piece.shape[:2]
            piece[..., 3] *= rounded_mask(bh, bw, min(bh, bw) / 2)
            save(piece, "Icons", "Badge_Alert")
            badge_done = True
        if timer and not clock_done:
            h, w = img.shape[:2]
            pill = (img[..., :3].max(axis=-1) < 70) & (img[..., 3] > 200)
            pill_area = region(img.shape, (0, h - 34, int(w * 0.30), h))
            dark_surface = fit_surface(img, pill & pill_area)
            clock = (img[..., 0] > 150) & (img[..., 1] > 90) & (img[..., 2] < 120) & pill_area
            clock = fill_holes(dilate(largest(clock), 2)) & pill_area
            save(cut(img, dark_surface, clock, ramp=(20, 70)), "Icons", "Icon_Clock")
            clock_done = True
    for file, name in NAV:
        img, surface, icon, badge, _ = tile_parts(file, 42)
        save(cut(img, surface, icon), "Icons", name)


def frame_from(file, name, label_from_bottom, border):
    img, surface, icon, badge, _ = tile_parts(file, 0)
    inner = erode(img[..., 3] > 200, 7)
    content = ((color_dist(img, surface) > 26) & inner) | (badge & inner)
    content = dilate(content, 2) & inner
    out = img.copy()
    out[..., :3] = np.where(content[..., None], surface, img[..., :3])
    # Selo vermelho fora do miolo: remove e restaura a borda pelo espelho horizontal (a moldura é simétrica).
    if badge.any():
        mirrored = out[:, ::-1]
        spill = dilate(badge, 2) & ~inner
        out = np.where(spill[..., None], mirrored, out)
    save(out, "Buttons", name, border)


def frames():
    frame_from("13_amigos.png", "Button_SideFrame", 40, (26, 26, 26, 26))
    frame_from("22_menu_herois.png", "Button_NavFrame", 42, (28, 28, 28, 28))
    frame_from("21_menu_inicio.png", "Button_NavFrameSelected", 42, (30, 30, 30, 30))
    # Pílula do contador (fundo escuro translúcido arredondado).
    pill = np.zeros((40, 64, 4), np.float32)
    pill[..., 0], pill[..., 1], pill[..., 2] = 2, 12, 38
    pill[..., 3] = 215 * rounded_mask(40, 64, 19)
    save(pill, "Panels", "Panel_TimerPill", (20, 0, 20, 0))


def world_panel():
    img = isolate_main(load("18_painel_mundo.png"))
    h, w = img.shape[:2]
    stray = (img[..., 0] > 140) & (img[..., 1] < 90) & region(img.shape, (w - 40, h - 14, w, h))
    img[..., 3] *= ~stray
    navy = is_navy(img, 150) & erode(img[..., 3] > 200, 6)
    # Miniatura do mundo (com o marcador de mapa) e baú.
    thumb = img[12:150, 14:178].copy()
    thumb[..., 3] *= rounded_mask(thumb.shape[0], thumb.shape[1], 14)
    save(thumb, "Sprites", "Thumb_FrozenKingdom")
    surface = fit_surface(img, navy)
    chest_area = region(img.shape, (480, 8, w - 8, h - 8))
    chest = fill_holes(largest(open_mask((color_dist(img, surface) > 45) & chest_area, 2)))
    save(cut(img, surface, chest), "Icons", "Icon_WorldChest")
    # Painel com o miolo à direita da miniatura limpo (textos, barra, coroa e baú voltam como peças).
    area = region(img.shape, (180, 9, w - 9, h - 9))
    clean, _, _ = erase(img, area, navy, full=True, feather=2.0)
    save(clean, "Panels", "Panel_WorldProgress", (190, 30, 30, 30))
    bar_col = img[118:148, 300].copy()
    fill_col = img[118:148, 250].copy()
    for name, col in (("Bar_TrackWorld", bar_col), ("Bar_FillTeal", fill_col)):
        col[:, 3] = 255 * (col[:, 3] > 0)
        save(capsule_from_column(col, 40, 12), "Panels", name, (13, 0, 13, 0))


def play_and_boss():
    play = isolate_main(load("19_botao_jogar.png"))
    save(play, "Buttons", "Button_Play")
    boss = isolate_main(load("20_botao_modo_boss.png"))
    h, w = boss.shape[:2]
    # Faixa do rótulo "MODO BOSS": limpa o texto e mantém a faixa escura.
    band = region(boss.shape, (12, h - 40, w - 12, h - 9))
    dark = (boss[..., :3].max(axis=-1) < 90) & band & (boss[..., 3] > 200)
    clean, _, _ = erase(boss, band, dark, thr=40, grow=2)
    save(clean, "Buttons", "Button_Boss")


def preview_sheet():
    if not PREVIEW:
        return
    os.makedirs(PREVIEW, exist_ok=True)
    cell = 190
    cols = 8
    rows = (len(saved) + cols - 1) // cols
    sheet = Image.new("RGBA", (cols * cell, rows * cell), (255, 0, 255, 255))
    for i, (_, name, img) in enumerate(saved):
        thumb = img.copy()
        thumb.thumbnail((cell - 10, cell - 10))
        x, y = (i % cols) * cell + 5, (i // cols) * cell + 5
        sheet.alpha_composite(thumb, (x, y))
    sheet.convert("RGB").save(os.path.join(PREVIEW, "main_menu_sheet.png"))


def main():
    if not os.path.isdir(SRC):
        sys.exit("Recortes não encontrados em " + SRC)
    logo_and_scenery()
    profile_panel()
    currency_bar("03_moedas.png", "Icon_Coin", "Bar_Currency")
    currency_bar("04_gemas.png", "Icon_Gem", "Bar_Currency")
    settings_button()
    side_and_nav_icons()
    frames()
    world_panel()
    play_and_boss()
    res = os.path.join(OUT, "Resources")
    os.makedirs(res, exist_ok=True)
    with open(os.path.join(res, "MainMenuBorders.json"), "w", encoding="utf-8") as f:
        f.write("{\n" + ",\n".join(f'  "{k}": {v}' for k, v in sorted(borders.items())) + "\n}\n")
    preview_sheet()
    print(f"{len(saved)} sprites gerados em {OUT}")


if __name__ == "__main__":
    main()
