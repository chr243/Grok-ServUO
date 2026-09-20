using Server.Commands;
using Server.Items;

namespace Server.Custom.Dudes.Commands
{
    public static class DudeGearBagCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("dudegearbag", AccessLevel.GameMaster, new CommandEventHandler(DudeGearBag_OnCommand));
            CommandSystem.Register("DudeGearBag", AccessLevel.GameMaster, new CommandEventHandler(DudeGearBag_OnCommand));
        }

        [Usage("dudegearbag")]
        [Description("Places a bag containing one of every DudeGear in your backpack.")]
        private static void DudeGearBag_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Deleted)
                return;

            if (from.Backpack == null)
            {
                from.SendMessage("You need a backpack.");
                return;
            }

            from.Backpack.DropItem(CreateBag());
            from.SendMessage("A Dude Gear Bag was placed in your backpack.");
        }

        /// <summary>
        /// Builds a Hue 1161 "Dude Gear Bag" containing one of every DudeGear piece.
        /// Contents remain LootType Regular (tradable).
        /// </summary>
        public static Bag CreateBag()
        {
            Bag bag = new Bag();
            bag.Hue = 1161;
            bag.Name = "Dude Gear Bag";

            DropAllGear(bag);
            return bag;
        }

        public static void DropAllGear(Container bag)
        {
            // Fire
            bag.DropItem(new EmberSash());
            bag.DropItem(new EmberCirclet());
            bag.DropItem(new EmberBracers());
            // Water
            bag.DropItem(new TideSash());
            bag.DropItem(new TideCirclet());
            bag.DropItem(new TideBracers());
            // Earth
            bag.DropItem(new StoneSash());
            bag.DropItem(new StoneCirclet());
            bag.DropItem(new StoneBracers());
            // Air
            bag.DropItem(new GaleSash());
            bag.DropItem(new GaleCirclet());
            bag.DropItem(new GaleBracers());
            // Universal
            bag.DropItem(new MagicalDudeHat());
            bag.DropItem(new DudeShield());
            bag.DropItem(new DudeCostume());
        }
    }
}
