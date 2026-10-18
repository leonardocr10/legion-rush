# Legion Rush 3D

Crowd runner 3D de fantasia para Android/iOS, feito em **Unity 6 LTS (6000.0.83f1) + URP**.

## O que já funciona

O detalhamento por fase está em [`docs/PROGRESSO.md`](docs/PROGRESSO.md). Resumo:

- 6 classes de tropa com formação por papel, 4 composições e 3 posturas.
- 7 habilidades com recarga e mira automática.
- 5 chefões com máquina de estados, aviso visual, fases, vulnerabilidade e invocações.
- Portais de escolha estratégica (2 ou 3 opções) com 14 tipos de efeito, rotas e limites de balanceamento.
- Save robusto (versionado, atômico, com backup e exportação), economia offline, estrelas e recordes.
- Iluminação por mundo (4 mundos), qualidade Baixo/Médio/Alto, shaders ToonLit e Glow.
- Áudio com canais, limite de vozes e persistência.

- 20 telas UGUI navegáveis (menu, loja, heróis, tropas, missões, conquistas, recompensa diária, recordes, ajustes...) com meta-progressão completa.

**Ainda provisório:** os personagens são primitivas (a Fase B depende de arte 3D). O HUD durante a partida usa IMGUI simples, e os ícones e painéis ainda não têm arte 2D final.

## Gerar o APK pelo GitHub (sem PC no dia a dia)

O workflow `.github/workflows/build-android.yml` compila o APK na nuvem com [GameCI](https://game.ci).

1. Uma única vez, obtenha a licença Unity Personal (arquivo `.ulf`):
   - instale o Unity Hub num computador e faça login;
   - ative a licença Personal;
   - copie o conteúdo do arquivo:
     - Windows: `C:\ProgramData\Unity\Unity_lic.ulf`
     - macOS: `/Library/Application Support/Unity/Unity_lic.ulf`
     - Linux: `~/.local/share/unity3d/Unity/Unity_lic.ulf`
2. No GitHub, abra **Settings → Secrets and variables → Actions** e crie três segredos:
   - `UNITY_LICENSE`: conteúdo completo do `.ulf`
   - `UNITY_EMAIL`: email da conta Unity
   - `UNITY_PASSWORD`: senha da conta Unity
3. Abra **Actions → Build Android APK → Run workflow**. Isso funciona pelo navegador do celular.
4. Ao final, abra a release `build-N` em **Releases** e baixe o arquivo `LegionRush3D.apk` direto no celular.
5. No Android, permita **instalar apps desconhecidos** para o navegador e instale o APK.

A primeira build leva cerca de 30 a 60 minutos, por causa do IL2CPP ARM64. As seguintes reaproveitam o cache.

## Abrir no editor

1. Abra a pasta pelo Unity Hub com a versão 6000.0.83f1 e o módulo Android.
2. Rode **Legion Rush → Setup → Configure Project (URP + Scene + Android)**. O comando cria o URP Asset, a cena `Main` e as configurações Android.
3. Aperte Play. Para gerar o APK local, use **Legion Rush → Setup → Build Android APK**.

## Áudio

Os WAVs ficam em `Assets/_Game/Audio/Resources/LegionAudio/`.

- Para substituir um som, troque o arquivo mantendo o mesmo nome. Não é preciso mudar código.
- Para adicionar variações, use `nome_2.wav`, `nome_3.wav` e assim por diante.
- Trilhas opcionais já reconhecidas: `music_battle_loop.wav` e `music_boss_loop.wav`.

## Estrutura

```
Assets/_Game/
  Scripts/  Core, Army, Enemies, Combat, Levels, Gates, Player, Characters, Audio, Rendering, UI
  Editor/   ProjectSetup, AnimatorTemplateBuilder, AudioTools, LightingTools
  Shaders/  LegionToon.shader, LegionGlow.shader
  Audio/Resources/LegionAudio/  21 WAVs (protótipo sintetizado)
```
