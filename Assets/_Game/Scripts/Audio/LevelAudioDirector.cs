using UnityEngine;

namespace LegionRush
{
    // Liga eventos da fase ao áudio. Combate chama GameAudio direto (Soldier, Enemy, Projectile).
    public class LevelAudioDirector : MonoBehaviour
    {
        [SerializeField] private float rewardDelay = 1.1f;

        private LevelManager level;
        private float rewardTimer = -1f;

        public void Initialize(LevelManager levelManager)
        {
            level = levelManager;
            level.StateChanged += HandleStateChanged;
            level.GateApplied += HandleGateApplied;
            level.EnemyKilled += HandleEnemyKilled;
            HandleStateChanged(level.Current);
        }

        private void OnDestroy()
        {
            if (level == null) return;
            level.StateChanged -= HandleStateChanged;
            level.GateApplied -= HandleGateApplied;
            level.EnemyKilled -= HandleEnemyKilled;
        }

        private void Update()
        {
            if (rewardTimer < 0f) return;
            rewardTimer -= Time.unscaledDeltaTime;
            if (rewardTimer >= 0f) return;
            GameAudio.Play(SoundId.ChestOpen);
            GameAudio.Play(SoundId.CoinPickup);
        }

        private void HandleStateChanged(LevelManager.State state)
        {
            switch (state)
            {
                case LevelManager.State.Ready:
                    rewardTimer = -1f;
                    GameAudio.PlayMusic(MusicTrack.Menu);
                    break;
                case LevelManager.State.Running:
                    GameAudio.Play(SoundId.UiConfirm);
                    GameAudio.PlayMusic(MusicTrack.Battle);
                    break;
                case LevelManager.State.Victory:
                    GameAudio.Play(SoundId.Victory);
                    rewardTimer = rewardDelay;
                    break;
                case LevelManager.State.Defeat:
                    GameAudio.Play(SoundId.Defeat);
                    break;
            }
        }

        private void HandleGateApplied(GateDef gate, int before, int after)
        {
            GameAudio.Play(gate.IsPositive ? SoundId.PortalPositive : SoundId.PortalNegative);
        }

        private void HandleEnemyKilled(Vector3 position, int gold)
        {
            if (gold > 0) GameAudio.PlayAt(SoundId.CoinPickup, position);
        }
    }
}
