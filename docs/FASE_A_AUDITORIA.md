# Fase A — Auditoria e plano de integração

Data: 18/10/2026. Base: commit `f52031a` do repositório `leonardocr10/legion-rush`.

## 1. Estado real do projeto

### Inventário

| Área | Arquivos | Situação |
|---|---|---|
| Núcleo | `GameBootstrap`, `CoreUtilities`, `ArmyCameraController` | Funcional. A cena é montada em runtime e criada automaticamente se faltar. |
| Exército | `ArmyManager`, `Soldier`, `ArmyConfig` | Apenas 2 classes (Arqueiro e Guerreiro). Formação em espiral, sem papéis. |
| Combate | `Combatant`, `Projectile`, `CorpseQueue`, `Enemy` | Vida, dano, crítico, impacto sincronizado à animação, corpos e pooling. Ainda sem defesa, resistências, status e dano em área. |
| Portais | `GateRowController`, `GateDef` em `LevelConfig` | Só operações numéricas (+, x, −, −50%) em 2 colunas. Sem efeitos estratégicos. |
| Fases | `LevelConfig`, `LevelManager` | 1 fase padrão gerada em código. Sem obstáculos, estrelas ou objetivos. |
| Personagens | `CharacterFactory`, `CharacterVisualLibrary`, `CharacterAnimatorDriver`, `AnimatorTemplateBuilder` | O pipeline para modelos reais está pronto. Todos os personagens ainda são primitivas. |
| Áudio | `AudioManager`, `AudioLibrary`, `AudioSettingsStore`, `LevelAudioDirector`, `AudioTools` | Completo para o escopo atual. Os 21 WAVs são de protótipo. |
| Gráficos | `ToonLit` e `AdditiveGlow` (shaders), `VisualQualityManager`, `WorldLightingController`, `WorldLightingProfile`, `WorldAmbience`, `ProceduralTextures`, `LightingTools` | 4 mundos e 3 perfis de qualidade. O SSAO precisa ser adicionado ao Renderer manualmente. |
| UI | `GameHud` (IMGUI) | Temporária: HUD, pausa, ajustes, vitória e derrota. Nenhuma das 20 telas finais existe. |
| Save | `PlayerPrefs` (áudio, qualidade e mundo) | Não existe `SaveManager`. Não há progresso persistido. |
| Build | `ProjectSetup`, `build-android.yml`, `compile-check.yml` | O APK depende dos segredos da licença Unity. A compilação é verificada no CI sem licença. |

### Verificação

- `compile-check.yml` compila todos os scripts com o compilador e as DLLs do Unity 6000.0.83f1 em 3 passes: player Android, editor/play mode e scripts de editor.
- URP e Core RP entram como stubs em `ci/stubs/`. Os erros de API do URP só aparecem na build real com licença.
- Nada foi executado em aparelho. Não existem medições de desempenho.

## 2. Cobertura do pedido de finalização

| # | Requisito | Status | Bloqueio |
|---|---|---|---|
| 1 | Modelos 3D definitivos | ❌ | Exige artista ou ferramenta de modelagem. Ver seção 3. |
| 2 | Rigging e animações | ⚠️ só o pipeline | Exige os arquivos FBX. |
| 3 | 5 chefões | ❌ | Código possível agora. Modelos, como no item 1. |
| 4 | 6 classes de tropa + formação por papel | ⚠️ 2 de 6 | — |
| 5 | 7 habilidades especiais | ❌ | — |
| 6 | Portais estratégicos | ⚠️ só numéricos | — |
| 7 | 20 telas | ⚠️ HUD temporário | Ícones e painéis finais dependem de arte 2D. |
| 8 | Loja e economia offline | ❌ | — |
| 9 | SaveManager robusto | ❌ | — |
| 10 | 30 fases | ⚠️ 1 de 30 | — |
| 11 | Gráficos e iluminação | ✅ base pronta | Validar no aparelho. |
| 12 | Otimização | ⚠️ pooling, tiers e instancing | LODs dependem dos modelos. Ainda não há perfil de memória. |
| 13 | Profiling em aparelho real | ❌ | Exige o aparelho conectado a um PC com o Profiler. |
| 14 | APK instalável | ⚠️ pipeline pronto | Exige os segredos da licença Unity. |

## 3. O que não pode ser entregue sem ajuda externa

1. **Modelos, texturas, rigs e animações finais.** A IA não gera arquivos FBX/GLB com malha, UV, textura pintada, rig e clipes de animação. Caminhos possíveis:
   - contratar um artista 3D usando as folhas de referência e a especificação técnica da seção 5;
   - usar ferramentas text/image-to-3D (Meshy, Tripo, Rodin) com auto-rig (Mixamo ou AccuRIG) e revisão manual;
   - em ambos os casos, o código já integra o resultado via `CharacterVisualLibrary`, sem refatoração.
2. **Licença Unity para a build.** É preciso ativar a licença uma vez num PC e cadastrar 3 segredos no GitHub (ver README).
3. **Profiling real.** É preciso um aparelho conectado via USB/ADB a um PC com Unity. A Fase G entrega a build de desenvolvimento e o roteiro de coleta. Os números não serão inventados.
4. **Originalidade visual.** As referências de comandante, cavaleiro e arqueira estão muito próximas de Clash Royale. Paleta, silhuetas e brasão precisam ser diferenciados antes da modelagem.

## 4. Plano de integração por fase

### Fase C — Tropas, heróis, habilidades e chefões (próxima)

- **Tropas:**
  - `UnitClass` passa a ter Guerreiro, Arqueiro, Lanceiro, Mago, Curandeira e Cavaleiro;
  - `TroopDefinition` (ScriptableObject) define vida, dano, alcance, velocidade, frequência, defesa, nível, raridade e `CharacterId`;
  - `ArmyConfig` vira uma lista de definições com composição inicial.
- **Exército:**
  - um pool por classe;
  - `FormationLayout` com papéis: linha de frente (guerreiro, lanceiro, cavaleiro) e retaguarda (arqueiro, mago, curandeira);
  - `SetCount` passa a aceitar a classe, para portais do tipo "+30 arqueiros".
- **Combate:**
  - `DamageInfo` com tipo físico, mágico ou fogo, defesa, resistências e `StatusEffect` (lento, congelado, queimando, escudo);
  - dano em área para mago e chefões;
  - cura para a curandeira.
- **Habilidades:**
  - `SkillDefinition` (ScriptableObject) e `SkillSystem` com cooldown, alvo, duração e indicador de recarga;
  - as 7 habilidades listadas, ligadas ao `GameAudio` e a efeitos de partículas.
- **Chefões:**
  - `BossDefinition` + `BossController` (máquina de estados Intro → Fase1..3 → Vulnerável → Morte);
  - `BossAttack` com telegrafia (aviso no chão), janela de vulnerabilidade e invocações;
  - os 5 chefões: Titã Sombrio, Faraó Insano, Dragão Ancião, Lord Vulcano e Colosso Mecânico;
  - barra de vida do chefão e câmera cinematográfica de entrada.

### Fase D — Portais estratégicos e balanceamento

- `GateEffect` abstrato (ScriptableObject) com subtipos:
  - `AddTroops(classe, n)`, `MultiplyTroops(fator, teto)`, `RemoveTroops`;
  - `StatModifier(dano, velocidade, duração)`, `TemporaryShield`, `HealAll`, `TroopLevelUp`;
  - `SummonHero`, `GrantCurrency`, `SpawnEnemies`, `ReduceEnemies`.
- `GateChoice` com 2 ou 3 portais, ícone, título e descrição, que substitui `GateRow` mantendo a compatibilidade com o formato antigo.
- `RouteFork` para caminho curto (mais inimigos) ou longo (recompensas), com pista ramificada.
- `ArmyBudget`: teto por classe e de multiplicação acumulada, para impedir explosões numéricas.

### Fase E — Menus, loja, inventário e salvamento

- `SaveManager`:
  - JSON versionado em `persistentDataPath`;
  - gravação atômica (arquivo temporário → rename), backup `.bak` e checksum SHA-256;
  - migrações por versão, autosave e export/import em texto.
- `EconomyService` (ouro e gemas, transações validadas), `InventoryService`, `ShopCatalog` (offline, sem dinheiro real), missões, conquistas, recompensas diárias e recordes.
- UI final em **UGUI**: Canvas por tela, `ScreenRouter` com pilha de navegação, safe area, haptics no Android e as 20 telas. O `GameHud` IMGUI será removido.

### Fase F — 30 fases

- Mapeamento dos chefões, conforme o documento de finalização:
  - Mundo 1, fases 1–8: chefão na 8 (Titã);
  - Mundo 2, fases 9–15: chefão na 15 (Faraó);
  - Mundo 3, fases 16–22: chefão na 22 (Dragão);
  - Mundo 4, fases 23–30: chefão na 26 (Lord Vulcano) e na 30 (Colosso).
- `LevelDefinition` com segmentos de pista reutilizáveis, obstáculos (espinhos, pedra rolante, barreira, abismo, martelo), objetivos e critério de 3 estrelas.
- Gerador no editor que cria os 30 assets a partir de uma tabela de design, seguido de revisão manual de cada layout.

### Fase G — Otimização, profiling e build

- `PerformanceOverlay` (FPS, frame time, GC, unidades vivas) e logs de desempenho.
- Build de desenvolvimento com Autoconnect Profiler.
- Roteiro de coleta: Profiler, Frame Debugger, Memory Profiler e Android GPU Inspector.
- LODs via `LODGroup` nos prefabs finais e compressão ASTC.
- Limite de unidades simultâneas por tier.

## 5. Especificação técnica para os modelos (entrega do artista)

| Item | Soldados e inimigos comuns | Heróis e comandante | Chefões |
|---|---|---|---|
| Triângulos LOD0 / LOD1 / LOD2 | 2.5k / 1.2k / 500 | 6k / 3k / 1.2k | 15k / 7k / 3k |
| Textura | 512² atlas por facção | 1024² | 2048² |
| Materiais | 1 (atlas) | até 2 | até 3 |
| Rig | Humanoid (Mecanim), até 30 ossos | Humanoid | Generic, rig próprio |
| Escala | 1 unidade = 1 m; soldado ≈ 1,1 m; chefão ≈ 5,5 m | | |
| Orientação | +Z frente, +Y cima, pivô nos pés, pose A | | |
| Clipes | Idle, Walk, Run, Attack, Hit, Death, Victory, Skill + específicos da classe | | Intro, Idle, Locomoção, 4–5 ataques, Vulnerável, Fase, Morte |
| Formato | FBX (clipes nomeados `classe_acao`) | | |

As armas ficam em objetos separados, presos a um socket `hand_R` ou `hand_L`.

## 6. Decisões para confirmar

1. **Chefões:** a lista do documento atual (Faraó, Dragão Ancião, Lord Vulcano, Colosso) substitui a do prompt mestre (Rei de Gelo, Golem, Cavaleiro Fantasma, Imperador). Este plano segue o documento atual.
2. **UI final:** UGUI, porque tem mais exemplos, prefabs e suporte mobile. A alternativa é UI Toolkit.
3. **Fonte dos modelos:** artista contratado ou ferramenta de IA 3D com revisão.
