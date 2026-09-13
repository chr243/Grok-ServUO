using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Awards Dude EXP for combat kills.
    /// ServUO's GetLootingRights / OnKilledBy attributes pet damage to the player master,
    /// so we hook CreatureDeath and resolve the Dude via LastKiller or damage entries.
    /// </summary>
    public static class DudeKillHandler
    {
        public static void Initialize()
        {
            EventSink.CreatureDeath += OnCreatureDeath;
            DudeRegistry.EnsureInitialized();
            DudeAbilityRegistry.EnsureInitialized();
        }

        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (e == null || e.Creature == null || e.Creature.Deleted)
                return;

            // Never farm EXP off other Dudes / workers.
            if (e.Creature is DudeCreature || e.Creature is DudeJobWorker)
                return;

            Mobile victim = e.Creature;

            // Preferred: killing blow was the Dude itself.
            DudeCreature killerDude = e.Killer as DudeCreature;
            if (killerDude != null)
            {
                TryAward(killerDude, victim);
                return;
            }

            // Otherwise: master (or another pet) got the credit — award every controlled Dude
            // that actually dealt damage to this victim.
            Mobile master = ResolveMaster(e.Killer);
            if (master == null)
                return;

            List<DudeCreature> dudes = FindDamagingDudes(victim, master);
            for (int i = 0; i < dudes.Count; i++)
                TryAward(dudes[i], victim);
        }

        private static Mobile ResolveMaster(Mobile killer)
        {
            if (killer == null || killer.Deleted)
                return null;

            if (killer.Player)
                return killer;

            BaseCreature bc = killer as BaseCreature;
            if (bc != null)
                return bc.GetMaster();

            return null;
        }

        private static List<DudeCreature> FindDamagingDudes(Mobile victim, Mobile master)
        {
            List<DudeCreature> result = new List<DudeCreature>();

            if (victim == null || master == null)
                return result;

            List<DamageEntry> entries = victim.DamageEntries;
            if (entries == null)
                return result;

            for (int i = 0; i < entries.Count; i++)
            {
                DamageEntry de = entries[i];
                if (de == null || de.HasExpired)
                    continue;

                DudeCreature dude = de.Damager as DudeCreature;
                if (dude == null || dude.Deleted || dude.IsWild)
                    continue;

                if (dude.ControlMaster != master)
                    continue;

                if (dude.BoundBall == null || dude.BoundBall.Deleted || dude.BoundBall.StoredDude == null)
                    continue;

                bool already = false;
                for (int j = 0; j < result.Count; j++)
                {
                    if (result[j] == dude)
                    {
                        already = true;
                        break;
                    }
                }

                if (!already)
                    result.Add(dude);
            }

            return result;
        }

        private static void TryAward(DudeCreature dude, Mobile victim)
        {
            if (dude == null || dude.Deleted || dude.IsWild)
                return;

            DudeBall ball = dude.BoundBall;
            if (ball == null || ball.Deleted || ball.StoredDude == null)
                return;

            int amount = DudeExperience.CalculateKillExp(ball.StoredDude, victim);
            if (amount <= 0)
                return;

            Mobile notify = dude.ControlMaster;
            DudeExperience.AwardExperience(ball, amount, notify);
        }
    }
}
