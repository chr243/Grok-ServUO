using System;

namespace Server.Items
{
    /// <summary>
    /// Gale Essence — unique drop from the matching elemental Dude boss. No craft recipe yet.
    /// </summary>
    public class GaleCore : Item
    {
        [Constructable]
        public GaleCore()
            : this(1)
        {
        }

        [Constructable]
        public GaleCore(int amount)
            : base(0x1F1C)
        {
            Name = "Gale Essence";
            Hue = 1153;
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        public GaleCore(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Evolution material");
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
