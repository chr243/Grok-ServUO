using Server.Custom.Dudes;

namespace Server.Items
{
    /// <summary>Ember Essence — Fire-type Dude essence (reserved for future progression).</summary>
    public class EmberCore : DudeEssence
    {
        [Constructable]
        public EmberCore()
            : this(1)
        {
        }

        [Constructable]
        public EmberCore(int amount)
            : base("Ember Essence", 1161, amount)
        {
        }

        public EmberCore(Serial serial)
            : base(serial)
        {
        }

        public override DudeType EssenceType { get { return DudeType.Fire; } }
    }
}