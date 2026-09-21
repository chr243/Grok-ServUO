using System;
using Server.Custom.Dudes;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Converts a filled Dude Ball into DudeDust and empties the ball.
    /// Must be placed inside a house and secured; anyone may use it once secured.
    /// </summary>
    public class DudeMixer : Item, ISecurable
    {
        private const int MixerItemId = 0x974; // classic cauldron
        private SecureLevel m_SecureLevel = SecureLevel.Anyone;

        [Constructable]
        public DudeMixer()
            : base(MixerItemId)
        {
            Name = "Dude Mixer";
            Weight = 5.0;
            Hue = 0;
            LootType = LootType.Blessed;
            Movable = true;
        }

        public DudeMixer(Serial serial)
            : base(serial)
        {
        }

        /// <summary>House secure access — Anyone so any visitor can recycle once secured.</summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public SecureLevel Level
        {
            get { return m_SecureLevel; }
            set { m_SecureLevel = value; InvalidateProperties(); }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Converts a filled Dude Ball into Dude Dust");
            list.Add("Anyone may use when secured in a house");

            BaseHouse house = BaseHouse.FindHouseAt(this);
            if (house == null || !house.IsInside(this))
                list.Add("Requires: house placement");
            else if (!IsSecure)
                list.Add("Requires: secure (house)");
            else
                list.Add("Secured in house");
        }

        public override bool OnDroppedToWorld(Mobile from, Point3D p)
        {
            if (!base.OnDroppedToWorld(from, p))
                return false;

            if (from == null || from.Map == null)
                return false;

            BaseHouse house = BaseHouse.FindHouseAt(p, from.Map, ItemData.Height);
            if (house == null || !house.IsInside(p, ItemData.Height))
            {
                from.SendMessage("A Dude Mixer can only be placed inside a house.");
                return false;
            }

            if (from.AccessLevel < AccessLevel.GameMaster && !house.IsCoOwner(from))
            {
                from.SendMessage("You must be a house owner or co-owner to place a Dude Mixer.");
                return false;
            }

            from.SendMessage("Dude Mixer placed. Secure it in the house — then anyone can use it.");
            return true;
        }

        /// <summary>Must sit inside a house and be secured. No owner check — anyone can use.</summary>
        public bool ValidateHouseSecureUse(Mobile from, bool message)
        {
            if (Deleted)
                return false;

            if (from != null && from.AccessLevel >= AccessLevel.GameMaster)
                return true;

            // Not usable from backpack / bank / other containers.
            if (RootParent is Mobile || Parent is Container)
            {
                if (message && from != null)
                    from.SendMessage("The Dude Mixer must be secured in a house to use.");
                return false;
            }

            BaseHouse house = BaseHouse.FindHouseAt(this);
            if (house == null || !house.IsInside(this))
            {
                if (message && from != null)
                    from.SendMessage("The Dude Mixer must be placed inside a house.");
                return false;
            }

            if (!IsSecure)
            {
                if (message && from != null)
                    from.SendMessage("The Dude Mixer must be secured in the house first.");
                return false;
            }

            return true;
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            if (!from.InRange(GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            if (!ValidateHouseSecureUse(from, true))
                return;

            from.SendMessage("Select a filled Dude Ball to recycle into Dude Dust.");
            from.Target = new MixerTarget(this);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (!ValidateHouseSecureUse(from, true))
                return false;

            DudeBall ball = dropped as DudeBall;

            if (ball == null)
            {
                from.SendMessage("The Dude Mixer only accepts Dude Balls.");
                return false;
            }

            TryBeginMix(from, ball);
            return false;
        }

        public bool TryBeginMix(Mobile from, DudeBall ball)
        {
            if (from == null || ball == null || ball.Deleted || Deleted)
                return false;

            if (!ValidateHouseSecureUse(from, true))
                return false;

            string error;
            if (!CanMix(from, ball, out error))
            {
                from.SendMessage(error);
                return false;
            }

            string name = ball.StoredDude.DisplayName;
            int level = ball.StoredDude.Level;
            int dust = DudeDustFormula.Calculate(ball.StoredDude);

            string content = string.Format(
                "Recycle <B>{0}</B> (Level {1}) into <B>{2}</B> Dude Dust?<br><br>This permanently destroys the Dude. The empty Dude Ball will be kept.",
                name, level, dust);

            from.SendGump(new WarningGump(
                1060635, // Warning!
                30720,
                content,
                0xFFC000,
                420,
                280,
                new WarningGumpCallback(MixConfirm_Callback),
                new MixState(this, ball)));

            return true;
        }

        public static bool CanMix(Mobile from, DudeBall ball, out string error)
        {
            error = null;

            if (from == null || ball == null || ball.Deleted)
            {
                error = "Invalid Dude Ball.";
                return false;
            }

            if (!ball.HasDude || ball.StoredDude == null)
            {
                error = "That Dude Ball is empty.";
                return false;
            }

            if (ball.IsAssignedToJob)
            {
                error = "That Dude is assigned to a Job Station and cannot be mixed.";
                return false;
            }

            if (ball.IsSummoned)
            {
                error = "Recall the Dude before recycling it.";
                return false;
            }

            if (ball.Parent is DudeJobStation)
            {
                error = "Remove the Dude Ball from the Job Station first.";
                return false;
            }

            return true;
        }

        private static void MixConfirm_Callback(Mobile from, bool okay, object state)
        {
            MixState mix = state as MixState;
            if (mix == null)
                return;

            if (!okay)
            {
                from.SendMessage("You decide not to recycle the Dude.");
                return;
            }

            DudeMixer mixer = mix.Mixer;
            DudeBall ball = mix.Ball;

            if (mixer == null || mixer.Deleted || ball == null || ball.Deleted)
            {
                from.SendMessage("The mixer or ball is no longer available.");
                return;
            }

            if (!mixer.ValidateHouseSecureUse(from, true))
                return;

            if (!from.InRange(mixer.GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(500446);
                return;
            }

            string error;
            if (!CanMix(from, ball, out error))
            {
                from.SendMessage(error);
                return;
            }

            PerformMix(from, ball);
        }

        public static void PerformMix(Mobile from, DudeBall ball)
        {
            if (from == null || ball == null || !ball.HasDude)
                return;

            DudeData data = ball.StoredDude;
            string name = data.DisplayName;
            int amount = DudeDustFormula.Calculate(data);

            ball.ClearDude();

            DudeDust dust = new DudeDust(amount);

            if (from.Backpack != null && from.Backpack.TryDropItem(from, dust, false))
            {
                from.SendMessage(0x59, "You recycle {0} into {1} Dude Dust. The Dude Ball is empty.", name, amount);
            }
            else
            {
                dust.MoveToWorld(from.Location, from.Map);
                from.SendMessage(0x59, "You recycle {0} into {1} Dude Dust (dropped at your feet).", name, amount);
            }

            from.PlaySound(0x240);
            Effects.SendLocationParticles(
                new Entity(Serial.Zero, from.Location, from.Map),
                0x375A, 10, 15, 5020);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            writer.Write((int)m_SecureLevel);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            if (version >= 1)
                m_SecureLevel = (SecureLevel)reader.ReadInt();
            else
                m_SecureLevel = SecureLevel.Anyone;

            // Migrate older mortar & pestle art to cauldron.
            if (ItemID == 0xE27)
            {
                ItemID = MixerItemId;
                Hue = 0;
                Weight = 5.0;
            }
        }

        private class MixerTarget : Target
        {
            private readonly DudeMixer m_Mixer;

            public MixerTarget(DudeMixer mixer)
                : base(8, false, TargetFlags.None)
            {
                m_Mixer = mixer;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Mixer == null || m_Mixer.Deleted)
                    return;

                if (!m_Mixer.ValidateHouseSecureUse(from, true))
                    return;

                DudeBall ball = targeted as DudeBall;
                if (ball == null)
                {
                    from.SendMessage("That is not a Dude Ball.");
                    return;
                }

                m_Mixer.TryBeginMix(from, ball);
            }
        }

        private class MixState
        {
            public readonly DudeMixer Mixer;
            public readonly DudeBall Ball;

            public MixState(DudeMixer mixer, DudeBall ball)
            {
                Mixer = mixer;
                Ball = ball;
            }
        }
    }
}
