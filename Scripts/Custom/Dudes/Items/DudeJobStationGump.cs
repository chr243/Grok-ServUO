using System;
using Server.Custom.Dudes.Jobs;
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
            AddBackground(0, 0, 320, 250, 9270);
            AddAlphaRegion(10, 10, 300, 230);

            AddHtml(20, 20, 280, 20, "<CENTER><BASEFONT COLOR=#FFFFFF>Dude Job Station</BASEFONT></CENTER>", false, false);
            AddHtml(20, 50, 280, 70, string.Format("<BASEFONT COLOR=#FFFFFF>{0}</BASEFONT>", station.GetStatusMessage()), false, false);

            int y = 130;

            AddButton(20, y, 4005, 4007, 1, GumpButtonType.Reply, 0);
            AddLabel(55, y, 0x480, "Open Storage");
            y += 25;

            if (station.ActiveBall != null && !station.JobActive)
            {
                AddButton(20, y, 4005, 4007, 2, GumpButtonType.Reply, 0);
                AddLabel(55, y, 0x480, "Start Job");
                y += 25;

                AddButton(20, y, 4005, 4007, 3, GumpButtonType.Reply, 0);
                AddLabel(55, y, 0x480, "Retrieve Dude Ball");
            }
            else if (station.JobActive)
            {
                AddButton(20, y, 4005, 4007, 4, GumpButtonType.Reply, 0);
                AddLabel(55, y, 0x22, "Stop Job");
                y += 25;
                AddLabel(20, y, 0x480, "Looping until stop or ore cap.");
            }
            else
            {
                AddLabel(20, y, 0x480, "Drop a filled Dude Ball to assign.");
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
                    m_Station.TryStartJob(from);
                    break;
                case 3:
                    m_Station.TryRetrieveBall(from);
                    break;
                case 4:
                    m_Station.TryStopJob(from);
                    break;
            }
        }
    }
}
