using System;
using Server.Custom.Dudes;
using Server.Mobiles;

namespace Server.Items
{
    /// <summary>
    /// Universal OneHanded DudeGear: morphs the Dude body while equipped.
    /// Appearance only — SlotCost 0, no AbilityId, no gear XP/levels/combat effect.
    /// Form rolled once at construct.
    /// </summary>
    public class DudeCostume : DudeGear
    {
        private static readonly int[] FormBodies = new int[]
        {
            400, // Human male
            401, // Human female
            1,   // Ogre
            4,   // Gargoyle
            9,   // Daemon
            11,  // Ettin
            13,  // Air elemental
            14,  // Earth elemental
            15,  // Fire elemental
            16,  // Water elemental
            28,  // Giant spider
            18,  // Lizardman
            7,   // Troll
            0xD7,// Giant rat (215)
            29,  // Giant toad
            36,  // Lich
            48,  // Imp
            21,  // Giant serpent (0x15; 49 reserved for Dragon)
            80,  // Skeleton
            49,  // Dragon (0x31; not 50 which is skeleton art)
            60,  // Drake (0x3C)
            41   // Wisp (0x29)
        };

        private static readonly string[] FormNames = new string[]
        {
            "Human male",
            "Human female",
            "Ogre",
            "Gargoyle",
            "Daemon",
            "Ettin",
            "Air elemental",
            "Earth elemental",
            "Fire elemental",
            "Water elemental",
            "Giant spider",
            "Lizardman",
            "Troll",
            "Giant rat",
            "Giant toad",
            "Lich",
            "Imp",
            "Giant serpent",
            "Skeleton",
            "Dragon",
            "Drake",
            "Wisp"
        };

        private int m_FormBody;
        private string m_FormName;

        [Constructable]
        public DudeCostume()
            : base(6588)
        {
            Hue = 1175;
            Layer = Layer.OneHanded;
            SlotCost = 0;
            // RequiredType unset → universal. No AbilityId.
            // Appearance only: never gains gear XP / levels.
            GearLevel = 0;
            GearEXP = 0;

            int index = Utility.Random(FormBodies.Length);
            m_FormBody = FormBodies[index];
            m_FormName = FormNames[index];
            Name = m_FormName + " Dude Costume";
            Movable = true;
        }

        public DudeCostume(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int FormBody
        {
            get { return m_FormBody; }
            set { m_FormBody = value; InvalidateProperties(); }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string FormName
        {
            get { return m_FormName; }
            set
            {
                m_FormName = value;
                if (!string.IsNullOrEmpty(m_FormName))
                    Name = m_FormName + " Dude Costume";
                InvalidateProperties();
            }
        }

        /// <summary>Appearance only — no gear XP / levels / combat effect.</summary>
        public override void AwardGearExp(int amount)
        {
            // no-op
        }

        /// <summary>Unused for costumes (appearance only).</summary>
        public override double GetEffectMultiplier()
        {
            return 1.0;
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            // Skip DudeGear Lv / EXP / Effect lines — appearance only.
            AddNameProperties(list);
            list.Add("Dude only. Zero slots. Changes form while equipped.");
        }

        public override void OnAdded(object parent)
        {
            base.OnAdded(parent);

            DudeCreature dude = parent as DudeCreature;
            if (dude != null && !dude.Deleted && m_FormBody > 0)
                dude.Body = m_FormBody;
        }

        public override void OnRemoved(object parent)
        {
            base.OnRemoved(parent);

            DudeCreature dude = parent as DudeCreature;
            if (dude == null || dude.Deleted)
                return;

            dude.Body = 0x190;
            DudeDefinition def = DudeRegistry.Get(dude.DefinitionId);
            dude.EnsureTypeShorts(def);
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
            writer.Write(m_FormBody);
            writer.Write(m_FormName);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_FormBody = reader.ReadInt();
            m_FormName = reader.ReadString();

            // Repair mislabeled Air elemental body from prior table (Air was 14).
            if (string.Equals(m_FormName, "Air elemental", StringComparison.OrdinalIgnoreCase) && m_FormBody == 14)
                m_FormBody = 13;

            // Appearance only: never keep gear XP / levels on costumes.
            GearLevel = 0;
            GearEXP = 0;

            // Keep rolled form — do not reroll. Morph legacy Form Staff art/name.
            ItemID = 6588;
            Hue = 1175;
            Layer = Layer.OneHanded;
            SlotCost = 0;
            if (!string.IsNullOrEmpty(m_FormName))
                Name = m_FormName + " Dude Costume";
            else
                Name = "Dude Costume";
            Movable = true;
        }
    }

    /// <summary>
    /// Legacy [add] alias / construct stub. World-save items typed as DudeFormStaff
    /// remain valid (IS-A DudeCostume); new loot uses DudeCostume.
    /// </summary>
    public class DudeFormStaff : DudeCostume
    {
        [Constructable]
        public DudeFormStaff()
            : base()
        {
        }

        public DudeFormStaff(Serial serial)
            : base(serial)
        {
        }
    }
}
