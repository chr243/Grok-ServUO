using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
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
            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("ring_of_fire");
            double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.5;
            int damage = DudeAbility.ApplyEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(dude.DudeLevel) * vs)));
            dude.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Ring of Fire*");
            dude.PlaySound(0x208);

            Point3D center = dude.Location;
            Map map = dude.Map;
            if (map == null || map == Map.Internal)
                return;

            for (int r = 1; r <= AoERange; r++)
            {
                int radius = r;
                Timer.DelayCall(TimeSpan.FromMilliseconds(150 * (radius - 1)), () =>
                {
                    if (dude == null || dude.Deleted || map == null || map == Map.Internal)
                        return;

                    PlayExpandingRing(dude, dude.ControlMaster, center, map, radius, damage);
                });
            }
        }

        public override bool CanExecuteLinked(Mobile caster, DudeData data, Mobile target)
        {
            if (caster == null || caster.Deleted || !caster.Alive)
                return false;
            if (data == null)
                return false;
            if (ManaCost > 0 && caster.Mana < ManaCost)
                return false;
            if (target == null || target.Deleted || !target.Alive)
                return false;
            return true;
        }

        public override void ExecuteLinked(Mobile caster, DudeData data, DudeBall ball, Mobile target)
        {
            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("ring_of_fire");
            double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.5;
            int damage = DudeAbility.ApplyEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(data != null ? data.Level : 1) * vs)));
            caster.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Ring of Fire*");
            caster.PlaySound(0x208);

            Point3D center = caster.Location;
            Map map = caster.Map;
            if (map == null || map == Map.Internal)
                return;

            for (int r = 1; r <= AoERange; r++)
            {
                int radius = r;
                Timer.DelayCall(TimeSpan.FromMilliseconds(150 * (radius - 1)), () =>
                {
                    if (caster == null || caster.Deleted || map == null || map == Map.Internal)
                        return;

                    PlayExpandingRing(caster, caster, center, map, radius, damage);
                });
            }
        }

        // Visual spacing between flames on each ring, in tiles.
        private const double FlameSpacing = 1.5;

        private static void PlayExpandingRing(Mobile caster, Mobile master, Point3D center, Map map, int radius, int damage)
        {
            // Flames at evenly spaced points around the ring (at least 8). Drawing every ring tile
            // plus a random second effect on half of them was ~160 location effects per cast, each a
            // packet to every client in range; this is ~67. Damage below still covers every tile.
            int points = Math.Max(8, (int)Math.Round(2.0 * Math.PI * radius / FlameSpacing));
            int firstDx = 0, firstDy = 0, lastDx = 0, lastDy = 0;

            for (int i = 0; i < points; i++)
            {
                double angle = 2.0 * Math.PI * i / points;
                int dx = (int)Math.Round(radius * Math.Cos(angle));
                int dy = (int)Math.Round(radius * Math.Sin(angle));

                // Adjacent angles can round to the same tile on small rings.
                if (i > 0 && ((dx == lastDx && dy == lastDy) || (dx == firstDx && dy == firstDy)))
                    continue;

                if (i == 0)
                {
                    firstDx = dx;
                    firstDy = dy;
                }

                lastDx = dx;
                lastDy = dy;

                int z = center.Z;
                try { z = map.GetAverageZ(center.X + dx, center.Y + dy); } catch { }

                Effects.SendLocationEffect(new Point3D(center.X + dx, center.Y + dy, z), map, 0x3709, 16, 0, 0);
            }

            IPooledEnumerable eable = map.GetMobilesInRange(center, radius);
            try
            {
                foreach (Mobile m in eable)
                {
                    if (m == null || m == caster || m.Deleted || !m.Alive)
                        continue;
                    if (master != null && m == master)
                        continue;
                    if (m is DudeCreature)
                        continue;
                    if (DudeCreature.IsPackAlly(m, master))
                        continue;
                    if (!caster.CanBeHarmful(m))
                        continue;

                    BaseCreature bc = m as BaseCreature;
                    if (bc != null && master != null && bc.Controlled && bc.ControlMaster == master)
                        continue;

                    int dx = m.X - center.X;
                    int dy = m.Y - center.Y;
                    double d = Math.Sqrt(dx * dx + dy * dy);
                    if (Math.Abs(d - radius) > 0.6)
                        continue;

                    caster.DoHarmful(m);
                    AOS.Damage(m, caster, damage, 0, 100, 0, 0, 0);
                    DudeAbilityVfx.PlayFireHit(m, false);
                }
            }
            finally
            {
                eable.Free();
            }
        }
    }
}
