using System;
using UnityEngine;

namespace LegionRush
{
    // Carteira offline (ouro e gemas obtidos no jogo). Gastos são atômicos: validam saldo e gravam na hora.
    // Compras com dinheiro real não existem aqui; integração com lojas de plataforma fica para uma versão futura.
    public static class Economy
    {
        public static event Action<int, int> WalletChanged;

        public static int Gold => SaveManager.Current.wallet.gold;
        public static int Gems => SaveManager.Current.wallet.gems;

        public static void Add(int gold, int gems, string reason)
        {
            if (gold <= 0 && gems <= 0) return;
            WalletData wallet = SaveManager.Current.wallet;
            wallet.gold = SafeAdd(wallet.gold, Math.Max(0, gold));
            wallet.gems = SafeAdd(wallet.gems, Math.Max(0, gems));
            SaveManager.Current.records.totalGoldEarned += Math.Max(0, gold);
            SaveManager.MarkDirty();
            GameLog.Info($"Carteira +{gold} ouro, +{gems} gemas ({reason}). Saldo: {wallet.gold} / {wallet.gems}.");
            WalletChanged?.Invoke(wallet.gold, wallet.gems);
        }

        public static bool CanAfford(int gold, int gems)
        {
            WalletData wallet = SaveManager.Current.wallet;
            return gold >= 0 && gems >= 0 && wallet.gold >= gold && wallet.gems >= gems;
        }

        public static bool TrySpend(int gold, int gems, string reason)
        {
            if (!CanAfford(gold, gems))
            {
                GameLog.Info($"Saldo insuficiente para {reason} ({gold} ouro, {gems} gemas).");
                return false;
            }
            WalletData wallet = SaveManager.Current.wallet;
            wallet.gold -= gold;
            wallet.gems -= gems;
            if (!SaveManager.Save())
            {
                wallet.gold += gold;
                wallet.gems += gems;
                GameLog.Error($"Compra '{reason}' revertida: falha ao gravar.");
                return false;
            }
            GameLog.Info($"Gasto: {gold} ouro, {gems} gemas ({reason}). Saldo: {wallet.gold} / {wallet.gems}.");
            WalletChanged?.Invoke(wallet.gold, wallet.gems);
            return true;
        }

        private static int SafeAdd(int current, int amount)
        {
            long sum = (long)current + amount;
            return (int)Math.Min(sum, int.MaxValue);
        }
    }

    public struct LevelResult
    {
        public int LevelNumber;
        public bool Victory;
        public int Gold;
        public int Gems;
        public int Kills;
        public int Survivors;
        public int PeakArmy;
        public bool BossDefeated;
        public float Seconds;
        public int SkillsUsed;
    }

    public struct LevelOutcome
    {
        public int Stars;
        public int Score;
        public bool NewBestScore;
        public bool NewStars;
        public bool UnlockedNext;
        public int GoldEarned;
        public int GemsEarned;
    }

    // Registra o resultado da fase: estrelas, recordes, desbloqueio da próxima fase e recompensas na carteira.
    public static class Progression
    {
        public static int ComputeStars(LevelResult result)
        {
            if (!result.Victory) return 0;
            float survival = result.PeakArmy > 0 ? result.Survivors / (float)result.PeakArmy : 0f;
            if (survival >= 0.7f) return 3;
            if (survival >= 0.4f) return 2;
            return 1;
        }

        public static int ComputeScore(LevelResult result)
        {
            int score = result.Kills * 10 + result.Survivors * 25 + result.Gold + (result.BossDefeated ? 1000 : 0);
            if (result.Victory) score += Mathf.Max(0, 600 - Mathf.RoundToInt(result.Seconds * 2f));
            return Mathf.Max(0, score);
        }

        public static LevelOutcome Record(LevelResult result)
        {
            SaveData save = SaveManager.Current;
            LevelRecord record = save.progress.Get(result.LevelNumber, true);
            var outcome = new LevelOutcome { Stars = ComputeStars(result), Score = ComputeScore(result) };

            record.attempts++;
            if (result.Victory)
            {
                record.completed = true;
                if (outcome.Stars > record.stars)
                {
                    record.stars = outcome.Stars;
                    outcome.NewStars = true;
                }
                if (record.bestTimeSeconds <= 0f || result.Seconds < record.bestTimeSeconds) record.bestTimeSeconds = result.Seconds;
                if (save.progress.highestUnlocked <= result.LevelNumber)
                {
                    save.progress.highestUnlocked = result.LevelNumber + 1;
                    outcome.UnlockedNext = true;
                }
                save.records.victories++;
            }
            if (outcome.Score > record.bestScore)
            {
                record.bestScore = outcome.Score;
                outcome.NewBestScore = true;
            }

            save.records.levelsPlayed++;
            save.records.totalKills += result.Kills;
            if (result.BossDefeated) save.records.bossesDefeated++;
            save.records.bestArmySize = Mathf.Max(save.records.bestArmySize, result.PeakArmy);
            float goldMultiplier = MetaService.ComputeBonuses(false).Gold;
            outcome.GoldEarned = Mathf.RoundToInt(result.Gold * goldMultiplier);
            outcome.GemsEarned = result.Gems;
            Economy.Add(outcome.GoldEarned, outcome.GemsEarned, $"fase {result.LevelNumber}");
            MetaService.ReportLevel(result, outcome.Stars, outcome.GoldEarned);
            SaveManager.Save();
            return outcome;
        }
    }
}
