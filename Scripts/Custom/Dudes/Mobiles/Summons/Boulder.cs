namespace Server.Mobiles
{
    [CorpseName("a boulder corpse")]
    public class Boulder : DudeCreature
    {
        [Constructable]
        public Boulder()
            : base("boulder", true)
        {
        }

        public Boulder(Serial serial)
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
