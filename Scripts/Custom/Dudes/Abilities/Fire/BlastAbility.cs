using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
    public sealed class BlastAbility : DudeAbility
    {
        public BlastAbility()
            : base("blast", "Fire Blast", TimeSpan.FromSeconds(10.0), 5, 1, DudeType.Fire)
        {
        }

        public override void Execute(DudeCreature dude, Mobile target)
        {
            if (target == null || target.Deleted || !target.Alive)
                return;
            if (target is DudeCreature || dude.IsPackAlly(target))
                return;

            DudeAbilityConfig.EnsureLoaded();
            int damage = DudeAbility.ApplyEffect(DudeExperience.GetBlastDamage(dude.DudeLevel));
            dude.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Fire Blast*");
            AOS.Damage(target, dude, damage, 0, 100, 0, 0, 0);
            Effects.SendMovingEffect(dude, target, 0x36BD, 7, 0, false, false, 0, 0);
            DudeAbilityVfx.PlayFireHit(target, true);
        }

        public override void ExecuteLinked(Mobile caster, DudeData data, DudeBall ball, Mobile target)
        {
            if (target == null || target.Deleted || !target.Alive)
                return;
            if (target is DudeCreature || DudeCreature.IsPackAlly(target, caster))
                return;

            DudeAbilityConfig.EnsureLoaded();
            int damage = DudeAbility.ApplyEffect(DudeExperience.GetBlastDamage(data != null ? data.Level : 1));
            caster.PublicOverheadMessage(MessageType.Regular, 0x22, false, "*Fire Blast*");
            AOS.Damage(target, caster, damage, 0, 100, 0, 0, 0);
            Effects.SendMovingEffect(caster, target, 0x36BD, 7, 0, false, false, 0, 0);
            DudeAbilityVfx.PlayFireHit(target, true);
        }
    }
}
