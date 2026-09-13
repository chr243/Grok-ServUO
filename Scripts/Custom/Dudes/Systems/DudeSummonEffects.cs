using System;
using Server.Items;
using Server.Network;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Short type-themed summon burst: expanding ring out to 3 tiles over ~1 second.
    /// </summary>
    public static class DudeSummonEffects
    {
        private const int MaxRadius = 3;

        public static void Play(DudeType type, Point3D center, Map map)
        {
            if (map == null || map == Map.Internal)
                return;

            Effects.PlaySound(center, map, GetSound(type));

            // Center burst, then rings at r=1..3 (~300ms apart ≈ 1s total).
            PlayAt(type, center, map, 0);

            for (int r = 1; r <= MaxRadius; r++)
            {
                int radius = r;
                Timer.DelayCall(TimeSpan.FromMilliseconds(300 * radius), () =>
                {
                    PlayRing(type, center, map, radius);
                });
            }
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
                    Effects.SendLocationParticles(ent, 0x3728, 10, 20, 0x47E, 0, 5029, 0);
                    Effects.SendLocationEffect(p, map, 0x352D, 16, 0x47F, 0);
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
