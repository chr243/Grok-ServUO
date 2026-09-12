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

        public DudeJobWorker()
            : base(AIType.AI_Melee, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Name = "Dude Worker";
            Body = 14;
            Hue = 2413;
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
            {
                Body = def.Body;
                Hue = def.Hue;
                BaseSoundID = def.BaseSoundID;
            }

            Name = data.DisplayName + " (Working)";
            SetStr(data.Str);
            SetDex(data.Dex);
            SetInt(data.Int);
            SetHits(Math.Max(10, data.HitsMax));
            Hits = HitsMax;
        }

        public void SetGoal(Point3D goal)
        {
            m_Goal = goal;
            m_HasGoal = true;
            m_Path = null;
            m_LastProgress = DateTime.UtcNow;
            m_LastLocation = Location;
        }

        public void ClearGoal()
        {
            m_HasGoal = false;
            m_Path = null;
        }

        /// <summary>
        /// Pathfind one step toward goal. Returns true if within range.
        /// </summary>
        public bool FollowGoal(int range)
        {
            if (!m_HasGoal || Deleted || Map == null || Map == Map.Internal)
                return true;

            if (m_Path == null)
                m_Path = new PathFollower(this, m_Goal);

            bool arrived = m_Path.Follow(true, range);

            if (Location != m_LastLocation)
            {
                m_LastLocation = Location;
                m_LastProgress = DateTime.UtcNow;
            }

            return arrived;
        }

        public bool IsStuck(TimeSpan timeout)
        {
            if (!m_HasGoal)
                return false;

            return (DateTime.UtcNow - m_LastProgress) >= timeout;
        }

        /// <summary>True if MovementPath can currently reach the goal (false into many houses).</summary>
        public bool CanPathToGoal()
        {
            if (!m_HasGoal || Deleted || Map == null || Map == Map.Internal)
                return true;

            MovementPath path = new MovementPath(this, m_Goal);
            return path.Success;
        }

        public void TeleportToGoal()
        {
            if (!m_HasGoal || Map == null || Map == Map.Internal)
                return;

            // Force move — house tiles often fail CanFit/average-Z checks.
            TeleportTo(m_Goal, Map);
        }

        public void TeleportTo(Point3D loc, Map map)
        {
            if (map == null || map == Map.Internal)
                return;

            MoveToWorld(loc, map);
            m_LastLocation = Location;
            m_LastProgress = DateTime.UtcNow;
            m_Path = null;
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
