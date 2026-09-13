using System;
using System.Collections.Generic;
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

            // Extra rocks — brown hues, falling-rock feel
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

            // Extra splashes — cyan particles, no ugly 0x352D tiles
            target.FixedParticles(0x3728, 10, 20, 5029, 0x47E, 0, EffectLayer.Waist);
            Effects.SendLocationParticles(
                EffectItem.Create(target.Location, target.Map, EffectItem.DefaultDuration),
                0x3728, 10, 22, 0x59B, 0, 5029, 0);
            Effects.SendLocationParticles(
                EffectItem.Create(target.Location, target.Map, EffectItem.DefaultDuration),
                0x36B0, 8, 14, 0x966, 0, 5044, 0);
            target.PlaySound(0x26);
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
    }

    /// <summary>
    /// Dude combat abilities. Damage formula: base + (DudeLevel * 2). Expand by adding classes + registry entries.
    /// Damage uses ServUO AOS.Damage helper (works under UOR config; classic HP reduction).
    /// </summary>
    public sealed class EmberBurstAbility : DudeAbility
    {
        public EmberBurstAbility()
            : base("ember_burst", "Ember Burst", TimeSpan.FromSeconds(12.0), 5)
        {
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = 8 + (dude.DudeLevel * 2);
            dude.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Ember Burst*");
            AOS.Damage(target, dude, damage, 0, 100, 0, 0, 0);
            DudeAbilityVfx.PlayFireHit(target, true);
        }
    }

    public sealed class TideCrashAbility : DudeAbility
    {
        public TideCrashAbility()
            : base("tide_crash", "Tide Crash", TimeSpan.FromSeconds(12.0), 5)
        {
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = 7 + (dude.DudeLevel * 2);
            dude.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*Tide Crash*");
            AOS.Damage(target, dude, damage, 0, 0, 100, 0, 0);
            DudeAbilityVfx.PlayWaterHit(target);
        }
    }

    public sealed class StoneSlamAbility : DudeAbility
    {
        public StoneSlamAbility()
            : base("stone_slam", "Stone Slam", TimeSpan.FromSeconds(14.0), 5)
        {
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = 10 + (dude.DudeLevel * 2);
            dude.PublicOverheadMessage(MessageType.Regular, 0x3F, false, "*Stone Slam*");
            AOS.Damage(target, dude, damage, 100, 0, 0, 0, 0);
            DudeAbilityVfx.PlayEarthHit(target);
        }
    }

    public sealed class GustSlashAbility : DudeAbility
    {
        public GustSlashAbility()
            : base("gust_slash", "Gust Slash", TimeSpan.FromSeconds(10.0), 5)
        {
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = 6 + (dude.DudeLevel * 2);
            dude.PublicOverheadMessage(MessageType.Regular, 0x47E, false, "*Gust Slash*");
            AOS.Damage(target, dude, damage, 0, 0, 0, 0, 100);
            DudeAbilityVfx.PlayAirHit(target);
        }
    }

    // --- Medium abilities (higher base, same level*2 scaling) ---

    public sealed class CinderBiteAbility : DudeAbility
    {
        public CinderBiteAbility()
            : base("cinder_bite", "Cinder Bite", TimeSpan.FromSeconds(11.0), 6)
        {
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = 12 + (dude.DudeLevel * 2);
            dude.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Cinder Bite*");
            AOS.Damage(target, dude, damage, 0, 100, 0, 0, 0);
            DudeAbilityVfx.PlayFireHit(target, true);
        }
    }

    public sealed class RiptideCrashAbility : DudeAbility
    {
        public RiptideCrashAbility()
            : base("riptide_crash", "Riptide Crash", TimeSpan.FromSeconds(11.0), 6)
        {
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = 11 + (dude.DudeLevel * 2);
            dude.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*Riptide Crash*");
            AOS.Damage(target, dude, damage, 0, 0, 100, 0, 0);
            DudeAbilityVfx.PlayWaterHit(target);
        }
    }

    public sealed class BoulderCrushAbility : DudeAbility
    {
        public BoulderCrushAbility()
            : base("boulder_crush", "Boulder Crush", TimeSpan.FromSeconds(13.0), 7)
        {
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = 14 + (dude.DudeLevel * 2);
            dude.PublicOverheadMessage(MessageType.Regular, 0x3F, false, "*Boulder Crush*");
            AOS.Damage(target, dude, damage, 100, 0, 0, 0, 0);
            DudeAbilityVfx.PlayEarthHit(target);
        }
    }

    // --- Strong elite ability ---

    public sealed class PyreBlastAbility : DudeAbility
    {
        public PyreBlastAbility()
            : base("pyre_blast", "Pyre Blast", TimeSpan.FromSeconds(10.0), 8)
        {
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = 18 + (dude.DudeLevel * 2);
            dude.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Pyre Blast*");
            AOS.Damage(target, dude, damage, 0, 100, 0, 0, 0);
            DudeAbilityVfx.PlayFireHit(target, true);
            DudeAbilityVfx.PlayBriefFireRing(target.Location, target.Map, 2);
        }
    }

    // --- Embit evolution line ---

    public sealed class BlastAbility : DudeAbility
    {
        public BlastAbility()
            : base("blast", "Blast", TimeSpan.FromSeconds(12.0), 5)
        {
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            int damage = DudeExperience.GetBlastDamage(dude.DudeLevel);
            dude.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Blast*");
            AOS.Damage(target, dude, damage, 0, 100, 0, 0, 0);
            Effects.SendMovingEffect(dude, target, 0x36BD, 7, 0, false, false, 0, 0);
            DudeAbilityVfx.PlayFireHit(target, true);
        }
    }

    public sealed class RingOfFireAbility : DudeAbility
    {
        public const int AoERange = 5;

        public RingOfFireAbility()
            : base("ring_of_fire", "Ring of Fire", TimeSpan.FromSeconds(18.0), 8)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            // AOE: combatant (or any live hostile later) is enough for CD/mana gate.
            if (dude == null || dude.Deleted)
                return false;

            if (dude.IsWild)
                return false;

            if (DateTime.UtcNow < dude.NextAbilityTime)
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

            // Visual ring on tiles around the Dude — extra flame particles.
            for (int dx = -AoERange; dx <= AoERange; dx++)
            {
                for (int dy = -AoERange; dy <= AoERange; dy++)
                {
                    int adx = dx < 0 ? -dx : dx;
                    int ady = dy < 0 ? -dy : dy;
                    int cheb = Math.Max(adx, ady);
                    if (cheb != AoERange && cheb != AoERange - 1)
                        continue;
                    if (adx == AoERange && ady == AoERange)
                        continue;

                    Point3D p = new Point3D(dude.X + dx, dude.Y + dy, dude.Z);
                    Effects.SendLocationEffect(p, dude.Map, 0x3709, 16, 0, 0);
                    if (Utility.RandomBool())
                        Effects.SendLocationEffect(p, dude.Map, 0x36BD, 12, 0, 0);
                }
            }

            List<Mobile> list = new List<Mobile>();
            foreach (Mobile m in dude.GetMobilesInRange(AoERange))
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

                list.Add(m);
            }

            for (int i = 0; i < list.Count; i++)
            {
                Mobile m = list[i];
                dude.DoHarmful(m);
                AOS.Damage(m, dude, damage, 0, 100, 0, 0, 0);
                DudeAbilityVfx.PlayFireHit(m, false);
            }
        }
    }

    /// <summary>
    /// Display-only stub for Infernox passive Burn. Combat is handled in DudeCreature.TryInfernoxPassive.
    /// </summary>
    public sealed class BurnAbility : DudeAbility
    {
        public BurnAbility()
            : base("burn", "Burn", TimeSpan.FromSeconds(9999.0), 0)
        {
        }

        public override bool CanExecute(DudeCreature dude, Mobile target)
        {
            return false; // passive only — never fired via TryUseAbility
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            // no-op
        }
    }
}
