using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Shard rule: players and their summoned Dudes cannot PvP other players,
    /// summoned (non-wild) Dudes, or player-controlled pets.
    /// Wild Dudes and normal monsters remain fair game. Job workers stay Blessed.
    /// Does not affect beneficial/healing actions.
    /// </summary>
    public static class DudeNoPvP
    {
        /// <summary>PlayerMobile, or a summoned (non-wild) DudeCreature.</summary>
        public static bool IsPlayerSideAttacker(Mobile from)
        {
            if (from == null || from.Deleted)
                return false;

            if (from is PlayerMobile)
                return true;

            DudeCreature dude = from as DudeCreature;
            return dude != null && !dude.IsWild;
        }

        /// <summary>
        /// Targets protected from player-side harm: other players, summoned Dudes,
        /// and BaseCreatures whose ControlMaster is a player.
        /// </summary>
        public static bool IsProtectedTarget(Mobile target)
        {
            if (target == null || target.Deleted)
                return false;

            if (target is PlayerMobile)
                return true;

            DudeCreature dude = target as DudeCreature;
            if (dude != null)
                return !dude.IsWild;

            BaseCreature bc = target as BaseCreature;
            if (bc != null)
            {
                Mobile master = bc.ControlMaster;
                if (master != null && !master.Deleted && master is PlayerMobile)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// True when <paramref name="from"/> (player or summoned Dude) must not harm
        /// <paramref name="target"/> under no-PvP rules.
        /// </summary>
        public static bool BlocksHarm(Mobile from, Mobile target)
        {
            if (from == null || target == null || from == target)
                return false;

            if (!IsPlayerSideAttacker(from))
                return false;

            return IsProtectedTarget(target);
        }

        /// <summary>
        /// Owner looking at their own Dude/pet, or summoned Dude looking at its master /
        /// same-master packmate — leave existing notoriety alone.
        /// </summary>
        public static bool IsOwnerOwnRelationship(Mobile source, Mobile target)
        {
            if (source == null || target == null || source == target)
                return false;

            DudeCreature sourceDude = source as DudeCreature;
            if (sourceDude != null && !sourceDude.IsWild)
            {
                Mobile myMaster = sourceDude.ControlMaster;
                if (myMaster != null && !myMaster.Deleted)
                {
                    if (target == myMaster)
                        return true;

                    BaseCreature targetBc = target as BaseCreature;
                    if (targetBc != null && targetBc.ControlMaster == myMaster)
                        return true;
                }
            }

            if (source is PlayerMobile)
            {
                BaseCreature targetBc = target as BaseCreature;
                if (targetBc != null && targetBc.ControlMaster == source)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Force Innocent (blue) notoriety for player-side vs protected targets,
        /// except owner↔own Dude/pet (unchanged).
        /// </summary>
        public static bool ShouldForceInnocent(Mobile source, Mobile target)
        {
            if (source == null || target == null || source == target)
                return false;

            if (!IsPlayerSideAttacker(source))
                return false;

            if (IsOwnerOwnRelationship(source, target))
                return false;

            return IsProtectedTarget(target);
        }
    }
}
