using System;
using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    /// <summary>
    /// Placeable Daily Training board. Double-click in range to open DudeDailyGump.
    /// </summary>
    public class DudeDailyBoard : Item
    {
        [Constructable]
        public DudeDailyBoard()
            : base(0xED4) // bulletin board graphic; 0x1183 also acceptable
        {
            Name = "Daily Training Board";
            Hue = 1161;
            Movable = true;
            Weight = 5.0;
        }

        public DudeDailyBoard(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || Deleted)
                return;

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                return;
            }

            from.CloseGump(typeof(DudeDailyGump));
            from.SendGump(new DudeDailyGump(from));
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Daily Dude training quests");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            Movable = true;
        }
    }
}
