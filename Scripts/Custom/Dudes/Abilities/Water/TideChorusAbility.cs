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

            // Heal = (10 + 2 * gearLevel)% of each target's HitsMax. No Blast cap, no effect multiplier.
            double frac = 0.10 + (0.02 * GetChorusGearLevel(dude));

            for (int i = 0; i < allies.Count; i++)
            {
                DudeCreature ally = allies[i];
                if (ally == null || ally.Deleted || !ally.Alive)
                    continue;

                int heal = Math.Max(1, (int)(ally.HitsMax * frac));

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

            // Heal = (10 + 2 * gearLevel)% of each target's HitsMax. No Blast cap, no effect multiplier.
            double frac = 0.10 + (0.02 * GetChorusGearLevel(ball));

            // Heal linked caster
            int selfHeal = Math.Max(1, (int)(caster.HitsMax * frac));
            caster.Heal(selfHeal, caster, false);
            DudeAbilityVfx.PlayWaterHeal(caster);

            for (int i = 0; i < allies.Count; i++)
            {
                DudeCreature ally = allies[i];
                if (ally == null || ally.Deleted || !ally.Alive)
                    continue;

                int heal = Math.Max(1, (int)(ally.HitsMax * frac));

                ally.Heal(heal, caster, false);
                DudeAbilityVfx.PlayWaterHeal(ally);
            }
        }

        /// <summary>Chorus earrings gear level (0..10) worn by the casting Dude.</summary>
        private static int GetChorusGearLevel(DudeCreature dude)
        {
            if (dude == null || dude.Deleted)
                return 0;
            return ClampGearLevel(dude.FindEquippedGearByAbility("tide_chorus"));
        }

        /// <summary>Chorus earrings gear level (0..10) on the Dude parked in the linked ball.</summary>
        private static int GetChorusGearLevel(DudeBall ball)
        {
            if (ball == null || ball.Deleted)
                return 0;

            DudeCreature parked = ball.SummonedDude;
            if (parked == null || parked.Deleted)
                return 0;
            return ClampGearLevel(parked.FindEquippedGearByAbility("tide_chorus"));
        }

        private static int ClampGearLevel(DudeGear gear)
        {
            if (gear == null || gear.Deleted)
                return 0;

            int lv = gear.GearLevel;
            if (lv < 0)
                lv = 0;
            if (lv > 10)
                lv = 10;
            return lv;
        }
    }
}
