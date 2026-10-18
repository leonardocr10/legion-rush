# Tela inicial: implementação e relatório de assets

Tela inicial do Legion Rush 3D reconstruída em UGUI a partir de duas referências:

- `projeto/Tela Inicial Legion Rush.webp`: composição e posição dos elementos.
- `projeto/Pacote Tela Inicial.webp` e os recortes em `projeto/tela-inicial/`: a arte de cada componente.

A tela não é uma imagem única. Cada botão, painel, ícone e texto é um elemento próprio, com toque, estado e dados reais.

## Onde está cada parte

```
Assets/LegionRush/UI/MainMenu/
  Icons/        ícones isolados (Icon_Coin, Icon_Gem, Icon_Store ... Icon_More, Icon_Crown, Icon_Clock, Badge_Alert)
  Buttons/      molduras e botões (Button_SideFrame, Button_NavFrame[Selected], Button_Play, Button_Boss, Button_Add, Button_Settings)
  Panels/       painéis e barras 9-slice (Panel_PlayerProfile, Panel_WorldProgress, Panel_TimerPill, Bar_*)
  Backgrounds/  camadas do cenário (Background_Sky, Background_Mountains, Background_FrozenKingdom, Background_Ground, Shade_Vertical)
  Characters/   Character_KingArmy, Avatar_King
  Sprites/      Logo_LegionRush, Thumb_FrozenKingdom, Deco_* (pinheiros, rochas, bandeiras, tochas), MainMenu.spriteatlasv2
  Resources/    MainMenuArt.asset (catálogo de sprites) e MainMenuBorders.json (bordas 9-slice)
  Prefabs/      componentes salvos como prefab (gerados)
  Scenes/       MainMenuPreview.unity: a tela isolada com os dados do mockup
  Scripts/      MainMenuScreen, MainMenuView, MainMenuWidgets, MainMenuData, MainMenuArt, MainMenuPreview, MainMenuSelfTest
  Editor/       MainMenuArtTools: importação, catálogo, atlas, prefabs, cena e capturas
tools/art/extract_main_menu.py   recorta os 30 recortes em 57 sprites
```

### Regerar a arte

```
python tools/art/extract_main_menu.py
```

Depois, no Unity: **Legion Rush › UI › Tela Inicial › Atualizar arte** (catálogo e atlas) e **Gerar prefabs e cena de prévia**.

Em lote: `Unity.exe -batchmode -projectPath . -executeMethod LegionRush.EditorTools.MainMenuArtTools.BuildAllBatch -captureDir Builds/MainMenuPreview`.

## Arquitetura

- **MainMenuScreen** é a tela da pilha de navegação (`UIRoot`). O nome não mudou: `GameBootstrap` e a tela de abertura continuam chamando `ui.Replace<MainMenuScreen>()`.
- **MainMenuView** monta a composição e só recebe dados (`Bind(MainMenuModel)`). Ela não lê o save nem a economia.
- **IMainMenuData**: `GameMainMenuData` lê save, carteira, missões, conquistas, recompensa diária e catálogo de fases. `SampleMainMenuData` traz os valores do mockup para a prévia.
- **IMainMenuNavigator**: `UIRootMainMenuNavigator` abre as telas que já existem e chama `GameBootstrap.PlayLevel`.
- **Componentes** (`MainMenuWidgets.cs`): botão lateral, botão de navegação, selo de notificação, contador regressivo, barra de progresso, barra de moeda/gema, painel de perfil, painel de mundo e janela modal. Também há as animações leves: pressionar, pulso, flutuação, entrada do logo, aura, parallax e área segura.

## O que cada botão faz

| Botão | Destino | Dado real exibido |
|---|---|---|
| Perfil | Heróis | Nível = maior fase liberada; barra = estrelas ganhas / estrelas possíveis (mesma regra do menu anterior) |
| Moedas / Gemas e "+" | Loja (rola até Baús ou Visuais) | `Economy.Gold` / `Economy.Gems`, contagem animada ao receber |
| Engrenagem | Configurações | — |
| LOJA | Loja | — |
| MISSÕES | Missões | selo quando há missão para resgatar |
| EVENTOS | aviso "em breve" | o jogo ainda não tem eventos |
| MELHORIAS | Melhorias | selo quando alguma melhoria cabe no ouro atual |
| Baú temporizado | Recompensa diária | contagem até a próxima diária ou "ABRIR!", e selo |
| RANKING | Recordes | — |
| RECOMPENSAS | Conquistas | conquistas resgatadas / total, selo quando há resgate |
| TEMPORADA, AMIGOS, MENSAGENS | aviso "em breve" | sistemas inexistentes no projeto |
| Painel do mundo e baú | Fases do mundo atual | mundo, nome, "FASE m-f", estrelas do mundo |
| JOGAR | `PlayLevel` da fase atual (preparação de tropas) | — |
| MODO BOSS | Maior fase de chefão liberada (8, 15, 22, 26, 30) | cadeado e aviso "libera na fase N" quando ainda bloqueado |
| INÍCIO | (selecionado) | — |
| HERÓIS, TROPAS | telas existentes | — |
| PASSE | aviso "em breve" | — |
| MAIS | janela com Mundos, Inventário, Equipar, Conquistas, Recordes e Diária | — |

## Responsividade

- O `CanvasScaler` do `UIRoot` usa 1080×1920 como referência. Em celulares, a escala segue a largura; em telas mais largas que 0,7 (tablets e dobráveis), segue a altura.
- O layout usa as coordenadas da arte (941×1672), cada grupo preso à sua borda: topo, colunas laterais, base. Nada é esticado: ícones mantêm a proporção, e molduras e barras são 9-slice.
- Os botões ficam dentro da área segura. O cenário (`UIFullBleed`) passa por baixo do entalhe e da barra de gestos.

## Relatório de assets

### Separados dos recortes (prontos)

Logo, avatar, coroa, moeda, gema, "+", engrenagem, os 10 ícones laterais, os 5 ícones da navegação, o selo "!", o relógio, o baú do painel, o JOGAR, o retrato do Modo Boss, a miniatura do mundo, o castelo, o rei com o exército, as montanhas, os pinheiros, as rochas, as bandeiras e as tochas.

### Reconstruídos (o original tinha texto ou peças por cima)

- **Molduras vazias** dos botões laterais e da navegação (normal e selecionada). O miolo foi refeito a partir do gradiente da própria arte.
- **Painel do perfil e painel do mundo** sem nome, nível, valores e barra. O fundo foi refeito por gradiente, então a textura de colmeia do painel do mundo não foi mantida.
- **Faixa do Modo Boss** sem o texto, que agora é TextMeshPro.
- **Trilhos e preenchimentos** das barras (XP, estrelas, moedas), feitos a partir do perfil vertical real da arte.
- **Pílula do contador**: desenhada, não há uma pílula limpa na arte.
- **Céu, chão de neve e sombras de leitura**: gradientes gerados para emendar o cenário em tela vertical.

### Ausentes ou que pedem arte nova

1. **Cenário em resolução maior.** O castelo (706 px) e o rei com o exército (420 px) são ampliados cerca de 1,5× e 2,5× numa tela 1080×1920. Ficam aceitáveis, mas perdem nitidez. O ideal é uma versão de 2048 px de cada camada.
2. **Ícones em 2×.** Os ícones têm de 70 a 110 px e aparecem com cerca de 1,15×. Para telas de alta densidade, o ideal é exportar a 2×.
3. **Botão JOGAR sem texto.** O "JOGAR" continua desenhado na arte, porque a fonte da referência não está no projeto. Para traduzir, falta a versão sem texto.
4. **Bandeiras em pedaços** para animar o tecido. Hoje a bandeira inteira balança no mastro.
5. **Avatares de outros heróis** no mesmo formato do Avatar_King, para o perfil acompanhar o herói escolhido.
6. **Nome do jogador.** O save não tem nome, então aparece "Comandante". O mockup mostra "Leonardo".

### Tipografia

Usa as fontes do projeto (licença OFL, uso comercial permitido): **Lilita One** em títulos, rótulos e botões; **Baloo 2** em números; **Fredoka** em textos. Elas se aproximam do estilo da referência (caixa alta robusta, contorno escuro), mas não são a mesma fonte do mockup.
