using System;
using Server.Commands;
using Server.Custom.Dudes;
using Server.Custom.Dudes.Jobs;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.Dudes.Commands
{
    /// <summary>
    /// TEST HELPERS for the Dude system — GameMaster only.
    /// </summary>
    public static class DudeTestCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("CreateDudeBall", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeBall_OnCommand));
            CommandSystem.Register("SpawnTestDude", AccessLevel.GameMaster, new CommandEventHandler(SpawnTestDude_OnCommand));
            CommandSystem.Register("SpawnAllTestDudes", AccessLevel.GameMaster, new CommandEventHandler(SpawnAllTestDudes_OnCommand));
            CommandSystem.Register("FillDudeBall", AccessLevel.GameMaster, new CommandEventHandler(FillDudeBall_OnCommand));
            CommandSystem.Register("CreateDudeMixer", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeMixer_OnCommand));
            CommandSystem.Register("CreateDudeDust", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeDust_OnCommand));
            CommandSystem.Register("CreateDudeCraftKit", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeCraftKit_OnCommand));
            CommandSystem.Register("CreateDudeJobStation", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeJobStation_OnCommand));
            CommandSystem.Register("StartDudeJob", AccessLevel.GameMaster, new CommandEventHandler(StartDudeJob_OnCommand));
            CommandSystem.Register("SpawnEmberlord", AccessLevel.GameMaster, new CommandEventHandler(SpawnEmberlord_OnCommand));
            CommandSystem.Register("CreateEmberCore", AccessLevel.GameMaster, new CommandEventHandler(CreateEmberCore_OnCommand));
        }

        [Usage("CreateDudeBall")]
        [Description("TEST: Creates an empty Dude Ball in your backpack.")]
        private static void CreateDudeBall_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            DudeBall ball = new DudeBall();
            from.Backpack.DropItem(ball);
            from.SendMessage(0x59, "TEST: Empty Dude Ball created.");
        }

        [Usage("SpawnTestDude [definitionId]")]
        [Description("TEST: Spawns a wild Dude at your location. Ids: emberling, tideling, stonepaw, gustling")]
        private static void SpawnTestDude_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Map == null || from.Map == Map.Internal)
                return;

            DudeRegistry.EnsureInitialized();

            string id = "emberling";
            if (e.Arguments != null && e.Arguments.Length > 0 && !string.IsNullOrEmpty(e.Arguments[0]))
                id = e.Arguments[0];

            DudeDefinition def = DudeRegistry.Get(id);
            if (def == null)
            {
                from.SendMessage("Unknown Dude id '{0}'. Try: emberling, tideling, stonepaw, gustling", id);
                return;
            }

            DudeCreature wild = new DudeCreature(def.Id, true);
            wild.MoveToWorld(from.Location, from.Map);
            from.SendMessage(0x59, "TEST: Spawned wild {0}.", def.Name);
        }

        [Usage("SpawnAllTestDudes")]
        [Description("TEST: Spawns one wild Dude of each registered type near you.")]
        private static void SpawnAllTestDudes_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Map == null || from.Map == Map.Internal)
                return;

            DudeRegistry.EnsureInitialized();

            System.Collections.Generic.IList<DudeDefinition> all = DudeRegistry.GetAll();
            int offset = 0;

            for (int i = 0; i < all.Count; i++)
            {
                DudeDefinition def = all[i];
                DudeCreature wild = new DudeCreature(def.Id, true);

                Point3D loc = new Point3D(from.X + offset, from.Y, from.Z);
                wild.MoveToWorld(loc, from.Map);
                offset += 2;
            }

            from.SendMessage(0x59, "TEST: Spawned {0} wild Dudes.", all.Count);
        }

        [Usage("FillDudeBall [definitionId]")]
        [Description("TEST: Targets a Dude Ball and fills it with a level-1 Dude (skips catch).")]
        private static void FillDudeBall_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null)
                return;

            DudeRegistry.EnsureInitialized();

            string id = "emberling";
            if (e.Arguments != null && e.Arguments.Length > 0 && !string.IsNullOrEmpty(e.Arguments[0]))
                id = e.Arguments[0];

            DudeDefinition def = DudeRegistry.Get(id);
            if (def == null)
            {
                from.SendMessage("Unknown Dude id '{0}'.", id);
                return;
            }

            from.SendMessage("TEST: Target a Dude Ball to fill with {0}.", def.Name);
            from.Target = new FillBallTarget(def, from);
        }

        [Usage("CreateDudeMixer")]
        [Description("TEST: Creates a Dude Mixer in your backpack.")]
        private static void CreateDudeMixer_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new DudeMixer());
            from.SendMessage(0x59, "TEST: Dude Mixer created.");
        }

        [Usage("CreateDudeDust [amount]")]
        [Description("TEST: Creates Dude Dust in your backpack.")]
        private static void CreateDudeDust_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            int amount = 5;
            if (e.Arguments != null && e.Arguments.Length > 0)
                int.TryParse(e.Arguments[0], out amount);
            if (amount < 1)
                amount = 1;

            from.Backpack.DropItem(new DudeDust(amount));
            from.SendMessage(0x59, "TEST: Created {0} Dude Dust.", amount);
        }

        [Usage("CreateDudeCraftKit")]
        [Description("TEST: Creates a Dude Crafting Kit in your backpack.")]
        private static void CreateDudeCraftKit_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new DudeCraftingKit());
            from.Backpack.DropItem(new IronIngot(20));
            from.SendMessage(0x59, "TEST: Dude Crafting Kit + 20 Iron Ingots created. Use with Dude Dust.");
        }

        [Usage("CreateDudeJobStation")]
        [Description("TEST: Creates a Dude Job Station at your feet.")]
        private static void CreateDudeJobStation_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Map == null || from.Map == Map.Internal)
                return;

            DudeJobRegistry.EnsureInitialized();

            DudeJobStation station = new DudeJobStation();
            station.MoveToWorld(from.Location, from.Map);
            from.SendMessage(0x59, "TEST: Dude Job Station placed. Assign an Earth Dude (stonepaw) near mineable terrain.");
        }

        [Usage("SpawnEmberlord")]
        [Description("TEST: Spawns Emberlord (Dude boss) at your location. Not catchable.")]
        private static void SpawnEmberlord_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Map == null || from.Map == Map.Internal)
                return;

            Emberlord boss = new Emberlord();
            boss.MoveToWorld(from.Location, from.Map);
            from.SendMessage(0x59, "TEST: Spawned Emberlord. Hostile, uncatchable, no world spawner.");
        }

        [Usage("CreateEmberCore")]
        [Description("TEST: Creates an Ember Core in your backpack.")]
        private static void CreateEmberCore_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new EmberCore());
            from.SendMessage(0x59, "TEST: Ember Core created.");
        }

        [Usage("StartDudeJob")]
        [Description("TEST: Target a Dude Job Station to force-start its job.")]
        private static void StartDudeJob_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null)
                return;

            from.SendMessage("TEST: Target a Dude Job Station.");
            from.Target = new StartJobTarget();
        }

        private class FillBallTarget : Target
        {
            private readonly DudeDefinition m_Def;
            private readonly Mobile m_From;

            public FillBallTarget(DudeDefinition def, Mobile from)
                : base(8, false, TargetFlags.None)
            {
                m_Def = def;
                m_From = from;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                DudeBall ball = targeted as DudeBall;
                if (ball == null)
                {
                    from.SendMessage("That is not a Dude Ball.");
                    return;
                }

                if (ball.HasDude)
                {
                    from.SendMessage("That ball already has a Dude.");
                    return;
                }

                DudeData data = DudeData.FromDefinition(m_Def, m_From);
                ball.StoreDude(data);
                from.SendMessage(0x59, "TEST: Filled ball with {0}.", data.DisplayName);
            }
        }

        private class StartJobTarget : Target
        {
            public StartJobTarget()
                : base(8, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                DudeJobStation station = targeted as DudeJobStation;
                if (station == null)
                {
                    from.SendMessage("That is not a Dude Job Station.");
                    return;
                }

                station.TryStartJob(from);
            }
        }
    }
}
