using System;

namespace Server.Items
{
    public class TrainersDiaryPart1 : RedBook
    {
        public static readonly BookContent Content = new BookContent(
            "Trainer's Diary, Part 1", "A New Trainer",
            new BookPageInfo(
                "Day 1.",
                "Dear diary!",
                "",
                "What an amazing",
                "thing happened",
                "today. I picked",
                "up one of those",
                "Dude Balls and"),
            new BookPageInfo(
                "threw it at a",
                "creature - and",
                "you'll never",
                "guess what!",
                "",
                "I caught it! We're",
                "going on adventures",
                "now. It's"),
            new BookPageInfo(
                "spectacular. I",
                "named him Pebble.",
                "",
                "I can't wait to",
                "see what tomorrow",
                "brings!"));

        public override BookContent DefaultContent
        {
            get { return Content; }
        }

        [Constructable]
        public TrainersDiaryPart1()
            : base(false)
        {
            Name = "Trainer's Diary, Part 1";
            Hue = 0x47E;
            LootType = LootType.Blessed;
            Writable = false;
        }

        public TrainersDiaryPart1(Serial serial)
            : base(serial)
        {
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
        }
    }
}
