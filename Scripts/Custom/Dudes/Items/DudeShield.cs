using System;
using Server.Custom.Dudes;

namespace Server.Items
{
    /// <summary>
    /// Universal TwoHanded DudeGear: Parrying (copy-in only, no gain).
    /// No RequiredType — any Dude type may wear it. Consumes SlotCost like other gear.
    /// </summary>
    public class DudeShield : DudeGear
    {
        private const double ItemCap = 120.0;
        private const int RollMin = 50;
        private const int RollMax = 80;

        private double m_Parrying;

        [Constructable]
        public DudeShield()
            : base(0x1B7B)
        {
            Name = "Dude Shield";
            Layer = Layer.TwoHanded;
            SlotCost = 1;
            LootType = LootType.Blessed;
            // RequiredType unset → HasRequiredType stays false (universal).

            Parrying = (double)Utility.RandomMinMax(RollMin, RollMax);
            Movable = true;
        }

        public DudeShield(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Parrying
        {
            get { return m_Parrying; }
            set { m_Parrying = ClampItem(value); InvalidateProperties(); }
        }

        private static double ClampItem(double value)
        {
            if (value < 0.0)
                return 0.0;
            if (value > ItemCap)
                return ItemCap;
            return value;
        }

        protected override string GetRoleLine()
        {
            return "Grants parrying; taunts";
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Parrying: {0:0.0}", m_Parrying);
            list.Add("Skills do not gain. Capped by Dude stage.");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
            writer.Write(m_Parrying);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_Parrying = ClampItem(reader.ReadDouble());

            if (m_Parrying < 50.0)
                m_Parrying = 50.0;

            Name = "Dude Shield";
            Layer = Layer.TwoHanded;
            SlotCost = 1;
            LootType = LootType.Blessed;
            // Do not set RequiredType — keep universal.
            Movable = true;
        }
    }
}
