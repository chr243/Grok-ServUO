using System;
using Server.Custom.Dudes;

namespace Server.Items
{
    /// <summary>
    /// Universal Helm DudeGear: Magery / Eval Int / Meditation (copy-in only, no gain).
    /// No RequiredType — any Dude type may wear it. Exclusive with other Helm gear.
    /// </summary>
    public class MagicalDudeHat : DudeGear
    {
        private const double ItemCap = 120.0;
        private const int RollMin = 40;
        private const int RollMax = 80;

        private double m_Magery;
        private double m_EvalInt;
        private double m_Meditation;

        [Constructable]
        public MagicalDudeHat()
            : base(0x1718)
        {
            Name = "Magical Dude Hat";
            Hue = 0x482;
            Layer = Layer.Helm;
            SlotCost = 1;
            // RequiredType unset → HasRequiredType stays false (universal).

            Magery = (double)Utility.RandomMinMax(RollMin, RollMax);
            EvalInt = (double)Utility.RandomMinMax(RollMin, RollMax);
            Meditation = (double)Utility.RandomMinMax(RollMin, RollMax);
            Movable = true;
        }

        public MagicalDudeHat(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Magery
        {
            get { return m_Magery; }
            set { m_Magery = ClampItem(value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double EvalInt
        {
            get { return m_EvalInt; }
            set { m_EvalInt = ClampItem(value); InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double Meditation
        {
            get { return m_Meditation; }
            set { m_Meditation = ClampItem(value); InvalidateProperties(); }
        }

        private static double ClampItem(double value)
        {
            if (value < 0.0)
                return 0.0;
            if (value > ItemCap)
                return ItemCap;
            return value;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Magery: {0:0.0}", m_Magery);
            list.Add("Eval Int: {0:0.0}", m_EvalInt);
            list.Add("Meditation: {0:0.0}", m_Meditation);
            list.Add("Skills do not gain. Capped by Dude stage.");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
            writer.Write(m_Magery);
            writer.Write(m_EvalInt);
            writer.Write(m_Meditation);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_Magery = ClampItem(reader.ReadDouble());
            m_EvalInt = ClampItem(reader.ReadDouble());
            m_Meditation = ClampItem(reader.ReadDouble());

            Name = "Magical Dude Hat";
            Hue = 0x482;
            Layer = Layer.Helm;
            SlotCost = 1;
            // Do not set RequiredType — keep universal.
            Movable = true;
        }
    }
}
