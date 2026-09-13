using System;

namespace Server.Items
{
    /// <summary>
    /// Rare Emberlord drop. Serializes; reserved for future Dude crafting (no recipe yet).
    /// </summary>
    public class EmberCore : Item
    {
        [Constructable]
        public EmberCore()
            : this(1)
        {
        }

        [Constructable]
        public EmberCore(int amount)
            : base(0x1F1C) // classic power-crystal graphic (UOR-friendly)
        {
            Name = "Ember Essence";
            Hue = 1161;
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        public EmberCore(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Ember Essence taken from Emberlord");
            list.Add("Reserved for future Dude crafting");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version — reserve for crafting fields later
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
