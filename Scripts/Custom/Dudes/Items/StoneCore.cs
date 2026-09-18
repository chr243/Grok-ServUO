using System;
using Server.Custom.Dudes;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Stone Essence — evolves the Earth line (pebble → boulder → quake).
    /// </summary>
    public class StoneCore : Item
    {
        [Constructable]
        public StoneCore()
            : this(1)
        {
        }

        [Constructable]
        public StoneCore(int amount)
            : base(0x1F1C)
        {
            Name = "Stone Essence";
            Hue = 2413;
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        public StoneCore(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Evolution material");
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

            from.SendMessage("Target a Dude Ball or summoned Dude to evolve.");
            from.Target = new EvolveTarget(this);
        }

        public bool TryEvolve(Mobile from, DudeBall ball)
        {
            return DudeEvolution.TryEvolve(from, ball, this, DudeType.Earth, "pebble", "boulder", "quake");
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

        private class EvolveTarget : Target
        {
            private readonly StoneCore m_Core;

            public EvolveTarget(StoneCore core)
                : base(8, false, TargetFlags.None)
            {
                m_Core = core;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Core == null || m_Core.Deleted)
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
                    from.SendMessage("That is not a filled Dude Ball or summoned Dude.");
                    return;
                }

                m_Core.TryEvolve(from, ball);
            }
        }
    }
}
