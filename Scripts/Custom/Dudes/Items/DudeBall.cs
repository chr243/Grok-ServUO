using System;
using Server.Custom.Dudes;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Stores one Dude. Ball is the authoritative persistence container.
    /// Double-click: empty = catch target; filled + no summon = summon; filled + summoned = recall.
    /// Classic crystal-ball ItemID for UOR clients.
    /// </summary>
    public class DudeBall : Item
    {
        private DudeData m_StoredDude;
        private DudeCreature m_SummonedDude;
        private DudeJobStation m_AssignedStation;

        private const int EmptyHue = 0;
        private const int FilledHue = 1153;

        [Constructable]
        public DudeBall()
            : base(0xE2E)
        {
            Name = "Dude Ball";
            Weight = 1.0;
            Light = LightType.Circle150;
            Hue = EmptyHue;
            LootType = LootType.Blessed;
        }

        public DudeBall(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasDude
        {
            get { return m_StoredDude != null; }
        }

        public DudeData StoredDude
        {
            get { return m_StoredDude; }
        }

        public DudeCreature SummonedDude
        {
            get
            {
                if (m_SummonedDude != null && m_SummonedDude.Deleted)
                    m_SummonedDude = null;

                return m_SummonedDude;
            }
        }

        public bool IsSummoned
        {
            get { return SummonedDude != null; }
        }

        /// <summary>True while this ball is assigned to a Job Station (possibly mid-job).</summary>
        public bool IsAssignedToJob
        {
            get
            {
                if (m_AssignedStation != null && m_AssignedStation.Deleted)
                    m_AssignedStation = null;

                return m_AssignedStation != null;
            }
        }

        public DudeJobStation AssignedStation
        {
            get
            {
                if (m_AssignedStation != null && m_AssignedStation.Deleted)
                    m_AssignedStation = null;

                return m_AssignedStation;
            }
            set { m_AssignedStation = value; }
        }

        public void StoreDude(DudeData data)
        {
            m_StoredDude = data;
            Hue = data != null ? FilledHue : EmptyHue;
            InvalidateProperties();
        }

        public void ClearDude()
        {
            RecallInternal(null, false);
            m_StoredDude = null;
            Hue = EmptyHue;
            InvalidateProperties();
        }

        public override bool OnDragLift(Mobile from)
        {
            if (IsAssignedToJob && m_AssignedStation != null && m_AssignedStation.JobActive)
            {
                from.SendMessage("You cannot remove the Dude Ball while a job is in progress.");
                return false;
            }

            return base.OnDragLift(from);
        }

        public void ClearSummonLink()
        {
            m_SummonedDude = null;
            InvalidateProperties();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (m_StoredDude == null)
            {
                list.Add("Empty Dude Ball");
                list.Add("Double-click and target a wild Dude to catch it.");
            }
            else
            {
                list.Add("Contains: {0}", m_StoredDude.DisplayName);
                list.Add("Type: {0}", m_StoredDude.Type);
                list.Add("Level: {0}", m_StoredDude.Level);
                list.Add("EXP: {0} / {1}", m_StoredDude.CurrentEXP, m_StoredDude.EXPToNext);
                list.Add("HP: {0} / {1}", m_StoredDude.Hits, m_StoredDude.HitsMax);

                if (IsAssignedToJob)
                    list.Add("Status: At Job Station");
                else if (m_StoredDude.IsFainted)
                    list.Add("Status: Fainted");
                else if (IsSummoned)
                    list.Add("Status: Summoned");
                else
                    list.Add("Status: Ready");

                list.Add("Double-click to summon or recall.");
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_StoredDude == null)
                LabelTo(from, "an empty Dude Ball");
            else
                LabelTo(from, "a Dude Ball containing {0} (Lv {1})", m_StoredDude.DisplayName, m_StoredDude.Level);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (IsAssignedToJob)
            {
                from.SendMessage("This Dude Ball is assigned to a Job Station.");
                return;
            }

            if (!IsChildOf(from.Backpack) && RootParent != from)
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            if (m_StoredDude == null)
            {
                from.SendMessage("Target a wild Dude to catch.");
                from.Target = new CatchTarget(this);
                return;
            }

            if (IsSummoned)
            {
                Recall(from);
            }
            else
            {
                Summon(from);
            }
        }

        public void Summon(Mobile from)
        {
            if (from == null || Deleted)
                return;

            if (m_StoredDude == null)
            {
                from.SendMessage("The Dude Ball is empty.");
                return;
            }

            if (IsSummoned)
            {
                from.SendMessage("{0} is already summoned.", m_StoredDude.DisplayName);
                return;
            }

            DudeDefinition def = DudeRegistry.Get(m_StoredDude.DefinitionId);
            int slots = def != null ? def.ControlSlots : 1;

            if (from.Followers + slots > from.FollowersMax)
            {
                from.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
                return;
            }

            Point3D loc = from.Location;
            Map map = from.Map;

            if (map == null || map == Map.Internal)
                return;

            DudeCreature dude = new DudeCreature(m_StoredDude.DefinitionId, false);
            dude.BoundBall = this;
            dude.ApplyData(m_StoredDude, m_StoredDude.IsFainted);

            // Clear fainted on successful summon (revive).
            m_StoredDude.IsFainted = false;
            m_StoredDude.Hits = dude.Hits;

            dude.MoveToWorld(loc, map);

            if (!dude.SetControlMaster(from))
            {
                from.SendMessage("You cannot control this Dude right now.");
                dude.Delete();
                return;
            }

            dude.ControlTarget = from;
            dude.ControlOrder = OrderType.Follow;
            m_SummonedDude = dude;
            InvalidateProperties();

            from.SendMessage(0x59, "{0} emerges from the Dude Ball!", m_StoredDude.DisplayName);
            from.PlaySound(0x20E);
            Effects.SendLocationParticles(
                EffectItem.Create(dude.Location, dude.Map, EffectItem.DefaultDuration),
                0x3728, 10, 10, 2023);
        }

        public void Recall(Mobile from)
        {
            RecallInternal(from, true);
        }

        private void RecallInternal(Mobile from, bool notify)
        {
            DudeCreature dude = SummonedDude;
            if (dude == null)
            {
                if (notify && from != null)
                    from.SendMessage("No Dude is currently summoned from this ball.");
                return;
            }

            dude.SyncToBall();

            if (m_StoredDude != null)
                m_StoredDude.IsFainted = false;

            Point3D loc = dude.Location;
            Map map = dude.Map;

            dude.BoundBall = null;
            m_SummonedDude = null;

            dude.SetControlMaster(null);
            dude.Delete();

            InvalidateProperties();

            if (notify && from != null)
            {
                from.SendMessage(0x59, "{0} returns to the Dude Ball.", m_StoredDude != null ? m_StoredDude.DisplayName : "Your Dude");
                from.PlaySound(0x1F1);
            }

            if (map != null && map != Map.Internal)
            {
                Effects.SendLocationParticles(
                    EffectItem.Create(loc, map, EffectItem.DefaultDuration),
                    0x3728, 10, 10, 2024);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version

            writer.Write(m_StoredDude != null);
            if (m_StoredDude != null)
                m_StoredDude.Serialize(writer);

            writer.Write(m_SummonedDude);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            DudeRegistry.EnsureInitialized();
            DudeAbilityRegistry.EnsureInitialized();

            bool hasDude = reader.ReadBool();
            if (hasDude)
            {
                m_StoredDude = new DudeData();
                m_StoredDude.Deserialize(reader);
            }
            else
            {
                m_StoredDude = null;
            }

            m_SummonedDude = reader.ReadMobile() as DudeCreature;

            Hue = m_StoredDude != null ? FilledHue : EmptyHue;
        }

        private class CatchTarget : Target
        {
            private readonly DudeBall m_Ball;

            public CatchTarget(DudeBall ball)
                : base(8, false, TargetFlags.None)
            {
                m_Ball = ball;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Ball == null || m_Ball.Deleted)
                    return;

                if (!m_Ball.IsChildOf(from.Backpack) && m_Ball.RootParent != from)
                {
                    from.SendLocalizedMessage(1042001);
                    return;
                }

                if (m_Ball.HasDude)
                {
                    from.SendMessage("That Dude Ball is already occupied.");
                    return;
                }

                DudeCreature wild = targeted as DudeCreature;
                if (wild == null || !wild.IsWild)
                {
                    from.SendMessage("That is not a wild Dude.");
                    return;
                }

                DudeCapture.TryCapture(from, wild, m_Ball);
            }
        }
    }
}
