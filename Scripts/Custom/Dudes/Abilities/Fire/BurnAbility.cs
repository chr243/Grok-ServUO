using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Stage-3 Fire passive: every tick, each fight-list target has a chance to take fire damage.
    /// Not cast — pulsed by DudeCreature.TryStage3Passives (Pulse) and, for linked players, by
    /// DudeLinkSystem.PulsePassives (PulseLinked).
    /// </summary>
    public sealed class BurnAbility : DudeAbility
    {
        private static readonly TimeSpan BurnTextInterval = TimeSpan.FromSeconds(10.0);

        public BurnAbility()
            : base("burn", "Burn", TimeSpan.FromSeconds(9999.0), 0, 3, DudeType.Fire)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            return false;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
        }

        public static void Pulse(DudeCreature dude, bool inCombat, ref DateTime nextPulse, ref DateTime nextText)
        {
            if (!inCombat)
                return;

            // Cheap pulse check first — this runs every AI tick (10/s) while summoned.
            DateTime now = DateTime.UtcNow;
            if (now < nextPulse)
                return;

            DudeDefinition burnDef = DudeRegistry.Get(dude.DefinitionId);
            if (burnDef == null || burnDef.Type != DudeType.Fire)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("burn");
            double tick = tune != null && tune.TickSeconds > 0.0 ? tune.TickSeconds : 1.0;
            double hitChance = tune != null && tune.HitChance > 0.0 ? tune.HitChance : 0.5;
            double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.3;

            nextPulse = now + TimeSpan.FromSeconds(tick);

            List<Mobile> candidates = new List<Mobile>();
            DudeAbilityVfx.CollectFightList(dude, candidates);

            if (candidates.Count == 0)
                return;

            DudeGear burnGear = dude.FindEquippedGearByAbility("burn");
            double prevBurn = DudeAbility.CurrentEffectMultiplier;
            DudeAbility.CurrentEffectMultiplier = burnGear != null ? burnGear.GetEffectMultiplier() : 1.0;
            int damage = DudeAbility.ApplyEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(dude.DudeLevel) * vs)));
            DudeAbility.CurrentEffectMultiplier = prevBurn;
            bool anyHit = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (Utility.RandomDouble() >= hitChance)
                    continue;

                Mobile m = candidates[i];
                AOS.Damage(m, dude, damage, 0, 100, 0, 0, 0);
                DudeAbilityVfx.PlayBurnTick(m);
                anyHit = true;
            }

            if (anyHit)
            {
                // Burn ticks every second: one sound per pulse, and the label at most every 10s.
                if (now >= nextText)
                {
                    nextText = now + BurnTextInterval;
                    dude.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Burn*");
                }

                dude.PlaySound(0x208);
            }
        }

        public static void PulseLinked(Mobile caster, DudeData data, bool inCombat, ref DateTime nextPulse)
        {
            if (!inCombat)
                return;

            DudeDefinition def = DudeRegistry.Get(data.DefinitionId);
            if (def == null || def.Type != DudeType.Fire)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("burn");
            double tick = tune != null && tune.TickSeconds > 0.0 ? tune.TickSeconds : 1.0;
            double hitChance = tune != null && tune.HitChance > 0.0 ? tune.HitChance : 0.5;
            double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.3;

            DateTime now = DateTime.UtcNow;
            if (now < nextPulse)
                return;

            nextPulse = now + TimeSpan.FromSeconds(tick);

            List<Mobile> candidates = new List<Mobile>();
            DudeAbilityVfx.CollectFightList(caster, caster, candidates);
            if (candidates.Count == 0)
                return;

            int damage = Math.Max(1, (int)(DudeExperience.GetBlastDamage(data.Level) * vs));
            bool anyHit = false;

            for (int i = 0; i < candidates.Count; i++)
            {
                if (Utility.RandomDouble() >= hitChance)
                    continue;

                Mobile target = candidates[i];
                AOS.Damage(target, caster, damage, 0, 100, 0, 0, 0);
                DudeAbilityVfx.PlayFireHit(target, false);
                anyHit = true;
            }

            if (anyHit)
            {
                caster.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Burn*");
                caster.PlaySound(0x208);
            }
        }
    }
}
