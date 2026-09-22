using System;
using System.Collections.Generic;

namespace Server.Custom.Dudes.Spawning
{
    /// <summary>
    /// Weighted wild-spawn table for DudeSpawner. Derived from DudeTypeProfiles so a new
    /// type is added in exactly one place. Wilds are always stage 1.
    /// </summary>
    public static class DudeSpawnTables
    {
        /// <summary>Default type used when nothing matches.</summary>
        public const string FallbackId = "fire";

        public static string Pick(DudeSpawnPreset preset)
        {
            DudeTypeProfiles.EnsureInitialized();

            IList<DudeTypeProfile> all = DudeTypeProfiles.GetAll();
            List<DudeTypeProfile> pool = new List<DudeTypeProfile>();
            int total = 0;

            for (int i = 0; i < all.Count; i++)
            {
                DudeTypeProfile p = all[i];
                if (p == null || p.SpawnWeight <= 0)
                    continue;
                if (!Matches(preset, p.Type))
                    continue;

                pool.Add(p);
            }

            if (pool.Count == 0)
                return FallbackId;

            for (int i = 0; i < pool.Count; i++)
                total += pool[i].SpawnWeight;

            int roll = Utility.Random(total);
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= pool[i].SpawnWeight;
                if (roll < 0)
                    return pool[i].Id;
            }

            return pool[pool.Count - 1].Id;
        }

        private static bool Matches(DudeSpawnPreset preset, DudeType type)
        {
            // A preset whose name is a type id (Fire / Water / Earth / Air / …) selects just that
            // type, so a new type gets a preset for free once its profile is registered.
            // Any other preset (All, Weak, Starter, …) draws from the whole stage-1 pool, since
            // only stage-1 wilds exist.
            DudeTypeProfile typeProfile = DudeTypeProfiles.GetById(preset.ToString());
            if (typeProfile == null)
                return true;

            return typeProfile.Type == type;
        }
    }
}