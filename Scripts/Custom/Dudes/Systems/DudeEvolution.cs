using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Same-type evolution via elemental cores. No cross-type, no sacrifice.
    /// </summary>
    public static class DudeEvolution
    {
        public static bool TryEvolve(Mobile from, DudeBall ball, Item core, DudeType requiredType,
            string stage1Id, string stage2Id, string stage3Id)
        {
            if (from == null || ball == null || ball.Deleted || !ball.HasDude || core == null || core.Deleted)
                return false;

            if (!core.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001);
                return false;
            }

            DudeData data = ball.StoredDude;
            if (data == null)
                return false;

            if (data.Type != requiredType)
            {
                from.SendMessage("That essence only evolves {0}-type Dudes.", requiredType);
                return false;
            }

            string nextId;
            int requiredStage;
            int requiredLevel;

            if (string.Equals(data.DefinitionId, stage1Id, StringComparison.OrdinalIgnoreCase)
                && data.EvolutionStage == 1)
            {
                nextId = stage2Id;
                requiredStage = 1;
                requiredLevel = 10;
            }
            else if (string.Equals(data.DefinitionId, stage2Id, StringComparison.OrdinalIgnoreCase)
                && data.EvolutionStage == 2)
            {
                nextId = stage3Id;
                requiredStage = 2;
                requiredLevel = 20;
            }
            else
            {
                from.SendMessage("That essence only evolves {0} or {1} at their stage max level.",
                    Capitalize(stage1Id), Capitalize(stage2Id));
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

            data.DefinitionId = nextDef.Id;
            data.Type = nextDef.Type;
            data.AbilityId = nextDef.AbilityId;
            data.EvolutionStage = requiredStage + 1;
            data.EXPToNext = DudeExperience.GetExpRequiredForLevel(data.Level);

            data.UnlockAbility(nextDef.AbilityId);
            DudeExperience.EnsureEvolutionAbilities(data);

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
                DudeCombatSkills.RaiseCapsOnMobile(live, data);
                if (live.Map != null && live.Map != Map.Internal)
                    DudeSummonEffects.Play(requiredType, live.Location, live.Map, data.DefinitionId);
            }
            else if (from.Map != null && from.Map != Map.Internal)
            {
                DudeSummonEffects.Play(requiredType, from.Location, from.Map, data.DefinitionId);
            }

            if (DudeLinkSystem.IsLinked(from))
            {
                DudeBall linked = DudeLinkSystem.GetLinkedBall(from);
                if (linked == ball)
                    DudeCombatSkills.RaiseCapsOnMobile(from, data);
            }

            from.SendMessage(0x44, "{0} evolved into {1}!", oldName, nextDef.Name);
            from.PlaySound(0x208);

            core.Consume();
            return true;
        }

        private static string Capitalize(string id)
        {
            if (string.IsNullOrEmpty(id))
                return id;
            if (id.Length == 1)
                return id.ToUpperInvariant();
            return char.ToUpperInvariant(id[0]) + id.Substring(1);
        }
    }
}
