using System;
using System.Collections.Generic;
using Server.ContextMenus;
using Server.Custom.Dudes;
using Server.Gumps;
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
    public partial class DudeBall : Item
    {
        private DudeData m_StoredDude;
        private DudeCreature m_SummonedDude;
        private DudeJobStation m_AssignedStation;
        private DateTime m_NextUseUtc;

        private const int BallItemId = 0xE73; // BolaBall graphic
        private const int EmptyHue = 0x59; // bright green — easy to spot empty in pack
        private const int FireHue = 0x21; // red
        private const int WaterHue = 0x5A; // blue
        private const int AirHue = 0x47E; // white
        private const int EarthHue = 0x22C; // light brown

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
            ClearDude(null);
        }

        /// <summary>
        /// Empties the ball (e.g. recycled in the Mixer) and deletes its Dude instance. Re-parking the
        /// instance on the empty ball (the old recall path) left it linked, so the next Dude caught into
        /// the ball reused it, worn gear and all, and after a restart it was an orphan deleted with that
        /// gear. Its DudeGear goes to <paramref name="from"/> first (see ReturnGear).
        /// </summary>
        /// <returns>The number of gear pieces returned.</returns>
        public int ClearDude(Mobile from)
        {
            DudeCreature dude = m_SummonedDude;
            int returned = 0;

            if (dude != null && !dude.Deleted)
                returned = ReturnGear(dude, from);

            m_StoredDude = null;
            RefreshHue();

            // Deletes the instance instead of re-parking it; a recall still finishing its FX sees the
            // deleted Dude in FinishRecallPark and leaves the ball alone.
            DestroyParkedDude();

            return returned;
        }

        /// <summary>
        /// Moves every DudeGear piece off a Dude instance that is about to be deleted (worn, loose on the
        /// mobile, or in its pack): into <paramref name="to"/>'s backpack, else at their feet; with no
        /// player, into the container holding this ball or next to it. Type shorts are not gear and go
        /// with the Dude.
        /// </summary>
        private int ReturnGear(DudeCreature dude, Mobile to)
        {
            List<Item> gear = new List<Item>();

            for (int i = 0; i < dude.Items.Count; i++)
            {
                Item item = dude.Items[i];

                if (item is DudeGear && !item.Deleted)
                    gear.Add(item);
            }

            Container pack = dude.Backpack;

            if (pack != null)
                gear.AddRange(pack.FindItemsByType<DudeGear>(true));

            for (int i = 0; i < gear.Count; i++)
                GiveItem(gear[i], to);

            return gear.Count;
        }

        private void GiveItem(Item item, Mobile to)
        {
            if (to != null && !to.Deleted)
            {
                Container pack = to.Backpack;

                if (pack != null && pack.TryDropItem(to, item, false))
                    return;

                if (to.Map != null && to.Map != Map.Internal)
                {
                    item.MoveToWorld(to.Location, to.Map);
                    return;
                }

                // Offline (Internal map) with a full pack: overfill it rather than lose the item.
                if (pack != null)
                {
                    pack.DropItem(item);
                    return;
                }
            }

            Container parent = Parent as Container;

            if (parent != null)
                parent.DropItem(item);
            else if (Map != null && Map != Map.Internal)
                item.MoveToWorld(GetWorldLocation(), Map);
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

            // Ball hue tracks the type profile (shorts colour), so a new type needs no edit here.
            DudeTypeProfile p = DudeTypeProfiles.Get(data.Type);
            return p != null ? p.ShortsHue : FireHue;
        }

        public override bool OnDragLift(Mobile from)
        {
            if (IsAssignedToJob && m_AssignedStation != null && m_AssignedStation.JobActive)
            {
                from.SendMessage("You cannot remove the Dude Ball while a job is in progress.");
                return false;
            }

            if (!base.OnDragLift(from))
                return false;

            // The ball is the Dude's anchor: lifting it (moving, trading or dropping it) recalls
            // the summoned Dude, so the creature can never outlive the ball's place in the pack.
            if (IsSummoned)
                Recall(from);

            return true;
        }

        public void ClearSummonLink()
        {
            m_SummonedDude = null;
            InvalidateProperties();
        }

        private ThrottledPropertyRefresh m_PropertyRefresh;

        /// <summary>
        /// InvalidateProperties for per-hit / per-kill callers (skill gains, EXP): at most one
        /// tooltip rebuild per 5s, see ThrottledPropertyRefresh.
        /// </summary>
        public void InvalidatePropertiesThrottled()
        {
            if (m_PropertyRefresh == null)
                m_PropertyRefresh = new ThrottledPropertyRefresh(InvalidateProperties, () => Deleted, TimeSpan.FromSeconds(5.0));

            m_PropertyRefresh.Request();
        }

        /// <summary>Drop the parked/world Dude instance (frees serial). Used on ball delete and by ClearDude.</summary>
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
                list.Add("Wrest {0:0.0} / Tact {1:0.0}", m_StoredDude.Wrestling, m_StoredDude.Tactics);
                list.Add("Anat {0:0.0} / Resist {1:0.0}", m_StoredDude.Anatomy, m_StoredDude.MagicResist);

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

        public override void GetContextMenuEntries(Mobile from, List<ContextMenuEntry> list)
        {
            base.GetContextMenuEntries(from, list);

            if (from == null || !from.Alive || !HasDude || StoredDude == null)
                return;

            list.Add(new LookCloserEntry(this));

            // Link/Unlink context entries parked — DudeLinkSystem + LinkingDevice stubs retained.
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

        public override void OnDelete()
        {
            DestroyParkedDude();
            base.OnDelete();
        }

        /// <summary>Deletes a Dude instance left on this ball after it was emptied, keeping its gear.</summary>
        private void DeleteOrphan(DudeCreature orphan)
        {
            if (orphan == null || orphan.Deleted)
                return;

            ReturnGear(orphan, null);

            orphan.BoundBall = null;

            if (orphan.ControlMaster != null)
                orphan.SetControlMaster(null);

            orphan.Delete();
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

                // Unknown / removed species: empty the ball (no migration).
                if (DudeRegistry.Get(m_StoredDude.DefinitionId) == null)
                    m_StoredDude = null;
            }
            else
            {
                m_StoredDude = null;
            }

            m_SummonedDude = reader.ReadMobile() as DudeCreature;

            // Never Delete() during World.Loading — it can hang/cascade the dual-save load.
            // Drop the link now and delete the orphan once loading finishes. Nothing else
            // references it (e.g. a Dude parked on a ball recycled in the Mixer before ClearDude
            // deleted instances), so previously it stayed on the Internal map, and in every save,
            // forever. Its gear is returned next to the ball first.
            if (m_StoredDude == null && m_SummonedDude != null)
            {
                DudeCreature orphan = m_SummonedDude;
                m_SummonedDude = null;

                if (orphan != null && !orphan.Deleted)
                {
                    if (World.Loading)
                        Timer.DelayCall(TimeSpan.Zero, () => DeleteOrphan(orphan));
                    else
                        DeleteOrphan(orphan);
                }
            }

            if (version >= 1)
                m_NextUseUtc = reader.ReadDateTime();
            else
                m_NextUseUtc = DateTime.MinValue;

            ItemID = BallItemId; // migrate older ball art
            RefreshHue(); // migrate steel/charged hues to green / type colors
        }

        private class LookCloserEntry : ContextMenuEntry
        {
            private readonly DudeBall m_Ball;

            // Verified Cliloc.enu 3006121 = "Look At". No "Trainer's Manual"/"Examine" cliloc or custom-text CME in this fork.
            public LookCloserEntry(DudeBall ball)
                : base(3006121, 2)
            {
                m_Ball = ball;
            }

            public override void OnClick()
            {
                if (Owner == null || Owner.From == null)
                    return;

                Mobile from = Owner.From;

                if (m_Ball == null || m_Ball.Deleted || !m_Ball.HasDude)
                    return;

                if (!from.Alive)
                    return;

                bool inPack = m_Ball.IsChildOf(from.Backpack) || m_Ball.RootParent == from;
                bool inRange = from.InRange(m_Ball.GetWorldLocation(), 2);

                if (!inPack && !inRange)
                {
                    from.SendLocalizedMessage(500446); // That is too far away.
                    return;
                }

                from.CloseGump(typeof(DudeInfoGump));
                from.SendGump(new DudeInfoGump(DudeInfoView.FromDudeBall(m_Ball)));
            }
        }

        private class LinkEntry : ContextMenuEntry
        {
            private readonly DudeBall m_Ball;

            // Cliloc 1115891 = "Link"
            public LinkEntry(DudeBall ball)
                : base(1115891, 2)
            {
                m_Ball = ball;
            }

            public override void OnClick()
            {
                if (Owner == null || Owner.From == null)
                    return;

                Mobile from = Owner.From;

                if (m_Ball == null || m_Ball.Deleted || !m_Ball.HasDude)
                    return;

                if (!from.Alive)
                    return;

                if (!m_Ball.IsChildOf(from.Backpack) && m_Ball.RootParent != from)
                {
                    from.SendMessage("That Dude Ball must be in your backpack.");
                    return;
                }

                DudeLinkSystem.TryLink(from, m_Ball);
            }
        }

        private class UnlinkEntry : ContextMenuEntry
        {
            private readonly DudeBall m_Ball;

            // Cliloc 1115930 = "Unlink"
            public UnlinkEntry(DudeBall ball)
                : base(1115930, 2)
            {
                m_Ball = ball;
            }

            public override void OnClick()
            {
                if (Owner == null || Owner.From == null)
                    return;

                Mobile from = Owner.From;

                if (m_Ball == null || m_Ball.Deleted)
                    return;

                if (!from.Alive)
                    return;

                if (!m_Ball.IsChildOf(from.Backpack) && m_Ball.RootParent != from)
                {
                    from.SendMessage("That Dude Ball must be in your backpack.");
                    return;
                }

                if (!DudeLinkSystem.IsLinked(from) || DudeLinkSystem.GetLinkedBall(from) != m_Ball)
                {
                    from.SendMessage("You are not linked to this Dude.");
                    return;
                }

                DudeLinkSystem.TryUnlink(from);
            }
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
