using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    public sealed class TailwindSelfAbility : DudeAbility
    {
        public TailwindSelfAbility()
            : base("tailwind_self", "Tailwind Self", TimeSpan.FromSeconds(10.0), 5, 1, DudeType.Air)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            if (dude == null || dude.Deleted)
                return false;
            if (dude.IsWild)
                return false;
            if (ManaCost > 0 && dude.Mana < ManaCost)
                return false;
            if (target == null || target.Deleted || !target.Alive)
                return false;
            return true;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("tailwind_self");
            double duration = tune != null && tune.DurationSeconds > 0.0 ? tune.DurationSeconds : 5.0;
            double speed = DudeAbility.ApplyEffectSpeed(tune != null && tune.SpeedFactor > 0.0 ? tune.SpeedFactor : 0.5);

            dude.PublicOverheadMessage(MessageType.Regular, 0x47E, false, "*Tailwind*");
            DudeAbilityVfx.ApplyTailwindSpeed(dude, TimeSpan.FromSeconds(duration), speed);
        }

        public override bool CanExecuteLinked(Mobile caster, DudeData data, Mobile target)
        {
            if (caster == null || caster.Deleted || !caster.Alive)
                return false;
            if (data == null)
                return false;
            if (ManaCost > 0 && caster.Mana < ManaCost)
                return false;
            return true;
        }

        public override void ExecuteLinked(Mobile caster, DudeData data, DudeBall ball, Mobile target)
        {
            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("tailwind_self");
            double duration = tune != null && tune.DurationSeconds > 0.0 ? tune.DurationSeconds : 5.0;

            caster.PublicOverheadMessage(MessageType.Regular, 0x47E, false, "*Tailwind*");
            DudeAbilityVfx.ApplyTailwindSpeedPlayer(caster, TimeSpan.FromSeconds(duration));
        }
    }
}
