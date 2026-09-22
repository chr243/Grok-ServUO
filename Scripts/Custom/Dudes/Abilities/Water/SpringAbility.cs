using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Stage-3 Water passive: in combat, periodically heals the Dude (or linked player) and owned
    /// Dudes within 2 tiles. Not cast — pulsed by DudeCreature.TryStage3Passives (Pulse) and, for
    /// linked players, by DudeLinkSystem.PulsePassives (PulseLinked).
    /// </summary>
    public sealed class SpringAbility : DudeAbility
    {
        public SpringAbility()
            : base("spring", "Spring", TimeSpan.FromSeconds(9999.0), 0, 3, DudeType.Water)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            return false;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
        }

        public static void Pulse(DudeCreature dude, bool inCombat, ref DateTime nextPulse)
        {
            if (!inCombat)
                return;

            DateTime now = DateTime.UtcNow;
            if (now < nextPulse)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("spring");
            double tick = tune != null && tune.TickSeconds > 0.0 ? tune.TickSeconds : 2.0;
            double healFrac = tune != null && tune.HealHitsFraction > 0.0 ? tune.HealHitsFraction : 0.05;

            nextPulse = now + TimeSpan.FromSeconds(tick);

            DudeGear springGear = dude.FindEquippedGearByAbility("spring");
            double prevSpring = DudeAbility.CurrentEffectMultiplier;
            DudeAbility.CurrentEffectMultiplier = springGear != null ? springGear.GetEffectMultiplier() : 1.0;

            int blast = DudeExperience.GetBlastDamage(dude.DudeLevel);
            int selfHeal = Math.Max(1, (int)(blast * 0.15));
            int pctHeal = Math.Max(1, (int)(dude.HitsMax * healFrac));
            int heal = Math.Min(selfHeal, pctHeal);
            if (heal < 1)
                heal = 1;
            heal = DudeAbility.ApplyEffect(heal);

            // The pulse runs every 2s in combat; only show the heal effect when something was healed.
            if (dude.Heal(heal, dude, false) > 0)
                DudeAbilityVfx.PlayWaterHeal(dude);

            // Heal owned DudeCreatures within range 2 via master's followers (no hostile scan).
            Mobile master = dude.ControlMaster;
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
                if (ally == null || ally == dude || ally.Deleted || !ally.Alive)
                    continue;
                if (ally.Map != dude.Map)
                    continue;
                if (!dude.InRange(ally, 2))
                    continue;

                int allyPct = Math.Max(1, (int)(ally.HitsMax * healFrac));
                int allyHeal = Math.Min(Math.Max(1, (int)(blast * 0.15)), allyPct);
                allyHeal = DudeAbility.ApplyEffect(allyHeal);

                if (ally.Heal(allyHeal, dude, false) > 0)
                    DudeAbilityVfx.PlayWaterHeal(ally);
            }

            DudeAbility.CurrentEffectMultiplier = prevSpring;
        }

        public static void PulseLinked(Mobile caster, DudeData data, bool inCombat, ref DateTime nextPulse)
        {
            if (!inCombat)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("spring");
            double tick = tune != null && tune.TickSeconds > 0.0 ? tune.TickSeconds : 2.0;
            double healFrac = tune != null && tune.HealHitsFraction > 0.0 ? tune.HealHitsFraction : 0.05;

            DateTime now = DateTime.UtcNow;
            if (now < nextPulse)
                return;

            nextPulse = now + TimeSpan.FromSeconds(tick);

            int blast = DudeExperience.GetBlastDamage(data.Level);
            int selfHeal = Math.Max(1, (int)(blast * 0.15));
            int pctHeal = Math.Max(1, (int)(caster.HitsMax * healFrac));
            int heal = Math.Min(selfHeal, pctHeal);
            if (heal < 1)
                heal = 1;

            caster.Heal(heal, caster, false);
            DudeAbilityVfx.PlayWaterHeal(caster);

            PlayerMobile pm = caster as PlayerMobile;
            List<Mobile> followers = pm != null ? pm.AllFollowers : null;
            if (followers == null)
                return;

            for (int i = 0; i < followers.Count; i++)
            {
                DudeCreature ally = followers[i] as DudeCreature;
                if (ally == null || ally.Deleted || !ally.Alive)
                    continue;
                if (ally.Map != caster.Map)
                    continue;
                if (!caster.InRange(ally, 2))
                    continue;

                int allyPct = Math.Max(1, (int)(ally.HitsMax * healFrac));
                int allyHeal = Math.Min(Math.Max(1, (int)(blast * 0.15)), allyPct);
                ally.Heal(allyHeal, caster, false);
                DudeAbilityVfx.PlayWaterHeal(ally);
            }
        }
    }
}
