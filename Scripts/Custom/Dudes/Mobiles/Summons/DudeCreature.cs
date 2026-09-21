using System;
using System.Collections.Generic;
using Server.Custom.Dudes;
using Server.Items;

namespace Server.Mobiles
{
    /// <summary>
    /// World representation of a Dude (wild or summoned).
    /// Persistent data lives on DudeBall; this creature mirrors it while out.
    /// Classic BaseCreature stats / AI_Melee / ControlSlots for UOR pet rules.
    /// Split like the stock summons: this file (definition, stats, persistence),
    /// DudeCreature.Summon.cs (summon lifecycle), DudeCreature.Inventory.cs (gear / paperdoll),
    /// DudeCreature.Abilities.cs (ability casting) and Mobiles/AI/DudeCreature.Behavior.cs (pack AI).
    /// Abilities themselves live in Abilities/(Fire|Water|Earth|Air)/.
    /// </summary>
    [CorpseName("a dude corpse")]
    public partial class DudeCreature : BaseCreature
    {
        private string m_DefinitionId;
        private bool m_IsWild;
        private DudeBall m_BoundBall;
        private DateTime m_NextAbilityTime;
        private int m_DudeLevel;
        private string m_AbilityId;
        private int m_EvolutionStage;
        private DateTime m_NextBurnPulse;

        private const double DudeForceSpeed = 0.1;

        [Constructable]
        public DudeCreature()
            : this("ember", true)
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
            m_EquippedGear = new List<DudeGear>();
            m_EquippedAbilityIds = new List<string>();

            DudeDefinition def = DudeRegistry.Get(definitionId);
            if (def == null)
                def = DudeRegistry.GetByType(DudeType.Fire);

            ApplyDefinition(def);

            // Species ControlSlots mirrors stage (1/2/3) for named / wild spawns.
            if (def != null && def.ControlSlots >= 1 && def.ControlSlots <= 3)
                m_EvolutionStage = def.ControlSlots;

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
                ControlSlots = DudeRegistry.GetControlSlots(def != null ? def.Id : m_DefinitionId, 1);
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
        /// Summoned Dudes keep full AI move delay when Stam/Hits are low.
        /// Wild Dudes keep stock SpeedInfo.TransformMoveDelay wound slowdown.
        /// </summary>
        public override bool ReduceSpeedWithDamage
        {
            get { return m_IsWild; }
        }

        /// <summary>
        /// If movement ever goes through Mobile.ComputeMovementSpeed, summoned Dudes
        /// still use the forced delay (ms) instead of foot/mount tables.
        /// </summary>
        public override int ComputeMovementSpeed(Direction dir, bool checkTurning)
        {
            if (m_IsWild)
                return base.ComputeMovementSpeed(dir, checkTurning);

            double speed = ForceActiveSpeed > 0.0 ? ForceActiveSpeed : DudeForceSpeed;
            return Math.Max(1, (int)(speed * 1000.0));
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

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            DudeData data = null;
            if (m_BoundBall != null && !m_BoundBall.Deleted && m_BoundBall.StoredDude != null)
                data = m_BoundBall.StoredDude;

            int level = data != null ? data.Level : m_DudeLevel;
            if (level < 1)
                level = 1;

            list.Add("Level {0}", level);

            // Wild / no owner data: Level only (hide EXP bar).
            bool showExp = data != null && m_BoundBall != null && !m_BoundBall.Deleted && !m_IsWild && Controlled;
            if (!showExp)
                return;

            int cur = data.CurrentEXP;
            int need = data.EXPToNext < 1 ? 1 : data.EXPToNext;
            int pct = cur * 100 / need;
            if (pct < 0)
                pct = 0;
            if (pct > 100)
                pct = 100;

            int width = 10;
            int filled = pct * width / 100;
            string bar = "[" + new string('#', filled) + new string('.', width - filled) + "]";
            list.Add("EXP {0} {1}%", bar, pct);
        }

        private ThrottledPropertyRefresh m_PropertyRefresh;

        /// <summary>At most one tooltip rebuild per 5s for per-kill EXP updates (see ThrottledPropertyRefresh).</summary>
        public void InvalidatePropertiesThrottled()
        {
            if (m_PropertyRefresh == null)
                m_PropertyRefresh = new ThrottledPropertyRefresh(InvalidateProperties, () => Deleted, TimeSpan.FromSeconds(5.0));

            m_PropertyRefresh.Request();
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

            Name = ResolveDudeName(null, def);
            ApplyHumanMaleAppearance(def);
            // Starter type sash only on first wild spawn — never re-equip if owner removed it.
            if (m_IsWild)
                EnsureTypeSash(def);
            BaseSoundID = def.BaseSoundID;

            // One-time IV-style variance for wild / freshly defined Dudes.
            int strMod = Utility.RandomMinMax(-4, 4);
            int dexMod = Utility.RandomMinMax(-4, 4);
            int intMod = Utility.RandomMinMax(-4, 4);
            SetStr(Math.Max(1, def.Str + strMod));
            SetDex(Math.Max(1, def.Dex + dexMod));
            SetInt(Math.Max(1, def.Int + intMod));

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
            ApplyControlSlots(DudeRegistry.GetControlSlots(def.Id, m_EvolutionStage));

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
                BaseSoundID = def.BaseSoundID;
                m_DefinitionId = def.Id;
            }

            Name = ResolveDudeName(data, def);
            ApplyHumanMaleAppearance(def, data);
            m_DudeLevel = data.Level;
            m_AbilityId = data.AbilityId;
            m_EvolutionStage = data.EvolutionStage;
            m_IsWild = false;

            SetStr(data.Str);
            SetDex(data.Dex);
            SetInt(data.Int);

            // Set HitsMaxSeed directly — SetHits() always assigns Hits = HitsMax (full heal side-effect).
            HitsMaxSeed = Math.Max(1, data.HitsMax);

            if (fullHeal || data.IsFainted)
                Hits = HitsMax;
            else
                Hits = Math.Max(1, Math.Min(data.Hits, HitsMax));

            SetMana(30 + (data.Level * 2));
            Mana = ManaMax;

            SetDamage(data.MinDamage, data.MaxDamage);
            VirtualArmor = data.VirtualArmor;

            ApplyControlSlots(DudeRegistry.GetControlSlots(data));

            ApplyCombatSkills(data);
            ApplyDudeSpeeds();
            CleanupDuplicateTypeSashes();
            RebuildGearCache();
        }

        /// <summary>
        /// All Dudes are human males; type is shown by shorts hue (def.Hue) and starter sash.
        /// Skin uses RandomSkinHue (or persisted DudeData.SkinHue). Never Hue = 0 after apply.
        /// </summary>
        public void ApplyHumanMaleAppearance(DudeDefinition def, DudeData data = null)
        {
            Body = 0x190;
            Female = false;
            EnsureTypeShorts(def);
            // EnsureTypeSash: wild ApplyDefinition only — do not re-equip on ApplyData / Deserialize / Refresh.

            if (data != null && data.SkinHue > 0)
            {
                Hue = data.SkinHue;
            }
            else if (data != null)
            {
                Hue = Utility.RandomSkinHue();
                data.SkinHue = Hue;
            }
            else if (Hue <= 0)
            {
                // Wild / first spawn (or legacy Hue 0). Preserve Hue after world load.
                Hue = Utility.RandomSkinHue();
            }
        }

        /// <summary>
        /// Display name "{Name} Dude". Uses data.DisplayName when present; if that has no
        /// "Dude" suffix and matches the species name, uses def.Name + " Dude".
        /// </summary>
        public static string ResolveDudeName(DudeData data, DudeDefinition def)
        {
            string display = data != null ? data.DisplayName : null;
            string species = def != null ? def.Name : null;

            if (string.IsNullOrEmpty(display))
            {
                if (!string.IsNullOrEmpty(species))
                    return species + " Dude";
                return "Dude";
            }

            if (HasDudeSuffix(display))
                return display;

            if (!string.IsNullOrEmpty(species)
                && string.Equals(display, species, StringComparison.OrdinalIgnoreCase))
                return species + " Dude";

            return display;
        }

        private static bool HasDudeSuffix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name.EndsWith(" Dude", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Dude", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Apply stored DudeData combat skills (Wrestling/Tactics/Anatomy/MagicResist), capped at 100.
        /// Wild (no data): roll 40–60 each. Stage GetCombatSkillCap is unused for these four.
        /// </summary>
        public void ApplyCombatSkills(DudeData data)
        {
            if (data != null)
            {
                DudeCombatSkills.EnsureRolled(data);
                SetSkill(SkillName.Wrestling, DudeCombatSkills.Clamp(data.Wrestling));
                SetSkill(SkillName.Tactics, DudeCombatSkills.Clamp(data.Tactics));
                SetSkill(SkillName.Anatomy, DudeCombatSkills.Clamp(data.Anatomy));
                SetSkill(SkillName.MagicResist, DudeCombatSkills.Clamp(data.MagicResist));
                return;
            }

            SetSkill(SkillName.Wrestling, DudeCombatSkills.Roll());
            SetSkill(SkillName.Tactics, DudeCombatSkills.Roll());
            SetSkill(SkillName.Anatomy, DudeCombatSkills.Roll());
            SetSkill(SkillName.MagicResist, DudeCombatSkills.Roll());
        }

        /// <summary>Legacy name — redirects to ApplyCombatSkills with stage 1.</summary>
        public void ApplyLevelSkills(int level)
        {
            ApplyCombatSkills(null);
        }

        /// <summary>
        /// Sets ControlSlots and adjusts master's Followers if already controlled
        /// (needed when evolving live Embit→Emberon→Infernox).
        /// </summary>
        public void ApplyControlSlots(int slots)
        {
            if (slots < 1)
                slots = 1;

            int old = ControlSlots;
            if (old == slots)
                return;

            Mobile master = ControlMaster;
            if (Controlled && master != null && !master.Deleted)
            {
                master.Followers -= old;
                if (master.Followers < 0)
                    master.Followers = 0;

                ControlSlots = slots;
                master.Followers += slots;
            }
            else
            {
                ControlSlots = slots;
            }
        }

        public override void GenerateLoot()
        {
            base.GenerateLoot();

            if (!m_IsWild)
                return;

            // Guaranteed Dude Dust: 5–10 × EvolutionStage (wilds are stage 1 → 5–10).
            int stage = m_EvolutionStage;
            if (stage < 1)
                stage = 1;
            int dustMin = 5 * stage;
            int dustMax = 10 * stage;
            PackItem(new DudeDust(Utility.RandomMinMax(dustMin, dustMax)));

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

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)2);

            writer.Write(m_DefinitionId);
            writer.Write(m_IsWild);
            writer.Write(m_BoundBall);
            writer.Write(m_NextAbilityTime);
            writer.Write(m_DudeLevel);
            writer.Write(m_AbilityId);
            writer.Write(m_EvolutionStage);
            writer.Write(m_NextBurnPulse);
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

            if (version >= 2)
                m_NextBurnPulse = reader.ReadDateTime();
            else
                m_NextBurnPulse = DateTime.UtcNow;

            DudeRegistry.EnsureInitialized();
            DudeAbilityRegistry.EnsureInitialized();

            DudeDefinition loaded = DudeRegistry.Get(m_DefinitionId);
            if (loaded != null && string.IsNullOrEmpty(Name))
                Name = ResolveDudeName(null, loaded);
            else if (loaded != null && !HasDudeSuffix(Name)
                && string.Equals(Name, loaded.Name, StringComparison.OrdinalIgnoreCase))
                Name = loaded.Name + " Dude";

            ApplyDudeSpeeds();

            if (m_EquippedGear == null)
                m_EquippedGear = new List<DudeGear>();
            if (m_EquippedAbilityIds == null)
                m_EquippedAbilityIds = new List<string>();

            // Mobiles deserialize before items, so every worn item still reads Layer.Invalid here.
            // Doing appearance/gear work now made EnsureTypeShorts add a new pair of shorts on every
            // restart and rebuilt the gear cache from blank items (dropping hat/shield skills and
            // costume bodies). Run it once the world has finished loading instead.
            Timer.DelayCall(TimeSpan.Zero, new TimerCallback(AfterWorldLoad));
        }

        private void AfterWorldLoad()
        {
            if (Deleted)
                return;

            RemoveDuplicateTypeShorts();
            ApplyHumanMaleAppearance(DudeRegistry.Get(m_DefinitionId));
            RebuildGearCache();
        }
    }
}
