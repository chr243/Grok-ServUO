using Server.Custom.Dudes;

namespace Server.Mobiles
{
    /// <summary>
    /// Named wild Dude types for XmlSpawner / [add] (S1 only).
    /// Use Ember, Droplet, Pebble, or Breeze as the spawn object name.
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

    [CorpseName("a breeze corpse")]
    public class Breeze : DudeCreature
    {
        [Constructable]
        public Breeze()
            : base("breeze", true)
        {
        }

        public Breeze(Serial serial)
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
