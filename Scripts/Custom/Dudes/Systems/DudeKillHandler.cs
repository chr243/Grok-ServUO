using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Awards Dude EXP for combat kills.
    /// Every summoned, controlled Dude in range whose ControlMaster participated
    /// in the kill gets full XP (not split). Last-hit Dude is not required.
    /// </summary>
    public static class DudeKillHandler
    {
        private const int AwardRange = 10; // within requested 8–12 tiles

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
            Mobile lastKiller = e.Killer;

            List<Mobile> masters = CollectParticipatingMasters(victim, lastKiller);
            if (masters.Count == 0)
                return;

            List<DudeCreature> recipients = FindEligibleDudesInRange(victim, masters);
            for (int i = 0; i < recipients.Count; i++)
                TryAward(recipients[i], victim);
        }

        /// <summary>
        /// Participated = player is LastKiller, or aggressor/attacker on the dead mobile,
        /// or the LastKiller is their Dude (or other controlled pet).
        /// </summary>
        private static List<Mobile> CollectParticipatingMasters(Mobile victim, Mobile lastKiller)
        {
            List<Mobile> masters = new List<Mobile>();

            Mobile killerMaster = ResolveMaster(lastKiller);
            if (killerMaster != null)
                AddUnique(masters, killerMaster);

            if (victim == null)
                return masters;

            // Damage entries: player or their Dude dealt damage.
            List<DamageEntry> entries = victim.DamageEntries;
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    DamageEntry de = entries[i];
                    if (de == null || de.HasExpired)
                        continue;

                    Mobile m = ResolveMaster(de.Damager);
                    if (m != null)
                        AddUnique(masters, m);
                }
            }

            // Aggressors / attackers on the dead mobile.
            List<AggressorInfo> aggressors = victim.Aggressors;
            if (aggressors != null)
            {
                for (int i = 0; i < aggressors.Count; i++)
                {
                    AggressorInfo info = aggressors[i];
                    if (info == null || info.Expired)
                        continue;

                    Mobile m = ResolveMaster(info.Attacker);
                    if (m != null)
                        AddUnique(masters, m);
                }
            }

            List<AggressorInfo> aggressed = victim.Aggressed;
            if (aggressed != null)
            {
                for (int i = 0; i < aggressed.Count; i++)
                {
                    AggressorInfo info = aggressed[i];
                    if (info == null || info.Expired)
                        continue;

                    // When we are the aggressor against Defender, the other party may be listed here.
                    Mobile m = ResolveMaster(info.Defender);
                    if (m != null)
                        AddUnique(masters, m);
                }
            }

            return masters;
        }

        private static Mobile ResolveMaster(Mobile mobile)
        {
            if (mobile == null || mobile.Deleted)
                return null;

            if (mobile.Player)
                return mobile;

            BaseCreature bc = mobile as BaseCreature;
            if (bc != null)
            {
                Mobile master = bc.ControlMaster;
                if (master != null && !master.Deleted && master.Player)
                    return master;

                master = bc.GetMaster();
                if (master != null && !master.Deleted && master.Player)
                    return master;
            }

            return null;
        }

        private static List<DudeCreature> FindEligibleDudesInRange(Mobile victim, List<Mobile> masters)
        {
            List<DudeCreature> result = new List<DudeCreature>();

            if (victim == null || victim.Map == null || victim.Map == Map.Internal)
                return result;

            IPooledEnumerable eable = victim.GetMobilesInRange(AwardRange);
            foreach (Mobile m in eable)
            {
                DudeCreature dude = m as DudeCreature;
                if (dude == null || dude.Deleted)
                    continue;

                // Wild / ball-only never receive kill XP (summoned Controlled only).
                if (dude.IsWild)
                    continue;

                if (!dude.Controlled || dude.ControlMaster == null || dude.ControlMaster.Deleted)
                    continue;

                if (!IsInList(masters, dude.ControlMaster))
                    continue;

                // Must be summoned in the world (not parked / internal).
                if (dude.Map == null || dude.Map == Map.Internal)
                    continue;

                DudeBall ball = dude.BoundBall;
                if (ball == null || ball.Deleted || ball.StoredDude == null)
                    continue;

                // Prefer the live summoned projection only.
                if (ball.SummonedDude != dude)
                    continue;

                AddUniqueDude(result, dude);
            }
            eable.Free();

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

        private static void AddUnique(List<Mobile> list, Mobile m)
        {
            if (m == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == m)
                    return;
            }

            list.Add(m);
        }

        private static void AddUniqueDude(List<DudeCreature> list, DudeCreature dude)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == dude)
                    return;
            }

            list.Add(dude);
        }

        private static bool IsInList(List<Mobile> list, Mobile m)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == m)
                    return true;
            }

            return false;
        }
    }
}
