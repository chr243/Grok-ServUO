using System;
using System.Collections.Generic;
using Server.Custom.Dudes;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    /// <summary>
    /// Water farm boss. Staff spawn: [SpawnTidewarden. No world spawner.
    /// </summary>
    [CorpseName("the remains of Tidewarden")]
    public class Tidewarden : DudeBoss
    {
        private bool m_AbilityPending;

        [Constructable]
        public Tidewarden()
            : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "Tidewarden";
            Body = 16;
            Hue = 1365;
            BaseSoundID = 278;

            SetStr(280, 320);
            SetDex(110, 130);
            SetInt(80, 100);

            SetHits(1800, 2200);
            SetMana(80);
            SetStam(110, 130);

            SetDamage(10, 16);

            SetDamageType(ResistanceType.Physical, 30);
            SetDamageType(ResistanceType.Cold, 70);

            SetResistance(ResistanceType.Physical, 35, 45);
            SetResistance(ResistanceType.Fire, 10, 20);
            SetResistance(ResistanceType.Cold, 60, 75);
            SetResistance(ResistanceType.Poison, 30, 40);
            SetResistance(ResistanceType.Energy, 30, 40);

            SetSkill(SkillName.MagicResist, 80.0, 100.0);
            SetSkill(SkillName.Tactics, 90.0, 100.0);
            SetSkill(SkillName.Wrestling, 90.0, 100.0);

            Fame = 8000;
            Karma = -8000;
            VirtualArmor = 50;
        }

        public Tidewarden(Serial serial)
            : base(serial)
        {
        }

        public override DudeType DudeAffinity
        {
            get { return DudeType.Water; }
        }

        public override string AbilityDisplayName
        {
            get { return "Tide Crash"; }
        }

        public override string AbilityDescription
        {
            get { return "Warns, then crashes water tiles near the target and soaks nearby players and pets."; }
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

            PublicOverheadMessage(MessageType.Regular, 0x44, false, "*Tide Crash*");
            PlaySound(0x026);
            Effects.SendLocationParticles(
                EffectItem.Create(Location, map, EffectItem.DefaultDuration),
                0x352D, 10, 30, 1365, 0, 5022, 0);

            Point3D focus = primaryTarget.Location;
            Timer.DelayCall(TimeSpan.FromSeconds(1.2), delegate
            {
                FinishTideCrash(focus, map, targets);
            });

            return true;
        }

        private void FinishTideCrash(Point3D focus, Map map, List<Mobile> targets)
        {
            m_AbilityPending = false;

            if (Deleted || map == null || map == Map.Internal)
                return;

            // Splash on boss
            Effects.SendLocationParticles(
                EffectItem.Create(Location, map, EffectItem.DefaultDuration),
                0x352D, 10, 40, 1365, 0, 5022, 0);

            // 3 water tiles near the target
            Point3D[] tiles = new Point3D[]
            {
                focus,
                new Point3D(focus.X + 1, focus.Y, focus.Z),
                new Point3D(focus.X, focus.Y + 1, focus.Z)
            };

            for (int i = 0; i < tiles.Length; i++)
            {
                Point3D loc = tiles[i];
                Effects.SendLocationEffect(loc, map, 0x352D, 20, 10, 1365, 0);
                Effects.SendLocationParticles(
                    EffectItem.Create(loc, map, EffectItem.DefaultDuration),
                    0x3728, 8, 20, 1365, 0, 5022, 0);
            }

            PlaySound(0x026);

            for (int i = 0; i < targets.Count; i++)
            {
                Mobile m = targets[i];
                if (m == null || m.Deleted || !m.Alive)
                    continue;
                if (!InRange(m, AbilityRange) || !CanBeHarmful(m))
                    continue;

                int damage = Utility.RandomMinMax(14, 20);
                DoHarmful(m);
                AOS.Damage(m, this, damage, 0, 0, 100, 0, 0);
                m.FixedParticles(0x352D, 10, 30, 5022, EffectLayer.Waist);
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
                PackItem(new TideCore(n));

            PackExtraRandomDudeGear(
                typeof(TideCirclet),
                typeof(TideBracers),
                typeof(MagicalDudeHat),
                typeof(DudeShield),
                typeof(DudeCostume));
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
