# Reino Congelado — relatório de fidelidade ao blueprint

Referência: folha *Vista geral do cenário — Reino Congelado — Cenário 1*, com as seções vista geral, castelo final, vistas frontal, lateral, aérea e traseira, módulos, castelo, obstáculos, portais, unidades e decoração.
Resultado: cena `Assets/LegionRush/Worlds/FrozenKingdom/Scenes/FrozenKingdom_01.unity`.

Capturas reais do Unity:
- `Builds/EnvPreview/scene2/` (vistas de inspeção) e `Builds/EnvPreview/frozenkingdom01_vistas.png` (folha das 6 vistas).
- Build jogável: `Builds/WebGL/FrozenKingdom01`.

## Como o mapa foi lido

- **Direção de avanço**: do spawn, na parte de baixo das vistas frontal e traseira, até o castelo no topo da vista geral. No Unity é +Z, com o castelo centrado no eixo da pista e o portão voltado para as tropas.
- **Três corredores da vista geral**: o brief pede para não transformá-los em três fases. Viraram um percurso contínuo, onde cada trecho repete a sequência dos corredores: portais, depois inimigos, depois mais portais. A sequência real vem do `LevelConfig` da fase 1: 5 fileiras de portais, 4 ondas e o chefão (ver `MAPA.md`).
- **Pistas**: calçamento cinza com bordas de pedra e neve, largo o bastante para o exército e com o centro livre. Dos lados há campos de neve abertos com florestas, como na vista geral, e não um corredor fechado.
- **Medidas**: vêm do jogo (pista de 220 m × 10 m). A folha não traz cotas, então distâncias, alturas e tamanhos de decoração e do castelo são estimativas. Isso está registrado em `MAPA.md`.

## Conferência (as 10 perguntas do brief)

| # | Pergunta | Situação |
|---|---|---|
| 1 | Castelo na posição correta? | **Sim.** Centralizado no fim do percurso (z = 252), escadaria ligada ao pavimento, portão azul voltado para as tropas, braseiros nos dois lados da escada e estandartes na muralha. |
| 2 | Pista com proporções semelhantes? | **Sim**, dentro do que o jogo exige (10 m de largura, bordas elevadas, neve nas laterais). Os blocos do calçamento são textura procedural. |
| 3 | Portais distribuídos corretamente? | **Sim.** Fileiras transversais com 2 ou 3 portais lado a lado, nas posições da fase 1. A moldura é de pedra clara, com estandarte azul em cada pilar, orbe na cor da categoria, interior translúcido e faíscas. |
| 4 | Montanhas coerentes? | **Parcial.** A cordilheira 3D fica atrás e dos lados do castelo, mais o anel distante no céu. Os picos são facetados (low-poly), menos detalhados que os da folha. |
| 5 | Vegetação nas áreas adequadas? | **Sim.** São 483 pinheiros nas laterais e nas colinas, mais denso longe da pista, com o centro de movimento livre. |
| 6 | Torres nas posições esperadas? | **Sim.** Torres de observação alternando os lados a cada 64 m, com base quadrada, ameias, estandarte e telhado azul. Há também o arco de chegada com duas torres pequenas. |
| 7 | Bandeiras no mesmo padrão? | **Sim.** Azuis com flor-de-lis e borda dourada, nos mastros da borda, nas torres, no castelo e nos portais, balançando ao vento (shader). |
| 8 | Iluminação semelhante? | **Parcial.** Dia frio com neve branca, sombra azul, fogo quente e portais brilhando. Não há luz pontual real nas tochas (o brilho é simulado com halo e poça de luz) nem céu com nuvens volumétricas. |
| 9 | Mesma profundidade? | **Sim nas vistas panorâmicas.** Na câmera do jogo a neblina (55–150 m) e o far clip de 140 m escondem o castelo até a reta final, que é o comportamento esperado na partida. |
| 10 | Mesma identidade visual? | **Sim no cenário.** As unidades continuam com os bonecos provisórios (ver abaixo). |

## Diferenças que continuam

1. **Personagens**: o brief manda usar os modelos oficiais do exército azul e vermelho se existirem. Este projeto não tinha modelos 3D de unidades: o jogo usa bonecos montados por código. Em paralelo, outra sessão está trabalhando em `Assets/LegionRush/Characters`. A cena não cria personagens: o jogo continua gerando tropas, inimigos e chefão pelo `LevelManager`.
2. **Modelos de artista**: castelo, torres, rochas e props são malhas procedurais facetadas, geradas pelo kit (`Assets/LegionRush/Environments`) e salvas como assets em `Worlds/FrozenKingdom/Models`. São modelos 3D reais com materiais e prefabs, não cubos provisórios, mas não têm a escultura nem a pintura à mão da folha. Não há Blender nesta máquina.
3. **Decals**: `Snow_Decal` e `Stone_Decal` são malhas finas, não Decal Projector do URP (mais barato no Android).
4. **Torres da folha**: em algumas vistas, o topo das torres tem uma estrutura de madeira (tipo catapulta ou guarita aberta). Aqui o topo é de ameias de pedra com telhado azul, porque o brief pede "topo com ameias".
5. **Céu**: degradê com halo do sol e nuvens leves, sem as nuvens volumosas da arte.
6. **Otimização**: há LOD Group de corte na vegetação e nos props, Static Batching nas peças paradas, materiais compartilhados (8 materiais no cenário todo) e colisores só na pista e nos portais. Não usei **baked lighting**: a luz é do jogo (perfil por mundo, aplicado em runtime). O FPS **não foi medido**, porque o painel do navegador estava oculto e o navegador limita o ritmo da página. Falta medir num aparelho Android.
7. **Duas formas de montar o mesmo mapa**: a cena `Main` do jogo continua com o cenário procedural (mais leve: uma malha por material a cada 30 m). A `FrozenKingdom_01` usa as 753 peças por prefab. Vale medir as duas no aparelho antes de escolher qual fica no build final.

## Validação feita

- Cena gerada pelo construtor (753 peças, 0 prefabs faltando), aberta em batch e capturada nas 6 vistas.
- Build WebGL só desta cena (`FrozenKingdomSceneBuilder.BuildWebGLBatch`): abre, passa pela splash e pelo menu e roda a fase 1.
  - O log mostra que o mapa da cena foi usado: não aparece a mensagem do procedural "Cenário Reino Congelado: 11 trechos".
  - Tropas avançam, portais aparecem nas fileiras e a câmera acompanha.
  - Combate, chefão e vitória foram testados na build da `Main` (mesma lógica). O exército perdeu para o chefão nas duas tentativas, então a abertura do portão do castelo foi conferida só na prévia do editor (`09_portao_aberto`).
- Console: sem erros novos. Os avisos "Can't add component because class 'CapsuleCollider' doesn't exist!" já existiam antes. Eles vêm do `GameObject.CreatePrimitive` com *Strip Engine Code* ligado no WebGL e não são do cenário.

## Especificação para modelagem (Blender ou similar)

Para substituir as malhas procedurais mantendo prefabs, nomes e pivôs:

| Asset | Pivô / dimensões (estimadas) | Polígonos (Android) | Observações |
|---|---|---|---|
| Castle_Main | centro da frente do portão, no chão; ~42 m de largura × 30 m de altura até a ponta da torre central; plataforma de 3,2 m | 15–25 mil tri | portão de 5,8 × 5,8 m em duas folhas separadas (pivô na dobradiça) para a animação de abertura; telhados azuis em cone; neve nas ameias |
| Castle_Tower_Large / Small | centro da base; raio 2,6 / 1,25 m; altura 11 / 5,2 m + telhado | 1,5–3 mil | telhado cônico azul, ponta dourada, flâmula |
| WatchTower | centro da base; 2,6 × 2,6 m; 5,6 m + telhado | 1,5–2,5 mil | ameias, estandarte voltado para a pista (−Z local) |
| Track_* (módulos) | início do módulo, eixo da pista em X = 0; 10 m × 10,6 m | 300–800 | bordas de 0,9 m; manter o topo do piso em y = 0 |
| Gate_* | centro da base; 5 m de largura × 4,4 m | 1–2 mil | interior separado (material translúcido); 4 variações de cor |
| PineTree_Snow | centro da base; 5 m | 300–600 | camadas com neve; LOD1 com metade dos triângulos |
| Rock_Snow (cluster/large) | centro da base | 150–400 | neve no topo |
| Props (cerca, barril, caixa, carroça, tenda, fogueira, lamparina, tocha, estátua) | centro da base | 100–1.500 | tamanhos dos prefabs atuais |
| Unidades azuis e vermelhas (soldado, arqueiro, lanceiro, capitão) | pés na origem, frente +Z, ~1 m de altura | 3–8 mil, humanoide | animações idle, correr, atacar, dano, morrer, vitória; vagas em `CharacterVisualLibrary` |

Textura: atlas de 1024 px por grupo (castelo, natureza, props) com compressão ASTC no Android. As cores estão em `EnvPalette`.

## Rodada 4 — pacote de 46 PNGs

Pacote: `LEGION_RUSH_Reino_Congelado_46_PNGs.zip`, 46 recortes RGBA de 70 a 450 px. Todos têm alfa, mas sobrou fundo quadriculado: pontos claros soltos em volta dos objetos e, no `21_portal_azul`, cantos ainda opacos. O `tools/art/clean_frozen_refs.py` limpa os recortes e grava em `Worlds/FrozenKingdom/References/`.

Os PNGs são **só referência visual**. Nenhum é usado como textura ou sprite no cenário: cada um orienta um modelo 3D procedural (malha + materiais + prefab) ou uma câmera de comparação.

- **Inventário completo** (arquivo, elemento, categoria, modelo, material, prefab, alfa, estado): `INVENTARIO_46_PNGS.md`.
- **Comparação lado a lado PNG × render do Unity**: `Builds/EnvPreview/comparacao_46_pngs.png`.

### O que mudou nos modelos para bater com os PNGs

| PNG | Ajuste |
|---|---|
| 04 muralha | torres quadradas mais altas nas pontas, com ameias e neve (`Wall(..., endTowers: true)`) |
| 05 portão | mastro com flâmula azul no alto |
| 07 pilar com bandeira | base de pedra em dois degraus com neve, haste e barra douradas com remates e ponta de lança |
| 08 estátua | lança na mão direita (no lugar da espada à frente) |
| 14 cerca | travessas em X entre os postes |
| 17 carroça | caçamba aberta de tábuas, 4 rodas com aro, sem lona nem barril |
| 18 tenda | telhado em faixas azuis e brancas, esteio de madeira |
| 20 lamparina | poste e braço de madeira com mão-francesa, lanterna pendurada |
| 21–24 portais | moldura tingida pela cor da categoria, placa no topo com emblema (escudo, espadas cruzadas, moeda, anéis), hastes douradas com tocha acesa, interior saturado em mistura alfa |
| 33–41 pistas | laje de pedra de 1,1 m sob o calçamento (laterais em blocos, neve nos cantos), novo `Track_PlatformBlock` (40) |

Os nomes pedidos (`FK_Castle_Main`, `FK_Castle_TowerLarge`, `FK_Castle_TowerSmall`, `FK_Castle_Wall`, `FK_Castle_Gate`, `FK_Castle_Arch`, `FK_Castle_Pillar`, `FK_Castle_Stairs`, `FK_Gate_Blue/Green/Yellow/Purple`) foram aplicados renomeando os prefabs anteriores com `AssetDatabase.MoveAsset`. Assim o GUID se mantém e não há modelos duplicados. A cena anterior foi preservada em `Scenes/Versions/FrozenKingdom_01_v1.unity` antes da remontagem.

### Concluído × pendente (rodada 4)

- **Concluído**:
  - 33 elementos de cenário (castelo e peças, estruturas, natureza, props, portais e 9 módulos de pista) como modelo 3D + prefab, com colisores na pista e trigger nos portais.
  - Cena remontada (761 peças).
  - 5 vistas de comparação.
- **Personagens (25–32)**: feitos pela sessão paralela de personagens, em FBX rigados do Blender: `Ally_Warrior`, `Ally_RoyalArcher`, `Ally_Spearman`, `Ally_RoyalCommander`, `Enemy_Soldier`, `Enemy_Archer`, `Enemy_Pikeman`… Eles entram no jogo pelo `CharacterVisualLibrary` (Resources). **Pendente**: não há modelo de capitão vermelho (32).
- **Materiais**: o cenário usa 8 materiais compartilhados com cor por vértice (pedra, alvenaria, calçamento, brilho, tecido, gelo, distante, cortina do portal). Pedra, neve, madeira, metal, ouro, tecido azul e vermelho e cristal são cores da paleta (`EnvPalette`) dentro desses materiais, e não materiais separados. Isso foi escolhido para reduzir draw calls no Android.
- **Continua pendente**:
  - Texturas pintadas à mão e escultura de artista (as malhas seguem facetadas).
  - Curva suave (o módulo atual faz 90°).
  - Medição de FPS em aparelho Android.
