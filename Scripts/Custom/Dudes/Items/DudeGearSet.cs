using Server.Custom.Dudes;

namespace Server.Items
{
    // Type-locked DudeGear granting the kit abilities (4 types × 3 abilities = 12).
    // Each type profile names its starter sash (DudeTypeProfile.StarterSashType), so no
    // per-type switch is needed to find a Dude's starting gear.
    // Hues match essences: Fire 1161, Water 1365, Earth 2413, Air 1153.
    // Layers: Helm reserved for Magical Dude Hat; type earrings on Earrings;
    // sash InnerTorso; bracers Bracelet. Slot budget is a count, not a layer ladder.
    // ItemIDs: sash 0x1541, earrings 0x1087, bracers/bracelet 0x1086.

    #region Fire

    public class EmberCirclet : DudeGear
    {
        [Constructable]
        public EmberCirclet()
            : base(0x1087)
        {
            Name = "Ember Earrings";
            Hue = 1161;
            Layer = Layer.Earrings;
            AbilityId = "ring_of_fire";
            SlotCost = 1;
            RequiredType = DudeType.Fire;
            Movable = true;
        }

        public EmberCirclet(Serial serial)
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

            Name = "Ember Earrings";
            Hue = 1161;
            ItemID = 0x1087;
            Layer = Layer.Earrings;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "ring_of_fire";
            RequiredType = DudeType.Fire;
            Movable = true;
        }
    }

    public class EmberBracers : DudeGear
    {
        [Constructable]
        public EmberBracers()
            : base(0x1086)
        {
            Name = "Ember Bracers";
            Hue = 1161;
            Layer = Layer.Bracelet;
            AbilityId = "burn";
            SlotCost = 1;
            RequiredType = DudeType.Fire;
            Movable = true;
        }

        public EmberBracers(Serial serial)
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

            Name = "Ember Bracers";
            Hue = 1161;
            Layer = Layer.Bracelet;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "burn";
            RequiredType = DudeType.Fire;
            Movable = true;
        }
    }

    #endregion

    #region Water

    public class TideSash : DudeGear
    {
        [Constructable]
        public TideSash()
            : base(0x1541)
        {
            Name = "Tide Sash";
            Hue = 1365;
            Layer = Layer.InnerTorso;
            AbilityId = "tide_mend";
            SlotCost = 1;
            RequiredType = DudeType.Water;
            Movable = true;
        }

        public TideSash(Serial serial)
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

            Name = "Tide Sash";
            Hue = 1365;
            Layer = Layer.InnerTorso;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "tide_mend";
            RequiredType = DudeType.Water;
            Movable = true;
        }
    }

    public class TideCirclet : DudeGear
    {
        [Constructable]
        public TideCirclet()
            : base(0x1087)
        {
            Name = "Tide Earrings";
            Hue = 1365;
            Layer = Layer.Earrings;
            AbilityId = "tide_chorus";
            SlotCost = 1;
            RequiredType = DudeType.Water;
            Movable = true;
        }

        public TideCirclet(Serial serial)
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

            Name = "Tide Earrings";
            Hue = 1365;
            ItemID = 0x1087;
            Layer = Layer.Earrings;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "tide_chorus";
            RequiredType = DudeType.Water;
            Movable = true;
        }
    }

    public class TideBracers : DudeGear
    {
        [Constructable]
        public TideBracers()
            : base(0x1086)
        {
            Name = "Tide Bracers";
            Hue = 1365;
            Layer = Layer.Bracelet;
            AbilityId = "spring";
            SlotCost = 1;
            RequiredType = DudeType.Water;
            Movable = true;
        }

        public TideBracers(Serial serial)
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

            Name = "Tide Bracers";
            Hue = 1365;
            Layer = Layer.Bracelet;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "spring";
            RequiredType = DudeType.Water;
            Movable = true;
        }
    }

    #endregion

    #region Earth

    public class StoneSash : DudeGear
    {
        [Constructable]
        public StoneSash()
            : base(0x1541)
        {
            Name = "Stone Sash";
            Hue = 2413;
            Layer = Layer.InnerTorso;
            AbilityId = "fault_strike";
            SlotCost = 1;
            RequiredType = DudeType.Earth;
            Movable = true;
        }

        public StoneSash(Serial serial)
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

            Name = "Stone Sash";
            Hue = 2413;
            Layer = Layer.InnerTorso;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "fault_strike";
            RequiredType = DudeType.Earth;
            Movable = true;
        }
    }

    public class StoneCirclet : DudeGear
    {
        [Constructable]
        public StoneCirclet()
            : base(0x1087)
        {
            Name = "Stone Earrings";
            Hue = 2413;
            Layer = Layer.Earrings;
            AbilityId = "aftershock";
            SlotCost = 1;
            RequiredType = DudeType.Earth;
            Movable = true;
        }

        public StoneCirclet(Serial serial)
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

            Name = "Stone Earrings";
            Hue = 2413;
            ItemID = 0x1087;
            Layer = Layer.Earrings;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "aftershock";
            RequiredType = DudeType.Earth;
            Movable = true;
        }
    }

    public class StoneBracers : DudeGear
    {
        [Constructable]
        public StoneBracers()
            : base(0x1086)
        {
            Name = "Stone Bracers";
            Hue = 2413;
            Layer = Layer.Bracelet;
            AbilityId = "faultline";
            SlotCost = 1;
            RequiredType = DudeType.Earth;
            Movable = true;
        }

        public StoneBracers(Serial serial)
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

            Name = "Stone Bracers";
            Hue = 2413;
            Layer = Layer.Bracelet;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "faultline";
            RequiredType = DudeType.Earth;
            Movable = true;
        }
    }

    #endregion

    #region Air

    public class GaleSash : DudeGear
    {
        [Constructable]
        public GaleSash()
            : base(0x1541)
        {
            Name = "Gale Sash";
            Hue = 1153;
            Layer = Layer.InnerTorso;
            AbilityId = "tailwind_self";
            SlotCost = 1;
            RequiredType = DudeType.Air;
            Movable = true;
        }

        public GaleSash(Serial serial)
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

            Name = "Gale Sash";
            Hue = 1153;
            Layer = Layer.InnerTorso;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "tailwind_self";
            RequiredType = DudeType.Air;
            Movable = true;
        }
    }

    public class GaleCirclet : DudeGear
    {
        [Constructable]
        public GaleCirclet()
            : base(0x1087)
        {
            Name = "Gale Earrings";
            Hue = 1153;
            Layer = Layer.Earrings;
            AbilityId = "tailwind";
            SlotCost = 1;
            RequiredType = DudeType.Air;
            Movable = true;
        }

        public GaleCirclet(Serial serial)
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

            Name = "Gale Earrings";
            Hue = 1153;
            ItemID = 0x1087;
            Layer = Layer.Earrings;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "tailwind";
            RequiredType = DudeType.Air;
            Movable = true;
        }
    }

    public class GaleBracers : DudeGear
    {
        [Constructable]
        public GaleBracers()
            : base(0x1086)
        {
            Name = "Gale Bracers";
            Hue = 1153;
            Layer = Layer.Bracelet;
            AbilityId = "slipstream";
            SlotCost = 1;
            RequiredType = DudeType.Air;
            Movable = true;
        }

        public GaleBracers(Serial serial)
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

            Name = "Gale Bracers";
            Hue = 1153;
            Layer = Layer.Bracelet;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "slipstream";
            RequiredType = DudeType.Air;
            Movable = true;
        }
    }

    #endregion
}
