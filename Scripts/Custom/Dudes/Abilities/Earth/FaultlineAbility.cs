using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Stage-3 Earth passive: in combat, periodically strikes one random fight-list target for
    /// earth damage and a short stun. Not cast — pulsed by DudeCreature.TryStage3Passives (Pulse)
    /// and, for linked players, by DudeLinkSystem.PulsePassives (PulseLinked).
    /// </summary>
    public sealed class FaultlineAbility : DudeAbility
    {
        public FaultlineAbility()
            : base("faultline", "Faultline", TimeSpan.FromSeconds(9999.0), 0, 3, DudeType.Earth)
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
            DudeAbilityTune tune = DudeAbilityConfig.Get("faultline");
            double gap = tune != null && tune.GapSeconds > 0.0 ? tune.GapSeconds : 10.0;
            double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.33;
            double stunMin = tune != null && tune.StunMin > 0.0 ? tune.StunMin : 0.5;
            double stunMax = tune != null && tune.StunMax > 0.0 ? tune.StunMax : 1.0;
            if (stunMax < stunMin)
                stunMax = stunMin;

            nextPulse = now + TimeSpan.FromSeconds(gap);

            List<Mobile> candidates = new List<Mobile>();
            DudeAbilityVfx.CollectFightList(dude, candidates);

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
                if (dude.IsPackAlly(m))
                    continue;
                valid.Add(m);
            }

            if (valid.Count == 0)
                return;

            Mobile target = valid[Utility.Random(valid.Count)];
            DudeGear faultGear = dude.FindEquippedGearByAbility("faultline");
            double prevFault = DudeAbility.CurrentEffectMultiplier;
            DudeAbility.CurrentEffectMultiplier = faultGear != null ? faultGear.GetEffectMultiplier() : 1.0;
            int damage = DudeAbility.ApplyEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(dude.DudeLevel) * vs)));
            DudeAbility.CurrentEffectMultiplier = prevFault;

            dude.PublicOverheadMessage(MessageType.Regular, 0x3F, false, "*Faultline*");
            AOS.Damage(target, dude, damage, 100, 0, 0, 0, 0);
            DudeAbilityVfx.PlayEarthHit(target);

            double stun = stunMin + (Utility.RandomDouble() * (stunMax - stunMin));
            target.Paralyze(TimeSpan.FromSeconds(stun));
        }

        public static void PulseLinked(Mobile caster, DudeData data, bool inCombat, ref DateTime nextPulse)
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
            if (now < nextPulse)
                return;

            nextPulse = now + TimeSpan.FromSeconds(gap);

            List<Mobile> candidates = new List<Mobile>();
            DudeAbilityVfx.CollectFightList(caster, caster, candidates);

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
                if (DudeCreature.IsPackAlly(m, caster))
                    continue;
                valid.Add(m);
            }

            if (valid.Count == 0)
                return;

            Mobile target = valid[Utility.Random(valid.Count)];
            int damage = Math.Max(1, (int)(DudeExperience.GetBlastDamage(data.Level) * vs));

            caster.PublicOverheadMessage(MessageType.Regular, 0x3F, false, "*Faultline*");
            AOS.Damage(target, caster, damage, 100, 0, 0, 0, 0);
            DudeAbilityVfx.PlayEarthHit(target);

            double stun = stunMin + (Utility.RandomDouble() * (stunMax - stunMin));
            target.Paralyze(TimeSpan.FromSeconds(stun));
        }
    }
}
