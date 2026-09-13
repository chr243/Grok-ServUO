using System;
using System.Collections.Generic;
using Server.Custom.Dudes;
using Server.Items;
using Server.Network;

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
        private int m_EvolutionStage;
        private Dictionary<string, DateTime> m_NextAbilityById;
        private bool m_Fainting;

        private const double DudeForceSpeed = 0.1;

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
            m_EvolutionStage = 1;
            m_NextAbilityById = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            DudeDefinition def = DudeRegistry.Get(definitionId);
            if (def == null)
                def = DudeRegistry.GetByType(DudeType.Fire);

            ApplyDefinition(def);
            ApplyDudeSpeeds();

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
                ControlSlots = def != null ? def.ControlSlots : 4;
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

        [CommandProperty(AccessLevel.GameMaster)]
        public int EvolutionStage
        {
            get { return m_EvolutionStage < 1 ? 1 : m_EvolutionStage; }
            set { m_EvolutionStage = value < 1 ? 1 : value; }
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

        /// <summary>
        /// Wild Dudes are blue (innocent) — only bosses stay AlwaysMurderer.
        /// </summary>
        public override bool AlwaysMurderer
        {
            get { return false; }
        }

        /// <summary>
        /// Wild and summoned Dudes are blue/innocent. Bosses use DudeBoss.AlwaysMurderer.
        /// </summary>
        public override bool InitialInnocent
        {
            get { return true; }
        }

        /// <summary>
        /// Staff/Owner characters resolve as gray in ServUO notoriety; skip inheriting that
        /// so summoned Dudes stay blue. Normal players still use ControlMaster notoriety.
        /// </summary>
        public override bool ForceNotoriety
        {
            get { return !m_IsWild && ControlMaster != null && ControlMaster.AccessLevel > AccessLevel.Player; }
        }

        public override bool IsDispellable
        {
            get { return false; }
        }

        /// <summary>
        /// Force player-comparable speed. SpeedInfo.GetSpeeds overwrites ctor Active/Passive
        /// when UseNewSpeeds is on; Force* bypasses that via the ActiveSpeed getter.
        /// </summary>
        public void ApplyDudeSpeeds()
        {
            if (m_IsWild)
            {
                // 0.0 = do not force — use normal mob SpeedInfo (wilds must not match pet speed).
                ForceActiveSpeed = 0.0;
                ForcePassiveSpeed = 0.0;
                AdjustSpeeds();
                CurrentSpeed = PassiveSpeed;
            }
            else
            {
                ForceActiveSpeed = DudeForceSpeed;
                ForcePassiveSpeed = DudeForceSpeed;
                CurrentSpeed = DudeForceSpeed;
            }
        }

        public void ApplyDefinition(DudeDefinition def)
        {
            if (def == null)
                return;

            m_DefinitionId = def.Id;
            m_AbilityId = def.AbilityId;
            m_DudeLevel = 1;
            m_EvolutionStage = 1;

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

            ApplyCombatSkills(null);

            Fame = m_IsWild ? 100 : 0;
            Karma = m_IsWild ? 0 : 0;
            VirtualArmor = def.VirtualArmor;
            ControlSlots = def.ControlSlots;

            ApplyDudeSpeeds();
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
            m_EvolutionStage = data.EvolutionStage;
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

            ApplyCombatSkills(data);
            ApplyDudeSpeeds();
        }

        /// <summary>
        /// Wrestling / Tactics / MagicResist capped by evolution stage (100 / 110 / 120), not level.
        /// </summary>
        public void ApplyCombatSkills(DudeData data)
        {
            double skill = DudeExperience.GetCombatSkillCap(data);
            SetSkill(SkillName.Tactics, skill);
            SetSkill(SkillName.Wrestling, skill);
            SetSkill(SkillName.MagicResist, skill); // Resist Spells — stage caps 100 / 110 / 120
        }

        /// <summary>Legacy name — redirects to ApplyCombatSkills with stage 1.</summary>
        public void ApplyLevelSkills(int level)
        {
            ApplyCombatSkills(null);
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

        /// <summary>
        /// Town guards focusing a summoned Dude must not trigger pet AI fight-back
        /// (Combatant ↔ Guard Focus ↔ DoHarmful recursion / StackOverflow).
        /// </summary>
        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            if (aggressor is BaseGuard)
            {
                IDamageable oldCombatant = Combatant;
                base.AggressiveAction(aggressor, criminal);

                // Undo BaseCreature AI / Combatant assignment against the guard.
                if (Combatant == aggressor)
                    Combatant = oldCombatant;

                if (ControlOrder == OrderType.Attack && ControlTarget == aggressor)
                {
                    ControlTarget = ControlMaster;
                    ControlOrder = OrderType.Guard;
                }

                return;
            }

            base.AggressiveAction(aggressor, criminal);
        }

        /// <summary>
        /// Never assign Combatant to a town guard (indirect DoHarmful) — breaks recursion.
        /// </summary>
        public override void DoHarmful(IDamageable target, bool indirect)
        {
            if (target is BaseGuard)
                base.DoHarmful(target, true);
            else
                base.DoHarmful(target, indirect);
        }

        public override void OnThink()
        {
            base.OnThink();

            if (m_IsWild || Deleted || Map == null || Map == Map.Internal || m_Fainting)
                return;

            if (!Controlled || ControlMaster == null || ControlMaster.Deleted)
                return;

            // Default combat stance is Guard — keep hunting nearby threats after each kill.
            // Do NOT force Follow when idle; that stopped aggression after the first corpse.
            if (ControlOrder == OrderType.None)
            {
                ControlTarget = ControlMaster;
                ControlOrder = OrderType.Guard;
            }
            else if (ControlOrder == OrderType.Guard && ControlTarget != ControlMaster)
            {
                ControlTarget = ControlMaster;
            }

            TryUseAbility();
            TryInfernoxPassive();
        }

        private List<string> GetUnlockedAbilityIds()
        {
            if (m_BoundBall != null && !m_BoundBall.Deleted && m_BoundBall.StoredDude != null)
                return m_BoundBall.StoredDude.GetUnlockedAbilityIds();

            List<string> list = new List<string>();
            if (!string.IsNullOrEmpty(m_AbilityId))
                list.Add(m_AbilityId);
            return list;
        }

        private void TryUseAbility()
        {
            if (m_Fainting || Frozen)
                return;

            Mobile target = Combatant as Mobile;
            if (target == null || target.Deleted || !target.Alive)
                return;

            if (!CanBeHarmful(target))
                return;

            if (m_NextAbilityById == null)
                m_NextAbilityById = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            List<string> ids = GetUnlockedAbilityIds();
            for (int i = 0; i < ids.Count; i++)
            {
                DudeAbility ability = DudeAbilityRegistry.Get(ids[i]);
                if (ability == null)
                    continue;

                // Passive display stub — combat handled by TryInfernoxPassive.
                if (string.Equals(ability.Id, "burn", StringComparison.OrdinalIgnoreCase))
                    continue;

                DateTime readyAt;
                if (m_NextAbilityById.TryGetValue(ability.Id, out readyAt) && DateTime.UtcNow < readyAt)
                    continue;

                int range = 3;
                if (string.Equals(ability.Id, "ring_of_fire", StringComparison.OrdinalIgnoreCase))
                    range = RingOfFireAbility.AoERange;

                if (!InRange(target, range))
                    continue;

                if (ability.ManaCost > 0 && Mana < ability.ManaCost)
                    continue;

                if (ability.ManaCost > 0)
                    Mana -= ability.ManaCost;

                ability.Execute(this, target);
                m_NextAbilityById[ability.Id] = DateTime.UtcNow + ability.Cooldown;
                // Independent cooldowns — keep scanning so Blast and Ring of Fire both work.
            }
        }

        private void TryInfernoxPassive()
        {
            if (m_Fainting || Frozen || Combatant == null)
                return;

            bool infernox = EvolutionStage >= 3
                || string.Equals(m_DefinitionId, "infernox", StringComparison.OrdinalIgnoreCase);

            if (!infernox)
                return;

            if (Utility.RandomDouble() >= 0.05)
                return;

            int damage = Math.Max(1, (int)(DudeExperience.GetBlastDamage(m_DudeLevel) * 0.3));

            List<Mobile> list = new List<Mobile>();
            foreach (Mobile m in GetMobilesInRange(8))
            {
                if (m == null || m == this || m.Deleted || !m.Alive)
                    continue;
                if (m == ControlMaster)
                    continue;
                if (!CanBeHarmful(m))
                    continue;

                BaseCreature bc = m as BaseCreature;
                if (bc != null && bc.Controlled && bc.ControlMaster == ControlMaster)
                    continue;

                list.Add(m);
            }

            if (list.Count == 0)
                return;

            PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Burn*");
            PlaySound(0x208);

            for (int i = 0; i < list.Count; i++)
            {
                Mobile m = list[i];
                DoHarmful(m);
                AOS.Damage(m, this, damage, 0, 100, 0, 0, 0);
                DudeAbilityVfx.PlayFireHit(m, true);
            }
        }

        public override void GenerateLoot()
        {
            base.GenerateLoot();

            if (!m_IsWild)
                return;

            PackGold(10, 50);

            if (Utility.RandomDouble() < 0.35)
                PackItem(new Bandage(Utility.RandomMinMax(1, 3)));

            if (Utility.RandomDouble() < 0.30)
            {
                switch (Utility.Random(8))
                {
                    case 0: PackItem(new BlackPearl(Utility.RandomMinMax(1, 3))); break;
                    case 1: PackItem(new Bloodmoss(Utility.RandomMinMax(1, 3))); break;
                    case 2: PackItem(new Garlic(Utility.RandomMinMax(1, 3))); break;
                    case 3: PackItem(new Ginseng(Utility.RandomMinMax(1, 3))); break;
                    case 4: PackItem(new MandrakeRoot(Utility.RandomMinMax(1, 3))); break;
                    case 5: PackItem(new Nightshade(Utility.RandomMinMax(1, 3))); break;
                    case 6: PackItem(new SulfurousAsh(Utility.RandomMinMax(1, 3))); break;
                    default: PackItem(new SpidersSilk(Utility.RandomMinMax(1, 3))); break;
                }
            }

            if (Utility.RandomDouble() < 0.20)
                PackItem(new BreadLoaf());

            DudeDefinition def = DudeRegistry.Get(m_DefinitionId);
            if (def != null && def.Type == DudeType.Fire && Utility.RandomDouble() < 0.05)
                PackItem(new EmberCore());
        }

        public override bool OnBeforeDeath()
        {
            if (!m_IsWild && m_BoundBall != null && !m_BoundBall.Deleted)
            {
                // Faint: play despawn FX, then park on Internal so serial survives.
                if (m_Fainting)
                    return false;

                m_SyncingDeath = true;
                m_Fainting = true;
                SyncToBall();

                DudeData data = m_BoundBall.StoredDude;
                if (data != null)
                {
                    data.IsFainted = true;
                    data.Hits = 0;
                }

                Point3D loc = Location;
                Map map = Map;
                DudeType fxType = data != null ? data.Type : DudeType.Fire;
                string defId = data != null ? data.DefinitionId : m_DefinitionId;

                Mobile master = ControlMaster;
                SetControlMaster(null);
                Combatant = null;
                Warmode = false;
                Frozen = true;

                // Keep visible at 1 HP so death stays cancelled while FX plays.
                if (Hits < 1)
                    Hits = 1;

                TimeSpan delay = TimeSpan.Zero;
                if (map != null && map != Map.Internal)
                {
                    DudeSummonEffects.PlayDespawn(fxType, loc, map, defId);
                    delay = DudeSummonEffects.GetDespawnDuration(fxType, defId);
                }

                DudeBall ball = m_BoundBall;
                string dudeName = Name;

                Timer.DelayCall(delay, () =>
                {
                    FinishFaintPark(ball, master, dudeName);
                });

                m_SyncingDeath = false;
                return false;
            }

            return base.OnBeforeDeath();
        }

        private void FinishFaintPark(DudeBall ball, Mobile master, string dudeName)
        {
            m_Fainting = false;
            Frozen = false;

            if (Deleted)
                return;

            m_SyncingDeath = true;
            Internalize();
            m_SyncingDeath = false;

            if (ball != null && !ball.Deleted)
                ball.InvalidateProperties();

            if (master != null && !master.Deleted)
                master.SendMessage(0x22, "{0} fainted and returned to the Dude Ball!", dudeName);
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
            writer.Write((int)1);

            writer.Write(m_DefinitionId);
            writer.Write(m_IsWild);
            writer.Write(m_BoundBall);
            writer.Write(m_NextAbilityTime);
            writer.Write(m_DudeLevel);
            writer.Write(m_AbilityId);
            writer.Write(m_EvolutionStage);
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

            if (version >= 1)
                m_EvolutionStage = reader.ReadInt();
            else
                m_EvolutionStage = 1;

            DudeRegistry.EnsureInitialized();
            DudeAbilityRegistry.EnsureInitialized();

            ApplyDudeSpeeds();
        }
    }
}
