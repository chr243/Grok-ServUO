using System;
using Server.Custom.Dudes;
using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    /// <summary>
    /// One-shot starter gift. Double-click to pick a beginner Dude; consumes into a filled Dude Ball.
    /// </summary>
    public class BeginnersBall : Item
    {
        public static readonly string[] StarterIds = new string[]
        {
            "fire",
            "water",
            "earth",
            "air"
        };

        private const int BallItemId = 0xE73; // match Dude Ball graphic
        private const int GiftHue = 0x35; // gold — distinct from empty/filled Dude Balls

        [Constructable]
        public BeginnersBall()
            : base(BallItemId)
        {
            Name = "Beginner's Ball";
            Weight = 1.0;
            Hue = GiftHue;
            LootType = LootType.Blessed;
        }

        public BeginnersBall(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Double-click to choose your first Dude");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || from.Deleted)
                return;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack to use it.
                return;
            }

            from.CloseGump(typeof(BeginnersBallGump));
            from.SendGump(new BeginnersBallGump(this));
        }

        public bool TryClaim(Mobile from, string definitionId)
        {
            if (from == null || from.Deleted || Deleted)
                return false;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return false;
            }

            DudeDefinition def = DudeRegistry.Get(definitionId);
            if (def == null)
            {
                from.SendMessage("That Dude is not available.");
                return false;
            }

            bool allowed = false;
            for (int i = 0; i < StarterIds.Length; i++)
            {
                if (string.Equals(StarterIds[i], definitionId, StringComparison.OrdinalIgnoreCase))
                {
                    allowed = true;
                    break;
                }
            }

            if (!allowed)
            {
                from.SendMessage("That Dude is not a beginner choice.");
                return false;
            }

            DudeData data = DudeData.FromDefinition(def, from);
            if (data == null)
                return false;

            DudeBall ball = new DudeBall();
            ball.StoreDude(data);

            if (!from.PlaceInBackpack(ball))
            {
                ball.Delete();
                from.SendMessage("Your backpack is full.");
                return false;
            }

            from.SendMessage(0x59, "You received {0} in a Dude Ball! Double-click the ball to summon it.", data.DisplayName);
            Delete();
            return true;
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

    public class BeginnersBallGump : Gump
    {
        private readonly BeginnersBall m_Ball;

        public BeginnersBallGump(BeginnersBall ball)
            : base(50, 50)
        {
            m_Ball = ball;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            AddPage(0);
            AddBackground(0, 0, 320, 220, 9270);
            AddAlphaRegion(10, 10, 300, 200);

            AddHtml(20, 18, 280, 20, "<CENTER><BASEFONT COLOR=#FFFFFF>Choose Your First Dude</BASEFONT></CENTER>", false, false);

            int y = 48;
            int labelHue = 0x480;

            string[] ids = BeginnersBall.StarterIds;
            for (int i = 0; i < ids.Length; i++)
            {
                DudeDefinition def = DudeRegistry.Get(ids[i]);
                string label = def != null
                    ? string.Format("{0} ({1})", def.Name, def.Type)
                    : ids[i];

                AddButton(24, y, 4005, 4007, i + 1, GumpButtonType.Reply, 0);
                AddLabel(60, y, labelHue, Truncate(label, 32));
                y += 28;
            }

            AddLabel(24, y + 4, 0x34, "One choice. Ball becomes a Dude Ball.");
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            if (from == null || m_Ball == null || m_Ball.Deleted)
                return;

            int idx = info.ButtonID - 1;
            if (idx < 0 || idx >= BeginnersBall.StarterIds.Length)
                return;

            m_Ball.TryClaim(from, BeginnersBall.StarterIds[idx]);
        }

        private static string Truncate(string text, int max)
        {
            if (string.IsNullOrEmpty(text))
                return "-";
            if (text.Length <= max)
                return text;
            return text.Substring(0, max - 1) + "...";
        }
    }
}
