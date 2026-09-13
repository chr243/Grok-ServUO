using System;
using Server.Items;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Short type-themed summon burst: expanding ring over ~1 second.
    /// Radius scales by Dude tier (weak 1 / basic+medium 2 / strong 3).
    /// Despawn mirrors Play with rings collapsing inward.
    /// </summary>
    public static class DudeSummonEffects
    {
        public static int GetSummonRadius(DudeType type, string definitionId)
        {
            if (!string.IsNullOrEmpty(definitionId))
            {
                switch (definitionId.ToLowerInvariant())
                {
                    // Weak fodder + Embit
                    case "sparkmite":
                    case "puddling":
                    case "pebblet":
                    case "breezeling":
                    case "embit":
                        return 1;

                    // Basic starters + medium + Emberon
                    case "emberling":
                    case "tideling":
                    case "stonepaw":
                    case "gustling":
                    case "cinderfang":
                    case "riptide":
                    case "boulderback":
                    case "emberon":
                        return 2;

                    // Strong elite + Infernox
                    case "pyreclaw":
                    case "infernox":
                        return 3;
                }
            }

            // Fallback by type only — medium-small default
            return 2;
        }

        public static void Play(DudeType type, Point3D center, Map map)
        {
            Play(type, center, map, 3);
        }

        public static void Play(DudeType type, Point3D center, Map map, string definitionId)
        {
            Play(type, center, map, GetSummonRadius(type, definitionId));
        }

        public static void Play(DudeType type, Point3D center, Map map, int maxRadius)
        {
            if (map == null || map == Map.Internal)
                return;

            if (maxRadius < 1)
                maxRadius = 1;
            if (maxRadius > 3)
                maxRadius = 3;

            Effects.PlaySound(center, map, GetSound(type));

            // Center burst, then rings at r=1..maxRadius (~300ms apart).
            PlayAt(type, center, map, 0);

            for (int r = 1; r <= maxRadius; r++)
            {
                int radius = r;
                Timer.DelayCall(TimeSpan.FromMilliseconds(300 * radius), () =>
                {
                    PlayRing(type, center, map, radius);
                });
            }
        }

        /// <summary>
        /// Inward despawn: outer ring first, then collapse to center.
        /// </summary>
        public static void PlayDespawn(DudeType type, Point3D center, Map map)
        {
            PlayDespawn(type, center, map, 3);
        }

        public static void PlayDespawn(DudeType type, Point3D center, Map map, string definitionId)
        {
            PlayDespawn(type, center, map, GetSummonRadius(type, definitionId));
        }

        public static void PlayDespawn(DudeType type, Point3D center, Map map, int maxRadius)
        {
            if (map == null || map == Map.Internal)
                return;

            if (maxRadius < 1)
                maxRadius = 1;
            if (maxRadius > 3)
                maxRadius = 3;

            Effects.PlaySound(center, map, GetSound(type));

            // Outer ring first, then collapse — delays 0, 300, ... then center.
            for (int r = maxRadius; r >= 1; r--)
            {
                int radius = r;
                int delayIndex = maxRadius - radius;
                Timer.DelayCall(TimeSpan.FromMilliseconds(300 * delayIndex), () =>
                {
                    PlayRing(type, center, map, radius);
                });
            }

            Timer.DelayCall(TimeSpan.FromMilliseconds(300 * maxRadius), () =>
            {
                PlayAt(type, center, map, 0);
            });
        }

        private static void PlayRing(DudeType type, Point3D center, Map map, int radius)
        {
            if (map == null || map == Map.Internal || radius < 1)
                return;

            for (int dx = -radius; dx <= radius; dx++)
            {
                for (int dy = -radius; dy <= radius; dy++)
                {
                    int adx = dx < 0 ? -dx : dx;
                    int ady = dy < 0 ? -dy : dy;

                    // Chebyshev ring; skip far corners so it reads rounder.
                    if (Math.Max(adx, ady) != radius)
                        continue;
                    if (adx == radius && ady == radius && radius > 1)
                        continue;

                    PlayAt(type, new Point3D(center.X + dx, center.Y + dy, center.Z), map, radius);
                }
            }
        }

        private static void PlayAt(DudeType type, Point3D loc, Map map, int wave)
        {
            int z = loc.Z;
            try
            {
                z = map.GetAverageZ(loc.X, loc.Y);
            }
            catch
            {
            }

            Point3D p = new Point3D(loc.X, loc.Y, z);
            IEntity ent = EffectItem.Create(p, map, EffectItem.DefaultDuration);

            switch (type)
            {
                case DudeType.Fire:
                    Effects.SendLocationParticles(ent, 0x3709, 10, 30, 5052);
                    if (wave == 0 || Utility.RandomBool())
                        Effects.SendLocationEffect(p, map, 0x36BD, 16);
                    break;

                case DudeType.Water:
                    // Particle splashes — muted hues, no ugly 0x352D water tiles.
                    Effects.SendLocationParticles(ent, 0x3728, 10, 20, 0x59B, 0, 5029, 0);
                    Effects.SendLocationParticles(
                        EffectItem.Create(p, map, EffectItem.DefaultDuration),
                        0x36B0, 10, 16, 0x966, 0, 5044, 0);
                    if (wave == 0 || Utility.RandomBool())
                        Effects.SendLocationEffect(p, map, 0x3728, 12, 2101, 0);
                    break;

                case DudeType.Earth:
                    Effects.SendLocationParticles(ent, 0x36B0, 10, 20, 0x3F, 0, 5044, 0);
                    Effects.SendLocationEffect(p, map, 0x3728, 12, 0x3B2, 0);
                    break;

                case DudeType.Air:
                default:
                    Effects.SendLocationParticles(ent, 0x37CC, 1, 20, 0x47E, 3, 9917, 0);
                    if (wave == 0 || Utility.Random(3) == 0)
                        Effects.SendBoltEffect(new Entity(Serial.Zero, p, map), true, 0);
                    break;
            }
        }

        private static int GetSound(DudeType type)
        {
            switch (type)
            {
                case DudeType.Fire:
                    return 0x208;
                case DudeType.Water:
                    return 0x026;
                case DudeType.Earth:
                    return 0x2F3;
                case DudeType.Air:
                default:
                    return 0x29;
            }
        }
    }
}
