using System;
using System.Collections.Generic;
using Server.Custom.Dudes;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    /// <summary>
    /// First Dude boss — hostile Fire affinity. Uncatchable; FilthyRich loot + dust + 0-2 Ember Essence.
    /// Staff spawn: [SpawnEmberlord or [add Emberlord. No automatic world spawner.
    /// </summary>
    [CorpseName("the remains of Emberlord")]
    public class Emberlord : DudeBoss
    {
        [Constructable]
        public Emberlord()
            : base(AIType.AI_Melee, FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            Name = "Emberlord";
            Body = 9;     // daemon — distinct from Emberling fire-elemental body 15
            Hue = 1161;   // bright fire; Emberling uses 1359
            BaseSoundID = 838;

            SetStr(280, 320);
            SetDex(110, 130);
            SetInt(80, 100);

            SetHits(1800, 2200);
            SetMana(80);
            SetStam(110, 130);

            SetDamage(18, 26);

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

        public override TimeSpan AbilityCooldown
        {
            get { return TimeSpan.FromSeconds(12.0); }
        }

        public override int AbilityRange
        {
            get { return 6; }
        }

        public override string AbilityDisplayName
        {
            get { return "Ember Burst"; }
        }

        public override string AbilityDescription
        {
            get { return "AoE fire burst that scorches nearby players and pets."; }
        }

        public override bool BleedImmune
        {
            get { return true; }
        }

        protected override bool ExecuteAbility(Mobile primaryTarget)
        {
            Map map = Map;
            if (map == null || map == Map.Internal)
                return false;

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

            if (targets.Count == 0)
                return false;

            PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Ember Burst*");
            PlaySound(0x208);

            Effects.SendLocationParticles(
                EffectItem.Create(Location, map, EffectItem.DefaultDuration),
                0x3709, 10, 30, 5052);

            for (int i = 0; i < targets.Count; i++)
            {
                Mobile m = targets[i];
                int damage = Utility.RandomMinMax(22, 30);

                DoHarmful(m);
                AOS.Damage(m, this, damage, 0, 100, 0, 0, 0);
                m.FixedParticles(0x3709, 10, 30, 5052, EffectLayer.LeftFoot);
            }

            return true;
        }

        protected override void PackUniqueLoot()
        {
            int essence = Utility.RandomMinMax(0, 2);
            if (essence > 0)
                PackItem(new EmberCore(essence));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}
