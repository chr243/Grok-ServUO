namespace Server.Mobiles
{
    [CorpseName("an air dude corpse")]
    public class AirDude : DudeCreature
    {
        [Constructable]
        public AirDude()
            : base("air", true)
        {
        }

        public AirDude(Serial serial)
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
