using System;
using System.Collections.Generic;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
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
            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("aftershock");
            double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.5;
            double stun = tune != null && tune.StunSeconds > 0.0 ? tune.StunSeconds : 1.0;
            int radius = tune != null && tune.Radius > 0 ? tune.Radius : 3;

            int damage = DudeAbility.ApplyEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(dude.DudeLevel) * vs)));
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
                if (dude.IsPackAlly(m))
                    continue;

                // Prefer nearby fight-list targets (Chebyshev ≤ radius).
                int dist = Math.Max(Math.Abs(m.X - dude.X), Math.Abs(m.Y - dude.Y));
                if (dist > radius)
                    continue;

                dude.DoHarmful(m);
                AOS.Damage(m, dude, damage, 100, 0, 0, 0, 0);
                DudeAbilityVfx.PlayEarthHit(m);
                m.Paralyze(TimeSpan.FromSeconds(stun));
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
            return true;
        }

        public override void ExecuteLinked(Mobile caster, DudeData data, DudeBall ball, Mobile target)
        {
            DudeAbilityConfig.EnsureLoaded();
            DudeAbilityTune tune = DudeAbilityConfig.Get("aftershock");
            double vs = tune != null && tune.DamageVsBlast > 0.0 ? tune.DamageVsBlast : 0.5;
            double stun = tune != null && tune.StunSeconds > 0.0 ? tune.StunSeconds : 1.0;
            int radius = tune != null && tune.Radius > 0 ? tune.Radius : 3;

            int damage = DudeAbility.ApplyEffect(Math.Max(1, (int)(DudeExperience.GetBlastDamage(data != null ? data.Level : 1) * vs)));
            caster.PublicOverheadMessage(MessageType.Regular, 0x3F, false, "*Aftershock*");
            caster.PlaySound(0x1F3);

            List<Mobile> list = new List<Mobile>();
            DudeAbilityVfx.CollectFightList(caster, caster, list);

            for (int i = 0; i < list.Count; i++)
            {
                Mobile m = list[i];
                if (m == null || m.Deleted || !m.Alive)
                    continue;
                if (m is PlayerMobile)
                    continue;
                if (m is DudeCreature)
                    continue;
                if (DudeCreature.IsPackAlly(m, caster))
                    continue;

                int dist = Math.Max(Math.Abs(m.X - caster.X), Math.Abs(m.Y - caster.Y));
                if (dist > radius)
                    continue;

                caster.DoHarmful(m);
                AOS.Damage(m, caster, damage, 100, 0, 0, 0, 0);
                DudeAbilityVfx.PlayEarthHit(m);
                m.Paralyze(TimeSpan.FromSeconds(stun));
            }
        }
    }
}
