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
