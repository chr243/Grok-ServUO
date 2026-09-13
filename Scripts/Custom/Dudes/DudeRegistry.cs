using System;
using System.Collections.Generic;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Central registry for Dude species definitions. Add new Dudes by registering here.
    /// </summary>
    public static class DudeRegistry
    {
        private static readonly Dictionary<string, DudeDefinition> m_ById =
            new Dictionary<string, DudeDefinition>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<DudeDefinition> m_All = new List<DudeDefinition>();

        private static bool m_Initialized;

        public static void EnsureInitialized()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;
            RegisterDefaults();
        }

        public static void Register(DudeDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id))
                return;

            EnsureInitialized();

            if (m_ById.ContainsKey(def.Id))
                m_ById[def.Id] = def;
            else
            {
                m_ById.Add(def.Id, def);
                m_All.Add(def);
            }
        }

        public static DudeDefinition Get(string id)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(id))
                return null;

            DudeDefinition def;
            if (m_ById.TryGetValue(id, out def))
                return def;

            return null;
        }

        public static IList<DudeDefinition> GetAll()
        {
            EnsureInitialized();
            return m_All.AsReadOnly();
        }

        public static DudeDefinition GetByType(DudeType type)
        {
            EnsureInitialized();

            for (int i = 0; i < m_All.Count; i++)
            {
                if (m_All[i].Type == type)
                    return m_All[i];
            }

            return null;
        }

        private static void RegisterDefaults()
        {
            // Classic UO / UOR-safe bodies. Tiers: weak fodder < basic starters < medium < strong elite.
            // Leveling: Str+2, HitsMax+8, Min/MaxDamage+1 per level; abilities base+(DudeLevel*2).
            // Weak L1 tuned to clear skeletons/zombies; higher tiers keep clear separation.

            // --- WEAK (starter-fodder / early catch) — ControlSlots 4, reuse type abilities ---

            Register(new DudeDefinition(
                "sparkmite",
                "Sparkmite",
                DudeType.Fire,
                51,   // slime
                1358, // hot orange
                456,
                45, 40, 15,
                50,
                4, 7,
                14,
                "ember_burst",
                4));

            Register(new DudeDefinition(
                "puddling",
                "Puddling",
                DudeType.Water,
                81,   // bullfrog
                1366, // deep cyan
                0x266,
                42, 38, 18,
                52,
                4, 7,
                14,
                "tide_crash",
                4));

            Register(new DudeDefinition(
                "pebblet",
                "Pebblet",
                DudeType.Earth,
                48,   // scorpion
                2412, // dusty stone
                397,
                50, 28, 12,
                55,
                5, 8,
                16,
                "stone_slam",
                4));

            Register(new DudeDefinition(
                "breezeling",
                "Breezeling",
                DudeType.Air,
                6,    // bird
                1150, // pale air
                0x1B,
                40, 50, 20,
                48,
                4, 7,
                12,
                "gust_slash",
                4));

            // --- BASIC starters (baseline) — ControlSlots 4, elemental bodies 13–16 ---

            Register(new DudeDefinition(
                "emberling",
                "Emberling",
                DudeType.Fire,
                15,   // fire elemental body
                1359, // bright orange-red
                838,
                55, 50, 30,
                70,
                6, 10,
                18,
                "ember_burst",
                4));

            Register(new DudeDefinition(
                "tideling",
                "Tideling",
                DudeType.Water,
                16,   // water elemental body
                1365, // cyan-blue
                278,
                50, 55, 35,
                72,
                5, 9,
                16,
                "tide_crash",
                4));

            Register(new DudeDefinition(
                "stonepaw",
                "Stonepaw",
                DudeType.Earth,
                14,   // earth elemental body
                2413, // brown/stone
                268,
                65, 35, 22,
                85,
                7, 11,
                22,
                "stone_slam",
                4));

            Register(new DudeDefinition(
                "gustling",
                "Gustling",
                DudeType.Air,
                13,   // air elemental body
                1153, // pale sky
                655,
                48, 65, 40,
                68,
                5, 9,
                15,
                "gust_slash",
                4));

            // --- MEDIUM (stronger than starters, still catchable) ---

            Register(new DudeDefinition(
                "cinderfang",
                "Cinderfang",
                DudeType.Fire,
                0xC9, // hell cat
                1359,
                0x69,
                70, 70, 35,
                100,
                9, 14,
                20,
                "cinder_bite",
                4));

            Register(new DudeDefinition(
                "riptide",
                "Riptide",
                DudeType.Water,
                161,  // ice elemental
                1154, // frost blue
                268,
                65, 60, 50,
                105,
                8, 13,
                20,
                "riptide_crash",
                4));

            Register(new DudeDefinition(
                "boulderback",
                "Boulderback",
                DudeType.Earth,
                67,   // stone gargoyle
                2413,
                0x174,
                90, 40, 25,
                130,
                10, 16,
                32,
                "boulder_crush",
                4));

            // --- STRONG (elite wild, catchable, not DudeBoss) ---

            Register(new DudeDefinition(
                "pyreclaw",
                "Pyreclaw",
                DudeType.Fire,
                130,  // fire gargoyle
                1161, // bright fire (distinct from Emberling 1359)
                0x174,
                110, 65, 55,
                180,
                13, 19,
                40,
                "pyre_blast",
                4));
        }
    }
}
