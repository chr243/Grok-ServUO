using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// EXP award and leveling. Simple configurable formulas for v1.
    /// </summary>
    public static class DudeExperience
    {
        /// <summary>Base EXP granted per kill before level scaling.</summary>
        public static int BaseKillExp = 25;

        /// <summary>Bonus EXP per band of victim HitsMax.</summary>
        public static int KillExpPerVictimHits = 1;

        public static int GetExpRequiredForLevel(int level)
        {
            if (level < 1)
                level = 1;

            // Simple curve: 100 * level^2
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

            if (owner != null)
                owner.SendMessage(0x59, "{0} gained {1} EXP.", data.DisplayName, amount);

            int safety = 0;
            while (data.CurrentEXP >= data.EXPToNext && data.Level < 100 && safety < 50)
            {
                data.CurrentEXP -= data.EXPToNext;
                LevelUp(data, owner);
                safety++;
            }

            ball.InvalidateProperties();

            DudeCreature live = ball.SummonedDude;
            if (live != null && !live.Deleted)
                live.ApplyData(data, false);
        }

        public static void LevelUp(DudeData data, Mobile owner)
        {
            if (data == null)
                return;

            data.Level++;
            data.EXPToNext = GetExpRequiredForLevel(data.Level);

            // Classic UO-style bumps (modest per level).
            data.Str += 2;
            data.Dex += 2;
            data.Int += 1;
            data.HitsMax += 5;
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
    }
}
