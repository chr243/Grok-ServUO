using Server.Custom.Dudes;

namespace Server.Items
{
    /// <summary>
    /// Typed elemental essence dropped by Dude bosses.
    ///
    /// RESERVED: these have no gameplay effect yet. Ascension does NOT require or consume
    /// them (ascension is level-gated and free). A future progression system will consume
    /// essences. Until then they are inert collectables.
    ///
    /// Double-clicking does nothing on purpose — do not re-attach ascension here.
    /// </summary>
    public abstract class DudeEssence : Item
    {
        /// <summary>Which Dude type this essence belongs to.</summary>
        public abstract DudeType EssenceType { get; }

        protected DudeEssence(string name, int hue, int amount)
            : base(0x1F1C)
        {
            Name = name;
            Hue = hue;
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        protected DudeEssence(Serial serial)
            : base(serial)
        {
        }

        /// <summary>
        /// Central type → essence mapping, so spawning code never switches on DudeType.
        /// Add a row here when a new type gets an essence.
        /// </summary>
        public static DudeEssence CreateFor(DudeType type)
        {
            switch (type)
            {
                case DudeType.Fire:
                    return new EmberCore();
                case DudeType.Water:
                    return new TideCore();
                case DudeType.Earth:
                    return new StoneCore();
                case DudeType.Air:
                    return new GaleCore();
                default:
                    return null;
            }
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Reserved material (no use yet)");
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