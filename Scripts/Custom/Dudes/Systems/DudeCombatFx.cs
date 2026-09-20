using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Shared combat FX for Dude heals that bypass Mobile.Heal.
    /// Prefer mobile.Heal(amount, from, false) when possible — Heal already shows +N.
    /// </summary>
    public static class DudeCombatFx
    {
        /// <summary>Green-ish overhead hue for +heal (0x42; alternatives 0x3E / 0x59).</summary>
        public const int HealHue = 0x42;

        public static void ShowHeal(Mobile target, int amount)
        {
            if (target == null || target.Deleted || amount <= 0)
                return;

            target.PublicOverheadMessage(MessageType.Regular, HealHue, false, "+" + amount);
        }
    }
}
