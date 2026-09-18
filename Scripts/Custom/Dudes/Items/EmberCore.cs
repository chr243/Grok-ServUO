using System;
using Server.Custom.Dudes;
using Server.Mobiles;
using Server.Targeting;

namespace Server.Items
{
    /// <summary>
    /// Ember Essence — evolves the Embit fire line (Embit → Emberon → Infernox).
    /// Also drops from Emberlord and rarely from wild Fire Dudes.
    /// </summary>
    public class EmberCore : Item
    {
        [Constructable]
        public EmberCore()
            : this(1)
        {
        }

        [Constructable]
        public EmberCore(int amount)
            : base(0x1F1C) // classic power-crystal graphic (UOR-friendly)
        {
            Name = "Ember Essence";
            Hue = 1161;
            Stackable = true;
            Amount = amount;
            Weight = 1.0;
        }

        public EmberCore(Serial serial)
            : base(serial)
        {
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);
            list.Add("Evolution material");
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || Deleted)
                return;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return;
            }

            from.SendMessage("Target a Dude Ball or summoned Dude to evolve.");
            from.Target = new EvolveTarget(this);
        }

        public bool TryEvolve(Mobile from, DudeBall ball)
        {
            if (from == null || Deleted || ball == null || ball.Deleted || !ball.HasDude)
                return false;

            if (!IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return false;
            }

            DudeData data = ball.StoredDude;
            if (data == null)
                return false;

            // Must be owned / catcher or ball in pack (already checked pack).
            if (data.Catcher != null && data.Catcher != from && from.AccessLevel < AccessLevel.GameMaster)
            {
                // Allow if ball is in their pack regardless of catcher.
            }

            string nextId;
            int requiredStage;
            int requiredLevel;
            string unlockAbility = null;

            if (string.Equals(data.DefinitionId, "embit", StringComparison.OrdinalIgnoreCase)
                && data.EvolutionStage == 1)
            {
                nextId = "emberon";
                requiredStage = 1;
                requiredLevel = 10;
                unlockAbility = "ring_of_fire";
            }
            else if (string.Equals(data.DefinitionId, "emberon", StringComparison.OrdinalIgnoreCase)
                && data.EvolutionStage == 2)
            {
                nextId = "infernox";
                requiredStage = 2;
                requiredLevel = 20;
                unlockAbility = "burn"; // display + legacy unlock for Infernox passive
            }
            else
            {
                from.SendMessage("Ember Essence only evolves Embit or Emberon at their stage max level.");
                return false;
            }

            if (data.EvolutionStage != requiredStage)
            {
                from.SendMessage("{0} is not ready for that evolution.", data.DisplayName);
                return false;
            }

            int maxForStage = DudeExperience.GetMaxLevel(data);
            if (data.Level < requiredLevel || data.Level < maxForStage)
            {
                from.SendMessage("{0} must reach level {1} before evolving.", data.DisplayName, requiredLevel);
                return false;
            }

            DudeDefinition nextDef = DudeRegistry.Get(nextId);
            if (nextDef == null)
            {
                from.SendMessage("Evolution data is missing.");
                return false;
            }

            string oldName = data.DisplayName;
            string oldSpecies = null;
            DudeDefinition oldDef = DudeRegistry.Get(data.DefinitionId);
            if (oldDef != null)
                oldSpecies = oldDef.Name;

            // Keep Level, CurrentEXP, stats; bump stage and species.
            data.DefinitionId = nextDef.Id;
            data.Type = nextDef.Type;
            data.AbilityId = nextDef.AbilityId;
            data.EvolutionStage = requiredStage + 1;
            data.EXPToNext = DudeExperience.GetExpRequiredForLevel(data.Level);

            // Keep prior-tier abilities; stage 3 must have Blast + Ring of Fire + Burn.
            data.UnlockAbility(nextDef.AbilityId); // blast
            if (string.Equals(nextId, "emberon", StringComparison.OrdinalIgnoreCase)
                || string.Equals(nextId, "infernox", StringComparison.OrdinalIgnoreCase))
                data.UnlockAbility("ring_of_fire");
            if (string.Equals(nextId, "infernox", StringComparison.OrdinalIgnoreCase))
                data.UnlockAbility("burn");
            else if (!string.IsNullOrEmpty(unlockAbility))
                data.UnlockAbility(unlockAbility);

            DudeExperience.EnsureEvolutionAbilities(data);

            // Refresh display name if still default old species name.
            if (!string.IsNullOrEmpty(data.CustomName)
                && (string.Equals(data.CustomName, oldSpecies, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(data.CustomName, oldName, StringComparison.OrdinalIgnoreCase)))
            {
                data.CustomName = nextDef.Name;
            }
            else if (string.IsNullOrEmpty(data.CustomName))
            {
                data.CustomName = nextDef.Name;
            }

            ball.RefreshHue();
            ball.InvalidateProperties();

            DudeCreature live = ball.SummonedDude;
            if (live != null && !live.Deleted)
            {
                live.ApplyData(data, false);
                if (live.Map != null && live.Map != Map.Internal)
                    DudeSummonEffects.Play(DudeType.Fire, live.Location, live.Map, data.DefinitionId);
            }
            else if (from.Map != null && from.Map != Map.Internal)
            {
                DudeSummonEffects.Play(DudeType.Fire, from.Location, from.Map, data.DefinitionId);
            }

            from.SendMessage(0x44, "{0} evolved into {1}!", oldName, nextDef.Name);
            from.PlaySound(0x208);

            Consume();
            return true;
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }

        private class EvolveTarget : Target
        {
            private readonly EmberCore m_Core;

            public EvolveTarget(EmberCore core)
                : base(8, false, TargetFlags.None)
            {
                m_Core = core;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_Core == null || m_Core.Deleted)
                    return;

                DudeBall ball = targeted as DudeBall;
                if (ball == null)
                {
                    DudeCreature dude = targeted as DudeCreature;
                    if (dude != null && !dude.IsWild)
                        ball = dude.BoundBall;
                }

                if (ball == null || !ball.HasDude)
                {
                    from.SendMessage("That is not a filled Dude Ball or summoned Dude.");
                    return;
                }

                m_Core.TryEvolve(from, ball);
            }
        }
    }
}
