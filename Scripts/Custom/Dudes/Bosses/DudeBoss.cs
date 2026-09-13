using System;
using Server.Custom.Dudes;
using Server.Items;

namespace Server.Mobiles
{
    /// <summary>
    /// Base for uncatchable Dude-world bosses. Subclass for stats, ability, unique loot.
    /// Not part of the catch/summon companion loop — persistent state is loot items, not a ball.
    /// </summary>
    public abstract class DudeBoss : BaseCreature
    {
        private DateTime m_NextAbilityTime;

        protected DudeBoss(AIType ai, FightMode mode, int percep, int range, double activeSpeed, double passiveSpeed)
            : base(ai, mode, percep, range, activeSpeed, passiveSpeed)
        {
            Tamable = false;
            Controlled = false;
            ControlMaster = null;
            m_NextAbilityTime = DateTime.UtcNow + TimeSpan.FromSeconds(3.0);
        }

        public DudeBoss(Serial serial)
            : base(serial)
        {
        }

        /// <summary>Elemental affinity, matching DudeType used by companions.</summary>
        public abstract DudeType DudeAffinity { get; }

        /// <summary>Bosses are never catchable. Capture systems should check this.</summary>
        public virtual bool CanBeCaught
        {
            get { return false; }
        }

        public virtual TimeSpan AbilityCooldown
        {
            get { return TimeSpan.FromSeconds(12.0); }
        }

        public virtual int AbilityRange
        {
            get { return 6; }
        }

        /// <summary>Display name for Trainer's Manual / scouting UI.</summary>
        public virtual string AbilityDisplayName
        {
            get { return "Unknown Ability"; }
        }

        /// <summary>Short ability blurb for Trainer's Manual. Null hides the line.</summary>
        public virtual string AbilityDescription
        {
            get { return null; }
        }

        public override bool AlwaysMurderer
        {
            get { return true; }
        }

        public override bool IsDispellable
        {
            get { return false; }
        }

        public override bool DeleteCorpseOnDeath
        {
            get { return false; }
        }

        public override void OnThink()
        {
            base.OnThink();
            TryUseAbility();
        }

        protected virtual void TryUseAbility()
        {
            if (Deleted || Map == null || Map == Map.Internal)
                return;

            if (DateTime.UtcNow < m_NextAbilityTime)
                return;

            Mobile combatant = Combatant as Mobile;
            if (combatant == null || combatant.Deleted || !combatant.Alive)
                return;

            if (!InRange(combatant, AbilityRange))
                return;

            if (!CanBeHarmful(combatant))
                return;

            if (ExecuteAbility(combatant))
                m_NextAbilityTime = DateTime.UtcNow + AbilityCooldown;
        }

        /// <summary>
        /// Boss-specific combat ability. Return true if the ability fired (starts cooldown).
        /// </summary>
        protected abstract bool ExecuteAbility(Mobile primaryTarget);

        public override void GenerateLoot()
        {
            // Normal high-tier boss packs + Dude resources.
            AddLoot(LootPack.FilthyRich, 2);
            AddLoot(LootPack.MedScrolls, 1);
            AddLoot(LootPack.Gems, Utility.RandomMinMax(2, 4));
            PackDudeCommonLoot();
            PackUniqueLoot();
        }

        /// <summary>Shared Dude-relevant drops for every boss. Retune amounts freely.</summary>
        protected virtual void PackDudeCommonLoot()
        {
            PackItem(new DudeDust(Utility.RandomMinMax(10, 30)));
            PackItem(new IronIngot(Utility.RandomMinMax(8, 15)));
        }

        /// <summary>Subclass unique drop (e.g. Ember Core). Default none.</summary>
        protected virtual void PackUniqueLoot()
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version

            writer.Write(m_NextAbilityTime);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_NextAbilityTime = reader.ReadDateTime();
        }
    }
}
