using System;
namespace Server.Items
{
    /// <summary>
    /// Retired stub. Old world items deserialize then delete themselves.
    /// Link/unlink now lives on DudeBall context menu via DudeLinkSystem.
    /// </summary>
    public class LinkingDevice : Item
    {
        [Constructable]
        public LinkingDevice()
            : base(0x2F58)
        {
            Name = "Linking Device (retired)";
            Weight = 1.0;
            Hue = 0x48D;
            LootType = LootType.Blessed;
        }

        public LinkingDevice(Serial serial)
            : base(serial)
        {
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from != null)
                from.SendMessage("The Linking Device has been retired. Use a Dude Ball's context menu to Link or Unlink.");
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version

            // Write empty legacy payload so format stays readable if somehow re-saved before delete.
            writer.Write(false); // m_IsLinked
            writer.Write((int)Serial.MinusOne); // m_LinkedBallSerial
            writer.Write(false); // m_FollowersHeld
            writer.Write(false); // m_HasBackup
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            reader.ReadBool(); // was m_IsLinked
            reader.ReadInt(); // linked ball serial
            reader.ReadBool(); // followers held

            bool hasBackup = reader.ReadBool();
            if (hasBackup)
            {
                reader.ReadInt(); // body
                reader.ReadInt(); // bodyMod
                reader.ReadInt(); // hue
                reader.ReadInt(); // hueMod
                reader.ReadInt(); // str
                reader.ReadInt(); // dex
                reader.ReadInt(); // int
                reader.ReadInt(); // hits
                reader.ReadInt(); // stam
                reader.ReadInt(); // mana

                int len = reader.ReadInt();
                for (int i = 0; i < len; i++)
                    reader.ReadDouble();
            }

            // Leftover devices vanish after load. Mid-link revert is owned by DudeLinkState
            // on the player (login/logout/disconnect); device no longer holds backup.
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), () =>
            {
                if (!Deleted)
                    Delete();
            });
        }
    }
}
