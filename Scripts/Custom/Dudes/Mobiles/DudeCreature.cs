using System;
using Server.Custom.Dudes;
using Server.Items;

namespace Server.Mobiles
{
    /// <summary>
    /// World representation of a Dude (wild or summoned).
    /// Persistent data lives on DudeBall; this creature mirrors it while out.
    /// Classic BaseCreature stats / AI_Melee / ControlSlots for UOR pet rules.
    /// </summary>
    [CorpseName("a dude corpse")]
    public class DudeCreature : BaseCreature
    {
        private string m_DefinitionId;
        private bool m_IsWild;
        private DudeBall m_BoundBall;
        private DateTime m_NextAbilityTime;
        private int m_DudeLevel;
        private string m_AbilityId;
        private bool m_SyncingDeath;

        [Constructable]
        public DudeCreature()
            : this("emberling", true)
        {
        }

        public DudeCreature(string definitionId, bool wild)
            : base(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.2, 0.4)
        {
            DudeRegistry.EnsureInitialized();
            DudeAbilityRegistry.EnsureInitialized();

            m_DefinitionId = definitionId;
            m_IsWild = wild;
            m_NextAbilityTime = DateTime.UtcNow;

            DudeDefinition def = DudeRegistry.Get(definitionId);
            if (def == null)
                def = DudeRegistry.GetByType(DudeType.Fire);

            ApplyDefinition(def);

            if (m_IsWild)
            {
                Tamable = false;
                Controlled = false;
                ControlMaster = null;
                FightMode = FightMode.Aggressor;
            }
            else
            {
                Tamable = false; // ownership via DudeBall, not classic taming
                ControlSlots = def != null ? def.ControlSlots : 1;
                MinTameSkill = 0.0;
                FightMode = FightMode.Closest;
            }
        }

        public DudeCreature(Serial serial)
            : base(serial)
        {
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string DefinitionId
        {
            get { return m_DefinitionId; }
            set { m_DefinitionId = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool IsWild
        {
            get { return m_IsWild; }
            set { m_IsWild = value; }
        }

        /// <summary>
        /// Wild companions are catchable; bosses / special Dudes override to false.
        /// </summary>
        public virtual bool CanBeCaught
        {
            get { return m_IsWild; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int DudeLevel
        {
            get { return m_DudeLevel; }
            set { m_DudeLevel = value; }
        }

        public DudeBall BoundBall
        {
            get { return m_BoundBall; }
            set { m_BoundBall = value; }
        }

        public DateTime NextAbilityTime
        {
            get { return m_NextAbilityTime; }
            set { m_NextAbilityTime = value; }
        }

        public string AbilityId
        {
            get { return m_AbilityId; }
            set { m_AbilityId = value; }
        }

        public override bool DeleteCorpseOnDeath
        {
            get { return !m_IsWild; }
        }

        public override bool AlwaysMurderer
        {
            get { return m_IsWild; }
        }

        public override bool IsDispellable
        {
            get { return false; }
        }

        public void ApplyDefinition(DudeDefinition def)
        {
            if (def == null)
                return;

            m_DefinitionId = def.Id;
            m_AbilityId = def.AbilityId;
            m_DudeLevel = 1;

            Name = def.Name;
            Body = def.Body;
            Hue = def.Hue;
            BaseSoundID = def.BaseSoundID;

            SetStr(def.Str);
            SetDex(def.Dex);
            SetInt(def.Int);

            SetHits(def.Hits);
            SetMana(30);
            SetStam(def.Dex);

            SetDamage(def.MinDamage, def.MaxDamage);
            SetDamageType(ResistanceType.Physical, 100);

            // Light classic resists — VirtualArmor is the UOR-era primary mitigation.
            SetResistance(ResistanceType.Physical, 10, 20);

            SetSkill(SkillName.MagicResist, 25.0, 40.0);
            SetSkill(SkillName.Tactics, 30.0, 50.0);
            SetSkill(SkillName.Wrestling, 30.0, 50.0);

            Fame = m_IsWild ? 500 : 0;
            Karma = m_IsWild ? -500 : 0;
            VirtualArmor = def.VirtualArmor;
            ControlSlots = def.ControlSlots;
        }

        /// <summary>
        /// Apply persisted DudeData onto this live creature.
        /// </summary>
        public void ApplyData(DudeData data, bool fullHeal)
        {
            if (data == null)
                return;

            DudeDefinition def = DudeRegistry.Get(data.DefinitionId);
            if (def != null)
            {
                Body = def.Body;
                Hue = def.Hue;
                BaseSoundID = def.BaseSoundID;
                m_DefinitionId = def.Id;
            }

            Name = data.DisplayName;
            m_DudeLevel = data.Level;
            m_AbilityId = data.AbilityId;
            m_IsWild = false;

            SetStr(data.Str);
            SetDex(data.Dex);
            SetInt(data.Int);

            SetHits(data.HitsMax);
            HitsMaxSeed = data.HitsMax;

            if (fullHeal || data.IsFainted)
                Hits = data.HitsMax;
            else
                Hits = Math.Max(1, Math.Min(data.Hits, data.HitsMax));

            SetMana(30 + (data.Level * 2));
            Mana = ManaMax;

            SetDamage(data.MinDamage, data.MaxDamage);
            VirtualArmor = data.VirtualArmor;

            // Scale combat skills lightly with level (classic Wrestling/Tactics).
            double skill = 30.0 + (data.Level * 2.5);
            if (skill > 100.0)
                skill = 100.0;

            SetSkill(SkillName.Tactics, skill);
            SetSkill(SkillName.Wrestling, skill);
            SetSkill(SkillName.MagicResist, 25.0 + data.Level);
        }

        public void SyncToBall()
        {
            if (m_BoundBall == null || m_BoundBall.Deleted || m_BoundBall.StoredDude == null)
                return;

            DudeData data = m_BoundBall.StoredDude;
            data.Hits = Hits;
            data.HitsMax = HitsMax;
            data.Str = RawStr;
            data.Dex = RawDex;
            data.Int = RawInt;
            data.MinDamage = DamageMin;
            data.MaxDamage = DamageMax;
            data.VirtualArmor = VirtualArmor;
            data.Level = m_DudeLevel;
            data.CustomName = Name;
            m_BoundBall.InvalidateProperties();
        }

        public override void OnThink()
        {
            base.OnThink();

            if (m_IsWild || Deleted || Map == null || Map == Map.Internal)
                return;

            if (!Controlled || ControlMaster == null || ControlMaster.Deleted)
                return;

            // Stay following when idle (classic pet Follow order).
            if (Combatant == null && ControlOrder != OrderType.Attack && ControlOrder != OrderType.Stop && ControlOrder != OrderType.Stay)
            {
                if (ControlOrder != OrderType.Follow || ControlTarget != ControlMaster)
                {
                    ControlTarget = ControlMaster;
                    ControlOrder = OrderType.Follow;
                }
            }

            TryUseAbility();
        }

        private void TryUseAbility()
        {
            Mobile target = Combatant as Mobile;
            if (target == null || target.Deleted || !target.Alive)
                return;

            if (!CanBeHarmful(target))
                return;

            DudeAbility ability = DudeAbilityRegistry.Get(m_AbilityId);
            if (ability == null)
                return;

            // Use when in range and cooldown ready.
            if (!InRange(target, 3))
                return;

            ability.TryExecute(this, target);
        }

        public override bool OnBeforeDeath()
        {
            if (!m_IsWild && m_BoundBall != null && !m_BoundBall.Deleted)
            {
                // Death policy: write last state to ball as fainted, remove world creature.
                // Prevents soft-lock; ball remains authoritative and can re-summon (revives).
                m_SyncingDeath = true;
                SyncToBall();

                DudeData data = m_BoundBall.StoredDude;
                if (data != null)
                {
                    data.IsFainted = true;
                    data.Hits = 0;
                }

                m_BoundBall.ClearSummonLink();

                Mobile master = ControlMaster;
                if (master != null)
                    master.SendMessage(0x22, "{0} fainted and returned to the Dude Ball!", Name);

                Delete();
                return false;
            }

            return base.OnBeforeDeath();
        }

        public override void OnDelete()
        {
            if (!m_SyncingDeath && m_BoundBall != null && !m_BoundBall.Deleted)
            {
                if (m_BoundBall.SummonedDude == this)
                    m_BoundBall.ClearSummonLink();
            }

            base.OnDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);

            writer.Write(m_DefinitionId);
            writer.Write(m_IsWild);
            writer.Write(m_BoundBall);
            writer.Write(m_NextAbilityTime);
            writer.Write(m_DudeLevel);
            writer.Write(m_AbilityId);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_DefinitionId = reader.ReadString();
            m_IsWild = reader.ReadBool();
            m_BoundBall = reader.ReadItem() as DudeBall;
            m_NextAbilityTime = reader.ReadDateTime();
            m_DudeLevel = reader.ReadInt();
            m_AbilityId = reader.ReadString();

            DudeRegistry.EnsureInitialized();
            DudeAbilityRegistry.EnsureInitialized();
        }
    }
}
