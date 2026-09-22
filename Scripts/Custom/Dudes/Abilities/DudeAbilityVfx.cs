using System;
using System.Collections.Generic;
using Server.Engines.PartySystem;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Shared combat VFX helpers — damage formulas stay in each ability.
    /// Location particles are anchored on a plain Entity(Serial.Zero, loc, map), not EffectItem:
    /// classic clients get a location effect that never uses the anchor's serial, so an EffectItem
    /// only added an item packet, a tooltip packet and a remove packet per nearby client (plus a
    /// world item and a 5s timer on the server) for every effect.
    /// </summary>
    public static class DudeAbilityVfx
    {
        public static void PlayFireHit(Mobile target, bool withRing)
        {
            if (target == null || target.Deleted || target.Map == null || target.Map == Map.Internal)
                return;

            target.FixedParticles(0x3709, 10, 30, 5052, EffectLayer.LeftFoot);
            target.FixedParticles(0x36BD, 10, 20, 5052, EffectLayer.Waist);
            Effects.SendLocationParticles(
                new Entity(Serial.Zero, target.Location, target.Map),
                0x3709, 10, 25, 5052);

            if (withRing)
                PlayBriefFireRing(target.Location, target.Map, 1);

            target.PlaySound(0x208);
        }

        /// <summary>
        /// Light per-target flame for the once-a-second Burn passive: one particle effect, no sound
        /// (the Dude plays one per pulse). PlayFireHit is four packets per target to every nearby
        /// client, which suits a one-off hit but not a damage-over-time tick.
        /// </summary>
        public static void PlayBurnTick(Mobile target)
        {
            if (target == null || target.Deleted || target.Map == null || target.Map == Map.Internal)
                return;

            target.FixedParticles(0x3709, 10, 30, 5052, EffectLayer.LeftFoot);
        }

        public static void PlayBriefFireRing(Point3D center, Map map, int radius)
        {
            if (map == null || map == Map.Internal || radius < 1)
                return;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    if (Math.Abs(d - radius) > 0.6)
                        continue;

                    Point3D p = new Point3D(center.X + dx, center.Y + dy, center.Z);
                    Effects.SendLocationEffect(p, map, 0x3709, 12, 0, 0);
                    if (Utility.RandomBool())
                        Effects.SendLocationEffect(p, map, 0x36BD, 10, 0, 0);
                }
            }
        }

        public static void PlayEarthHit(Mobile target)
        {
            if (target == null || target.Deleted || target.Map == null || target.Map == Map.Internal)
                return;

            target.FixedParticles(0x36B0, 20, 14, 5044, EffectLayer.Head);
            target.FixedParticles(0x3728, 10, 16, 5044, 0x3B2, 0, EffectLayer.Waist);
            Effects.SendLocationParticles(
                new Entity(Serial.Zero, target.Location, target.Map),
                0x36B0, 10, 20, 0x3F, 0, 5044, 0);
            Effects.SendLocationEffect(target.Location, target.Map, 0x3728, 14, 0x3B2, 0);
            target.PlaySound(0x1F3);
        }

        public static void PlayWaterHit(Mobile target)
        {
            if (target == null || target.Deleted || target.Map == null || target.Map == Map.Internal)
                return;

            target.FixedParticles(0x3728, 10, 20, 5029, 0x47E, 0, EffectLayer.Waist);
            Effects.SendLocationParticles(
                new Entity(Serial.Zero, target.Location, target.Map),
                0x3728, 10, 22, 0x59B, 0, 5029, 0);
            Effects.SendLocationParticles(
                new Entity(Serial.Zero, target.Location, target.Map),
                0x36B0, 8, 14, 0x966, 0, 5044, 0);
            target.PlaySound(0x26);
        }

        public static void PlayWaterHeal(Mobile target)
        {
            if (target == null || target.Deleted || target.Map == null || target.Map == Map.Internal)
                return;

            target.FixedParticles(0x376A, 9, 32, 5005, 0x47E, 0, EffectLayer.Waist);
            target.PlaySound(0x1F2);
        }

        public static void PlayAirHit(Mobile target)
        {
            if (target == null || target.Deleted || target.Map == null || target.Map == Map.Internal)
                return;

            target.FixedParticles(0x37CC, 1, 20, 9917, 0x47E, 3, EffectLayer.Waist);
            Effects.SendLocationParticles(
                new Entity(Serial.Zero, target.Location, target.Map),
                0x37CC, 1, 18, 0x47E, 3, 9917, 0);
            Effects.SendBoltEffect(target, true, 0);
            target.PlaySound(0x1F5);
        }

        public static void PlayAirBuff(Mobile target)
        {
            if (target == null || target.Deleted || target.Map == null || target.Map == Map.Internal)
                return;

            target.FixedParticles(0x37CC, 1, 16, 9917, 0x47E, 3, EffectLayer.Waist);
            target.PlaySound(0x1F5);
        }

        /// <summary>
        /// Collect Combatant + Aggressors/Aggressed for any Mobile (linked player). No GetMobilesInRange.
        /// </summary>
        public static void CollectFightList(Mobile caster, Mobile master, List<Mobile> list)
        {
            if (caster == null || list == null)
                return;

            AddFightCandidateMobile(caster, master, list, caster.Combatant as Mobile);

            List<AggressorInfo> aggressors = caster.Aggressors;
            if (aggressors != null)
            {
                for (int i = 0; i < aggressors.Count; i++)
                {
                    AggressorInfo info = aggressors[i];
                    if (info == null || info.Expired)
                        continue;
                    AddFightCandidateMobile(caster, master, list, info.Attacker);
                }
            }

            List<AggressorInfo> aggressed = caster.Aggressed;
            if (aggressed != null)
            {
                for (int i = 0; i < aggressed.Count; i++)
                {
                    AggressorInfo info = aggressed[i];
                    if (info == null || info.Expired)
                        continue;
                    AddFightCandidateMobile(caster, master, list, info.Defender);
                }
            }
        }

        private static void AddFightCandidateMobile(Mobile caster, Mobile master, List<Mobile> list, Mobile m)
        {
            if (m == null || m == caster || m.Deleted || !m.Alive)
                return;
            if (master != null && m == master)
                return;
            // Never fight-list any Dude (any master).
            if (m is DudeCreature)
                return;
            // Skip same-master pets and master's party.
            if (DudeCreature.IsPackAlly(m, master))
                return;
            if (!caster.CanBeHarmful(m))
                return;

            BaseCreature bc = m as BaseCreature;
            if (bc != null && master != null && bc.Controlled && bc.ControlMaster == master)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == m)
                    return;
            }

            list.Add(m);
        }

        /// <summary>
        /// Collect Combatant + Aggressors/Aggressed (fight-list). No GetMobilesInRange.
        /// </summary>
        public static void CollectFightList(DudeCreature dude, List<Mobile> list)
        {
            if (dude == null || list == null)
                return;

            AddFightCandidate(dude, list, dude.Combatant as Mobile);

            List<AggressorInfo> aggressors = dude.Aggressors;
            if (aggressors != null)
            {
                for (int i = 0; i < aggressors.Count; i++)
                {
                    AggressorInfo info = aggressors[i];
                    if (info == null || info.Expired)
                        continue;
                    AddFightCandidate(dude, list, info.Attacker);
                }
            }

            List<AggressorInfo> aggressed = dude.Aggressed;
            if (aggressed != null)
            {
                for (int i = 0; i < aggressed.Count; i++)
                {
                    AggressorInfo info = aggressed[i];
                    if (info == null || info.Expired)
                        continue;
                    AddFightCandidate(dude, list, info.Defender);
                }
            }
        }

        private static void AddFightCandidate(DudeCreature dude, List<Mobile> list, Mobile m)
        {
            if (m == null || m == dude || m.Deleted || !m.Alive)
                return;
            if (m == dude.ControlMaster)
                return;
            // Never fight-list any Dude (any master).
            if (m is DudeCreature)
                return;
            // Skip same-master pets and master's party.
            if (dude.IsPackAlly(m))
                return;
            if (!dude.CanBeHarmful(m))
                return;

            BaseCreature bc = m as BaseCreature;
            if (bc != null && dude.ControlMaster != null
                && bc.Controlled && bc.ControlMaster == dude.ControlMaster)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == m)
                    return;
            }

            list.Add(m);
        }

        /// <summary>
        /// Owned DudeCreatures of master + party members within range of center.
        /// Iterates AllFollowers — no hostile GetMobilesInRange.
        /// </summary>
        public static void CollectPartyOwnedDudes(Mobile master, Point3D center, Map map, int range, List<DudeCreature> list)
        {
            if (list == null || map == null || map == Map.Internal)
                return;

            AddOwnedDudesOf(master, center, map, range, list);

            Party party = Party.Get(master);
            if (party == null || party.Members == null)
                return;

            for (int i = 0; i < party.Members.Count; i++)
            {
                PartyMemberInfo info = party.Members[i];
                if (info == null || info.Mobile == null || info.Mobile == master)
                    continue;
                AddOwnedDudesOf(info.Mobile, center, map, range, list);
            }
        }

        private static void AddOwnedDudesOf(Mobile master, Point3D center, Map map, int range, List<DudeCreature> list)
        {
            if (master == null || master.Deleted)
                return;

            PlayerMobile pm = master as PlayerMobile;
            List<Mobile> followers = pm != null ? pm.AllFollowers : null;
            if (followers == null || followers.Count == 0)
                return;

            for (int i = 0; i < followers.Count; i++)
            {
                DudeCreature dude = followers[i] as DudeCreature;
                if (dude == null || dude.Deleted || !dude.Alive)
                    continue;
                if (dude.Map != map)
                    continue;
                if (!dude.InRange(center, range))
                    continue;

                bool already = false;
                for (int j = 0; j < list.Count; j++)
                {
                    if (list[j] == dude)
                    {
                        already = true;
                        break;
                    }
                }
                if (!already)
                    list.Add(dude);
            }
        }

        public static void ApplyTailwindSpeedPlayer(Mobile m, TimeSpan duration)
        {
            if (m == null || m.Deleted || !m.Alive)
                return;

            // Linked form uses Dude Dex; MountSpeed omitted to avoid client packet flood.
            PlayAirBuff(m);
        }

        public static void ApplyTailwindSpeed(DudeCreature dude, TimeSpan duration, double speedFactor)
        {
            if (dude == null || dude.Deleted || dude.IsWild)
                return;

            if (speedFactor <= 0.0)
                speedFactor = 0.5;

            double previous = dude.ForceActiveSpeed;
            if (previous <= 0.0)
                previous = 0.1;

            // Lower ForceActiveSpeed = faster AI ticks / attack cadence.
            double buffed = Math.Max(0.05, previous * speedFactor);
            dude.ForceActiveSpeed = buffed;
            dude.ForcePassiveSpeed = buffed;
            dude.CurrentSpeed = buffed;
            PlayAirBuff(dude);

            Timer.DelayCall(duration, () =>
            {
                if (dude == null || dude.Deleted)
                    return;
                dude.ApplyDudeSpeeds();
            });
        }
    }
}
