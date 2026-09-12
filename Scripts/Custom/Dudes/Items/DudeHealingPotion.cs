using System;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Heals a summoned Dude for 20% of HitsMax. Double-click, then target the live Dude.
    /// </summary>
    public class DudeHealingPotion : Item
    {
        [Constructable]
        public DudeHealingPotion()
            : base(0xF0C) // heal potion bottle
        {
            Name = "Dude Healing Potion";
            Hue = 0x21; // warm red-pink
            Weight = 1.0;
            Stackable = false;
        }

        public DudeHealingPotion(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Double-click and target a summoned Dude to heal 20% HP.");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || Deleted)
                return;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            from.SendMessage("Target a summoned Dude to heal.");
            from.Target = new HealTarget(this);
        }

        public bool TryHeal(Mobile from, DudeCreature dude)
        {
            if (from == null || Deleted || dude == null || dude.Deleted)
                return false;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return false;
            }

            if (dude.IsWild)
            {
                from.SendMessage("That Dude is wild.");
                return false;
            }

            if (dude.BoundBall == null || dude.BoundBall.Deleted)
            {
                from.SendMessage("That Dude is not bound to a Dude Ball.");
                return false;
            }

            if (dude.ControlMaster != from && from.AccessLevel < AccessLevel.GameMaster)
            {
                from.SendMessage("That is not your Dude.");
                return false;
            }

            if (!from.InRange(dude, 8) || !from.CanSee(dude))
            {
                from.SendMessage("You cannot reach that Dude.");
                return false;
            }

            if (dude.Hits >= dude.HitsMax)
            {
                from.SendMessage("{0} is already at full health.", dude.Name);
                return false;
            }

            int heal = Math.Max(1, dude.HitsMax / 5); // 20%
            int before = dude.Hits;
            dude.Hits = Math.Min(dude.HitsMax, dude.Hits + heal);
            int actual = dude.Hits - before;

            if (dude.BoundBall != null && dude.BoundBall.StoredDude != null)
            {
                dude.BoundBall.StoredDude.Hits = dude.Hits;
                dude.BoundBall.InvalidateProperties();
            }

            from.SendMessage(0x59, "You heal {0} for {1} hit points.", dude.Name, actual);
            dude.PlaySound(0x1F2);
            dude.FixedEffect(0x376A, 9, 32);

            Consume();
            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        private class HealTarget : Target
        {
            private readonly DudeHealingPotion m_Potion;

            public HealTarget(DudeHealingPotion potion)
                : base(8, false, TargetFlags.None)
            {
                m_Potion = potion;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Potion == null || m_Potion.Deleted)
                    return;

                DudeCreature dude = targeted as DudeCreature;
                if (dude == null)
                {
                    from.SendMessage("That is not a summoned Dude.");
                    return;
                }

                m_Potion.TryHeal(from, dude);
            }
        }
    }
}
