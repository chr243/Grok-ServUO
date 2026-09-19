using System;
using System.Collections.Generic;
using Server.Custom.Dudes;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    /// <summary>
    /// Fire farm boss. Uncatchable; FilthyRich loot + dust + 0-2 Ember Essence.
    /// Staff spawn: [SpawnEmberlord or [add Emberlord. No automatic world spawner.
    /// </summary>
    [CorpseName("the remains of Emberlord")]
    public class Emberlord : DudeBoss
    {
        private bool m_AbilityPending;

        [Constructable]
        public Emberlord()
            : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "Emberlord";
            Body = 9;
            Hue = 1161;
            BaseSoundID = 838;

            SetStr(280, 320);
            SetDex(110, 130);
            SetInt(80, 100);

            SetHits(1800, 2200);
            SetMana(80);
            SetStam(110, 130);

            SetDamage(10, 16);

            SetDamageType(ResistanceType.Physical, 20);
            SetDamageType(ResistanceType.Fire, 80);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 60, 75);
            SetResistance(ResistanceType.Cold, 10, 20);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.MagicResist, 80.0, 100.0);
            SetSkill(SkillName.Tactics, 90.0, 100.0);
            SetSkill(SkillName.Wrestling, 90.0, 100.0);

            Fame = 8000;
            Karma = -8000;
            VirtualArmor = 50;

            AddItem(new LightSource());
        }

        public Emberlord(Serial serial)
            : base(serial)
        {
        }

        public override DudeType DudeAffinity
        {
            get { return DudeType.Fire; }
        }

        public override string AbilityDisplayName
        {
            get { return "Ember Burst"; }
        }

        public override string AbilityDescription
        {
            get { return "Warns, then FlameStrike on self and cardinal tiles; scorches nearby players and pets."; }
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
            if (map == null || map == Map.Internal)
                return false;

            List<Mobile> targets = CollectTargets();
            if (targets.Count == 0)
                return false;

            m_AbilityPending = true;

            PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Ember Burst*");
            PlaySound(0x208);
            Effects.SendLocationParticles(
                EffectItem.Create(Location, map, EffectItem.DefaultDuration),
                0x3709, 10, 30, 5052);

            Point3D origin = Location;
            Timer.DelayCall(TimeSpan.FromSeconds(1.2), delegate
            {
                FinishEmberBurst(origin, map, targets);
            });

            return true;
        }

        private void FinishEmberBurst(Point3D origin, Map map, List<Mobile> targets)
        {
            m_AbilityPending = false;

            if (Deleted || map == null || map == Map.Internal)
                return;

            // FlameStrike on boss + 4 cardinals 2 tiles out
            PlayStrike(origin, map);
            PlayStrike(new Point3D(origin.X + 2, origin.Y, origin.Z), map);
            PlayStrike(new Point3D(origin.X - 2, origin.Y, origin.Z), map);
            PlayStrike(new Point3D(origin.X, origin.Y + 2, origin.Z), map);
            PlayStrike(new Point3D(origin.X, origin.Y - 2, origin.Z), map);

            // Optional short fire tiles (visual / tiny tick)
            SpawnFireTile(new Point3D(origin.X + 1, origin.Y, origin.Z), map);
            SpawnFireTile(new Point3D(origin.X - 1, origin.Y, origin.Z), map);
            SpawnFireTile(new Point3D(origin.X, origin.Y + 1, origin.Z), map);
            SpawnFireTile(new Point3D(origin.X, origin.Y - 1, origin.Z), map);

            PlaySound(0x208);

            for (int i = 0; i < targets.Count; i++)
            {
                Mobile m = targets[i];
                if (m == null || m.Deleted || !m.Alive)
                    continue;
                if (!InRange(m, AbilityRange) || !CanBeHarmful(m))
                    continue;

                int damage = Utility.RandomMinMax(14, 20);
                DoHarmful(m);
                AOS.Damage(m, this, damage, 0, 100, 0, 0, 0);
                m.FixedParticles(0x3709, 10, 30, 5052, EffectLayer.LeftFoot);
            }
        }

        private static void PlayStrike(Point3D loc, Map map)
        {
            Effects.SendLocationParticles(
                EffectItem.Create(loc, map, EffectItem.DefaultDuration),
                0x3709, 10, 30, 5052);
        }

        private void SpawnFireTile(Point3D loc, Map map)
        {
            if (!map.CanFit(loc, 16, false, false))
                return;

            Effects.SendLocationEffect(loc, map, 0x3709, 16, 10, 1161, 0);

            // Tiny optional tick (0-4), visual-first
            Timer.DelayCall(TimeSpan.FromSeconds(0.4), delegate
            {
                if (Deleted || map == null)
                    return;

                IPooledEnumerable eable = map.GetMobilesInRange(loc, 0);
                foreach (Mobile m in eable)
                {
                    if (m == this || m == null || m.Deleted || !m.Alive)
                        continue;
                    if (!(m.Player || (m is BaseCreature && (((BaseCreature)m).Controlled || ((BaseCreature)m).Summoned))))
                        continue;
                    if (!CanBeHarmful(m))
                        continue;

                    int tick = Utility.RandomMinMax(0, 4);
                    if (tick > 0)
                    {
                        DoHarmful(m);
                        AOS.Damage(m, this, tick, 0, 100, 0, 0, 0);
                    }
                }
                eable.Free();
            });
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
            int essence = Utility.RandomMinMax(0, 2);
            if (essence > 0)
                PackItem(new EmberCore(essence));

            PackExtraRandomDudeGear(
                typeof(EmberCirclet),
                typeof(EmberBracers),
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
