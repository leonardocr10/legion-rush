# Legion Rush 3D

Crowd runner 3D de fantasia para Android/iOS, feito em **Unity 6 LTS (6000.0.83f1) + URP**.

## O que já funciona

- Comandante controlado por arrasto, com avanço automático e câmera retrato adaptativa
- Exército de arqueiros e guerreiros com formação dinâmica e object pooling
- Portais +, x, − e −50%, com regras de arredondamento, limites e autoteste
- Combate automático: críticos, flechas com rastro dourado, animação de morte e celebração de vitória
- Shader `LegionRush/ToonLit` com contorno, rim light, flash de dano e sombras tingidas por mundo
- Iluminação por mundo: Reino Congelado, Deserto Perdido, Floresta Encantada e Reino Vulcânico
  - sol, luz ambiente, neblina e color grading próprios
  - tochas simuladas, lava emissiva, feixes de sol e partículas (neve, poeira, vaga-lumes, brasas)
- Qualidade BAIXO/MÉDIO/ALTO com detecção automática do aparelho, opção de 30/60 FPS e redução automática de qualidade se o FPS cair
- Áudio com canais de música, efeitos e interface:
  - limite de vozes e agregação, para centenas de soldados atacando juntos
  - mute, volume por canal e configurações persistentes
- Menu de pausa no jogo para ajustar áudio, gráficos e FPS, e trocar de mundo
- Pipeline de animação pronto para receber os modelos finais com `CharacterVisualLibrary` e `CharacterAnimatorDriver`

## Pendente

- Modelos 3D finais, rigs e animações: os personagens atuais são placeholders feitos de primitivas.
- Chefões, mago, lanceiro, curandeiro, heróis e habilidades.
- Portais de escolha estratégica, telas de menu, loja, save completo e as 30 fases.
- Profiling em aparelho real.

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
