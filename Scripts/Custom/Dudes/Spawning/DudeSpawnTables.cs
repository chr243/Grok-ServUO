using System;
using System.Collections.Generic;

namespace Server.Custom.Dudes.Spawning
{
    /// <summary>
    /// Weighted species tables for DudeSpawner. Weights are relative within the active preset.
    /// Evolution-only forms (emberon / infernox) are intentionally omitted.
    /// </summary>
    public static class DudeSpawnTables
    {
        private struct Entry
        {
            public readonly string Id;
            public readonly int Weight;
            public readonly DudeType Type;
            public readonly int Tier; // 0 weak, 1 basic, 2 medium, 3 strong

            public Entry(string id, int weight, DudeType type, int tier)
            {
                Id = id;
                Weight = weight;
                Type = type;
                Tier = tier;
            }
        }

        private static readonly Entry[] AllEntries = new Entry[]
        {
            // weak — common
            new Entry("sparkmite", 10, DudeType.Fire, 0),
            new Entry("embit", 10, DudeType.Fire, 0),
            new Entry("puddling", 10, DudeType.Water, 0),
            new Entry("pebblet", 10, DudeType.Earth, 0),
            new Entry("breezeling", 10, DudeType.Air, 0),
            // basic
            new Entry("emberling", 6, DudeType.Fire, 1),
            new Entry("tideling", 6, DudeType.Water, 1),
            new Entry("stonepaw", 6, DudeType.Earth, 1),
            new Entry("gustling", 6, DudeType.Air, 1),
            // medium
            new Entry("cinderfang", 3, DudeType.Fire, 2),
            new Entry("riptide", 3, DudeType.Water, 2),
            new Entry("boulderback", 3, DudeType.Earth, 2),
            // strong — rare
            new Entry("pyreclaw", 1, DudeType.Fire, 3)
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
                return "emberling";

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
                case DudeSpawnPreset.Weak:
                    return e.Tier == 0;
                case DudeSpawnPreset.Basic:
                    return e.Tier == 1;
                case DudeSpawnPreset.Medium:
                    return e.Tier == 2;
                case DudeSpawnPreset.Strong:
                    return e.Tier == 3;
                case DudeSpawnPreset.Fire:
                    return e.Type == DudeType.Fire;
                case DudeSpawnPreset.Water:
                    return e.Type == DudeType.Water;
                case DudeSpawnPreset.Earth:
                    return e.Type == DudeType.Earth;
                case DudeSpawnPreset.Air:
                    return e.Type == DudeType.Air;
                case DudeSpawnPreset.Starter:
                    return e.Tier <= 1;
                case DudeSpawnPreset.All:
                default:
                    return true;
            }
        }
    }
}
