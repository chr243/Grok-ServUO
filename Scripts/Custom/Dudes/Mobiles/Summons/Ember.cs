namespace Server.Mobiles
{
    /// <summary>
    /// Named Dude types for XmlSpawner / [add].
    /// Fire: Ember, Flame, Blaze | Water: Droplet, Ripple, Torrent |
    /// Earth: Pebble, Boulder, Quake | Air: Breeze, Gale, Hurricane.
    /// Farm bosses already constructable: Emberlord, Tidewarden, Stonewarden, Galewarden.
    /// </summary>
    [CorpseName("an ember corpse")]
    public class Ember : DudeCreature
    {
        [Constructable]
        public Ember()
            : base("ember", true)
        {
        }

        public Ember(Serial serial)
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
