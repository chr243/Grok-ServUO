using Server.Mobiles;

namespace Server.Items
{
    /// <summary>
    /// Visual-only hatchet for Air DudeJobWorkers. Blessed / immovable; not lootable gear.
    /// </summary>
    [FlipableAttribute(0xF43, 0xF44)]
    public class DudeWorkHatchet : Hatchet
    {
        [Constructable]
        public DudeWorkHatchet()
        {
            LootType = LootType.Blessed;
            Movable = false;
            Layer = Layer.OneHanded;
        }

        public DudeWorkHatchet(Serial serial)
            : base(serial)
        {
        }

        public override bool CanEquip(Mobile from)
        {
            // Direct AddItem on DudeJobWorker bypasses this.
            if (from is DudeJobWorker)
                return base.CanEquip(from);

            return false;
        }

        public override bool OnDragLift(Mobile from)
        {
            if (from != null)
                from.SendMessage("You cannot remove that tool.");

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

            LootType = LootType.Blessed;
            Movable = false;
            Layer = Layer.OneHanded;
        }
    }
}
