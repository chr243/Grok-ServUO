using System;
using System.Collections.Generic;
using Server.Items;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// The single source of truth for per-type data. Every system reads a profile from here
    /// instead of switching on DudeType. To add a type: add the enum value, add one row below
    /// (or a row in Data/DudeTypes.cfg), and give it gear.
    /// </summary>
    public static class DudeTypeProfiles
    {
        private static readonly Dictionary<DudeType, DudeTypeProfile> m_ByType =
            new Dictionary<DudeType, DudeTypeProfile>();

        private static readonly List<DudeTypeProfile> m_All = new List<DudeTypeProfile>();

        private static bool m_Initialized;

        /// <summary>Types that spawn in the wild as stage 1.</summary>
        public static void EnsureInitialized()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;
            RegisterDefaults();

            // Overlay live-tunable values from Data/DudeTypes.cfg (safe no-op if absent).
            // DudeTypeConfig calls back into EnsureInitialized, which is already a no-op here.
            DudeTypeConfig.EnsureLoaded();
        }

        public static void Register(DudeTypeProfile p)
        {
            if (p == null)
                return;

            EnsureInitialized();

            if (m_ByType.ContainsKey(p.Type))
                m_ByType[p.Type] = p;
            else
            {
                m_ByType.Add(p.Type, p);
                m_All.Add(p);
            }
        }

        public static DudeTypeProfile Get(DudeType type)
        {
            EnsureInitialized();

            DudeTypeProfile p;
            if (m_ByType.TryGetValue(type, out p))
                return p;

            // Unknown type (e.g. a removed profile): fall back to the first registered type
            // so nothing crashes and existing Dudes still render.
            return m_All.Count > 0 ? m_All[0] : null;
        }

        public static IList<DudeTypeProfile> GetAll()
        {
            EnsureInitialized();
            return m_All.AsReadOnly();
        }

        public static bool Has(DudeType type)
        {
            EnsureInitialized();
            return m_ByType.ContainsKey(type);
        }

        /// <summary>Look up a profile by its lowercase id (e.g. "fire").</summary>
        public static DudeTypeProfile GetById(string id)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(id))
                return null;

            id = id.Trim();
            for (int i = 0; i < m_All.Count; i++)
            {
                if (string.Equals(m_All[i].Id, id, StringComparison.OrdinalIgnoreCase))
                    return m_All[i];
            }

            return null;
        }

        /// <summary>Drop all profiles and re-register the code defaults (used by config reload).</summary>
        public static void ResetToDefaults()
        {
            m_ByType.Clear();
            m_All.Clear();
            m_Initialized = false;
            EnsureInitialized();
        }

        private static void RegisterDefaults()
        {
            // Fire is the baseline: Str 45 / Dex 40 / Int 15 / Hits 50, balanced gains.
            Register(new DudeTypeProfile(
                DudeType.Fire, "Fire", 0x21, 0x208, DudeVfx.Fire, 10,
                45, 40, 15, 50, 4, 7, 14,
                5, 6, 2, 1.0, 1, 3)
            {
                StarterSashType = typeof(EmberSash)
            });

            // Water: caster — double Int gain (2 → 4) and double mana per Int.
            Register(new DudeTypeProfile(
                DudeType.Water, "Water", 0x5A, 0x026, DudeVfx.Water, 10,
                45, 40, 15, 50, 4, 7, 14,
                5, 6, 4, 1.0, 1, 3)
            {
                ManaPerInt = 4,
                StarterSashType = typeof(TideSash)
            });

            // Earth: tank — +50% hits per level, +2 armor every level.
            Register(new DudeTypeProfile(
                DudeType.Earth, "Earth", 0x22C, 0x2F3, DudeVfx.Earth, 10,
                50, 28, 12, 55, 5, 8, 16,
                5, 6, 2, 1.5, 2, 1)
            {
                StarterSashType = typeof(StoneSash)
            });

            // Air: fast — Dex +3 extra (6 → 9 via GainDex 9).
            Register(new DudeTypeProfile(
                DudeType.Air, "Air", 0x47E, 0x29, DudeVfx.Air, 10,
                40, 50, 20, 48, 4, 7, 12,
                5, 9, 2, 1.0, 1, 3)
            {
                StarterSashType = typeof(GaleSash)
            });
        }
    }
}
