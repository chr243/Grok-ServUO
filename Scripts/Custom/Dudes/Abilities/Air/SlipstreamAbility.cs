using System;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Stage-3 Air passive: shortens the cooldown of every other ability the Dude (or linked
    /// player) uses, down to a floor. Applied through ReduceCooldown by DudeCreature.TryUseAbility
    /// and DudeLinkSystem.TryFireAbility.
    /// </summary>
    public sealed class SlipstreamAbility : DudeAbility
    {
        public SlipstreamAbility()
            : base("slipstream", "Slipstream", TimeSpan.FromSeconds(9999.0), 0, 3, DudeType.Air)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            return false;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
        }

        /// <summary>
        /// Cooldown after Slipstream: minus ReduceSeconds (scaled by the Slipstream gear's effect
        /// multiplier; 1.0 for linked players), never below FloorSeconds.
        /// </summary>
        public static TimeSpan ReduceCooldown(TimeSpan cd, double effectMultiplier)
        {
            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune slip = DudeAbilityConfig.Get("slipstream");
            double reduce = slip != null && slip.ReduceSeconds > 0.0 ? slip.ReduceSeconds : 2.0;
            reduce *= effectMultiplier;
            double floor = slip != null && slip.FloorSeconds > 0.0 ? slip.FloorSeconds : 7.0;
            return TimeSpan.FromSeconds(Math.Max(floor, cd.TotalSeconds - reduce));
        }
    }
}
