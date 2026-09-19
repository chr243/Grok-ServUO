using Server.Custom.Dudes;
using System;
using Server.Engines.Harvest;
using Server.Items;

namespace Server.Custom.Dudes.Jobs
{
    /// <summary>
    /// Effective gathering skill from DudeData.GatherSkill (trainable), plus vein-style resource rolls.
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
                return DudeJobConfig.BaseGatherSkill;

            double skill = data.GatherSkill;
            if (skill < 0.0)
                skill = 0.0;
            if (skill > DudeJobConfig.MaxGatherSkill)
                skill = DudeJobConfig.MaxGatherSkill;
            return skill;
        }

        public static string FormatSkillLabel(DudeData data)
        {
            return string.Format("{0:0.#}", GetEffectiveSkill(data));
        }

        /// <summary>
        /// Chance to raise GatherSkill after a successful gather cycle. Harder as skill rises.
        /// </summary>
        public static bool TryGainGatherSkill(DudeData data)
        {
            if (data == null)
                return false;

            double skill = data.GatherSkill;
            double max = DudeJobConfig.MaxGatherSkill;
            if (skill >= max)
                return false;

            // Chance scales down as skill approaches cap (classic UO-ish).
            double chance = DudeJobConfig.GatherSkillGainChance * ((max - skill) / max);
            if (chance < 0.02)
                chance = 0.02;

            if (Utility.RandomDouble() >= chance)
                return false;

            double amount = DudeJobConfig.GatherSkillGainAmount;
            if (skill >= 100.0)
                amount *= 0.25;
            else if (skill >= 70.0)
                amount *= 0.5;

            if (amount < 0.01)
                amount = 0.01;

            data.GatherSkill = skill + amount;
            return true;
        }


        public static bool HasResources(HarvestDefinition def, Map map, int x, int y)
        {
            if (def == null || map == null || map == Map.Internal)
                return false;

            HarvestBank bank = def.GetBank(map, x, y);
            return bank != null && bank.Current >= 1;
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
            if (bank == null || bank.Current < 1)
                return null;

            // Job stations pay a fixed small stack — never player lumberjack 10/20 per swing.
            int give = minGiveAmount > 0 ? minGiveAmount : 3;
            if (give < 1)
                give = 1;

            Item item = CreateFromVein(def, bank.Vein, skill, give);
            if (item == null)
                return null;

            // Deplete one unit per successful cycle (not ConsumedPerHarvest).
            int consume = 1;
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

        /// <summary>
        /// Job-station lumber table (UOR-safe). Does not depend on Core.ML player lumber veins.
        /// </summary>
        public static Item CreateLumberHaul(double skill, int amount)
        {
            if (amount < 1)
                amount = 1;

            Type type = typeof(Log);

            if (skill < 50.0)
            {
                type = typeof(Log);
            }
            else if (skill < 70.0)
            {
                // 70% Log, 30% OakLog
                type = Utility.RandomDouble() < 0.70 ? typeof(Log) : typeof(OakLog);
            }
            else if (skill < 85.0)
            {
                // 50% Log, 30% Oak, 20% Ash
                double roll = Utility.RandomDouble();
                if (roll < 0.50)
                    type = typeof(Log);
                else if (roll < 0.80)
                    type = typeof(OakLog);
                else
                    type = typeof(AshLog);
            }
            else if (skill < 100.0)
            {
                // 35% Log, 30% Oak, 20% Ash, 15% Yew
                double roll = Utility.RandomDouble();
                if (roll < 0.35)
                    type = typeof(Log);
                else if (roll < 0.65)
                    type = typeof(OakLog);
                else if (roll < 0.85)
                    type = typeof(AshLog);
                else
                    type = typeof(YewLog);
            }
            else
            {
                // 25% Log, 25% Oak, 20% Ash, 15% Yew, 8% Heartwood, 5% Bloodwood, 2% Frostwood
                double roll = Utility.RandomDouble();
                if (roll < 0.25)
                    type = typeof(Log);
                else if (roll < 0.50)
                    type = typeof(OakLog);
                else if (roll < 0.70)
                    type = typeof(AshLog);
                else if (roll < 0.85)
                    type = typeof(YewLog);
                else if (roll < 0.93)
                    type = typeof(HeartwoodLog);
                else if (roll < 0.98)
                    type = typeof(BloodwoodLog);
                else
                    type = typeof(FrostwoodLog);
            }

            Item item = Construct(type, amount);
            if (item != null)
                return item;
            return new Log(amount);
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
