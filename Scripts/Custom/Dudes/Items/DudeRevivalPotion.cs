using System;
using Server.Custom.Dudes;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Base revival potion. Double-click, then target a fainted Dude Ball.
    /// Tiered by MaxReviveStage (evolution stage limit).
    /// </summary>
    public abstract class BaseDudeRevivalPotion : Item
    {
        public abstract int MaxReviveStage { get; }

        public BaseDudeRevivalPotion(int itemID)
            : base(itemID)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = 1;
        }

        public BaseDudeRevivalPotion(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Revives a fainted Dude Ball (stage {0} or lower).", MaxReviveStage);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || Deleted)
                return;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            from.SendMessage("Target a Dude Ball containing a fainted Dude.");
            from.Target = new ReviveTarget(this);
        }

        public bool TryRevive(Mobile from, DudeBall ball)
        {
            if (from == null || Deleted || ball == null || ball.Deleted)
                return false;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return false;
            }

            if (!ball.IsChildOf(from.Backpack) && ball.RootParent != from)
            {
                from.SendMessage("That Dude Ball must be in your pack.");
                return false;
            }

            if (!ball.HasDude || ball.StoredDude == null)
            {
                from.SendMessage("That Dude Ball is empty.");
                return false;
            }

            if (!ball.StoredDude.IsFainted)
            {
                from.SendMessage("{0} is not fainted.", ball.StoredDude.DisplayName);
                return false;
            }

            if (ball.IsSummoned)
            {
                from.SendMessage("Recall the Dude before using a revival potion.");
                return false;
            }

            DudeData data = ball.StoredDude;
            if (data.EvolutionStage > MaxReviveStage)
            {
                from.SendMessage("This potion is too weak to revive a stage {0} Dude.", data.EvolutionStage);
                return false; // do not Consume
            }

            data.IsFainted = false;
            data.Hits = data.HitsMax;
            ball.InvalidateProperties();

            from.SendMessage(0x59, "{0} has been revived!", data.DisplayName);
            from.PlaySound(0x214);
            from.FixedEffect(0x376A, 10, 16);

            Consume();
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
            int version = reader.ReadInt();
            Stackable = true;
        }

        private class ReviveTarget : Target
        {
            private readonly BaseDudeRevivalPotion m_Potion;

            public ReviveTarget(BaseDudeRevivalPotion potion)
                : base(8, false, TargetFlags.None)
            {
                m_Potion = potion;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Potion == null || m_Potion.Deleted)
                    return;

                DudeBall ball = targeted as DudeBall;
                if (ball == null)
                {
                    from.SendMessage("That is not a Dude Ball.");
                    return;
                }

                m_Potion.TryRevive(from, ball);
            }
        }
    }

    public class WeakDudeRevivalPotion : BaseDudeRevivalPotion
    {
        public override int MaxReviveStage { get { return 1; } }

        [Constructable]
        public WeakDudeRevivalPotion()
            : this(1)
        {
        }

        [Constructable]
        public WeakDudeRevivalPotion(int amount)
            : base(0xF0B)
        {
            Name = "Weak Dude Revival Potion";
            Hue = 0x59; // lighter green
            Stackable = true;
            Amount = amount > 0 ? amount : 1;
        }

        public WeakDudeRevivalPotion(Serial serial)
            : base(serial)
        {
        }
    }

    /// <summary>
    /// Normal revival (stage 2). Type name kept for dispensers / world items.
    /// Serialize uses Base only so existing packed potions still load.
    /// </summary>
    public class DudeRevivalPotion : BaseDudeRevivalPotion
    {
        public override int MaxReviveStage { get { return 2; } }

        [Constructable]
        public DudeRevivalPotion()
            : this(1)
        {
        }

        [Constructable]
        public DudeRevivalPotion(int amount)
            : base(0xF0B)
        {
            Name = "Dude Revival Potion";
            Hue = 0x48E; // soft green revive tint
            Stackable = true;
            Amount = amount > 0 ? amount : 1;
        }

        public DudeRevivalPotion(Serial serial)
            : base(serial)
        {
        }
    }

    public class StrongDudeRevivalPotion : BaseDudeRevivalPotion
    {
        public override int MaxReviveStage { get { return 3; } }

        [Constructable]
        public StrongDudeRevivalPotion()
            : this(1)
        {
        }

        [Constructable]
        public StrongDudeRevivalPotion(int amount)
            : base(0xF0B)
        {
            Name = "Strong Dude Revival Potion";
            Hue = 0x48A; // deeper green
            Stackable = true;
            Amount = amount > 0 ? amount : 1;
        }

        public StrongDudeRevivalPotion(Serial serial)
            : base(serial)
        {
        }
    }
}
