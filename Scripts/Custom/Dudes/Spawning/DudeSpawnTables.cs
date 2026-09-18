using System;
using System.Collections.Generic;

namespace Server.Custom.Dudes.Spawning
{
    /// <summary>
    /// Weighted species tables for DudeSpawner. Wilds are stage-1 only.
    /// </summary>
    public static class DudeSpawnTables
    {
        private struct Entry
        {
            public readonly string Id;
            public readonly int Weight;
            public readonly DudeType Type;

            public Entry(string id, int weight, DudeType type)
            {
                Id = id;
                Weight = weight;
                Type = type;
            }
        }

        private static readonly Entry[] AllEntries = new Entry[]
        {
            new Entry("ember", 10, DudeType.Fire),
            new Entry("droplet", 10, DudeType.Water),
            new Entry("pebble", 10, DudeType.Earth),
            new Entry("breeze", 10, DudeType.Air)
        };

        public static string Pick(DudeSpawnPreset preset)
        {
            List<Entry> pool = new List<Entry>();

            for (int i = 0; i < AllEntries.Length; i++)
            {
                Entry e = AllEntries[i];
                if (Matches(preset, e))
                    pool.Add(e);
            }

            if (pool.Count == 0)
                return "ember";

            int total = 0;
            for (int i = 0; i < pool.Count; i++)
                total += pool[i].Weight;

            int roll = Utility.Random(total);
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= pool[i].Weight;
                if (roll < 0)
                    return pool[i].Id;
            }

            return pool[pool.Count - 1].Id;
        }

        private static bool Matches(DudeSpawnPreset preset, Entry e)
        {
            switch (preset)
            {
                case DudeSpawnPreset.Fire:
                    return e.Type == DudeType.Fire;
                case DudeSpawnPreset.Water:
                    return e.Type == DudeType.Water;
                case DudeSpawnPreset.Earth:
                    return e.Type == DudeType.Earth;
                case DudeSpawnPreset.Air:
                    return e.Type == DudeType.Air;
                case DudeSpawnPreset.Weak:
                case DudeSpawnPreset.Basic:
                case DudeSpawnPreset.Starter:
                case DudeSpawnPreset.All:
                case DudeSpawnPreset.Medium:
                case DudeSpawnPreset.Strong:
                default:
                    // Only S1s exist in the wild table — all presets draw from them.
                    return true;
            }
        }
    }
}
