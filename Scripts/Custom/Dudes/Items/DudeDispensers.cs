using System;
using Server.Network;

namespace Server.Items
{
    /// <summary>
    /// GM-placeable gravestone dispenser. Lock it down (or leave immovable); players dclick to receive an item.
    /// </summary>
    [Flipable(0x1173, 0x1174)]
    public abstract class BaseDudeDispenser : Item
    {
        public abstract string DispenserLabel { get; }
        public abstract Item CreateDispensedItem();

        public BaseDudeDispenser(int hue)
            : base(0x1173) // ancestral gravestone graphic
        {
            Hue = hue;
            Weight = 10.0;
            Movable = true; // GM places, then [lock] / lock down
        }

        public override void OnAfterDuped(Item newItem)
        {
            base.OnAfterDuped(newItem);
            BaseDudeDispenser d = newItem as BaseDudeDispenser;
            if (d != null)
                d.Name = d.DispenserLabel;
        }

        public BaseDudeDispenser(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Double-click to take one.");
            if (Movable && !IsLockedDown && !IsSecure)
                list.Add("GM: place and lock down for players to use.");
        }

        public override void OnSingleClick(Mobile from)
        {
            LabelTo(from, Name);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || Deleted)
                return;

            if (!from.InRange(GetWorldLocation(), 2) || !from.InLOS(this))
            {
                from.SendLocalizedMessage(500446); // That is too far away.
                return;
            }

            // Players may use only when locked/secured or when the stone is fixed in the world.
            if (from.AccessLevel < AccessLevel.GameMaster)
            {
                if (RootParent != null)
                {
                    from.SendMessage("That dispenser must be placed in the world.");
                    return;
                }

                if (Movable && !IsLockedDown && !IsSecure)
                {
                    from.SendMessage("That dispenser must be locked down before it can be used.");
                    return;
                }
            }

            if (from.Backpack == null)
                return;

            Item item = CreateDispensedItem();
            if (item == null)
            {
                from.SendMessage("The dispenser is empty.");
                return;
            }

            if (!from.Backpack.TryDropItem(from, item, false))
            {
                item.Delete();
                from.SendLocalizedMessage(500720); // You don't have enough room in your backpack!
                return;
            }

            from.SendMessage(0x59, "You take {0} from the {1}.", item.Name, Name);
            from.PlaySound(0x57);
            Effects.SendLocationParticles(
                EffectItem.Create(Location, Map, EffectItem.DefaultDuration),
                0x376A, 9, 32, 5029);
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
            Name = DispenserLabel;
        }
    }

    [Flipable(0x1173, 0x1174)]
    public class DudeBallDispenser : BaseDudeDispenser
    {
        public override string DispenserLabel { get { return "Dude Ball Dispenser"; } }

        [Constructable]
        public DudeBallDispenser()
            : base(0x59) // green tint like empty balls
        {
            Name = DispenserLabel;
        }

        public DudeBallDispenser(Serial serial)
            : base(serial)
        {
        }

        public override Item CreateDispensedItem()
        {
            return new DudeBall();
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

    [Flipable(0x1173, 0x1174)]
    public class DudeHealthPotDispenser : BaseDudeDispenser
    {
        public override string DispenserLabel { get { return "Dude Health Pots Dispenser"; } }

        [Constructable]
        public DudeHealthPotDispenser()
            : base(0x21) // red heal tint
        {
            Name = DispenserLabel;
        }

        public DudeHealthPotDispenser(Serial serial)
            : base(serial)
        {
        }

        public override Item CreateDispensedItem()
        {
            return new DudeHealingPotion();
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

    [Flipable(0x1173, 0x1174)]
    public class DudeRevivalPotDispenser : BaseDudeDispenser
    {
        public override string DispenserLabel { get { return "Dude Revival Pots Dispenser"; } }

        [Constructable]
        public DudeRevivalPotDispenser()
            : base(0x48E) // soft green revive tint
        {
            Name = DispenserLabel;
        }

        public DudeRevivalPotDispenser(Serial serial)
            : base(serial)
        {
        }

        public override Item CreateDispensedItem()
        {
            return new DudeRevivalPotion();
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
