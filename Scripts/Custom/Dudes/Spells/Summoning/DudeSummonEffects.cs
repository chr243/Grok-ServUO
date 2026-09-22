using System;
using Server.Items;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Short type-themed summon/despawn burst (~0.3–0.5s).
    /// Radius scales by ascension stage (1 / 2 / 3).
    /// Despawn mirrors Play with rings collapsing inward.
    /// </summary>
    public static class DudeSummonEffects
    {
        private const int RingStepMs = 100;
        private const int DespawnTailMs = 50;

        /// <summary>Summon/despawn radius now comes purely from the ascension stage (1 / 2 / 3).</summary>
        public static int GetSummonRadius(int stage)
        {
            return DudeStage.SummonRadius(stage);
        }

        public static void Play(DudeType type, Point3D center, Map map)
        {
            Play(type, center, map, 3);
        }

        public static void PlayForStage(DudeType type, Point3D center, Map map, int stage)
        {
            Play(type, center, map, GetSummonRadius(stage));
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

        public static TimeSpan GetDespawnDurationForStage(int stage)
        {
            return GetDespawnDuration(GetSummonRadius(stage));
        }

        public static void PlayDespawn(DudeType type, Point3D center, Map map)
        {
            PlayDespawn(type, center, map, 3);
        }

        public static void PlayDespawnForStage(DudeType type, Point3D center, Map map, int stage)
        {
            PlayDespawn(type, center, map, GetSummonRadius(stage));
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
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    if (Math.Abs(d - radius) > 0.6)
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
            IEntity ent = new Entity(Serial.Zero, p, map);

            switch (GetVfx(type))
            {
                case DudeVfx.Fire:
                    // Short particle flash (was 10/30).
                    Effects.SendLocationParticles(ent, 0x3709, 5, 10, 5052);
                    if (wave == 0)
                        Effects.SendLocationEffect(p, map, 0x36BD, 8);
                    break;

                case DudeVfx.Water:
                    Effects.SendLocationParticles(ent, 0x3728, 5, 8, 0x59B, 0, 5029, 0);
                    if (wave == 0)
                        Effects.SendLocationEffect(p, map, 0x3728, 6, 2101, 0);
                    break;

                case DudeVfx.Earth:
                    // Stonewarden Fault Line look: dirt burst (0x36BD) + stone spikes (0x36B0), hue 2413.
                    Effects.SendLocationParticles(ent, 0x36BD, 10, 30, 2413, 0, 5044, 0);
                    if (wave == 0)
                    {
                        Effects.SendLocationParticles(ent, 0x36B0, 8, 20, 2413, 0, 5044, 0);
                        Effects.SendLocationEffect(p, map, 0x36BD, 12, 8, 2413, 0);
                    }
                    break;

                case DudeVfx.Poison:
                    Effects.SendLocationParticles(ent, 0x374A, 5, 8, 0x44, 0, 5031, 0);
                    if (wave == 0)
                        Effects.SendLocationEffect(p, map, 0x3728, 6, 0x44, 0);
                    break;

                case DudeVfx.Air:
                default:
                    Effects.SendLocationParticles(ent, 0x37CC, 1, 8, 0x47E, 3, 9917, 0);
                    if (wave == 0)
                        Effects.SendBoltEffect(new Entity(Serial.Zero, p, map), true, 0);
                    break;
            }
        }

        private static DudeVfx GetVfx(DudeType type)
        {
            DudeTypeProfile p = DudeTypeProfiles.Get(type);
            return p != null ? p.Vfx : DudeVfx.Fire;
        }

        private static int GetSound(DudeType type)
        {
            DudeTypeProfile p = DudeTypeProfiles.Get(type);
            return p != null && p.SoundId > 0 ? p.SoundId : 0x208;
        }
    }
}
