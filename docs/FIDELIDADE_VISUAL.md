# Fidelidade visual — análise e assets

## Estado do projeto (analisado)

- **Unity** 6000.6.6f1 local; CI (game-ci) usa 6000.0.83f1. **URP** (`Assets/_Game/Settings/LR_URP.asset`), shaders próprios `LegionToon` e `LegionGlow`.
- **Cena única** `Assets/_Game/Scenes/Main.unity` com `Bootstrap` + câmera. Todo o resto (fases, personagens, UI) é montado por código.
- **Interface**: UGUI criada por código (`UIRoot`, `UI`, telas em `Assets/_Game/Scripts/UI/Screens`). Canvas Screen Space Overlay, `CanvasScaler` 1080×1920, ajuste largura/altura conforme a proporção, área segura aplicada em `UIRoot.ApplySafeArea`. Não há prefabs de UI; os "componentes reutilizáveis" são fábricas em código (`LegionUI`, `UI`), o mesmo padrão do resto do projeto.
- **Tipografia**: TextMeshPro com Lilita One / Fredoka / Baloo 2 (`Assets/Fonts`, tema em `Resources/Typography`). Gerar os assets SDF: menu *Legion Rush › Typography › Generate Font Assets & Theme* (o build também gera se faltar).
- **Arte 2D**: recortada das referências em `projeto/` por `tools/art/extract_ui_art.py` para `Assets/_Game/Resources/UIArt`.

## Tela 1 — Menu principal (implementada)

Referência: `projeto/Menu Heroico do Reino Congelado.png`.

| Elemento | Origem do visual | Dados ao vivo |
|---|---|---|
| Fundo (castelo + heróis) | recorte limpo da arte, respiração lenta, neve, vinheta | — |
| Perfil, moedas, gemas, ajustes | sprites `home_profile/gold/gems/settings` | nível, XP, ouro, gemas |
| Missões / Diárias / Eventos | sprites `home_missions/daily/events` | selos de alerta |
| Baú grátis / Baú épico / Passe | sprites `home_*_chest`, `home_battle_pass` | timer, vitórias, progresso |
| Mundos / JOGAR / Heróis | sprites `home_worlds/play/heroes` | fase e mundo atuais |
| 4 mundos | sprites `home_world_0..3` | cadeados conforme progresso |
| Navegação inferior | sprite `home_nav` | 7 abas funcionais |

Responsividade: grupos presos ao topo, às laterais e à base (`LegionUI.Place`); o cenário cobre a tela inteira.

## Assets ausentes (precisam ser produzidos)

Nenhum modelo 3D existe no projeto. O código já tem as vagas em `CharacterVisualLibrary` (`CharacterId`); sem modelo, usa-se o boneco provisório.

**Modelos 3D rigados (FBX/GLB, humanoide, ~3–8 mil triângulos, textura 512–1024):**

- Aliados: Comandante, Arqueira, Guerreiro, Mago, Lanceiro, Curandeira, Cavaleiro montado.
- Inimigos: Soldado, Arqueiro, Berserker, Guerreiro pesado, Piqueiro, Mago sombrio.
- Chefões: Titã Sombrio, Faraó Insano, Dragão Ancião, Lord Vulcano, Colosso Mecânico.

**Animações** (por personagem): idle, correr, atacar, receber dano, morrer, vitória.

**Cenário 3D**: castelo congelado, montanhas, bandeiras, tochas, portais e elementos de cada mundo (as folhas de referência mostram o estilo). Hoje o cenário é feito de primitivas.

**Artes 2D ainda faltando para as outras telas:** molduras 9-slice limpas (painel, cabeçalho, aba, card) e botões verde/azul/vermelho sem texto. As referências só trazem esses elementos já com texto/conteúdo; recortes aproximados serão gerados, mas versões limpas exportadas pelo artista ficam melhores.

## Pacote de assets de UI (integrado)

Fonte: `projeto/Pacote de Assets Graficos UI.png` e `projeto/Prancha Componentes Batalha.png`, separados por `tools/art/extract_ui_pack.py` em `Assets/LegionRush/Art/UI/Resources/<Categoria>/` (PNG com alpha, ampliados 2×, conferidos numa folha de contato). Bordas 9-slice em `borders.json`; importação (Sprite, alpha, sem mipmaps, ASTC no Android) feita por `Assets/_Game/Editor/LegionUIArtImporter.cs`.

- **Reutilizados diretamente:** ícones, baús, recompensas, selos, troféus, avatares, retratos, cartas de tropa, setas, bandeiras, efeitos, ícones de botões quadrados, artes das habilidades e retratos do HUD.
- **Reconstruídos como 9-slice editáveis:** botões dourado/verde/azul/roxo/laranja/vermelho/cinza (texto removido, texto real em TMP), molduras douradas, trilho e preenchimentos das barras de progresso, barra de vida do chefe, cabeçalho de popup com coroa (título apagado).
- **Números fixos removidos:** moedas, gemas e selos das habilidades; os valores vêm do jogo.
- **Aplicação:** `UI.Button` escolhe o sprite pela cor (estado desabilitado = cinza), `UI.Panel`/`UI.Row` usam as molduras, `MenuScreen` (todas as telas de lista) ganhou cabeçalho com coroa, carteira e fundo nevado; popups com coroa e entrada animada; HUD de batalha refeito em UGUI.
- **Validado no build WebGL:** abertura, menu principal, Loja, Preparar Tropas e HUD da partida (capturas no navegador).
- **Ainda sem arte específica no pacote:** cartas de herói/tropa e loja como componentes dedicados por tela (as telas usam linhas genéricas com moldura), abas por tela (Ofertas/Gemas/Moedas etc.), passe de batalha, ranking e amigos com layout próprio.

## Cenário 3D — Reino Congelado (mundo 1)

Kit em `Assets/LegionRush/Environments/` (o Blender não está instalado, então as malhas são geradas por código, facetadas e com cor por vértice):

- **Peças**: pinheiro em camadas com neve, arbusto e monte de neve (`Vegetation`), rochas chanfradas com neve no topo, penhascos em degraus, cristais de gelo emissivos e cachoeira congelada translúcida (`Rocks`), muralha com ameias, torre de telhado cônico azul com flâmula, portão com luz azul, escadaria e castelo de fundo (`Castles`), braseiro em pilar, mastro com estandarte de flor-de-lis que balança ao vento, mureta da pista, caixa e barril (`Props`), calçamento e barrancos de neve formando um cânion (`Terrain`), céu em degradê com anel de montanhas (`Lighting`) e fogo em partículas com halo e poça de luz (`VFX`).
- **Shaders** (`Shaders/Resources`): `LegionRush/EnvToon` (luz em degrau igual à dos personagens, cor por vértice, emissão, vento por vértice), `EnvIce` (gelo translúcido com fresnel) e `EnvSky` (céu e montanhas sem luz). Todos com instancing e compatíveis com o SRP Batcher; texturas procedurais (calçamento, alvenaria, estandarte).
- **Pista**: `FrozenKingdomTrack` monta 11 trechos de 30 m (1 objeto por material por trecho, ~440 mil vértices no Alto). O `LevelManager` usa o kit só no mundo 1; portões, inimigos, chefão e colisão não mudaram. Os outros mundos continuam com primitivas.
- **Qualidade** (`VisualQualityManager`): Baixo = ~55% da decoração, sem sombras do cenário, fogo sem partículas; Médio = 80%, sombras, poucas partículas; Alto = tudo. Meta de 60 FPS em todos os níveis.
- **Luz**: preset Frozen com neblina azul mais longa (55–150 m) e céu da mesma cor no horizonte.
- **Ferramentas**: menu *Legion Rush › Ambiente › Capturar prévia* (também em batch: `LegionRush.Environments.EditorTools.EnvPreviewCapture.CaptureBatch -captureDir <pasta>`) e *Legion Rush › Reino Congelado › 1. Gerar prefabs / 2. Montar cena* (ver rodada 3).

### Falta / limitações (cenário)

- Não foi validado num build WebGL próprio no navegador: só capturas renderizadas no editor (sem partículas animadas nem câmera seguindo). Falta gerar o build, jogar a fase 1 e medir o FPS.
- Malhas por código não são modelos de artista: faltam silhuetas orgânicas, texturas pintadas à mão e variação de detalhe. Para chegar ao nível das referências, o ideal são FBX de castelo, torres, pinheiros, rochas e props.
- Sem luzes reais nas tochas (o calor é simulado com halo e poça), sem god rays, sem reflexos.
- Fundo 3D do menu não foi feito (o menu usa arte 2D).
- Personagens continuam provisórios (sem modelos em `CharacterVisualLibrary`).

### Reconstrução do Reino Congelado (rodada 2)

Comparado com a folha `Vista geral do cenário — Reino Congelado` e com capturas reais do editor (`Builds/EnvPreview`).

- **Cor e luz**: o cenário estava lavado e arroxeado (ambiente somado em 85% no `EnvToon` + exposição +0,15 + split toning azul forte). Agora o ambiente entra com 50%, exposição 0, contraste 20, saturação 14, balanço quase neutro: neve branca na luz e azul só na sombra.
- **Pista**: textura do calçamento refeita (512 px, 7 fiadas de alturas diferentes, pedras de larguras variadas, chanfro iluminado, juntas fundas, rachaduras, desgaste e neve em algumas juntas). Largura, comprimento e colisão não mudaram.
- **Portais**: com o kit disponível, `GateRowController` monta o portal ornamentado (`EnvGatePortal`): pilares de pedra com base e capitel dourados, orbes luminosos, arco na cor da categoria com friso dourado e neve, cortina de energia em degradê e faixa no chão. Placa na cor da categoria com moldura dourada; títulos longos encolhem para caber. Valores e textos continuam vindo do `GateOption`. Lógica intacta.
- **Castelo**: portão azul de madeira (tábuas, cintas e cravos dourados, brasão, argola) no lugar do painel ciano. As folhas abrem para fora na vitória (`EnvGateDoors`, chamado pelo `LevelManager` ao entrar em `Victory`).
- **Montanhas**: cordilheira 3D atrás e dos lados do castelo (`EnvRocks.MountainPeak`, material `Distant` com 45% de neblina, dentro do far clip de 140 m) + anel do céu mais alto.
- **Paisagem**: torres de observação a cada 64 m, dois acampamentos (tenda, fogueira, carroça, cerca, caixas, lamparina), estátuas de cavaleiro na reta final, árvores parcialmente congeladas, pinheiros com neve intensa, blocos de gelo, rochas grandes e plantinhas.
- **Fogo**: chama de malha em línguas laranja (braseiro, fogueira, tocha), sem o cone amarelo.
- **Distribuição configurável**: `FrozenScatterProfile` (ScriptableObject em `Worlds/FrozenKingdom/Configurations/Resources/FrozenScatter_Default`): tipo, distância da borda, peças por metro, espaçamento mínimo, giro, escala, probabilidade e semente. Nada a menos de 1,5 m da pista; áreas dos marcos ficam reservadas.

#### Prefabs

Substituídos na rodada 3 pelos prefabs de `Assets/LegionRush/Worlds/FrozenKingdom` (nomes do blueprint). Ver abaixo.

#### Ainda falta

- Modelos de artista (FBX) para castelo, personagens e props: as malhas continuam procedurais facetadas; não há Blender nesta máquina.
- Tropas e inimigos continuam com os bonecos provisórios (sem modelos em `CharacterVisualLibrary`).
- LOD Groups não foram criados (as peças já são low-poly; o corte por distância é a neblina + far clip).
- Partículas de vitória específicas na chegada ao castelo: hoje só a abertura do portão + efeitos já existentes do exército.
- Não testado em aparelho Android (FPS real).

### Rodada 3 — mapa fiel ao blueprint (cena FrozenKingdom_01)

Tudo do Reino Congelado agora fica em `Assets/LegionRush/Worlds/FrozenKingdom/` (`Scenes`, `Prefabs`, `Models`, `Materials`, `Textures`, `Terrain`, `Tracks`, `Gates`, `Castle`, `Structures`, `Nature`, `Props`, `Lighting`, `VFX`, `Scripts`, `Configurations`). O kit genérico (malhas, shaders, peças) continua em `Assets/LegionRush/Environments`, para os outros mundos.

- **Cena** `FrozenKingdom_01.unity`: cópia da Main (Bootstrap, câmera, sol), então é jogável. O mapa é montado por prefabs com a mesma lógica do cenário do jogo: 753 peças, terreno de neve em 11 malhas, pista modular contínua, castelo, torres, florestas, acampamentos e montanhas. O `LevelManager` usa esse mapa quando a fase bate (Reino Congelado, 220 m, meia-largura 5). Nas outras fases ele some e o cenário procedural volta. A cena Main não mudou.
- **Construção automática**: menu *Legion Rush › Reino Congelado* → *1. Gerar prefabs* (`EnvPrefabBaker`) e *2. Montar cena* (`FrozenKingdomSceneBuilder`). Pode rodar de novo: apaga e remonta só o que criou. Em batch: `FrozenKingdomSceneBuilder.BuildAllBatch`, `CaptureBatch` e `BuildWebGLBatch`.
- **Prefabs (73)**: `Track_Straight/Bordered/Ramp/Platform/Crossroad/Curve/Stairs/Elevated` (+ Start, End, Gate2, Gate3, Enemy, Boss, BorderLeft/Right) com colisores simplificados e pivô no início do módulo de 10 m. `Gate_Blue/Green/Yellow/Purple` têm trigger e `EnvGatePortalInfo` (categoria do `GateOption`). Também: `Castle_*` (Main, Tower_Large/Small, Wall, Gate, Arch, Pillar, Stairs, Banner, Torch, Battlement), `WatchTower`, `StonePillar`, `StoneArch`, `BannerPole`, `Banner_Large/Medium/Small`, `TorchStand`, `PineTree_Snow` (+ Small/Large/Heavy), `Rock_Snow`, `Fence_Wood`, `Barrel_Wood`, `Crate_Wood`, `Cart_Wood`, `Tent_Blue`, `Campfire`, `Snow_Decal`, `Stone_Decal` e os marcadores `PlayerSpawnPoint`, `EnemySpawnPoint`, `BossSpawnPoint` e `GateRowPoint`. A vegetação e os props têm LOD Group de corte.
- **Tabela de coordenadas**: `docs/reino_congelado/MAPA.md` (sequência do percurso, peças principais e resumo da vegetação) e `coordenadas.csv` (todas as peças com a seção da referência).
- **Relatório de diferenças**: `docs/reino_congelado/RELATORIO.md`.
