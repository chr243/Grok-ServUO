using Server.Network;

namespace Server.Items
{
    public class NewbieBag : Bag
    {
        [Constructable]
        public NewbieBag()
        {
            Name = "Newbie Bag";
            Hue = 1161;
            Weight = 2.0;
            LootType = LootType.Blessed;
            MaxItems = 30;
        }

        public NewbieBag(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Starter gear for new trainers.");
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
        }
    }
}
