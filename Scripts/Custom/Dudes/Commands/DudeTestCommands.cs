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
            CommandSystem.Register("SpawnTidewarden", AccessLevel.GameMaster, new CommandEventHandler(SpawnTidewarden_OnCommand));
            CommandSystem.Register("SpawnStonewarden", AccessLevel.GameMaster, new CommandEventHandler(SpawnStonewarden_OnCommand));
            CommandSystem.Register("SpawnGalewarden", AccessLevel.GameMaster, new CommandEventHandler(SpawnGalewarden_OnCommand));
            CommandSystem.Register("CreateEmberCore", AccessLevel.GameMaster, new CommandEventHandler(CreateEmberCore_OnCommand));
            CommandSystem.Register("CreateTideCore", AccessLevel.GameMaster, new CommandEventHandler(CreateTideCore_OnCommand));
            CommandSystem.Register("CreateStoneCore", AccessLevel.GameMaster, new CommandEventHandler(CreateStoneCore_OnCommand));
            CommandSystem.Register("CreateGaleCore", AccessLevel.GameMaster, new CommandEventHandler(CreateGaleCore_OnCommand));
            CommandSystem.Register("CreateTrainersManual", AccessLevel.GameMaster, new CommandEventHandler(CreateTrainersManual_OnCommand));
            CommandSystem.Register("CreateDudeRevivalPotion", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeRevivalPotion_OnCommand));
            CommandSystem.Register("CreateDudeHealingPotion", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeHealingPotion_OnCommand));
            CommandSystem.Register("CreateGreaterDudeHealingPotion", AccessLevel.GameMaster, new CommandEventHandler(CreateGreaterDudeHealingPotion_OnCommand));
            CommandSystem.Register("CreateMysteryJuice", AccessLevel.GameMaster, new CommandEventHandler(CreateMysteryJuice_OnCommand));
            CommandSystem.Register("CreateBeginnersBall", AccessLevel.GameMaster, new CommandEventHandler(CreateBeginnersBall_OnCommand));
            CommandSystem.Register("CreateDudeSpawners", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeSpawners_OnCommand));
            CommandSystem.Register("CreateDudeBallDispenser", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeBallDispenser_OnCommand));
            CommandSystem.Register("CreateDudeHealthPotDispenser", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeHealthPotDispenser_OnCommand));
            CommandSystem.Register("CreateDudeRevivalPotDispenser", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeRevivalPotDispenser_OnCommand));
        }


        [Usage("CreateDudeSpawners")]
        [Description("TEST: Places Fire/Water/Earth/Air DudeSpawners at your feet and fills them.")]
        private static void CreateDudeSpawners_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Map == null)
                return;

            Item[] spawners = new Item[]
            {
                new FireDudeSpawner(),
                new WaterDudeSpawner(),
                new EarthDudeSpawner(),
                new AirDudeSpawner()
            };

            for (int i = 0; i < spawners.Length; i++)
            {
                Item s = spawners[i];
                Point3D loc = new Point3D(from.X + (i % 2), from.Y + (i / 2), from.Z);
                s.MoveToWorld(loc, from.Map);

                DudeSpawner ds = s as DudeSpawner;
                if (ds != null)
                    ds.DoSpawn();
            }

            from.SendMessage(0x59, "TEST: Placed Fire/Water/Earth/Air Dude spawners.");
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


        [Usage("CreateBeginnersBall")]
        [Description("TEST: Creates a Beginner's Ball in your backpack.")]
        private static void CreateBeginnersBall_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new BeginnersBall());
            from.SendMessage(0x59, "TEST: Beginner's Ball created.");
        }

        [Usage("SpawnTestDude [definitionId]")]
        [Description("TEST: Spawns a wild Dude at your location. See DudeRegistry / README for ids.")]
        private static void SpawnTestDude_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Map == null || from.Map == Map.Internal)
                return;

            DudeRegistry.EnsureInitialized();

            string id = "ember";
            if (e.Arguments != null && e.Arguments.Length > 0 && !string.IsNullOrEmpty(e.Arguments[0]))
                id = e.Arguments[0];

            DudeDefinition def = DudeRegistry.Get(id);
            if (def == null)
            {
                from.SendMessage("Unknown Dude id '{0}'. Use [SpawnAllTestDudes or see Dude README for ids.", id);
                return;
            }

            DudeCreature wild = new DudeCreature(def.Id, true);
            wild.MoveToWorld(from.Location, from.Map);
            from.SendMessage(0x59, "TEST: Spawned wild {0}.", def.Name);
        }

        [Usage("SpawnAllTestDudes")]
        [Description("TEST: Spawns one wild Dude of each registered species near you (weak/basic/medium/strong).")]
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

            string id = "ember";
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
            from.SendMessage(0x59, "TEST: Dude Job Station placed. Assign an Earth Dude (pebble/boulder/quake) near mineable terrain.");
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

        [Usage("SpawnTidewarden")]
        [Description("TEST: Spawns Tidewarden (Water Dude boss) at your location. Not catchable.")]
        private static void SpawnTidewarden_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Map == null || from.Map == Map.Internal)
                return;

            Tidewarden boss = new Tidewarden();
            boss.MoveToWorld(from.Location, from.Map);
            from.SendMessage(0x59, "TEST: Spawned Tidewarden. Hostile, uncatchable, no world spawner.");
        }

        [Usage("SpawnStonewarden")]
        [Description("TEST: Spawns Stonewarden (Earth Dude boss) at your location. Not catchable.")]
        private static void SpawnStonewarden_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Map == null || from.Map == Map.Internal)
                return;

            Stonewarden boss = new Stonewarden();
            boss.MoveToWorld(from.Location, from.Map);
            from.SendMessage(0x59, "TEST: Spawned Stonewarden. Hostile, uncatchable, no world spawner.");
        }

        [Usage("SpawnGalewarden")]
        [Description("TEST: Spawns Galewarden (Air Dude boss) at your location. Not catchable.")]
        private static void SpawnGalewarden_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Map == null || from.Map == Map.Internal)
                return;

            Galewarden boss = new Galewarden();
            boss.MoveToWorld(from.Location, from.Map);
            from.SendMessage(0x59, "TEST: Spawned Galewarden. Hostile, uncatchable, no world spawner.");
        }

        [Usage("CreateTideCore")]
        [Description("TEST: Creates a Tide Essence in your backpack.")]
        private static void CreateTideCore_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new TideCore());
            from.SendMessage(0x59, "TEST: Tide Essence created.");
        }

        [Usage("CreateStoneCore")]
        [Description("TEST: Creates a Stone Essence in your backpack.")]
        private static void CreateStoneCore_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new StoneCore());
            from.SendMessage(0x59, "TEST: Stone Essence created.");
        }

        [Usage("CreateGaleCore")]
        [Description("TEST: Creates a Gale Essence in your backpack.")]
        private static void CreateGaleCore_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new GaleCore());
            from.SendMessage(0x59, "TEST: Gale Essence created.");
        }

        [Usage("CreateTrainersManual")]
        [Description("TEST: Creates a Trainer's Manual in your backpack.")]
        private static void CreateTrainersManual_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new TrainersManual());
            from.SendMessage(0x59, "TEST: Trainer's Manual created. Double-click and target a Dude, ball, or boss.");
        }

        [Usage("CreateDudeRevivalPotion")]
        [Description("TEST: Creates a Dude Revival Potion in your backpack.")]
        private static void CreateDudeRevivalPotion_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new DudeRevivalPotion());
            from.SendMessage(0x59, "TEST: Dude Revival Potion created.");
        }

        [Usage("CreateDudeHealingPotion")]
        [Description("TEST: Creates a Dude Healing Potion in your backpack.")]
        private static void CreateDudeHealingPotion_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new DudeHealingPotion());
            from.SendMessage(0x59, "TEST: Dude Healing Potion created.");
        }

        [Usage("CreateGreaterDudeHealingPotion")]
        [Description("TEST: Creates a Greater Dude Healing Potion (50% heal).")]
        private static void CreateGreaterDudeHealingPotion_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new GreaterDudeHealingPotion());
            from.SendMessage(0x59, "Created Greater Dude Healing Potion.");
        }

        [Usage("CreateMysteryJuice")]
        [Description("TEST: Creates Mystery Juice (+1 Dude level).")]
        private static void CreateMysteryJuice_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;

            from.Backpack.DropItem(new MysteryJuice());
            from.SendMessage(0x59, "Created Mystery Juice.");
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

        [Usage("CreateDudeBallDispenser")]
        [Description("Creates a Dude Ball Dispenser (gravestone) in your backpack. Place and lock down for players.")]
        private static void CreateDudeBallDispenser_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;
            from.Backpack.DropItem(new DudeBallDispenser());
            from.SendMessage(0x59, "Dude Ball Dispenser created. Place it and lock it down.");
        }

        [Usage("CreateDudeHealthPotDispenser")]
        [Description("Creates a Dude Health Pots Dispenser (gravestone) in your backpack.")]
        private static void CreateDudeHealthPotDispenser_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;
            from.Backpack.DropItem(new DudeHealthPotDispenser());
            from.SendMessage(0x59, "Dude Health Pots Dispenser created. Place it and lock it down.");
        }

        [Usage("CreateDudeRevivalPotDispenser")]
        [Description("Creates a Dude Revival Pots Dispenser (gravestone) in your backpack.")]
        private static void CreateDudeRevivalPotDispenser_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Backpack == null)
                return;
            from.Backpack.DropItem(new DudeRevivalPotDispenser());
            from.SendMessage(0x59, "Dude Revival Pots Dispenser created. Place it and lock it down.");
        }
    }
}
