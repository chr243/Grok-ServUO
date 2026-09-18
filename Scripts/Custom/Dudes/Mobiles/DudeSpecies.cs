using Server.Custom.Dudes;

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

    [CorpseName("a flame corpse")]
    public class Flame : DudeCreature
    {
        [Constructable]
        public Flame()
            : base("flame", true)
        {
        }

        public Flame(Serial serial)
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

    [CorpseName("a blaze corpse")]
    public class Blaze : DudeCreature
    {
        [Constructable]
        public Blaze()
            : base("blaze", true)
        {
        }

        public Blaze(Serial serial)
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

    [CorpseName("a quake corpse")]
    public class Quake : DudeCreature
    {
        [Constructable]
        public Quake()
            : base("quake", true)
        {
        }

        public Quake(Serial serial)
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

    [CorpseName("a gale corpse")]
    public class Gale : DudeCreature
    {
        [Constructable]
        public Gale()
            : base("gale", true)
        {
        }

        public Gale(Serial serial)
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

    [CorpseName("a hurricane corpse")]
    public class Hurricane : DudeCreature
    {
        [Constructable]
        public Hurricane()
            : base("hurricane", true)
        {
        }

        public Hurricane(Serial serial)
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
