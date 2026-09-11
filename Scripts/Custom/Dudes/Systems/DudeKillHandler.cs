using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Awards Dude EXP when a summoned Dude (or its master credited kill via pet) gets a kill.
    /// Hooks EventSink.OnKilledBy — UOR-safe, no expansion-specific kill tables.
    /// </summary>
    public static class DudeKillHandler
    {
        public static void Initialize()
        {
            EventSink.OnKilledBy += OnKilledBy;
            DudeRegistry.EnsureInitialized();
            DudeAbilityRegistry.EnsureInitialized();
        }

        private static void OnKilledBy(OnKilledByEventArgs e)
        {
            if (e == null || e.KilledBy == null || e.Killed == null)
                return;

            DudeCreature dude = e.KilledBy as DudeCreature;

            // Also credit if the master's pet damage led to kill attribution on the master:
            // ServUO usually attributes pet kills to the pet itself via DamageEntries.
            if (dude == null)
                return;

            if (dude.IsWild || dude.BoundBall == null || dude.BoundBall.Deleted)
                return;

            DudeBall ball = dude.BoundBall;
            if (ball.StoredDude == null)
                return;

            int amount = DudeExperience.CalculateKillExp(ball.StoredDude, e.Killed);
            if (amount > 0)
                DudeExperience.AwardExperience(ball, amount);
        }
    }
}
