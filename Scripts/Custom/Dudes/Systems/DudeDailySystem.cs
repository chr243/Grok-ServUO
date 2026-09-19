using System;
using System.Collections.Generic;
using System.IO;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    public enum DudeDailyKind
    {
        KillBoss = 0,
        ExpInRegion = 1,
        KillSlayer = 2
    }

    public sealed class DudeDailyQuest
    {
        public DudeDailyKind Kind;
        public int Required;
        public int Current;
        public bool Claimed;
        public string RegionName;   // ExpInRegion
        public SlayerName Slayer;   // KillSlayer

        public bool IsComplete
        {
            get { return Current >= Required && Required > 0; }
        }

        public string Title
        {
            get
            {
                switch (Kind)
                {
                    case DudeDailyKind.KillBoss:
                        return string.Format("Hunt {0} bosses", Required);
                    case DudeDailyKind.ExpInRegion:
                        return string.Format("Gain {0} EXP in {1}", Required, RegionName ?? "?");
                    case DudeDailyKind.KillSlayer:
                        return string.Format("Slay {0} {1}", Required, DudeDailySystem.GetSlayerPrettyName(Slayer));
                    default:
                        return "Unknown";
                }
            }
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version
            writer.Write((int)Kind);
            writer.Write(Required);
            writer.Write(Current);
            writer.Write(Claimed);
            writer.Write(RegionName);
            writer.Write((int)Slayer);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();
            Kind = (DudeDailyKind)reader.ReadInt();
            Required = reader.ReadInt();
            Current = reader.ReadInt();
            Claimed = reader.ReadBool();
            RegionName = reader.ReadString();
            Slayer = (SlayerName)reader.ReadInt();
        }
    }

    public sealed class DudeDailyRecord
    {
        public string Date; // yyyy-MM-dd UTC
        public DudeDailyQuest[] Quests = new DudeDailyQuest[3];
        public bool TonicGranted;

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version
            writer.Write(Date);
            writer.Write(TonicGranted);
            writer.Write(Quests != null ? Quests.Length : 0);
            if (Quests != null)
            {
                for (int i = 0; i < Quests.Length; i++)
                {
                    if (Quests[i] == null)
                        Quests[i] = new DudeDailyQuest();
                    Quests[i].Serialize(writer);
                }
            }
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();
            Date = reader.ReadString();
            TonicGranted = reader.ReadBool();
            int count = reader.ReadInt();
            Quests = new DudeDailyQuest[3];
            for (int i = 0; i < count; i++)
            {
                DudeDailyQuest q = new DudeDailyQuest();
                q.Deserialize(reader);
                if (i < 3)
                    Quests[i] = q;
            }
            for (int i = 0; i < 3; i++)
            {
                if (Quests[i] == null)
                    Quests[i] = new DudeDailyQuest();
            }
        }
    }

    /// <summary>
    /// Per-character (Serial) daily Dude training quests. Persists to Saves/DudeDailies.bin.
    /// </summary>
    public static class DudeDailySystem
    {
        private static readonly string FilePath = Path.Combine(Core.BaseDirectory, "Saves", "DudeDailies.bin");

        private static readonly Dictionary<Serial, DudeDailyRecord> Records =
            new Dictionary<Serial, DudeDailyRecord>();

        private static readonly Dictionary<Serial, DateTime> LastProgressMessage =
            new Dictionary<Serial, DateTime>();

        private static readonly string[] RegionPool = new string[]
        {
            "Destard", "Shame", "Hythloth", "Deceit", "Despise", "Wrong", "Covetous"
        };

        // Prefer list mapped to compiling SlayerName enum values in this repo.
        private static readonly SlayerName[] SlayerPool = new SlayerName[]
        {
            SlayerName.DragonSlaying, // Dragon
            SlayerName.Silver,        // Undead
            SlayerName.Exorcism,
            SlayerName.ElementalBan,
            SlayerName.Repond,
            SlayerName.ArachnidDoom,
            SlayerName.ReptilianDeath,
            SlayerName.OrcSlaying,
            SlayerName.Fey
        };

        public static void Initialize()
        {
            EventSink.WorldSave += OnWorldSave;
            EventSink.WorldLoad += OnWorldLoad;
            LoadRecords(); // WorldLoad may have already fired
        }

        public static string TodayUtc()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-dd");
        }

        public static DudeDailyRecord GetOrCreate(Mobile m)
        {
            if (m == null || !(m is PlayerMobile))
                return null;

            return GetOrCreate(m.Serial);
        }

        public static DudeDailyRecord GetOrCreate(Serial serial)
        {
            DudeDailyRecord rec;
            if (!Records.TryGetValue(serial, out rec) || rec == null)
            {
                rec = RollFresh();
                Records[serial] = rec;
                return rec;
            }

            if (!string.Equals(rec.Date, TodayUtc(), StringComparison.Ordinal))
            {
                rec = RollFresh();
                Records[serial] = rec;
            }

            return rec;
        }

        public static DudeDailyRecord EnsureForBoard(Mobile m)
        {
            return GetOrCreate(m);
        }

        private static DudeDailyRecord RollFresh()
        {
            DudeDailyRecord rec = new DudeDailyRecord();
            rec.Date = TodayUtc();
            rec.TonicGranted = false;
            rec.Quests = new DudeDailyQuest[3];

            // One of each kind every day (pool slots, not three of one kind).
            rec.Quests[0] = new DudeDailyQuest
            {
                Kind = DudeDailyKind.KillBoss,
                Required = Utility.RandomMinMax(1, 2),
                Current = 0,
                Claimed = false
            };

            rec.Quests[1] = new DudeDailyQuest
            {
                Kind = DudeDailyKind.ExpInRegion,
                Required = Utility.RandomMinMax(1000, 2000),
                Current = 0,
                Claimed = false,
                RegionName = RegionPool[Utility.Random(RegionPool.Length)]
            };

            rec.Quests[2] = new DudeDailyQuest
            {
                Kind = DudeDailyKind.KillSlayer,
                Required = Utility.RandomMinMax(20, 30),
                Current = 0,
                Claimed = false,
                Slayer = SlayerPool[Utility.Random(SlayerPool.Length)]
            };

            return rec;
        }

        public static string GetSlayerPrettyName(SlayerName name)
        {
            switch (name)
            {
                case SlayerName.DragonSlaying: return "dragons";
                case SlayerName.Silver: return "undead";
                case SlayerName.Exorcism: return "demons";
                case SlayerName.ElementalBan: return "elementals";
                case SlayerName.Repond: return "humanoids";
                case SlayerName.ArachnidDoom: return "arachnids";
                case SlayerName.ReptilianDeath: return "reptiles";
                case SlayerName.OrcSlaying: return "orcs";
                case SlayerName.Fey: return "fey";
                default: return name.ToString().ToLowerInvariant();
            }
        }

        public static void OnKill(Mobile master, Mobile victim)
        {
            if (master == null || !(master is PlayerMobile) || victim == null || victim.Deleted)
                return;

            DudeDailyRecord rec = GetOrCreate(master);
            if (rec == null || rec.Quests == null)
                return;

            if (victim is DudeBoss)
            {
                Increment(master, rec, FindQuest(rec, DudeDailyKind.KillBoss), 1);
                return;
            }

            // KillSlayer: skip companions, workers, players, bosses (bosses are quest 1 only).
            if (victim is DudeCreature || victim is DudeJobWorker || victim is PlayerMobile || victim is DudeBoss)
                return;

            DudeDailyQuest slayerQuest = FindQuest(rec, DudeDailyKind.KillSlayer);
            if (slayerQuest == null || slayerQuest.Claimed || slayerQuest.IsComplete)
                return;

            SlayerEntry entry = null;
            try
            {
                entry = Server.Items.SlayerGroup.GetEntryByName(slayerQuest.Slayer);
            }
            catch
            {
                entry = null;
            }

            if (entry != null && entry.Slays(victim))
                Increment(master, rec, slayerQuest, 1);
        }

        public static void OnExp(Mobile owner, int amount, Point3D loc, Map map)
        {
            if (owner == null || !(owner is PlayerMobile) || amount <= 0)
                return;

            DudeDailyRecord rec = GetOrCreate(owner);
            if (rec == null || rec.Quests == null)
                return;

            DudeDailyQuest quest = FindQuest(rec, DudeDailyKind.ExpInRegion);
            if (quest == null || quest.Claimed || quest.IsComplete)
                return;

            if (string.IsNullOrEmpty(quest.RegionName))
                return;

            if (!IsInNamedRegion(loc, map, quest.RegionName))
                return;

            int add = amount;
            if (quest.Current + add > quest.Required)
                add = quest.Required - quest.Current;

            if (add <= 0)
                return;

            Increment(owner, rec, quest, add);
        }

        private static DudeDailyQuest FindQuest(DudeDailyRecord rec, DudeDailyKind kind)
        {
            if (rec == null || rec.Quests == null)
                return null;

            for (int i = 0; i < rec.Quests.Length; i++)
            {
                if (rec.Quests[i] != null && rec.Quests[i].Kind == kind)
                    return rec.Quests[i];
            }

            return null;
        }

        private static void Increment(Mobile m, DudeDailyRecord rec, DudeDailyQuest quest, int amount)
        {
            if (m == null || quest == null || amount <= 0 || quest.Claimed)
                return;

            if (quest.IsComplete)
                return;

            quest.Current += amount;
            if (quest.Current > quest.Required)
                quest.Current = quest.Required;

            TrySendProgress(m, quest);
        }

        private static void TrySendProgress(Mobile m, DudeDailyQuest quest)
        {
            if (m == null || quest == null)
                return;

            DateTime now = DateTime.UtcNow;
            DateTime last;
            if (LastProgressMessage.TryGetValue(m.Serial, out last))
            {
                if ((now - last).TotalSeconds < 2.0)
                    return;
            }

            LastProgressMessage[m.Serial] = now;
            m.SendMessage("Daily: {0} {1}/{2}", quest.Title, quest.Current, quest.Required);
        }

        /// <summary>
        /// Case-insensitive contains match walking Region.Find parent chain (and IsPartOf fallback).
        /// </summary>
        public static bool IsInNamedRegion(Point3D loc, Map map, string regionName)
        {
            if (map == null || map == Map.Internal || string.IsNullOrEmpty(regionName))
                return false;

            Region r = Region.Find(loc, map);
            Region walk = r;
            while (walk != null)
            {
                if (!string.IsNullOrEmpty(walk.Name)
                    && walk.Name.IndexOf(regionName, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                walk = walk.Parent;
            }

            // Fallback: exact IsPartOf when name matches region registration.
            if (r != null && r.IsPartOf(regionName))
                return true;

            return false;
        }

        public static bool TryClaim(Mobile from, int questIndex)
        {
            if (from == null || !(from is PlayerMobile))
                return false;

            DudeDailyRecord rec = GetOrCreate(from);
            if (rec == null || rec.Quests == null || questIndex < 0 || questIndex >= rec.Quests.Length)
                return false;

            DudeDailyQuest quest = rec.Quests[questIndex];
            if (quest == null || !quest.IsComplete || quest.Claimed)
                return false;

            GiveReward(from, new Gold(200));
            GiveReward(from, new DudeDust(2));
            quest.Claimed = true;
            from.SendMessage(0x44, "Daily claimed: {0}", quest.Title);

            if (AllClaimed(rec) && !rec.TonicGranted)
            {
                GiveReward(from, new TrainersTonic(1));
                rec.TonicGranted = true;
                from.SendMessage(0x44, "You finished today's training. A Trainer's Tonic is yours.");
            }

            return true;
        }

        private static bool AllClaimed(DudeDailyRecord rec)
        {
            if (rec == null || rec.Quests == null)
                return false;

            for (int i = 0; i < rec.Quests.Length; i++)
            {
                if (rec.Quests[i] == null || !rec.Quests[i].Claimed)
                    return false;
            }

            return true;
        }

        private static void GiveReward(Mobile from, Item item)
        {
            if (from == null || item == null)
                return;

            if (from.Backpack != null && from.Backpack.TryDropItem(from, item, false))
                return;

            BankBox bank = from.BankBox;
            if (bank != null && bank.TryDropItem(from, item, false))
                return;

            // Last resort: ground at feet.
            item.MoveToWorld(from.Location, from.Map);
        }

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            Persistence.Serialize(FilePath, writer =>
            {
                writer.Write((int)0); // file version
                writer.Write(Records.Count);
                foreach (KeyValuePair<Serial, DudeDailyRecord> kv in Records)
                {
                    writer.Write(kv.Key);
                    if (kv.Value == null)
                    {
                        writer.Write(false);
                    }
                    else
                    {
                        writer.Write(true);
                        kv.Value.Serialize(writer);
                    }
                }
            });
        }

        private static void OnWorldLoad()
        {
            LoadRecords();
        }

        private static void LoadRecords()
        {
            Records.Clear();

            if (!File.Exists(FilePath))
                return; // first boot: leave empty, do not RollFresh

            Persistence.Deserialize(FilePath, reader =>
            {
                int version = reader.ReadInt();
                int count = reader.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    Serial serial = reader.ReadInt();
                    bool has = reader.ReadBool();
                    if (!has)
                        continue;

                    DudeDailyRecord rec = new DudeDailyRecord();
                    rec.Deserialize(reader);
                    Records[serial] = rec;
                }
            });
        }
    }
}
