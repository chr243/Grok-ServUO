namespace Server.Mobiles
{
    [CorpseName("an earth dude corpse")]
    public class EarthDude : DudeCreature
    {
        [Constructable]
        public EarthDude()
            : base("earth", true)
        {
        }

        public EarthDude(Serial serial)
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
