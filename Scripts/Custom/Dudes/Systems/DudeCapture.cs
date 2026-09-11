using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Modular capture-chance provider. v1 is always 100% for valid wild Dudes.
    /// Swap or extend Calculate for rarity / HP / ball-type formulas later.
    /// </summary>
    public static class DudeCapture
    {
        /// <summary>
        /// Why this target cannot be caught, or null if capture may proceed.
        /// Blocks DudeBoss (and any DudeCreature.CanBeCaught == false).
        /// </summary>
        public static string GetCaptureBlockReason(Mobile thrower, object targeted, DudeBall ball)
        {
            if (thrower == null || ball == null)
                return "You cannot catch that.";

            if (ball.HasDude)
                return "That Dude Ball is already occupied.";

            DudeBoss boss = targeted as DudeBoss;
            if (boss != null)
                return string.Format("{0} cannot be caught in a Dude Ball!", boss.Name);

            DudeCreature dude = targeted as DudeCreature;
            if (dude == null || dude.Deleted)
                return "That is not a wild Dude.";

            if (!dude.CanBeCaught)
                return string.Format("{0} cannot be caught.", dude.Name);

            return null;
        }

        public static double Calculate(Mobile thrower, DudeCreature wild, DudeBall ball)
        {
            if (thrower == null || wild == null || ball == null)
                return 0.0;

            if (wild.Deleted || !wild.CanBeCaught)
                return 0.0;

            if (ball.HasDude)
                return 0.0;

            // v1: guaranteed capture
            return 1.0;
        }

        public static bool TryCapture(Mobile thrower, DudeCreature wild, DudeBall ball)
        {
            double chance = Calculate(thrower, wild, ball);

            if (chance <= 0.0)
                return false;

            if (Utility.RandomDouble() > chance)
                return false;

            DudeDefinition def = DudeRegistry.Get(wild.DefinitionId);
            if (def == null)
                return false;

            DudeData data = DudeData.FromDefinition(def, thrower);

            // Preserve live HP ratio into storage (UOR-simple).
            if (wild.HitsMax > 0)
            {
                data.Hits = Math.Max(1, (int)(data.HitsMax * ((double)wild.Hits / wild.HitsMax)));
            }

            ball.StoreDude(data);
            wild.Delete();

            thrower.SendMessage(0x59, "You caught {0}!", data.DisplayName);
            thrower.PlaySound(0x1E2);

            return true;
        }
    }
}
