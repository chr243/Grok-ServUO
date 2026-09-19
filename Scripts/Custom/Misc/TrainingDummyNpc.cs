using System;
using Server.Items;

namespace Server.Mobiles
{
    [CorpseName("a training dummy corpse")]
    public class TrainingDummyNpc : BaseCreature
    {
        [Constructable]
        public TrainingDummyNpc()
            : base(AIType.AI_Animal, FightMode.None, 10, 1, 0.2, 0.4)
        {
            Name = "a training dummy";
            Title = null;
            Body = 0x190;
            Hue = 0x83F;

            Blessed = false;
            CantWalk = true;
            Frozen = true;
            Tamable = false;

            SetStr(100);
            SetDex(100);
            SetInt(25);
            SetHits(500000);
            Hits = HitsMax;

            SetDamage(0, 0);
            SetDamageType(ResistanceType.Physical, 100);
            VirtualArmor = 0;

            SetSkill(SkillName.Wrestling, 0.0);
            SetSkill(SkillName.Tactics, 0.0);
            SetSkill(SkillName.MagicResist, 0.0);
            SetSkill(SkillName.Anatomy, 0.0);

            Fame = 0;
            Karma = 0;
        }

        public TrainingDummyNpc(Serial serial)
            : base(serial)
        {
        }

        public override bool BardImmune { get { return true; } }
        public override bool IsScaryToPets { get { return false; } }
        public override bool AlwaysMurderer { get { return false; } }
        public override bool InitialInnocent { get { return true; } }
        public override bool DeleteCorpseOnDeath { get { return true; } }

        public override bool IsEnemy(Mobile m)
        {
            return false;
        }

        public override void OnThink()
        {
            Hits = HitsMax;
            Combatant = null;
            Warmode = false;
            base.OnThink();
        }

        public override bool OnBeforeDeath()
        {
            Hits = HitsMax;
            return false;
        }

        public override void AlterMeleeDamageFrom(Mobile from, ref int damage)
        {
            // Keep damage so attacker skills can gain; refill after apply via OnDamage.
        }

        public override void AlterSpellDamageFrom(Mobile from, ref int damage)
        {
            // Keep damage for skill gains.
        }

        public override void OnDamage(int amount, Mobile from, bool willKill)
        {
            base.OnDamage(amount, from, willKill);
            Hits = HitsMax;
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            Combatant = null;
            Warmode = false;
        }

        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            base.AggressiveAction(aggressor, criminal);
            Combatant = null;
            Warmode = false;
        }

        public override void GenerateLoot()
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            reader.ReadInt();
            Frozen = true;
            CantWalk = true;
        }
    }
}
