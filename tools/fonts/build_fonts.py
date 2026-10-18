"""Gera as fontes do jogo em Assets/Fonts/Source a partir dos originais (Google Fonts, licença OFL) em tools/fonts/original.

Fredoka e Baloo 2 são fontes variáveis; o Unity só usa a instância padrão, então geramos instâncias estáticas
(Bold/Medium) e reduzimos ao conjunto Latin (português + inglês + símbolos do jogo) para builds menores.
Uso: python tools/fonts/build_fonts.py   (requer: pip install fonttools)
"""
import os
import shutil

from fontTools import subset
from fontTools.ttLib import TTFont
from fontTools.varLib import instancer

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ORIGINAL = os.path.join(ROOT, "tools", "fonts", "original")
OUT = os.path.join(ROOT, "Assets", "Fonts", "Source")

# Latin básico + Latin-1 (acentos PT) + pontuação geral + × − • … € ★ etc.
UNICODES = "U+0020-007E,U+00A0-00FF,U+0152-0153,U+2013-2014,U+2018-201E,U+2022,U+2026,U+20AC,U+00D7,U+2212,U+2190-2193,U+2605,U+2665"

INSTANCES = [
    ("Fredoka[wdth,wght].ttf", "Fredoka/Fredoka-Bold.ttf", {"wght": 700, "wdth": 100}, "Bold"),
    ("Fredoka[wdth,wght].ttf", "Fredoka/Fredoka-Medium.ttf", {"wght": 500, "wdth": 100}, "Medium"),
    ("Baloo2[wght].ttf", "Baloo2/Baloo2-Bold.ttf", {"wght": 700}, "Bold"),
    ("Baloo2[wght].ttf", "Baloo2/Baloo2-ExtraBold.ttf", {"wght": 800}, "ExtraBold"),
]


def subset_font(font):
    options = subset.Options()
    options.layout_features = ["*"]
    options.name_IDs = ["*"]
    options.notdef_outline = True
    sub = subset.Subsetter(options)
    sub.populate(unicodes=subset.parse_unicodes(UNICODES))
    sub.subset(font)


def rename(font, style):
    family = font["name"].getDebugName(1)
    for record in font["name"].names:
        if record.nameID in (2, 17):
            record.string = style
        elif record.nameID in (4,):
            record.string = f"{family} {style}"
        elif record.nameID == 6:
            record.string = f"{family.replace(' ', '')}-{style}"


def main():
    for source, target, axes, style in INSTANCES:
        font = instancer.instantiateVariableFont(TTFont(os.path.join(ORIGINAL, source)), axes)
        rename(font, style)
        subset_font(font)
        path = os.path.join(OUT, target)
        os.makedirs(os.path.dirname(path), exist_ok=True)
        font.save(path)
        print(target, os.path.getsize(path))
    shutil.copy(os.path.join(ORIGINAL, "LilitaOne-Regular.ttf"), os.path.join(OUT, "LilitaOne-Regular.ttf"))
    for name in ("OFL-LilitaOne.txt", "OFL-Fredoka.txt", "OFL-Baloo2.txt"):
        shutil.copy(os.path.join(ORIGINAL, name), os.path.join(OUT, name))


if __name__ == "__main__":
    main()
