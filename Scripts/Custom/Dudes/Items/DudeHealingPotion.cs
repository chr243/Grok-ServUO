using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Base throwable Dude heal. Dclick → target Dude → potion flies, then heals.
    /// Shared 10s per-player cooldown with [healdude].
    /// </summary>
    public abstract class BaseDudeHealingPotion : Item
    {
        public static readonly TimeSpan HealCooldown = TimeSpan.FromSeconds(10.0);

        private static readonly Dictionary<Mobile, DateTime> m_NextHealAllowed = new Dictionary<Mobile, DateTime>();

        public abstract double HealFraction { get; }
        public abstract string PotionLabel { get; }

        public BaseDudeHealingPotion(int itemID)
            : base(itemID)
        {
            Weight = 1.0;
            Stackable = true;
            Amount = 1;
        }

        public BaseDudeHealingPotion(Serial serial)
            : base(serial)
        {
        }

        public static bool CheckHealCooldown(Mobile from, bool message)
        {
            if (from == null)
                return false;

            DateTime next;
            if (m_NextHealAllowed.TryGetValue(from, out next) && DateTime.UtcNow < next)
            {
                if (message)
                {
                    double secs = (next - DateTime.UtcNow).TotalSeconds;
                    if (secs < 0.1)
                        secs = 0.1;
                    from.SendMessage("You must wait {0:0.#} more seconds before healing a Dude.", secs);
                }
                return false;
            }

            return true;
        }

        public static void MarkHealCooldown(Mobile from)
        {
            if (from == null)
                return;

            m_NextHealAllowed[from] = DateTime.UtcNow + HealCooldown;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            int pct = (int)(HealFraction * 100.0 + 0.5);
            list.Add("Double-click and target a summoned Dude in range 8. Heals {0}%.", pct);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || Deleted)
                return;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            if (!CheckHealCooldown(from, true))
                return;

            from.SendMessage("Target a summoned Dude to heal.");
            from.Target = new ThrowHealTarget(this);
        }

        public bool CanHeal(Mobile from, DudeCreature dude, bool message)
        {
            if (from == null || Deleted || dude == null || dude.Deleted)
                return false;

            if (!IsChildOf(from.Backpack) && Map != Map.Internal)
            {
                if (message)
                    from.SendLocalizedMessage(1042001);
                return false;
            }

            if (dude.IsWild)
            {
                if (message)
                    from.SendMessage("That Dude is wild.");
                return false;
            }

            if (dude.BoundBall == null || dude.BoundBall.Deleted)
            {
                if (message)
                    from.SendMessage("That Dude is not bound to a Dude Ball.");
                return false;
            }

            if (dude.ControlMaster != from && from.AccessLevel < AccessLevel.GameMaster)
            {
                if (message)
                    from.SendMessage("That is not your Dude.");
                return false;
            }

            if (dude.Map == null || dude.Map == Map.Internal)
            {
                if (message)
                    from.SendMessage("That Dude is not summoned.");
                return false;
            }

            if (!from.InRange(dude, 8) || !from.CanSee(dude) || !from.InLOS(dude))
            {
                if (message)
                    from.SendMessage("You cannot reach that Dude.");
                return false;
            }

            if (dude.Hits >= dude.HitsMax)
            {
                if (message)
                    from.SendMessage("{0} is already at full health.", dude.Name);
                return false;
            }

            return true;
        }

        public void BeginThrow(Mobile from, DudeCreature dude)
        {
            if (!CanHeal(from, dude, true))
                return;

            if (!CheckHealCooldown(from, true))
                return;

            MarkHealCooldown(from);

            from.RevealingAction();
            Effects.SendMovingEffect(from, dude, ItemID, 7, 0, false, false, Hue, 0);

            // Consume one from the stack immediately so it can't be reused mid-flight.
            // If Amount>1, decrement; otherwise internalize the single bottle for FinishHeal to Delete.
            bool consumedFromStack = false;
            if (Amount > 1)
            {
                Amount--;
                consumedFromStack = true;
            }
            else
            {
                Internalize();
            }

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), () => FinishHeal(from, dude, consumedFromStack));
        }

        private void FinishHeal(Mobile from, DudeCreature dude, bool consumedFromStack)
        {
            // When consumedFromStack, `this` is the remaining stack in the pack — never Delete it.
            // When !consumedFromStack, `this` is the internalized single bottle — Delete after use.
            if (!consumedFromStack && Deleted)
                return;

            if (from == null || from.Deleted || dude == null || dude.Deleted || !dude.Alive)
            {
                if (!consumedFromStack)
                    Delete();
                return;
            }

            // Re-validate loosely after flight (range can drift a little).
            if (dude.IsWild || dude.BoundBall == null || dude.BoundBall.Deleted)
            {
                if (from != null)
                    from.SendMessage("The potion fails to take effect.");
                if (!consumedFromStack)
                    Delete();
                return;
            }

            if (dude.ControlMaster != from && from.AccessLevel < AccessLevel.GameMaster)
            {
                if (!consumedFromStack)
                    Delete();
                return;
            }

            if (dude.Hits >= dude.HitsMax)
            {
                from.SendMessage("{0} is already at full health.", dude.Name);
                if (!consumedFromStack)
                    Delete();
                return;
            }

            int heal = Math.Max(1, (int)(dude.HitsMax * HealFraction));
            int before = dude.Hits;
            dude.Hits = Math.Min(dude.HitsMax, dude.Hits + heal);
            int actual = dude.Hits - before;

            if (dude.BoundBall.StoredDude != null)
            {
                dude.BoundBall.StoredDude.Hits = dude.Hits;
                dude.BoundBall.InvalidateProperties();
            }

            from.SendMessage(0x59, "You heal {0} for {1} hit points.", dude.Name, actual);
            dude.PlaySound(0x1F2);
            dude.FixedEffect(0x376A, 9, 32);
            Effects.SendLocationParticles(
                EffectItem.Create(dude.Location, dude.Map, EffectItem.DefaultDuration),
                0x3728, 10, 10, 5029);

            if (!consumedFromStack)
                Delete();
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

        private class ThrowHealTarget : Target
        {
            private readonly BaseDudeHealingPotion m_Potion;

            public ThrowHealTarget(BaseDudeHealingPotion potion)
                : base(8, false, TargetFlags.Beneficial)
            {
                m_Potion = potion;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Potion == null || m_Potion.Deleted)
                    return;

                DudeCreature dude = targeted as DudeCreature;
                if (dude == null)
                {
                    from.SendMessage("That is not a summoned Dude.");
                    return;
                }

                m_Potion.BeginThrow(from, dude);
            }
        }
    }

    public class DudeHealingPotion : BaseDudeHealingPotion
    {
        public override double HealFraction { get { return 0.20; } }
        public override string PotionLabel { get { return "Dude Healing Potion"; } }

        [Constructable]
        public DudeHealingPotion()
            : this(1)
        {
        }

        [Constructable]
        public DudeHealingPotion(int amount)
            : base(0xF0C)
        {
            Name = "Dude Healing Potion";
            Hue = 0x21;
            Stackable = true;
            Amount = amount > 0 ? amount : 1;
        }

        public DudeHealingPotion(Serial serial)
            : base(serial)
        {
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

    public class GreaterDudeHealingPotion : BaseDudeHealingPotion
    {
        public override double HealFraction { get { return 0.50; } }
        public override string PotionLabel { get { return "Greater Dude Healing Potion"; } }

        [Constructable]
        public GreaterDudeHealingPotion()
            : this(1)
        {
        }

        [Constructable]
        public GreaterDudeHealingPotion(int amount)
            : base(0xF0B)
        {
            Name = "Greater Dude Healing Potion";
            Hue = 0x26; // deeper red
            Stackable = true;
            Amount = amount > 0 ? amount : 1;
        }

        public GreaterDudeHealingPotion(Serial serial)
            : base(serial)
        {
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
}
