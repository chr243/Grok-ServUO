using System;
using Server.Custom.Dudes;
using Server.Mobiles;

namespace Server.Items
{
    /// <summary>
    /// Wearable gear for DudeCreature paperdoll slots. Grants AbilityId while equipped.
    /// Allowed layers: Helm (hat), InnerTorso (sash), Earrings (type earrings), Bracelet (bracers);
    /// Layer.TwoHanded also accepted for universal Dude Shield; Layer.OneHanded for DudeCostume (SlotCost 0).
    /// Pants reserved for type shorts.
    /// Slot budget is SlotCost sum vs GetGearSlotCount() — not a fixed layer unlock ladder.
    /// Optional RequiredType locks gear to Fire/Water/Earth/Air Dudes.
    /// Leave RequiredType unset so HasRequiredType stays false (universal gear).
    /// </summary>
    public class DudeGear : Item
    {
        private string m_AbilityId;
        private int m_SlotCost = 1;
        private DudeType m_RequiredType;
        private bool m_HasRequiredType;
        private int m_GearLevel;
        private int m_GearEXP;

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
            Movable = true;
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
            // 0 is valid (e.g. DudeCostume) and ignored in gear slot budget.
            get { return m_SlotCost < 0 ? 0 : m_SlotCost; }
            set { m_SlotCost = value < 0 ? 0 : value; InvalidateProperties(); }
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

        [CommandProperty(AccessLevel.GameMaster)]
        public int GearLevel
        {
            get
            {
                int lv = m_GearLevel;
                if (lv < 0)
                    lv = 0;
                if (lv > 10)
                    lv = 10;
                return lv;
            }
            set
            {
                int lv = value;
                if (lv < 0)
                    lv = 0;
                if (lv > 10)
                    lv = 10;
                m_GearLevel = lv;
                if (m_GearLevel >= 10)
                    m_GearEXP = 0;
                InvalidateProperties();
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int GearEXP
        {
            get { return m_GearEXP < 0 ? 0 : m_GearEXP; }
            set
            {
                m_GearEXP = value < 0 ? 0 : value;
                InvalidateProperties();
            }
        }

        /// <summary>EXP required to go from <paramref name="level"/> to level+1.</summary>
        public static int GetExpToNext(int level)
        {
            if (level < 0)
                level = 0;
            if (level >= 10)
                return 0;
            return 1000 * (level + 1); // 1000, 2000, … 10000
        }

        /// <summary>Effect strength: level 0 = x1.0, each level +10%, level 10 = x2.0.</summary>
        public double GetEffectMultiplier()
        {
            int lv = GearLevel;
            if (lv < 0)
                lv = 0;
            if (lv > 10)
                lv = 10;
            return 1.0 + (lv * 0.10);
        }

        /// <summary>Award gear EXP from the same kill amount the Dude received (full, not split).</summary>
        public void AwardGearExp(int amount)
        {
            if (amount < 1 || GearLevel >= 10)
                return;

            int oldLevel = GearLevel;
            m_GearEXP += amount;

            while (GearLevel < 10 && m_GearEXP >= GetExpToNext(GearLevel))
            {
                m_GearEXP -= GetExpToNext(GearLevel);
                m_GearLevel++;
                InvalidateProperties();
            }

            if (GearLevel >= 10)
                m_GearEXP = 0;

            if (GearLevel > oldLevel)
            {
                Mobile wearer = Parent as Mobile;
                DudeCreature dude = wearer as DudeCreature;
                Mobile master = dude != null ? dude.ControlMaster : null;
                if (master != null && !master.Deleted)
                {
                    string n = Name;
                    if (string.IsNullOrEmpty(n))
                        n = GetType().Name;
                    master.SendMessage(0x44, "{0} reached gear level {1}.", n, GearLevel);
                }

                // Re-apply hat/shield skills etc. after level-up.
                if (dude != null && !dude.Deleted)
                    dude.RebuildGearCache();
            }
            else
            {
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

            int lv = GearLevel;
            list.Add("Level {0} / 10", lv);
            if (lv < 10)
                list.Add("EXP {0} / {1}", GearEXP, GetExpToNext(lv));
            else
                list.Add("MAX");
            list.Add("Effect +{0}%", lv * 10);

            int cost = SlotCost;
            if (cost != 1)
                list.Add("Slot cost: {0}", cost);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)2); // version

            writer.Write(m_AbilityId);
            writer.Write(m_SlotCost);
            writer.Write(m_HasRequiredType);
            if (m_HasRequiredType)
                writer.Write((int)m_RequiredType);

            writer.Write(m_GearLevel);
            writer.Write(m_GearEXP);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_AbilityId = reader.ReadString();
            m_SlotCost = reader.ReadInt();
            if (m_SlotCost < 0)
                m_SlotCost = 0;

            if (version >= 1)
            {
                m_HasRequiredType = reader.ReadBool();
                if (m_HasRequiredType)
                    m_RequiredType = (DudeType)reader.ReadInt();
            }

            if (version >= 2)
            {
                m_GearLevel = reader.ReadInt();
                m_GearEXP = reader.ReadInt();
            }
            else
            {
                m_GearLevel = 0;
                m_GearEXP = 0;
            }

            if (m_GearLevel < 0)
                m_GearLevel = 0;
            if (m_GearLevel > 10)
                m_GearLevel = 10;
            if (m_GearEXP < 0)
                m_GearEXP = 0;
            if (m_GearLevel >= 10)
                m_GearEXP = 0;

            Movable = true;
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
            Movable = true;
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
            Movable = true;
        }
    }
}
