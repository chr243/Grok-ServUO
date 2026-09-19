using Server.Mobiles;

namespace Server.Items
{
    /// <summary>
    /// Locked shorts whose hue shows Dude elemental type. Blessed / immovable; cannot be taken off.
    /// </summary>
    [FlipableAttribute(0x152e, 0x152f)]
    public class DudeTypeShorts : ShortPants
    {
        [Constructable]
        public DudeTypeShorts()
            : this(0)
        {
        }

        [Constructable]
        public DudeTypeShorts(int hue)
            : base(hue)
        {
            Name = "Type shorts";
            LootType = LootType.Blessed;
            Movable = false;
        }

        public DudeTypeShorts(Serial serial)
            : base(serial)
        {
        }

        public override bool CanEquip(Mobile from)
        {
            // Direct AddItem on DudeCreature / DudeJobWorker bypasses this.
            // Refuse players (and anyone else) equipping type shorts.
            if (from is DudeCreature || from is DudeJobWorker)
                return base.CanEquip(from);

            return false;
        }

        public override bool OnDragLift(Mobile from)
        {
            if (from != null)
                from.SendMessage("You cannot remove those shorts.");

            return false;
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

            Name = "Type shorts";
            LootType = LootType.Blessed;
            Movable = false;
        }
    }
}
