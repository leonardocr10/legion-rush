using System.Collections.Generic;
using UnityEngine;

namespace LegionRush.Environments
{
    // Raiz do mapa montado na cena FrozenKingdom_01 (prefabs do kit). Quando a fase atual bate com o mapa
    // (mesmo mundo, comprimento e largura), o LevelManager usa este cenário em vez de gerar o procedural;
    // os marcadores de prévia (portais, inimigos) somem no Play porque o jogo cria os reais.
    public class FrozenKingdomSceneLayout : MonoBehaviour
    {
        public float trackLength = 220f;
        public float trackHalfWidth = 5f;
        public float castleZ = 252f;
        public float castlePlateau = 3.2f;
        [Tooltip("Grupo com marcadores de portais/inimigos/exército só para o editor.")] public GameObject previewGameplay;
        [Tooltip("Peças de detalhe que o perfil gráfico pode cortar (vegetação, rochas, props).")] public Transform[] detailGroups = new Transform[0];
        public EnvGateDoors castleDoors;

        private readonly List<GameObject> details = new List<GameObject>();
        private VisualQualityManager quality;
        private EnvSkyBackdrop sky;

        public static FrozenKingdomSceneLayout FindFor(WorldId world, float length, float halfWidth)
        {
            FrozenKingdomSceneLayout layout = FindAnyObjectByType<FrozenKingdomSceneLayout>(FindObjectsInactive.Include);
            if (layout == null) return null;
            bool match = world == WorldId.FrozenKingdom && Mathf.Abs(layout.trackLength - length) < 0.5f && Mathf.Abs(layout.trackHalfWidth - halfWidth) < 0.05f;
            layout.gameObject.SetActive(match);
            return match ? layout : null;
        }

        private void Awake()
        {
            if (Application.isPlaying && previewGameplay != null) previewGameplay.SetActive(false);
        }

        public void Activate(WorldLightingProfile world, VisualQualityManager qualityManager, Camera targetCamera)
        {
            if (previewGameplay != null) previewGameplay.SetActive(false);
            if (castleDoors != null) castleDoors.ResetClosed();
            quality = qualityManager;
            if (sky == null)
            {
                sky = new GameObject("Sky").AddComponent<EnvSkyBackdrop>();
                sky.transform.SetParent(transform, false);
                if (world != null)
                {
                    sky.Horizon = world.fogColor;
                    sky.MountainHaze = world.fogColor;
                }
                Vector3 sun = world != null ? Quaternion.Euler(world.sunPitch, world.sunYaw, 0f) * Vector3.forward : new Vector3(0.3f, -0.7f, 0.6f);
                sky.Build(targetCamera != null ? targetCamera : Camera.main, sun, 17);
            }
            if (details.Count == 0)
            {
                foreach (Transform group in detailGroups)
                {
                    if (group == null) continue;
                    foreach (Transform child in group) details.Add(child.gameObject);
                }
            }
            ApplyQuality();
            if (quality != null)
            {
                quality.TierChanged -= HandleTier;
                quality.TierChanged += HandleTier;
            }
        }

        private void HandleTier(QualityTier tier) => ApplyQuality();

        // Densidade do perfil: corta uma fração estável das peças de detalhe; fogo segue o perfil.
        private void ApplyQuality()
        {
            QualityTierSettings s = quality != null ? quality.Settings : new QualityTierSettings { EnvironmentDensity = 1f, FireParticles = 1f, TorchPools = true };
            float density = Mathf.Clamp01(s.EnvironmentDensity);
            for (int i = 0; i < details.Count; i++)
            {
                if (details[i] == null) continue;
                float h = Mathf.Repeat(i * 0.6180339f, 1f);
                details[i].SetActive(h < density);
            }
            foreach (EnvFlameSpawner spawner in GetComponentsInChildren<EnvFlameSpawner>(true))
                spawner.SetQuality(s.FireParticles, s.TorchPools);
        }

        public void OpenCastleGate()
        {
            if (castleDoors == null || castleDoors.IsOpen) return;
            castleDoors.Open();
            Vector3 gate = new Vector3(0f, 0f, castleZ) + EnvCastle.GateOffset(castlePlateau);
            Vfx.Burst(gate + Vector3.up * (EnvCastle.GateArchTop * 0.5f), EnvPalette.Gold, 4.5f, 1.1f);
            Vfx.Ring(gate + Vector3.up * 0.1f, EnvPalette.GateGlow, 7f, 1.2f);
        }

        private void OnDestroy()
        {
            if (quality != null) quality.TierChanged -= HandleTier;
        }
    }
}
