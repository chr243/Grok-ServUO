using System;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
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
            target.FixedParticles(0x3709, 10, 30, 5052, EffectLayer.LeftFoot);
            target.PlaySound(0x208);
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
            target.FixedParticles(0x374A, 10, 15, 5021, EffectLayer.Waist);
            target.PlaySound(0x26);
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
            target.FixedParticles(0x36BD, 20, 10, 5044, EffectLayer.Head);
            target.PlaySound(0x1F3);
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
            target.FixedParticles(0x3728, 10, 15, 5013, EffectLayer.Waist);
            target.PlaySound(0x1F5);
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
            target.FixedParticles(0x3709, 10, 25, 5052, EffectLayer.Head);
            target.PlaySound(0x208);
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
            target.FixedParticles(0x374A, 10, 20, 5021, EffectLayer.Waist);
            target.PlaySound(0x26);
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
            target.FixedParticles(0x36BD, 20, 12, 5044, EffectLayer.Head);
            target.PlaySound(0x1F3);
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
            target.FixedParticles(0x3709, 10, 35, 5052, EffectLayer.LeftFoot);
            target.PlaySound(0x208);
        }
    }
}
