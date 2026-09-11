using System;
using Server.Engines.Craft;

namespace Server.Items
{
    /// <summary>
    /// Tool that opens DefDudeCrafting (empty Dude Ball recipe).
    /// </summary>
    public class DudeCraftingKit : BaseTool
    {
        [Constructable]
        public DudeCraftingKit()
            : base(0x1EB8)
        {
            Name = "Dude Crafting Kit";
            Weight = 1.0;
            Hue = 1153;
        }

        [Constructable]
        public DudeCraftingKit(int uses)
            : base(uses, 0x1EB8)
        {
            Name = "Dude Crafting Kit";
            Weight = 1.0;
            Hue = 1153;
        }

        public DudeCraftingKit(Serial serial)
            : base(serial)
        {
        }

        public override CraftSystem CraftSystem
        {
            get { return DefDudeCrafting.CraftSystem; }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
