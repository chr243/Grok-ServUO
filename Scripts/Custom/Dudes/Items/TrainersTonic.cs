using System;
using Server.Custom.Dudes;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Tradable tonic: grants 20% of a Dude's EXP to next level via DudeExperience.AwardExperience.
    /// </summary>
    public class TrainersTonic : Item
    {
        [Constructable]
        public TrainersTonic()
            : this(1)
        {
        }

        [Constructable]
        public TrainersTonic(int amount)
            : base(0xF0B) // refresh potion graphic
        {
            Name = "Trainer's Tonic";
            Hue = 1161; // matches Newbie Bag
            Weight = 0.2;
            Stackable = true;
            Amount = amount > 0 ? amount : 1;
            LootType = LootType.Regular;
        }

        public TrainersTonic(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Grants 20% of a Dude's EXP to next level.");
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

            from.SendMessage("Target a Dude Ball or your summoned Dude.");
            from.Target = new TonicTarget(this);
        }

        public bool TryApply(Mobile from, DudeBall ball, DudeCreature targetedDude)
        {
            if (from == null || Deleted || ball == null || ball.Deleted)
                return false;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return false;
            }

            if (!ball.HasDude || ball.StoredDude == null)
            {
                from.SendMessage("That is not your Dude.");
                return false;
            }

            if (!OwnsDude(from, ball, targetedDude))
            {
                from.SendMessage("That is not your Dude.");
                return false;
            }

            DudeData data = ball.StoredDude;

            if (data.Level >= DudeExperience.GetMaxLevel(data))
            {
                from.SendMessage("Already at max level for this form.");
                return false;
            }

            int need = data.EXPToNext;
            if (need < 1)
                need = 1;

            int amount = need / 5;
            if (amount < 1)
                amount = 1;

            DudeExperience.AwardExperience(ball, amount, from);
            Consume();
            from.SendMessage(0x44, "{0} gained {1} EXP from the tonic.", data.DisplayName, amount);
            return true;
        }

        private static bool OwnsDude(Mobile from, DudeBall ball, DudeCreature targetedDude)
        {
            if (ball.IsChildOf(from.Backpack) || ball.RootParent == from)
                return true;

            if (targetedDude != null && !targetedDude.Deleted && targetedDude.ControlMaster == from)
                return true;

            DudeCreature live = ball.SummonedDude;
            if (live != null && !live.Deleted && live.ControlMaster == from)
                return true;

            return false;
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

            Stackable = true;
            LootType = LootType.Regular;
        }

        private class TonicTarget : Target
        {
            private readonly TrainersTonic m_Tonic;

            public TonicTarget(TrainersTonic tonic)
                : base(8, false, TargetFlags.None)
            {
                m_Tonic = tonic;
                CheckLOS = false; // allow targeting Dude Balls in pack
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Tonic == null || m_Tonic.Deleted || from == null)
                    return;

                DudeBall ball = targeted as DudeBall;
                DudeCreature dude = null;

                if (ball == null)
                {
                    dude = targeted as DudeCreature;
                    if (dude != null && !dude.IsWild && dude.BoundBall != null)
                        ball = dude.BoundBall;
                }

                if (ball == null || !ball.HasDude || ball.StoredDude == null)
                {
                    from.SendMessage("That is not your Dude.");
                    return;
                }

                m_Tonic.TryApply(from, ball, dude);
            }
        }
    }
}
