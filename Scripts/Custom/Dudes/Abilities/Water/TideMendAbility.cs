using System;
using Server.Items;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.Dudes
{
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
            DudeAbilityConfig.EnsureLoaded();
            int heal = DudeAbility.ApplyEffect(DudeExperience.GetBlastDamage(dude.DudeLevel));
            dude.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*Tide Mend*");
            dude.Heal(heal, dude, false);
            DudeAbilityVfx.PlayWaterHeal(dude);
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
            int heal = DudeAbility.ApplyEffect(DudeExperience.GetBlastDamage(data != null ? data.Level : 1));
            caster.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "*Tide Mend*");
            caster.Heal(heal, caster, false);
            DudeAbilityVfx.PlayWaterHeal(caster);
        }
    }
}
