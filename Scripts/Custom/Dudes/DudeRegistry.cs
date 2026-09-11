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
            // Classic elemental bodies (13-16) — present since early UO / UOR clients.
            // One ControlSlot each for classic follower-slot math (FollowersMax typically 5).

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
        }
    }
}
