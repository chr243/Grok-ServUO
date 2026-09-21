using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    public sealed class TideChorusAbility : DudeAbility
    {
        public const int ChorusRange = 8;

        public TideChorusAbility()
            : base("tide_chorus", "Tide Chorus", TimeSpan.FromSeconds(12.0), 8, 2, DudeType.Water)
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
            dude.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*Tide Chorus*");

            List<DudeCreature> allies = new List<DudeCreature>();
            DudeAbilityVfx.CollectPartyOwnedDudes(dude.ControlMaster, dude.Location, dude.Map, ChorusRange, allies);

            // Always include self if in range of own location.
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
            DudeAbilityTune tune = DudeAbilityConfig.Get("tide_chorus");
            double healFrac = tune != null && tune.HealHitsFraction > 0.0 ? tune.HealHitsFraction : 0.20;

            int blastHeal = DudeExperience.GetBlastDamage(dude.DudeLevel);

            for (int i = 0; i < allies.Count; i++)
            {
                DudeCreature ally = allies[i];
                if (ally == null || ally.Deleted || !ally.Alive)
                    continue;

                int pctHeal = Math.Max(1, (int)(ally.HitsMax * healFrac));
                int heal = Math.Min(pctHeal, blastHeal);
                if (heal < 1)
                    heal = 1;
                heal = DudeAbility.ApplyEffect(heal);

                ally.Heal(heal, dude, false);
                DudeAbilityVfx.PlayWaterHeal(ally);
            }
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
            caster.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*Tide Chorus*");

            List<DudeCreature> allies = new List<DudeCreature>();
            DudeAbilityVfx.CollectPartyOwnedDudes(caster, caster.Location, caster.Map, ChorusRange, allies);

            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("tide_chorus");
            double healFrac = tune != null && tune.HealHitsFraction > 0.0 ? tune.HealHitsFraction : 0.20;
            int blastHeal = DudeExperience.GetBlastDamage(data != null ? data.Level : 1);

            // Heal linked caster
            int selfPct = Math.Max(1, (int)(caster.HitsMax * healFrac));
            int selfHeal = Math.Min(selfPct, blastHeal);
            if (selfHeal < 1)
                selfHeal = 1;
            selfHeal = DudeAbility.ApplyEffect(selfHeal);
            caster.Heal(selfHeal, caster, false);
            DudeAbilityVfx.PlayWaterHeal(caster);

            for (int i = 0; i < allies.Count; i++)
            {
                DudeCreature ally = allies[i];
                if (ally == null || ally.Deleted || !ally.Alive)
                    continue;

                int pctHeal = Math.Max(1, (int)(ally.HitsMax * healFrac));
                int heal = Math.Min(pctHeal, blastHeal);
                if (heal < 1)
                    heal = 1;
                heal = DudeAbility.ApplyEffect(heal);

                ally.Heal(heal, caster, false);
                DudeAbilityVfx.PlayWaterHeal(ally);
            }
        }
    }
}
