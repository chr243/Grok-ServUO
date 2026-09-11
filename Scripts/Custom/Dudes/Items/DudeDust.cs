using System;

namespace Server.Items
{
    /// <summary>
    /// Recycled essence from a Dude (via DudeMixer). Used to craft empty Dude Balls.
    /// Stackable; quantity is authoritative. Architecture ready for typed/rarity variants later.
    /// </summary>
    public class DudeDust : Item
    {
        [Constructable]
        public DudeDust()
            : this(1)
        {
        }

        [Constructable]
        public DudeDust(int amount)
            : base(0xF8C) // classic ash/powder graphic (UOR-friendly)
        {
            Name = "Dude Dust";
            Hue = 2413;
            Stackable = true;
            Amount = amount;
            Weight = 0.1;
        }

        public DudeDust(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Recycled Dude essence");
            list.Add("Used to craft empty Dude Balls");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version — reserve for rarity/type fields later
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
