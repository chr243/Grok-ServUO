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
        public static double Calculate(Mobile thrower, DudeCreature wild, DudeBall ball)
        {
            if (thrower == null || wild == null || ball == null)
                return 0.0;

            if (wild.Deleted || !wild.IsWild)
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
