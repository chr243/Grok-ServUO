namespace Server.Mobiles
{
    [CorpseName("a pebble corpse")]
    public class Pebble : DudeCreature
    {
        [Constructable]
        public Pebble()
            : base("pebble", true)
        {
        }

        public Pebble(Serial serial)
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
