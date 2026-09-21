namespace Server.Mobiles
{
    [CorpseName("a blaze corpse")]
    public class Blaze : DudeCreature
    {
        [Constructable]
        public Blaze()
            : base("blaze", true)
        {
        }

        public Blaze(Serial serial)
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
