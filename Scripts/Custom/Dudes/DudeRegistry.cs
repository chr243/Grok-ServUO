using System;
using System.Collections.Generic;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Central registry for Dude species. ONE definition per element type (Fire/Water/
    /// Earth/Air) — Dudes grow within their type instead of being separate species per stage.
    /// Stats come from <see cref="DudeTypeProfiles"/>; the definition only carries the
    /// presentation/identity bits (body, hue, sound, default ability id).
    ///
    /// Legacy id compatibility: old saves used per-stage ids (ember/flame/blaze, ...).
    /// <see cref="Get"/> still resolves those to the owning type so pre-existing balls
    /// render, even though the beta wipe means nothing depends on it.
    /// </summary>
    public static class DudeRegistry
    {
        private static readonly Dictionary<string, DudeDefinition> m_ById =
            new Dictionary<string, DudeDefinition>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<DudeType, DudeDefinition> m_ByType =
            new Dictionary<DudeType, DudeDefinition>();

        private static readonly List<DudeDefinition> m_All = new List<DudeDefinition>();

        private static bool m_Initialized;

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
                m_ById.Add(def.Id, def);

            if (!m_ByType.ContainsKey(def.Type))
            {
                m_ByType.Add(def.Type, def);
                m_All.Add(def);
            }
        }

        /// <summary>Resolve a definition by id. Accepts current type ids and legacy stage ids.</summary>
        public static DudeDefinition Get(string id)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(id))
                return null;

            DudeDefinition def;
            if (m_ById.TryGetValue(id, out def))
                return def;

            DudeType type;
            if (TryParseLegacyId(id, out type))
                return GetByType(type);

            return null;
        }

        public static DudeDefinition GetByType(DudeType type)
        {
            EnsureInitialized();

            DudeDefinition def;
            if (m_ByType.TryGetValue(type, out def))
                return def;

            return null;
        }

        public static IList<DudeDefinition> GetAll()
        {
            EnsureInitialized();
            return m_All.AsReadOnly();
        }

        /// <summary>Follower slots are a global stage property now (1 / 2 / 3).</summary>
        public static int GetControlSlots(string definitionId, int evolutionStage)
        {
            return DudeStage.ControlSlots(evolutionStage);
        }

        public static int GetControlSlots(DudeData data)
        {
            if (data == null)
                return DudeStage.ControlSlots(1);
            return DudeStage.ControlSlots(data.EvolutionStage);
        }

        /// <summary>
        /// Maps historical per-stage ids to their element type. Keeps old saves/tooling working.
        /// </summary>
        private static bool TryParseLegacyId(string id, out DudeType type)
        {
            type = DudeType.Fire;

            switch (id.ToLowerInvariant())
            {
                case "fire":
                case "ember":
                case "flame":
                case "blaze":
                    type = DudeType.Fire;
                    return true;

                case "water":
                case "droplet":
                case "ripple":
                case "torrent":
                    type = DudeType.Water;
                    return true;

                case "earth":
                case "pebble":
                case "boulder":
                case "quake":
                    type = DudeType.Earth;
                    return true;

                case "air":
                case "breeze":
                case "gale":
                case "hurricane":
                    type = DudeType.Air;
                    return true;

                default:
                    return false;
            }
        }

        private static void RegisterDefaults()
        {
            DudeTypeProfiles.EnsureInitialized();

            DudeType[] types = new DudeType[]
            {
                DudeType.Fire, DudeType.Water, DudeType.Earth, DudeType.Air
            };

            for (int i = 0; i < types.Length; i++)
            {
                DudeType type = types[i];
                DudeTypeProfile p = DudeTypeProfiles.Get(type);
                if (p == null)
                    continue;

                // Body always human male 0x190. Hue = type color (shorts + ball).
                // Stats here mirror the profile for legacy callers; DudeData derives its own.
                Register(new DudeDefinition(
                    p.Id, p.Name, p.Type,
                    HumanMaleBody, p.ShortsHue, p.SoundId,
                    p.BaseStr, p.BaseDex, p.BaseInt, p.BaseHits,
                    p.BaseMinDamage, p.BaseMaxDamage, p.BaseVirtualArmor,
                    null, DudeStage.ControlSlots(1)));
            }
        }
    }
}