using System;
using Server.Items;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Short type-themed summon/despawn burst (~0.3–0.5s).
    /// Radius scales by Dude tier (weak 1 / basic+medium 2 / strong 3).
    /// Despawn mirrors Play with rings collapsing inward.
    /// </summary>
    public static class DudeSummonEffects
    {
        private const int RingStepMs = 100;
        private const int DespawnTailMs = 50;

        public static int GetSummonRadius(DudeType type, string definitionId)
        {
            if (!string.IsNullOrEmpty(definitionId))
            {
                switch (definitionId.ToLowerInvariant())
                {
                    case "ember":
                    case "droplet":
                    case "pebble":
                    case "breeze":
                        return 1;

                    case "flame":
                    case "ripple":
                    case "boulder":
                    case "gale":
                        return 2;

                    case "blaze":
                    case "torrent":
                    case "quake":
                    case "hurricane":
                        return 3;
                }
            }

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

            // Center burst, then quick rings (~100ms apart → ~0.3–0.4s total).
            PlayAt(type, center, map, 0);

            for (int r = 1; r <= maxRadius; r++)
            {
                int radius = r;
                Timer.DelayCall(TimeSpan.FromMilliseconds(RingStepMs * radius), () =>
                {
                    PlayRing(type, center, map, radius);
                });
            }
        }

        /// <summary>Total inward despawn length (matches PlayDespawn timers).</summary>
        public static TimeSpan GetDespawnDuration(int maxRadius)
        {
            if (maxRadius < 1)
                maxRadius = 1;
            if (maxRadius > 3)
                maxRadius = 3;

            return TimeSpan.FromMilliseconds(RingStepMs * maxRadius + DespawnTailMs);
        }

        public static TimeSpan GetDespawnDuration(DudeType type, string definitionId)
        {
            return GetDespawnDuration(GetSummonRadius(type, definitionId));
        }

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

            for (int r = maxRadius; r >= 1; r--)
            {
                int radius = r;
                int delayIndex = maxRadius - radius;
                Timer.DelayCall(TimeSpan.FromMilliseconds(RingStepMs * delayIndex), () =>
                {
                    PlayRing(type, center, map, radius);
                });
            }

            Timer.DelayCall(TimeSpan.FromMilliseconds(RingStepMs * maxRadius), () =>
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
                    // Short particle flash (was 10/30).
                    Effects.SendLocationParticles(ent, 0x3709, 5, 10, 5052);
                    if (wave == 0)
                        Effects.SendLocationEffect(p, map, 0x36BD, 8);
                    break;

                case DudeType.Water:
                    Effects.SendLocationParticles(ent, 0x3728, 5, 8, 0x59B, 0, 5029, 0);
                    if (wave == 0)
                        Effects.SendLocationEffect(p, map, 0x3728, 6, 2101, 0);
                    break;

                case DudeType.Earth:
                    Effects.SendLocationParticles(ent, 0x36B0, 5, 8, 0x3F, 0, 5044, 0);
                    if (wave == 0)
                        Effects.SendLocationEffect(p, map, 0x3728, 6, 0x3B2, 0);
                    break;

                case DudeType.Air:
                default:
                    Effects.SendLocationParticles(ent, 0x37CC, 1, 8, 0x47E, 3, 9917, 0);
                    if (wave == 0)
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
