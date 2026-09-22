namespace Server.Mobiles
{
    /// <summary>
    /// Named Dude types for XmlSpawner / [add]. One class per element; a Dude grows
    /// within its type via ascension rather than changing species.
    /// Farm bosses (Emberlord, Tidewarden, Stonewarden, Galewarden) are unchanged.
    /// </summary>
    [CorpseName("a fire dude corpse")]
    public class FireDude : DudeCreature
    {
        [Constructable]
        public FireDude()
            : base("fire", true)
        {
        }

        public FireDude(Serial serial)
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
