using System;

namespace Server.Items
{
    /// <summary>
    /// Tide Essence — unique drop from the matching elemental Dude boss. No craft recipe yet.
    /// </summary>
    public class TideCore : Item
    {
        [Constructable]
        public TideCore()
            : this(1)
        {
        }

        [Constructable]
        public TideCore(int amount)
            : base(0x1F1C)
        {
            Name = "Tide Essence";
            Hue = 1365;
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        public TideCore(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Rare drop from Tidewarden (future crafting)");
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
