"""Limpa os 46 PNGs de referência do Reino Congelado e copia para o projeto.

Uso: python -I tools/art/clean_frozen_refs.py <pasta_dos_pngs> <pasta_destino>

Os recortes vieram com resto do fundo quadriculado: pontinhos claros soltos em volta do objeto e, em
alguns arquivos, cantos ainda opacos. A limpeza:
1. torna transparente o fundo claro e sem saturação ligado à borda (flood fill a partir das bordas);
2. mantém só os componentes opacos grandes (descarta os pontinhos soltos);
3. grava em <destino> com o mesmo nome e devolve um relatório por arquivo (tamanho, % de pixels removidos).
As imagens são só referência visual: nada no jogo as usa como textura.
"""
import os
import sys
from collections import deque

from PIL import Image


def is_background(r, g, b, a):
    if a < 16:
        return True
    lo, hi = min(r, g, b), max(r, g, b)
    return lo > 185 and hi - lo < 14  # cinza claro/branco do quadriculado


def clean(path_in, path_out):
    im = Image.open(path_in).convert("RGBA")
    w, h = im.size
    px = im.load()
    seen = bytearray(w * h)
    queue = deque()
    for x in range(w):
        queue.append((x, 0))
        queue.append((x, h - 1))
    for y in range(h):
        queue.append((0, y))
        queue.append((w - 1, y))
    removed = 0
    while queue:
        x, y = queue.popleft()
        i = y * w + x
        if seen[i]:
            continue
        seen[i] = 1
        r, g, b, a = px[x, y]
        if not is_background(r, g, b, a):
            continue
        if a:
            removed += 1
        px[x, y] = (r, g, b, 0)
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            nx, ny = x + dx, y + dy
            if 0 <= nx < w and 0 <= ny < h and not seen[ny * w + nx]:
                queue.append((nx, ny))

    # Componentes opacos: mantém os que têm pelo menos 2% do maior.
    label = [0] * (w * h)
    sizes = [0]
    for y in range(h):
        for x in range(w):
            if px[x, y][3] < 16 or label[y * w + x]:
                continue
            n = len(sizes)
            count = 0
            stack = [(x, y)]
            label[y * w + x] = n
            while stack:
                cx, cy = stack.pop()
                count += 1
                for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                    nx, ny = cx + dx, cy + dy
                    if 0 <= nx < w and 0 <= ny < h and not label[ny * w + nx] and px[nx, ny][3] >= 16:
                        label[ny * w + nx] = n
                        stack.append((nx, ny))
            sizes.append(count)
    biggest = max(sizes) if len(sizes) > 1 else 0
    specks = 0
    for y in range(h):
        for x in range(w):
            n = label[y * w + x]
            if n and sizes[n] < biggest * 0.02:
                r, g, b, _ = px[x, y]
                px[x, y] = (r, g, b, 0)
                specks += 1
    im.save(path_out)
    return w, h, removed, specks


def main():
    src, dst = sys.argv[1], sys.argv[2]
    os.makedirs(dst, exist_ok=True)
    for name in sorted(os.listdir(src)):
        if not name.lower().endswith(".png"):
            continue
        w, h, removed, specks = clean(os.path.join(src, name), os.path.join(dst, name))
        print(f"{name};{w}x{h};fundo_removido={removed};pontos_soltos={specks}")


if __name__ == "__main__":
    main()
