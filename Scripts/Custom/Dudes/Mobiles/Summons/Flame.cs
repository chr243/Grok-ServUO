namespace Server.Mobiles
{
    [CorpseName("a flame corpse")]
    public class Flame : DudeCreature
    {
        [Constructable]
        public Flame()
            : base("flame", true)
        {
        }

        public Flame(Serial serial)
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
