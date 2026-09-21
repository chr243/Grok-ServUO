namespace Server.Mobiles
{
    [CorpseName("a torrent corpse")]
    public class Torrent : DudeCreature
    {
        [Constructable]
        public Torrent()
            : base("torrent", true)
        {
        }

        public Torrent(Serial serial)
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
