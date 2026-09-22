// TEST-ONLY command for a scratch test server. This file is NOT part of the shard build: it is
// compiled in only by Tests/build.ps1 -Harness. See Tests/README.md.
//
// [TestMixGear exercises DudeBall.ClearDude through the real Mixer path -- recycling a parked Dude,
// clearing a ball mid-recall, and cleaning up a legacy orphan -- reporting PASS/FAIL to the caller.
using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Commands;
using Server.Custom.Dudes;
using Server.Items;
using Server.Mobiles;

namespace Server.TestHarness
{
    public static class DudeMixerGearTest
    {
        public static void Initialize()
        {
            CommandSystem.Register("TestMixGear", AccessLevel.Administrator, OnCommand);
        }

        private static int m_Pass, m_Fail;

        private static void Check(Mobile from, bool ok, string what)
        {
            if (ok) m_Pass++; else m_Fail++;
            from.SendMessage("MIXTEST {0}: {1}", ok ? "PASS" : "FAIL", what);
        }

        private static DudeBall NewBall(Mobile from, string id)
        {
            DudeBall ball = new DudeBall();
            ball.StoreDude(DudeData.FromDefinition(DudeRegistry.Get(id), from));
            from.Backpack.DropItem(ball);
            return ball;
        }

        private static List<Item> EquipGear(DudeCreature dude)
        {
            List<Item> gear = new List<Item>();

            Item sash = new EmberSash();
            dude.AddItem(sash);          // worn (InnerTorso)
            gear.Add(sash);

            Item hat = new MagicalDudeHat();
            dude.AddItem(hat);           // worn (Helm)
            gear.Add(hat);

            Item spare = new EmberSash();
            dude.PackItem(spare);        // in the Dude's backpack
            gear.Add(spare);

            return gear;
        }

        private static bool AllInPack(List<Item> gear, Mobile from)
        {
            foreach (Item g in gear)
                if (g.Deleted || !g.IsChildOf(from.Backpack))
                    return false;
            return true;
        }

        private static void OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            m_Pass = m_Fail = 0;

            // ---- A: normal recycle of a parked Dude wearing gear ----
            DudeBall ballA = NewBall(from, "ember");
            ballA.Summon(from);
            DudeCreature dudeA = ballA.SummonedDude;
            Check(from, dudeA != null && ballA.IsSummoned, "A: Dude summoned");
            if (dudeA == null)
                return;

            List<Item> gearA = EquipGear(dudeA);
            ballA.Recall(from);

            Timer.DelayCall(TimeSpan.FromSeconds(2.0), () =>
            {
                Check(from, ballA.SummonedDude == dudeA && dudeA.Map == Map.Internal, "A: Dude parked on Internal after recall");

                DudeMixer.PerformMix(from, ballA);

                Check(from, dudeA.Deleted, "A: parked instance deleted by recycle");
                Check(from, ballA.SummonedDude == null && !ballA.HasDude, "A: ball empty and unlinked");
                Check(from, AllInPack(gearA, from), "A: all 3 gear pieces (2 worn + 1 in Dude pack) in player's backpack");

                Timer.DelayCall(TimeSpan.FromSeconds(1.0), () =>
                {
                    Check(from, ballA.SummonedDude == null && !ballA.IsRecalling, "A: nothing re-linked afterwards");

                    // Catch a new Dude into the same ball: must get a fresh instance with no gear.
                    ballA.StoreDude(DudeData.FromDefinition(DudeRegistry.Get("ember"), from));
                    ballA.Summon(from);
                    DudeCreature fresh = ballA.SummonedDude;
                    int freshGear = 0;
                    if (fresh != null)
                        foreach (Item i in fresh.Items)
                            if (i is DudeGear) freshGear++;
                    Check(from, fresh != null && fresh != dudeA && freshGear == 0, "A: next Dude in the ball is a fresh instance with no inherited gear");
                    if (fresh != null)
                        ballA.Recall(from);

                    RunB(from);
                });
            });
        }

        // ---- B: ball cleared while the recall FX is still playing ----
        private static void RunB(Mobile from)
        {
            DudeBall ballB = NewBall(from, "ember");
            ballB.Summon(from);
            DudeCreature dudeB = ballB.SummonedDude;
            if (dudeB == null)
            {
                Check(from, false, "B: Dude summoned");
                return;
            }

            Item sash = new EmberSash();
            dudeB.AddItem(sash);

            ballB.Recall(from);                    // starts despawn FX timer
            bool recallingAtClear = ballB.IsRecalling;
            int returned = ballB.ClearDude(from);  // before FinishRecallPark runs

            Check(from, recallingAtClear, "B: cleared while recall FX pending");
            Check(from, dudeB.Deleted && returned == 1 && !sash.Deleted && sash.IsChildOf(from.Backpack), "B: instance deleted, gear returned");

            Timer.DelayCall(TimeSpan.FromSeconds(2.0), () =>
            {
                Check(from, ballB.SummonedDude == null && !ballB.IsRecalling, "B: pending FinishRecallPark did not re-link; ball not stuck recalling");
                RunC(from);
            });
        }

        // ---- C: legacy orphan (pre-fix recycle) cleaned up after load ----
        private static void RunC(Mobile from)
        {
            DudeBall ballC = NewBall(from, "ember");
            ballC.Summon(from);
            DudeCreature dudeC = ballC.SummonedDude;
            if (dudeC == null)
            {
                Check(from, false, "C: Dude summoned");
                return;
            }

            Item sash = new EmberSash();
            dudeC.AddItem(sash);
            ballC.Recall(from);

            Timer.DelayCall(TimeSpan.FromSeconds(2.0), () =>
            {
                // Recreate the old bug's state: empty ball still linked to its parked instance.
                FieldInfo stored = typeof(DudeBall).GetField("m_StoredDude", BindingFlags.Instance | BindingFlags.NonPublic);
                stored.SetValue(ballC, null);

                MethodInfo deleteOrphan = typeof(DudeBall).GetMethod("DeleteOrphan", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo summoned = typeof(DudeBall).GetField("m_SummonedDude", BindingFlags.Instance | BindingFlags.NonPublic);
                summoned.SetValue(ballC, null);  // Deserialize drops the link before scheduling the delete
                deleteOrphan.Invoke(ballC, new object[] { dudeC });

                Check(from, dudeC.Deleted, "C: orphan deleted");
                Check(from, !sash.Deleted && sash.Parent == ballC.Parent, "C: orphan's gear placed next to the ball (same container)");

                from.SendMessage("MIXTEST DONE: {0} passed, {1} failed", m_Pass, m_Fail);
            });
        }
    }
}
