using System;
using System.Collections.Generic;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Central registry for Dude species. Four elemental lines × three stages (12 total).
    /// All Dudes use human male body 0x190; Hue is the type color for shorts.
    /// </summary>
    public static class DudeRegistry
    {
        private static readonly Dictionary<string, DudeDefinition> m_ById =
            new Dictionary<string, DudeDefinition>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<DudeDefinition> m_All = new List<DudeDefinition>();

        private static bool m_Initialized;

        // Type colors (match DudeBall filled hues) — shown on shorts, not body tint.
        private const int FireHue = 0x21;
        private const int WaterHue = 0x5A;
        private const int EarthHue = 0x22C;
        private const int AirHue = 0x47E;
        private const int HumanMaleBody = 0x190;

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
            // Body always human male 0x190. Hue = type color for shorts.

            // --- Fire: Ember → Flame → Blaze ---
            Register(new DudeDefinition(
                "ember", "Ember", DudeType.Fire,
                HumanMaleBody, FireHue, 422,
                45, 40, 15, 50, 4, 7, 14,
                "blast", 1));

            Register(new DudeDefinition(
                "flame", "Flame", DudeType.Fire,
                HumanMaleBody, FireHue, 0x174,
                70, 70, 35, 100, 9, 14, 20,
                "ring_of_fire", 2));

            Register(new DudeDefinition(
                "blaze", "Blaze", DudeType.Fire,
                HumanMaleBody, FireHue, 357,
                110, 65, 55, 180, 13, 19, 40,
                "burn", 3));

            // --- Water: Droplet → Ripple → Torrent ---
            Register(new DudeDefinition(
                "droplet", "Droplet", DudeType.Water,
                HumanMaleBody, WaterHue, 0x266,
                45, 40, 15, 50, 4, 7, 14,
                "tide_mend", 1));

            Register(new DudeDefinition(
                "ripple", "Ripple", DudeType.Water,
                HumanMaleBody, WaterHue, 278,
                70, 70, 35, 100, 9, 14, 20,
                "tide_chorus", 2));

            Register(new DudeDefinition(
                "torrent", "Torrent", DudeType.Water,
                HumanMaleBody, WaterHue, 278,
                110, 65, 55, 180, 13, 19, 40,
                "spring", 3));

            // --- Earth: Pebble → Boulder → Quake ---
            Register(new DudeDefinition(
                "pebble", "Pebble", DudeType.Earth,
                HumanMaleBody, EarthHue, 397,
                50, 28, 12, 55, 5, 8, 16,
                "fault_strike", 1));

            Register(new DudeDefinition(
                "boulder", "Boulder", DudeType.Earth,
                HumanMaleBody, EarthHue, 0x174,
                90, 40, 25, 130, 10, 16, 32,
                "aftershock", 2));

            Register(new DudeDefinition(
                "quake", "Quake", DudeType.Earth,
                HumanMaleBody, EarthHue, 268,
                110, 65, 55, 180, 13, 19, 40,
                "faultline", 3));

            // --- Air: Breeze → Gale → Hurricane ---
            Register(new DudeDefinition(
                "breeze", "Breeze", DudeType.Air,
                HumanMaleBody, AirHue, 0x1B,
                40, 50, 20, 48, 4, 7, 12,
                "tailwind_self", 1));

            Register(new DudeDefinition(
                "gale", "Gale", DudeType.Air,
                HumanMaleBody, AirHue, 655,
                70, 70, 35, 100, 9, 14, 20,
                "tailwind", 2));

            Register(new DudeDefinition(
                "hurricane", "Hurricane", DudeType.Air,
                HumanMaleBody, AirHue, 655,
                110, 65, 55, 180, 13, 19, 40,
                "slipstream", 3));
        }
    }
}
