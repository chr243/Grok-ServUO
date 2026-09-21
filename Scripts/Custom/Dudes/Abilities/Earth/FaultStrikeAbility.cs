using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    public sealed class FaultStrikeAbility : DudeAbility
    {
        public FaultStrikeAbility()
            : base("fault_strike", "Fault Strike", TimeSpan.FromSeconds(10.0), 5, 1, DudeType.Earth)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            if (!base.CanExecute(dude, target))
                return false;
            if (target is PlayerMobile)
                return false;
            return true;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            if (target is PlayerMobile)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("fault_strike");
            double stun = tune != null && tune.StunSeconds > 0.0 ? tune.StunSeconds : 1.0;

            int damage = DudeAbility.ApplyEffect(DudeExperience.GetBlastDamage(dude.DudeLevel));
            dude.PublicOverheadMessage(MessageType.Regular, 0x3F, false, "*Fault Strike*");
            AOS.Damage(target, dude, damage, 100, 0, 0, 0, 0);
            DudeAbilityVfx.PlayEarthHit(target);
            target.Paralyze(TimeSpan.FromSeconds(stun));
        }

        public override bool CanExecuteLinked(Mobile caster, DudeData data, Mobile target)
        {
            if (!base.CanExecuteLinked(caster, data, target))
                return false;
            if (target is PlayerMobile)
                return false;
            return true;
        }

        public override void ExecuteLinked(Mobile caster, DudeData data, DudeBall ball, Mobile target)
        {
            if (target is PlayerMobile)
                return;

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("fault_strike");
            double stun = tune != null && tune.StunSeconds > 0.0 ? tune.StunSeconds : 1.0;

            int damage = DudeAbility.ApplyEffect(DudeExperience.GetBlastDamage(data != null ? data.Level : 1));
            caster.PublicOverheadMessage(MessageType.Regular, 0x3F, false, "*Fault Strike*");
            AOS.Damage(target, caster, damage, 100, 0, 0, 0, 0);
            DudeAbilityVfx.PlayEarthHit(target);
            target.Paralyze(TimeSpan.FromSeconds(stun));
        }
    }
}
