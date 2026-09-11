using System;
using Server.Custom.Dudes;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Inspect tool: double-click then target a wild/summoned Dude, filled Dude Ball, or Dude boss.
    /// </summary>
    public class TrainersManual : Item
    {
        [Constructable]
        public TrainersManual()
            : base(0xFF4) // classic closed book graphic (UOR-friendly)
        {
            Name = "Trainer's Manual";
            Weight = 1.0;
            Hue = 0x48D;
            LootType = LootType.Blessed;
        }

        public TrainersManual(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("A field guide for inspecting Dudes");
            list.Add("Double-click and target a Dude, Dude Ball, or boss");
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, "a Trainer's Manual");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            bool inPack = IsChildOf(from.Backpack) || RootParent == from;
            bool inRange = from.InRange(GetWorldLocation(), 2);

            if (!inPack && !inRange)
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            from.SendMessage("Target a Dude, a filled Dude Ball, or a Dude boss to inspect.");
            from.Target = new ManualTarget(this);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            DudeRegistry.EnsureInitialized();
            DudeAbilityRegistry.EnsureInitialized();
        }

        private class ManualTarget : Target
        {
            private readonly TrainersManual m_Manual;

            public ManualTarget(TrainersManual manual)
                : base(12, false, TargetFlags.None)
            {
                m_Manual = manual;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Manual == null || m_Manual.Deleted || from == null)
                    return;

                bool inPack = m_Manual.IsChildOf(from.Backpack) || m_Manual.RootParent == from;
                bool inRange = from.InRange(m_Manual.GetWorldLocation(), 2);

                if (!inPack && !inRange)
                {
                    from.SendLocalizedMessage(500446);
                    return;
                }

                DudeInfoView view = null;

                DudeBall ball = targeted as DudeBall;
                if (ball != null)
                {
                    if (!ball.HasDude || ball.StoredDude == null)
                    {
                        from.SendMessage("That Dude Ball is empty. Catch a Dude first, or target a filled ball.");
                        return;
                    }

                    view = DudeInfoView.FromDudeBall(ball);
                    from.SendGump(new DudeInfoGump(view));
                    from.SendMessage(0x59, "You study the Dude sealed within the ball.");
                    return;
                }

                DudeCreature dude = targeted as DudeCreature;
                if (dude != null)
                {
                    if (dude.Deleted)
                    {
                        from.SendMessage("That Dude is no longer here.");
                        return;
                    }

                    view = DudeInfoView.FromDudeCreature(dude);
                    from.SendGump(new DudeInfoGump(view));

                    if (dude.IsWild)
                        from.SendMessage(0x59, "You study the wild Dude.");
                    else
                        from.SendMessage(0x59, "You study the summoned Dude.");
                    return;
                }

                DudeBoss boss = targeted as DudeBoss;
                if (boss != null)
                {
                    if (boss.Deleted)
                    {
                        from.SendMessage("That creature is no longer here.");
                        return;
                    }

                    view = DudeInfoView.FromDudeBoss(boss);
                    from.SendGump(new DudeInfoGump(view));
                    from.SendMessage(0x59, "You study the boss. It cannot be caught in a Dude Ball.");
                    return;
                }

                if (targeted is Mobile)
                {
                    from.SendMessage("That is not a Dude. Target a Dude, Dude Ball, or Dude boss.");
                    return;
                }

                if (targeted is Item)
                {
                    from.SendMessage("That is not a filled Dude Ball. Target a Dude, Dude Ball, or Dude boss.");
                    return;
                }

                from.SendMessage("You cannot inspect that with the Trainer's Manual.");
            }
        }
    }
}
