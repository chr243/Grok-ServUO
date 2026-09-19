using System;
using Server.Mobiles;

namespace Server.Items
{
    /// <summary>
    /// Wearable gear for DudeCreature paperdoll slots. Grants AbilityId while equipped.
    /// Layers: Helm, InnerTorso, Bracelet, Talisman (Pants reserved for type shorts).
    /// </summary>
    public class DudeGear : Item
    {
        private string m_AbilityId;
        private int m_SlotCost = 1;

        [Constructable]
        public DudeGear()
            : this(0x1541)
        {
        }

        [Constructable]
        public DudeGear(int itemID)
            : base(itemID)
        {
            Weight = 1.0;
            m_SlotCost = 1;
        }

        public DudeGear(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string AbilityId
        {
            get { return m_AbilityId; }
            set { m_AbilityId = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int SlotCost
        {
            get { return m_SlotCost < 1 ? 1 : m_SlotCost; }
            set { m_SlotCost = value < 1 ? 1 : value; InvalidateProperties(); }
        }

        public override bool CanEquip(Mobile from)
        {
            DudeCreature dude = from as DudeCreature;
            if (dude == null)
            {
                if (from != null)
                    from.SendMessage("That can only be worn by a Dude.");
                return false;
            }

            string reason;
            if (!dude.CanAcceptGear(this, out reason))
            {
                Mobile notify = dude.ControlMaster;
                if (notify == null || notify.Deleted)
                    notify = from;
                if (notify != null && !string.IsNullOrEmpty(reason))
                    notify.SendMessage(reason);
                return false;
            }

            return base.CanEquip(from);
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (!string.IsNullOrEmpty(m_AbilityId))
                list.Add("Ability: {0}", m_AbilityId);

            int cost = SlotCost;
            if (cost != 1)
                list.Add("Slot cost: {0}", cost);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version

            writer.Write(m_AbilityId);
            writer.Write(m_SlotCost);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_AbilityId = reader.ReadString();
            m_SlotCost = reader.ReadInt();
            if (m_SlotCost < 1)
                m_SlotCost = 1;
        }
    }

    /// <summary>Starter test gear: InnerTorso sash granting blast.</summary>
    public class EmberSash : DudeGear
    {
        [Constructable]
        public EmberSash()
            : base(0x1541)
        {
            Name = "Ember Sash";
            Hue = 0x21; // FireHue
            Layer = Layer.InnerTorso;
            AbilityId = "blast";
            SlotCost = 1;
        }

        public EmberSash(Serial serial)
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

            Name = "Ember Sash";
            Layer = Layer.InnerTorso;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "blast";
        }
    }
}
