namespace Server.Mobiles
{
    [CorpseName("a hurricane corpse")]
    public class Hurricane : DudeCreature
    {
        [Constructable]
        public Hurricane()
            : base("hurricane", true)
        {
        }

        public Hurricane(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }
}
