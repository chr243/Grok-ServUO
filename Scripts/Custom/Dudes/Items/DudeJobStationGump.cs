using System;
using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    public class DudeJobStationGump : Gump
    {
        private readonly DudeJobStation m_Station;

        public DudeJobStationGump(Mobile from, DudeJobStation station)
            : base(50, 50)
        {
            m_Station = station;

            AddPage(0);
            AddBackground(0, 0, 320, 260, 9270);
            AddAlphaRegion(10, 10, 300, 240);

            AddHtml(20, 18, 280, 20, "<CENTER><BASEFONT COLOR=#FFFFFF>Dude Job Station</BASEFONT></CENTER>", false, false);

            int y = 48;
            int labelHue = 0x480;
            int valueHue = 0x34;

            string[] statusLines = station.GetStatusLines();
            for (int i = 0; i < statusLines.Length; i++)
            {
                AddLabel(24, y, valueHue, Truncate(statusLines[i], 40));
                y += 22;
            }

            y += 10;

            AddButton(20, y, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddLabel(55, y, labelHue, "Open Storage");
            y += 28;

            if (station.ActiveBall != null)
            {
                AddButton(20, y, 4005, 4007, 2, GumpButtonType.Reply, 0);
                AddLabel(55, y, labelHue, "Retrieve Dude Ball");
            }
            else
            {
                AddLabel(20, y, labelHue, "Drop a filled Dude Ball to start.");
            }
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;
            if (from == null || m_Station == null || m_Station.Deleted)
                return;

            if (!from.InRange(m_Station.GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446);
                return;
            }

            switch (info.ButtonID)
            {
                case 1:
                    m_Station.DisplayTo(from);
                    break;
                case 2:
                    m_Station.TryRetrieveBall(from);
                    break;
            }
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
