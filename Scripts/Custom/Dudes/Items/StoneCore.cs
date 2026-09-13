using System;

namespace Server.Items
{
    /// <summary>
    /// Stone Essence — unique drop from the matching elemental Dude boss. No craft recipe yet.
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
            list.Add("Rare drop from Stonewarden (future crafting)");
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
    }
}
