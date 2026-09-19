using System;
using System.Collections.Generic;
using Server.Gumps;
using Server.Network;

namespace Server.Items
{
    public class SevenSkillBall : Item
    {
        private bool m_Used;

        [Constructable]
        public SevenSkillBall()
            : base(0xE73)
        {
            Name = "a 7x Skill Ball";
            Hue = 0x482;
            Weight = 1.0;
            LootType = LootType.Blessed;
            Stackable = false;
            m_Used = false;
        }

        public SevenSkillBall(Serial serial)
            : base(serial)
        {
        }

        public bool Used
        {
            get { return m_Used; }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Double-click to choose exactly 7 skills");
            list.Add("Sets them to 100 and zeros all others");
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

            if (m_Used)
            {
                from.SendMessage("That skill ball has already been used.");
                return;
            }

            from.CloseGump(typeof(SevenSkillBallGump));
            from.SendGump(new SevenSkillBallGump(this));
        }

        public void Apply(Mobile from, SkillName[] picked)
        {
            if (from == null || from.Deleted || picked == null || picked.Length != 7)
                return;

            if (Deleted || m_Used)
                return;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            for (int i = 0; i < from.Skills.Length; i++)
            {
                Skill sk = from.Skills[i];

                if (sk != null)
                    sk.Base = 0.0;
            }

            string[] names = new string[7];

            for (int i = 0; i < picked.Length; i++)
            {
                Skill sk = from.Skills[picked[i]];

                if (sk == null)
                    continue;

                if (sk.Cap < 100.0)
                    sk.Cap = 100.0;

                sk.Base = 100.0;
                names[i] = sk.Name;
            }

            from.SendMessage("Skills set to 100: {0}, {1}, {2}, {3}, {4}, {5}, {6}",
                names[0], names[1], names[2], names[3], names[4], names[5], names[6]);
            from.PlaySound(0x1F7);

            m_Used = true;
            Delete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
            writer.Write(m_Used);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            m_Used = reader.ReadBool();
        }
    }

    public class SevenSkillBallGump : Gump
    {
        private static readonly SkillName[] UorSkills = new SkillName[]
        {
            SkillName.Alchemy, SkillName.Anatomy, SkillName.AnimalLore, SkillName.AnimalTaming,
            SkillName.Archery, SkillName.ArmsLore, SkillName.Begging, SkillName.Blacksmith,
            SkillName.Camping, SkillName.Carpentry, SkillName.Cartography, SkillName.Cooking,
            SkillName.DetectHidden, SkillName.Discordance, SkillName.EvalInt,
            SkillName.Fencing, SkillName.Fishing, SkillName.Forensics, SkillName.Healing,
            SkillName.Herding, SkillName.Hiding, SkillName.Inscribe, SkillName.ItemID,
            SkillName.Lockpicking, SkillName.Lumberjacking, SkillName.Macing, SkillName.Magery,
            SkillName.MagicResist, SkillName.Meditation, SkillName.Mining, SkillName.Musicianship,
            SkillName.Parry, SkillName.Peacemaking, SkillName.Poisoning, SkillName.Provocation,
            SkillName.RemoveTrap, SkillName.Snooping, SkillName.SpiritSpeak, SkillName.Stealing,
            SkillName.Stealth, SkillName.Swords, SkillName.Tactics, SkillName.Tailoring,
            SkillName.TasteID, SkillName.Tinkering, SkillName.Tracking, SkillName.Veterinary,
            SkillName.Wrestling
        };

        private readonly SevenSkillBall m_Ball;

        private static bool IsUorSkill(SkillName sn)
        {
            for (int i = 0; i < UorSkills.Length; i++)
            {
                if (UorSkills[i] == sn)
                    return true;
            }
            return false;
        }

        public SevenSkillBallGump(SevenSkillBall ball)
            : base(50, 50)
        {
            m_Ball = ball;

            Closable = true;
            Disposable = true;
            Dragable = true;
            Resizable = false;

            SkillName[] skills = UorSkills;
            int count = skills.Length;
            int rows = (count + 2) / 3;
            int height = 50 + (rows * 25) + 50;

            AddPage(0);
            AddBackground(0, 0, 650, height, 9270);
            AddAlphaRegion(10, 10, 630, height - 20);

            AddLabel(20, 20, 0x480, "Select exactly 7 skills (set to 100, others 0)");

            int[] colX = new int[] { 20, 220, 420 };
            int startY = 50;

            for (int i = 0; i < count; i++)
            {
                SkillName sn = skills[i];
                int col = i / rows;
                int row = i % rows;

                if (col > 2)
                    col = 2;

                int x = colX[col];
                int y = startY + (row * 25);
                int switchId = 100 + (int)sn;

                AddCheck(x, y, 210, 211, false, switchId);
                AddLabel(x + 30, y, 0x480, sn.ToString());
            }

            int btnY = height - 40;
            AddButton(20, btnY, 247, 248, 1, GumpButtonType.Reply, 0); // OK
            AddButton(100, btnY, 241, 242, 0, GumpButtonType.Reply, 0); // Cancel
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            Mobile from = sender.Mobile;

            if (from == null || from.Deleted)
                return;

            if (info.ButtonID == 0)
                return;

            if (info.ButtonID != 1)
                return;

            int[] switches = info.Switches;
            List<SkillName> selected = new List<SkillName>();

            if (switches != null)
            {
                for (int i = 0; i < switches.Length; i++)
                {
                    int id = switches[i] - 100;

                    if (id < 0)
                        continue;

                    SkillName sn = (SkillName)id;
                    if (!IsUorSkill(sn))
                        continue;

                    selected.Add(sn);
                }
            }

            if (selected.Count != 7)
            {
                from.SendMessage("Pick exactly 7 skills.");
                from.SendGump(new SevenSkillBallGump(m_Ball));
                return;
            }

            if (m_Ball == null || m_Ball.Deleted || m_Ball.Used)
            {
                from.SendMessage("That skill ball is no longer available.");
                return;
            }

            if (!m_Ball.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            m_Ball.Apply(from, selected.ToArray());
        }
    }
}
