using System;
using System.Collections.Generic;
using Server.Commands;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Linked-player casting: [abi1 / [abi2 fire the linked Dude's stage 1 / 2 ability, and a
    /// 1s timer pulses its stage-3 passives. Ability effects live in Abilities/(Fire|Water|Earth|Air)/
    /// (ExecuteLinked / PulseLinked).
    /// </summary>
    public static partial class DudeLinkSystem
    {
        private static void OnAbi1(CommandEventArgs e)
        {
            TryFireAbility(e.Mobile, 1);
        }

        private static void OnAbi2(CommandEventArgs e)
        {
            TryFireAbility(e.Mobile, 2);
        }

        private static void TryFireAbility(Mobile from, int stage)
        {
            if (from == null)
                return;

            if (!IsLinked(from))
            {
                from.SendMessage(0x22, "Only a Dude can do that!");
                return;
            }

            DudeBall ball = GetLinkedBall(from);
            if (ball == null || ball.Deleted || ball.StoredDude == null)
            {
                from.SendMessage(0x22, "Your linked Dude Ball is missing.");
                return;
            }

            DudeData data = ball.StoredDude;
            DudeExperience.EnsureEvolutionAbilities(data);

            int evo = GetEffectiveEvolutionStage(data);
            if (stage >= 2 && evo < 2)
            {
                from.SendMessage(0x22, "Stage 2 abilities unlock when this Dude evolves.");
                return;
            }

            DudeAbility ability = FindActiveAbility(data, stage);
            if (ability == null)
            {
                from.SendMessage(0x22, "No stage-{0} combat ability is unlocked.", stage);
                return;
            }

            LinkRuntime rt;
            if (!m_ByPlayer.TryGetValue(from, out rt) || rt == null)
                return;

            if (rt.NextAbilityById == null)
                rt.NextAbilityById = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);

            DateTime readyAt;
            if (rt.NextAbilityById.TryGetValue(ability.Id, out readyAt) && DateTime.UtcNow < readyAt)
            {
                double left = (readyAt - DateTime.UtcNow).TotalSeconds;
                from.SendMessage("Ability not ready ({0:0.0}s).", left);
                return;
            }

            Mobile target = ResolveAbilityTarget(from, ability);
            if (target == null)
            {
                from.SendMessage(0x22, "You need a valid combat target.");
                return;
            }

            if (!ability.TryExecuteLinked(from, data, ball, target))
            {
                from.SendMessage(0x22, "You cannot use {0} right now.", ability.Name);
                return;
            }

            TimeSpan cd = ability.Cooldown;
            if (GetEffectiveEvolutionStage(data) >= 3 && HasUnlocked(data, "slipstream"))
                cd = SlipstreamAbility.ReduceCooldown(cd, 1.0);

            rt.NextAbilityById[ability.Id] = DateTime.UtcNow + cd;
        }

        private static Mobile ResolveAbilityTarget(Mobile from, DudeAbility ability)
        {
            if (ability == null)
                return null;

            string id = ability.Id;
            if (string.Equals(id, "tide_mend", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "tailwind_self", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "tide_chorus", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "tailwind", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "aftershock", StringComparison.OrdinalIgnoreCase)
                || string.Equals(id, "ring_of_fire", StringComparison.OrdinalIgnoreCase))
            {
                Mobile combatant = from.Combatant as Mobile;
                if (combatant != null && !combatant.Deleted && combatant.Alive)
                    return combatant;
                return from;
            }

            Mobile t = from.Combatant as Mobile;
            if (t != null && !t.Deleted && t.Alive)
                return t;

            return null;
        }

        /// <summary>
        /// Ascension stage for link gates. Stage is now purely data-driven (no id implies it).
        /// </summary>
        private static int GetEffectiveEvolutionStage(DudeData data)
        {
            if (data == null)
                return 1;

            int stage = data.EvolutionStage;
            return stage < 1 ? 1 : stage;
        }

        private static DudeAbility FindActiveAbility(DudeData data, int stage)
        {
            if (data == null)
                return null;

            if (GetEffectiveEvolutionStage(data) < stage)
                return null;

            List<string> ids = data.GetUnlockedAbilityIds();
            for (int i = 0; i < ids.Count; i++)
            {
                DudeAbility ability = DudeAbilityRegistry.Get(ids[i]);
                if (ability == null)
                    continue;
                if (ability.Stage != stage)
                    continue;
                if (DudeCreature.IsPassiveAbilityId(ability.Id))
                    continue;
                return ability;
            }

            return null;
        }

        private static bool HasUnlocked(DudeData data, string abilityId)
        {
            if (data == null || string.IsNullOrEmpty(abilityId))
                return false;

            List<string> ids = data.GetUnlockedAbilityIds();
            for (int i = 0; i < ids.Count; i++)
            {
                if (string.Equals(ids[i], abilityId, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void PulsePassives()
        {
            if (m_ByPlayer.Count == 0)
                return;

            List<Mobile> players = new List<Mobile>(m_ByPlayer.Keys);
            for (int i = 0; i < players.Count; i++)
            {
                Mobile m = players[i];
                if (m == null || m.Deleted || !m.Alive)
                    continue;

                LinkRuntime rt;
                if (!m_ByPlayer.TryGetValue(m, out rt) || rt == null)
                    continue;

                DudeBall ball = rt.Ball;
                if (ball == null || ball.Deleted)
                {
                    DudeLinkState state = DudeLinkState.Get(m);
                    ball = state != null ? state.ResolveLinkedBall() : null;
                }

                if (ball == null || ball.StoredDude == null)
                    continue;

                TryStage3PassivesLinked(m, ball.StoredDude, rt);

            }
        }

        private static void TryStage3PassivesLinked(Mobile caster, DudeData data, LinkRuntime rt)
        {
            if (caster == null || data == null || rt == null)
                return;

            if (!caster.Alive || caster.Map == null || caster.Map == Map.Internal)
                return;

            // Passives are stage 3 only — abi1/abi2 stay stage-gated separately.
            if (GetEffectiveEvolutionStage(data) < 3)
                return;

            Mobile combatant = caster.Combatant as Mobile;
            bool inCombat = combatant != null && !combatant.Deleted && combatant.Alive;

            if (HasUnlocked(data, "burn"))
                BurnAbility.PulseLinked(caster, data, inCombat, ref rt.NextBurnPulse);

            if (HasUnlocked(data, "spring"))
                SpringAbility.PulseLinked(caster, data, inCombat, ref rt.NextSpringPulse);

            if (HasUnlocked(data, "faultline"))
                FaultlineAbility.PulseLinked(caster, data, inCombat, ref rt.NextFaultlinePulse);
        }
    }
}
