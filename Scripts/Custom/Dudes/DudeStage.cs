namespace Server.Custom.Dudes
{
    /// <summary>
    /// Global ascension table. Deliberately NOT per type: every Dude follows the same
    /// stage ladder (max level, follower slots, gear slots, skill cap, summon radius).
    /// </summary>
    public static class DudeStage
    {
        public const int MaxStage = 3;

        /// <summary>Max level for a stage (1 → 10, 2 → 20, 3 → 30).</summary>
        public static int MaxLevel(int stage)
        {
            if (stage <= 1)
                return 10;
            if (stage == 2)
                return 20;
            return 30;
        }

        /// <summary>Level required to ascend out of this stage (0 = cannot ascend).</summary>
        public static int AscendLevel(int stage)
        {
            if (stage == 1)
                return 10;
            if (stage == 2)
                return 20;
            return 0;
        }

        /// <summary>Follower control slots at this stage.</summary>
        public static int ControlSlots(int stage)
        {
            if (stage <= 1)
                return 1;
            if (stage == 2)
                return 2;
            return 3;
        }

        /// <summary>Gear slots unlocked at this stage.</summary>
        public static int GearSlots(int stage)
        {
            if (stage <= 1)
                return 2;
            if (stage == 2)
                return 3;
            return 4;
        }

        /// <summary>Combat skill cap at this stage (100 / 110 / 120).</summary>
        public static double SkillCap(int stage)
        {
            if (stage <= 1)
                return 100.0;
            if (stage == 2)
                return 110.0;
            return 120.0;
        }

        /// <summary>Summon/despawn effect radius at this stage (1 / 2 / 3).</summary>
        public static int SummonRadius(int stage)
        {
            if (stage <= 1)
                return 1;
            if (stage == 2)
                return 2;
            return 3;
        }
    }
}
