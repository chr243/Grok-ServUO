using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    public sealed class TailwindAbility : DudeAbility
    {
        public const int TailwindRange = 8;

        public TailwindAbility()
            : base("tailwind", "Tailwind", TimeSpan.FromSeconds(12.0), 8, 2, DudeType.Air)
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
            dude.PublicOverheadMessage(MessageType.Regular, 0x47E, false, "*Tailwind*");

            List<DudeCreature> allies = new List<DudeCreature>();
            DudeAbilityVfx.CollectPartyOwnedDudes(dude.ControlMaster, dude.Location, dude.Map, TailwindRange, allies);

            bool hasSelf = false;
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] == dude)
                {
                    hasSelf = true;
                    break;
                }
            }
            if (!hasSelf)
                allies.Add(dude);

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("tailwind");
            double duration = tune != null && tune.DurationSeconds > 0.0 ? tune.DurationSeconds : 5.0;
            double speed = DudeAbility.ApplyEffectSpeed(tune != null && tune.SpeedFactor > 0.0 ? tune.SpeedFactor : 0.5);

            for (int i = 0; i < allies.Count; i++)
                DudeAbilityVfx.ApplyTailwindSpeed(allies[i], TimeSpan.FromSeconds(duration), speed);
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
            caster.PublicOverheadMessage(MessageType.Regular, 0x47E, false, "*Tailwind*");

            List<DudeCreature> allies = new List<DudeCreature>();
            DudeAbilityVfx.CollectPartyOwnedDudes(caster, caster.Location, caster.Map, TailwindRange, allies);

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("tailwind");
            double duration = tune != null && tune.DurationSeconds > 0.0 ? tune.DurationSeconds : 5.0;
            double speed = DudeAbility.ApplyEffectSpeed(tune != null && tune.SpeedFactor > 0.0 ? tune.SpeedFactor : 0.5);

            DudeAbilityVfx.ApplyTailwindSpeedPlayer(caster, TimeSpan.FromSeconds(duration));

            for (int i = 0; i < allies.Count; i++)
                DudeAbilityVfx.ApplyTailwindSpeed(allies[i], TimeSpan.FromSeconds(duration), speed);
        }
    }
}
