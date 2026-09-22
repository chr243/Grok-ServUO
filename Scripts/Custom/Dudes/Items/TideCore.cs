using Server.Custom.Dudes;

namespace Server.Items
{
    /// <summary>Tide Essence — Water-type Dude essence (reserved for future progression).</summary>
    public class TideCore : DudeEssence
    {
        [Constructable]
        public TideCore()
            : this(1)
        {
        }

        [Constructable]
        public TideCore(int amount)
            : base("Tide Essence", 1365, amount)
        {
        }

        public TideCore(Serial serial)
            : base(serial)
        {
        }

        public override DudeType EssenceType { get { return DudeType.Water; } }
    }
}
