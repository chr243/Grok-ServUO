namespace Server.Mobiles
{
    [CorpseName("a droplet corpse")]
    public class Droplet : DudeCreature
    {
        [Constructable]
        public Droplet()
            : base("droplet", true)
        {
        }

        public Droplet(Serial serial)
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
