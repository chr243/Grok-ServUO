using System;
using System.Collections.Generic;
using Server.Custom.Dudes;
using Server.Custom.Dudes.Spawning;
using Server.Mobiles;

namespace Server.Items
{
    /// <summary>
    /// Respawns wild Dudes from a weighted pool. Prefer the typed subclasses
    /// (FireDudeSpawner, WaterDudeSpawner, EarthDudeSpawner, AirDudeSpawner).
    /// </summary>
    public class DudeSpawner : Item
    {
        private DudeSpawnPreset m_Preset = DudeSpawnPreset.All;
        private int m_MaxCount = 4;
        private int m_HomeRange = 8;
        private int m_SpawnRange = 4;
        private TimeSpan m_MinDelay = TimeSpan.FromMinutes(2.0);
        private TimeSpan m_MaxDelay = TimeSpan.FromMinutes(5.0);
        private bool m_Running = true;
        private DateTime m_NextSpawn;
        private bool m_WasFull; // transient: last tick saw the spawner full (see OnTick)
        private Timer m_Timer;
        private readonly List<DudeCreature> m_Spawned = new List<DudeCreature>();

        [Constructable]
        public DudeSpawner()
            : this(DudeSpawnPreset.All)
        {
        }

        public DudeSpawner(DudeSpawnPreset preset)
            : base(0x1F13)
        {
            Name = "Dude Spawner";
            Movable = false;
            Visible = false;
            m_Preset = preset;
            ApplyPresetName();
            m_NextSpawn = DateTime.UtcNow;
            StartTimer();
        }

        public DudeSpawner(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DudeSpawnPreset Preset
        {
            get { return m_Preset; }
            set
            {
                m_Preset = value;
                ApplyPresetName();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MaxCount
        {
            get { return m_MaxCount; }
            set
            {
                int max = Math.Max(0, value);

                // Raising the cap is a GM change, not a kill/capture: fill the new slots on the
                // usual schedule instead of starting a fresh respawn delay.
                if (max > m_MaxCount)
                    m_WasFull = false;

                m_MaxCount = max;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int HomeRange
        {
            get { return m_HomeRange; }
            set { m_HomeRange = Math.Max(0, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpawnRange
        {
            get { return m_SpawnRange; }
            set { m_SpawnRange = Math.Max(0, value); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan MinDelay
        {
            get { return m_MinDelay; }
            set { m_MinDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public TimeSpan MaxDelay
        {
            get { return m_MaxDelay; }
            set { m_MaxDelay = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Running
        {
            get { return m_Running; }
            set
            {
                // Re-enabling is a GM change too: slots emptied while stopped refill on the usual
                // schedule rather than waiting a fresh respawn delay.
                if (value && !m_Running)
                    m_WasFull = false;

                m_Running = value;
                if (m_Running)
                    StartTimer();
                else
                    StopTimer();
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SpawnCount
        {
            get
            {
                Defrag();
                return m_Spawned.Count;
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Preset: {0}", m_Preset);
            list.Add("Count: {0} / {1}", SpawnCount, m_MaxCount);
            list.Add(m_Running ? "Running" : "Stopped");
        }

        public override void OnMapChange()
        {
            base.OnMapChange();
            if (m_Running)
                StartTimer();
        }

        public override void OnLocationChange(Point3D oldLocation)
        {
            base.OnLocationChange(oldLocation);
            if (m_Running)
                StartTimer();
        }

        public override void OnDelete()
        {
            StopTimer();
            Defrag();

            for (int i = m_Spawned.Count - 1; i >= 0; i--)
            {
                DudeCreature c = m_Spawned[i];
                if (c != null && !c.Deleted)
                    c.Delete();
            }

            m_Spawned.Clear();
            base.OnDelete();
        }

        public void DoSpawn()
        {
            if (Deleted || Map == null || Map == Map.Internal)
                return;

            Defrag();

            while (m_Spawned.Count < m_MaxCount)
            {
                if (!TrySpawnOne())
                    break;
            }

            ScheduleNext();
        }

        private bool TrySpawnOne()
        {
            Map map = Map;
            if (map == null || map == Map.Internal)
                return false;

            string id = DudeSpawnTables.Pick(m_Preset);
            DudeDefinition def = DudeRegistry.Get(id);
            if (def == null)
                return false;

            Point3D loc = GetSpawnLocation();
            DudeCreature dude = new DudeCreature(id, true);
            dude.Home = Location;
            dude.RangeHome = m_HomeRange;
            dude.MoveToWorld(loc, map);
            m_Spawned.Add(dude);
            return true;
        }

        private Point3D GetSpawnLocation()
        {
            Map map = Map;
            Point3D home = Location;

            for (int i = 0; i < 20; i++)
            {
                int x = home.X + Utility.RandomMinMax(-m_SpawnRange, m_SpawnRange);
                int y = home.Y + Utility.RandomMinMax(-m_SpawnRange, m_SpawnRange);
                int z = map.GetAverageZ(x, y);
                Point3D p = new Point3D(x, y, z);

                if (map.CanSpawnMobile(p))
                    return p;

                z = home.Z;
                p = new Point3D(x, y, z);
                if (map.CanSpawnMobile(p))
                    return p;
            }

            return home;
        }

        private void Defrag()
        {
            for (int i = m_Spawned.Count - 1; i >= 0; i--)
            {
                DudeCreature c = m_Spawned[i];
                if (c == null || c.Deleted || !c.IsWild)
                    m_Spawned.RemoveAt(i);
            }
        }

        private void ScheduleNext()
        {
            double min = m_MinDelay.TotalSeconds;
            double max = m_MaxDelay.TotalSeconds;
            if (max < min)
                max = min;

            double sec = min + (Utility.RandomDouble() * (max - min));
            m_NextSpawn = DateTime.UtcNow + TimeSpan.FromSeconds(sec);
        }

        private void StartTimer()
        {
            StopTimer();
            m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0), OnTick);
        }

        private void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        private void OnTick()
        {
            if (Deleted || !m_Running)
                return;

            if (Map == null || Map == Map.Internal)
                return;

            Defrag();

            if (m_Spawned.Count >= m_MaxCount)
            {
                m_WasFull = true;
                return;
            }

            // A slot just opened (kill or capture) after the spawner was full. m_NextSpawn was set
            // by the last fill and goes stale while full, so start a fresh MinDelay..MaxDelay wait
            // instead of respawning on the next tick.
            if (m_WasFull)
            {
                m_WasFull = false;
                ScheduleNext();
                return;
            }

            if (DateTime.UtcNow >= m_NextSpawn)
                DoSpawn();
        }

        private void ApplyPresetName()
        {
            // A type preset names itself "{Type} Dude Spawner" automatically; anything else falls
            // back to the plain preset label.
            DudeTypeProfile typeProfile = DudeTypeProfiles.GetById(m_Preset.ToString());
            if (typeProfile != null)
                Name = typeProfile.DisplayName + " Spawner";
            else
                Name = "Dude Spawner (" + m_Preset + ")";
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            writer.Write((int)m_Preset);
            writer.Write(m_MaxCount);
            writer.Write(m_HomeRange);
            writer.Write(m_SpawnRange);
            writer.Write(m_MinDelay);
            writer.Write(m_MaxDelay);
            writer.Write(m_Running);
            writer.Write(m_NextSpawn);

            Defrag();
            writer.Write(m_Spawned.Count);
            for (int i = 0; i < m_Spawned.Count; i++)
                writer.Write(m_Spawned[i]);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_Preset = (DudeSpawnPreset)reader.ReadInt();
            m_MaxCount = reader.ReadInt();
            m_HomeRange = reader.ReadInt();
            m_SpawnRange = reader.ReadInt();
            m_MinDelay = reader.ReadTimeSpan();
            m_MaxDelay = reader.ReadTimeSpan();
            m_Running = reader.ReadBool();
            m_NextSpawn = reader.ReadDateTime();

            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                DudeCreature c = reader.ReadMobile() as DudeCreature;
                if (c != null && !c.Deleted)
                    m_Spawned.Add(c);
            }

            ApplyPresetName();

            if (m_Running)
                StartTimer();
        }
    }

    public class FireDudeSpawner : DudeSpawner
    {
        [Constructable]
        public FireDudeSpawner()
            : base(DudeSpawnPreset.Fire)
        {
        }

        public FireDudeSpawner(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    public class WaterDudeSpawner : DudeSpawner
    {
        [Constructable]
        public WaterDudeSpawner()
            : base(DudeSpawnPreset.Water)
        {
        }

        public WaterDudeSpawner(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    public class EarthDudeSpawner : DudeSpawner
    {
        [Constructable]
        public EarthDudeSpawner()
            : base(DudeSpawnPreset.Earth)
        {
        }

        public EarthDudeSpawner(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    public class AirDudeSpawner : DudeSpawner
    {
        [Constructable]
        public AirDudeSpawner()
            : base(DudeSpawnPreset.Air)
        {
        }

        public AirDudeSpawner(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}
