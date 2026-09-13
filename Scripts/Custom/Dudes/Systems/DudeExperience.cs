using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// EXP award and leveling. Simple configurable formulas for v1.
    /// </summary>
    public static class DudeExperience
    {
        /// <summary>
        /// Legacy soft max (stage 1). Prefer GetMaxLevel(DudeData) for evolution-aware caps.
        /// </summary>
        public static int MaxLevel = 10;

        /// <summary>Base EXP granted per kill before level scaling.</summary>
        public static int BaseKillExp = 25;

        /// <summary>Bonus EXP per band of victim HitsMax.</summary>
        public static int KillExpPerVictimHits = 1;

        public static int GetMaxLevel(DudeData data)
        {
            int stage = data != null ? data.EvolutionStage : 1;
            return GetMaxLevel(stage);
        }

        public static int GetMaxLevel(int evolutionStage)
        {
            if (evolutionStage <= 1)
                return 10;
            if (evolutionStage == 2)
                return 20;
            return 30;
        }

        public static int GetExpRequiredForLevel(int level)
        {
            if (level < 1)
                level = 1;

            // Simple curve: 100 * level^2 (continues through L11–30)
            return 100 * level * level;
        }

        public static int CalculateKillExp(DudeData data, Mobile victim)
        {
            if (data == null || victim == null)
                return 0;

            int exp = BaseKillExp;

            BaseCreature bc = victim as BaseCreature;
            if (bc != null)
            {
                exp += Math.Max(0, bc.HitsMax / 10) * KillExpPerVictimHits;
            }

            if (data.Level > 1)
            {
                // Mild soft-scaling so high-level Dudes still gain, but slower vs trash.
                exp = Math.Max(5, exp - (data.Level - 1) * 2);
            }

            return exp;
        }

        /// <summary>
        /// Combat skill cap by evolution stage: stage1=100, stage2=110, stage3=120.
        /// NOT level-based.
        /// </summary>
        public static double GetCombatSkillCap(DudeData data)
        {
            int stage = data != null ? data.EvolutionStage : 1;
            return GetCombatSkillCap(stage);
        }

        public static double GetCombatSkillCap(int evolutionStage)
        {
            if (evolutionStage <= 1)
                return 100.0;
            if (evolutionStage == 2)
                return 110.0;
            return 120.0;
        }

        /// <summary>
        /// Legacy GatherSkill migration helper only — NOT for combat skills.
        /// Level 1 → 50, Level 10 → 100.
        /// </summary>
        public static double GetSkillCapForLevel(int level)
        {
            if (level < 1)
                level = 1;

            double skill = 50.0 + ((level - 1) * (50.0 / 9.0));
            if (skill > 100.0)
                skill = 100.0;
            return skill;
        }

        /// <summary>
        /// Awards EXP to the Dude stored in the ball (and live creature if present).
        /// Handles multi-level-ups and classic Str/Dex/Int/Hits bumps.
        /// </summary>
        public static void AwardExperience(DudeBall ball, int amount)
        {
            AwardExperience(ball, amount, null);
        }

        public static void AwardExperience(DudeBall ball, int amount, Mobile notify)
        {
            if (ball == null || ball.StoredDude == null || amount <= 0)
                return;

            DudeData data = ball.StoredDude;
            Mobile owner = notify;
            if (owner == null)
                owner = ball.RootParent as Mobile;

            data.CurrentEXP += amount;

            DudeCreature live = ball.SummonedDude;
            if (live != null && !live.Deleted && live.Map != null && live.Map != Map.Internal)
            {
                live.PublicOverheadMessage(MessageType.Regular, 0x59, false,
                    string.Format("+{0} EXP", amount));
            }

            if (owner != null)
                owner.SendMessage(0x59, "{0} gained {1} EXP.", data.DisplayName, amount);

            int maxLevel = GetMaxLevel(data);
            int safety = 0;
            while (data.CurrentEXP >= data.EXPToNext && data.Level < maxLevel && safety < 50)
            {
                data.CurrentEXP -= data.EXPToNext;
                LevelUp(data, owner);
                safety++;
                maxLevel = GetMaxLevel(data);
            }

            // At cap: keep the bar filled (evolution gate) but do not overflow endlessly.
            if (data.Level >= maxLevel && data.CurrentEXP > data.EXPToNext)
                data.CurrentEXP = data.EXPToNext;

            ball.InvalidateProperties();

            if (live != null && !live.Deleted)
                live.ApplyData(data, false);
        }

        public static void LevelUp(DudeData data, Mobile owner)
        {
            if (data == null)
                return;

            if (data.Level >= GetMaxLevel(data))
                return;

            data.Level++;
            data.EXPToNext = GetExpRequiredForLevel(data.Level);

            // Classic UO-style bumps; HitsMax +8 so pets stay ahead of early undead.
            data.Str += 2;
            data.Dex += 2;
            data.Int += 1;
            data.HitsMax += 8;
            data.Hits = data.HitsMax;
            data.MinDamage += 1;
            data.MaxDamage += 1;

            if (data.Level % 3 == 0)
                data.VirtualArmor += 1;

            data.IsFainted = false;

            if (owner != null)
            {
                owner.SendMessage(0x44, "{0} reached level {1}!", data.DisplayName, data.Level);
                owner.PlaySound(0x1F2);
            }
        }

        /// <summary>
        /// Blast damage formula shared by Blast, Ring of Fire, and Infernox passive.
        /// </summary>
        public static int GetBlastDamage(int level)
        {
            if (level < 1)
                level = 1;
            return 8 + (level * 2);
        }
    }
}
