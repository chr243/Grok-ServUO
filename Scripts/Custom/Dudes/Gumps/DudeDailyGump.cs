using Server.Custom.Dudes;
using Server.Network;

namespace Server.Gumps
{
    /// <summary>
    /// Small Daily Training board gump (DudeInfoGump style).
    /// </summary>
    public class DudeDailyGump : Gump
    {
        private const int LabelHue = 0x480;
        private const int ValueHue = 0x34;

        public DudeDailyGump(Mobile from)
            : base(50, 50)
        {
            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            DudeDailyRecord rec = DudeDailySystem.EnsureForBoard(from);

            AddPage(0);
            AddBackground(0, 0, 380, 340, 9270);
            AddAlphaRegion(10, 10, 360, 320);

            AddHtml(20, 16, 340, 22, "<CENTER><BASEFONT COLOR=#FFFFFF>Daily Training</BASEFONT></CENTER>", false, false);

            int y = 48;
            if (rec == null || rec.Quests == null)
            {
                AddLabel(24, y, LabelHue, "No daily quests available.");
            }
            else
            {
                for (int i = 0; i < rec.Quests.Length && i < 3; i++)
                {
                    DudeDailyQuest q = rec.Quests[i];
                    if (q == null)
                        continue;

                    AddLabel(24, y, LabelHue, Truncate(q.Title, 42));
                    y += 20;

                    string progress = string.Format("{0} / {1}", q.Current, q.Required);
                    AddLabel(24, y, ValueHue, progress);

                    if (q.IsComplete && !q.Claimed)
                    {
                        AddButton(280, y - 2, 4005, 4007, i + 1, GumpButtonType.Reply, 0);
                        AddLabel(315, y, 0x35, "Claim");
                    }
                    else if (q.Claimed)
                    {
                        AddLabel(280, y, LabelHue, "Claimed");
                    }

                    y += 36;
                }
            }

            AddButton(340, 300, 4017, 4019, 0, GumpButtonType.Reply, 0);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            if (info == null || sender == null || sender.Mobile == null)
                return;

            Mobile from = sender.Mobile;
            int id = info.ButtonID;

            if (id >= 1 && id <= 3)
            {
                DudeDailySystem.TryClaim(from, id - 1);
                from.SendGump(new DudeDailyGump(from));
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
