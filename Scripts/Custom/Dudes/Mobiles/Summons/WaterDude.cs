namespace Server.Mobiles
{
    [CorpseName("a water dude corpse")]
    public class WaterDude : DudeCreature
    {
        [Constructable]
        public WaterDude()
            : base("water", true)
        {
        }

        public WaterDude(Serial serial)
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
