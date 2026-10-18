# Inventário dos 46 PNGs — Reino Congelado

Gerado por `tools/art/frozen_inventory.py`. Os PNGs são **referências visuais**, nunca usadas como textura: cada um orienta um modelo 3D
procedural (malha + materiais + prefab) ou uma câmera de comparação. Recortes limpos em `Assets/LegionRush/Worlds/FrozenKingdom/References/`
(`tools/art/clean_frozen_refs.py`: fundo quadriculado residual e pontos soltos removidos). Comparação lado a lado: `Builds/EnvPreview/comparacao_46_pngs.png`.

| # | Arquivo | Elemento | Categoria | Modelo 3D | Material | Prefab | Alfa | Estado |
|---|---|---|---|---|---|---|---|---|
| 1 | 01_castelo_principal.png | Castelo principal | Castle | EnvCastle.Castle + DoorLeaf | LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner | `FK_Castle_Main` | sim (445×392) | Modelo 3D |
| 2 | 02_torre_grande.png | Torre grande | Castle | EnvCastle.Tower | LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner | `FK_Castle_TowerLarge` | sim (109×231) | Modelo 3D |
| 3 | 03_torre_pequena.png | Torre pequena | Castle | EnvCastle.Tower | LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner | `FK_Castle_TowerSmall` | sim (107×196) | Modelo 3D |
| 4 | 04_muralha.png | Muralha com torres nas pontas | Castle | EnvCastle.Wall (endTowers) | LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner | `FK_Castle_Wall` | sim (159×167) | Modelo 3D |
| 5 | 05_portao.png | Portão | Castle | EnvCastle.Gatehouse + DoorLeaf | LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner | `FK_Castle_Gate` | sim (137×181) | Modelo 3D (portas abrem) |
| 6 | 06_arco.png | Arco de pedra | Castle | EnvMedieval.StoneArch | LR_Env_Masonry + LR_Env_Opaque | `FK_Castle_Arch` | sim (148×150) | Modelo 3D |
| 7 | 07_pilar_bandeira.png | Pilar com bandeira | Structures | EnvProps.BannerPole | LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner | `BannerPole / Banner_Large/Medium/Small` | sim (127×231) | Modelo 3D (tecido com vento) |
| 8 | 08_estatua.png | Estátua de cavaleiro | Structures | EnvMedieval.KnightStatue | LR_Env_Masonry + LR_Env_Opaque | `KnightStatue` | sim (122×216) | Modelo 3D |
| 9 | 09_tocha.png | Tocha (braseiro em pedestal) | Structures | EnvProps.Brazier | LR_Env_Opaque + LR_Env_Glow (+ partículas EnvFlameSpawner) | `TorchStand / FK_Castle_Torch` | sim (100×191) | Modelo 3D + fogo |
| 10 | 10_pinheiro_nevado.png | Pinheiro nevado | Nature | EnvVegetation.Pine | LR_Env_Opaque | `PineTree_Snow (+ Small/Large/Heavy)` | sim (95×134) | Modelo 3D, 4 variações |
| 11 | 11_rochas_nevadas.png | Rochas nevadas | Nature | EnvRocks.Rock / RockCluster | LR_Env_Opaque | `Rock_Snow / Rock_Snow_Large / Rock_Snow_Cluster` | sim (92×111) | Modelo 3D, 3 tamanhos |
| 12 | 12_pedras_pequenas.png | Pedras pequenas (decal) | Props | EnvRocks.PebbleScatter | LR_Env_Opaque | `Stone_Decal` | sim (72×71) | Modelo 3D (malha fina) |
| 13 | 13_neve_decal.png | Neve (decal) | Props | EnvRocks.SnowPatch / EnvVegetation.SnowDrift | LR_Env_Opaque | `Snow_Decal / Snow_Pile` | sim (121×118) | Modelo 3D (malha fina) |
| 14 | 14_cerca.png | Cerca | Props | EnvMedieval.Fence | LR_Env_Opaque | `Fence_Wood` | sim (95×109) | Modelo 3D |
| 15 | 15_barril.png | Barril | Props | EnvProps.Barrel | LR_Env_Opaque | `Barrel_Wood` | sim (72×99) | Modelo 3D |
| 16 | 16_caixa.png | Caixa | Props | EnvProps.Crate | LR_Env_Opaque | `Crate_Wood` | sim (85×95) | Modelo 3D |
| 17 | 17_carroca.png | Carroça | Props | EnvMedieval.Cart | LR_Env_Opaque | `Cart_Wood` | sim (121×104) | Modelo 3D |
| 18 | 18_tenda.png | Tenda | Props | EnvMedieval.Tent | LR_Env_Opaque | `Tent_Blue` | sim (120×123) | Modelo 3D |
| 19 | 19_fogueira.png | Fogueira | Props | EnvMedieval.Campfire | LR_Env_Opaque + LR_Env_Glow (+ partículas EnvFlameSpawner) | `Campfire` | sim (90×130) | Modelo 3D + fogo |
| 20 | 20_lamparina.png | Lamparina | Structures | EnvMedieval.Lantern | LR_Env_Opaque + LR_Env_Glow (+ partículas EnvFlameSpawner) | `Lantern` | sim (93×120) | Modelo 3D |
| 21 | 21_portal_azul.png | Portal azul | Gates | EnvGatePortal (Defense) | LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner + LR_Gate_Curtain + LR_Gate_Spark | `FK_Gate_Blue` | sim (142×154) | Modelo 3D + trigger + partículas |
| 22 | 22_portal_verde.png | Portal verde | Gates | EnvGatePortal (Multiply) | LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner + LR_Gate_Curtain + LR_Gate_Spark | `FK_Gate_Green` | sim (131×154) | Modelo 3D + trigger + partículas |
| 23 | 23_portal_amarelo.png | Portal amarelo | Gates | EnvGatePortal (Resource) | LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner + LR_Gate_Curtain + LR_Gate_Spark | `FK_Gate_Yellow` | sim (137×154) | Modelo 3D + trigger + partículas |
| 24 | 24_portal_roxo.png | Portal roxo | Gates | EnvGatePortal (Special) | LR_Env_Masonry + LR_Env_Opaque + LR_Env_Glow + LR_Env_Banner + LR_Gate_Curtain + LR_Gate_Spark | `FK_Gate_Purple` | sim (132×154) | Modelo 3D + trigger + partículas |
| 25 | 25_soldado_azul.png | Soldado básico azul | Characters | FBX (Blender) | materiais dos FBX (sessão de personagens) | `Ally_Warrior` | sim (106×157) | Personagens (sessão paralela) |
| 26 | 26_arqueiro_azul.png | Arqueiro azul | Characters | FBX (Blender) | materiais dos FBX (sessão de personagens) | `Ally_RoyalArcher` | sim (122×154) | Personagens (sessão paralela) |
| 27 | 27_lanceiro_azul.png | Lanceiro azul | Characters | FBX (Blender) | materiais dos FBX (sessão de personagens) | `Ally_Spearman` | sim (100×157) | Personagens (sessão paralela) |
| 28 | 28_capitao_azul.png | Capitão azul | Characters | FBX (Blender) | materiais dos FBX (sessão de personagens) | `Ally_RoyalCommander` | sim (128×161) | Personagens (sessão paralela) |
| 29 | 29_soldado_vermelho.png | Soldado vermelho | Characters | FBX (Blender) | materiais dos FBX (sessão de personagens) | `Enemy_Soldier` | sim (111×159) | Personagens (sessão paralela) |
| 30 | 30_arqueiro_vermelho.png | Arqueiro vermelho | Characters | FBX (Blender) | materiais dos FBX (sessão de personagens) | `Enemy_Archer` | sim (115×160) | Personagens (sessão paralela) |
| 31 | 31_lanceiro_vermelho.png | Lanceiro vermelho | Characters | FBX (Blender) | materiais dos FBX (sessão de personagens) | `Enemy_Pikeman` | sim (119×163) | Personagens (sessão paralela) |
| 32 | 32_capitao_vermelho.png | Capitão vermelho | Characters | — | materiais dos FBX (sessão de personagens) | `—` | sim (149×161) | Pendente: sem modelo de capitão vermelho |
| 33 | 33_piso_reto.png | Piso reto | Tracks | EnvTrackModules.Straight (sem borda) | LR_Env_Road + LR_Env_Masonry + LR_Env_Opaque | `Track_Straight` | sim (164×117) | Modelo 3D + collider |
| 34 | 34_piso_com_borda.png | Piso com borda | Tracks | EnvTrackModules.Straight | LR_Env_Road + LR_Env_Masonry + LR_Env_Opaque | `Track_Bordered` | sim (157×119) | Modelo 3D + colliders |
| 35 | 35_rampa.png | Rampa | Tracks | EnvTrackModules.Ramp | LR_Env_Road + LR_Env_Masonry + LR_Env_Opaque | `Track_Ramp` | sim (180×118) | Modelo 3D + collider |
| 36 | 36_plataforma.png | Plataforma | Tracks | EnvTrackModules.Platform | LR_Env_Road + LR_Env_Masonry + LR_Env_Opaque | `Track_Platform` | sim (182×117) | Modelo 3D + collider |
| 37 | 37_cruzamento.png | Cruzamento | Tracks | EnvTrackModules.Crossroad | LR_Env_Road + LR_Env_Masonry + LR_Env_Opaque | `Track_Crossroad` | sim (224×123) | Modelo 3D + colliders |
| 38 | 38_curva.png | Curva | Tracks | EnvTrackModules.Curve | LR_Env_Road + LR_Env_Masonry + LR_Env_Opaque | `Track_Curve` | sim (184×119) | Modelo 3D + colliders (90°) |
| 39 | 39_escada.png | Escada | Tracks | EnvTrackModules.Stairs | LR_Env_Road + LR_Env_Masonry + LR_Env_Opaque | `Track_Stairs` | sim (116×116) | Modelo 3D + collider |
| 40 | 40_plataforma_bloco.png | Plataforma (bloco) | Tracks | EnvTrackModules.PlatformBlock | LR_Env_Road + LR_Env_Masonry + LR_Env_Opaque | `Track_PlatformBlock` | sim (136×118) | Modelo 3D |
| 41 | 41_plataforma_elevada.png | Plataforma elevada | Tracks | EnvTrackModules.Platform (1,6 m) | LR_Env_Road + LR_Env_Masonry + LR_Env_Opaque | `Track_Elevated` | sim (156×115) | Modelo 3D + collider |
| 42 | 42_vista_geral.png | Vista geral | Views | cena FrozenKingdom_01 | — | `Cam_VistaGeral` | sim (336×164) | Referência de câmera |
| 43 | 43_vista_frontal.png | Vista frontal | Views | cena FrozenKingdom_01 | — | `Cam_VistaFrontal` | sim (287×164) | Referência de câmera |
| 44 | 44_vista_lateral.png | Vista lateral | Views | cena FrozenKingdom_01 | — | `Cam_VistaLateral` | sim (289×164) | Referência de câmera |
| 45 | 45_vista_aerea.png | Vista aérea | Views | cena FrozenKingdom_01 | — | `Cam_VistaAerea` | sim (277×164) | Referência de câmera |
| 46 | 46_vista_spawn.png | Vista traseira (spawn) | Views | cena FrozenKingdom_01 | — | `Cam_VistaTraseira` | sim (306×164) | Referência de câmera |
