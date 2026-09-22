using Server.Custom.Dudes;

namespace Server.Items
{
    /// <summary>Gale Essence — Air-type Dude essence (reserved for future progression).</summary>
    public class GaleCore : DudeEssence
    {
        [Constructable]
        public GaleCore()
            : this(1)
        {
        }

        [Constructable]
        public GaleCore(int amount)
            : base("Gale Essence", 1153, amount)
        {
        }

        public GaleCore(Serial serial)
            : base(serial)
        {
        }

        public override DudeType EssenceType { get { return DudeType.Air; } }
    }
}
