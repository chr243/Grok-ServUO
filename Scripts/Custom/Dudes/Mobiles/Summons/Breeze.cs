namespace Server.Mobiles
{
    [CorpseName("a breeze corpse")]
    public class Breeze : DudeCreature
    {
        [Constructable]
        public Breeze()
            : base("breeze", true)
        {
        }

        public Breeze(Serial serial)
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
