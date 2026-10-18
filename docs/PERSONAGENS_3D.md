# Personagens 3D — pipeline e estado

Os 13 personagens (7 aliados e 6 inimigos) são **modelos 3D reais**: malha, UV, textura, esqueleto, pesos,
blend shapes faciais, LODs e animações. Todos são gerados por script no **Blender 4.5.14 LTS** (portátil,
`D:\tools\blender-4.5.14-windows-x64`), exportados em FBX e montados como prefabs no Unity por ferramentas de Editor.

> Limitação honesta: os modelos são **modelados por código** (formas paramétricas: lofts, tornos, extrusões,
> subdivisão), não esculpidos à mão. Eles reproduzem silhueta, proporções, cores, armaduras, armas e acessórios
> das folhas de referência, mas não têm o nível de detalhe de escultura e textura pintada das imagens.
> Para chegar a esse nível é preciso um artista (ou ferramenta de IA imagem→3D) refinando estes mesmos arquivos;
> o rig, os nomes de ossos, os encaixes e o pipeline do Unity continuam valendo.

## Como gerar

```
blender -b --factory-startup -P tools/characters/build.py -- --char warrior          # um personagem
blender -b --factory-startup -P tools/characters/build.py -- --char warrior --stage  # grava em Builds/CharacterStage (Unity aberto)
python tools/characters/sheet.py Warrior                                             # folhas de prévia do Blender
```

Personagens: `warrior archer commander mage knight lancer healer enemy_soldier enemy_archer berserker
heavy_warrior pikeman dark_mage`.

No Unity: **Legion Rush › Personagens › Montar prefabs** (batch:
`-executeMethod LegionRush.Characters.EditorTools.CharacterPrefabBuilder.BuildBatch`) e
**Legion Rush › Personagens › Capturar prévias** (`CharacterPreviewCapture.CaptureBatch -captureDir <pasta> [-only Warrior]`).

## Código

| Parte | Arquivo |
|---|---|
| Kit de modelagem (loft, torno, extrusão, tubos, placas, subdivisão) | `tools/characters/lr/mesh.py` |
| Paleta/atlas compartilhado (1 material para todos) | `tools/characters/lr/palette.py` |
| Corpo, rosto expressivo, cabelo | `tools/characters/lr/humanoid.py` |
| Armaduras, capacetes, capas, capuzes, coroa | `tools/characters/lr/armor.py` |
| Armas e equipamentos | `tools/characters/lr/weapons.py` |
| Esqueleto, pesos, poses | `tools/characters/lr/rig.py` |
| Animações | `tools/characters/lr/anim.py` (+ clips próprios em `chars/knight.py`, `chars/berserker.py`) |
| Montagem, LOD, exportação, prévias | `tools/characters/lr/character.py`, `finalize.py`, `render.py` |
| Um módulo por personagem | `tools/characters/lr/chars/*.py` |
| Importação (Generic, 2 ossos/vértice, clips, loop) | `Assets/LegionRush/Characters/Editor/CharacterAssetPostprocessor.cs` |
| Prefabs, Animator Controllers, biblioteca | `Assets/LegionRush/Characters/Editor/CharacterPrefabBuilder.cs` |
| Capturas de validação | `Assets/LegionRush/Characters/Editor/CharacterPreviewCapture.cs` |
| Encaixes em runtime | `Assets/_Game/Scripts/Characters/CharacterSockets.cs` |
| Tipos de inimigo | `Assets/_Game/Scripts/Enemies/EnemyKinds.cs`, `Enemy.cs` |

## Arquivos gerados

```
Assets/LegionRush/Characters/
  Allies/<Nome>/Models/<Nome>.fbx (+ .json: clips, slots, encaixes, polígonos)
  Allies/<Nome>/Controllers/<Nome>.controller
  Allies/<Nome>/Prefabs/Ally_<Nome>.prefab
  Enemies/<Nome>/...                         (Enemy_*.prefab)
  Weapons/Models/Weapon_*.fbx, Prop_*.fbx    (armas e acessórios independentes, pivô na empunhadura)
  Weapons/Prefabs/*.prefab
  Textures/LR_CharacterAtlas.png, LR_CharacterAtlas_Emission.png
  Materials/LR_CharacterAtlas.mat            (LegionRush/ToonLit, instancing)
  Resources/CharacterVisualLibrary.asset     (lida pelo GameBootstrap quando o campo da cena está vazio)
```

| Prefab | CharacterId | Modelo |
|---|---|---|
| Ally_Warrior | Warrior | Warrior |
| Ally_RoyalArcher | Archer | RoyalArcher |
| Ally_RoyalCommander | Commander | RoyalCommander |
| Ally_ArcaneMage | Mage | ArcaneMage |
| Ally_MountedKnight | MountedKnight | MountedKnight |
| Ally_Spearman | Lancer | Spearman |
| Ally_Healer | Healer | Healer |
| Enemy_Soldier | EnemySoldier | EnemySoldier |
| Enemy_Archer | EnemyArcher | EnemyArcher |
| Enemy_Berserker | Berserker | Berserker |
| Enemy_HeavyWarrior | HeavyWarrior | HeavyWarrior |
| Enemy_Pikeman | Pikeman | Pikeman |
| Enemy_DarkMage | DarkMage | DarkMage |

## Técnica

- **Escala/orientação**: metros, `bakeAxisConversion`. O FBX chega olhando para −Z; o prefab gira o filho `Model`
  em 180° para o personagem olhar para +Z (direção de marcha). Altura ~1,4 m (comandante ~1,8 m,
  cavaleiro montado ~1,75 m), parecida com os bonecos provisórios, então formação e colisão não mudaram.
- **Esqueleto** humanoide com nomes do Unity (Hips, Spine, Chest, Neck, Head, Left/RightUpperArm...), importado como
  **Generic** (mais barato para multidões). Ossos de capa (Cape1–3) e encaixes não deformantes: `RightHandSocket`,
  `LeftHandSocket`, `HeadSocket`, `BackSocket`, `ProjectileSpawn` (arqueiras, magos, curandeira), `VFXSocket`.
- **Cavaleiro**: cavalo e cavaleiro são malhas separadas no mesmo esqueleto; o quadril do cavaleiro é filho do osso
  `Saddle` do cavalo, então os clips animam os dois sincronizados.
- **Rosto**: olhos, pálpebras, sobrancelhas, boca, bigode e barba modelados; 7 blend shapes
  (Happy, Angry, Attack, Surprised, Pain, Victory, Defeat) no LOD0.
- **Material único**: atlas de paleta 512×512 (swatches com degradê pintado) + mapa de emissão para cristais/chamas.
  Todos os personagens e armas usam o mesmo material → SRP Batcher/instancing.
- **LOD**: LOD0 ~8–15 mil triângulos (close-ups), LOD1 ~3–5 mil, LOD2 ~1–2 mil (multidão), escolhidos pelo
  `LODGroup` (16% / 5,5% / 0,4% da tela). LOD2 não projeta sombra. 2 ossos por vértice, `SkinQuality.Bone2`.
- **Armas** são objetos separados filhos dos encaixes (trocáveis); aljava, livros e cristais vão fundidos no corpo
  (sem draw call extra) e também exportados como prop independente.

## Animações por personagem

| Personagem | Clips |
|---|---|
| Guerreiro | Idle, Walk, Run, Attack, AttackCombo, Block, Hit, Death, Victory |
| Arqueira Real | Idle, Walk, Run, DrawBow, Aim, Shoot, Reload, Dodge, Hit, Death, Victory |
| Comandante | Idle, Walk, Run, Attack, Command, Rally, Buff, Hit, Death, Victory |
| Mago Arcano | Idle, Walk, Run, Cast, Attack, AreaSpell, Hit, Death, Victory |
| Cavaleiro | Idle, Walk, Run, Charge, Attack, Skill (empina e gira a lança), Hit, Death, Victory |
| Lanceiro | Idle, Walk, Run, SpearThrust, Attack, Defend, Charge, Hit, Death, Victory |
| Curandeira | Idle, Walk, Run, Heal, AreaHeal, Cast, Shield, Revive, Hit, Death, Victory |
| Soldado | Idle, Walk, Run, Attack, Block, Hit, Death, Victory |
| Arqueiro | mesmos da Arqueira Real |
| Berserker | Idle, Walk, Run, HeavyAttack, Rage, Hit, Death, Victory |
| Guerreiro Pesado | Idle, Walk, Run, Attack, Defend, ShieldBlock, Hit, Death, Victory |
| Piqueiro | Idle, Walk, Run, SpearAttack, Thrust, Hit, Death, Victory |
| Mago Sombrio | Idle, Walk, Run, Cast, Attack, AreaAttack, Hit, Death, Victory |

O Animator de cada personagem é gerado a partir do `.json`: locomoção (Idle/Walk/Run por `Speed`), `Attack`
(+ `AttackAlt` sorteado por `AttackVariant`), `Hit`, `Die`, `Victory`, `Skill`, `Block`. O dano continua sendo
aplicado pelo sistema de combate no tempo de impacto (`impactDelay` do prefab = quadro de impacto do clip).

## Jogabilidade integrada

- Tropas, comandante e inimigos usam os prefabs pela `CharacterVisualLibrary` (sem prefab, volta ao boneco provisório).
- Flechas e magias saem do encaixe `ProjectileSpawn`; flechas usam os modelos `Weapon_RoyalArrow`/`Weapon_EnemyArrow`.
- Comandante: corrida pelo Animator, "Comando" ao usar habilidade, comemoração na vitória.
- **Tipos de inimigo** (`EnemyKinds`), liberados por fase (1: Soldado; 2: +Arqueiro; 3: +Piqueiro; 4: +Berserker;
  5: +Pesado; 6: +Mago Sombrio). Ondas sorteiam a composição (determinística por fase) com corpo a corpo na frente
  e atiradores atrás; `EnemyWave.kinds` permite fixar a composição.
  - Arqueiro: flecha à distância (7 m), menos vida. Mago Sombrio: orbe de fogo com dano em área no exército.
  - Piqueiro: alcance maior. Berserker: fúria abaixo de 50% de vida (ataques 40% mais rápidos, +25% velocidade, animação Rage).
  - Guerreiro Pesado: muita vida/armadura, lento, 30% de chance de bloquear (−70% de dano, animação de escudo).
  - O balanceamento muda nas fases 2+ (antes só havia soldados).

## Validação (2026-10-18)

- Unity 6000.6.6f1 em batch: os 13 FBX importados, os 13 prefabs + Animators + biblioteca montados sem erro.
- Capturas reais (URP + LegionToon) em `Builds/CharacterUnity/<Nome>/turnaround.png` (frente, lado, costas, perspectiva)
  e `animations.png` (6 quadros por clip); cena de jogo em `Builds/CharacterUnity/gameplay*.png`
  (exército azul com as 6 classes + comandante, inimigos dos 6 tipos). Prévias do Blender em `Builds/CharacterPreview/`.

### Pendente

- Teste jogando (Play/WebGL/Android): combate com os novos tipos de inimigo, Console sem erros e FPS no aparelho.
- VFX específicos por classe (fúria, cura em área, faíscas de bloqueio): hoje usam as partículas existentes (`Vfx.Burst`).
- Refinamento artístico (escultura/textura pintada) para aproximar dos detalhes das folhas de referência.
