using System;
using System.Collections.Generic;
using Server.Custom.Dudes;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Network;

namespace Server.Mobiles
{
    /// <summary>
    /// Pack AI on top of the stock AI_Melee / AI_Mage: guard stance, owner-death recall,
    /// pack-ally / no-PvP target filtering, guard-safe aggression, follow/attack spread and
    /// the Dude Shield taunt.
    /// </summary>
    public partial class DudeCreature
    {
        private DateTime m_NextFollowSpread;
        private DateTime m_NextShieldTaunt;
        private DateTime m_NextTauntOverhead;

        /// <summary>
        /// Town guards focusing a summoned Dude must not trigger pet AI fight-back
        /// (Combatant ↔ Guard Focus ↔ DoHarmful recursion / StackOverflow).
        /// </summary>
        public override void AggressiveAction(Mobile aggressor, bool criminal)
        {
            if (m_Despawning)
            {
                // Still record aggression lists via base Mobile path without pet AI fight-back.
                // Avoid Combatant assignment against anyone while returning to ball.
                IDamageable old = Combatant;
                base.AggressiveAction(aggressor, criminal);
                Combatant = null;
                Warmode = false;
                return;
            }

            if (aggressor is BaseGuard)
            {
                IDamageable oldCombatant = Combatant;
                base.AggressiveAction(aggressor, criminal);

                // Undo BaseCreature AI / Combatant assignment against the guard.
                if (Combatant == aggressor)
                    Combatant = oldCombatant;

                if (ControlOrder == OrderType.Attack && ControlTarget == aggressor)
                {
                    ControlTarget = ControlMaster;
                    ControlOrder = OrderType.Guard;
                }

                return;
            }

            base.AggressiveAction(aggressor, criminal);
        }

        /// <summary>
        /// Never assign Combatant to a town guard (indirect DoHarmful) — breaks recursion.
        /// </summary>
        public override void DoHarmful(IDamageable target, bool indirect)
        {
            Mobile harmTarget = target as Mobile;
            if (harmTarget != null && Server.Custom.Dudes.DudeNoPvP.BlocksHarm(this, harmTarget))
                return;

            if (target is BaseGuard)
                base.DoHarmful(target, true);
            else
                base.DoHarmful(target, indirect);
        }

        public override void OnGaveMeleeAttack(Mobile defender)
        {
            base.OnGaveMeleeAttack(defender);

            if (m_IsWild || m_Fainting || m_Despawning)
                return;

            if (m_BoundBall == null || m_BoundBall.Deleted || m_BoundBall.StoredDude == null)
                return;

            DudeCombatSkills.TryGainOnHit(m_BoundBall.StoredDude, m_BoundBall, this);
        }

        public override void OnThink()
        {
            base.OnThink();

            if (m_IsWild || Deleted || Map == null || Map == Map.Internal || m_Fainting)
                return;

            if (m_Despawning)
            {
                Combatant = null;
                Warmode = false;
                return;
            }

            if (m_BoundBall != null && !m_BoundBall.Deleted && m_BoundBall.IsRecalling)
                return;

            // Owner died (or deleted) — auto return to ball.
            if (Controlled && ControlMaster != null && (ControlMaster.Deleted || !ControlMaster.Alive))
            {
                if (m_BoundBall != null && !m_BoundBall.Deleted)
                    m_BoundBall.Recall(ControlMaster);
                return;
            }

            if (!Controlled || ControlMaster == null || ControlMaster.Deleted)
                return;

            // Default combat stance is Guard — keep hunting nearby threats after each kill.
            // Do NOT force Follow when idle; that stopped aggression after the first corpse.
            if (ControlOrder == OrderType.None)
            {
                ControlTarget = ControlMaster;
                ControlOrder = OrderType.Guard;
            }
            else if (ControlOrder == OrderType.Guard && ControlTarget != ControlMaster)
            {
                ControlTarget = ControlMaster;
            }

            // Never attack pack allies / master / party, or no-PvP protected targets.
            Mobile thinkCombatant = Combatant as Mobile;
            if (thinkCombatant != null && (IsPackAlly(thinkCombatant) || Server.Custom.Dudes.DudeNoPvP.BlocksHarm(this, thinkCombatant)))
                Combatant = null;

            TrySpreadFollowOffset();
            TrySpreadAttackOffset();
            TryShieldTaunt();
            TryUseAbility();
            TryStage3Passives();
        }

        /// <summary>
        /// Dude Shield taunt tank: pull mobs already fighting the master/pack onto this Dude.
        /// Summoned only (OnThink early-out skips wild). 1s throttle. No pathing / InvalidateProperties.
        /// </summary>
        private void TryShieldTaunt()
        {
            if (!Alive || Deleted)
                return;

            DateTime now = DateTime.UtcNow;
            if (now < m_NextShieldTaunt)
                return;
            m_NextShieldTaunt = now + TimeSpan.FromSeconds(1.0);

            DudeShield shield = FindItemOnLayer(Layer.TwoHanded) as DudeShield;
            if (shield == null || shield.Deleted)
                return;

            Mobile master = ControlMaster;
            if (master == null || master.Deleted)
                return;

            if (Map == null || Map == Map.Internal)
                return;

            bool anyTaunt = false;
            IPooledEnumerable eable = GetMobilesInRange(8);

            foreach (Mobile m in eable)
            {
                if (m == null || m == this || m.Deleted || !m.Alive)
                    continue;
                if (m.Player || m.AccessLevel > AccessLevel.Player)
                    continue;
                if (m is BaseVendor || m is BaseGuard)
                    continue;

                // Never taunt an owned Dude (any master) — that would make owned Dudes fight.
                DudeCreature ownedDude = m as DudeCreature;
                if (ownedDude != null && ownedDude.ControlMaster != null)
                    continue;

                BaseCreature bc = m as BaseCreature;
                if (bc == null)
                    continue;

                // Do not taunt mobs that are fighting some other player.
                Mobile combatant = bc.Combatant as Mobile;
                if (combatant != null && combatant.Player && combatant != master)
                    continue;

                if (!IsEngagedWithPack(bc, master))
                    continue;

                // Only pull onto this tank — never onto some other Dude.
                if (bc.Combatant != this)
                {
                    bc.Combatant = this;
                    anyTaunt = true;
                }

                bc.Warmode = true;
            }

            eable.Free();

            if (anyTaunt && now >= m_NextTauntOverhead)
            {
                m_NextTauntOverhead = now + TimeSpan.FromSeconds(10.0);
                PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*taunts*");
            }
        }

        /// <summary>
        /// Pack ally of master: the master, same-master pets/Dudes, master's party members,
        /// and pets/Dudes owned by those party members. Wilds (master null) never match.
        /// </summary>
        public static bool IsPackAlly(Mobile m, Mobile master)
        {
            if (m == null || master == null || m.Deleted || master.Deleted)
                return false;
            if (m == master)
                return true;

            Server.Engines.PartySystem.Party party = Server.Engines.PartySystem.Party.Get(master);
            if (party != null && party.Contains(m))
                return true;

            BaseCreature bc = m as BaseCreature;
            if (bc != null)
            {
                Mobile theirMaster = bc.ControlMaster;
                if (theirMaster != null && !theirMaster.Deleted)
                {
                    if (theirMaster == master)
                        return true;
                    if (party != null && party.Contains(theirMaster))
                        return true;
                }
            }

            return false;
        }

        /// <summary>Instance pack check — false for wilds / no ControlMaster.</summary>
        public bool IsPackAlly(Mobile m)
        {
            if (m_IsWild || ControlMaster == null)
                return false;
            return IsPackAlly(m, ControlMaster);
        }

        public override bool IsEnemy(Mobile m)
        {
            if (IsPackAlly(m))
                return false;
            // Summoned Dudes: no PvP vs players, other summoned Dudes, or player pets.
            if (!m_IsWild && Server.Custom.Dudes.DudeNoPvP.IsProtectedTarget(m))
                return false;
            return base.IsEnemy(m);
        }

        public override bool IsFriend(Mobile m)
        {
            if (IsPackAlly(m))
                return true;
            return base.IsFriend(m);
        }

        public override bool CanBeHarmful(IDamageable damageable, bool message, bool ignoreOurBlessedness)
        {
            Mobile target = damageable as Mobile;
            if (target != null && Server.Custom.Dudes.DudeNoPvP.BlocksHarm(this, target))
            {
                if (message)
                    SendLocalizedMessage(1001018); // You can not perform negative acts on your target.
                return false;
            }

            return base.CanBeHarmful(damageable, message, ignoreOurBlessedness);
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public override IDamageable Combatant
        {
            get { return base.Combatant; }
            set
            {
                Mobile m = value as Mobile;
                if (m != null && (IsPackAlly(m) || Server.Custom.Dudes.DudeNoPvP.BlocksHarm(this, m)))
                {
                    if (base.Combatant != null)
                        base.Combatant = null;
                    return;
                }
                base.Combatant = value;
            }
        }

        /// <summary>
        /// True if the creature's Combatant is the master/pack Dude, or Aggressors/Aggressed
        /// includes the master or pack Dudes.
        /// </summary>
        private static bool IsEngagedWithPack(BaseCreature bc, Mobile master)
        {
            if (bc == null || master == null)
                return false;

            if (IsPackAlly(bc.Combatant as Mobile, master))
                return true;

            List<AggressorInfo> aggressors = bc.Aggressors;
            if (aggressors != null)
            {
                for (int i = 0; i < aggressors.Count; i++)
                {
                    AggressorInfo info = aggressors[i];
                    if (info == null || info.Expired)
                        continue;
                    if (IsPackAlly(info.Attacker, master))
                        return true;
                }
            }

            List<AggressorInfo> aggressed = bc.Aggressed;
            if (aggressed != null)
            {
                for (int i = 0; i < aggressed.Count; i++)
                {
                    AggressorInfo info = aggressed[i];
                    if (info == null || info.Expired)
                        continue;
                    if (IsPackAlly(info.Defender, master))
                        return true;
                }
            }

            return false;
        }

        private static readonly int[] FollowRingDX = { 1, 1, 0, -1, -1, -1, 0, 1 };
        private static readonly int[] FollowRingDY = { 0, 1, 1, 1, 0, -1, -1, -1 };

        /// <summary>
        /// Spread summoned followers onto ring tiles around the master so they do not stack.
        /// Only while Follow/Guard and follow speed is active — never overrides Attack/combat.
        /// </summary>
        private void TrySpreadFollowOffset()
        {
            if (ControlOrder != OrderType.Follow && ControlOrder != OrderType.Guard)
                return;

            // Follow pace only (do not override Attack / stay / stop).
            if (CurrentSpeed != ActiveSpeed)
                return;

            // Leave Guard combat alone — attack spread handles that.
            Mobile combatant = Combatant as Mobile;
            if (combatant != null && !combatant.Deleted && combatant.Alive)
                return;

            DateTime now = DateTime.UtcNow;
            if (now < m_NextFollowSpread)
                return;
            m_NextFollowSpread = now + TimeSpan.FromSeconds(1.0);

            Mobile master = ControlMaster;
            if (master == null || master.Deleted || Map == null || Map != master.Map)
                return;

            int index;
            int packCount;
            if (!TryGetPackIndex(master, out index, out packCount))
                return;

            Point3D dest;
            if (!TryResolveRingOffset(master.Location, master.Map, master.Z, index, packCount, out dest))
                return;

            // Already on a valid offset tile — do not shuffle every tick.
            if (X == dest.X && Y == dest.Y)
                return;

            if (AIObject != null)
                AIObject.WalkMobileRange(dest, 1, false, 0, 0);
        }

        /// <summary>
        /// Spread attacking Dudes onto ring tiles around the combatant so they do not stack
        /// on the target tile. Only micro-adjusts when already near the target (within 3).
        /// Does not cancel Attack or change RangeFight.
        /// </summary>
        private void TrySpreadAttackOffset()
        {
            Mobile target = Combatant as Mobile;
            if (target == null || target.Deleted || !target.Alive)
            {
                if (ControlOrder != OrderType.Attack)
                    return;

                target = ControlTarget as Mobile;
                if (target == null || target.Deleted || !target.Alive)
                    return;
            }

            if (Map == null || target.Map != Map)
                return;

            // Only micro-adjust when already near — do not pull across the map.
            if (!InRange(target, 3))
                return;

            DateTime now = DateTime.UtcNow;
            if (now < m_NextFollowSpread)
                return;
            m_NextFollowSpread = now + TimeSpan.FromSeconds(1.0);

            Mobile master = ControlMaster;
            if (master == null || master.Deleted)
                return;

            int index;
            int packCount;
            if (!TryGetPackIndex(master, out index, out packCount))
                return;

            Point3D dest;
            if (!TryResolveRingOffset(target.Location, target.Map, target.Z, index, packCount, out dest))
                return;

            // Already on a valid adjacent/offset tile — do not shuffle every tick.
            if (X == dest.X && Y == dest.Y)
                return;

            // Do not kite out of melee: if already hitting, stay unless dest is also melee-range.
            if (InRange(target, 1) && !target.InRange(dest, 1))
                return;

            if (AIObject != null)
                AIObject.WalkMobileRange(dest, 1, false, 0, 0);
        }

        private bool TryGetPackIndex(Mobile master, out int index, out int packCount)
        {
            index = -1;
            packCount = 0;

            List<DudeCreature> pack = CollectPackDudes(master);
            if (pack.Count == 0)
                return false;

            pack.Sort(delegate(DudeCreature a, DudeCreature b)
            {
                return a.Serial.CompareTo(b.Serial);
            });

            for (int i = 0; i < pack.Count; i++)
            {
                if (pack[i] == this)
                {
                    index = i;
                    break;
                }
            }
            if (index < 0)
                return false;

            packCount = pack.Count;
            return true;
        }

        private static List<DudeCreature> CollectPackDudes(Mobile master)
        {
            List<DudeCreature> pack = new List<DudeCreature>();
            PlayerMobile pm = master as PlayerMobile;
            List<Mobile> followers = pm != null ? pm.AllFollowers : null;
            if (followers == null)
                return pack;

            for (int i = 0; i < followers.Count; i++)
            {
                DudeCreature dude = followers[i] as DudeCreature;
                if (dude == null || dude.Deleted || dude.IsWild)
                    continue;
                if (!dude.Controlled || dude.ControlMaster != master)
                    continue;
                if (dude.Map != master.Map || !dude.InRange(master, 18))
                    continue;
                pack.Add(dude);
            }
            return pack;
        }

        /// <summary>
        /// Resolve a walkable 8-way ring tile around <paramref name="center"/> for pack index.
        /// Prefers distance 1; if blocked, next ring slot; then outer rings as pack grows.
        /// </summary>
        private static bool TryResolveRingOffset(Point3D center, Map map, int centerZ, int index, int packCount, out Point3D dest)
        {
            dest = Point3D.Zero;
            if (map == null || map == Map.Internal)
                return false;

            int rings = Math.Max(1, (packCount + 7) / 8);
            int slotCount = rings * 8;

            for (int attempt = 0; attempt < slotCount; attempt++)
            {
                int slot = (index + attempt) % slotCount;
                int ring = (slot / 8) + 1;
                int dir = slot % 8;
                int x = center.X + FollowRingDX[dir] * ring;
                int y = center.Y + FollowRingDY[dir] * ring;
                int z = centerZ;

                Point3D p = new Point3D(x, y, z);
                if (map.CanFit(p, 16, false, false))
                {
                    dest = p;
                    return true;
                }

                int avgZ = map.GetAverageZ(x, y);
                if (avgZ != z)
                {
                    p = new Point3D(x, y, avgZ);
                    if (map.CanFit(p, 16, false, false))
                    {
                        dest = p;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
