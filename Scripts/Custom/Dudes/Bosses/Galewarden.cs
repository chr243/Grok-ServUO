using System;
using System.Collections.Generic;
using Server.Custom.Dudes;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    /// <summary>
    /// Air farm boss. Staff spawn: [SpawnGalewarden. No world spawner.
    /// </summary>
    [CorpseName("the remains of Galewarden")]
    public class Galewarden : DudeBoss
    {
        private bool m_AbilityPending;

        [Constructable]
        public Galewarden()
            : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "Galewarden";
            Body = 13;
            Hue = 1153;
            BaseSoundID = 655;

            SetStr(280, 320);
            SetDex(110, 130);
            SetInt(80, 100);

            SetHits(1800, 2200);
            SetMana(80);
            SetStam(110, 130);

            SetDamage(10, 16);

            SetDamageType(ResistanceType.Physical, 40);
            SetDamageType(ResistanceType.Energy, 60);

            SetResistance(ResistanceType.Physical, 30, 40);
            SetResistance(ResistanceType.Fire, 25, 35);
            SetResistance(ResistanceType.Cold, 25, 35);
            SetResistance(ResistanceType.Poison, 20, 30);
            SetResistance(ResistanceType.Energy, 55, 70);

            SetSkill(SkillName.MagicResist, 80.0, 100.0);
            SetSkill(SkillName.Tactics, 90.0, 100.0);
            SetSkill(SkillName.Wrestling, 90.0, 100.0);

            Fame = 8000;
            Karma = -8000;
            VirtualArmor = 50;
        }

        public Galewarden(Serial serial)
            : base(serial)
        {
        }

        public override DudeType DudeAffinity
        {
            get { return DudeType.Air; }
        }

        public override string AbilityDisplayName
        {
            get { return "Shear"; }
        }

        public override string AbilityDescription
        {
            get { return "Warns, then shears an eight-tile wind ring and knocks foes back if space allows."; }
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

            PublicOverheadMessage(MessageType.Regular, 0x47E, false, "*Shear*");
            PlaySound(0x015);
            Effects.SendLocationParticles(
                EffectItem.Create(Location, map, EffectItem.DefaultDuration),
                0x3728, 10, 30, 1153, 0, 5022, 0);

            Point3D origin = Location;
            Timer.DelayCall(TimeSpan.FromSeconds(1.3), delegate
            {
                FinishShear(origin, map, targets);
            });

            return true;
        }

        private void FinishShear(Point3D origin, Map map, List<Mobile> targets)
        {
            m_AbilityPending = false;

            if (Deleted || map == null || map == Map.Internal)
                return;

            // 8-tile ring around self (cardinals + diagonals at radius 2)
            int[] ox = new int[] { 2, 2, 0, -2, -2, -2, 0, 2 };
            int[] oy = new int[] { 0, 2, 2, 2, 0, -2, -2, -2 };

            for (int i = 0; i < 8; i++)
            {
                Point3D loc = new Point3D(origin.X + ox[i], origin.Y + oy[i], origin.Z);
                Effects.SendLocationParticles(
                    EffectItem.Create(loc, map, EffectItem.DefaultDuration),
                    0x3728, 8, 20, 1153, 0, 5022, 0);
            }

            PlaySound(0x015);

            for (int i = 0; i < targets.Count; i++)
            {
                Mobile m = targets[i];
                if (m == null || m.Deleted || !m.Alive)
                    continue;
                if (!InRange(m, AbilityRange) || !CanBeHarmful(m))
                    continue;

                TryKnockback(m, origin, map);

                int damage = Utility.RandomMinMax(14, 20);
                DoHarmful(m);
                AOS.Damage(m, this, damage, 40, 0, 0, 0, 60);
                m.FixedParticles(0x3728, 10, 25, 5022, EffectLayer.Waist);
            }
        }

        private void TryKnockback(Mobile m, Point3D origin, Map map)
        {
            if (m == null || m.Deleted || map == null)
                return;

            int dx = Math.Sign(m.X - origin.X);
            int dy = Math.Sign(m.Y - origin.Y);
            if (dx == 0 && dy == 0)
            {
                dx = Utility.RandomBool() ? 1 : -1;
                dy = 0;
            }

            int dist = Utility.RandomMinMax(1, 2);
            Point3D dest = new Point3D(m.X + dx * dist, m.Y + dy * dist, m.Z);

            if (!map.CanFit(dest, 16, false, false))
            {
                // Try 1-tile if 2 failed
                dest = new Point3D(m.X + dx, m.Y + dy, m.Z);
                if (!map.CanFit(dest, 16, false, false))
                    return;
            }

            m.MoveToWorld(dest, map);
            Effects.SendLocationParticles(
                EffectItem.Create(dest, map, EffectItem.DefaultDuration),
                0x3728, 6, 12, 1153, 0, 5022, 0);
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
                PackItem(new GaleCore(n));

            PackExtraRandomDudeGear(
                typeof(GaleCirclet),
                typeof(GaleBracers),
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
