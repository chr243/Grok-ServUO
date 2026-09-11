using System;
using Server.Custom.Dudes;
using Server.Gumps;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Converts a filled Dude Ball into DudeDust and empties the ball.
    /// Accepts filled balls only; confirms before destroying the Dude.
    /// </summary>
    public class DudeMixer : Item
    {
        [Constructable]
        public DudeMixer()
            : base(0xE27) // mortar & pestle
        {
            Name = "Dude Mixer";
            Weight = 2.0;
            Hue = 1150;
            LootType = LootType.Blessed;
        }

        public DudeMixer(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Converts a filled Dude Ball into Dude Dust");
            list.Add("Double-click to select a Dude Ball");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null)
                return;

            bool inPack = IsChildOf(from.Backpack) || RootParent == from;
            bool inRange = from.InRange(GetWorldLocation(), 2);

            if (!inPack && !inRange)
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            from.SendMessage("Select a filled Dude Ball to recycle into Dude Dust.");
            from.Target = new MixerTarget(this);
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            DudeBall ball = dropped as DudeBall;

            if (ball == null)
            {
                from.SendMessage("The Dude Mixer only accepts Dude Balls.");
                return false;
            }

            // Open confirm gump; do not consume the ball until confirmed.
            TryBeginMix(from, ball);
            return false;
        }

        public bool TryBeginMix(Mobile from, DudeBall ball)
        {
            if (from == null || ball == null || ball.Deleted || Deleted)
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
                EffectItem.Create(from.Location, from.Map, EffectItem.DefaultDuration),
                0x375A, 10, 15, 5020);
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
