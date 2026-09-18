using System;
using System.Collections.Generic;
using Server.Engines.PartySystem;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Shared combat VFX helpers — damage formulas stay in each ability.
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
                EffectItem.Create(target.Location, target.Map, EffectItem.DefaultDuration),
                0x3709, 10, 25, 5052);

            if (withRing)
                PlayBriefFireRing(target.Location, target.Map, 1);

            target.PlaySound(0x208);
        }

        public static void PlayBriefFireRing(Point3D center, Map map, int radius)
        {
            if (map == null || map == Map.Internal || radius < 1)
                return;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int adx = dx < 0 ? -dx : dx;
                    int ady = dy < 0 ? -dy : dy;
                    if (Math.Max(adx, ady) != radius)
                        continue;
                    if (adx == radius && ady == radius)
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
                EffectItem.Create(target.Location, target.Map, EffectItem.DefaultDuration),
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
                EffectItem.Create(target.Location, target.Map, EffectItem.DefaultDuration),
                0x3728, 10, 22, 0x59B, 0, 5029, 0);
            Effects.SendLocationParticles(
                EffectItem.Create(target.Location, target.Map, EffectItem.DefaultDuration),
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
                EffectItem.Create(target.Location, target.Map, EffectItem.DefaultDuration),
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

        public static void ApplyTailwindSpeed(DudeCreature dude, TimeSpan duration)
        {
            if (dude == null || dude.Deleted || dude.IsWild)
                return;

            double previous = dude.ForceActiveSpeed;
            if (previous <= 0.0)
                previous = 0.1;

            // Lower ForceActiveSpeed = faster AI ticks / attack cadence.
            double buffed = Math.Max(0.05, previous * 0.5);
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

    // --- S1 (~10s CD) ---

    public sealed class BlastAbility : DudeAbility
    {
        public BlastAbility()
            : base("blast", "Fire Blast", TimeSpan.FromSeconds(10.0), 5, 1, DudeType.Fire)
        {
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = DudeExperience.GetBlastDamage(dude.DudeLevel);
            dude.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Fire Blast*");
            AOS.Damage(target, dude, damage, 0, 100, 0, 0, 0);
            Effects.SendMovingEffect(dude, target, 0x36BD, 7, 0, false, false, 0, 0);
            DudeAbilityVfx.PlayFireHit(target, true);
        }
    }

    public sealed class TideMendAbility : DudeAbility
    {
        public TideMendAbility()
            : base("tide_mend", "Tide Mend", TimeSpan.FromSeconds(10.0), 5, 1, DudeType.Water)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            if (dude == null || dude.Deleted)
                return false;
            if (dude.IsWild)
                return false;
            if (ManaCost > 0 && dude.Mana < ManaCost)
                return false;
            if (target == null || target.Deleted || !target.Alive)
                return false;
            return true;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int heal = DudeExperience.GetBlastDamage(dude.DudeLevel);
            dude.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*Tide Mend*");
            dude.Hits = Math.Min(dude.HitsMax, dude.Hits + heal);
            DudeAbilityVfx.PlayWaterHeal(dude);
        }
    }

    public sealed class FaultStrikeAbility : DudeAbility
    {
        public FaultStrikeAbility()
            : base("fault_strike", "Fault Strike", TimeSpan.FromSeconds(10.0), 5, 1, DudeType.Earth)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            if (!base.CanExecute(dude, target))
                return false;
            if (target is PlayerMobile)
                return false;
            return true;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            if (target is PlayerMobile)
                return;

            int damage = DudeExperience.GetBlastDamage(dude.DudeLevel);
            dude.PublicOverheadMessage(MessageType.Regular, 0x3F, false, "*Fault Strike*");
            AOS.Damage(target, dude, damage, 100, 0, 0, 0, 0);
            DudeAbilityVfx.PlayEarthHit(target);
            target.Paralyze(TimeSpan.FromSeconds(1.0));
        }
    }

    public sealed class TailwindSelfAbility : DudeAbility
    {
        public TailwindSelfAbility()
            : base("tailwind_self", "Tailwind Self", TimeSpan.FromSeconds(10.0), 5, 1, DudeType.Air)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            if (dude == null || dude.Deleted)
                return false;
            if (dude.IsWild)
                return false;
            if (ManaCost > 0 && dude.Mana < ManaCost)
                return false;
            if (target == null || target.Deleted || !target.Alive)
                return false;
            return true;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            dude.PublicOverheadMessage(MessageType.Regular, 0x47E, false, "*Tailwind*");
            DudeAbilityVfx.ApplyTailwindSpeed(dude, TimeSpan.FromSeconds(5.0));
        }
    }

    // --- S2 (~12s CD) ---

    public sealed class RingOfFireAbility : DudeAbility
    {
        public const int AoERange = 5;

        public RingOfFireAbility()
            : base("ring_of_fire", "Ring of Fire", TimeSpan.FromSeconds(12.0), 8, 2, DudeType.Fire)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            if (dude == null || dude.Deleted)
                return false;
            if (dude.IsWild)
                return false;
            if (ManaCost > 0 && dude.Mana < ManaCost)
                return false;
            if (target == null || target.Deleted || !target.Alive)
                return false;
            return true;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = Math.Max(1, DudeExperience.GetBlastDamage(dude.DudeLevel) / 2);
            dude.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Ring of Fire*");
            dude.PlaySound(0x208);

            Point3D center = dude.Location;
            Map map = dude.Map;
            if (map == null || map == Map.Internal)
                return;

            for (int r = 1; r <= AoERange; r++)
            {
                int radius = r;
                Timer.DelayCall(TimeSpan.FromMilliseconds(250 * (radius - 1)), () =>
                {
                    if (dude == null || dude.Deleted || map == null || map == Map.Internal)
                        return;

                    PlayExpandingRing(dude, center, map, radius, damage);
                });
            }
        }

        private static void PlayExpandingRing(DudeCreature dude, Point3D center, Map map, int radius, int damage)
        {
            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int adx = dx < 0 ? -dx : dx;
                    int ady = dy < 0 ? -dy : dy;
                    if (Math.Max(adx, ady) != radius)
                        continue;
                    if (adx == radius && ady == radius && radius > 1)
                        continue;

                    int z = center.Z;
                    try { z = map.GetAverageZ(center.X + dx, center.Y + dy); } catch { }

                    Point3D p = new Point3D(center.X + dx, center.Y + dy, z);
                    Effects.SendLocationEffect(p, map, 0x3709, 16, 0, 0);
                    if (Utility.RandomBool())
                        Effects.SendLocationEffect(p, map, 0x36BD, 12, 0, 0);
                }
            }

            foreach (Mobile m in map.GetMobilesInRange(center, radius))
            {
                if (m == null || m == dude || m.Deleted || !m.Alive)
                    continue;
                if (m == dude.ControlMaster)
                    continue;
                if (!dude.CanBeHarmful(m))
                    continue;

                BaseCreature bc = m as BaseCreature;
                if (bc != null && bc.Controlled && bc.ControlMaster == dude.ControlMaster)
                    continue;

                int dist = Math.Max(Math.Abs(m.X - center.X), Math.Abs(m.Y - center.Y));
                if (dist != radius)
                    continue;

                dude.DoHarmful(m);
                AOS.Damage(m, dude, damage, 0, 100, 0, 0, 0);
                DudeAbilityVfx.PlayFireHit(m, false);
            }
        }
    }

    public sealed class TideChorusAbility : DudeAbility
    {
        public const int ChorusRange = 8;

        public TideChorusAbility()
            : base("tide_chorus", "Tide Chorus", TimeSpan.FromSeconds(12.0), 8, 2, DudeType.Water)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            if (dude == null || dude.Deleted)
                return false;
            if (dude.IsWild)
                return false;
            if (ManaCost > 0 && dude.Mana < ManaCost)
                return false;
            if (target == null || target.Deleted || !target.Alive)
                return false;
            return true;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            dude.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*Tide Chorus*");

            List<DudeCreature> allies = new List<DudeCreature>();
            DudeAbilityVfx.CollectPartyOwnedDudes(dude.ControlMaster, dude.Location, dude.Map, ChorusRange, allies);

            // Always include self if in range of own location.
            bool hasSelf = false;
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] == dude)
                {
                    hasSelf = true;
                    break;
                }
            }
            if (!hasSelf)
                allies.Add(dude);

            int blastHeal = DudeExperience.GetBlastDamage(dude.DudeLevel);

            for (int i = 0; i < allies.Count; i++)
            {
                DudeCreature ally = allies[i];
                if (ally == null || ally.Deleted || !ally.Alive)
                    continue;

                int pctHeal = Math.Max(1, (int)(ally.HitsMax * 0.20));
                int heal = Math.Min(pctHeal, blastHeal);
                if (heal < 1)
                    heal = 1;

                ally.Hits = Math.Min(ally.HitsMax, ally.Hits + heal);
                DudeAbilityVfx.PlayWaterHeal(ally);
            }
        }
    }

    public sealed class AftershockAbility : DudeAbility
    {
        public AftershockAbility()
            : base("aftershock", "Aftershock", TimeSpan.FromSeconds(12.0), 8, 2, DudeType.Earth)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            if (dude == null || dude.Deleted)
                return false;
            if (dude.IsWild)
                return false;
            if (ManaCost > 0 && dude.Mana < ManaCost)
                return false;
            if (target == null || target.Deleted || !target.Alive)
                return false;
            return true;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = Math.Max(1, DudeExperience.GetBlastDamage(dude.DudeLevel) / 2);
            dude.PublicOverheadMessage(MessageType.Regular, 0x3F, false, "*Aftershock*");
            dude.PlaySound(0x1F3);

            List<Mobile> list = new List<Mobile>();
            DudeAbilityVfx.CollectFightList(dude, list);

            for (int i = 0; i < list.Count; i++)
            {
                Mobile m = list[i];
                if (m == null || m.Deleted || !m.Alive)
                    continue;
                if (m is PlayerMobile)
                    continue;
                if (m is DudeCreature)
                    continue;

                // Prefer nearby fight-list targets (Chebyshev ≤ 3).
                int dist = Math.Max(Math.Abs(m.X - dude.X), Math.Abs(m.Y - dude.Y));
                if (dist > 3)
                    continue;

                dude.DoHarmful(m);
                AOS.Damage(m, dude, damage, 100, 0, 0, 0, 0);
                DudeAbilityVfx.PlayEarthHit(m);
                m.Paralyze(TimeSpan.FromSeconds(1.0));
            }
        }
    }

    public sealed class TailwindAbility : DudeAbility
    {
        public const int TailwindRange = 8;

        public TailwindAbility()
            : base("tailwind", "Tailwind", TimeSpan.FromSeconds(12.0), 8, 2, DudeType.Air)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            if (dude == null || dude.Deleted)
                return false;
            if (dude.IsWild)
                return false;
            if (ManaCost > 0 && dude.Mana < ManaCost)
                return false;
            if (target == null || target.Deleted || !target.Alive)
                return false;
            return true;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            dude.PublicOverheadMessage(MessageType.Regular, 0x47E, false, "*Tailwind*");

            List<DudeCreature> allies = new List<DudeCreature>();
            DudeAbilityVfx.CollectPartyOwnedDudes(dude.ControlMaster, dude.Location, dude.Map, TailwindRange, allies);

            bool hasSelf = false;
            for (int i = 0; i < allies.Count; i++)
            {
                if (allies[i] == dude)
                {
                    hasSelf = true;
                    break;
                }
            }
            if (!hasSelf)
                allies.Add(dude);

            for (int i = 0; i < allies.Count; i++)
                DudeAbilityVfx.ApplyTailwindSpeed(allies[i], TimeSpan.FromSeconds(5.0));
        }
    }

    // --- S3 passives (stubs; combat in DudeCreature think) ---

    public sealed class BurnAbility : DudeAbility
    {
        public BurnAbility()
            : base("burn", "Burn", TimeSpan.FromSeconds(9999.0), 0, 3, DudeType.Fire)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            return false;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
        }
    }

    public sealed class SpringAbility : DudeAbility
    {
        public SpringAbility()
            : base("spring", "Spring", TimeSpan.FromSeconds(9999.0), 0, 3, DudeType.Water)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            return false;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
        }
    }

    public sealed class FaultlineAbility : DudeAbility
    {
        public FaultlineAbility()
            : base("faultline", "Faultline", TimeSpan.FromSeconds(9999.0), 0, 3, DudeType.Earth)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            return false;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
        }
    }

    public sealed class SlipstreamAbility : DudeAbility
    {
        public SlipstreamAbility()
            : base("slipstream", "Slipstream", TimeSpan.FromSeconds(9999.0), 0, 3, DudeType.Air)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            return false;
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
        }
    }
}
