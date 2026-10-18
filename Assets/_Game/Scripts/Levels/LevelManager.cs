using System.Collections.Generic;
using UnityEngine;

namespace LegionRush
{
    public class LevelManager : MonoBehaviour
    {
        public enum State { Ready, Running, Victory, Defeat }

        [SerializeField] private float startInputDelay = 0.4f;
        [SerializeField] private float propSpacing = 9f;

        public State Current { get; private set; } = State.Ready;
        public bool IsPaused { get; private set; }
        public int Gold { get; private set; }
        public int Kills { get; private set; }
        public int Gems { get; private set; }
        public BossController Boss => boss;
        public LevelOutcome LastOutcome { get; private set; }
        public int PeakArmy { get; private set; }
        public float RunSeconds { get; private set; }
        public LevelConfig Config => config;
        public WorldLightingProfile World => world;
        public float Progress => commander == null ? 0f : Mathf.Clamp01(commander.transform.position.z / config.trackLength);
        public event System.Action<State> StateChanged;
        public event System.Action<bool> PauseChanged;
        public event System.Action<GateOption, int, int> GateApplied;
        public event System.Action<Vector3, int> EnemyKilled;
        public event System.Action<BossController> BossAwakened;
        public event System.Action<BossController> BossDefeated;

        private readonly List<GateRowController> gates = new List<GateRowController>();
        private LevelConfig config;
        private WorldLightingProfile world;
        private ArmyManager army;
        private CommanderController commander;
        private ArmyCameraController cameraController;
        private VisualQualityManager quality;
        private Transform enemyRoot;
        private CorpseQueue enemyCorpses;
        private BossController boss;
        private float stateTimer;
        private float slowMotionTimer;

        public void Initialize(LevelConfig levelConfig, WorldLightingProfile worldProfile, ArmyManager armyManager,
            CommanderController commanderController, ArmyCameraController cameraRig, VisualQualityManager qualityManager,
            BossDefinition bossDefinition)
        {
            config = levelConfig;
            world = worldProfile;
            army = armyManager;
            commander = commanderController;
            cameraController = cameraRig;
            quality = qualityManager;
            enemyCorpses = new CorpseQueue(body => { if (body != null) body.gameObject.SetActive(false); });

            CombatRegistry.Reset();
            BuildTrack();
            BuildGates();
            SpawnWaves();
            if (bossDefinition != null) SpawnBoss(bossDefinition);

            army.CountChanged += HandleArmyCountChanged;
            commander.SetCountLabel(army.Count);
            PeakArmy = army.Count;
            SetState(State.Ready);
            GameLog.Info($"Level {config.levelNumber} ({world.displayName}) ready: {gates.Count} gate rows, {CombatRegistry.Enemies.Count} enemies, chefão {(boss != null ? boss.Definition.displayName : "nenhum")}, {config.trackLength}m track.");
        }

        public void StartRun()
        {
            if (Current != State.Ready || stateTimer < startInputDelay) return;
            SetState(State.Running);
        }

        public void HandleEnemyDeath(Enemy enemy)
        {
            CombatRegistry.Unregister(enemy);
            enemyCorpses.Add(enemy);
            Gold += config.goldPerEnemy;
            Kills++;
            EnemyKilled?.Invoke(enemy.transform.position, config.goldPerEnemy);
        }

        public void SetPaused(bool paused)
        {
            if (paused && Current != State.Running) return;
            if (IsPaused == paused) return;
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;
            commander.ControlEnabled = !paused && Current == State.Running;
            GameAudio.SetGameplayPaused(paused);
            GameLog.Info(paused ? "Pausado." : "Retomado.");
            PauseChanged?.Invoke(paused);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && Current == State.Running) SetPaused(true);
        }

        public void HandleBossDefeated(BossController defeated)
        {
            Gold += defeated.Definition.goldReward;
            Gems += defeated.Definition.gemReward;
            Kills++;
            slowMotionTimer = 1.4f;
            if (!IsPaused) Time.timeScale = 0.35f;
            cameraController.SetZoom(1f);
            GameLog.Info($"Chefão derrotado: {defeated.Definition.displayName} (+{defeated.Definition.goldReward} ouro, +{defeated.Definition.gemReward} gemas).");
            BossDefeated?.Invoke(defeated);
        }

        // Invocações do chefão: surgem em círculo e já entram em combate.
        public void SpawnSummons(Vector3 center, int count, Color color, float healthMultiplier)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)Mathf.Max(1, count)) * Mathf.PI * 2f + Random.value * 0.5f;
                float radius = 1.2f + Random.value * 1.8f;
                Vector3 p = center + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
                p.x = Mathf.Clamp(p.x, -config.trackHalfWidth + 0.4f, config.trackHalfWidth - 0.4f);
                p.y = 0f;
                Enemy enemy = SpawnEnemy(p, healthMultiplier, color);
                enemy.ForceAggro();
                Vfx.Burst(p + Vector3.up * 0.6f, new Color(color.r, color.g, color.b, 0.7f), 0.9f, 0.4f);
            }
        }

        private void OnDestroy()
        {
            if (army != null) army.CountChanged -= HandleArmyCountChanged;
            if (boss != null)
            {
                boss.Awakened -= HandleBossAwakened;
            }
            if (!IsPaused && slowMotionTimer <= 0f) return;
            Time.timeScale = 1f;
            GameAudio.SetGameplayPaused(false);
        }

        private void Update()
        {
            if (config == null) return;
            float dt = Time.deltaTime;
            enemyCorpses.Tick(dt);
            if (slowMotionTimer > 0f)
            {
                slowMotionTimer -= Time.unscaledDeltaTime;
                if (slowMotionTimer <= 0f && !IsPaused) Time.timeScale = 1f;
            }
            if (IsPaused) return;
            stateTimer += dt;

            if (Current == State.Running)
            {
                TickRunning(dt);
            }
        }

        private void TickRunning(float deltaTime)
        {
            RunSeconds += deltaTime;
            Vector3 commanderPosition = commander.transform.position;
            bool engaged = false;
            List<Enemy> enemies = CombatRegistry.Enemies;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                if (i >= enemies.Count) continue;
                Enemy enemy = enemies[i];
                enemy.Tick(deltaTime, commanderPosition);
                if (enemy.IsAggro && enemy.IsAlive) engaged = true;
            }
            if (boss != null && boss.IsAlive)
            {
                boss.Tick(deltaTime, commanderPosition);
                if (boss.IsEngaged) engaged = true;
            }
            commander.SetEngaged(engaged);

            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i].TryResolve(commanderPosition, out GateOption option)) ApplyGate(option);
            }

            if (army.Count == 0)
            {
                SetState(State.Defeat);
                return;
            }
            bool bossDown = boss == null || !boss.IsAlive;
            if (commanderPosition.z >= config.trackLength && !engaged && bossDown) SetState(State.Victory);
        }

        // Aplica todos os efeitos do portal escolhido. Cada efeito altera o estado real (tropas, bônus, inimigos, recursos, rota).
        private void ApplyGate(GateOption option)
        {
            int before = army.Count;
            foreach (GateEffect effect in option.effects)
            {
                if (effect == null) continue;
                int amount = Mathf.Max(0, Mathf.RoundToInt(effect.value));
                switch (effect.type)
                {
                    case GateEffectType.AddTroops:
                        if (effect.anyClass) army.SetCount(army.Count + amount);
                        else army.AddTroops(effect.unitClass, amount);
                        break;
                    case GateEffectType.MultiplyTroops:
                        army.MultiplyTroops(effect.value, army.Config.maxMultiplyGain);
                        break;
                    case GateEffectType.RemoveTroops:
                        army.SetCount(army.Count - amount);
                        break;
                    case GateEffectType.RemovePercent:
                        army.SetCount(Mathf.FloorToInt(army.Count * (1f - Mathf.Clamp01(effect.value))));
                        break;
                    case GateEffectType.DamageBoost:
                        army.AddDamageBonus(effect.value, effect.duration);
                        break;
                    case GateEffectType.AttackSpeedBoost:
                        army.AddAttackSpeedBonus(effect.value, effect.duration);
                        break;
                    case GateEffectType.Shield:
                        army.ShieldAll(effect.value, Mathf.Max(1f, effect.duration));
                        Vfx.Bubble(commander.transform, army.FormationRadius + 1.8f, new Color(0.35f, 0.75f, 1f, 0.28f), Mathf.Max(1f, effect.duration));
                        break;
                    case GateEffectType.HealAll:
                        army.HealAllPercent(Mathf.Clamp01(effect.value));
                        GameAudio.Play(SoundId.HealingSpell);
                        break;
                    case GateEffectType.LevelUp:
                        army.LevelUp(effect.anyClass, effect.unitClass, Mathf.Max(1, amount));
                        break;
                    case GateEffectType.GrantGold:
                        Gold += amount;
                        GameAudio.Play(SoundId.CoinPickup);
                        break;
                    case GateEffectType.GrantGems:
                        Gems += amount;
                        GameAudio.Play(SoundId.GemPickup);
                        break;
                    case GateEffectType.SpawnEnemies:
                        SpawnAmbush(amount);
                        break;
                    case GateEffectType.ReduceEnemies:
                        ReduceEnemiesAhead(Mathf.Clamp01(effect.value));
                        break;
                    case GateEffectType.SkipAhead:
                        SkipAhead(effect.value);
                        break;
                }
            }
            int after = army.Count;
            cameraController.Shake(option.IsPositive ? 0.15f : 0.5f);
            GameLog.Info($"Portal {option.title} {option.subtitle}: {before} -> {after}");
            GateApplied?.Invoke(option, before, after);
        }

        private void SpawnAmbush(int count)
        {
            if (count <= 0) return;
            float z = commander.transform.position.z + 22f;
            int columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(count * 1.6f)), 1, Mathf.FloorToInt(config.trackHalfWidth * 2f / 0.9f));
            for (int i = 0; i < count; i++)
            {
                float x = (i % columns - (columns - 1) * 0.5f) * 0.9f;
                SpawnEnemy(new Vector3(x, 0f, z + (i / columns) * 0.9f), 1f, config.enemy.color);
            }
        }

        private void ReduceEnemiesAhead(float fraction)
        {
            float minZ = commander.transform.position.z + 5f;
            List<Enemy> enemies = CombatRegistry.Enemies;
            int removed = 0;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                Enemy enemy = enemies[i];
                if (enemy.IsAggro || !enemy.IsAlive || enemy.transform.position.z < minZ) continue;
                if (Random.value > fraction) continue;
                Vfx.Burst(enemy.transform.position + Vector3.up * 0.6f, new Color(0.65f, 0.35f, 1f, 0.7f), 0.7f, 0.4f);
                CombatRegistry.Unregister(enemy);
                enemy.gameObject.SetActive(false);
                removed++;
            }
            GameLog.Info($"Portal removeu {removed} inimigos à frente.");
        }

        // Atalho: o exército avança instantaneamente; portais do trecho pulado somem sem efeito.
        private void SkipAhead(float meters)
        {
            Vector3 position = commander.transform.position;
            float target = Mathf.Min(position.z + Mathf.Max(0f, meters), config.trackLength - 15f);
            float delta = target - position.z;
            if (delta <= 0f) return;
            commander.transform.position = new Vector3(position.x, position.y, target);
            army.ApplyToAll(soldier => soldier.transform.position += new Vector3(0f, 0f, delta));
            foreach (GateRowController gate in gates)
            {
                if (!gate.Consumed && gate.Z <= target) gate.Skip();
            }
            Vfx.Ring(commander.transform.position, new Color(0.2f, 0.8f, 0.7f, 0.8f), 6f, 0.6f);
        }

        private void HandleArmyCountChanged(int previous, int current)
        {
            commander.SetCountLabel(current);
            if (current > PeakArmy) PeakArmy = current;
        }

        private void RecordResult(bool victory)
        {
            var result = new LevelResult
            {
                LevelNumber = config.levelNumber,
                Victory = victory,
                Gold = Gold,
                Gems = Gems,
                Kills = Kills,
                Survivors = army.Count,
                PeakArmy = Mathf.Max(1, PeakArmy),
                BossDefeated = boss != null && !boss.IsAlive,
                Seconds = RunSeconds
            };
            LastOutcome = Progression.Record(result);
            GameLog.Info($"Resultado: {(victory ? "vitória" : "derrota")}, {LastOutcome.Stars} estrelas, pontuação {LastOutcome.Score}{(LastOutcome.NewBestScore ? " (recorde)" : string.Empty)}.");
        }

        private void SetState(State next)
        {
            Current = next;
            stateTimer = 0f;
            switch (next)
            {
                case State.Ready:
                    commander.ControlEnabled = false;
                    commander.AdvanceEnabled = false;
                    break;
                case State.Running:
                    commander.ControlEnabled = true;
                    commander.AdvanceEnabled = true;
                    break;
                case State.Victory:
                    slowMotionTimer = 0f;
                    Time.timeScale = 1f;
                    Gold += config.victoryGold;
                    commander.ControlEnabled = false;
                    commander.AdvanceEnabled = false;
                    cameraController.SetZoom(0.7f);
                    army.PlayVictory();
                    RecordResult(true);
                    break;
                case State.Defeat:
                    commander.ControlEnabled = false;
                    commander.AdvanceEnabled = false;
                    cameraController.Shake(0.8f);
                    RecordResult(false);
                    break;
            }
            GameLog.Info("State -> " + next);
            StateChanged?.Invoke(next);
        }

        // ---------- Cenário ----------

        private void BuildTrack()
        {
            Transform track = new GameObject("Track").transform;
            track.SetParent(transform, false);

            float hw = config.trackHalfWidth;
            float length = config.trackLength + 60f;
            float centerZ = config.trackLength * 0.5f;
            Material road = MaterialLibrary.Get(world.roadColor, 0f, 0f, false);
            Material ground = MaterialLibrary.Get(world.groundColor, 0f, 0f, false);
            Material border = MaterialLibrary.Get(world.borderColor, 0f, 0.1f);
            Material finish = MaterialLibrary.Get(new Color(1f, 0.8f, 0.25f), 1.5f, 0f, false);

            PrimitiveFactory.Part(PrimitiveType.Cube, track, new Vector3(0f, -0.5f, centerZ), new Vector3(hw * 2f, 1f, length), road, default, false);
            PrimitiveFactory.Part(PrimitiveType.Cube, track, new Vector3(0f, -0.6f, centerZ), new Vector3(80f, 1f, length), ground, default, false);
            for (int side = -1; side <= 1; side += 2)
            {
                PrimitiveFactory.Part(PrimitiveType.Cube, track, new Vector3(side * (hw + 0.25f), 0.25f, centerZ),
                    new Vector3(0.5f, 0.5f, length), border);
            }

            var rng = new System.Random(config.levelNumber * 7919 + (int)world.world * 131);
            int propIndex = 0;
            float lateralStart = world.lavaChannels ? hw + 5.6f : hw + 2f;
            for (float z = -20f; z < config.trackLength + 30f; z += propSpacing)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    float x = side * (lateralStart + (float)rng.NextDouble() * 10f);
                    float height = 1.6f + (float)rng.NextDouble() * 1.4f;
                    Vector3 position = new Vector3(x, 0f, z + (float)rng.NextDouble() * 4f);
                    BuildProp(track, position, height, rng);
                    if (propIndex % 3 == 0) BuildAccent(track, new Vector3(side * (lateralStart - 0.8f), 0f, z + 2f), rng);
                    propIndex++;
                }
            }

            float finishZ = config.trackLength;
            PrimitiveFactory.Part(PrimitiveType.Cube, track, new Vector3(0f, 0.02f, finishZ), new Vector3(hw * 2f, 0.05f, 1.2f), finish, default, false);
            for (int side = -1; side <= 1; side += 2)
                PrimitiveFactory.Part(PrimitiveType.Cube, track, new Vector3(side * (hw + 0.8f), 2.75f, finishZ), new Vector3(1f, 5.5f, 1f), border);
            PrimitiveFactory.Part(PrimitiveType.Cube, track, new Vector3(0f, 5.6f, finishZ), new Vector3(hw * 2f + 2.6f, 0.7f, 0.5f), border);
            TextMesh banner = PrimitiveFactory.Label(track, new Vector3(0f, 5.6f, finishZ - 0.3f), "VITÓRIA", 120, 0.07f, new Color(1f, 0.85f, 0.35f));
            banner.transform.localRotation = Quaternion.Euler(20f, 0f, 0f);

            WorldAmbience ambience = track.gameObject.AddComponent<WorldAmbience>();
            ambience.Build(world, config.trackLength, hw, commander.transform, quality);
        }

        private void BuildProp(Transform parent, Vector3 position, float height, System.Random rng)
        {
            Transform prop = new GameObject("Prop").transform;
            prop.SetParent(parent, false);
            prop.localPosition = position;
            prop.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            Material primary = MaterialLibrary.Get(world.propPrimary);
            Material secondary = MaterialLibrary.Get(world.propSecondary);

            switch (world.propStyle)
            {
                case PropStyle.SnowPine:
                {
                    Material snow = MaterialLibrary.Get(world.groundColor);
                    PrimitiveFactory.Part(PrimitiveType.Cylinder, prop, new Vector3(0f, 0.4f, 0f), new Vector3(0.25f, 0.4f, 0.25f), secondary);
                    PrimitiveFactory.Part(PrimitiveType.Capsule, prop, new Vector3(0f, 0.6f + height * 0.45f, 0f), new Vector3(height * 0.6f, height * 0.45f, height * 0.6f), primary);
                    PrimitiveFactory.Part(PrimitiveType.Capsule, prop, new Vector3(0f, 0.6f + height * 0.85f, 0f), new Vector3(height * 0.38f, height * 0.3f, height * 0.38f), primary);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, prop, new Vector3(0f, 0.6f + height * 1.12f, 0f), Vector3.one * (height * 0.22f), snow);
                    break;
                }
                case PropStyle.Palm:
                {
                    float lean = (float)rng.NextDouble() * 12f - 6f;
                    Transform trunk = PrimitiveFactory.Part(PrimitiveType.Cylinder, prop, new Vector3(0f, height, 0f), new Vector3(0.22f, height, 0.22f), secondary, new Vector3(lean, 0f, lean));
                    Vector3 top = trunk.localPosition + trunk.localRotation * Vector3.up * height;
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = i * 72f;
                        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * new Vector3(0f, 0f, 0.7f);
                        PrimitiveFactory.Part(PrimitiveType.Cube, prop, top + offset, new Vector3(0.35f, 0.06f, 1.5f), primary, new Vector3(25f, angle, 0f));
                    }
                    if (rng.NextDouble() < 0.5)
                        PrimitiveFactory.Part(PrimitiveType.Cube, prop, new Vector3(1.4f, 0.45f, 0.6f), new Vector3(1.2f, 0.9f, 1.2f), secondary, new Vector3(0f, 20f, 0f));
                    break;
                }
                case PropStyle.LeafyTree:
                {
                    PrimitiveFactory.Part(PrimitiveType.Cylinder, prop, new Vector3(0f, height * 0.5f, 0f), new Vector3(0.35f, height * 0.5f, 0.35f), secondary);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, prop, new Vector3(0f, height * 1.15f, 0f), Vector3.one * (height * 1.1f), primary);
                    PrimitiveFactory.Part(PrimitiveType.Sphere, prop, new Vector3(height * 0.35f, height * 0.95f, 0.2f), Vector3.one * (height * 0.7f), primary);
                    break;
                }
                default:
                {
                    Material crack = MaterialLibrary.Get(world.accentColor, world.accentEmission, 0f, false);
                    for (int i = 0; i < 3; i++)
                    {
                        float s = height * (0.5f + 0.25f * i);
                        PrimitiveFactory.Part(PrimitiveType.Cube, prop, new Vector3(i * 0.5f - 0.5f, s * 0.45f, (float)rng.NextDouble() - 0.5f),
                            new Vector3(s * 0.6f, s, s * 0.6f), primary, new Vector3((float)rng.NextDouble() * 20f - 10f, (float)rng.NextDouble() * 90f, 45f));
                    }
                    PrimitiveFactory.Part(PrimitiveType.Cube, prop, new Vector3(0f, 0.05f, 0f), new Vector3(1.8f, 0.08f, 0.15f), crack, new Vector3(0f, 30f, 0f), false);
                    break;
                }
            }
        }

        // Cristais luminosos (gelo, magia, lava) ou ruínas quando o mundo não tem emissivo.
        private void BuildAccent(Transform parent, Vector3 position, System.Random rng)
        {
            Transform accent = new GameObject("Accent").transform;
            accent.SetParent(parent, false);
            accent.localPosition = position;
            if (world.accentEmission <= 0f)
            {
                Material block = MaterialLibrary.Get(world.borderColor);
                PrimitiveFactory.Part(PrimitiveType.Cube, accent, new Vector3(0f, 0.5f, 0f), new Vector3(0.9f, 1f, 0.9f), block, new Vector3(0f, (float)rng.NextDouble() * 40f, 0f));
                PrimitiveFactory.Part(PrimitiveType.Cube, accent, new Vector3(0.2f, 1.2f, 0f), new Vector3(0.6f, 0.45f, 0.6f), block, new Vector3(0f, (float)rng.NextDouble() * 40f, 8f));
                return;
            }

            Material crystal = MaterialLibrary.Get(world.accentColor, world.accentEmission, 0.3f);
            for (int i = 0; i < 3; i++)
            {
                float h = 0.6f + (float)rng.NextDouble() * 0.9f;
                PrimitiveFactory.Part(PrimitiveType.Cube, accent, new Vector3(i * 0.3f - 0.3f, h * 0.45f, (float)rng.NextDouble() * 0.4f),
                    new Vector3(0.22f, h, 0.22f), crystal, new Vector3((float)rng.NextDouble() * 30f - 15f, 45f, (float)rng.NextDouble() * 30f - 15f), false);
            }
        }

        private void BuildGates()
        {
            foreach (GateChoiceRow row in config.choiceRows)
            {
                var go = new GameObject($"GateRow_{row.z:0}");
                go.transform.SetParent(transform, false);
                var gate = go.AddComponent<GateRowController>();
                gate.Initialize(row, config.trackHalfWidth);
                gates.Add(gate);
            }
        }

        private void SpawnWaves()
        {
            enemyRoot = new GameObject("Enemies").transform;
            enemyRoot.SetParent(transform, false);

            foreach (EnemyWave wave in config.waves)
            {
                int maxColumns = Mathf.Max(1, Mathf.FloorToInt((config.trackHalfWidth * 2f - 1f) / wave.spacing) + 1);
                int columns = Mathf.Clamp(Mathf.CeilToInt(Mathf.Sqrt(wave.count * 1.6f)), 1, maxColumns);
                for (int i = 0; i < wave.count; i++)
                {
                    int column = i % columns;
                    int rowIndex = i / columns;
                    float x = (column - (columns - 1) * 0.5f) * wave.spacing;
                    float z = wave.z + rowIndex * wave.spacing;
                    SpawnEnemy(new Vector3(x, 0f, z), wave.healthMultiplier, config.enemy.color);
                }
            }
        }

        private Enemy SpawnEnemy(Vector3 position, float healthMultiplier, Color color)
        {
            GameObject go = CharacterFactory.CreateEnemy(enemyRoot, color);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f));
            Enemy enemy = go.AddComponent<Enemy>();
            enemy.Initialize(config.enemy, healthMultiplier, this, army);
            CombatRegistry.Register(enemy);
            return enemy;
        }

        private void SpawnBoss(BossDefinition definition)
        {
            float z = Mathf.Max(20f, config.trackLength - config.boss.distanceFromEnd);
            GameObject go = CharacterFactory.CreateBoss(definition, transform);
            go.transform.SetPositionAndRotation(new Vector3(0f, 0f, z), Quaternion.Euler(0f, 180f, 0f));
            boss = go.AddComponent<BossController>();
            boss.Initialize(definition, config.boss.healthMultiplier, this, army, cameraController, config.trackHalfWidth);
            boss.Awakened += HandleBossAwakened;
            CombatRegistry.RegisterHostile(boss);
        }

        private void HandleBossAwakened(BossController awakened) => BossAwakened?.Invoke(awakened);
    }
}
