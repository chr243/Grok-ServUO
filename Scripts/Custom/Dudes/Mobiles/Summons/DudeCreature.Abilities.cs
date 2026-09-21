using System;
using System.Collections.Generic;
using Server.Custom.Dudes;
using Server.Items;

namespace Server.Mobiles
{
    /// <summary>
    /// Ability casting: picks unlocked abilities from equipped DudeGear, tracks cooldowns and
    /// drives the stage-3 passive pulses. Ability effects live in Abilities/(Fire|Water|Earth|Air)/.
    /// </summary>
    public partial class DudeCreature
    {
        private Dictionary<string, DateTime> m_NextAbilityById;
        private DateTime m_NextBurnText;
        private DateTime m_NextSpringPulse;
        private DateTime m_NextFaultlinePulse;

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

                // Passives — pulsed by TryStage3Passives / Slipstream CD mod.
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
                    cd = SlipstreamAbility.ReduceCooldown(cd, slipMult);

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
                BurnAbility.Pulse(this, inCombat, ref m_NextBurnPulse, ref m_NextBurnText);

            if (HasUnlockedAbility("spring"))
                SpringAbility.Pulse(this, inCombat, ref m_NextSpringPulse);

            if (HasUnlockedAbility("faultline"))
                FaultlineAbility.Pulse(this, inCombat, ref m_NextFaultlinePulse);
        }
    }
}
