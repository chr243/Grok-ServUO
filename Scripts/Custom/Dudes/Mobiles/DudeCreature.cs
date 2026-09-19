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
        private bool m_Despawning;
        private bool m_BlessedBeforeDespawn;
        private DateTime m_NextBurnPulse;
        private DateTime m_NextSpringPulse;
        private DateTime m_NextFaultlinePulse;
        private DateTime m_NextFollowSpread;
        private List<DudeGear> m_EquippedGear;
        private List<string> m_EquippedAbilityIds;

        /// <summary>
        /// Allowed DudeGear paperdoll layers (order is documentation only — slot budget is a count).
        /// Helm = Magical Dude Hat; InnerTorso = sash; Earrings = type earrings; Bracelet = bracers.
        /// TwoHanded (Dude Shield) allowed separately via CanAcceptGear; still costs a slot.
        /// Pants reserved for type shorts.
        /// </summary>
        public static readonly Layer[] GearLayerOrder = new Layer[]
        {
            Layer.Helm,        // hat
            Layer.InnerTorso,  // sash
            Layer.Earrings,    // type earrings (was circlet)
            Layer.Bracelet     // bracers
        };

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

        /// <summary>True while recall/despawn FX plays — no aggro, not a guard candidate.</summary>
        public bool IsDespawning
        {
            get { return m_Despawning; }
        }

        public void BeginDespawnSequence()
        {
            m_Despawning = true;
            m_BlessedBeforeDespawn = Blessed;
            Blessed = true;
            Combatant = null;
            Warmode = false;
            Frozen = true;
            Criminal = false;
            ControlOrder = OrderType.Stay;
            if (ControlMaster != null)
                ControlTarget = ControlMaster;
            FocusMob = null;
        }

        public void EndDespawnSequence()
        {
            m_Despawning = false;
            Blessed = m_BlessedBeforeDespawn;
            Frozen = false;
            Combatant = null;
            Warmode = false;
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
        /// Equip (or force) type shorts with Hue = def.Hue. Replace if wrong hue / wrong item.
        /// </summary>
        public void EnsureTypeShorts(DudeDefinition def)
        {
            int hue = def != null ? def.Hue : 0;

            Item existing = FindItemOnLayer(Layer.Pants);
            DudeTypeShorts shorts = existing as DudeTypeShorts;

            if (shorts != null)
            {
                if (shorts.Hue != hue)
                    shorts.Hue = hue;
                shorts.Name = "Type shorts";
                shorts.LootType = LootType.Blessed;
                shorts.Movable = false;
                return;
            }

            if (existing != null)
                existing.Delete();

            // Also clear a kilt on OuterLegs if somehow present.
            Item outer = FindItemOnLayer(Layer.OuterLegs);
            if (outer is Kilt)
                outer.Delete();

            shorts = new DudeTypeShorts(hue);
            AddItem(shorts);
        }

        /// <summary>
        /// Equip matching type sash on InnerTorso if none already (server AddItem; bypasses wild CanAcceptGear).
        /// Call only from wild ApplyDefinition (first spawn). Never from ApplyData / appearance / Refresh / Deserialize.
        /// Never AddItem a second sash; never drop a new sash into the backpack.
        /// Blessed; Movable so the owner can lift it into the backpack.
        /// </summary>
        public void EnsureTypeSash(DudeDefinition def)
        {
            DudeType type = def != null ? def.Type : DudeType.Fire;

            Item existing = FindItemOnLayer(Layer.InnerTorso);

            // Already wearing any DudeGear (includes matching type sash) — do nothing.
            if (existing is DudeGear)
                return;

            // Matching type sash already on the layer — do nothing.
            if (IsMatchingTypeSash(existing, type))
                return;

            // Clear non-gear / wrong item on InnerTorso only (never a second sash).
            if (existing != null)
            {
                existing.Delete();
                existing = FindItemOnLayer(Layer.InnerTorso);
                if (existing is DudeGear || IsMatchingTypeSash(existing, type))
                    return;
            }

            // Re-use an owned matching sash (orphan on Items or in backpack) instead of spawning another.
            DudeGear owned = FindOwnedMatchingTypeSash(type);
            if (owned != null)
            {
                owned.LootType = LootType.Blessed;
                owned.Movable = true;

                // Already the worn layer item — done.
                if (FindItemOnLayer(Layer.InnerTorso) == owned)
                    return;

                // Move from backpack (or re-assert on mobile) onto InnerTorso. Never create a second.
                if (owned.Parent != this)
                    AddItem(owned);
                return;
            }

            DudeGear sash = CreateTypeSash(type);
            if (sash == null)
                return;

            sash.LootType = LootType.Blessed;
            sash.Movable = true;
            AddItem(sash);
        }

        /// <summary>
        /// Leave non-worn DudeGear for the player (Manual ignores them via FillEquippedGear).
        /// Blessed starter type-sash duplicates of the worn sash are deleted.
        /// </summary>
        public void CleanupDuplicateTypeSashes()
        {
            Item worn = FindItemOnLayer(Layer.InnerTorso);
            if (!IsTypeSash(worn))
                worn = null;

            List<Item> toDelete = null;

            for (int i = 0; i < Items.Count; i++)
            {
                DudeGear gear = Items[i] as DudeGear;
                if (gear == null || gear.Deleted)
                    continue;
                if (FindItemOnLayer(gear.Layer) == gear)
                    continue;

                // Loose / layer-orphan on the mobile: keep unless blessed starter sash dupe of worn.
                if (worn != null && IsBlessedStarterTypeSashDuplicate(gear, worn))
                {
                    if (toDelete == null)
                        toDelete = new List<Item>();
                    toDelete.Add(gear);
                }
            }

            Container pack = Backpack;
            if (pack != null && worn != null)
            {
                for (int i = 0; i < pack.Items.Count; i++)
                {
                    Item item = pack.Items[i];
                    if (item == null || item.Deleted)
                        continue;
                    if (!IsBlessedStarterTypeSashDuplicate(item, worn))
                        continue;
                    if (toDelete == null)
                        toDelete = new List<Item>();
                    toDelete.Add(item);
                }
            }

            if (toDelete == null)
                return;

            for (int i = 0; i < toDelete.Count; i++)
            {
                Item item = toDelete[i];
                if (item != null && !item.Deleted)
                    item.Delete();
            }
        }

        private DudeGear FindOwnedMatchingTypeSash(DudeType type)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Item item = Items[i];
                if (item == null || item.Deleted)
                    continue;
                if (IsMatchingTypeSash(item, type))
                    return (DudeGear)item;
            }

            Container pack = Backpack;
            if (pack != null)
            {
                for (int i = 0; i < pack.Items.Count; i++)
                {
                    Item item = pack.Items[i];
                    if (item == null || item.Deleted)
                        continue;
                    if (IsMatchingTypeSash(item, type))
                        return (DudeGear)item;
                }
            }

            return null;
        }

        private static bool IsTypeSash(Item item)
        {
            return item is EmberSash || item is TideSash || item is StoneSash || item is GaleSash;
        }

        private static bool IsMatchingTypeSash(Item item, DudeType type)
        {
            if (item == null || item.Deleted)
                return false;

            switch (type)
            {
                case DudeType.Water:
                    return item is TideSash;
                case DudeType.Earth:
                    return item is StoneSash;
                case DudeType.Air:
                    return item is GaleSash;
                default:
                    return item is EmberSash;
            }
        }

        private static bool IsBlessedStarterTypeSashDuplicate(Item candidate, Item worn)
        {
            if (candidate == null || worn == null || candidate == worn || candidate.Deleted)
                return false;
            if (candidate.LootType != LootType.Blessed)
                return false;
            if (!IsTypeSash(candidate) || !IsTypeSash(worn))
                return false;
            return candidate.GetType() == worn.GetType();
        }

        private static DudeGear CreateTypeSash(DudeType type)
        {
            switch (type)
            {
                case DudeType.Water:
                    return new TideSash();
                case DudeType.Earth:
                    return new StoneSash();
                case DudeType.Air:
                    return new GaleSash();
                default:
                    return new EmberSash();
            }
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

        public void SyncToBall()
        {
            if (m_BoundBall == null || m_BoundBall.Deleted || m_BoundBall.StoredDude == null)
                return;

            DudeData data = m_BoundBall.StoredDude;
            data.Hits = Hits;
            // Persist seed, not HitsMax property (seed + Str offset), to avoid inflation.
            data.HitsMax = HitsMaxSeed > 0 ? HitsMaxSeed : HitsMax;
            data.Str = RawStr;
            data.Dex = RawDex;
            data.Int = RawInt;
            data.MinDamage = DamageMin;
            data.MaxDamage = DamageMax;
            data.VirtualArmor = VirtualArmor;
            data.Level = m_DudeLevel;
            data.CustomName = Name;
            if (Hue > 0)
                data.SkinHue = Hue;
            DudeCombatSkills.WriteFromMobile(this, data);
            m_BoundBall.InvalidateProperties();
        }

        /// <summary>
        /// Town guards focusing a summoned Dude must not trigger pet AI fight-back
        /// (Combatant ↔ Guard Focus ↔ DoHarmful recursion / StackOverflow).
        /// </summary>
        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            if (m_Despawning)
            {
                // Still record aggression lists via base Mobile path without pet AI fight-back.
                // Avoid Combatant assignment against anyone while returning to ball.
                IDamageable old = Combatant;
                base.AggressiveAction(aggressor, criminal);
                Combatant = null;
                Warmode = false;
                return;
            }

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

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (m_IsWild || m_Fainting || m_Despawning)
                return;

            if (m_BoundBall == null || m_BoundBall.Deleted || m_BoundBall.StoredDude == null)
                return;

            DudeCombatSkills.TryGainOnHit(m_BoundBall.StoredDude, m_BoundBall, this);
        }

        public override void OnThink()
        {
            base.OnThink();

            if (m_IsWild || Deleted || Map == null || Map == Map.Internal || m_Fainting)
                return;

            if (m_Despawning)
            {
                Combatant = null;
                Warmode = false;
                return;
            }

            if (m_BoundBall != null && !m_BoundBall.Deleted && m_BoundBall.IsRecalling)
                return;

            // Owner died (or deleted) — auto return to ball.
            if (Controlled && ControlMaster != null && (ControlMaster.Deleted || !ControlMaster.Alive))
            {
                if (m_BoundBall != null && !m_BoundBall.Deleted)
                    m_BoundBall.Recall(ControlMaster);
                return;
            }

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

            TrySpreadFollowOffset();
            TrySpreadAttackOffset();
            TryUseAbility();
            TryStage3Passives();
        }

        private static readonly int[] FollowRingDX = { 1, 1, 0, -1, -1, -1, 0, 1 };
        private static readonly int[] FollowRingDY = { 0, 1, 1, 1, 0, -1, -1, -1 };

        /// <summary>
        /// Spread summoned followers onto ring tiles around the master so they do not stack.
        /// Only while Follow/Guard and follow speed is active — never overrides Attack/combat.
        /// </summary>
        private void TrySpreadFollowOffset()
        {
            if (ControlOrder != OrderType.Follow && ControlOrder != OrderType.Guard)
                return;

            // Follow pace only (do not override Attack / stay / stop).
            if (CurrentSpeed != ActiveSpeed)
                return;

            // Leave Guard combat alone — attack spread handles that.
            Mobile combatant = Combatant as Mobile;
            if (combatant != null && !combatant.Deleted && combatant.Alive)
                return;

            DateTime now = DateTime.UtcNow;
            if (now < m_NextFollowSpread)
                return;
            m_NextFollowSpread = now + TimeSpan.FromSeconds(1.0);

            Mobile master = ControlMaster;
            if (master == null || master.Deleted || Map == null || Map != master.Map)
                return;

            int index;
            int packCount;
            if (!TryGetPackIndex(master, out index, out packCount))
                return;

            Point3D dest;
            if (!TryResolveRingOffset(master.Location, master.Map, master.Z, index, packCount, out dest))
                return;

            // Already on a valid offset tile — do not shuffle every tick.
            if (X == dest.X && Y == dest.Y)
                return;

            if (AIObject != null)
                AIObject.WalkMobileRange(dest, 1, false, 0, 0);
        }

        /// <summary>
        /// Spread attacking Dudes onto ring tiles around the combatant so they do not stack
        /// on the target tile. Only micro-adjusts when already near the target (within 3).
        /// Does not cancel Attack or change RangeFight.
        /// </summary>
        private void TrySpreadAttackOffset()
        {
            Mobile target = Combatant as Mobile;
            if (target == null || target.Deleted || !target.Alive)
            {
                if (ControlOrder != OrderType.Attack)
                    return;

                target = ControlTarget as Mobile;
                if (target == null || target.Deleted || !target.Alive)
                    return;
            }

            if (Map == null || target.Map != Map)
                return;

            // Only micro-adjust when already near — do not pull across the map.
            if (!InRange(target, 3))
                return;

            DateTime now = DateTime.UtcNow;
            if (now < m_NextFollowSpread)
                return;
            m_NextFollowSpread = now + TimeSpan.FromSeconds(1.0);

            Mobile master = ControlMaster;
            if (master == null || master.Deleted)
                return;

            int index;
            int packCount;
            if (!TryGetPackIndex(master, out index, out packCount))
                return;

            Point3D dest;
            if (!TryResolveRingOffset(target.Location, target.Map, target.Z, index, packCount, out dest))
                return;

            // Already on a valid adjacent/offset tile — do not shuffle every tick.
            if (X == dest.X && Y == dest.Y)
                return;

            // Do not kite out of melee: if already hitting, stay unless dest is also melee-range.
            if (InRange(target, 1) && !target.InRange(dest, 1))
                return;

            if (AIObject != null)
                AIObject.WalkMobileRange(dest, 1, false, 0, 0);
        }

        private bool TryGetPackIndex(Mobile master, out int index, out int packCount)
        {
            index = -1;
            packCount = 0;

            List<DudeCreature> pack = CollectPackDudes(master);
            if (pack.Count == 0)
                return false;

            pack.Sort(delegate(DudeCreature a, DudeCreature b)
            {
                return a.Serial.CompareTo(b.Serial);
            });

            for (int i = 0; i < pack.Count; i++)
            {
                if (pack[i] == this)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
                return false;

            packCount = pack.Count;
            return true;
        }

        private static List<DudeCreature> CollectPackDudes(Mobile master)
        {
            List<DudeCreature> pack = new List<DudeCreature>();
            PlayerMobile pm = master as PlayerMobile;
            List<Mobile> followers = pm != null ? pm.AllFollowers : null;
            if (followers == null)
                return pack;

            for (int i = 0; i < followers.Count; i++)
            {
                DudeCreature dude = followers[i] as DudeCreature;
                if (dude == null || dude.Deleted || dude.IsWild)
                    continue;
                if (!dude.Controlled || dude.ControlMaster != master)
                    continue;
                if (dude.Map != master.Map || !dude.InRange(master, 18))
                    continue;
                pack.Add(dude);
            }
            return pack;
        }

        /// <summary>
        /// Resolve a walkable 8-way ring tile around <paramref name="center"/> for pack index.
        /// Prefers distance 1; if blocked, next ring slot; then outer rings as pack grows.
        /// </summary>
        private static bool TryResolveRingOffset(Point3D center, Map map, int centerZ, int index, int packCount, out Point3D dest)
        {
            dest = Point3D.Zero;
            if (map == null || map == Map.Internal)
                return false;

            int rings = Math.Max(1, (packCount + 7) / 8);
            int slotCount = rings * 8;

            for (int attempt = 0; attempt < slotCount; attempt++)
            {
                int slot = (index + attempt) % slotCount;
                int ring = (slot / 8) + 1;
                int dir = slot % 8;
                int x = center.X + FollowRingDX[dir] * ring;
                int y = center.Y + FollowRingDY[dir] * ring;
                int z = centerZ;

                Point3D p = new Point3D(x, y, z);
                if (map.CanFit(p, 16, false, false))
                {
                    dest = p;
                    return true;
                }

                int avgZ = map.GetAverageZ(x, y);
                if (avgZ != z)
                {
                    p = new Point3D(x, y, avgZ);
                    if (map.CanFit(p, 16, false, false))
                    {
                        dest = p;
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsOwnerOrStaff(Mobile m)
        {
            if (m == null || m.Deleted)
                return false;
            if (m.AccessLevel >= AccessLevel.GameMaster)
                return true;
            return ControlMaster == m;
        }

        /// <summary>Gear slots unlocked by evolution stage (via ball data when present).</summary>
        public int GetGearSlotCount()
        {
            if (m_BoundBall != null && !m_BoundBall.Deleted && m_BoundBall.StoredDude != null)
                return m_BoundBall.StoredDude.GetGearSlots();
            return DudeExperience.GetGearSlots(EvolutionStage);
        }

        /// <summary>
        /// True if layer is one of the allowed DudeGear layers (or TwoHanded shield).
        /// Does not check slot budget — use CanAcceptGear for that.
        /// </summary>
        public bool IsGearLayerAllowed(Layer layer)
        {
            if (GetGearSlotCount() <= 0)
                return false;

            if (layer == Layer.TwoHanded)
                return true;

            for (int i = 0; i < GearLayerOrder.Length; i++)
            {
                if (GearLayerOrder[i] == layer)
                    return true;
            }
            return false;
        }

        /// <summary>Stage required to have at least <paramref name="slotsNeeded"/> gear slots.</summary>
        private static int StageNeededForSlotCount(int slotsNeeded)
        {
            // 1–2 slots → stage 1; 3 → stage 2; 4+ → stage 3
            if (slotsNeeded <= 2)
                return 1;
            if (slotsNeeded == 3)
                return 2;
            return 3;
        }

        private int GetEquippedGearSlotCost(DudeGear exclude)
        {
            int used = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                DudeGear gear = Items[i] as DudeGear;
                if (gear == null || gear == exclude || gear.Deleted)
                    continue;
                // Backpack / loose Items do not consume gear slots — only the layer winner.
                if (FindItemOnLayer(gear.Layer) != gear)
                    continue;
                used += gear.SlotCost;
            }
            return used;
        }

        /// <summary>
        /// Validates DudeGear for this Dude. On failure sets reason
        /// (e.g. "Needs stage X for another slot.").
        /// </summary>
        public bool CanAcceptGear(DudeGear gear, out string reason)
        {
            reason = null;
            if (gear == null || gear.Deleted)
            {
                reason = "That gear is invalid.";
                return false;
            }

            if (m_IsWild)
            {
                reason = "Wild Dudes cannot wear gear.";
                return false;
            }

            Layer layer = gear.Layer;
            bool isTwoHanded = (layer == Layer.TwoHanded);
            bool layerOk = isTwoHanded;
            if (!layerOk)
            {
                for (int i = 0; i < GearLayerOrder.Length; i++)
                {
                    if (GearLayerOrder[i] == layer)
                    {
                        layerOk = true;
                        break;
                    }
                }
            }

            if (!layerOk)
            {
                reason = "That gear uses an invalid slot.";
                return false;
            }

            // Slot budget is a count of SlotCost, not a fixed layer ladder.
            // Stage 1 may wear any two allowed layers (hat+sash, sash+earrings, hat+shield, etc.).
            int allowed = GetGearSlotCount();
            if (allowed < 0)
                allowed = 0;

            int used = GetEquippedGearSlotCost(gear);
            int cost = gear.SlotCost;
            if (used + cost > allowed)
            {
                int stageNeeded = StageNeededForSlotCount(used + cost);
                reason = string.Format("Needs stage {0} for another slot.", stageNeeded);
                return false;
            }

            if (gear.HasRequiredType)
            {
                DudeType dudeType = DudeType.Fire;
                DudeDefinition def = DudeRegistry.Get(m_DefinitionId);
                if (def != null)
                    dudeType = def.Type;
                else if (m_BoundBall != null && m_BoundBall.StoredDude != null)
                    dudeType = m_BoundBall.StoredDude.Type;

                if (dudeType != gear.RequiredType)
                {
                    reason = string.Format("Only a {0} Dude can wear that.", gear.RequiredType);
                    return false;
                }
            }

            return true;
        }

        public void RebuildGearCache()
        {
            if (m_EquippedGear == null)
                m_EquippedGear = new List<DudeGear>();
            else
                m_EquippedGear.Clear();

            if (m_EquippedAbilityIds == null)
                m_EquippedAbilityIds = new List<string>();
            else
                m_EquippedAbilityIds.Clear();

            for (int i = 0; i < Items.Count; i++)
            {
                DudeGear gear = Items[i] as DudeGear;
                if (gear == null || gear.Deleted)
                    continue;
                // Ignore backpack / loose DudeGear — only paperdoll layer winners count.
                if (FindItemOnLayer(gear.Layer) != gear)
                    continue;
                m_EquippedGear.Add(gear);
                if (!string.IsNullOrEmpty(gear.AbilityId))
                    m_EquippedAbilityIds.Add(gear.AbilityId);
            }

            ApplyUniversalGearEffects();
        }

        /// <summary>Find equipped gear granting the given ability id (first match).</summary>
        public DudeGear FindEquippedGearByAbility(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return null;
            if (m_EquippedGear == null)
                RebuildGearCache();
            for (int i = 0; i < m_EquippedGear.Count; i++)
            {
                DudeGear gear = m_EquippedGear[i];
                if (gear == null || gear.Deleted)
                    continue;
                if (string.Equals(gear.AbilityId, abilityId, StringComparison.OrdinalIgnoreCase))
                    return gear;
            }
            return null;
        }

        /// <summary>
        /// Magical Dude Hat / Dude Shield: copy stored skills (Min stored, stage cap), switch AI.
        /// Stored item values never change here — only effective Base / AI.
        /// </summary>
        private void ApplyUniversalGearEffects()
        {
            double cap = DudeCombatSkills.GetCap(EvolutionStage);
            if (m_BoundBall != null && !m_BoundBall.Deleted && m_BoundBall.StoredDude != null)
                cap = DudeCombatSkills.GetCap(m_BoundBall.StoredDude);

            MagicalDudeHat hat = FindItemOnLayer(Layer.Helm) as MagicalDudeHat;
            if (hat != null && !hat.Deleted)
            {
                double mult = hat.GetEffectMultiplier();
                double magery = Math.Min(hat.Magery * mult, cap);
                double eval = Math.Min(hat.EvalInt * mult, cap);
                double med = Math.Min(hat.Meditation * mult, cap);
                DudeCombatSkills.SetGearCopySkill(this, SkillName.Magery, magery, cap);
                DudeCombatSkills.SetGearCopySkill(this, SkillName.EvalInt, eval, cap);
                DudeCombatSkills.SetGearCopySkill(this, SkillName.Meditation, med, cap);
                // Level 0 = full stored skills (x1.0); higher levels scale up then stage-cap.
                if (magery > 0.0 || eval > 0.0 || med > 0.0)
                {
                    if (AI != AIType.AI_Mage)
                        AI = AIType.AI_Mage;
                }
                else if (AI != AIType.AI_Melee)
                {
                    AI = AIType.AI_Melee;
                }
            }
            else
            {
                DudeCombatSkills.SetGearCopySkill(this, SkillName.Magery, 0.0, cap);
                DudeCombatSkills.SetGearCopySkill(this, SkillName.EvalInt, 0.0, cap);
                DudeCombatSkills.SetGearCopySkill(this, SkillName.Meditation, 0.0, cap);
                if (AI != AIType.AI_Melee)
                    AI = AIType.AI_Melee;
            }

            DudeShield shield = FindItemOnLayer(Layer.TwoHanded) as DudeShield;
            if (shield != null && !shield.Deleted)
            {
                double parry = Math.Min(shield.Parrying * shield.GetEffectMultiplier(), cap);
                DudeCombatSkills.SetGearCopySkill(this, SkillName.Parry, parry, cap);
            }
            else
                DudeCombatSkills.SetGearCopySkill(this, SkillName.Parry, 0.0, cap);
        }

        /// <summary>Rebuild equipped-ability cache from worn layers only. Does not ApplyData / EnsureTypeSash / AddItem.</summary>
        public void Refresh()
        {
            RebuildGearCache();
        }

        public override void OnItemAdded(Item item)
        {
            base.OnItemAdded(item);
            if (item is DudeGear)
                RebuildGearCache();
        }

        public override void OnItemRemoved(Item item)
        {
            base.OnItemRemoved(item);
            if (item is DudeGear)
                RebuildGearCache();
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            if (IsOwnerOrStaff(from))
                return true;
            return base.AllowEquipFrom(from);
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            if (item is DudeTypeShorts)
                return false;

            if (IsOwnerOrStaff(from) && item is DudeGear)
                return true;

            return base.CheckNonlocalLift(from, item);
        }

        public override bool OnEquip(Item item)
        {
            if (item is DudeTypeShorts)
                return base.OnEquip(item);

            DudeGear gear = item as DudeGear;
            if (gear == null)
                return false;

            string reason;
            if (!CanAcceptGear(gear, out reason))
            {
                Mobile notify = ControlMaster;
                if (notify != null && !notify.Deleted && !string.IsNullOrEmpty(reason))
                    notify.SendMessage(reason);
                return false;
            }

            return base.OnEquip(item);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || from.Deleted)
                return;

            // Owner / staff: open paperdoll (do not fight).
            if (IsOwnerOrStaff(from))
            {
                DisplayPaperdollTo(from);
                return;
            }

            base.OnDoubleClick(from);
        }

        /// <summary>Combat abilities from equipped DudeGear cache.</summary>
        private List<string> GetUnlockedAbilityIds()
        {
            if (m_EquippedGear == null || m_EquippedAbilityIds == null)
                RebuildGearCache();
            return m_EquippedAbilityIds;
        }

        private List<DudeGear> GetEquippedGear()
        {
            if (m_EquippedGear == null)
                RebuildGearCache();
            return m_EquippedGear;
        }

        public static bool IsPassiveAbilityId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            return string.Equals(id, "burn", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "spring", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "faultline", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "slipstream", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsSupportAbilityId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return false;
            return string.Equals(id, "tide_mend", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "tailwind_self", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "tide_chorus", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "tailwind", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "aftershock", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "ring_of_fire", StringComparison.OrdinalIgnoreCase);
        }

        private bool HasUnlockedAbility(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return false;

            List<string> ids = GetUnlockedAbilityIds();
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], abilityId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private void TryUseAbility()
        {
            // Abilities come from equipped DudeGear cache (with per-piece effect multiplier).
            List<DudeGear> gears = GetEquippedGear();
            if (gears.Count == 0)
                return;

            if (m_Fainting || Frozen)
                return;

            Mobile target = Combatant as Mobile;
            if (target == null || target.Deleted || !target.Alive)
                return;

            if (!CanBeHarmful(target))
                return;

            if (m_NextAbilityById == null)
                m_NextAbilityById = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            DudeGear slipGear = FindEquippedGearByAbility("slipstream");
            bool slipstream = slipGear != null;
            double slipMult = slipGear != null ? slipGear.GetEffectMultiplier() : 1.0;

            for (int i = 0; i < gears.Count; i++)
            {
                DudeGear gear = gears[i];
                if (gear == null || gear.Deleted || string.IsNullOrEmpty(gear.AbilityId))
                    continue;

                DudeAbility ability = DudeAbilityRegistry.Get(gear.AbilityId);
                if (ability == null)
                    continue;

                // Passive stubs — combat handled by TryStage3Passives / Slipstream CD mod.
                if (IsPassiveAbilityId(ability.Id))
                    continue;

                DateTime readyAt;
                if (m_NextAbilityById.TryGetValue(ability.Id, out readyAt) && DateTime.UtcNow < readyAt)
                    continue;

                if (!IsSupportAbilityId(ability.Id))
                {
                    int range = 3;
                    if (!InRange(target, range))
                        continue;
                }
                else if (string.Equals(ability.Id, "ring_of_fire", StringComparison.OrdinalIgnoreCase))
                {
                    if (!InRange(target, RingOfFireAbility.AoERange))
                        continue;
                }

                if (!ability.CanExecute(this, target))
                    continue;

                if (ability.ManaCost > 0 && Mana < ability.ManaCost)
                    continue;

                if (ability.ManaCost > 0)
                    Mana -= ability.ManaCost;

                double prev = DudeAbility.CurrentEffectMultiplier;
                DudeAbility.CurrentEffectMultiplier = gear.GetEffectMultiplier();
                try
                {
                    ability.Execute(this, target);
                }
                finally
                {
                    DudeAbility.CurrentEffectMultiplier = prev;
                }

                TimeSpan cd = ability.Cooldown;
                if (slipstream)
                {
                    DudeAbilityConfig.EnsureLoaded();
                    DudeAbilityTune slip = DudeAbilityConfig.Get("slipstream");
                    double reduce = slip != null && slip.ReduceSeconds > 0.0 ? slip.ReduceSeconds : 2.0;
                    reduce *= slipMult;
                    double floor = slip != null && slip.FloorSeconds > 0.0 ? slip.FloorSeconds : 7.0;
                    cd = TimeSpan.FromSeconds(Math.Max(floor, cd.TotalSeconds - reduce));
                }

                m_NextAbilityById[ability.Id] = DateTime.UtcNow + cd;
                // Independent cooldowns — keep scanning remaining unlocked abilities.
            }
        }

        private void TryStage3Passives()
        {
            if (m_IsWild || m_Fainting || Frozen || m_Despawning || Deleted)
                return;

            Mobile combatant = Combatant as Mobile;
            bool inCombat = combatant != null && !combatant.Deleted && combatant.Alive;

            if (HasUnlockedAbility("burn"))
                TryBurnPassive(inCombat, combatant);

            if (HasUnlockedAbility("spring"))
                TrySpringPassive(inCombat);

            if (HasUnlockedAbility("faultline"))
                TryFaultlinePassive(inCombat);
        }

        private void TryBurnPassive(bool inCombat, Mobile combatant)
        {
            if (!inCombat)
                return;

            DudeDefinition burnDef = DudeRegistry.Get(m_DefinitionId);
            if (burnDef == null || burnDef.Type != DudeType.Fire)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("burn");
            double tick = tune != null && tune.TickSeconds > 0.0 ? tune.TickSeconds : 1.0;
            double hitChance = tune != null && tune.HitChance > 0.0 ? tune.HitChance : 0.5;
            double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.3;

            DateTime now = DateTime.UtcNow;
            if (now < m_NextBurnPulse)
                return;

            m_NextBurnPulse = now + TimeSpan.FromSeconds(tick);

            List<Mobile> candidates = new List<Mobile>();
            DudeAbilityVfx.CollectFightList(this, candidates);

            if (candidates.Count == 0)
                return;

            DudeGear burnGear = FindEquippedGearByAbility("burn");
            double prevBurn = DudeAbility.CurrentEffectMultiplier;
            DudeAbility.CurrentEffectMultiplier = burnGear != null ? burnGear.GetEffectMultiplier() : 1.0;
            int damage = DudeAbility.ApplyEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(m_DudeLevel) * vs)));
            DudeAbility.CurrentEffectMultiplier = prevBurn;
            bool anyHit = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (Utility.RandomDouble() >= hitChance)
                    continue;

                Mobile m = candidates[i];
                AOS.Damage(m, this, damage, 0, 100, 0, 0, 0);
                DudeAbilityVfx.PlayFireHit(m, false);
                anyHit = true;
            }

            if (anyHit)
            {
                PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Burn*");
                PlaySound(0x208);
            }
        }

        private void TrySpringPassive(bool inCombat)
        {
            if (!inCombat)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("spring");
            double tick = tune != null && tune.TickSeconds > 0.0 ? tune.TickSeconds : 2.0;
            double healFrac = tune != null && tune.HealHitsFraction > 0.0 ? tune.HealHitsFraction : 0.05;

            DateTime now = DateTime.UtcNow;
            if (now < m_NextSpringPulse)
                return;

            m_NextSpringPulse = now + TimeSpan.FromSeconds(tick);

            DudeGear springGear = FindEquippedGearByAbility("spring");
            double prevSpring = DudeAbility.CurrentEffectMultiplier;
            DudeAbility.CurrentEffectMultiplier = springGear != null ? springGear.GetEffectMultiplier() : 1.0;

            int blast = DudeExperience.GetBlastDamage(m_DudeLevel);
            int selfHeal = Math.Max(1, (int)(blast * 0.15));
            int pctHeal = Math.Max(1, (int)(HitsMax * healFrac));
            int heal = Math.Min(selfHeal, pctHeal);
            if (heal < 1)
                heal = 1;
            heal = DudeAbility.ApplyEffect(heal);

            Hits = Math.Min(HitsMax, Hits + heal);
            DudeAbilityVfx.PlayWaterHeal(this);

            // Heal owned DudeCreatures within range 2 via master's followers (no hostile scan).
            Mobile master = ControlMaster;
            if (master == null || master.Deleted)
            {
                DudeAbility.CurrentEffectMultiplier = prevSpring;
                return;
            }

            PlayerMobile pm = master as PlayerMobile;
            List<Mobile> followers = pm != null ? pm.AllFollowers : null;
            if (followers == null)
            {
                DudeAbility.CurrentEffectMultiplier = prevSpring;
                return;
            }

            for (int i = 0; i < followers.Count; i++)
            {
                DudeCreature ally = followers[i] as DudeCreature;
                if (ally == null || ally == this || ally.Deleted || !ally.Alive)
                    continue;
                if (ally.Map != Map)
                    continue;
                if (!InRange(ally, 2))
                    continue;

                int allyPct = Math.Max(1, (int)(ally.HitsMax * healFrac));
                int allyHeal = Math.Min(Math.Max(1, (int)(blast * 0.15)), allyPct);
                allyHeal = DudeAbility.ApplyEffect(allyHeal);
                ally.Hits = Math.Min(ally.HitsMax, ally.Hits + allyHeal);
                DudeAbilityVfx.PlayWaterHeal(ally);
            }

            DudeAbility.CurrentEffectMultiplier = prevSpring;
        }

        private void TryFaultlinePassive(bool inCombat)
        {
            if (!inCombat)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("faultline");
            double gap = tune != null && tune.GapSeconds > 0.0 ? tune.GapSeconds : 10.0;
            double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.33;
            double stunMin = tune != null && tune.StunMin > 0.0 ? tune.StunMin : 0.5;
            double stunMax = tune != null && tune.StunMax > 0.0 ? tune.StunMax : 1.0;
            if (stunMax < stunMin)
                stunMax = stunMin;

            DateTime now = DateTime.UtcNow;
            if (now < m_NextFaultlinePulse)
                return;

            m_NextFaultlinePulse = now + TimeSpan.FromSeconds(gap);

            List<Mobile> candidates = new List<Mobile>();
            DudeAbilityVfx.CollectFightList(this, candidates);

            List<Mobile> valid = new List<Mobile>();
            for (int i = 0; i < candidates.Count; i++)
            {
                Mobile m = candidates[i];
                if (m == null || m.Deleted || !m.Alive)
                    continue;
                if (m is PlayerMobile)
                    continue;
                if (m is DudeCreature)
                    continue;
                valid.Add(m);
            }

            if (valid.Count == 0)
                return;

            Mobile target = valid[Utility.Random(valid.Count)];
            DudeGear faultGear = FindEquippedGearByAbility("faultline");
            double prevFault = DudeAbility.CurrentEffectMultiplier;
            DudeAbility.CurrentEffectMultiplier = faultGear != null ? faultGear.GetEffectMultiplier() : 1.0;
            int damage = DudeAbility.ApplyEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(m_DudeLevel) * vs)));
            DudeAbility.CurrentEffectMultiplier = prevFault;

            PublicOverheadMessage(MessageType.Regular, 0x3F, false, "*Faultline*");
            AOS.Damage(target, this, damage, 100, 0, 0, 0, 0);
            DudeAbilityVfx.PlayEarthHit(target);

            double stun = stunMin + (Utility.RandomDouble() * (stunMax - stunMin));
            target.Paralyze(TimeSpan.FromSeconds(stun));
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


        /// <summary>
        /// Clear poison before parking (recall / faint) so Map.Internal Dudes stay cured.
        /// </summary>
        public void ClearPoisonForPark()
        {
            CurePoison(this);
            Poison = null;
        }

        private void FinishFaintPark(DudeBall ball, Mobile master, string dudeName)
        {
            m_Fainting = false;
            Frozen = false;

            if (Deleted)
                return;

            ClearPoisonForPark();

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
            ApplyHumanMaleAppearance(loaded);
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
            RebuildGearCache();
        }
    }
}
