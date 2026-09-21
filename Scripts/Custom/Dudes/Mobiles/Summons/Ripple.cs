namespace Server.Mobiles
{
    [CorpseName("a ripple corpse")]
    public class Ripple : DudeCreature
    {
        [Constructable]
        public Ripple()
            : base("ripple", true)
        {
        }

        public Ripple(Serial serial)
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
