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
            // Leveling (unchanged): Str+2, HitsMax+5, Min/MaxDamage+1 per level; abilities base+(DudeLevel*2).

            // --- WEAK (starter-fodder / early catch) — ControlSlots 1, reuse type abilities ---

            Register(new DudeDefinition(
                "sparkmite",
                "Sparkmite",
                DudeType.Fire,
                51,   // slime
                1358, // hot orange
                456,
                18, 30, 10,
                20,
                1, 3,
                4,
                "ember_burst",
                1));

            Register(new DudeDefinition(
                "puddling",
                "Puddling",
                DudeType.Water,
                81,   // bullfrog
                1366, // deep cyan
                0x266,
                16, 28, 12,
                22,
                1, 3,
                4,
                "tide_crash",
                1));

            Register(new DudeDefinition(
                "pebblet",
                "Pebblet",
                DudeType.Earth,
                48,   // scorpion
                2412, // dusty stone
                397,
                22, 15, 8,
                28,
                2, 4,
                8,
                "stone_slam",
                1));

            Register(new DudeDefinition(
                "breezeling",
                "Breezeling",
                DudeType.Air,
                6,    // bird
                1150, // pale air
                0x1B,
                14, 35, 15,
                18,
                1, 3,
                3,
                "gust_slash",
                1));

            // --- BASIC starters (baseline) — ControlSlots 1, elemental bodies 13–16 ---

            Register(new DudeDefinition(
                "emberling",
                "Emberling",
                DudeType.Fire,
                15,   // fire elemental body
                1359, // bright orange-red
                838,
                35, 40, 20,
                40,
                4, 7,
                12,
                "ember_burst",
                1));

            Register(new DudeDefinition(
                "tideling",
                "Tideling",
                DudeType.Water,
                16,   // water elemental body
                1365, // cyan-blue
                278,
                32, 45, 25,
                42,
                3, 6,
                10,
                "tide_crash",
                1));

            Register(new DudeDefinition(
                "stonepaw",
                "Stonepaw",
                DudeType.Earth,
                14,   // earth elemental body
                2413, // brown/stone
                268,
                50, 25, 15,
                55,
                5, 8,
                18,
                "stone_slam",
                1));

            Register(new DudeDefinition(
                "gustling",
                "Gustling",
                DudeType.Air,
                13,   // air elemental body
                1153, // pale sky
                655,
                28, 55, 30,
                35,
                3, 6,
                8,
                "gust_slash",
                1));

            // --- MEDIUM (stronger than starters, still catchable) ---

            Register(new DudeDefinition(
                "cinderfang",
                "Cinderfang",
                DudeType.Fire,
                0xC9, // hell cat
                1359,
                0x69,
                55, 60, 25,
                70,
                7, 11,
                16,
                "cinder_bite",
                1));

            Register(new DudeDefinition(
                "riptide",
                "Riptide",
                DudeType.Water,
                161,  // ice elemental
                1154, // frost blue
                268,
                48, 50, 40,
                78,
                6, 10,
                16,
                "riptide_crash",
                1));

            Register(new DudeDefinition(
                "boulderback",
                "Boulderback",
                DudeType.Earth,
                67,   // stone gargoyle
                2413,
                0x174,
                72, 28, 18,
                95,
                8, 13,
                28,
                "boulder_crush",
                2));

            // --- STRONG (elite wild, catchable, not DudeBoss) ---

            Register(new DudeDefinition(
                "pyreclaw",
                "Pyreclaw",
                DudeType.Fire,
                130,  // fire gargoyle
                1161, // bright fire (distinct from Emberling 1359)
                0x174,
                95, 55, 50,
                145,
                11, 16,
                35,
                "pyre_blast",
                2));
        }
    }
}
