using Server.Custom.Dudes;

namespace Server.Items
{
    /// <summary>Stone Essence — Earth-type Dude essence (reserved for future progression).</summary>
    public class StoneCore : DudeEssence
    {
        [Constructable]
        public StoneCore()
            : this(1)
        {
        }

        [Constructable]
        public StoneCore(int amount)
            : base("Stone Essence", 2413, amount)
        {
        }

        public StoneCore(Serial serial)
            : base(serial)
        {
        }

        public override DudeType EssenceType { get { return DudeType.Earth; } }
    }
}
