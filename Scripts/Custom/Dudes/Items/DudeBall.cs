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
    /// BolaBall ItemID with distinct empty/full hues.
    /// </summary>
    public class DudeBall : Item
    {
        private DudeData m_StoredDude;
        private DudeCreature m_SummonedDude;
        private DudeJobStation m_AssignedStation;
        private DateTime m_NextUseUtc;
        private bool m_Recalling;

        public bool IsRecalling { get { return m_Recalling; } }

        private const int BallItemId = 0xE73; // BolaBall graphic
        private const int EmptyHue = 0x59; // bright green — easy to spot empty in pack
        private const int FireHue = 0x21; // red
        private const int WaterHue = 0x5A; // blue
        private const int AirHue = 0x47E; // white
        private const int EarthHue = 0x22C; // light brown

        private static readonly TimeSpan UseCooldown = TimeSpan.FromSeconds(5.0);

        [Constructable]
        public DudeBall()
            : base(BallItemId)
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
            get
            {
                DudeCreature dude = SummonedDude;
                return dude != null && dude.Map != null && dude.Map != Map.Internal;
            }
        }

        /// <summary>True if a world Dude instance exists (out or parked on Internal for serial reuse).</summary>
        public bool HasParkedDude
        {
            get
            {
                DudeCreature dude = SummonedDude;
                return dude != null;
            }
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
            RefreshHue();
            InvalidateProperties();
        }

        public void ClearDude()
        {
            RecallInternal(null, false);
            m_StoredDude = null;
            RefreshHue();
            InvalidateProperties();
        }

        /// <summary>Empty = green; filled = Fire red / Water blue / Air white / Earth light brown.</summary>
        public void RefreshHue()
        {
            Hue = GetHueForStoredDude(m_StoredDude);
        }

        public static int GetHueForStoredDude(DudeData data)
        {
            if (data == null)
                return EmptyHue;

            switch (data.Type)
            {
                case DudeType.Fire:
                    return FireHue;
                case DudeType.Water:
                    return WaterHue;
                case DudeType.Air:
                    return AirHue;
                case DudeType.Earth:
                    return EarthHue;
                default:
                    return FireHue;
            }
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

        /// <summary>Drop the parked/world Dude instance (frees serial). Used on ball delete / faint cleanup.</summary>
        public void DestroyParkedDude()
        {
            DudeCreature dude = m_SummonedDude;
            m_SummonedDude = null;

            if (dude != null && !dude.Deleted)
            {
                dude.BoundBall = null;
                if (dude.ControlMaster != null)
                    dude.SetControlMaster(null);
                dude.Delete();
            }

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
                {
                    list.Add("Status: Fainted");
                    list.Add("Use a Dude Revival Potion to revive.");
                }
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

            if (DateTime.UtcNow < m_NextUseUtc)
            {
                double left = (m_NextUseUtc - DateTime.UtcNow).TotalSeconds;
                if (left < 0.1)
                    left = 0.1;
                from.SendMessage("You must wait {0:0.0}s before using this Dude Ball again.", left);
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

        private void MarkUsed()
        {
            m_NextUseUtc = DateTime.UtcNow + UseCooldown;
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

            if (m_StoredDude.IsFainted)
            {
                from.SendMessage("{0} is fainted. Use a Dude Revival Potion first.", m_StoredDude.DisplayName);
                return;
            }

            if (m_Recalling)
            {
                from.SendMessage("Your Dude is still returning to the ball.");
                return;
            }

            DudeDefinition def = DudeRegistry.Get(m_StoredDude.DefinitionId);
            int slots = def != null ? def.ControlSlots : 4;

            if (from.Followers + slots > from.FollowersMax)
            {
                from.SendLocalizedMessage(1049607); // You have too many followers to control that creature.
                return;
            }

            Point3D loc = from.Location;
            Map map = from.Map;

            if (map == null || map == Map.Internal)
                return;

            // Reuse parked instance so the client status bar keeps the same serial.
            DudeCreature dude = m_SummonedDude;
            bool created = false;

            if (dude == null || dude.Deleted)
            {
                dude = new DudeCreature(m_StoredDude.DefinitionId, false);
                created = true;
            }

            dude.BoundBall = this;
            dude.ApplyData(m_StoredDude, false);
            m_StoredDude.Hits = dude.Hits;

            dude.MoveToWorld(loc, map);

            if (!dude.SetControlMaster(from))
            {
                from.SendMessage("You cannot control this Dude right now.");
                if (created)
                {
                    dude.BoundBall = null;
                    dude.Delete();
                    m_SummonedDude = null;
                }
                else
                {
                    ParkDude(dude);
                }
                return;
            }

            // Match owner standing for karma-based systems; staff pets use ForceNotoriety above.
            dude.Karma = from.Karma;
            dude.Fame = from.Fame;

            dude.ControlTarget = from;
            dude.ControlOrder = OrderType.Guard;
            m_SummonedDude = dude;
            InvalidateProperties();

            from.SendMessage(0x59, "{0} emerges from the Dude Ball!", m_StoredDude.DisplayName);
            DudeSummonEffects.Play(m_StoredDude.Type, dude.Location, dude.Map, m_StoredDude.DefinitionId);
            MarkUsed();
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

            if (m_Recalling)
            {
                if (notify && from != null)
                    from.SendMessage("Your Dude is already returning to the ball.");
                return;
            }

            dude.SyncToBall();

            if (m_StoredDude != null)
                m_StoredDude.IsFainted = false;

            Point3D loc = dude.Location;
            Map map = dude.Map;
            DudeType fxType = m_StoredDude != null ? m_StoredDude.Type : DudeType.Fire;
            string defId = m_StoredDude != null ? m_StoredDude.DefinitionId : null;

            // Keep ControlMaster through the FX so the Dude never goes "wild" / guard-candidate.
            // Pacify + bless during the animation; park only after it finishes.
            m_Recalling = true;
            dude.BeginDespawnSequence();

            TimeSpan delay = TimeSpan.Zero;
            if (map != null && map != Map.Internal)
            {
                DudeSummonEffects.PlayDespawn(fxType, loc, map, defId);
                delay = DudeSummonEffects.GetDespawnDuration(fxType, defId);
            }

            MarkUsed();

            Mobile notifyMobile = from;
            bool doNotify = notify;
            DudeCreature parkTarget = dude;

            Timer.DelayCall(delay, () =>
            {
                FinishRecallPark(parkTarget, notifyMobile, doNotify);
            });
        }

        private void FinishRecallPark(DudeCreature dude, Mobile from, bool notify)
        {
            m_Recalling = false;

            if (dude != null && !dude.Deleted)
            {
                dude.EndDespawnSequence();
                dude.SetControlMaster(null);
                ParkDude(dude);
                m_SummonedDude = dude;
            }

            InvalidateProperties();

            if (notify && from != null && !from.Deleted)
            {
                from.SendMessage(0x59, "{0} returns to the Dude Ball.", m_StoredDude != null ? m_StoredDude.DisplayName : "Your Dude");
                from.PlaySound(0x1F1);
            }
        }

        private static void ParkDude(DudeCreature dude)
        {
            if (dude == null || dude.Deleted)
                return;

            dude.Combatant = null;
            dude.Warmode = false;
            dude.Internalize();
        }

        public override void OnDelete()
        {
            DestroyParkedDude();
            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            writer.Write(m_StoredDude != null);
            if (m_StoredDude != null)
                m_StoredDude.Serialize(writer);

            writer.Write(m_SummonedDude);
            writer.Write(m_NextUseUtc);
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

            if (version >= 1)
                m_NextUseUtc = reader.ReadDateTime();
            else
                m_NextUseUtc = DateTime.MinValue;

            ItemID = BallItemId; // migrate older ball art
            RefreshHue(); // migrate steel/charged hues to green / type colors
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

                string blocked = DudeCapture.GetCaptureBlockReason(from, targeted, m_Ball);
                if (blocked != null)
                {
                    from.SendMessage(blocked);
                    return;
                }

                DudeCreature wild = targeted as DudeCreature;
                if (wild == null)
                    return;

                DudeCapture.BeginCapture(from, wild, m_Ball);
            }
        }
    }
}
