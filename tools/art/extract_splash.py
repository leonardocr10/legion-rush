"""Separa o pacote da tela de abertura (projeto/Pacote Tela de Abertura.webp, 1536x1024) em sprites individuais.

A imagem NÃO tem canal alfa: o quadriculado de "transparência" está gravado nos pixels (cinza-claro/branco,
quadrados de ~11 px, sem saturação). Por isso cada recorte remove o fundo assim:

1. pixels "parecidos com o quadriculado" = pouca saturação e claros;
2. regiões desses pixels ligadas à borda do recorte são fundo; regiões fechadas só são fundo se tiverem a textura
   alternada do quadriculado (desvio alto) — isso preserva peles, neve e brilhos brancos lisos;
3. do que sobra, fica só o componente principal (ou os componentes que tocam a área central), para não levar
   pedaços dos vizinhos;
4. borda suavizada em 1 px e cor da franja puxada para dentro, para não sobrar halo cinza.

Também gera: botão sem o texto (o texto é TextMeshPro), barra de carregamento em trilho + preenchimento separados,
céu em degradê (não existe no pacote) e uma folha de contato sobre fundo escuro para conferir a transparência.

Uso: python tools/art/extract_splash.py
Saída: Assets/LegionRush/UI/SplashScreen/<Pasta>/Splash_*.png e Builds/SplashPreview/splash_contact.png
"""
import json
import os

import numpy as np
from PIL import Image, ImageDraw, ImageFilter
from scipy import ndimage

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SRC = os.path.join(ROOT, "projeto", "Pacote Tela de Abertura.webp")
OUT = os.path.join(ROOT, "Assets", "LegionRush", "UI", "SplashScreen")
CONTACT = os.path.join(ROOT, "Builds", "SplashPreview", "splash_contact.png")

# nome: (pasta, caixa x0, y0, x1, y1, modo). modo "largest" = só o maior componente; "all" = todos os componentes
# com área relevante (elementos com partes soltas, como o exército com lanças); "solid[:raio]" = objeto maciço
# cuja neve branca encosta direto no quadriculado (rochas): contorno fechado e interior preenchido.
PARTS = {
    "Splash_Logo": ("Sprites", (4, 4, 482, 382), "largest"),
    "Splash_King": ("Characters", (462, 4, 992, 420), "solid:4"),
    "Splash_Archer": ("Characters", (895, 20, 1206, 430), "largest"),
    "Splash_Mage": ("Characters", (1200, 20, 1532, 432), "largest"),
    "Splash_Army": ("Characters", (6, 384, 524, 628), "all"),
    "Splash_Castle": ("Backgrounds", (478, 400, 1134, 806), "solid:7"),
    "Splash_Mountains": ("Backgrounds", (1016, 428, 1532, 600), "solid:4"),
    "Splash_PineTree": ("Decorations", (1132, 592, 1262, 724), "largest"),
    "Splash_PineTree_2": ("Decorations", (1236, 602, 1312, 720), "largest"),
    "Splash_PineTree_3": ("Decorations", (1334, 597, 1440, 724), "largest"),
    "Splash_PineTree_4": ("Decorations", (1448, 598, 1528, 712), "largest"),
    "Splash_Rocks": ("Decorations", (1376, 708, 1520, 800), "solid"),
    "Splash_Rocks_2": ("Decorations", (1258, 718, 1368, 806), "solid"),
    "Splash_Rocks_3": ("Decorations", (1258, 938, 1388, 1020), "solid:12"),
    "Splash_Banner": ("Decorations", (8, 624, 158, 824), "largest"),
    "Splash_Banner_2": ("Decorations", (152, 658, 252, 810), "largest"),
    "Splash_Banner_3": ("Decorations", (266, 632, 330, 742), "largest"),
    "Splash_Banner_4": ("Decorations", (260, 736, 318, 824), "largest"),
    "Splash_Torch": ("Decorations", (338, 648, 410, 824), "largest"),
    "Splash_Torch_2": ("Decorations", (406, 616, 482, 824), "largest"),
    "Splash_Crown": ("Decorations", (538, 908, 690, 1018), "largest"),
    "Splash_Shield": ("Decorations", (694, 914, 786, 1020), "largest"),
    "Splash_CrystalBar": ("Decorations", (790, 920, 1074, 994), "largest"),
    "Splash_BannerSmall": ("Decorations", (1134, 930, 1210, 1020), "largest"),
    "Splash_Panel": ("Sprites", (502, 804, 952, 916), "largest"),
    "Splash_ProgressPanel": ("Sprites", (960, 800, 1532, 942), "largest"),
    "Splash_StartButton": ("Buttons", (14, 814, 502, 918), "largest"),
    "Splash_LoadingBar_Source": (None, (14, 917, 502, 992), "largest"),
}

# Retângulos (coordenadas da imagem) apagados antes do recorte: vizinhos que encostam no elemento.
EXCLUDE = {
    "Splash_Castle": [(1012, 400, 1134, 578)],
    "Splash_Mountains": [(1016, 540, 1066, 600)],
    "Splash_King": [(893, 0, 992, 250), (935, 250, 992, 324)],
}

# Faixa do texto "TOQUE PARA COMEÇAR" dentro do botão (coordenadas da imagem).
BUTTON_TEXT = (100, 852, 420, 896)
# Barra de carregamento: caixa, eixo de simetria (centro do cristal) e faixa vertical do interior.
BAR_BOX = (14, 917, 502, 992)
BAR_CENTER_X = 258
BAR_INNER_Y = (947, 975)
BAR_INNER_X = (40, 477)
BAR_GEM_X = (236, 281)


def checker_like(rgb):
    """Pixels candidatos a fundo: neutros, claros e com a textura alternada do quadriculado por perto.

    Neve e peles brancas são lisas (desvio local baixo); o quadriculado alterna ~205/253 a cada 11 px.
    """
    mx = rgb.max(2)
    mn = rgb.min(2).astype(np.float32)
    neutral = ((mx - mn) <= 18) & (mn >= 182)
    m = ndimage.uniform_filter(mn, 13)
    sd = np.sqrt(np.maximum(ndimage.uniform_filter(mn * mn, 13) - m * m, 0))
    textured = neutral & (sd >= 9)
    # a franja borrada entre quadrados e objeto também é fundo, se for neutra e encostar na textura
    return textured | (neutral & ndimage.binary_dilation(textured, iterations=2))


def cut(src, box, mode):
    x0, y0, x1, y1 = box
    rgb = src[y0:y1, x0:x1].astype(np.int32)
    h, w, _ = rgb.shape
    like = checker_like(rgb)
    lab, n = ndimage.label(like)
    bg = np.zeros((h, w), bool)
    if n:
        border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]]))) - {0}
        lum = rgb.min(2).astype(np.float32)
        idx = np.arange(1, n + 1)
        area = ndimage.sum(np.ones_like(lum), lab, idx)
        mean = ndimage.mean(lum, lab, idx)
        sq = ndimage.mean(lum * lum, lab, idx)
        std = np.sqrt(np.maximum(sq - mean * mean, 0))
        keep = np.zeros(n + 1, bool)
        for i in idx:
            if i in border or (area[i - 1] >= 120 and std[i - 1] >= 13):
                keep[i] = True
        bg = keep[lab]
    fg = ~bg
    if mode.startswith("solid"):
        # neve sem contorno encosta direto no quadriculado: parte do que é claramente objeto, fecha as falhas
        # da borda e preenche o interior (as calotas brancas ficam cercadas pela sombra azul).
        mx, mn = rgb.max(2), rgb.min(2)
        strict = ((mx - mn) > 22) | (mn < 175)
        strict = ndimage.binary_opening(strict, iterations=1)
        r = int(mode.split(":")[1]) if ":" in mode else 5
        disk = np.hypot(*np.mgrid[-r:r + 1, -r:r + 1]) <= r
        fg = ndimage.binary_fill_holes(ndimage.binary_closing(np.pad(strict, r + 1), disk))[r + 1:-r - 1, r + 1:-r - 1]
        mode = "largest"
    fg = ndimage.binary_opening(fg, iterations=1)
    flab, fn = ndimage.label(fg)
    if fn:
        sizes = ndimage.sum(fg, flab, np.arange(1, fn + 1))
        if mode == "largest":
            fg = flab == (int(np.argmax(sizes)) + 1)
        else:
            fg = np.isin(flab, np.where(sizes >= max(60, sizes.max() * 0.002))[0] + 1)
    # buracos pequenos do quadriculado que ficaram dentro (ex.: entre dedos) somem; buracos lisos permanecem.
    alpha = ndimage.binary_erosion(fg, iterations=1).astype(np.float32)
    alpha = np.array(Image.fromarray((alpha * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(0.9)))
    alpha = np.where(fg, np.maximum(alpha, 0), 0).astype(np.uint8)
    out = np.dstack([decontaminate(rgb, fg), alpha]).astype(np.uint8)
    img = Image.fromarray(out, "RGBA")
    bbox = img.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    bbox = bbox or (0, 0, w, h)
    return img.crop(bbox), (x0 + bbox[0], y0 + bbox[1])


def decontaminate(rgb, fg):
    """Pixels da franja (até 2 px da borda) recebem a cor do vizinho interior mais próximo."""
    inner = ndimage.binary_erosion(fg, iterations=2)
    if not inner.any():
        return rgb
    _, (iy, ix) = ndimage.distance_transform_edt(~inner, return_indices=True)
    fringe = fg & ~inner
    out = rgb.copy()
    out[fringe] = (rgb[fringe] * 0.35 + rgb[iy[fringe], ix[fringe]] * 0.65).astype(np.int32)
    return out


def erase_button_text(src):
    """Apaga o texto do botão copiando, linha a linha, uma coluna limpa do miolo azul."""
    px = src.copy()
    x0, y0, x1, y1 = BUTTON_TEXT
    left = px[y0:y1, x0 - 14:x0 - 4].astype(np.float32).mean(1)
    right = px[y0:y1, x1 + 4:x1 + 14].astype(np.float32).mean(1)
    for i, x in enumerate(range(x0, x1)):
        t = i / max(1, x1 - x0 - 1)
        px[y0:y1, x] = (left * (1 - t) + right * t).astype(np.uint8)
    return px


def build_bar(src):
    """Trilho vazio e preenchimento separados.

    A barra é simétrica em torno do cristal central e só a metade esquerda tem o preenchimento ciano, então o
    trilho vazio é a metade direita espelhada. O preenchimento é a faixa ciano limpa, esticada na horizontal.
    """
    px = src.copy()
    y0, y1 = BAR_BOX[1], BAR_BOX[3]
    c = BAR_CENTER_X
    for x in range(BAR_BOX[0], c):
        px[y0:y1, x] = src[y0:y1, 2 * c - x]
    yi0, yi1 = BAR_INNER_Y
    strip = src[yi0:yi1, 80:200].astype(np.float32).mean(1, keepdims=True)
    fill = np.repeat(strip, 64, axis=1).astype(np.uint8)
    return px, Image.fromarray(fill, "RGB").convert("RGBA")


def sky(width=1080, height=1920):
    """Céu de inverno em degradê com névoa e nuvens suaves (o pacote não traz céu)."""
    y = np.linspace(0, 1, height)[:, None]
    top = np.array([58, 112, 196], np.float32)
    mid = np.array([126, 182, 236], np.float32)
    low = np.array([214, 234, 250], np.float32)
    t1 = np.clip(y / 0.55, 0, 1)
    t2 = np.clip((y - 0.55) / 0.45, 0, 1)
    col = top * (1 - t1[..., None]) + mid * t1[..., None]
    col = col * (1 - t2[..., None]) + low * t2[..., None]
    img = np.repeat(col, width, axis=1)
    rng = np.random.default_rng(7)
    clouds = Image.new("L", (width, height), 0)
    d = ImageDraw.Draw(clouds)
    for _ in range(46):
        cx = rng.uniform(-100, width + 100)
        cy = rng.uniform(80, height * 0.55)
        rx = rng.uniform(90, 260)
        ry = rx * rng.uniform(0.25, 0.45)
        d.ellipse((cx - rx, cy - ry, cx + rx, cy + ry), fill=int(rng.uniform(40, 110)))
    clouds = np.array(clouds.filter(ImageFilter.GaussianBlur(38)), np.float32)[..., None] / 255.0
    img = img * (1 - clouds) + np.array([246, 250, 255], np.float32) * clouds
    return Image.fromarray(np.clip(img, 0, 255).astype(np.uint8), "RGB").resize((width // 2, height // 2), Image.LANCZOS)


def glow(size=128):
    """Brilho radial branco para reflexo do logotipo, partículas e luz das tochas."""
    r = np.linspace(-1, 1, size)
    d = np.sqrt(r[None, :] ** 2 + r[:, None] ** 2)
    a = np.clip(1 - d, 0, 1) ** 2.2
    return Image.fromarray(np.dstack([np.full((size, size, 3), 255), a * 255]).astype(np.uint8), "RGBA")


def shine(width=96, height=256):
    """Faixa diagonal de reflexo (passa sobre o logotipo de tempos em tempos)."""
    x = np.linspace(-1, 1, width)[None, :]
    a = np.clip(1 - np.abs(x) * 1.15, 0, 1) ** 1.6 * np.ones((height, 1))
    return Image.fromarray(np.dstack([np.full((height, width, 3), 255), a * 255]).astype(np.uint8), "RGBA")


def fog(width=64, height=256):
    """Faixa de névoa azulada (opaca no meio, some nas pontas): separa o fundo dos personagens."""
    y = np.linspace(-1, 1, height)[:, None]
    a = np.clip(1 - np.abs(y), 0, 1) ** 1.4 * np.ones((1, width))
    col = np.array([206, 228, 252])
    return Image.fromarray(np.dstack([np.broadcast_to(col, (height, width, 3)), a * 255]).astype(np.uint8), "RGBA")


def snow_ground(width=1024, height=256):
    """Chão de neve: monte largo com sombra azul por baixo e topo ondulado suave."""
    rng = np.random.default_rng(3)
    x = np.linspace(0, 1, width)
    wave = 0.18 + 0.05 * np.sin(x * 9.0 + 0.6) + 0.035 * np.sin(x * 23.0 + 1.7) + 0.02 * np.sin(x * 51.0)
    yy = np.linspace(0, 1, height)[:, None]
    top = wave[None, :]
    inside = yy >= top
    depth = np.clip((yy - top) / (1 - top), 0, 1)
    light = np.array([232, 242, 255], np.float32)
    shade = np.array([112, 152, 214], np.float32)
    col = light * (1 - depth[..., None] * 0.85) + shade * (depth[..., None] * 0.85)
    # montes suaves: faixas mais claras/escuras na horizontal quebram o branco chapado
    hills = 0.5 + 0.5 * np.sin(x[None, :] * 13.0 + yy * 4.0) * np.sin(x[None, :] * 5.0 + 1.1)
    col = col * (0.92 + 0.08 * hills[..., None])
    rim = np.clip(1 - (yy - top) * height / 6, 0, 1) * inside
    col = col * (1 - rim[..., None] * 0.25) + np.array([180, 208, 246]) * (rim[..., None] * 0.25)
    noise = rng.normal(0, 3, (height, width, 1))
    a = np.clip((yy - top) * height / 3, 0, 1)
    img = np.dstack([np.clip(col + noise, 0, 255), a * 255]).astype(np.uint8)
    return Image.fromarray(img, "RGBA").filter(ImageFilter.GaussianBlur(0.6))


def vignette(width=16, height=256):
    """Degradê vertical (transparente no topo, opaco embaixo): escurece a base atrás do botão."""
    y = np.linspace(0, 1, height)[:, None]
    a = (y ** 1.8) * np.ones((1, width))
    return Image.fromarray(np.dstack([np.full((height, width, 3), 255), a * 255]).astype(np.uint8), "RGBA")


def save(folder, name, img):
    path = os.path.join(OUT, folder)
    os.makedirs(path, exist_ok=True)
    img.save(os.path.join(path, name + ".png"), optimize=True)
    return os.path.join(folder, name + ".png")


def main():
    src = np.array(Image.open(SRC).convert("RGB"))
    assert src.shape[:2] == (1024, 1536), src.shape
    report = {}
    sheets = []

    btn_src = erase_button_text(src)
    bar_src, fill = build_bar(src)

    for name, (folder, box, mode) in PARTS.items():
        base = btn_src if name == "Splash_StartButton" else bar_src if name == "Splash_LoadingBar_Source" else src
        if name in EXCLUDE:
            base = base.copy()
            for (ex0, ey0, ex1, ey1) in EXCLUDE[name]:
                yy, xx = np.mgrid[ey0:ey1, ex0:ex1]
                base[ey0:ey1, ex0:ex1] = np.where((((yy // 11) + (xx // 11)) % 2 == 0)[..., None], 253, 207)
        img, origin = cut(base, box, mode)
        if name == "Splash_LoadingBar_Source":
            name, folder = "Splash_LoadingBar", "Sprites"
            bar_img, bar_origin = img, origin
        rel = save(folder, name, img)
        alpha = np.array(img.getchannel("A"))
        report[name] = {"file": rel.replace("\\", "/"), "size": list(img.size), "origin": list(origin),
                        "transparent_pct": round(float((alpha == 0).mean() * 100), 1)}
        sheets.append((name, img))

    # Interior da barra (onde corre o preenchimento), normalizado no sprite: x0, y0, x1, y1 com y de baixo p/ cima.
    bw, bh = bar_img.size
    ox, oy = bar_origin
    yi0, yi1 = BAR_INNER_Y
    inner = [(BAR_INNER_X[0] - ox) / bw, 1 - (yi1 - oy) / bh, (BAR_INNER_X[1] - ox) / bw, 1 - (yi0 - oy) / bh]
    report["Splash_LoadingBar"]["inner"] = [round(v, 4) for v in inner]
    # Cristal central recortado à parte: fica por cima do preenchimento.
    gx0, gx1 = BAR_GEM_X
    gem = bar_img.crop((gx0 - ox, 0, gx1 - ox, bh))
    gem_px = np.array(gem)
    rgbf = gem_px[..., :3].astype(np.int32)
    # mantém só o cristal/ornamento (fora do miolo azul-escuro uniforme)
    dark = (rgbf[..., 2] < 140) & (rgbf[..., 0] < 40) & (rgbf[..., 1] < 70)
    gem_px[..., 3] = np.where(dark, 0, gem_px[..., 3])
    gem_img = Image.fromarray(gem_px, "RGBA")
    report["Splash_LoadingBarGem"] = {"file": save("Sprites", "Splash_LoadingBarGem", gem_img).replace("\\", "/"),
                                      "size": list(gem_img.size),
                                      "rect": [round((gx0 - ox) / bw, 4), 0.0, round((gx1 - ox) / bw, 4), 1.0]}

    # preenchimento da barra com a mesma altura do interior
    fill_rel = save("Sprites", "Splash_LoadingBarFill", fill)
    report["Splash_LoadingBarFill"] = {"file": fill_rel.replace("\\", "/"), "size": list(fill.size), "transparent_pct": 0.0}
    sky_img = sky()
    report["Splash_Sky"] = {"file": save("Backgrounds", "Splash_Sky", sky_img).replace("\\", "/"), "size": list(sky_img.size),
                            "transparent_pct": 0.0, "generated": True}
    for name, img in (("Splash_Glow", glow()), ("Splash_Shine", shine()), ("Splash_Fog", fog()), ("Splash_SnowGround", snow_ground()), ("Splash_Vignette", vignette())):
        report[name] = {"file": save("Sprites", name, img).replace("\\", "/"), "size": list(img.size), "generated": True}

    with open(os.path.join(OUT, "Sprites", "splash_sprites.json"), "w", encoding="utf-8") as f:
        json.dump(report, f, indent=2, ensure_ascii=False)
    contact(sheets)
    for k, v in report.items():
        print(k, v)


def contact(items):
    """Folha de contato: cada sprite sobre fundo vermelho-escuro (mostra qualquer resto de quadriculado)."""
    cols, cell = 6, 260
    rows = (len(items) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cell, rows * (cell + 18)), (70, 16, 30))
    d = ImageDraw.Draw(sheet)
    for i, (name, img) in enumerate(items):
        th = img.copy()
        th.thumbnail((cell - 10, cell - 10), Image.LANCZOS)
        x, y = (i % cols) * cell, (i // cols) * (cell + 18)
        sheet.paste(th, (x + (cell - th.width) // 2, y + (cell - th.height) // 2), th)
        d.text((x + 4, y + cell + 2), name, fill=(255, 255, 255))
    os.makedirs(os.path.dirname(CONTACT), exist_ok=True)
    sheet.save(CONTACT)


if __name__ == "__main__":
    main()
