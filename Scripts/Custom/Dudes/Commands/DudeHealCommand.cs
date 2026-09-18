using System;
using Server.Commands;
using Server.Custom.Dudes;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes.Commands
{
    public static class DudeHealCommand
    {
        public static void Initialize()
        {
            CommandSystem.Register("healdude", AccessLevel.Player, new CommandEventHandler(HealDude_OnCommand));
            CommandSystem.Register("HealDude", AccessLevel.Player, new CommandEventHandler(HealDude_OnCommand));
        }

        [Usage("healdude")]
        [Description("Uses a Dude healing potion on a linked self or throws it at a summoned Dude within 8 tiles that needs healing.")]
        private static void HealDude_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            if (from == null || from.Deleted || from.Backpack == null)
                return;

            if (!BaseDudeHealingPotion.CheckHealCooldown(from, true))
                return;

            BaseDudeHealingPotion potion = FindBestPotion(from);
            if (potion == null)
            {
                from.SendMessage("You need a Dude Healing Potion in your backpack.");
                return;
            }

            if (DudeLinkSystem.IsLinked(from))
            {
                potion.BeginDrinkLinked(from);
                return;
            }

            DudeCreature dude = FindHealTarget(from);
            if (dude == null)
            {
                from.SendMessage("No summoned Dude within 8 tiles needs healing.");
                return;
            }

            potion.BeginThrow(from, dude);
        }

        private static BaseDudeHealingPotion FindBestPotion(Mobile from)
        {
            if (from.Backpack == null)
                return null;

            // Prefer greater heals when available.
            GreaterDudeHealingPotion greater = from.Backpack.FindItemByType(typeof(GreaterDudeHealingPotion), true) as GreaterDudeHealingPotion;
            if (greater != null && !greater.Deleted)
                return greater;

            DudeHealingPotion normal = from.Backpack.FindItemByType(typeof(DudeHealingPotion), true) as DudeHealingPotion;
            if (normal != null && !normal.Deleted)
                return normal;

            return null;
        }

        private static DudeCreature FindHealTarget(Mobile from)
        {
            DudeCreature best = null;
            int bestMissing = 0;

            foreach (Mobile m in from.GetMobilesInRange(8))
            {
                DudeCreature dude = m as DudeCreature;
                if (dude == null || dude.Deleted || !dude.Alive)
                    continue;

                if (dude.IsWild || dude.BoundBall == null || dude.BoundBall.Deleted)
                    continue;

                if (dude.ControlMaster != from && from.AccessLevel < AccessLevel.GameMaster)
                    continue;

                if (dude.Map == null || dude.Map == Map.Internal)
                    continue;

                if (!from.CanSee(dude) || !from.InLOS(dude))
                    continue;

                if (dude.Hits >= dude.HitsMax)
                    continue;

                int missing = dude.HitsMax - dude.Hits;
                if (best == null || missing > bestMissing)
                {
                    best = dude;
                    bestMissing = missing;
                }
            }

            return best;
        }
    }
}
