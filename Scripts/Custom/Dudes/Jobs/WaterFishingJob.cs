using System;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes.Jobs
{
    /// <summary>
    /// Water Dudes fish nearest valid water tile, same loop as Earth mining.
    /// </summary>
    public sealed class WaterFishingJob : GatheringJob
    {
        public WaterFishingJob()
            : base("water_fish", "Water Fishing")
        {
        }

        public override bool CanPerform(DudeData data)
        {
            return data != null && data.Type == DudeType.Water;
        }

        public override string GetResourceLabel()
        {
            return "Fish";
        }

        public override Type GetStoredRewardType()
        {
            return typeof(Fish);
        }

        public override int GetWorkAnimation(DudeData data)
        {
            return 12; // fish cast-ish
        }

        public override int GetWorkSound(DudeData data)
        {
            return 0x364;
        }

        protected override HarvestDefinition GetHarvestDefinition()
        {
            return Fishing.System.Definition;
        }

        protected override Item CreateGatheredItem(DudeData data, Map map, Point3D loc, Mobile harvester)
        {
            double skill = DudeJobHarvest.GetEffectiveSkill(data);
            HarvestDefinition def = GetHarvestDefinition();

            // Rare gold/gem still depletes the fishing bank at this spot.
            if (map != null && map != Map.Internal && harvester != null)
            {
                if (!DudeJobHarvest.HasResources(def, map, loc.X, loc.Y))
                    return null;

                HarvestBank bank = def.GetBank(map, loc.X, loc.Y);
                if (bank == null)
                    return null;

                int consume = def.ConsumedPerHarvest;
                if (consume > bank.Current)
                    consume = bank.Current;
                if (consume < 1)
                    return null;

                bank.Consume(consume, harvester);

                Item special = DudeJobHarvest.CreateFishingHaul(skill, Math.Max(1, DudeJobConfig.DefaultGatherRewardAmount));
                return special;
            }

            return DudeJobHarvest.CreateFishingHaul(skill, DudeJobConfig.DefaultGatherRewardAmount);
        }

        public override bool CountsTowardStorage(Item item)
        {
            if (item == null)
                return false;
            if (item is Fish || item is Gold)
                return true;
            return item is IGem;
        }

        public override bool TryFindDestination(DudeJobStation station, DudeData data, out Point3D destination, out int distance)
        {
            destination = Point3D.Zero;
            distance = 0;

            if (station == null || station.Deleted || station.Map == null || station.Map == Map.Internal)
                return false;

            Map map = station.Map;
            Point3D origin = station.Location;
            HarvestDefinition def = GetHarvestDefinition();
            int radius = DudeJobConfig.SearchRadius;

            Point3D best = Point3D.Zero;
            int bestDist = int.MaxValue;
            bool found = false;

            for (int r = 1; r <= radius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    // Ring edge cells only, in the original order (see EarthGatheringJob).
                    int dyStep = (dx == -r || dx == r) ? 1 : 2 * r;

                    for (int dy = -r; dy <= r; dy += dyStep)
                    {
                        int x = origin.X + dx;
                        int y = origin.Y + dy;

                        LandTile lt = map.Tiles.GetLandTile(x, y);
                        int tileId = lt.ID & 0x3FFF;

                        if (!def.Validate(tileId) && (tileId == lt.ID || !def.Validate(lt.ID)))
                            continue;

                        if (!DudeJobHarvest.HasResources(def, map, x, y))
                            continue;

                        int z = lt.Z;
                        Point3D candidate = new Point3D(x, y, z);
                        int dist = (int)Math.Sqrt((dx * dx) + (dy * dy));

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            best = candidate;
                            found = true;
                        }
                    }
                }

                if (found && bestDist <= r)
                    break;
            }

            if (!found)
                return false;

            destination = best;
            distance = bestDist;
            return true;
        }
    }
}
