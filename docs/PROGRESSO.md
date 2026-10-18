# Progresso da finalização

Cada fase é commitada e passa no **Compile Check**. O workflow compila todo o código com o compilador do Unity 6000.0.83f1. As classes do URP entram como stubs nessa verificação.

| Fase | Status | Entregue |
|---|---|---|
| A — Auditoria | ✅ | `docs/FASE_A_AUDITORIA.md` e verificação de compilação sem licença |
| B — Modelos e animações | ⛔ bloqueada | Depende de arte 3D (ver auditoria, seção 5). O pipeline de integração já existe. |
| C — Tropas, habilidades e chefões | ✅ código | Detalhes abaixo |
| D — Portais estratégicos | ✅ código | Detalhes abaixo |
| E — Menus, loja, inventário e save | ✅ código | Save, economia, meta-progressão e 20 telas UGUI |
| F — 30 fases | ⏳ | — |
| G — Otimização, profiling e build | ⏳ | Depende do aparelho e da licença Unity |

## Fase C

- **6 classes de tropa:**
  - Arqueiro (projétil);
  - Guerreiro (armadura alta);
  - Lanceiro (perfura até 3 inimigos em linha);
  - Mago (orbe com dano mágico em área);
  - Curandeira (cura o aliado mais ferido);
  - Cavaleiro (investida de 2,5× no primeiro golpe).
- Cada classe tem vida, dano, alcance, velocidade, frequência, armadura, crítico, nível e raridade (`ArmyConfig`).
- **Formação por papel:** a linha de frente fica à frente do comandante (cavaleiros, guerreiros e lanceiros) e a retaguarda fica atrás (arqueiros, magos e, por último, curandeiras).
- **Posturas durante a partida:**
  - Equilibrada;
  - Ofensiva: a frente avança mais, com −10% de armadura;
  - Defensiva: formação compacta, frente contida, +35% de armadura.
- **4 composições escolhidas antes da fase:** Equilibrada, Arqueiros, Muralha e Arcana.
- **Combate:**
  - tipos de dano: físico, mágico, fogo e gelo;
  - armadura e resistências, com fraquezas negativas;
  - efeitos: escudo, lentidão, atordoamento e queimadura.
- **7 habilidades com recarga:** Chuva de Flechas, Escudo Coletivo, Cura em Área, Explosão Arcana, Investida de Cavalaria, Congelamento e Ataque do Comandante.
  - 4 espaços, escolhidos na tela de preparação;
  - mira automática no maior grupo inimigo ou no chefão;
  - o poder cresce com a quantidade de tropas da classe relacionada.
- **Chefões (`BossController`):**
  - máquina de estados: Dormente → Intro → Movendo → Aviso → Execução → Vulnerável → Troca de fase → Morte;
  - 3 fases por limiar de vida, com velocidade crescente e invocação ao trocar de fase;
  - todo ataque tem aviso visual no chão; vários abrem uma janela de vulnerabilidade com dano extra;
  - tipos de ataque: golpe no alvo, giro, onda de choque, chuva de círculos, linha (sopro, laser, fissura), tempestade, invocação e voo (imune a corpo a corpo).
- **Os 5 chefões:**
  - Titã Sombrio;
  - Faraó Insano: armadilhas, tempestade de areia e guardiões;
  - Dragão Ancião: voo, sopro de fogo e chuva de fogo; exige tropas à distância;
  - Lord Vulcano: explosões, fissuras de lava e meteoros;
  - Colosso Mecânico: giro, laser com superaquecimento, mísseis e autômatos.
- **Sequência de vitória:** câmera lenta, explosão de luz, recompensa em ouro e gemas e música de vitória.

## Fase D

- `GateChoiceRow` com 2 ou 3 `GateOption` por fileira, configuráveis em cada `LevelConfig`. O formato antigo é convertido automaticamente.
- **14 efeitos:**
  - tropas por classe;
  - multiplicar, com teto de ganho;
  - perder tropas (número ou percentual);
  - dano ou velocidade de ataque (temporário ou até o fim da fase);
  - escudo;
  - cura;
  - nível das tropas;
  - ouro e gemas;
  - emboscada;
  - reduzir inimigos à frente;
  - atalho.
- **Rotas:** o caminho curto pula trecho mas gera emboscada; o caminho longo dá ouro e tropas.
- **Balanceamento:**
  - limite total de soldados;
  - limite por classe (por exemplo, 12 cavaleiros);
  - teto de +60 por multiplicação.

## Fase E — parte 1

- **`SaveManager`:**
  - JSON versionado com SHA-256;
  - gravação atômica com verificação e backup;
  - recuperação e quarentena de arquivos corrompidos;
  - migrações;
  - salvamento automático (por mudança, ao pausar e ao fechar);
  - exportação e importação por código.
- `Economy`: transações atômicas de ouro e gemas, sem dinheiro real.
- `Progression`: estrelas (vitória, 40% e 70% de sobrevivência), pontuação, recordes e desbloqueio da próxima fase.

## Fase E — parte 2

- **UI UGUI criada por código** (`UI/Framework`), sem prefabs:
  - pilha de navegação com transição;
  - área segura e escala adaptada a celular dobrável aberto ou fechado;
  - botão voltar do Android;
  - diálogos de confirmação e notificações;
  - vibração curta no Android (desligável).
- **Telas:**
  1. Splash
  2. Menu
  3. Mundos
  4. Fases (30, com estrelas, bloqueio e "em breve")
  5. Heróis
  6. Tropas
  7. Inventário
  8. Equipamentos
  9. Melhorias
  10. Loja
  11. Missões
  12. Recompensa diária
  13. Conquistas
  14. Recordes
  15. Ajustes
  16. Pausa
  17. Vitória
  18. Derrota
  19. Recompensas
  20. Confirmações e notificações

  A tela de **Preparação** (composição e habilidades) completa o fluxo.
- **Meta-progressão** (`Meta/`):
  - 7 melhorias permanentes;
  - nível (até 10) e desbloqueio das classes Lanceiro, Mago, Curandeira e Cavaleiro;
  - 5 heróis (ARTHOR, LYRA, MORVAN, KAEL, ELARA) com passiva e evolução;
  - 9 equipamentos em 3 espaços;
  - 3 poções;
  - 5 visuais do exército;
  - 3 baús;
  - 3 habilidades desbloqueáveis;
  - 7 missões (diárias e semanais);
  - 8 conquistas;
  - recompensa diária de 7 dias.
- **Bônus:** são somados em `MetaBonuses` e aplicados ao exército a cada montagem de fase. O ouro ganho recebe o bônus de ouro.
- **Fluxo:** Splash → Menu → Preparação → Partida → Resultado → Próxima fase ou Menu. A fase fica montada atrás dos menus como cenário.
- **CI:** compila o UGUI embutido no editor com o perfil .NET Standard 2.1, o padrão do Unity 6.

## Testar no celular (quando houver APK)

1. Toque na Splash e depois em **JOGAR** no menu.
2. Em **Preparar Tropas**, escolha a composição e as 4 habilidades e toque em **JOGAR**.
3. Durante a fase, use os botões de habilidade (parte de baixo) e de **Postura**.
4. Em **Ajustes → Teste no aparelho**, troque o mundo ou o chefão para testar todos os 5.
