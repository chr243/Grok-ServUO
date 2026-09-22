namespace Server.Custom.Dudes
{
    /// <summary>
    /// Everything that varies by Dude type, in one place. One profile == one type.
    /// Adding a new type means adding an enum value + one profile row; no switches anywhere else.
    /// Mutable fields so DudeTypeProfiles can overlay Data/DudeTypes.cfg for live tuning.
    /// </summary>
    public sealed class DudeTypeProfile
    {
        public DudeType Type;

        /// <summary>Display name, e.g. "Fire". Displayed as "{Name} Dude".</summary>
        public string Name;

        /// <summary>Shorts + Dude Ball hue for this type.</summary>
        public int ShortsHue;

        /// <summary>How skin hue is chosen: "Random" (default) or "Fixed".</summary>
        public string SkinHueMode = "Random";

        /// <summary>Summon/despawn sound id.</summary>
        public int SoundId;

        /// <summary>Summon/despawn particle family (see DudeSummonEffects).</summary>
        public DudeVfx Vfx;

        /// <summary>Wild-spawn weight. 0 = never spawns in the wild.</summary>
        public int SpawnWeight;

        // --- Level 1 base stats ---
        public int BaseStr;
        public int BaseDex;
        public int BaseInt;
        public int BaseHits;
        public int BaseMinDamage;
        public int BaseMaxDamage;
        public int BaseVirtualArmor;

        // --- Per level-up gains ---
        public int GainStr;
        public int GainDex;
        public int GainInt;

        /// <summary>Mana gained per point of Int (Water caster = 4, others 2).</summary>
        public int ManaPerInt = 2;

        /// <summary>Starter type sash item granted to a wild Dude of this type (see DudeGearSet).</summary>
        public System.Type StarterSashType;

        /// <summary>Multiplies the shared Hits-per-level band table (Earth = 1.5 tank).</summary>
        public double HitsGainMultiplier;

        /// <summary>Virtual armor added every ArmorGainInterval levels (Earth: 2 every level).</summary>
        public int ArmorGainAmount;
        public int ArmorGainInterval;

        public DudeTypeProfile(
            DudeType type, string name, int shortsHue, int soundId, DudeVfx vfx, int spawnWeight,
            int baseStr, int baseDex, int baseInt, int baseHits,
            int baseMinDamage, int baseMaxDamage, int baseVirtualArmor,
            int gainStr, int gainDex, int gainInt,
            double hitsGainMultiplier, int armorGainAmount, int armorGainInterval)
        {
            Type = type;
            Name = name;
            ShortsHue = shortsHue;
            SoundId = soundId;
            Vfx = vfx;
            SpawnWeight = spawnWeight;
            BaseStr = baseStr;
            BaseDex = baseDex;
            BaseInt = baseInt;
            BaseHits = baseHits;
            BaseMinDamage = baseMinDamage;
            BaseMaxDamage = baseMaxDamage;
            BaseVirtualArmor = baseVirtualArmor;
            GainStr = gainStr;
            GainDex = gainDex;
            GainInt = gainInt;
            HitsGainMultiplier = hitsGainMultiplier;
            ArmorGainAmount = armorGainAmount;
            ArmorGainInterval = armorGainInterval;
        }

        /// <summary>Stable registry id for this type (lowercase name).</summary>
        public string Id
        {
            get { return Type.ToString().ToLowerInvariant(); }
        }

        /// <summary>Always "{Name} Dude" — renaming is off.</summary>
        public string DisplayName
        {
            get { return Name + " Dude"; }
        }
    }
}
