using System;
using Server.Custom.Dudes;
using Server.Custom.Dudes.Jobs;
using Server.Items;

namespace Server.Mobiles
{
    /// <summary>
    /// Temporary world worker spawned from a DudeBall for Job Station tasks.
    /// Ball remains in the station; this creature is a linked projection.
    /// </summary>
    public class DudeJobWorker : BaseCreature
    {
        private DudeJobStation m_Station;
        private DudeBall m_BoundBall;
        private PathFollower m_Path;
        private Point3D m_Goal;
        private bool m_HasGoal;
        private DateTime m_LastProgress;
        private Point3D m_LastLocation;
        private bool m_PathAbandoned;

        public DudeJobWorker()
            : base(AIType.AI_Melee, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Name = "Dude Worker";
            Body = 0x190;
            Female = false;
            Hue = 0;
            Blessed = true;
            CantWalk = false;
            Tamable = false;
            Controlled = false;
            FightMode = FightMode.None;

            SetStr(30);
            SetDex(30);
            SetInt(10);
            SetHits(50);
            SetDamage(1, 2);
            VirtualArmor = 10;
        }

        public DudeJobWorker(Serial serial)
            : base(serial)
        {
        }

        public DudeJobStation Station
        {
            get { return m_Station; }
            set { m_Station = value; }
        }

        public DudeBall BoundBall
        {
            get { return m_BoundBall; }
            set { m_BoundBall = value; }
        }

        public Point3D Goal
        {
            get { return m_Goal; }
        }

        public bool HasGoal
        {
            get { return m_HasGoal; }
        }

        public DateTime LastProgress
        {
            get { return m_LastProgress; }
        }

        public void ApplyFromDude(DudeData data)
        {
            if (data == null)
                return;

            DudeDefinition def = DudeRegistry.Get(data.DefinitionId);
            if (def != null)
                BaseSoundID = def.BaseSoundID;

            Body = 0x190;
            Female = false;
            Hue = 0;
            EnsureTypeShorts(def);
            EnsureWorkTool(data);

            Name = DudeCreature.ResolveDudeName(data, def) + " (Working)";
            SetStr(data.Str);
            SetDex(data.Dex);
            SetInt(data.Int);
            SetHits(Math.Max(10, data.HitsMax));
            Hits = HitsMax;
        }

        private void EnsureTypeShorts(DudeDefinition def)
        {
            int hue = def != null ? def.Hue : 0;

            Item existing = FindItemOnLayer(Layer.Pants);
            DudeTypeShorts shorts = existing as DudeTypeShorts;

            if (shorts != null)
            {
                if (shorts.Hue != hue)
                    shorts.Hue = hue;
                shorts.Name = "Type shorts";
                shorts.LootType = LootType.Blessed;
                shorts.Movable = false;
                return;
            }

            if (existing != null)
                existing.Delete();

            Item outer = FindItemOnLayer(Layer.OuterLegs);
            if (outer is Kilt)
                outer.Delete();

            AddItem(new DudeTypeShorts(hue));
        }

        private void EnsureWorkTool(DudeData data)
        {
            // Workers are reused across job loops; keep a correct tool instead of deleting and
            // re-equipping it (each swap sends remove/equip packets to nearby clients).
            if (data != null && HasWorkTool(data.Type))
                return;

            ClearWorkTool();

            if (data == null)
                return;

            Item tool = null;

            switch (data.Type)
            {
                case DudeType.Earth:
                    tool = new DudeWorkPickaxe();
                    break;
                case DudeType.Air:
                    tool = new DudeWorkHatchet();
                    break;
                default:
                    return;
            }

            tool.LootType = LootType.Blessed;
            tool.Movable = false;
            tool.Layer = Layer.OneHanded;
            AddItem(tool);
        }

        private void ClearWorkTool()
        {
            Item one = FindItemOnLayer(Layer.OneHanded);
            if (IsWorkTool(one))
                one.Delete();

            Item two = FindItemOnLayer(Layer.TwoHanded);
            if (IsWorkTool(two))
                two.Delete();
        }

        private static bool IsWorkTool(Item item)
        {
            return item is DudeWorkPickaxe || item is DudeWorkHatchet;
        }

        /// <summary>True if exactly the tool EnsureWorkTool would equip for this type is held.</summary>
        private bool HasWorkTool(DudeType type)
        {
            if (IsWorkTool(FindItemOnLayer(Layer.TwoHanded)))
                return false;

            Item held = FindItemOnLayer(Layer.OneHanded);

            switch (type)
            {
                case DudeType.Earth:
                    return held is DudeWorkPickaxe;
                case DudeType.Air:
                    return held is DudeWorkHatchet;
                default:
                    return false;
            }
        }

        public bool PathAbandoned
        {
            get { return m_PathAbandoned; }
        }

        public void SetGoal(Point3D goal)
        {
            m_Goal = goal;
            m_HasGoal = true;
            m_Path = null;
            m_PathAbandoned = false;
            m_LastProgress = DateTime.UtcNow;
            m_LastLocation = Location;
        }

        public void ClearGoal()
        {
            m_HasGoal = false;
            m_Path = null;
            m_PathAbandoned = false;
        }

        public void AbandonPath()
        {
            m_PathAbandoned = true;
            m_Path = null;
        }

        /// <summary>
        /// Pathfind one step toward goal. Returns true if within range.
        /// Once abandoned (stuck), does not pathfind again until SetGoal.
        /// </summary>
        public bool FollowGoal(int range)
        {
            if (!m_HasGoal || Deleted || Map == null || Map == Map.Internal)
                return true;

            if (InRange(m_Goal, range))
                return true;

            // Do not rebuild MovementPath while blocked — station will teleport.
            if (m_PathAbandoned)
                return false;

            if (m_Path == null)
                m_Path = new PathFollower(this, m_Goal);

            bool arrived = m_Path.Follow(true, range);

            if (Location != m_LastLocation)
            {
                m_LastLocation = Location;
                m_LastProgress = DateTime.UtcNow;
            }
            else if (!arrived && IsStuck(DudeJobConfig.StuckTimeout))
            {
                AbandonPath();
            }

            return arrived;
        }

        public bool IsStuck(TimeSpan timeout)
        {
            if (!m_HasGoal)
                return false;

            return (DateTime.UtcNow - m_LastProgress) >= timeout;
        }

        public void TeleportToGoal()
        {
            if (!m_HasGoal || Map == null || Map == Map.Internal)
                return;

            TeleportNear(m_Goal, Map, 2);
        }

        public void TeleportTo(Point3D loc, Map map)
        {
            TeleportNear(loc, map, 0);
        }

        /// <summary>Teleport onto loc, or a nearby walkable tile if CanFit fails.</summary>
        public void TeleportNear(Point3D loc, Map map, int searchRange)
        {
            if (map == null || map == Map.Internal)
                return;

            Point3D dest = loc;
            if (searchRange > 0 && !map.CanFit(loc, 16, false, false))
            {
                bool found = false;
                for (int r = 1; r <= searchRange && !found; r++)
                {
                    for (int dx = -r; dx <= r && !found; dx++)
                    {
                        for (int dy = -r; dy <= r && !found; dy++)
                        {
                            Point3D p = new Point3D(loc.X + dx, loc.Y + dy, loc.Z);
                            if (map.CanFit(p, 16, false, false))
                            {
                                dest = p;
                                found = true;
                            }
                        }
                    }
                }
            }

            MoveToWorld(dest, map);
            m_LastLocation = Location;
            m_LastProgress = DateTime.UtcNow;
            m_Path = null;
            // Stay abandoned until SetGoal — prevents path spam after teleport to same dest.
            m_PathAbandoned = true;
        }

        public void PlayWorkAnimation(DudeJob job, DudeData data)
        {
            if (job == null || Deleted)
                return;

            int anim = job.GetWorkAnimation(data);
            Animate(anim, 5, 1, true, false, 0);

            int sound = job.GetWorkSound(data);
            if (sound > 0)
                PlaySound(sound);
        }

        public override bool DeleteCorpseOnDeath
        {
            get { return true; }
        }

        public override bool OnBeforeDeath()
        {
            // Blessed should prevent this; still notify station for safety.
            if (m_Station != null && !m_Station.Deleted)
                m_Station.OnWorkerLost(this);

            return base.OnBeforeDeath();
        }

        public override void OnDelete()
        {
            if (m_Station != null && !m_Station.Deleted && m_Station.Worker == this)
                m_Station.ClearWorkerLink();

            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            writer.Write(m_Station);
            writer.Write(m_BoundBall);
            writer.Write(m_HasGoal);
            writer.Write(m_Goal);
            writer.Write(m_LastProgress);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_Station = reader.ReadItem() as DudeJobStation;
            m_BoundBall = reader.ReadItem() as DudeBall;
            m_HasGoal = reader.ReadBool();
            m_Goal = reader.ReadPoint3D();
            m_LastProgress = reader.ReadDateTime();
            m_LastLocation = Location;
            Blessed = true;
            FightMode = FightMode.None;
        }
    }
}
