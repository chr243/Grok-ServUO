using System;
using Server.Custom.Dudes;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Testing aid: grants +1 Dude level. Blessed vial.
    /// </summary>
    public class MysteryJuice : Item
    {
        [Constructable]
        public MysteryJuice()
            : base(0xF0E) // potion vial
        {
            Name = "Mystery Juice";
            Hue = 0x13; // weird purple-ish
            Weight = 1.0;
            LootType = LootType.Blessed;
            Stackable = false;
        }

        public MysteryJuice(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Double-click and target a Dude to grant +1 level.");
            list.Add("Testing juice. Tastes like secrets.");
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

            from.SendMessage("Target a Dude Ball or summoned Dude.");
            from.Target = new JuiceTarget(this);
        }

        public bool TryLevel(Mobile from, DudeBall ball)
        {
            if (from == null || Deleted || ball == null || ball.Deleted || !ball.HasDude)
                return false;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return false;
            }

            DudeData data = ball.StoredDude;
            if (data.Level >= DudeExperience.GetMaxLevel(data))
            {
                from.SendMessage("{0} is already at max level for this form.", data.DisplayName);
                return false;
            }

            DudeExperience.LevelUp(data, from);
            ball.InvalidateProperties();

            DudeCreature live = ball.SummonedDude;
            if (live != null && !live.Deleted && live.Map != null && live.Map != Map.Internal)
                live.ApplyData(data, true);

            from.SendMessage(0x59, "The Mystery Juice levels {0} to {1}!", data.DisplayName, data.Level);
            from.PlaySound(0x1F2);
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
            LootType = LootType.Blessed;
        }

        private class JuiceTarget : Target
        {
            private readonly MysteryJuice m_Juice;

            public JuiceTarget(MysteryJuice juice)
                : base(8, false, TargetFlags.None)
            {
                m_Juice = juice;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Juice == null || m_Juice.Deleted)
                    return;

                DudeBall ball = targeted as DudeBall;
                if (ball == null)
                {
                    DudeCreature dude = targeted as DudeCreature;
                    if (dude != null && !dude.IsWild)
                        ball = dude.BoundBall;
                }

                if (ball == null || !ball.HasDude)
                {
                    from.SendMessage("That is not a Dude.");
                    return;
                }

                m_Juice.TryLevel(from, ball);
            }
        }
    }
}
