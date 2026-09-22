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
        public virtual double GetEffectMultiplier()
        {
            int lv = GearLevel;
            if (lv < 0)
                lv = 0;
            if (lv > 10)
                lv = 10;
            return 1.0 + (lv * 0.10);
        }

        /// <summary>
        /// DudeGear never drops for a cast. A Dude has no backpack, so the base ClearHand path
        /// (AddToBackpack → MoveToWorld) would strand costumes/shields on the ground. Treating
        /// DudeGear as cast-safe keeps Mage Dudes in costume like wrestling stays "empty hands".
        /// </summary>
        public override bool AllowEquipedCast(Mobile from)
        {
            return true;
        }

        /// <summary>
        /// Skill scaling for copy-in skills (Magical Dude Hat, Dude Shield): floor of stored,
        /// then Lerp(stored, ceiling, GearLevel / 10). Level 0 = stored value, level 10 = ceiling,
        /// never the ceiling before gear 10. Use instead of stored * GetEffectMultiplier().
        /// </summary>
        public double GetSkillLerp(double stored, double ceiling)
        {
            if (stored < 0.0)
                stored = 0.0;

            int lv = GearLevel;
            if (lv < 0)
                lv = 0;
            if (lv > 10)
                lv = 10;

            double scaled = stored + (ceiling - stored) * (lv / 10.0);
            if (scaled < stored)
                scaled = stored;
            return scaled;
        }

        /// <summary>Award gear EXP from the same kill amount the Dude received (full, not split).</summary>
        public virtual void AwardGearExp(int amount)
        {
            if (amount < 1 || GearLevel >= 10)
                return;

            int oldLevel = GearLevel;
            m_GearEXP += amount;

            while (GearLevel < 10 && m_GearEXP >= GetExpToNext(GearLevel))
            {
                m_GearEXP -= GetExpToNext(GearLevel);
                m_GearLevel++;
            }

            if (GearLevel >= 10)
                m_GearEXP = 0;

            if (GearLevel > oldLevel)
            {
                InvalidateProperties();

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
                // EXP-only change on a kill: coalesce tooltip refreshes (AoE kills land in bursts).
                InvalidatePropertiesThrottled();
            }
        }

        private ThrottledPropertyRefresh m_PropertyRefresh;

        /// <summary>At most one tooltip rebuild per 5s for per-kill EXP updates (see ThrottledPropertyRefresh).</summary>
        public void InvalidatePropertiesThrottled()
        {
            if (m_PropertyRefresh == null)
                m_PropertyRefresh = new ThrottledPropertyRefresh(InvalidateProperties, () => Deleted, TimeSpan.FromSeconds(5.0));

            m_PropertyRefresh.Request();
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

        /// <summary>
        /// Short mouseover role line (no numbers / no Trainer's Manual paragraph).
        /// Hat/Shield/Costume override; ability gear maps AbilityId.
        /// </summary>
        protected virtual string GetRoleLine()
        {
            if (string.IsNullOrEmpty(m_AbilityId))
                return null;

            switch (m_AbilityId.ToLowerInvariant())
            {
                case "blast":
                    return "Single-target damage";
                case "ring_of_fire":
                    return "AoE damage";
                case "burn":
                    return "Single-target damage";
                case "tide_mend":
                    return "Self heal";
                case "tide_chorus":
                    return "AoE heal";
                case "spring":
                    return "Self heal";
                case "fault_strike":
                    return "Single-target damage";
                case "aftershock":
                    return "AoE damage";
                case "faultline":
                    return "Periodic single-target damage";
                case "tailwind_self":
                    return "Self attack-speed";
                case "slipstream":
                    return "Self speed";
                default:
                    return null;
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            string role = GetRoleLine();
            if (!string.IsNullOrEmpty(role))
                list.Add(role);

            if (m_HasRequiredType)
                list.Add("{0} Dude gear", m_RequiredType);

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
