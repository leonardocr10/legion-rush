"""Composes Blender preview renders into sheet.png (turnaround) and anims.png (6 frames per clip).
python tools/characters/sheet.py <Name>"""
import glob
import os
import re
import sys

from PIL import Image

root = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Builds", "CharacterPreview", sys.argv[1])
views = [os.path.join(root, f"{sys.argv[1]}_{v}.png") for v in ("front", "side", "back", "perspective")]
ims = [Image.open(v) for v in views if os.path.exists(v)]
if ims:
    o = Image.new("RGB", (sum(i.width for i in ims), ims[0].height))
    x = 0
    for i in ims:
        o.paste(i, (x, 0))
        x += i.width
    o.save(os.path.join(root, "sheet.png"))
clips = sorted({re.match(r"anim_(.+)_\d+\.png", os.path.basename(p)).group(1)
                for p in glob.glob(os.path.join(root, "anim", "anim_*_0.png"))},
               key=lambda n: os.path.getmtime(os.path.join(root, "anim", f"anim_{n}_0.png")))
if clips:
    w, h = 120, 152
    sheet = Image.new("RGB", (w * 6, h * len(clips)), (20, 30, 60))
    for r, n in enumerate(clips):
        for i in range(6):
            p = os.path.join(root, "anim", f"anim_{n}_{i}.png")
            if os.path.exists(p):
                sheet.paste(Image.open(p).resize((w, h)), (i * w, r * h))
    sheet.save(os.path.join(root, "anims.png"))
    print("clips:", ", ".join(clips))
