// TEST-ONLY commands for a scratch test server. This file is NOT part of the shard build: it is
// compiled in only by Tests/build.ps1 -Harness. See Tests/README.md.
//
// [DrtRun exercises summon / recall / park, gear abilities, stage-3 passives, linked casting and
// pack AI, reporting one PASS/FAIL line per check. [DrtWorld builds a world holding Dudes in every
// state and [DrtDump writes a canonical dump of it, so a save can be compared across builds.
//
// It sticks to public APIs plus reflection on private members by name, so the same source compiles
// against different branches and the two runs can be diffed.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Server.Commands;
using Server.Custom.Dudes;
using Server.Items;
using Server.Mobiles;
using Server.Regions;

namespace Server.TestHarness
{
    public class DrtDummy : BaseCreature
    {
        [Constructable]
        public DrtDummy()
            : base(AIType.AI_Melee, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Name = "a test dummy";
            Body = 17;
            SetStr(100);
            SetDex(50);
            SetInt(10);
            SetHits(5000);
            SetDamage(1, 1);
            CantWalk = true;
            Frozen = true; // never fights back, so every hit point it loses comes from the Dude under test
            Karma = -1000;
        }

        public DrtDummy(Serial serial)
            : base(serial)
        {
        }

        public override bool AlwaysMurderer { get { return true; } }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
        }
    }

    public static class DudeRefactorTest
    {
        private const BindingFlags Inst = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags Stat = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly string[] AllAbilities =
        {
            "blast", "ring_of_fire", "burn", "tide_mend", "tide_chorus", "spring",
            "fault_strike", "aftershock", "faultline", "tailwind_self", "tailwind", "slipstream"
        };

        private static readonly string[] Actives = { "blast", "ring_of_fire", "tide_mend", "tide_chorus", "fault_strike", "aftershock", "tailwind_self", "tailwind" };
        private static readonly string[] DamageActives = { "blast", "ring_of_fire", "fault_strike", "aftershock" };
        private static readonly string[] HealActives = { "tide_mend", "tide_chorus" };

        private static Mobile m_From;
        private static int m_Pass, m_Fail;

        public static void Initialize()
        {
            CommandSystem.Register("DrtRun", AccessLevel.Administrator, new CommandEventHandler(OnRun));
            CommandSystem.Register("DrtWorld", AccessLevel.Administrator, new CommandEventHandler(OnWorld));
            CommandSystem.Register("DrtDump", AccessLevel.Administrator, new CommandEventHandler(OnDump));
        }

        // ---------------------------------------------------------------- reporting

        private static void Out(string line)
        {
            Console.WriteLine(line);
            if (m_From != null && !m_From.Deleted && m_From.NetState != null)
                m_From.SendMessage(line);
        }

        private static void Check(bool ok, string name, string detail, params object[] args)
        {
            if (ok) m_Pass++; else m_Fail++;
            Out(string.Format("DRT {0} {1}: {2}", ok ? "PASS" : "FAIL", name, args.Length > 0 ? string.Format(detail, args) : detail));
        }

        private static void Info(string detail, params object[] args)
        {
            Out("DRT INFO " + (args.Length > 0 ? string.Format(detail, args) : detail));
        }

        private static string Ser(IEntity e)
        {
            return e == null ? "-" : e.Serial.ToString();
        }

        // ---------------------------------------------------------------- sequencing

        private sealed class Seq
        {
            private readonly List<string> m_Names = new List<string>();
            private readonly List<double> m_Delays = new List<double>();
            private readonly List<Action> m_Steps = new List<Action>();

            public Seq Then(string name, double delaySeconds, Action step)
            {
                m_Names.Add(name);
                m_Delays.Add(delaySeconds);
                m_Steps.Add(step);
                return this;
            }

            public void Run()
            {
                RunAt(0);
            }

            private void RunAt(int i)
            {
                if (i >= m_Steps.Count)
                    return;

                Timer.DelayCall(TimeSpan.FromSeconds(m_Delays[i]), () =>
                {
                    try
                    {
                        m_Steps[i]();
                    }
                    catch (Exception ex)
                    {
                        Exception inner = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                        Console.WriteLine(inner);
                        Check(false, "step/" + m_Names[i], "{0}: {1}", inner.GetType().Name, inner.Message);
                    }

                    RunAt(i + 1);
                });
            }
        }

        // ---------------------------------------------------------------- reflection

        private static object GetField(Type t, object o, string name)
        {
            FieldInfo fi = t.GetField(name, o == null ? Stat : Inst);
            if (fi == null)
                throw new MissingFieldException(t.Name, name);
            return fi.GetValue(o);
        }

        private static void CallDude(DudeCreature d, string method)
        {
            MethodInfo mi = typeof(DudeCreature).GetMethod(method, Inst, null, Type.EmptyTypes, null);
            if (mi == null)
                throw new MissingMethodException("DudeCreature", method);
            mi.Invoke(d, null);
        }

        private static Dictionary<string, DateTime> NextAbility(DudeCreature d)
        {
            return GetField(typeof(DudeCreature), d, "m_NextAbilityById") as Dictionary<string, DateTime>;
        }

        // Linked stage-3 pulse. PR #3: <Ability>.PulseLinked(caster, data, inCombat, ref next);
        // main: DudeLinkSystem.Try<X>Passive(caster, data, LinkRuntime, inCombat).
        private static DateTime LinkedPulse(string passive, Mobile caster, DudeData data, out string path)
        {
            Type abilityType = passive == "burn" ? typeof(BurnAbility) : passive == "spring" ? typeof(SpringAbility) : typeof(FaultlineAbility);
            MethodInfo moved = abilityType.GetMethod("PulseLinked", Stat);
            if (moved != null)
            {
                path = abilityType.Name + ".PulseLinked";
                object[] args = { caster, data, true, DateTime.MinValue };
                moved.Invoke(null, args);
                return (DateTime)args[3];
            }

            string name = passive == "burn" ? "TryBurnPassive" : passive == "spring" ? "TrySpringPassive" : "TryFaultlinePassive";
            path = "DudeLinkSystem." + name;
            MethodInfo old = typeof(DudeLinkSystem).GetMethod(name, Stat);
            Type rtType = typeof(DudeLinkSystem).GetNestedType("LinkRuntime", BindingFlags.NonPublic);
            if (old == null || rtType == null)
                throw new MissingMethodException("DudeLinkSystem", name);
            object rt = Activator.CreateInstance(rtType, true);
            old.Invoke(null, new object[] { caster, data, rt, true });
            string field = passive == "burn" ? "NextBurnPulse" : passive == "spring" ? "NextSpringPulse" : "NextFaultlinePulse";
            return (DateTime)rtType.GetField(field).GetValue(rt);
        }

        // ---------------------------------------------------------------- helpers

        private static DudeBall NewBall(Mobile from, string id, int stage)
        {
            DudeData data = DudeData.FromDefinition(DudeRegistry.Get(id), from);
            if (stage > 0)
                data.EvolutionStage = stage;
            DudeBall ball = new DudeBall();
            ball.StoreDude(data);
            from.Backpack.DropItem(ball);
            return ball;
        }

        private static Point3D FindSpot(Map map, Point3D center, int dx, int dy)
        {
            for (int r = 0; r <= 6; r++)
            {
                for (int ox = -r; ox <= r; ox++)
                {
                    for (int oy = -r; oy <= r; oy++)
                    {
                        if (Math.Max(Math.Abs(ox), Math.Abs(oy)) != r)
                            continue;
                        int x = center.X + dx + ox, y = center.Y + dy + oy;
                        int z = map.GetAverageZ(x, y);
                        if (map.CanSpawnMobile(x, y, z) && map.CanSpawnMobile(x + 1, y, z))
                            return new Point3D(x, y, z);
                    }
                }
            }
            return new Point3D(center.X + dx, center.Y + dy, center.Z);
        }

        private static void DeleteLooseGear(Mobile from)
        {
            foreach (DudeGear g in from.Backpack.FindItemsByType<DudeGear>(true))
                g.Delete();
        }

        private static bool IsGuarded(Point3D p, Map map)
        {
            Region r = Region.Find(p, map);
            GuardedRegion g = r != null ? (GuardedRegion)r.GetRegion(typeof(GuardedRegion)) : null;
            return g != null && !g.IsDisabled();
        }

        // Open, unguarded ground (room for a mobile 4 tiles either side), searched outward from the owner.
        private static Point3D FindUnguarded(Mobile from)
        {
            Map map = from.Map;
            int[] dxs = { 1, 1, 0, -1, -1, -1, 0, 1 };
            int[] dys = { 0, 1, 1, 1, 0, -1, -1, -1 };
            for (int dist = 40; dist <= 800; dist += 20)
            {
                for (int k = 0; k < 8; k++)
                {
                    int x = from.X + dxs[k] * dist, y = from.Y + dys[k] * dist;
                    if (x < 10 || y < 10 || x > map.Width - 10 || y > map.Height - 10)
                        continue;
                    int z = map.GetAverageZ(x, y);
                    if (!map.CanSpawnMobile(x, y, z) || IsGuarded(new Point3D(x, y, z), map))
                        continue;
                    if (!map.CanSpawnMobile(x + 4, y, map.GetAverageZ(x + 4, y)) || !map.CanSpawnMobile(x - 4, y, map.GetAverageZ(x - 4, y)))
                        continue;
                    return new Point3D(x, y, z);
                }
            }
            return Point3D.Zero;
        }

        private sealed class Arena
        {
            public string Species, Passive;
            public string[] Abilities;
            public DudeBall Ball;
            public DudeCreature Dude;
            public DrtDummy Dummy;
            public int DummyNatural0, ProbeHits;
            public DateTime PulseAfterDirect;
            public readonly Dictionary<string, DateTime> ReadyAfterDirect = new Dictionary<string, DateTime>();
        }

        private sealed class RunState
        {
            public int OldFollowersMax, Followers0, Slots;
            public readonly List<DudeBall> Balls = new List<DudeBall>();
            public readonly List<DudeCreature> Dudes = new List<DudeCreature>();
            public readonly List<Arena> Arenas = new List<Arena>();
            public DudeBall LinkBall;
            public DrtDummy LinkDummy;
            public int Body0, Hue0, Str0, Dex0, Int0, LinkFollowers0, RingDummy0;
            public Point3D GuardHome;
            public readonly List<DudeBall> GuardBalls = new List<DudeBall>();
            public readonly List<DudeCreature> GuardDudes = new List<DudeCreature>();
            public readonly List<BaseCreature> Rats = new List<BaseCreature>();
            public readonly List<int> GuardExp0 = new List<int>();
            public readonly HashSet<Serial> Engaged = new HashSet<Serial>();
            public int BadTargets, Samples;
            public double RatsDeadAt = -1;
            public DateTime GuardStart;
            public Timer Sampler;
        }

        // ---------------------------------------------------------------- [DrtRun

        private static void OnRun(CommandEventArgs e)
        {
            m_From = e.Mobile;
            m_Pass = m_Fail = 0;
            Mobile from = m_From;

            if (DudeLinkSystem.IsLinked(from))
                DudeLinkSystem.TryUnlink(from);

            RunState st = new RunState();
            st.OldFollowersMax = from.FollowersMax;
            from.FollowersMax = 100;

            double despawnWait = DudeSummonEffects.GetDespawnDuration(3).TotalSeconds + 1.0;
            Info("start: {0} at {1} {2}, despawn wait {3:0.0}s", from.Name, from.Location, from.Map, despawnWait);

            new Seq()
                .Then("registry", 0.0, StepRegistry)
                .Then("summon-all", 0.2, () => StepSummonAll(st))
                .Then("think-then-recall", 1.5, () => StepThinkThenRecall(st))
                .Then("parked-1", despawnWait, () => StepParked(st, 1))
                .Then("resummon", 0.0, () => StepResummon(st))
                .Then("recall-2", 1.0, () => StepRecall(st, 2))
                .Then("parked-2", despawnWait, () => StepParked(st, 2))
                .Then("clear-all", 0.0, () => StepClearAll(st))
                .Then("arenas", 0.5, () => StepArenas(st))
                .Then("arenas-deferred", 1.0, () => StepArenasDeferred(st))
                .Then("arenas-fight", 12.0, () => StepArenasAfter(st))
                .Then("arenas-cleanup", 0.5, () => StepArenaCleanup(st))
                .Then("linked", 0.5, () => StepLinked(st))
                .Then("linked-passives", 1.0, () => StepLinkedPassives(st))
                .Then("unlink", 0.5, () => StepUnlink(st))
                .Then("guard", 0.5, () => StepGuardStart(st))
                .Then("guard-end", 30.0, () => StepGuardEnd(st))
                .Then("done", 0.5, () => StepDone(st))
                .Run();
        }

        private static void StepRegistry()
        {
            IList<DudeDefinition> defs = DudeRegistry.GetAll();
            Check(defs.Count == 12, "registry/species", "{0} species registered", defs.Count);
            foreach (DudeDefinition d in defs)
            {
                Info("DEF {0} name={1} type={2} ability={3} body={4} hue={5} slots={6} str={7} dex={8} int={9} hits={10} dmg={11}-{12} va={13}",
                    d.Id, d.Name, d.Type, d.AbilityId, d.Body, d.Hue, d.ControlSlots, d.Str, d.Dex, d.Int, d.Hits, d.MinDamage, d.MaxDamage, d.VirtualArmor);
            }

            foreach (DudeAbility a in DudeAbilityRegistry.GetAll())
                Info("ABIL {0} name={1} stage={2} kit={3} cd={4:0.0} mana={5}", a.Id, a.Name, a.Stage, a.Kit, a.Cooldown.TotalSeconds, a.ManaCost);

            List<string> missing = AllAbilities.Where(id => DudeAbilityRegistry.Get(id) == null).ToList();
            Check(missing.Count == 0, "registry/abilities", "all 12 ability ids resolve{0}", missing.Count > 0 ? " (missing " + string.Join(",", missing) + ")" : "");
        }

        private static void StepSummonAll(RunState st)
        {
            Mobile from = m_From;
            st.Followers0 = from.Followers;

            foreach (DudeDefinition d in DudeRegistry.GetAll())
            {
                DudeBall ball = NewBall(from, d.Id, 0);
                ball.Summon(from);
                DudeCreature dude = ball.SummonedDude;
                DudeData data = ball.StoredDude;
                int slots = DudeRegistry.GetControlSlots(data);
                st.Balls.Add(ball);
                st.Dudes.Add(dude);
                st.Slots += slots;

                bool ok = dude != null && ball.IsSummoned && !dude.IsWild && dude.Controlled && dude.ControlMaster == from
                    && dude.ControlOrder == OrderType.Guard && dude.Map == from.Map && dude.DefinitionId == d.Id && dude.BoundBall == ball
                    && dude.HitsMax == data.HitsMax && dude.Hits == data.Hits && dude.RawStr == data.Str && dude.ControlSlots == slots;
                Check(ok, "summon/" + d.Id, "{0} '{1}' hp={2}/{3} str={4} slots={5} order={6}",
                    Ser(dude), dude != null ? dude.Name : "-", dude != null ? dude.Hits : 0, dude != null ? dude.HitsMax : 0,
                    dude != null ? dude.RawStr : 0, slots, dude != null ? dude.ControlOrder.ToString() : "-");
            }

            Check(from.Followers == st.Followers0 + st.Slots, "summon/followers", "followers {0} -> {1} (expected +{2})", st.Followers0, from.Followers, st.Slots);
        }

        private static void StepThinkThenRecall(RunState st)
        {
            Mobile from = m_From;
            int ok = 0, spots = 0;
            HashSet<Point3D> seen = new HashSet<Point3D>();
            for (int i = 0; i < st.Dudes.Count; i++)
            {
                DudeCreature d = st.Dudes[i];
                if (d == null)
                    continue;
                if (!d.Deleted && d.Alive && d.Controlled && d.ControlMaster == from && d.ControlOrder == OrderType.Guard && d.Combatant == null && st.Balls[i].IsSummoned)
                    ok++;
                if (seen.Add(d.Location))
                    spots++;
            }
            Check(ok == st.Dudes.Count, "think/guard-idle", "{0}/{1} Dudes alive, guarding, no combatant after 1.5s of AI", ok, st.Dudes.Count);
            Info("12 Dudes stand on {0} distinct tiles after 1.5s (follow spread)", spots);

            StepRecall(st, 1);
        }

        private static void StepRecall(RunState st, int pass)
        {
            int recalling = 0;
            foreach (DudeBall ball in st.Balls)
            {
                ball.Recall(m_From);
                if (ball.IsRecalling)
                    recalling++;
            }
            Check(recalling == st.Balls.Count, "recall/" + pass, "{0}/{1} balls recalling (despawn FX running)", recalling, st.Balls.Count);
        }

        private static void StepParked(RunState st, int pass)
        {
            Mobile from = m_From;
            int ok = 0;
            List<string> bad = new List<string>();
            for (int i = 0; i < st.Balls.Count; i++)
            {
                DudeBall ball = st.Balls[i];
                DudeCreature d = st.Dudes[i];
                bool parked = d != null && !d.Deleted && !ball.IsRecalling && ball.SummonedDude == d && d.Map == Map.Internal
                    && !d.Controlled && d.ControlMaster == null && !ball.IsSummoned && ball.HasParkedDude;
                if (parked)
                    ok++;
                else
                    bad.Add(ball.StoredDude != null ? ball.StoredDude.DefinitionId : "?");
            }
            Check(ok == st.Balls.Count, "park/" + pass, "{0}/{1} parked on Internal, uncontrolled, still linked to the ball{2}", ok, st.Balls.Count, bad.Count > 0 ? " (bad: " + string.Join(",", bad) + ")" : "");
            Check(from.Followers == st.Followers0, "park/followers-" + pass, "followers back to {0} (now {1})", st.Followers0, from.Followers);
        }

        private static void StepResummon(RunState st)
        {
            Mobile from = m_From;
            int same = 0;
            for (int i = 0; i < st.Balls.Count; i++)
            {
                DudeBall ball = st.Balls[i];
                DudeCreature before = st.Dudes[i];
                int storedHits = ball.StoredDude.Hits;
                ball.Summon(from);
                DudeCreature after = ball.SummonedDude;
                if (after != null && ReferenceEquals(after, before) && ball.IsSummoned && after.Map == from.Map && after.Controlled
                    && after.ControlMaster == from && after.Hits == Math.Min(storedHits, after.HitsMax))
                    same++;
            }
            Check(same == st.Balls.Count, "resummon/same-instance", "{0}/{1} re-summons reused the parked instance (same serial, hits kept)", same, st.Balls.Count);
            Check(from.Followers == st.Followers0 + st.Slots, "resummon/followers", "followers {0} (expected {1})", from.Followers, st.Followers0 + st.Slots);
        }

        private static void StepClearAll(RunState st)
        {
            Mobile from = m_From;
            int deleted = 0;
            for (int i = 0; i < st.Balls.Count; i++)
            {
                st.Balls[i].ClearDude(from);
                if (st.Dudes[i] == null || st.Dudes[i].Deleted)
                    deleted++;
                st.Balls[i].Delete();
            }
            Check(deleted == st.Balls.Count, "clear/all", "{0}/{1} parked instances deleted by ClearDude", deleted, st.Balls.Count);
            DeleteLooseGear(from);
        }

        // ---------------------------------------------------------------- abilities (summoned path)

        private static void StepArenas(RunState st)
        {
            Mobile from = m_From;
            Map map = from.Map;
            string[] species = { "blaze", "torrent", "quake", "hurricane" };
            Type[][] gear =
            {
                new[] { typeof(EmberSash), typeof(EmberCirclet), typeof(EmberBracers) },
                new[] { typeof(TideSash), typeof(TideCirclet), typeof(TideBracers) },
                new[] { typeof(StoneSash), typeof(StoneCirclet), typeof(StoneBracers) },
                new[] { typeof(GaleSash), typeof(GaleCirclet), typeof(GaleBracers) }
            };
            string[][] abilities =
            {
                new[] { "blast", "ring_of_fire", "burn" },
                new[] { "tide_mend", "tide_chorus", "spring" },
                new[] { "fault_strike", "aftershock", "faultline" },
                new[] { "tailwind_self", "tailwind", "slipstream" }
            };
            int[][] offsets = { new[] { -9, -9 }, new[] { 9, -9 }, new[] { -9, 9 }, new[] { 9, 9 } };

            for (int k = 0; k < species.Length; k++)
            {
                Arena a = new Arena();
                a.Species = species[k];
                a.Abilities = abilities[k];
                a.Passive = abilities[k][2];
                Point3D spot = FindSpot(map, from.Location, offsets[k][0], offsets[k][1]);

                a.Ball = NewBall(from, a.Species, 3);
                a.Ball.Summon(from);
                a.Dude = a.Ball.SummonedDude;
                if (a.Dude == null)
                {
                    Check(false, "arena/" + a.Species, "summon failed");
                    continue;
                }
                a.Dude.MoveToWorld(spot, map);
                st.Arenas.Add(a);

                int equipped = 0;
                foreach (Type t in gear[k])
                {
                    Item g = (Item)Activator.CreateInstance(t);
                    if (a.Dude.EquipItem(g))
                        equipped++;
                    else
                        g.Delete();
                }
                a.Dude.RebuildGearCache();
                int granted = a.Abilities.Count(id => a.Dude.FindEquippedGearByAbility(id) != null);
                Check(equipped == 3 && granted == 3, "gear/" + a.Species, "stage 3: {0}/3 pieces equipped, {1}/3 abilities granted ({2})", equipped, granted, string.Join(",", a.Abilities));

                a.Dummy = new DrtDummy();
                a.Dummy.MoveToWorld(new Point3D(spot.X + 1, spot.Y, spot.Z), map);
                if (a.Species == "torrent")
                    a.Dude.Hits = a.Dude.HitsMax / 2;

                int dummy0 = a.Dummy.Hits, dude0 = a.Dude.Hits;
                double speed0 = a.Dude.ForceActiveSpeed;
                a.Dude.Warmode = true;
                a.Dude.Combatant = a.Dummy;
                Check(a.Dude.Combatant == a.Dummy, "combatant/" + a.Species, "summoned Dude accepts a hostile dummy as combatant");

                // Direct cast: one TryUseAbility call fires every ready active on the gear.
                DateTime t0 = DateTime.UtcNow;
                CallDude(a.Dude, "TryUseAbility");
                Dictionary<string, DateTime> next = NextAbility(a.Dude);
                for (int j = 0; j < 2; j++)
                {
                    string id = a.Abilities[j];
                    DateTime readyAt;
                    bool used = next != null && next.TryGetValue(id, out readyAt);
                    double cd = used ? (next[id] - t0).TotalSeconds : -1;
                    double nominal = DudeAbilityRegistry.Get(id).Cooldown.TotalSeconds;
                    if (used)
                        a.ReadyAfterDirect[id] = next[id];
                    Check(used, "cast/" + id, "{0} cast it; cooldown {1:0.0}s (nominal {2:0.0}s)", a.Species, cd, nominal);
                    if (a.Species == "hurricane")
                    {
                        DudeAbilityTune slip = DudeAbilityConfig.Get("slipstream");
                        double reduce = slip != null && slip.ReduceSeconds > 0.0 ? slip.ReduceSeconds : 2.0;
                        double floor = slip != null && slip.FloorSeconds > 0.0 ? slip.FloorSeconds : 7.0;
                        double expected = Math.Max(floor, nominal - reduce);
                        Check(used && Math.Abs(cd - expected) < 0.25, "slipstream/" + id, "cooldown {0:0.00}s = max({1:0.0}, {2:0.0} - {3:0.0}) = {4:0.00}s expected",
                            cd, floor, nominal, reduce, expected);
                    }
                }

                switch (a.Species)
                {
                    case "blaze":
                    case "quake":
                        Check(a.Dummy.Hits < dummy0, "effect/" + a.Species + "-actives", "dummy took {0} damage from {1} + {2}", dummy0 - a.Dummy.Hits, a.Abilities[0], a.Abilities[1]);
                        break;
                    case "torrent":
                        Check(a.Dude.Hits > dude0, "effect/torrent-heals", "Torrent healed {0} -> {1} (max {2})", dude0, a.Dude.Hits, a.Dude.HitsMax);
                        break;
                    case "hurricane":
                        Check(a.Dude.ForceActiveSpeed < speed0, "effect/hurricane-tailwind", "attack speed {0:0.000} -> {1:0.000}", speed0, a.Dude.ForceActiveSpeed);
                        break;
                }

                // Direct stage-3 passive pulse.
                if (a.Passive != "slipstream")
                {
                    string field = a.Passive == "burn" ? "m_NextBurnPulse" : a.Passive == "spring" ? "m_NextSpringPulse" : "m_NextFaultlinePulse";
                    int dummyP = a.Dummy.Hits, dudeP = a.Dude.Hits;
                    DateTime p0 = (DateTime)GetField(typeof(DudeCreature), a.Dude, field);
                    DateTime tp = DateTime.UtcNow;
                    CallDude(a.Dude, "TryStage3Passives");
                    DateTime p1 = (DateTime)GetField(typeof(DudeCreature), a.Dude, field);
                    a.PulseAfterDirect = p1;
                    Check(p1 > p0 && p1 >= tp, "pulse/" + a.Passive, "{0} pulsed; next in {1:0.0}s; dummy -{2}, dude {3:+#;-#;0}", a.Species, (p1 - tp).TotalSeconds, dummyP - a.Dummy.Hits, a.Dude.Hits - dudeP);
                    if (a.Passive == "faultline")
                        Check(a.Dummy.Hits < dummyP && a.Dummy.Paralyzed, "effect/faultline", "dummy -{0} and paralyzed={1}", dummyP - a.Dummy.Hits, a.Dummy.Paralyzed);
                    if (a.Passive == "spring")
                        Check(a.Dude.Hits > dudeP, "effect/spring", "Torrent healed {0} -> {1}", dudeP, a.Dude.Hits);
                }

                Check(DudeAbility.CurrentEffectMultiplier == 1.0, "multiplier-reset/" + a.Species, "CurrentEffectMultiplier back to {0}", DudeAbility.CurrentEffectMultiplier);

                // Stand down for a second, out of melee reach, so only timer-deferred damage (Ring of
                // Fire, centred where it was cast) can land on the dummy.
                a.Dude.Combatant = null;
                a.Dude.Warmode = false;
                a.Dude.ControlOrder = OrderType.Stay;
                a.Dude.MoveToWorld(new Point3D(spot.X - 3, spot.Y, spot.Z), map);
                a.ProbeHits = a.Dummy.Hits;
            }
        }

        private static void StepArenasDeferred(RunState st)
        {
            foreach (Arena a in st.Arenas)
            {
                int late = a.ProbeHits - a.Dummy.Hits;
                if (a.Species == "blaze")
                    Check(late > 0, "effect/ring_of_fire", "Ring of Fire's timer-deferred rings hit the dummy for {0}", late);
                else
                    Check(late == 0, "idle/" + a.Species, "no damage while standing down ({0})", late);

                // Hand over to the real AI loop (OnThink -> TryUseAbility / TryStage3Passives).
                a.Dude.Warmode = true;
                a.Dude.Combatant = a.Dummy;
                a.Dude.ControlTarget = a.Dummy;
                a.Dude.ControlOrder = OrderType.Attack;
                a.DummyNatural0 = a.Dummy.Hits;
            }
        }

        private static void StepArenasAfter(RunState st)
        {
            foreach (Arena a in st.Arenas)
            {
                int dealt = a.DummyNatural0 - a.Dummy.Hits;
                Check(!a.Dude.Deleted && a.Dude.Alive && a.Dude.Map == m_From.Map && dealt > 0, "fight/" + a.Species,
                    "12s AI fight: dummy -{0}, dude {1}/{2} hp, still on target={3}", dealt, a.Dude.Hits, a.Dude.HitsMax, a.Dude.Combatant == a.Dummy);

                Dictionary<string, DateTime> next = NextAbility(a.Dude);
                for (int j = 0; j < 2; j++)
                {
                    string id = a.Abilities[j];
                    DateTime first, now;
                    if (!a.ReadyAfterDirect.TryGetValue(id, out first) || next == null || !next.TryGetValue(id, out now))
                        continue;
                    bool due = first < DateTime.UtcNow.AddSeconds(-1.0);
                    if (due)
                        Check(now > first, "recast/" + id, "AI re-cast after its cooldown (next ready {0:0.0}s after the first)", (now - first).TotalSeconds);
                    else
                        Info("recast/{0}: cooldown longer than the window, not due yet", id);
                }

                if (a.Passive != "slipstream")
                {
                    string field = a.Passive == "burn" ? "m_NextBurnPulse" : a.Passive == "spring" ? "m_NextSpringPulse" : "m_NextFaultlinePulse";
                    DateTime p = (DateTime)GetField(typeof(DudeCreature), a.Dude, field);
                    Check(p > a.PulseAfterDirect, "pulse-ai/" + a.Passive, "AI loop kept pulsing (next pulse moved {0:0.0}s)", (p - a.PulseAfterDirect).TotalSeconds);
                }
            }
            Check(DudeAbility.CurrentEffectMultiplier == 1.0, "multiplier-reset/ai", "CurrentEffectMultiplier {0} after 12s of AI casting", DudeAbility.CurrentEffectMultiplier);
        }

        private static void StepArenaCleanup(RunState st)
        {
            foreach (Arena a in st.Arenas)
            {
                a.Ball.ClearDude(m_From);
                a.Ball.Delete();
                a.Dummy.Delete();
            }
            DeleteLooseGear(m_From);
            Check(m_From.Followers == st.Followers0, "arena/followers", "followers back to {0} (now {1})", st.Followers0, m_From.Followers);
        }

        // ---------------------------------------------------------------- linked player

        private static void StepLinked(RunState st)
        {
            Mobile from = m_From;
            st.Body0 = from.BodyMod;
            st.Hue0 = from.HueMod;
            st.Str0 = from.RawStr;
            st.Dex0 = from.RawDex;
            st.Int0 = from.RawInt;
            st.LinkFollowers0 = from.Followers;

            st.LinkBall = NewBall(from, "ember", 0);
            DudeData data = st.LinkBall.StoredDude;
            DudeDefinition def = DudeRegistry.Get("ember");

            DudeLinkSystem.TryLink(from, st.LinkBall);
            bool linked = DudeLinkSystem.IsLinked(from) && DudeLinkSystem.GetLinkedBall(from) == st.LinkBall;
            Check(linked && from.BodyMod == def.Body && from.HueMod == def.Hue && from.RawStr == Math.Max(1, data.Str)
                && from.Followers == st.LinkFollowers0 + DudeLinkSystem.LinkFollowerSlots, "link/form",
                "linked={0} bodyMod={1} hueMod={2} str={3} followers={4}", linked, from.BodyMod, from.HueMod, from.RawStr, from.Followers);

            st.LinkDummy = new DrtDummy();
            st.LinkDummy.MoveToWorld(FindSpot(from.Map, from.Location, 1, 0), from.Map);
            from.Warmode = true;
            from.Combatant = st.LinkDummy;

            // The real command handlers (the client sees their replies).
            bool h1 = CommandSystem.Handle(from, CommandSystem.Prefix + "abi1");
            bool h2 = CommandSystem.Handle(from, CommandSystem.Prefix + "abi2");
            Check(h1 && h2, "link/commands", "[abi1 / [abi2 handled");
            Info("linked data unlocks {0} ability ids (DudeData.GetUnlockedAbilityIds)", data.GetUnlockedAbilityIds().Count);

            // Every active's linked cast, called directly. Ring of Fire goes last: its rings land on
            // timers, so its damage is checked at the start of the next step.
            foreach (string id in Actives.Where(x => x != "ring_of_fire").Concat(new[] { "ring_of_fire" }))
            {
                DudeAbility ability = DudeAbilityRegistry.Get(id);
                from.Mana = from.ManaMax;
                if (HealActives.Contains(id))
                    from.Hits = Math.Max(1, from.HitsMax / 2);
                int dummy0 = st.LinkDummy.Hits, hits0 = from.Hits;
                bool ok = ability.TryExecuteLinked(from, data, st.LinkBall, st.LinkDummy);
                int dealt = dummy0 - st.LinkDummy.Hits, healed = from.Hits - hits0;
                if (id == "ring_of_fire")
                {
                    st.RingDummy0 = st.LinkDummy.Hits;
                    Check(ok, "linked-cast/ring_of_fire", "ok={0}, rings queued (mana {1}/{2})", ok, from.Mana, from.ManaMax);
                    continue;
                }
                bool effect = DamageActives.Contains(id) ? dealt > 0 : HealActives.Contains(id) ? healed > 0 : true;
                Check(ok && effect, "linked-cast/" + id, "ok={0} dummy -{1} caster {2:+#;-#;0} hp (mana {3}/{4})", ok, dealt, healed, from.Mana, from.ManaMax);
            }
        }

        private static void StepLinkedPassives(RunState st)
        {
            Mobile from = m_From;
            DudeData data = st.LinkBall.StoredDude;
            string path;

            Check(st.LinkDummy.Hits < st.RingDummy0, "linked-effect/ring_of_fire", "deferred rings hit the dummy for {0}", st.RingDummy0 - st.LinkDummy.Hits);

            int dummy0 = st.LinkDummy.Hits, tries = 0;
            DateTime next = DateTime.MinValue;
            while (st.LinkDummy.Hits == dummy0 && tries < 10)
            {
                tries++;
                next = LinkedPulse("burn", from, data, out path);
            }
            Check(next > DateTime.UtcNow && st.LinkDummy.Hits < dummy0, "linked-pulse/burn", "dummy -{0} after {1} pulse(s)", dummy0 - st.LinkDummy.Hits, tries);

            from.Hits = Math.Max(1, from.HitsMax / 2);
            int hits0 = from.Hits;
            next = LinkedPulse("spring", from, data, out path);
            Check(next > DateTime.UtcNow && from.Hits > hits0, "linked-pulse/spring", "caster {0} -> {1} via {2}", hits0, from.Hits, path);

            dummy0 = st.LinkDummy.Hits;
            next = LinkedPulse("faultline", from, data, out path);
            Check(next > DateTime.UtcNow && st.LinkDummy.Hits < dummy0 && st.LinkDummy.Paralyzed, "linked-pulse/faultline", "dummy -{0}, paralyzed={1} via {2}", dummy0 - st.LinkDummy.Hits, st.LinkDummy.Paralyzed, path);
        }

        private static void StepUnlink(RunState st)
        {
            Mobile from = m_From;
            from.Combatant = null;
            from.Warmode = false;
            DudeLinkSystem.TryUnlink(from);
            Check(!DudeLinkSystem.IsLinked(from) && from.BodyMod == st.Body0 && from.HueMod == st.Hue0 && from.RawStr == st.Str0
                && from.RawDex == st.Dex0 && from.RawInt == st.Int0 && from.Followers == st.LinkFollowers0, "unlink/restore",
                "linked={0} bodyMod={1} str/dex/int={2}/{3}/{4} followers={5}", DudeLinkSystem.IsLinked(from), from.BodyMod, from.RawStr, from.RawDex, from.RawInt, from.Followers);
            st.LinkDummy.Delete();
            st.LinkBall.Delete();
        }

        // ---------------------------------------------------------------- pack AI

        private static void StepGuardStart(RunState st)
        {
            Mobile from = m_From;

            // Town guards one-shot anything that attacks a player in a guarded region; fight outside.
            st.GuardHome = from.Location;
            Point3D field = FindUnguarded(from);
            Check(field != Point3D.Zero, "guard/field", "unguarded open ground at {0}", field);
            if (field != Point3D.Zero)
                from.MoveToWorld(field, from.Map);

            foreach (string id in new[] { "ember", "pebble" })
            {
                DudeBall ball = NewBall(from, id, 0);
                ball.Summon(from);
                st.GuardBalls.Add(ball);
                st.GuardDudes.Add(ball.SummonedDude);
                st.GuardExp0.Add(ball.StoredDude.Level * 1000000 + ball.StoredDude.CurrentEXP);
            }

            Rat r1 = new Rat();
            r1.MoveToWorld(FindSpot(from.Map, from.Location, 4, 0), from.Map);
            r1.Combatant = from;
            Rat r2 = new Rat();
            r2.MoveToWorld(FindSpot(from.Map, from.Location, -4, 0), from.Map);
            r2.Combatant = st.GuardDudes[1];
            st.Rats.Add(r1);
            st.Rats.Add(r2);
            st.GuardStart = DateTime.UtcNow;

            st.Sampler = Timer.DelayCall(TimeSpan.FromSeconds(0.25), TimeSpan.FromSeconds(0.25), () => SampleGuard(st));
        }

        private static void SampleGuard(RunState st)
        {
            st.Samples++;
            foreach (DudeCreature d in st.GuardDudes)
            {
                if (d == null || d.Deleted)
                    continue;
                Mobile c = d.Combatant as Mobile;
                DudeCreature cd = c as DudeCreature;
                if (c == m_From || (cd != null && cd.ControlMaster == m_From))
                    st.BadTargets++;
                if (c != null && st.Rats.Contains(c as BaseCreature))
                    st.Engaged.Add(d.Serial);
            }
            if (st.RatsDeadAt < 0 && st.Rats.All(r => r.Deleted || !r.Alive))
                st.RatsDeadAt = (DateTime.UtcNow - st.GuardStart).TotalSeconds;
        }

        private static void StepGuardEnd(RunState st)
        {
            if (st.Sampler != null)
                st.Sampler.Stop();

            Check(st.BadTargets == 0, "guard/no-friendly-fire", "{0} of {1} samples had a Dude targeting its owner or a pack Dude", st.BadTargets, st.Samples);
            Check(st.Engaged.Count == st.GuardDudes.Count, "guard/engage", "{0}/{1} guarding Dudes fought a rat", st.Engaged.Count, st.GuardDudes.Count);
            Check(st.RatsDeadAt >= 0, "guard/defended", "both rats dead {0}", st.RatsDeadAt >= 0 ? string.Format("after {0:0.0}s", st.RatsDeadAt) : "- still alive after 30s");
            foreach (BaseCreature r in st.Rats)
                Info("rat {0}: {1} hp {2}/{3}, combatant {4}", r.Serial, r.Deleted ? "deleted" : r.Alive ? "alive" : "dead", r.Hits, r.HitsMax, Ser(r.Combatant as Mobile));
            foreach (DudeCreature d in st.GuardDudes)
                Info("guard dude {0} ({1}): hp {2}/{3}, combatant {4}, order {5}", d.Serial, d.DefinitionId, d.Hits, d.HitsMax, Ser(d.Combatant as Mobile), d.ControlOrder);

            int gained = 0;
            for (int i = 0; i < st.GuardBalls.Count; i++)
            {
                DudeData d = st.GuardBalls[i].StoredDude;
                if (d.Level * 1000000 + d.CurrentEXP > st.GuardExp0[i])
                    gained++;
                Info("guard ball {0}: level {1} exp {2}/{3}", d.DefinitionId, d.Level, d.CurrentEXP, d.EXPToNext);
            }
            Check(gained > 0, "guard/exp", "{0} ball(s) gained EXP from the kills", gained);

            foreach (DudeBall ball in st.GuardBalls)
            {
                ball.ClearDude(m_From);
                ball.Delete();
            }
            foreach (BaseCreature r in st.Rats)
                if (!r.Deleted)
                    r.Delete();
            DeleteLooseGear(m_From);
            m_From.MoveToWorld(st.GuardHome, m_From.Map);
        }

        private static void StepDone(RunState st)
        {
            m_From.FollowersMax = st.OldFollowersMax;
            Out(string.Format("DRT DONE: {0} passed, {1} failed", m_Pass, m_Fail));
        }

        // ---------------------------------------------------------------- [DrtWorld / [DrtDump (save/load)

        private static void OnWorld(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            m_From = from;
            from.FollowersMax = 100;

            Bag bag = new Bag();
            bag.Name = "DRT world";
            from.Backpack.DropItem(bag);

            IList<DudeDefinition> defs = DudeRegistry.GetAll();
            Dictionary<string, DudeBall> byId = new Dictionary<string, DudeBall>();
            for (int i = 0; i < defs.Count; i++)
            {
                DudeDefinition d = defs[i];
                DudeData data = DudeData.FromDefinition(d, from);
                data.Level = 2 + i;
                data.CurrentEXP = 7 * i + 1;
                data.EvolutionStage = Math.Max(1, Math.Min(3, d.ControlSlots));
                if (i % 3 == 0)
                    data.CustomName = "Drt" + d.Name;
                DudeBall ball = new DudeBall();
                ball.StoreDude(data);
                bag.DropItem(ball);
                byId[d.Id] = ball;
            }

            byId["ripple"].StoredDude.IsFainted = true;

            // Left out in the world, following the owner.
            byId["ember"].Summon(from);
            byId["droplet"].Summon(from);
            if (byId["droplet"].SummonedDude != null)
                byId["droplet"].SummonedDude.EquipItem(new TideSash());

            // Parked on Internal (blaze with a full gear set).
            byId["blaze"].Summon(from);
            DudeCreature blaze = byId["blaze"].SummonedDude;
            if (blaze != null)
            {
                blaze.EquipItem(new EmberSash());
                blaze.EquipItem(new EmberCirclet());
                blaze.EquipItem(new EmberBracers());
            }
            byId["pebble"].Summon(from);
            byId["breeze"].Summon(from);

            StoneSash loose = new StoneSash();
            loose.GearLevel = 3;
            loose.GearEXP = 500;
            bag.DropItem(loose);
            bag.DropItem(new MagicalDudeHat());

            foreach (string wild in new[] { "gale", "quake" })
            {
                DudeCreature w = new DudeCreature(wild, true);
                w.MoveToWorld(FindSpot(from.Map, from.Location, 6, wild == "gale" ? 3 : -3), from.Map);
            }

            Timer.DelayCall(TimeSpan.FromSeconds(0.5), () =>
            {
                byId["blaze"].Recall(from);
                byId["pebble"].Recall(from);
                byId["breeze"].Recall(from);
            });

            Timer.DelayCall(TimeSpan.FromSeconds(0.5) + DudeSummonEffects.GetDespawnDuration(3) + TimeSpan.FromSeconds(1.0), () =>
                Out(string.Format("DRT WORLD READY: 12 balls, summoned ember+droplet, parked blaze/pebble/breeze, fainted ripple, 2 wild; followers {0}", from.Followers)));
        }

        private static string Parent(object parent)
        {
            Item i = parent as Item;
            if (i != null)
                return "I" + i.Serial + ":" + i.GetType().Name;
            Mobile m = parent as Mobile;
            if (m != null)
                return "M" + m.Serial + ":" + m.GetType().Name;
            return "world";
        }

        private static string DescribeData(DudeData x)
        {
            if (x == null)
                return "-";
            return string.Format("{0}|{1}|{2}|L{3}|x{4}/{5}|s{6}/{7}/{8}|m{9}/{10}/{11}|h{12}/{13}|d{14}-{15}|va{16}|ab{17}|un{18}|f{19}|c{20}|g{21:0.###}|st{22}|sk{23:0.#}/{24:0.#}/{25:0.#}/{26:0.#}|skin{27}",
                x.DefinitionId, x.CustomName, x.Type, x.Level, x.CurrentEXP, x.EXPToNext, x.Str, x.Dex, x.Int, x.StrMod, x.DexMod, x.IntMod,
                x.Hits, x.HitsMax, x.MinDamage, x.MaxDamage, x.VirtualArmor, x.AbilityId, x.UnlockedAbilities, x.IsFainted, Ser(x.Catcher),
                x.GatherSkill, x.EvolutionStage, x.Wrestling, x.Tactics, x.Anatomy, x.MagicResist, x.SkinHue);
        }

        private static string DescribeItems(Mobile m)
        {
            List<string> list = new List<string>();
            foreach (Item i in m.Items)
            {
                string s = i.GetType().Name + "@" + i.Layer + "#" + i.Serial;
                Container c = i as Container;
                if (c != null && i == m.Backpack)
                    s += "{" + string.Join(",", c.Items.Select(x => x.GetType().Name + "#" + x.Serial).OrderBy(x => x, StringComparer.Ordinal)) + "}";
                list.Add(s);
            }
            list.Sort(StringComparer.Ordinal);
            return string.Join(" ", list);
        }

        private static void OnDump(CommandEventArgs e)
        {
            m_From = e.Mobile;
            string label = e.Length > 0 ? e.GetString(0) : "dump";
            List<string> lines = new List<string>();

            foreach (Item item in new List<Item>(World.Items.Values))
            {
                DudeBall ball = item as DudeBall;
                if (ball != null)
                {
                    DudeCreature s = ball.SummonedDude;
                    lines.Add(string.Format("BALL {0} parent={1} name={2} hue={3} hasDude={4} summoned={5}@{6} isSummoned={7} parked={8} job={9} data=[{10}]",
                        ball.Serial, Parent(ball.Parent), ball.Name, ball.Hue, ball.HasDude, Ser(s), s != null ? (s.Map != null ? s.Map.ToString() : "null") : "-",
                        ball.IsSummoned, ball.HasParkedDude, ball.IsAssignedToJob, DescribeData(ball.StoredDude)));
                }

                DudeGear gear = item as DudeGear;
                if (gear != null)
                {
                    lines.Add(string.Format("GEAR {0} cls={1} name={2} hue={3} ab={4} cost={5} req={6} lvl={7} exp={8} layer={9} parent={10}",
                        gear.Serial, gear.GetType().Name, gear.Name, gear.Hue, gear.AbilityId, gear.SlotCost, gear.HasRequiredType ? gear.RequiredType.ToString() : "-",
                        gear.GearLevel, gear.GearEXP, gear.Layer, Parent(gear.Parent)));
                }
            }

            foreach (Mobile m in new List<Mobile>(World.Mobiles.Values))
            {
                DudeCreature d = m as DudeCreature;
                if (d != null)
                {
                    bool parked = d.Map == Map.Internal;
                    lines.Add(string.Format("DUDE {0} cls={1} def={2} wild={3} name={4} map={5} loc={6} ctl={7} master={8} order={9} ball={10} lvl={11} st={12} ab={13} sdi={14}/{15}/{16} hmax={17} hits={18} va={19} body={20} hue={21} fem={22} slots={23} dmg={24}-{25} sk={26:0.#}/{27:0.#}/{28:0.#}/{29:0.#} items=[{30}]",
                        d.Serial, d.GetType().Name, d.DefinitionId, d.IsWild, d.Name, d.Map, parked ? d.Location.ToString() : "-", d.Controlled, Ser(d.ControlMaster),
                        d.ControlOrder, Ser(d.BoundBall), d.DudeLevel, d.EvolutionStage, d.AbilityId, d.RawStr, d.RawDex, d.RawInt, d.HitsMax,
                        parked ? d.Hits.ToString() : "-", d.VirtualArmor, d.Body, d.Hue, d.Female, d.ControlSlots, d.DamageMin, d.DamageMax,
                        d.Skills.Wrestling.Base, d.Skills.Tactics.Base, d.Skills.Anatomy.Base, d.Skills.MagicResist.Base, DescribeItems(d)));
                }

                PlayerMobile pm = m as PlayerMobile;
                if (pm != null)
                {
                    lines.Add(string.Format("PLAYER {0} name={1} followers={2}/{3} bodyMod={4} hueMod={5} linked={6} str={7}",
                        pm.Serial, pm.Name, pm.Followers, pm.FollowersMax, pm.BodyMod, pm.HueMod, DudeLinkSystem.IsLinked(pm), pm.RawStr));
                }
            }

            lines.Sort(StringComparer.Ordinal);
            string path = Path.Combine(Core.BaseDirectory, "drt-" + label + ".txt");
            File.WriteAllLines(path, lines);
            Out(string.Format("DRT DUMP {0}: {1} lines -> {2}", label, lines.Count, path));
        }
    }
}
