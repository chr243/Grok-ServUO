using System;
using Server.Custom.Dudes;
using Server.Mobiles;

namespace Server.Items
{
    /// <summary>
    /// Wearable gear for DudeCreature paperdoll slots. Grants AbilityId while equipped.
    /// Layers: Helm, InnerTorso, Bracelet, Talisman (Pants reserved for type shorts);
    /// Layer.TwoHanded also accepted for universal Dude Shield.
    /// Optional RequiredType locks gear to Fire/Water/Earth/Air Dudes.
    /// Leave RequiredType unset so HasRequiredType stays false (universal gear).
    /// </summary>
    public class DudeGear : Item
    {
        private string m_AbilityId;
        private int m_SlotCost = 1;
        private DudeType m_RequiredType;
        private bool m_HasRequiredType;

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

        /// <summary>When true, only Dudes whose type matches RequiredType may equip this.</summary>
        [CommandProperty(AccessLevel.GameMaster)]
        public bool HasRequiredType
        {
            get { return m_HasRequiredType; }
            set { m_HasRequiredType = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DudeType RequiredType
        {
            get { return m_RequiredType; }
            set
            {
                m_RequiredType = value;
                m_HasRequiredType = true;
                InvalidateProperties();
            }
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

            if (m_HasRequiredType)
                list.Add("{0} Dude gear", m_RequiredType);

            if (!string.IsNullOrEmpty(m_AbilityId))
            {
                DudeAbility ability = DudeAbilityRegistry.Get(m_AbilityId);
                if (ability != null && !string.IsNullOrEmpty(ability.Name))
                    list.Add("Ability: {0}", ability.Name);
                else
                    list.Add("Ability: {0}", m_AbilityId);
            }

            int cost = SlotCost;
            if (cost != 1)
                list.Add("Slot cost: {0}", cost);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            writer.Write(m_AbilityId);
            writer.Write(m_SlotCost);
            writer.Write(m_HasRequiredType);
            if (m_HasRequiredType)
                writer.Write((int)m_RequiredType);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_AbilityId = reader.ReadString();
            m_SlotCost = reader.ReadInt();
            if (m_SlotCost < 1)
                m_SlotCost = 1;

            if (version >= 1)
            {
                m_HasRequiredType = reader.ReadBool();
                if (m_HasRequiredType)
                    m_RequiredType = (DudeType)reader.ReadInt();
            }
        }
    }

    /// <summary>Fire sash: InnerTorso granting blast.</summary>
    public class EmberSash : DudeGear
    {
        [Constructable]
        public EmberSash()
            : base(0x1541)
        {
            Name = "Ember Sash";
            Hue = 1161;
            Layer = Layer.InnerTorso;
            AbilityId = "blast";
            SlotCost = 1;
            RequiredType = DudeType.Fire;
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
            Hue = 1161;
            Layer = Layer.InnerTorso;
            if (string.IsNullOrEmpty(AbilityId))
                AbilityId = "blast";
            RequiredType = DudeType.Fire;
        }
    }
}
