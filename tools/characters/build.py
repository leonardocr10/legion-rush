"""Builds one character in Blender (headless) and exports it to the Unity project.

  blender -b --factory-startup -P tools/characters/build.py -- --char warrior [--no-render] [--no-strips]

Outputs:
  Assets/LegionRush/Characters/<Allies|Enemies>/<Name>/Models/<Name>.fbx (+ .json metadata)
  Assets/LegionRush/Characters/Weapons/Models/<WeaponId>.fbx
  Assets/LegionRush/Characters/Textures/LR_CharacterAtlas(.png|_Emission.png)
  Builds/CharacterPreview/<Name>/ (turnaround + animation frames)
"""
import argparse
import importlib
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, HERE)

from lr import character  # noqa: E402

ROSTER = {
    "warrior": "Allies",
    "archer": "Allies",
    "commander": "Allies",
    "mage": "Allies",
    "knight": "Allies",
    "lancer": "Allies",
    "healer": "Allies",
    "enemy_soldier": "Enemies",
    "enemy_archer": "Enemies",
    "berserker": "Enemies",
    "heavy_warrior": "Enemies",
    "pikeman": "Enemies",
    "dark_mage": "Enemies",
}


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    ap = argparse.ArgumentParser()
    ap.add_argument("--char", required=True)
    ap.add_argument("--no-render", action="store_true")
    ap.add_argument("--no-strips", action="store_true")
    ap.add_argument("--stage", action="store_true",
                    help="write into Builds/CharacterStage (copy to Assets later, e.g. while Unity is running)")
    args = ap.parse_args(argv)

    character.setup_scene()
    mod = importlib.import_module("lr.chars." + args.char)
    b = mod.build()
    group = ROSTER[args.char]
    base = os.path.join(ROOT, "Builds", "CharacterStage") if args.stage else ROOT
    chars = os.path.join(base, "Assets", "LegionRush", "Characters")
    out_dir = os.path.join(chars, group, b.name, "Models")
    weapons_dir = os.path.join(chars, "Weapons", "Models")
    preview_dir = os.path.join(ROOT, "Builds", "CharacterPreview", b.name)
    body, lod1 = character.finish(b, out_dir, weapons_dir, preview_dir, render_previews=not args.no_render,
                                  strips=not args.no_strips)
    print("BUILD_OK", b.name, b.meta["stats"])


if __name__ == "__main__":
    main()
