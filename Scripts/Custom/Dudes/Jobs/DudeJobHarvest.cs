using Server.Custom.Dudes;
using System;
using Server.Engines.Harvest;
using Server.Items;

namespace Server.Custom.Dudes.Jobs
{
    /// <summary>
    /// Effective gathering skill from Dude level, plus vein-style resource rolls.
    /// Formula: BaseGatherSkill + (Level * GatherSkillPerLevel), capped at 120.
    /// </summary>
    public static class DudeJobHarvest
    {
        private static readonly Type[] FishingGems = new Type[]
        {
            typeof(Amber),
            typeof(Citrine),
            typeof(Ruby),
            typeof(Tourmaline),
            typeof(Amethyst),
            typeof(Sapphire),
            typeof(Emerald),
            typeof(StarSapphire),
            typeof(Diamond)
        };

        public static double GetEffectiveSkill(DudeData data)
        {
            if (data == null)
                return DudeExperience.GetSkillCapForLevel(1);

            return DudeExperience.GetSkillCapForLevel(data.Level);
        }

        public static string FormatSkillLabel(DudeData data)
        {
            return string.Format("{0:0.#}", GetEffectiveSkill(data));
        }


        public static bool HasResources(HarvestDefinition def, Map map, int x, int y)
        {
            if (def == null || map == null || map == Map.Internal)
                return false;

            HarvestBank bank = def.GetBank(map, x, y);
            return bank != null && bank.Current >= def.ConsumedPerHarvest;
        }

        /// <summary>
        /// Consume from the harvest bank at loc (same banks players use) and build a reward from that vein.
        /// Returns null if the node is depleted. Pass any Mobile for Consume's race-bonus check (worker/owner).
        /// </summary>
        public static Item HarvestAt(HarvestDefinition def, Map map, Point3D loc, Mobile from, double skill, int minGiveAmount)
        {
            if (def == null || map == null || map == Map.Internal || from == null)
                return null;

            HarvestBank bank = def.GetBank(map, loc.X, loc.Y);
            if (bank == null || bank.Current < def.ConsumedPerHarvest)
                return null;

            int give = def.ConsumedPerHarvest;
            if (map.Rules == MapRules.FeluccaRules)
                give = def.ConsumedPerFeluccaHarvest;
            if (minGiveAmount > give)
                give = minGiveAmount;

            if (give > bank.Current)
                give = bank.Current;
            if (give < 1)
                return null;

            Item item = CreateFromVein(def, bank.Vein, skill, give);
            if (item == null)
                return null;

            int consume = def.ConsumedPerHarvest;
            if (consume > bank.Current)
                consume = bank.Current;

            bank.Consume(consume, from);
            return item;
        }

        public static Item CreateFromVein(HarvestDefinition def, HarvestVein vein, double skill, int amount)
        {
            if (def == null)
                return null;

            if (amount < 1)
                amount = 1;

            if (vein == null)
            {
                if (def.Veins == null || def.Veins.Length == 0)
                    return null;
                vein = def.GetVeinFrom(Utility.RandomDouble());
                if (vein == null)
                    vein = def.Veins[0];
            }

            HarvestResource primary = vein.PrimaryResource;
            HarvestResource fallback = vein.FallbackResource;
            HarvestResource resource = primary;

            if (vein.ChanceToFallback > Utility.RandomDouble())
            {
                if (fallback != null)
                    resource = fallback;
            }

            if (resource != null && (skill < resource.ReqSkill || skill < resource.MinSkill))
            {
                if (fallback != null)
                    resource = fallback;
                else if (def.Resources != null && def.Resources.Length > 0)
                    resource = def.Resources[0];
            }

            if (primary != null && resource == primary && primary.MaxSkill > primary.MinSkill)
            {
                double chance = (skill - primary.MinSkill) / (primary.MaxSkill - primary.MinSkill);
                if (chance < Utility.RandomDouble() && fallback != null)
                    resource = fallback;
            }

            if (resource == null || resource.Types == null || resource.Types.Length == 0)
                return null;

            return Construct(resource.Types[0], amount);
        }

        public static Item CreateFromVeins(HarvestDefinition def, double skill, int amount)
        {
            return CreateFromVein(def, null, skill, amount);
        }

        public static Item CreateFishingHaul(double skill, int amount)
        {
            if (amount < 1)
                amount = 1;

            // Higher skill → rare gold / gem instead of fish (not every cycle).
            if (skill >= 90.0)
            {
                double goldChance = (skill - 90.0) / 60.0; // ~0 at 90, ~0.5 at 120
                if (goldChance > 0.35)
                    goldChance = 0.35;
                if (Utility.RandomDouble() < goldChance)
                    return new Gold(Utility.RandomMinMax(25, 75 + (int)skill));
            }

            if (skill >= 70.0)
            {
                double gemChance = (skill - 70.0) / 100.0; // ~0 at 70, ~0.5 at 120
                if (gemChance > 0.25)
                    gemChance = 0.25;
                if (Utility.RandomDouble() < gemChance)
                {
                    Type gemType = FishingGems[Utility.Random(FishingGems.Length)];
                    Item gem = Construct(gemType, 1);
                    if (gem != null)
                        return gem;
                }
            }

            return new Fish(amount);
        }

        public static Item Construct(Type type, int amount)
        {
            if (type == null)
                return null;

            try
            {
                if (amount > 1)
                {
                    object created = Activator.CreateInstance(type, new object[] { amount });
                    Item stacked = created as Item;
                    if (stacked != null)
                        return stacked;
                }

                object plain = Activator.CreateInstance(type);
                Item item = plain as Item;
                if (item != null && item.Stackable && amount > 1)
                    item.Amount = amount;
                return item;
            }
            catch
            {
                try
                {
                    return Activator.CreateInstance(type) as Item;
                }
                catch
                {
                    return null;
                }
            }
        }
    }
}
