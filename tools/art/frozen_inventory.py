"""Inventário dos 46 PNGs do Reino Congelado + folha de comparação PNG × render do Unity.

Uso: python -I tools/art/frozen_inventory.py
Lê:   Assets/LegionRush/Worlds/FrozenKingdom/References/*.png (recortes limpos)
      Builds/EnvPreview/Prefabs/<prefab>.png (miniaturas: EnvPreviewCapture.CapturePrefabsBatch)
      Builds/EnvPreview/scene3/<camera>.png (vistas: FrozenKingdomSceneBuilder.CaptureBatch)
Grava: docs/reino_congelado/INVENTARIO_46_PNGS.md e Builds/EnvPreview/comparacao_46_pngs.png
"""
import os

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
REFS = os.path.join(ROOT, "Assets/LegionRush/Worlds/FrozenKingdom/References")
THUMBS = os.path.join(ROOT, "Builds/EnvPreview/Prefabs")
VIEWS = os.path.join(ROOT, "Builds/EnvPreview/scene3")

CHAR = "Personagens (sessão paralela)"
MATS = {
    "castle": "LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner",
    "stone": "LR_Env_Masonry + LR_Env_Opaque",
    "nature": "LR_Env_Opaque",
    "ice": "LR_Env_Opaque + LR_Env_Ice",
    "wood": "LR_Env_Opaque",
    "fire": "LR_Env_Opaque + LR_Env_Glow (+ partículas EnvFlameSpawner)",
    "gate": "LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner + LR_Gate_Curtain + LR_Gate_Spark",
    "track": "LR_Env_Road + LR_Env_Masonry + LR_Env_Opaque",
    "char": "materiais dos FBX (sessão de personagens)",
    "view": "—",
}

# arquivo, elemento, categoria, modelo/função, chave de material, prefab, estado
ITEMS = [
    ("01_castelo_principal", "Castelo principal", "Castle", "EnvCastle.Castle + DoorLeaf", "castle", "FK_Castle_Main", "Modelo 3D"),
    ("02_torre_grande", "Torre grande", "Castle", "EnvCastle.Tower", "castle", "FK_Castle_TowerLarge", "Modelo 3D"),
    ("03_torre_pequena", "Torre pequena", "Castle", "EnvCastle.Tower", "castle", "FK_Castle_TowerSmall", "Modelo 3D"),
    ("04_muralha", "Muralha com torres nas pontas", "Castle", "EnvCastle.Wall (endTowers)", "castle", "FK_Castle_Wall", "Modelo 3D"),
    ("05_portao", "Portão", "Castle", "EnvCastle.Gatehouse + DoorLeaf", "castle", "FK_Castle_Gate", "Modelo 3D (portas abrem)"),
    ("06_arco", "Arco de pedra", "Castle", "EnvMedieval.StoneArch", "stone", "FK_Castle_Arch", "Modelo 3D"),
    ("07_pilar_bandeira", "Pilar com bandeira", "Structures", "EnvProps.BannerPole", "castle", "BannerPole / Banner_Large/Medium/Small", "Modelo 3D (tecido com vento)"),
    ("08_estatua", "Estátua de cavaleiro", "Structures", "EnvMedieval.KnightStatue", "stone", "KnightStatue", "Modelo 3D"),
    ("09_tocha", "Tocha (braseiro em pedestal)", "Structures", "EnvProps.Brazier", "fire", "TorchStand / FK_Castle_Torch", "Modelo 3D + fogo"),
    ("10_pinheiro_nevado", "Pinheiro nevado", "Nature", "EnvVegetation.Pine", "nature", "PineTree_Snow (+ Small/Large/Heavy)", "Modelo 3D, 4 variações"),
    ("11_rochas_nevadas", "Rochas nevadas", "Nature", "EnvRocks.Rock / RockCluster", "nature", "Rock_Snow / Rock_Snow_Large / Rock_Snow_Cluster", "Modelo 3D, 3 tamanhos"),
    ("12_pedras_pequenas", "Pedras pequenas (decal)", "Props", "EnvRocks.PebbleScatter", "nature", "Stone_Decal", "Modelo 3D (malha fina)"),
    ("13_neve_decal", "Neve (decal)", "Props", "EnvRocks.SnowPatch / EnvVegetation.SnowDrift", "nature", "Snow_Decal / Snow_Pile", "Modelo 3D (malha fina)"),
    ("14_cerca", "Cerca", "Props", "EnvMedieval.Fence", "wood", "Fence_Wood", "Modelo 3D"),
    ("15_barril", "Barril", "Props", "EnvProps.Barrel", "wood", "Barrel_Wood", "Modelo 3D"),
    ("16_caixa", "Caixa", "Props", "EnvProps.Crate", "wood", "Crate_Wood", "Modelo 3D"),
    ("17_carroca", "Carroça", "Props", "EnvMedieval.Cart", "wood", "Cart_Wood", "Modelo 3D"),
    ("18_tenda", "Tenda", "Props", "EnvMedieval.Tent", "wood", "Tent_Blue", "Modelo 3D"),
    ("19_fogueira", "Fogueira", "Props", "EnvMedieval.Campfire", "fire", "Campfire", "Modelo 3D + fogo"),
    ("20_lamparina", "Lamparina", "Structures", "EnvMedieval.Lantern", "fire", "Lantern", "Modelo 3D"),
    ("21_portal_azul", "Portal azul", "Gates", "EnvGatePortal (Defense)", "gate", "FK_Gate_Blue", "Modelo 3D + trigger + partículas"),
    ("22_portal_verde", "Portal verde", "Gates", "EnvGatePortal (Multiply)", "gate", "FK_Gate_Green", "Modelo 3D + trigger + partículas"),
    ("23_portal_amarelo", "Portal amarelo", "Gates", "EnvGatePortal (Resource)", "gate", "FK_Gate_Yellow", "Modelo 3D + trigger + partículas"),
    ("24_portal_roxo", "Portal roxo", "Gates", "EnvGatePortal (Special)", "gate", "FK_Gate_Purple", "Modelo 3D + trigger + partículas"),
    ("25_soldado_azul", "Soldado básico azul", "Characters", "FBX (Blender)", "char", "Ally_Warrior", CHAR),
    ("26_arqueiro_azul", "Arqueiro azul", "Characters", "FBX (Blender)", "char", "Ally_RoyalArcher", CHAR),
    ("27_lanceiro_azul", "Lanceiro azul", "Characters", "FBX (Blender)", "char", "Ally_Spearman", CHAR),
    ("28_capitao_azul", "Capitão azul", "Characters", "FBX (Blender)", "char", "Ally_RoyalCommander", CHAR),
    ("29_soldado_vermelho", "Soldado vermelho", "Characters", "FBX (Blender)", "char", "Enemy_Soldier", CHAR),
    ("30_arqueiro_vermelho", "Arqueiro vermelho", "Characters", "FBX (Blender)", "char", "Enemy_Archer", CHAR),
    ("31_lanceiro_vermelho", "Lanceiro vermelho", "Characters", "FBX (Blender)", "char", "Enemy_Pikeman", CHAR),
    ("32_capitao_vermelho", "Capitão vermelho", "Characters", "—", "char", "—", "Pendente: sem modelo de capitão vermelho"),
    ("33_piso_reto", "Piso reto", "Tracks", "EnvTrackModules.Straight (sem borda)", "track", "Track_Straight", "Modelo 3D + collider"),
    ("34_piso_com_borda", "Piso com borda", "Tracks", "EnvTrackModules.Straight", "track", "Track_Bordered", "Modelo 3D + colliders"),
    ("35_rampa", "Rampa", "Tracks", "EnvTrackModules.Ramp", "track", "Track_Ramp", "Modelo 3D + collider"),
    ("36_plataforma", "Plataforma", "Tracks", "EnvTrackModules.Platform", "track", "Track_Platform", "Modelo 3D + collider"),
    ("37_cruzamento", "Cruzamento", "Tracks", "EnvTrackModules.Crossroad", "track", "Track_Crossroad", "Modelo 3D + colliders"),
    ("38_curva", "Curva", "Tracks", "EnvTrackModules.Curve", "track", "Track_Curve", "Modelo 3D + colliders (90°)"),
    ("39_escada", "Escada", "Tracks", "EnvTrackModules.Stairs", "track", "Track_Stairs", "Modelo 3D + collider"),
    ("40_plataforma_bloco", "Plataforma (bloco)", "Tracks", "EnvTrackModules.PlatformBlock", "track", "Track_PlatformBlock", "Modelo 3D"),
    ("41_plataforma_elevada", "Plataforma elevada", "Tracks", "EnvTrackModules.Platform (1,6 m)", "track", "Track_Elevated", "Modelo 3D + collider"),
    ("42_vista_geral", "Vista geral", "Views", "cena FrozenKingdom_01", "view", "Cam_VistaGeral", "Referência de câmera"),
    ("43_vista_frontal", "Vista frontal", "Views", "cena FrozenKingdom_01", "view", "Cam_VistaFrontal", "Referência de câmera"),
    ("44_vista_lateral", "Vista lateral", "Views", "cena FrozenKingdom_01", "view", "Cam_VistaLateral", "Referência de câmera"),
    ("45_vista_aerea", "Vista aérea", "Views", "cena FrozenKingdom_01", "view", "Cam_VistaAerea", "Referência de câmera"),
    ("46_vista_spawn", "Vista traseira (spawn)", "Views", "cena FrozenKingdom_01", "view", "Cam_VistaTraseira", "Referência de câmera"),
]


def render_for(prefab_field, category):
    first = prefab_field.split(" ")[0]
    folder = VIEWS if category == "Views" else THUMBS
    path = os.path.join(folder, first + ".png")
    return path if os.path.exists(path) else None


def main():
    lines = [
        "# Inventário dos 46 PNGs — Reino Congelado",
        "",
        "Gerado por `tools/art/frozen_inventory.py`. Os PNGs são **referências visuais**, nunca usadas como textura: cada um orienta um modelo 3D",
        "procedural (malha + materiais + prefab) ou uma câmera de comparação. Recortes limpos em `Assets/LegionRush/Worlds/FrozenKingdom/References/`",
        "(`tools/art/clean_frozen_refs.py`: fundo quadriculado residual e pontos soltos removidos). Comparação lado a lado: `Builds/EnvPreview/comparacao_46_pngs.png`.",
        "",
        "| # | Arquivo | Elemento | Categoria | Modelo 3D | Material | Prefab | Alfa | Estado |",
        "|---|---|---|---|---|---|---|---|---|",
    ]
    pairs = []
    for i, (name, element, cat, model, mat, prefab, state) in enumerate(ITEMS, 1):
        ref = os.path.join(REFS, name + ".png")
        alpha = "—"
        if os.path.exists(ref):
            im = Image.open(ref)
            alpha = f"sim ({im.width}×{im.height})" if im.mode == "RGBA" else "não"
        lines.append(f"| {i} | {name}.png | {element} | {cat} | {model} | {MATS[mat]} | `{prefab}` | {alpha} | {state} |")
        pairs.append((name, ref, render_for(prefab, cat), element))
    with open(os.path.join(ROOT, "docs/reino_congelado/INVENTARIO_46_PNGS.md"), "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")

    cell, cols = 150, 6
    rows = (len(pairs) + cols - 1) // cols
    sheet = Image.new("RGB", (cols * cell * 2, rows * (cell + 14)), (24, 28, 40))
    draw = ImageDraw.Draw(sheet)
    for i, (name, ref, render, element) in enumerate(pairs):
        x = (i % cols) * cell * 2
        y = (i // cols) * (cell + 14)
        for k, path in enumerate((ref, render)):
            if not path or not os.path.exists(path):
                draw.text((x + k * cell + 40, y + 60), "—", fill=(160, 160, 160))
                continue
            im = Image.open(path).convert("RGBA")
            im.thumbnail((cell - 6, cell - 6))
            sheet.paste(im, (x + k * cell + (cell - im.width) // 2, y + (cell - im.height) // 2), im)
        draw.text((x + 3, y + cell), f"{name[:2]} {element[:28]}", fill="white")
    sheet.save(os.path.join(ROOT, "Builds/EnvPreview/comparacao_46_pngs.png"))
    print("ok", len(pairs))


if __name__ == "__main__":
    main()
