using System;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes.Jobs
{
    /// <summary>
    /// First gathering job: Earth Dudes mine nearest valid ore tile (real world harvestables).
    /// </summary>
    public sealed class EarthGatheringJob : GatheringJob
    {
        public EarthGatheringJob()
            : base("earth_gather", "Earth Gathering")
        {
        }

        public override bool CanPerform(DudeData data)
        {
            return data != null && data.Type == DudeType.Earth;
        }

        public override string GetResourceLabel()
        {
            return "Ore";
        }

        public override Type GetStoredRewardType()
        {
            return typeof(BaseOre);
        }

        protected override HarvestDefinition GetHarvestDefinition()
        {
            return Mining.System.OreAndStone;
        }

        protected override Item CreateGatheredItem(DudeData data, Map map, Point3D loc, Mobile harvester)
        {
            double skill = DudeJobHarvest.GetEffectiveSkill(data);
            HarvestDefinition def = GetHarvestDefinition();

            if (map != null && map != Map.Internal && harvester != null)
            {
                Item harvested = DudeJobHarvest.HarvestAt(def, map, loc, harvester, skill, DudeJobConfig.DefaultGatherRewardAmount);
                if (harvested != null)
                    return harvested;
            }

            Item item = DudeJobHarvest.CreateFromVeins(def, skill, DudeJobConfig.DefaultGatherRewardAmount);
            if (item != null)
                return item;
            return new IronOre(DudeJobConfig.DefaultGatherRewardAmount);
        }

        public override bool CountsTowardStorage(Item item)
        {
            return item is BaseOre;
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
                    // Walk only the ring's edge cells (same order as before): every dy on the
                    // outer columns, otherwise just dy = -r and dy = r. Scanning the whole
                    // (2r+1)^2 square per ring and skipping the interior cost O(radius^3).
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
