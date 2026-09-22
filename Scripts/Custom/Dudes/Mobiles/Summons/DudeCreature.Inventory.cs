using System;
using System.Collections.Generic;
using Server.Custom.Dudes;
using Server.Items;

namespace Server.Mobiles
{
    /// <summary>
    /// Inventory: type shorts / starter sash, DudeGear paperdoll slots, equip rules and the
    /// gear-driven skill/AI effects (Magical Dude Hat, Dude Shield, Dude Costume).
    /// </summary>
    public partial class DudeCreature
    {
        private List<DudeGear> m_EquippedGear;
        private List<string> m_EquippedAbilityIds;

        /// <summary>
        /// Allowed DudeGear paperdoll layers (order is documentation only — slot budget is a count).
        /// Helm = Magical Dude Hat; InnerTorso = sash; Earrings = type earrings; Bracelet = bracers.
        /// TwoHanded (Dude Shield) allowed separately via CanAcceptGear; still costs a slot.
        /// OneHanded (DudeCostume) allowed separately; SlotCost 0 is ignored in slot budget.
        /// Pants reserved for type shorts.
        /// </summary>
        public static readonly Layer[] GearLayerOrder = new Layer[]
        {
            Layer.Helm,        // hat
            Layer.InnerTorso,  // sash
            Layer.Earrings,    // type earrings (was circlet)
            Layer.Bracelet     // bracers
        };

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

            DudeTypeProfile profile = DudeTypeProfiles.Get(type);
            Type sashType = profile != null ? profile.StarterSashType : null;
            return sashType != null && sashType.IsInstanceOfType(item);
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
            DudeTypeProfile profile = DudeTypeProfiles.Get(type);
            Type sashType = profile != null ? profile.StarterSashType : null;
            if (sashType == null)
                return null;

            return Activator.CreateInstance(sashType) as DudeGear;
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
        /// True if layer is one of the allowed DudeGear layers (or TwoHanded / OneHanded).
        /// Does not check slot budget — use CanAcceptGear for that.
        /// </summary>
        public bool IsGearLayerAllowed(Layer layer)
        {
            if (layer == Layer.OneHanded)
                return true;

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
            bool isOneHanded = (layer == Layer.OneHanded);
            bool layerOk = isTwoHanded || isOneHanded;
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
            // SlotCost 0 (e.g. DudeCostume) does not consume budget.
            int cost = gear.SlotCost;
            if (cost > 0)
            {
                int allowed = GetGearSlotCount();
                if (allowed < 0)
                    allowed = 0;

                int used = GetEquippedGearSlotCost(gear);
                if (used + cost > allowed)
                {
                    int stageNeeded = StageNeededForSlotCount(used + cost);
                    reason = string.Format("Needs stage {0} for another slot.", stageNeeded);
                    return false;
                }
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
                // Appearance-only costumes are not combat gear.
                if (gear is DudeCostume)
                    continue;
                if (gear.SlotCost == 0 && string.IsNullOrEmpty(gear.AbilityId))
                    continue;
                m_EquippedGear.Add(gear);
                if (!string.IsNullOrEmpty(gear.AbilityId))
                    m_EquippedAbilityIds.Add(gear.AbilityId);
            }

            // Re-apply Dude Costume body after appearance resets (world load / ApplyData).
            DudeCostume costume = FindItemOnLayer(Layer.OneHanded) as DudeCostume;
            if (costume != null && !costume.Deleted && costume.FormBody > 0)
                Body = costume.FormBody;

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
                // Lerp stored → 120 by gear level, then stage cap. Never 120 before gear 10.
                double magery = Math.Min(hat.GetSkillLerp(hat.Magery, 120.0), cap);
                double eval = Math.Min(hat.GetSkillLerp(hat.EvalInt, 120.0), cap);
                double med = Math.Min(hat.GetSkillLerp(hat.Meditation, 120.0), cap);
                DudeCombatSkills.SetGearCopySkill(this, SkillName.Magery, magery, cap);
                DudeCombatSkills.SetGearCopySkill(this, SkillName.EvalInt, eval, cap);
                DudeCombatSkills.SetGearCopySkill(this, SkillName.Meditation, med, cap);
                // Level 0 = full stored skills; higher levels lerp toward 120 then stage-cap.
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
                double parry = Math.Min(shield.GetSkillLerp(shield.Parrying, 120.0), cap);
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

        /// <summary>
        /// A Dude has no backpack. If a cast ever removes a hand item, re-equip it in place instead
        /// of letting the base ClearHand path drop it (AddToBackpack → MoveToWorld). Never delete.
        /// </summary>
        public override void ClearHand(Item item)
        {
            DudeGear gear = item as DudeGear;
            if (gear == null || gear.Deleted)
                return;

            if (FindItemOnLayer(gear.Layer) != gear)
                EquipItem(gear);
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

        /// <summary>
        /// Deletes extra worn DudeTypeShorts (left by the old load-time EnsureTypeShorts bug),
        /// keeping the pair that FindItemOnLayer(Layer.Pants) resolves to.
        /// </summary>
        private void RemoveDuplicateTypeShorts()
        {
            Item worn = FindItemOnLayer(Layer.Pants);
            List<Item> extras = null;

            for (int i = 0; i < Items.Count; i++)
            {
                Item item = Items[i];

                if (item is DudeTypeShorts && item != worn && !item.Deleted)
                {
                    if (extras == null)
                        extras = new List<Item>();

                    extras.Add(item);
                }
            }

            if (extras == null)
                return;

            for (int i = 0; i < extras.Count; i++)
                extras[i].Delete();
        }
    }
}
