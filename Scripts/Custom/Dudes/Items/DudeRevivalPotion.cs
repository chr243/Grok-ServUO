using System;
using Server.Custom.Dudes;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Revives a fainted Dude stored in a Dude Ball. Double-click, then target the ball.
    /// </summary>
    public class DudeRevivalPotion : Item
    {
        [Constructable]
        public DudeRevivalPotion()
            : this(1)
        {
        }

        [Constructable]
        public DudeRevivalPotion(int amount)
            : base(0xF0B) // classic potion bottle graphic
        {
            Name = "Dude Revival Potion";
            Hue = 0x48E; // soft green revive tint
            Weight = 1.0;
            Stackable = true;
            Amount = amount > 0 ? amount : 1;
        }

        public DudeRevivalPotion(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Double-click and target a fainted Dude Ball.");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || Deleted)
                return;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            from.SendMessage("Target a Dude Ball containing a fainted Dude.");
            from.Target = new ReviveTarget(this);
        }

        public bool TryRevive(Mobile from, DudeBall ball)
        {
            if (from == null || Deleted || ball == null || ball.Deleted)
                return false;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return false;
            }

            if (!ball.IsChildOf(from.Backpack) && ball.RootParent != from)
            {
                from.SendMessage("That Dude Ball must be in your pack.");
                return false;
            }

            if (!ball.HasDude || ball.StoredDude == null)
            {
                from.SendMessage("That Dude Ball is empty.");
                return false;
            }

            if (!ball.StoredDude.IsFainted)
            {
                from.SendMessage("{0} is not fainted.", ball.StoredDude.DisplayName);
                return false;
            }

            if (ball.IsSummoned)
            {
                from.SendMessage("Recall the Dude before using a revival potion.");
                return false;
            }

            DudeData data = ball.StoredDude;
            data.IsFainted = false;
            data.Hits = data.HitsMax;
            ball.InvalidateProperties();

            from.SendMessage(0x59, "{0} has been revived!", data.DisplayName);
            from.PlaySound(0x214);
            from.FixedEffect(0x376A, 10, 16);

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
            Stackable = true;
        }

        private class ReviveTarget : Target
        {
            private readonly DudeRevivalPotion m_Potion;

            public ReviveTarget(DudeRevivalPotion potion)
                : base(8, false, TargetFlags.None)
            {
                m_Potion = potion;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Potion == null || m_Potion.Deleted)
                    return;

                DudeBall ball = targeted as DudeBall;
                if (ball == null)
                {
                    from.SendMessage("That is not a Dude Ball.");
                    return;
                }

                m_Potion.TryRevive(from, ball);
            }
        }
    }
}
