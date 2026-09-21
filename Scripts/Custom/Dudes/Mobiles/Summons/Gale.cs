namespace Server.Mobiles
{
    [CorpseName("a gale corpse")]
    public class Gale : DudeCreature
    {
        [Constructable]
        public Gale()
            : base("gale", true)
        {
        }

        public Gale(Serial serial)
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
