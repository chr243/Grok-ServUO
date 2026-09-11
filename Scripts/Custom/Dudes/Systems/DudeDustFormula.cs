using System;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Converts a Dude into DudeDust quantity. Level-based for now;
    /// hooks left for rarity / type multipliers later.
    /// </summary>
    public static class DudeDustFormula
    {
        /// <summary>Flat dust granted before level scaling.</summary>
        public static int BaseDust = 1;

        /// <summary>Extra dust per level above 1.</summary>
        public static int DustPerLevel = 1;

        /// <summary>Reserved for future rarity multipliers (1.0 = no change).</summary>
        public static double RarityMultiplier = 1.0;

        /// <summary>Reserved for future type multipliers (1.0 = no change).</summary>
        public static double TypeMultiplier = 1.0;

        public static int Calculate(DudeData data)
        {
            if (data == null)
                return Math.Max(1, BaseDust);

            int level = data.Level;
            if (level < 1)
                level = 1;

            double amount = BaseDust + ((level - 1) * DustPerLevel);
            amount *= RarityMultiplier;
            amount *= TypeMultiplier;

            // Future: branch on rarity / DudeType here without changing callers.
            int result = (int)Math.Floor(amount);

            if (result < 1)
                result = 1;

            return result;
        }
    }
}
