using System;
using System.Collections.Generic;
using Server.Custom.Dudes;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    /// <summary>
    /// Earth farm boss. Staff spawn: [SpawnStonewarden. No world spawner.
    /// </summary>
    [CorpseName("the remains of Stonewarden")]
    public class Stonewarden : DudeBoss
    {
        private bool m_AbilityPending;

        [Constructable]
        public Stonewarden()
            : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "Stonewarden";
            Body = 14;
            Hue = 2413;
            BaseSoundID = 268;

            SetStr(280, 320);
            SetDex(110, 130);
            SetInt(80, 100);

            SetHits(1800, 2200);
            SetMana(80);
            SetStam(110, 130);

            SetDamage(10, 16);

            SetDamageType(ResistanceType.Physical, 100);

            SetResistance(ResistanceType.Physical, 45, 55);
            SetResistance(ResistanceType.Fire, 30, 40);
            SetResistance(ResistanceType.Cold, 30, 40);
            SetResistance(ResistanceType.Poison, 40, 50);
            SetResistance(ResistanceType.Energy, 20, 30);

            SetSkill(SkillName.MagicResist, 80.0, 100.0);
            SetSkill(SkillName.Tactics, 90.0, 100.0);
            SetSkill(SkillName.Wrestling, 90.0, 100.0);

            Fame = 8000;
            Karma = -8000;
            VirtualArmor = 55;
        }

        public Stonewarden(Serial serial)
            : base(serial)
        {
        }

        public override DudeType DudeAffinity
        {
            get { return DudeType.Earth; }
        }

        public override string AbilityDisplayName
        {
            get { return "Fault Line"; }
        }

        public override string AbilityDescription
        {
            get { return "Warns, then rips a four-tile spike line toward the target before crushing nearby foes."; }
        }

        public override bool BleedImmune
        {
            get { return true; }
        }

        protected override bool ExecuteAbility(Mobile primaryTarget)
        {
            if (m_AbilityPending)
                return false;

            Map map = Map;
            if (map == null || map == Map.Internal || primaryTarget == null)
                return false;

            List<Mobile> targets = CollectTargets();
            if (targets.Count == 0)
                return false;

            m_AbilityPending = true;

            PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*Fault Line*");
            PlaySound(0x1F3);
            Effects.SendLocationParticles(
                EffectItem.Create(Location, map, EffectItem.DefaultDuration),
                0x36BD, 10, 30, 2413, 0, 5044, 0);

            Point3D from = Location;
            Point3D to = primaryTarget.Location;
            Timer.DelayCall(TimeSpan.FromSeconds(1.0), delegate
            {
                RunFaultLine(from, to, map, targets);
            });

            return true;
        }

        private void RunFaultLine(Point3D from, Point3D to, Map map, List<Mobile> targets)
        {
            if (Deleted || map == null || map == Map.Internal)
            {
                m_AbilityPending = false;
                return;
            }

            int dx = Math.Sign(to.X - from.X);
            int dy = Math.Sign(to.Y - from.Y);
            if (dx == 0 && dy == 0)
            {
                dx = 1;
                dy = 0;
            }

            // Dirt burst at origin
            Effects.SendLocationParticles(
                EffectItem.Create(from, map, EffectItem.DefaultDuration),
                0x36BD, 12, 40, 2413, 0, 5044, 0);
            PlaySound(0x1F3);

            // 4-tile spike line, one tile at a time
            for (int step = 1; step <= 4; step++)
            {
                int s = step;
                Timer.DelayCall(TimeSpan.FromSeconds(0.2 * s), delegate
                {
                    if (Deleted || map == null)
                        return;

                    Point3D loc = new Point3D(from.X + dx * s, from.Y + dy * s, from.Z);
                    Effects.SendLocationParticles(
                        EffectItem.Create(loc, map, EffectItem.DefaultDuration),
                        0x36B0, 8, 20, 2413, 0, 5044, 0);
                    Effects.SendLocationEffect(loc, map, 0x36BD, 12, 8, 2413, 0);
                });
            }

            Timer.DelayCall(TimeSpan.FromSeconds(1.0), delegate
            {
                FinishFaultLine(targets);
            });
        }

        private void FinishFaultLine(List<Mobile> targets)
        {
            m_AbilityPending = false;

            if (Deleted)
                return;

            for (int i = 0; i < targets.Count; i++)
            {
                Mobile m = targets[i];
                if (m == null || m.Deleted || !m.Alive)
                    continue;
                if (!InRange(m, AbilityRange) || !CanBeHarmful(m))
                    continue;

                int damage = Utility.RandomMinMax(14, 20);
                DoHarmful(m);
                AOS.Damage(m, this, damage, 100, 0, 0, 0, 0);
                m.FixedParticles(0x36BD, 10, 25, 5044, EffectLayer.Waist);
            }
        }

        private List<Mobile> CollectTargets()
        {
            List<Mobile> targets = new List<Mobile>();
            IPooledEnumerable eable = GetMobilesInRange(AbilityRange);

            foreach (Mobile m in eable)
            {
                if (m == this || m == null || m.Deleted || !m.Alive)
                    continue;
                if (!CanBeHarmful(m))
                    continue;

                if (m.Player)
                {
                    targets.Add(m);
                    continue;
                }

                BaseCreature bc = m as BaseCreature;
                if (bc != null && (bc.Controlled || bc.Summoned))
                    targets.Add(m);
            }

            eable.Free();
            return targets;
        }

        protected override void PackUniqueLoot()
        {
            int n = Utility.RandomMinMax(0, 2);
            if (n > 0)
                PackItem(new StoneCore(n));

            PackExtraRandomDudeGear(
                typeof(StoneCirclet),
                typeof(StoneBracers),
                typeof(MagicalDudeHat),
                typeof(DudeShield));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
