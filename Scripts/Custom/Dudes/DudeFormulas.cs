using System;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Result of deriving a Dude's combat stats from (type, level, IVs, stage).
    /// Plain struct — no allocation, no persistence.
    /// </summary>
    public struct DudeStats
    {
        public int Str;
        public int Dex;
        public int Int;
        public int ManaMax;
        public int HitsMax;
        public int MinDamage;
        public int MaxDamage;
        public int VirtualArmor;
        public double Wrestling;
        public double Tactics;
        public double Anatomy;
        public double MagicResist;
    }

    /// <summary>
    /// Pure derivation: (DudeType, Level, IVs, AscensionStage) → combat stats.
    ///
    /// Nothing derived is ever persisted. This is the single authority for Str/Dex/Int/Hits/
    /// damage/armor/skills, so a recalled Dude can never disagree with its saved self.
    /// Cost is a few dozen multiply-adds — safe to call on access.
    ///
    /// ASSUMPTION: the placeholder curves below (scaling, mana, per-level melee damage and the
    /// LPF table) are first-pass values and are meant to be retuned against the live server.
    /// </summary>
    public static class DudeFormulas
    {
        /// <summary>Shared level-power factor. Shape of the power curve for every type.</summary>
        private static readonly double[] Lpf = new double[]
        {
            0.05, 0.18, 0.37, 0.60, 0.88, 1.20, 1.56, 1.96, 2.40, 2.88,   // L1-10
            3.40, 3.96, 4.56, 5.20, 5.88, 6.60, 7.36, 8.16, 9.00, 9.88,   // L11-20
            10.80, 11.76, 12.76, 13.80, 14.88, 16.00                          // L21-26
        };

        public static int MaxLevelFor(int stage)
        {
            return DudeStage.MaxLevel(stage);
        }

        /// <summary>Level-power factor for a level (monotonic; clamped at the table ends).</summary>
        public static double PowerFactor(int level)
        {
            if (level < 1)
                level = 1;
            if (level > Lpf.Length)
                level = Lpf.Length;
            return Lpf[level - 1];
        }

        /// <summary>Hits added on reaching newLevel (shared band table × type multiplier).</summary>
        public static int HitsGainForLevel(int newLevel, DudeTypeProfile p)
        {
            DudeScalingConfig.EnsureLoaded();

            int band;
            if (newLevel <= 10)
                band = DudeScalingConfig.HitsGainL2to10;
            else if (newLevel <= 20)
                band = DudeScalingConfig.HitsGainL11to20;
            else
                band = DudeScalingConfig.HitsGainL21to30;

            double mult = p != null ? p.HitsGainMultiplier : 1.0;
            int gain = (int)Math.Round(band * mult);
            return gain < 0 ? 0 : gain;
        }

        /// <summary>EXP needed to reach the NEXT level from the given level.</summary>
        public static int ExpForLevel(int level)
        {
            if (level < 1)
                level = 1;

            DudeScalingConfig.EnsureLoaded();

            double raw = 100.0 * level * level * DudeScalingConfig.ExpScale;
            int required = (int)Math.Round(raw);
            return required < 1 ? 1 : required;
        }

        /// <summary>Mana pool for a caster-ish Dude. Per-Int curve comes from the type profile.</summary>
        public static int ManaMaxFor(DudeTypeProfile p, int intel)
        {
            int perInt = p != null && p.ManaPerInt > 0 ? p.ManaPerInt : 2;
            int mana = 10 + intel * perInt;
            return mana < 0 ? 0 : mana;
        }

        /// <summary>
        /// Pure stat derivation. IVs are the one-time ±4 rolls; stage only affects skill caps
        /// (via Clamp) since levels differ per stage.
        /// </summary>
        public static DudeStats Derive(
            DudeType type, int level, int stage,
            int ivStr, int ivDex, int ivInt,
            double wrestling, double tactics, double anatomy, double magicResist)
        {
            DudeScalingConfig.EnsureLoaded();

            DudeTypeProfile p = DudeTypeProfiles.Get(type);
            if (p == null)
                return new DudeStats();

            if (level < 1)
                level = 1;

            int maxLevel = MaxLevelFor(stage);
            if (level > maxLevel)
                level = maxLevel;

            int levelUps = level - 1;

            int str = p.BaseStr + ivStr + p.GainStr * levelUps;
            int dex = p.BaseDex + ivDex + p.GainDex * levelUps;
            int intel = p.BaseInt + ivInt + p.GainInt * levelUps;

            int hits = p.BaseHits;
            for (int lv = 2; lv <= level; lv++)
                hits += HitsGainForLevel(lv, p);

            int melee = DudeScalingConfig.MeleeDamagePerLevel * levelUps;

            int armor = p.BaseVirtualArmor;
            if (p.ArmorGainInterval > 0)
            {
                for (int lv = 2; lv <= level; lv++)
                {
                    if (lv % p.ArmorGainInterval == 0)
                        armor += p.ArmorGainAmount;
                }
            }

            double cap = DudeStage.SkillCap(stage);

            DudeStats s = new DudeStats();
            s.Str = ClampMin(str, 1);
            s.Dex = ClampMin(dex, 1);
            s.Int = ClampMin(intel, 1);
            s.HitsMax = ClampMin(hits, 1);
            s.MinDamage = ClampMin(p.BaseMinDamage + melee, 1);
            s.MaxDamage = ClampMin(p.BaseMaxDamage + melee, 1);
            s.VirtualArmor = ClampMin(armor, 0);
            s.ManaMax = ManaMaxFor(p, s.Int);
            s.Wrestling = ClampSkill(wrestling, cap);
            s.Tactics = ClampSkill(tactics, cap);
            s.Anatomy = ClampSkill(anatomy, cap);
            s.MagicResist = ClampSkill(magicResist, cap);
            return s;
        }

        public static double ClampSkill(double value, double cap)
        {
            if (value < 0.0)
                return 0.0;
            if (value > cap)
                return cap;
            return value;
        }

        private static int ClampMin(int value, int min)
        {
            return value < min ? min : value;
        }
    }
}
