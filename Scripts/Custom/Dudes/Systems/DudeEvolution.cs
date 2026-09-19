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
        public static int GetCoreCost(int currentStage)
        {
            if (currentStage == 1)
                return 10;
            if (currentStage == 2)
                return 30;
            return 0;
        }

        public static bool CanEvolve(DudeData data)
        {
            if (data == null)
                return false;

            if (data.EvolutionStage == 1 && data.Level >= 10 && data.Level >= DudeExperience.GetMaxLevel(1))
                return true;

            if (data.EvolutionStage == 2 && data.Level >= 20 && data.Level >= DudeExperience.GetMaxLevel(2))
                return true;

            return false;
        }

        public static Type GetRequiredCoreType(DudeType t)
        {
            switch (t)
            {
                case DudeType.Fire:
                    return typeof(EmberCore);
                case DudeType.Water:
                    return typeof(TideCore);
                case DudeType.Earth:
                    return typeof(StoneCore);
                case DudeType.Air:
                    return typeof(GaleCore);
                default:
                    return null;
            }
        }

        public static string GetCoreDisplayName(DudeType t)
        {
            switch (t)
            {
                case DudeType.Fire:
                    return "Ember Essence";
                case DudeType.Water:
                    return "Tide Essence";
                case DudeType.Earth:
                    return "Stone Essence";
                case DudeType.Air:
                    return "Gale Essence";
                default:
                    return "Essence";
            }
        }

        public static bool CanPlayerEvolveBall(Mobile from, DudeBall ball)
        {
            if (from == null || ball == null || ball.Deleted || !ball.HasDude)
                return false;

            if (!CanEvolve(ball.StoredDude))
                return false;

            if (ball.IsChildOf(from.Backpack))
                return true;

            DudeCreature live = ball.SummonedDude;
            if (live != null && !live.Deleted && live.ControlMaster == from)
                return true;

            return false;
        }

        public static bool ConsumeMatchingCores(Mobile from, Type coreType, int cost, Item preferredStack)
        {
            if (from == null || from.Backpack == null || coreType == null || cost < 1)
                return false;

            if (preferredStack != null
                && (preferredStack.Deleted
                    || preferredStack.GetType() != coreType
                    || !preferredStack.IsChildOf(from.Backpack)))
            {
                preferredStack = null;
            }

            Item[] found = from.Backpack.FindItemsByType(coreType, true);
            int total = 0;

            for (int i = 0; i < found.Length; i++)
            {
                Item item = found[i];
                if (item != null && !item.Deleted)
                    total += item.Amount;
            }

            string name = ResolveCoreName(coreType, preferredStack, found);

            if (total < cost)
            {
                from.SendMessage("You need {0} {1}.", cost, name);
                return false;
            }

            int remaining = cost;

            if (preferredStack != null && !preferredStack.Deleted)
                remaining = ConsumeFromStack(preferredStack, remaining);

            if (remaining > 0)
            {
                found = from.Backpack.FindItemsByType(coreType, true);

                for (int i = 0; i < found.Length && remaining > 0; i++)
                {
                    Item item = found[i];
                    if (item == null || item.Deleted)
                        continue;

                    remaining = ConsumeFromStack(item, remaining);
                }
            }

            return remaining <= 0;
        }

        private static int ConsumeFromStack(Item stack, int remaining)
        {
            if (stack == null || stack.Deleted || remaining <= 0)
                return remaining;

            if (stack.Amount <= remaining)
            {
                remaining -= stack.Amount;
                stack.Delete();
            }
            else
            {
                stack.Amount -= remaining;
                remaining = 0;
            }

            return remaining;
        }

        private static string ResolveCoreName(Type coreType, Item preferredStack, Item[] found)
        {
            if (preferredStack != null && !string.IsNullOrEmpty(preferredStack.Name))
                return preferredStack.Name;

            if (found != null)
            {
                for (int i = 0; i < found.Length; i++)
                {
                    if (found[i] != null && !found[i].Deleted && !string.IsNullOrEmpty(found[i].Name))
                        return found[i].Name;
                }
            }

            if (coreType == typeof(EmberCore))
                return "Ember Essence";
            if (coreType == typeof(TideCore))
                return "Tide Essence";
            if (coreType == typeof(StoneCore))
                return "Stone Essence";
            if (coreType == typeof(GaleCore))
                return "Gale Essence";

            return "essences";
        }

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

            int cost = GetCoreCost(requiredStage);
            if (cost < 1 || !ConsumeMatchingCores(from, core.GetType(), cost, core))
                return false;

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

            string oldSpeciesDude = !string.IsNullOrEmpty(oldSpecies) ? oldSpecies + " Dude" : null;
            if (!string.IsNullOrEmpty(data.CustomName)
                && (string.Equals(data.CustomName, oldSpecies, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(data.CustomName, oldSpeciesDude, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(data.CustomName, oldName, StringComparison.OrdinalIgnoreCase)))
            {
                data.CustomName = nextDef.Name + " Dude";
            }
            else if (string.IsNullOrEmpty(data.CustomName))
            {
                data.CustomName = nextDef.Name + " Dude";
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
