using System;
using Server.Commands;
using Server.Custom.Dudes;
using Server.Items;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Custom.Dudes.Commands
{
    /// <summary>
    /// TEST HELPERS for the Dude system — GameMaster only.
    /// Commands: [CreateDudeBall, [SpawnTestDude, [SpawnAllTestDudes, [FillDudeBall
    /// </summary>
    public static class DudeTestCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("CreateDudeBall", AccessLevel.GameMaster, new CommandEventHandler(CreateDudeBall_OnCommand));
            CommandSystem.Register("SpawnTestDude", AccessLevel.GameMaster, new CommandEventHandler(SpawnTestDude_OnCommand));
            CommandSystem.Register("SpawnAllTestDudes", AccessLevel.GameMaster, new CommandEventHandler(SpawnAllTestDudes_OnCommand));
            CommandSystem.Register("FillDudeBall", AccessLevel.GameMaster, new CommandEventHandler(FillDudeBall_OnCommand));
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
    }
}
