using System;
using System.Collections.Generic;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Central registry for Dude species. Four elemental lines × three stages (12 total).
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

        /// <summary>First wild-friendly (stage-1) match for type.</summary>
        public static DudeDefinition GetByType(DudeType type)
        {
            EnsureInitialized();

            for (int i = 0; i < m_All.Count; i++)
            {
                DudeDefinition def = m_All[i];
                if (def.Type != type)
                    continue;
                if (IsEvolutionOnly(def.Id))
                    continue;
                return def;
            }

            return null;
        }

        public static bool IsEvolutionOnly(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return false;

            string id = definitionId.ToLowerInvariant();
            return id == "flame" || id == "blaze"
                || id == "ripple" || id == "torrent"
                || id == "boulder" || id == "quake"
                || id == "gale" || id == "hurricane";
        }

        public static bool IsStage1(string definitionId)
        {
            if (string.IsNullOrEmpty(definitionId))
                return false;

            string id = definitionId.ToLowerInvariant();
            return id == "ember" || id == "droplet" || id == "pebble" || id == "breeze";
        }

        /// <summary>Follower slots: stage1=1, stage2=2, stage3=3.</summary>
        public static int GetControlSlots(string definitionId, int evolutionStage)
        {
            EnsureInitialized();

            if (evolutionStage >= 3)
                return 3;
            if (evolutionStage >= 2)
                return 2;

            DudeDefinition def = Get(definitionId);
            if (def != null && def.ControlSlots > 0)
                return def.ControlSlots;

            return 1;
        }

        public static int GetControlSlots(DudeData data)
        {
            if (data == null)
                return 1;
            return GetControlSlots(data.DefinitionId, data.EvolutionStage);
        }

        private static void RegisterDefaults()
        {
            // Hue 0 for all. ControlSlots 1/2/3 by stage. Stats keep S1/S2/S3 separation.

            // --- Fire: Ember → Flame → Blaze ---
            Register(new DudeDefinition(
                "ember", "Ember", DudeType.Fire,
                74, 0, 422, // imp body (former Embit)
                45, 40, 15, 50, 4, 7, 14,
                "blast", 1));

            Register(new DudeDefinition(
                "flame", "Flame", DudeType.Fire,
                784, 0, 0x174,
                70, 70, 35, 100, 9, 14, 20,
                "blast", 2));

            Register(new DudeDefinition(
                "blaze", "Blaze", DudeType.Fire,
                1433, 0, 357,
                110, 65, 55, 180, 13, 19, 40,
                "blast", 3));

            // --- Water: Droplet → Ripple → Torrent ---
            Register(new DudeDefinition(
                "droplet", "Droplet", DudeType.Water,
                51, 0, 0x266,
                45, 40, 15, 50, 4, 7, 14,
                "tide_crash", 1));

            Register(new DudeDefinition(
                "ripple", "Ripple", DudeType.Water,
                1244, 0, 278,
                70, 70, 35, 100, 9, 14, 20,
                "tide_crash", 2));

            Register(new DudeDefinition(
                "torrent", "Torrent", DudeType.Water,
                1427, 0, 278,
                110, 65, 55, 180, 13, 19, 40,
                "tide_crash", 3));

            // --- Earth: Pebble → Boulder → Quake ---
            Register(new DudeDefinition(
                "pebble", "Pebble", DudeType.Earth,
                196, 0, 397,
                50, 28, 12, 55, 5, 8, 16,
                "stone_slam", 1));

            Register(new DudeDefinition(
                "boulder", "Boulder", DudeType.Earth,
                829, 0, 0x174,
                90, 40, 25, 130, 10, 16, 32,
                "stone_slam", 2));

            Register(new DudeDefinition(
                "quake", "Quake", DudeType.Earth,
                1248, 0, 268,
                110, 65, 55, 180, 13, 19, 40,
                "stone_slam", 3));

            // --- Air: Breeze → Gale → Hurricane ---
            Register(new DudeDefinition(
                "breeze", "Breeze", DudeType.Air,
                58, 0, 0x1B,
                40, 50, 20, 48, 4, 7, 12,
                "gust_slash", 1));

            Register(new DudeDefinition(
                "gale", "Gale", DudeType.Air,
                199, 0, 655,
                70, 70, 35, 100, 9, 14, 20,
                "gust_slash", 2));

            Register(new DudeDefinition(
                "hurricane", "Hurricane", DudeType.Air,
                1427, 0, 655,
                110, 65, 55, 180, 13, 19, 40,
                "gust_slash", 3));
        }
    }
}
